using UnityEngine;

public class ReadyPlayerMeAnimationController : MonoBehaviour
{
    [Header("Primary References")]
    public AnatomyLiveClient liveClient;
    public ReadyPlayerMeLipSyncBinder lipSyncBinder;
    public GameObject avatarRoot;
    public Animator avatarAnimator;
    public AudioSource targetAudioSource;

    [Header("Animator Parameters")]
    public string speakingBoolParameter = "IsSpeaking";
    public string nodTriggerParameter = "DoNod";

    [Header("Behavior")]
    public bool autoConfigureOnStart = true;
    public bool nodAfterCompletedAnswer = true;
    [Range(0f, 1.5f)]
    public float nodDelayAfterSpeechStops = 0.1f;
    public bool logStateChanges = true;

    private bool subscribed;
    private bool pendingNod;
    private float pendingNodTime;
    private bool lastSpeakingValue;
    private int speakingBoolHash;
    private int nodTriggerHash;

    private void Start()
    {
        CacheHashes();

        if (autoConfigureOnStart)
        {
            ResolveReferences();
        }

        SubscribeToLiveClient();
    }

    private void OnEnable()
    {
        SubscribeToLiveClient();
    }

    private void OnDisable()
    {
        UnsubscribeFromLiveClient();
    }

    [ContextMenu("Auto Configure Animator References")]
    public void AutoConfigureReferences()
    {
        ResolveReferences();
        CacheHashes();
    }

    private void Update()
    {
        if (avatarAnimator == null)
        {
            return;
        }

        bool isSpeaking = liveClient != null
            ? liveClient.IsTeacherAudioActive
            : (targetAudioSource != null && targetAudioSource.isPlaying);
        if (isSpeaking != lastSpeakingValue)
        {
            avatarAnimator.SetBool(speakingBoolHash, isSpeaking);

            if (logStateChanges)
            {
                Debug.Log("[ReadyPlayerMeAnimationController] IsSpeaking -> " + isSpeaking);
            }

            lastSpeakingValue = isSpeaking;
        }

        if (pendingNod && !isSpeaking && Time.unscaledTime >= pendingNodTime)
        {
            pendingNod = false;
            avatarAnimator.SetTrigger(nodTriggerHash);

            if (logStateChanges)
            {
                Debug.Log("[ReadyPlayerMeAnimationController] Triggered nod animation.");
            }
        }
    }

    private void HandleQuestionSent(string _)
    {
        pendingNod = false;
    }

    private void HandleTeacherTurnCompleted()
    {
        if (!nodAfterCompletedAnswer || avatarAnimator == null)
        {
            return;
        }

        pendingNod = true;
        pendingNodTime = Time.unscaledTime + Mathf.Max(0f, nodDelayAfterSpeechStops);

        if (logStateChanges)
        {
            Debug.Log("[ReadyPlayerMeAnimationController] Queued nod after completed answer.");
        }
    }

    private void SubscribeToLiveClient()
    {
        if (subscribed)
        {
            return;
        }

        if (liveClient == null)
        {
            ResolveReferences();
        }

        if (liveClient == null)
        {
            return;
        }

        liveClient.QuestionSent += HandleQuestionSent;
        liveClient.TeacherTurnCompleted += HandleTeacherTurnCompleted;
        subscribed = true;
    }

    private void UnsubscribeFromLiveClient()
    {
        if (!subscribed || liveClient == null)
        {
            return;
        }

        liveClient.QuestionSent -= HandleQuestionSent;
        liveClient.TeacherTurnCompleted -= HandleTeacherTurnCompleted;
        subscribed = false;
    }

    private void ResolveReferences()
    {
        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>();
        }

        if (lipSyncBinder == null)
        {
            lipSyncBinder = FindObjectOfType<ReadyPlayerMeLipSyncBinder>();
        }

        if (targetAudioSource == null && liveClient != null)
        {
            targetAudioSource = liveClient.teacherAudioSource;
        }

        if (avatarRoot == null && lipSyncBinder != null && lipSyncBinder.avatarRoot != null)
        {
            avatarRoot = lipSyncBinder.avatarRoot;
        }

        if (avatarAnimator == null)
        {
            if (avatarRoot != null)
            {
                avatarAnimator = avatarRoot.GetComponent<Animator>();
            }

            if (avatarAnimator == null)
            {
                avatarAnimator = FindObjectOfType<Animator>();
            }
        }

        if (logStateChanges && avatarAnimator != null)
        {
            Debug.Log("[ReadyPlayerMeAnimationController] Using animator on '" + avatarAnimator.gameObject.name + "'.");
        }
    }

    private void CacheHashes()
    {
        speakingBoolHash = Animator.StringToHash(speakingBoolParameter);
        nodTriggerHash = Animator.StringToHash(nodTriggerParameter);
    }
}
