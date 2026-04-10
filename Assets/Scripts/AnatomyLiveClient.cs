using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using TMPro;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class AnatomyLiveClient : MonoBehaviour
{
    public event Action<string> ContextUpdated;
    public event Action<string> QuestionSent;
    public event Action TeacherTurnCompleted;

    public enum TeacherPersonalityPreset
    {
        FriendlyTeacher,
        QuizTeacher,
        ExamCoach,
        BeginnerGuide,
        SocraticTutor
    }

    [Header("Connection Settings")]
    public string backendUrl = "ws://127.0.0.1:8080/live"; // Change to computer's IP for Quest build
    public string modelContext = "inner_ear";
    public TeacherPersonalityPreset teacherPersonality = TeacherPersonalityPreset.FriendlyTeacher;
    [TextArea(2, 5)]
    public string extraContext = "";
    [TextArea(2, 5)]
    public string customPersonalityInstructions = "";
    public bool reconnectOnClose = true;
    public float reconnectDelaySeconds = 2f;
    
    [Header("Audio Output (Teacher Voice)")]
    public AudioSource teacherAudioSource;
    public bool autoAddAudioSource = true;
    private Queue<float> audioOutputBuffer = new Queue<float>();
    private AudioClip dynamicOutputClip;
    private int outputSampleRate = 24000;
    private float lastReceivedTeacherAudioTime = -999f;
    public bool IsTeacherAudioActive
    {
        get
        {
            lock (audioOutputBuffer)
            {
                if (audioOutputBuffer.Count > 0)
                {
                    return true;
                }
            }

            return teacherAudioSource != null &&
                   teacherAudioSource.isPlaying &&
                   (Time.unscaledTime - lastReceivedTeacherAudioTime) < 0.2f;
        }
    }

    [Serializable]
    private class BackendEvent
    {
        public string type;
        public string text;
    }

    [Header("Audio Input (Microphone)")]
    public bool enableMicrophoneStreaming = true;
    public string microphoneName = null; // null uses default mic (Quest built-in mic)
    private AudioClip micClip;
    private int micReadPosition = 0;
    private int inputSampleRate = 16000; // Gemini Live usually prefers 16kHz

    [Header("Optional Text UI")]
    public bool autoHookQuestionInputFields = true;
    public TMP_InputField[] questionInputFields;
    public TMP_Text latestUserText;
    public TMP_Text latestTeacherText;
    public TMP_Text connectionStatusText;

    [Header("Optional Personality UI")]
    public TMP_Dropdown personalityDropdown;
    public bool autoConfigurePersonalityDropdown = true;
    
    private WebSocket websocket;
    private bool isQuitting;
    private bool reconnectCoroutineRunning;
    private bool suppressReconnect;
    private string currentSelectedObject = "";
    public bool IsConnected { get; private set; }
    public string CurrentSelectedObject => currentSelectedObject;

    [Serializable]
    private class OutboundPacket
    {
        public string type;
        public string selected_object;
        public string model_context;
        public string extra_context;
        public string question;
    }

    async void Start()
    {
        EnsureAudioSource();
        SetupDynamicAudioClip();
        HookQuestionInputFields();
        ConfigurePersonalityDropdown();
        await ConnectToBackend();
    }

    private void EnsureAudioSource()
    {
        if (teacherAudioSource == null)
        {
            teacherAudioSource = GetComponent<AudioSource>();
        }

        if (teacherAudioSource == null && autoAddAudioSource)
        {
            teacherAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (teacherAudioSource == null)
        {
            Debug.LogError("[AnatomyLiveClient] No AudioSource assigned for teacher output.");
            return;
        }

        teacherAudioSource.playOnAwake = false;
        teacherAudioSource.loop = true;
    }

    private void SetupDynamicAudioClip()
    {
        if (teacherAudioSource == null)
        {
            return;
        }

        dynamicOutputClip = AudioClip.Create("TeacherVoice", outputSampleRate * 10, 1, outputSampleRate, true, OnAudioRead);
        teacherAudioSource.clip = dynamicOutputClip;
    }

    private async Task ConnectToBackend()
    {
        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            SetConnectionStatus("Backend URL missing");
            Debug.LogError("[AnatomyLiveClient] Backend URL is empty.");
            return;
        }

        if (websocket != null &&
            (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting))
        {
            return;
        }

        SetConnectionStatus("Connecting...");
        websocket = new WebSocket(backendUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("[AnatomyLiveClient] Connected to Gemini Backend!");
            IsConnected = true;
            SetConnectionStatus("Connected");

            if (enableMicrophoneStreaming)
            {
                StartCoroutine(BeginMicrophoneCapture());
            }
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"[AnatomyLiveClient] Error: {e}");
            SetConnectionStatus("Connection error");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log($"[AnatomyLiveClient] Connection closed: {e}");
            IsConnected = false;
            SetConnectionStatus("Disconnected");
            StopMicrophone();

            if (!isQuitting && !suppressReconnect && reconnectOnClose)
            {
                StartCoroutine(ReconnectAfterDelay());
            }
        };

        websocket.OnMessage += (bytes) =>
        {
            HandleIncomingMessage(bytes);
        };

        try
        {
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AnatomyLiveClient] Failed to connect: {ex.Message}");
            SetConnectionStatus("Connect failed");

            if (!isQuitting && !suppressReconnect && reconnectOnClose)
            {
                StartCoroutine(ReconnectAfterDelay());
            }
        }
    }

    void Update()
    {
        if (websocket != null)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
            #endif
        }

        if (IsConnected && Microphone.IsRecording(microphoneName))
        {
            ProcessMicrophoneData();
        }
    }

    private System.Collections.IEnumerator BeginMicrophoneCapture()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            float timeout = 5f;

            while (timeout > 0f && !Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            SetConnectionStatus("Mic permission denied");
            Debug.LogError("[AnatomyLiveClient] Microphone permission denied.");
            yield break;
        }
        #endif

        StartMicrophone();
        yield break;
    }

    private void StartMicrophone()
    {
        StopMicrophone();

        if (Microphone.devices.Length > 0)
        {
            micClip = Microphone.Start(microphoneName, true, 10, inputSampleRate);
            micReadPosition = 0;
            Debug.Log("[AnatomyLiveClient] Microphone started capturing.");
            SetConnectionStatus("Connected - listening");
        }
        else
        {
            Debug.LogError("[AnatomyLiveClient] No microphone detected!");
            SetConnectionStatus("No microphone found");
        }
    }

    private void StopMicrophone()
    {
        if (!string.IsNullOrEmpty(microphoneName))
        {
            if (Microphone.IsRecording(microphoneName))
            {
                Microphone.End(microphoneName);
            }
        }
        else if (micClip != null)
        {
            Microphone.End(null);
        }

        micClip = null;
        micReadPosition = 0;
    }

    private void ProcessMicrophoneData()
    {
        if (micClip == null || websocket == null || websocket.State != WebSocketState.Open)
        {
            return;
        }

        int micCurrentPosition = Microphone.GetPosition(microphoneName);
        if (micCurrentPosition < 0 || micReadPosition == micCurrentPosition)
        {
            return;
        }

        if (micCurrentPosition > micReadPosition)
        {
            ReadAndSendMicrophoneSamples(micReadPosition, micCurrentPosition - micReadPosition);
        }
        else
        {
            ReadAndSendMicrophoneSamples(micReadPosition, micClip.samples - micReadPosition);

            if (micCurrentPosition > 0)
            {
                ReadAndSendMicrophoneSamples(0, micCurrentPosition);
            }
        }

        micReadPosition = micCurrentPosition;
    }

    private void ReadAndSendMicrophoneSamples(int offset, int length)
    {
        if (length <= 0 || micClip == null)
        {
            return;
        }

        float[] samples = new float[length];
        micClip.GetData(samples, offset);
        SendAudioToBackend(samples);
    }

    void SendAudioToBackend(float[] samples)
    {
        if (websocket == null || websocket.State != WebSocketState.Open || samples == null || samples.Length == 0)
        {
            return;
        }

        // Convert Unity float samples (-1.0 to 1.0) into 16-bit PCM bytes
        short[] pcm16 = new short[samples.Length];
        byte[] bytes = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            float val = samples[i] * 32767f;
            pcm16[i] = (short)Mathf.Clamp(val, short.MinValue, short.MaxValue);
            byte[] byteBlock = BitConverter.GetBytes(pcm16[i]);
            bytes[i * 2] = byteBlock[0];
            bytes[i * 2 + 1] = byteBlock[1];
        }

        _ = websocket.Send(bytes);
    }

    private void HandleIncomingMessage(byte[] data)
    {
        try
        {
            string textTry = System.Text.Encoding.UTF8.GetString(data);
            if (textTry.StartsWith("{") && textTry.EndsWith("}"))
            {
                var evt = JsonUtility.FromJson<BackendEvent>(textTry);
                if (evt != null && evt.type == "interrupted")
                {
                    lock (audioOutputBuffer)
                    {
                        audioOutputBuffer.Clear();
                    }
                    lastReceivedTeacherAudioTime = -999f;
                    teacherAudioSource.Stop();
                    Debug.Log("[AnatomyLiveClient] Gemini interrupted. Cleared playback buffer.");
                }
                else if (evt != null && evt.type == "turn_complete")
                {
                    SetConnectionStatus("Connected - ready");
                    TeacherTurnCompleted?.Invoke();
                }
                else if (evt != null && (evt.type == "text" || evt.type == "output_transcription" || evt.type == "input_transcription"))
                {
                    Debug.Log($"[AnatomyLiveClient] {evt.type}: {evt.text}");
                    if (evt.type == "input_transcription" && latestUserText != null)
                    {
                        latestUserText.text = evt.text;
                    }
                    else if ((evt.type == "text" || evt.type == "output_transcription") && latestTeacherText != null)
                    {
                        latestTeacherText.text = evt.text;
                    }
                }
            }
            else
            {
                float[] audioFloats = new float[data.Length / 2];
                for (int i = 0; i < data.Length; i += 2)
                {
                    short sample16 = BitConverter.ToInt16(data, i);
                    audioFloats[i / 2] = sample16 / 32768f;
                }

                lock (audioOutputBuffer)
                {
                    foreach (var f in audioFloats) audioOutputBuffer.Enqueue(f);
                }
                lastReceivedTeacherAudioTime = Time.unscaledTime;

                if (teacherAudioSource != null && !teacherAudioSource.isPlaying)
                {
                    teacherAudioSource.Play();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing incoming message: " + e.Message);
        }
    }

    // Called dynamically by Unity's Audio system when the AudioClip needs data
    private void OnAudioRead(float[] data)
    {
        lock (audioOutputBuffer)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (audioOutputBuffer.Count > 0)
                    data[i] = audioOutputBuffer.Dequeue();
                else
                    data[i] = 0.0f; // Silence if no data
            }
        }
    }

    private string BuildEffectiveExtraContext()
    {
        List<string> contextSections = new List<string>();
        string presetInstructions = GetPersonalityInstructions(teacherPersonality);

        if (!string.IsNullOrWhiteSpace(presetInstructions))
        {
            contextSections.Add(presetInstructions.Trim());
        }

        if (!string.IsNullOrWhiteSpace(customPersonalityInstructions))
        {
            contextSections.Add(customPersonalityInstructions.Trim());
        }

        if (!string.IsNullOrWhiteSpace(extraContext))
        {
            contextSections.Add(extraContext.Trim());
        }

        return string.Join("\n\n", contextSections);
    }

    private static string GetPersonalityInstructions(TeacherPersonalityPreset preset)
    {
        switch (preset)
        {
            case TeacherPersonalityPreset.QuizTeacher:
                return "You are a friendly anatomy quiz teacher in a VR learning app. Focus on the anatomy structure the student is interacting with. Answer clearly and briefly, then sometimes ask a short follow-up quiz question to check understanding. Keep responses encouraging, accurate, and easy to understand.";
            case TeacherPersonalityPreset.ExamCoach:
                return "You are an anatomy exam coach in a VR learning app. Give precise, high-yield answers that help the student prepare for tests and oral exams. Emphasize important functions, locations, relations, and clinically relevant facts without becoming overly long.";
            case TeacherPersonalityPreset.BeginnerGuide:
                return "You are a beginner-friendly anatomy teacher in a VR learning app. Use simple language, explain unfamiliar terms briefly, and keep answers short, clear, and supportive. Prioritize understanding over technical detail unless the student asks for more.";
            case TeacherPersonalityPreset.SocraticTutor:
                return "You are a Socratic anatomy tutor in a VR learning app. Guide the student with concise explanations and gentle follow-up questions that help them think through the answer. Stay supportive and avoid sounding like an interrogation.";
            case TeacherPersonalityPreset.FriendlyTeacher:
            default:
                return "You are a friendly anatomy teacher helping a student learn in VR. Focus on the anatomy part the student is interacting with and answer in a short, clear, educational way. Keep responses concise, accurate, and easy to understand.";
        }
    }

    public void SetTeacherPersonality(int presetIndex)
    {
        if (!Enum.IsDefined(typeof(TeacherPersonalityPreset), presetIndex))
        {
            Debug.LogWarning($"[AnatomyLiveClient] Invalid personality preset index: {presetIndex}");
            return;
        }

        teacherPersonality = (TeacherPersonalityPreset)presetIndex;
        SyncPersonalityDropdownValue();
        Debug.Log($"[AnatomyLiveClient] Teacher personality set to {teacherPersonality}.");

        if (IsConnected && !string.IsNullOrWhiteSpace(currentSelectedObject))
        {
            SendContextUpdate(currentSelectedObject);
        }
    }

    public void SetTeacherPersonalityByName(string presetName)
    {
        string normalizedPresetName = NormalizePresetName(presetName);
        foreach (TeacherPersonalityPreset preset in Enum.GetValues(typeof(TeacherPersonalityPreset)))
        {
            if (NormalizePresetName(preset.ToString()) == normalizedPresetName)
            {
                SetTeacherPersonality((int)preset);
                return;
            }
        }

        Debug.LogWarning($"[AnatomyLiveClient] Unknown personality preset: {presetName}");
    }

    // Update Context Context so the AI knows what part of the ear the user is pointing at
    public async void SendContextUpdate(string selectedModelName)
    {
        if (!IsConnected || websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("[AnatomyLiveClient] Cannot send context update while disconnected.");
            return;
        }

        currentSelectedObject = selectedModelName?.Trim() ?? "";

        var packet = new OutboundPacket
        {
            type = "context_update",
            selected_object = currentSelectedObject,
            model_context = modelContext,
            extra_context = BuildEffectiveExtraContext()
        };

        string contextJson = JsonUtility.ToJson(packet);
        await websocket.SendText(contextJson);
        ContextUpdated?.Invoke(currentSelectedObject);
        Debug.Log($"[AnatomyLiveClient] Sent context: {currentSelectedObject}");
    }

    public async void SendQuestion(string question)
    {
        string trimmedQuestion = question?.Trim();
        if (string.IsNullOrEmpty(trimmedQuestion))
        {
            return;
        }

        if (!IsConnected || websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("[AnatomyLiveClient] Cannot send question while disconnected.");
            SetConnectionStatus("Disconnected - question not sent");
            return;
        }

        var packet = new OutboundPacket
        {
            selected_object = currentSelectedObject,
            model_context = modelContext,
            extra_context = BuildEffectiveExtraContext(),
            question = trimmedQuestion
        };

        if (latestUserText != null)
        {
            latestUserText.text = trimmedQuestion;
        }

        SetConnectionStatus("Question sent");
        QuestionSent?.Invoke(trimmedQuestion);
        await websocket.SendText(JsonUtility.ToJson(packet));
    }

    private void HookQuestionInputFields()
    {
        if (!autoHookQuestionInputFields && (questionInputFields == null || questionInputFields.Length == 0))
        {
            return;
        }

        IEnumerable<TMP_InputField> discoveredFields = questionInputFields ?? Array.Empty<TMP_InputField>();
        if (autoHookQuestionInputFields)
        {
            discoveredFields = discoveredFields.Concat(
                FindObjectsOfType<TMP_InputField>(true)
                    .Where(field =>
                        field != null &&
                        (
                            field.gameObject.name.IndexOf("input", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            field.gameObject.name.IndexOf("textfield", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            field.gameObject.tag == "QDSUITextInputField"
                        )));
        }

        questionInputFields = discoveredFields
            .Where(field => field != null)
            .Distinct()
            .ToArray();

        foreach (TMP_InputField inputField in questionInputFields)
        {
            inputField.onSubmit.RemoveListener(SendQuestion);
            inputField.onSubmit.AddListener(SendQuestion);
        }
    }

    private void ConfigurePersonalityDropdown()
    {
        if (personalityDropdown == null)
        {
            return;
        }

        personalityDropdown.onValueChanged.RemoveListener(SetTeacherPersonality);

        if (autoConfigurePersonalityDropdown)
        {
            personalityDropdown.ClearOptions();
            personalityDropdown.AddOptions(
                Enum.GetValues(typeof(TeacherPersonalityPreset))
                    .Cast<TeacherPersonalityPreset>()
                    .Select(GetPersonalityDisplayName)
                    .ToList()
            );
        }

        SyncPersonalityDropdownValue();
        personalityDropdown.onValueChanged.AddListener(SetTeacherPersonality);
    }

    private void SyncPersonalityDropdownValue()
    {
        if (personalityDropdown == null)
        {
            return;
        }

        int personalityIndex = (int)teacherPersonality;
        if (personalityDropdown.options.Count <= personalityIndex)
        {
            return;
        }

        personalityDropdown.SetValueWithoutNotify(personalityIndex);
    }

    private static string GetPersonalityDisplayName(TeacherPersonalityPreset preset)
    {
        switch (preset)
        {
            case TeacherPersonalityPreset.QuizTeacher:
                return "Quiz Instructor";
            case TeacherPersonalityPreset.ExamCoach:
                return "Exam Coach";
            case TeacherPersonalityPreset.BeginnerGuide:
                return "Beginner Guide";
            case TeacherPersonalityPreset.SocraticTutor:
                return "Socratic Tutor";
            case TeacherPersonalityPreset.FriendlyTeacher:
            default:
                return "Friendly Instructor";
        }
    }

    private static string FormatEnumLabel(string rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return string.Empty;
        }

        List<char> characters = new List<char>(rawValue.Length + 4);
        for (int i = 0; i < rawValue.Length; i++)
        {
            char currentChar = rawValue[i];
            if (i > 0 && char.IsUpper(currentChar) && !char.IsWhiteSpace(rawValue[i - 1]))
            {
                characters.Add(' ');
            }

            characters.Add(currentChar);
        }

        return new string(characters.ToArray());
    }

    private static string NormalizePresetName(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return new string(rawValue.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private void UnhookQuestionInputFields()
    {
        if (questionInputFields == null)
        {
            return;
        }

        foreach (TMP_InputField inputField in questionInputFields)
        {
            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(SendQuestion);
            }
        }
    }

    private void UnhookPersonalityDropdown()
    {
        if (personalityDropdown != null)
        {
            personalityDropdown.onValueChanged.RemoveListener(SetTeacherPersonality);
        }
    }

    private System.Collections.IEnumerator ReconnectAfterDelay()
    {
        if (reconnectCoroutineRunning)
        {
            yield break;
        }

        reconnectCoroutineRunning = true;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, reconnectDelaySeconds));
        reconnectCoroutineRunning = false;

        if (!isQuitting && !IsConnected)
        {
            _ = ConnectToBackend();
        }
    }

    private void SetConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }
    }

    private async Task CloseWebSocket()
    {
        if (websocket != null)
        {
            try
            {
                suppressReconnect = true;
                if (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting)
                {
                    await websocket.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AnatomyLiveClient] Error while closing socket: {ex.Message}");
            }
            finally
            {
                suppressReconnect = false;
            }
        }
    }

    private async void OnDisable()
    {
        StopMicrophone();
        UnhookPersonalityDropdown();
        await CloseWebSocket();
    }

    private async void OnApplicationQuit()
    {
        isQuitting = true;
        UnhookQuestionInputFields();
        UnhookPersonalityDropdown();
        StopMicrophone();
        await CloseWebSocket();
    }
}
