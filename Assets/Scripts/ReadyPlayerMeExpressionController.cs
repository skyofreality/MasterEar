using System;
using UnityEngine;

public class ReadyPlayerMeExpressionController : MonoBehaviour
{
    private enum ExpressionPhase
    {
        Neutral,
        Attentive,
        Thinking,
        Speaking,
        PostAnswer,
        Disconnected
    }

    [Serializable]
    private struct ExpressionWeights
    {
        public float smile;
        public float browDown;
        public float browInnerUp;
        public float browOuterUp;
        public float eyeSquint;
        public float eyeWide;
        public float frown;
        public float cheekSquint;
    }

    [Header("Primary References")]
    public AnatomyLiveClient liveClient;
    public ReadyPlayerMeLipSyncBinder lipSyncBinder;
    public GameObject avatarRoot;
    public AudioSource targetAudioSource;
    public SkinnedMeshRenderer faceRenderer;

    [Header("Auto Setup")]
    public bool autoConfigureOnStart = true;
    public bool searchInactiveChildren = true;
    public bool logResolutionDetails = true;

    [Header("Blending")]
    [Range(1f, 30f)]
    public float blendSpeed = 10f;
    [Range(0.1f, 3f)]
    public float postAnswerHoldSeconds = 0.8f;
    [Range(0.1f, 2f)]
    public float attentiveHoldSeconds = 0.5f;
    [Range(0.5f, 4f)]
    public float expressionIntensity = 2f;
    [Range(0f, 6f)]
    public float idleMotionAmount = 2f;

    private int smileIndex = -1;
    private int smileLeftIndex = -1;
    private int smileRightIndex = -1;
    private int browDownLeftIndex = -1;
    private int browDownRightIndex = -1;
    private int browInnerUpIndex = -1;
    private int browOuterUpLeftIndex = -1;
    private int browOuterUpRightIndex = -1;
    private int eyeSquintLeftIndex = -1;
    private int eyeSquintRightIndex = -1;
    private int eyeWideLeftIndex = -1;
    private int eyeWideRightIndex = -1;
    private int mouthFrownLeftIndex = -1;
    private int mouthFrownRightIndex = -1;
    private int cheekSquintLeftIndex = -1;
    private int cheekSquintRightIndex = -1;

    private bool waitingForAnswer;
    private bool subscribed;
    private float attentiveUntilTime;
    private float postAnswerUntilTime;
    private bool wasSpeakingLastFrame;

    private void Start()
    {
        if (autoConfigureOnStart)
        {
            AutoConfigureExpressions();
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

    [ContextMenu("Auto Configure Expressions")]
    public void AutoConfigureExpressions()
    {
        ResolveReferences();
        ResolveBlendshapeIndices();
    }

    private void Update()
    {
        if (liveClient == null || faceRenderer == null)
        {
            return;
        }

        bool speaking = targetAudioSource != null && targetAudioSource.isPlaying;
        if (speaking && !wasSpeakingLastFrame)
        {
            waitingForAnswer = false;
        }

        wasSpeakingLastFrame = speaking;

        ExpressionPhase phase = DeterminePhase(speaking);
        ExpressionWeights targetWeights = BuildWeights(phase, liveClient.teacherPersonality);
        ApplyWeights(targetWeights);
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

        liveClient.ContextUpdated += HandleContextUpdated;
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

        liveClient.ContextUpdated -= HandleContextUpdated;
        liveClient.QuestionSent -= HandleQuestionSent;
        liveClient.TeacherTurnCompleted -= HandleTeacherTurnCompleted;
        subscribed = false;
    }

    private void HandleContextUpdated(string _)
    {
        attentiveUntilTime = Time.unscaledTime + Mathf.Max(0.1f, attentiveHoldSeconds);
    }

    private void HandleQuestionSent(string _)
    {
        waitingForAnswer = true;
        postAnswerUntilTime = 0f;
        attentiveUntilTime = Time.unscaledTime + Mathf.Max(0.1f, attentiveHoldSeconds);
    }

    private void HandleTeacherTurnCompleted()
    {
        waitingForAnswer = false;
        postAnswerUntilTime = Time.unscaledTime + Mathf.Max(0.1f, postAnswerHoldSeconds);
    }

    private ExpressionPhase DeterminePhase(bool speaking)
    {
        if (!liveClient.IsConnected)
        {
            return ExpressionPhase.Disconnected;
        }

        if (speaking)
        {
            return ExpressionPhase.Speaking;
        }

        if (Time.unscaledTime < attentiveUntilTime)
        {
            return ExpressionPhase.Attentive;
        }

        if (waitingForAnswer)
        {
            return ExpressionPhase.Thinking;
        }

        if (Time.unscaledTime < postAnswerUntilTime)
        {
            return ExpressionPhase.PostAnswer;
        }

        return ExpressionPhase.Neutral;
    }

    private ExpressionWeights BuildWeights(
        ExpressionPhase phase,
        AnatomyLiveClient.TeacherPersonalityPreset personality)
    {
        ExpressionWeights weights = default;

        switch (phase)
        {
            case ExpressionPhase.Disconnected:
                weights.browDown = 10f;
                weights.eyeSquint = 5f;
                weights.frown = 6f;
                weights.cheekSquint = 2f;
                break;

            case ExpressionPhase.Attentive:
                switch (personality)
                {
                    case AnatomyLiveClient.TeacherPersonalityPreset.FriendlyTeacher:
                        weights.smile = 8f;
                        weights.browOuterUp = 8f;
                        weights.eyeWide = 6f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.QuizTeacher:
                        weights.smile = 6f;
                        weights.browOuterUp = 10f;
                        weights.eyeWide = 8f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.ExamCoach:
                        weights.browDown = 4f;
                        weights.browOuterUp = 4f;
                        weights.eyeSquint = 4f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.BeginnerGuide:
                        weights.smile = 10f;
                        weights.browOuterUp = 10f;
                        weights.eyeWide = 10f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.SocraticTutor:
                        weights.browInnerUp = 10f;
                        weights.eyeSquint = 5f;
                        weights.eyeWide = 3f;
                        break;
                }
                break;

            case ExpressionPhase.Thinking:
                switch (personality)
                {
                    case AnatomyLiveClient.TeacherPersonalityPreset.FriendlyTeacher:
                        weights.smile = 4f;
                        weights.browInnerUp = 18f;
                        weights.eyeSquint = 8f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.QuizTeacher:
                        weights.smile = 2f;
                        weights.browInnerUp = 20f;
                        weights.browOuterUp = 6f;
                        weights.eyeSquint = 8f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.ExamCoach:
                        weights.browDown = 6f;
                        weights.browInnerUp = 14f;
                        weights.eyeSquint = 12f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.BeginnerGuide:
                        weights.smile = 6f;
                        weights.browInnerUp = 20f;
                        weights.eyeWide = 8f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.SocraticTutor:
                        weights.browInnerUp = 24f;
                        weights.eyeSquint = 10f;
                        weights.browOuterUp = 4f;
                        break;
                }
                break;

            case ExpressionPhase.PostAnswer:
                switch (personality)
                {
                    case AnatomyLiveClient.TeacherPersonalityPreset.FriendlyTeacher:
                        weights.smile = 18f;
                        weights.browOuterUp = 8f;
                        weights.cheekSquint = 5f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.QuizTeacher:
                        weights.smile = 14f;
                        weights.browOuterUp = 10f;
                        weights.eyeWide = 6f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.ExamCoach:
                        weights.smile = 4f;
                        weights.browDown = 2f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.BeginnerGuide:
                        weights.smile = 20f;
                        weights.browOuterUp = 10f;
                        weights.cheekSquint = 6f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.SocraticTutor:
                        weights.smile = 8f;
                        weights.browInnerUp = 8f;
                        break;
                }
                break;

            case ExpressionPhase.Speaking:
                switch (personality)
                {
                    case AnatomyLiveClient.TeacherPersonalityPreset.FriendlyTeacher:
                        weights.smile = 22f;
                        weights.browInnerUp = 8f;
                        weights.eyeWide = 8f;
                        weights.cheekSquint = 4f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.QuizTeacher:
                        weights.smile = 16f;
                        weights.browOuterUp = 12f;
                        weights.eyeWide = 10f;
                        weights.cheekSquint = 2f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.ExamCoach:
                        weights.browDown = 12f;
                        weights.eyeSquint = 8f;
                        weights.frown = 4f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.BeginnerGuide:
                        weights.smile = 26f;
                        weights.browInnerUp = 10f;
                        weights.eyeWide = 12f;
                        weights.cheekSquint = 6f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.SocraticTutor:
                        weights.smile = 8f;
                        weights.browInnerUp = 18f;
                        weights.eyeSquint = 10f;
                        weights.browOuterUp = 4f;
                        break;
                }
                break;

            case ExpressionPhase.Neutral:
                switch (personality)
                {
                    case AnatomyLiveClient.TeacherPersonalityPreset.FriendlyTeacher:
                        weights.smile = 10f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.QuizTeacher:
                        weights.smile = 8f;
                        weights.browOuterUp = 6f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.ExamCoach:
                        weights.browDown = 5f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.BeginnerGuide:
                        weights.smile = 12f;
                        weights.eyeWide = 8f;
                        break;

                    case AnatomyLiveClient.TeacherPersonalityPreset.SocraticTutor:
                        weights.browInnerUp = 8f;
                        break;
                }
                break;
        }

        return weights;
    }

    private void ApplyWeights(ExpressionWeights target)
    {
        float step = Mathf.Max(1f, blendSpeed) * 100f * Time.unscaledDeltaTime;
        float scaledSmile = target.smile * expressionIntensity;
        float scaledBrowDown = target.browDown * expressionIntensity;
        float scaledBrowInnerUp = target.browInnerUp * expressionIntensity;
        float scaledBrowOuterUp = target.browOuterUp * expressionIntensity;
        float scaledEyeSquint = target.eyeSquint * expressionIntensity;
        float scaledEyeWide = target.eyeWide * expressionIntensity;
        float scaledFrown = target.frown * expressionIntensity;
        float scaledCheekSquint = target.cheekSquint * expressionIntensity;
        float idleOffset = Mathf.Sin(Time.unscaledTime * 1.7f) * idleMotionAmount;
        float idleWideOffset = Mathf.Cos(Time.unscaledTime * 1.1f) * idleMotionAmount * 0.5f;

        if (!waitingForAnswer && Time.unscaledTime >= postAnswerUntilTime && !wasSpeakingLastFrame)
        {
            scaledBrowInnerUp = Mathf.Max(0f, scaledBrowInnerUp + idleOffset);
            scaledEyeWide = Mathf.Max(0f, scaledEyeWide + idleWideOffset);
        }

        SetBlendWeight(smileIndex, scaledSmile, step);
        SetBlendWeight(smileLeftIndex, scaledSmile, step);
        SetBlendWeight(smileRightIndex, scaledSmile, step);
        SetBlendWeight(browDownLeftIndex, scaledBrowDown, step);
        SetBlendWeight(browDownRightIndex, scaledBrowDown, step);
        SetBlendWeight(browInnerUpIndex, scaledBrowInnerUp, step);
        SetBlendWeight(browOuterUpLeftIndex, scaledBrowOuterUp, step);
        SetBlendWeight(browOuterUpRightIndex, scaledBrowOuterUp, step);
        SetBlendWeight(eyeSquintLeftIndex, scaledEyeSquint, step);
        SetBlendWeight(eyeSquintRightIndex, scaledEyeSquint, step);
        SetBlendWeight(eyeWideLeftIndex, scaledEyeWide, step);
        SetBlendWeight(eyeWideRightIndex, scaledEyeWide, step);
        SetBlendWeight(mouthFrownLeftIndex, scaledFrown, step);
        SetBlendWeight(mouthFrownRightIndex, scaledFrown, step);
        SetBlendWeight(cheekSquintLeftIndex, scaledCheekSquint, step);
        SetBlendWeight(cheekSquintRightIndex, scaledCheekSquint, step);
    }

    private void SetBlendWeight(int index, float targetWeight, float step)
    {
        if (faceRenderer == null || index < 0)
        {
            return;
        }

        float currentWeight = faceRenderer.GetBlendShapeWeight(index);
        float nextWeight = Mathf.MoveTowards(currentWeight, targetWeight, step);
        faceRenderer.SetBlendShapeWeight(index, nextWeight);
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

        if (faceRenderer == null && lipSyncBinder != null && lipSyncBinder.faceRenderer != null)
        {
            faceRenderer = lipSyncBinder.faceRenderer;
        }

        if (avatarRoot == null && lipSyncBinder != null && lipSyncBinder.avatarRoot != null)
        {
            avatarRoot = lipSyncBinder.avatarRoot;
        }

        if (faceRenderer == null && avatarRoot != null)
        {
            faceRenderer = FindBestFaceRenderer(avatarRoot);
        }
    }

    private void ResolveBlendshapeIndices()
    {
        if (faceRenderer == null || faceRenderer.sharedMesh == null)
        {
            Debug.LogWarning("[ReadyPlayerMeExpressionController] No face renderer with blendshapes found.");
            return;
        }

        smileIndex = FindBlendShapeIndex("mouthSmile");
        smileLeftIndex = FindBlendShapeIndex("mouthSmileLeft");
        smileRightIndex = FindBlendShapeIndex("mouthSmileRight");
        browDownLeftIndex = FindBlendShapeIndex("browDownLeft");
        browDownRightIndex = FindBlendShapeIndex("browDownRight");
        browInnerUpIndex = FindBlendShapeIndex("browInnerUp");
        browOuterUpLeftIndex = FindBlendShapeIndex("browOuterUpLeft");
        browOuterUpRightIndex = FindBlendShapeIndex("browOuterUpRight");
        eyeSquintLeftIndex = FindBlendShapeIndex("eyeSquintLeft");
        eyeSquintRightIndex = FindBlendShapeIndex("eyeSquintRight");
        eyeWideLeftIndex = FindBlendShapeIndex("eyeWideLeft");
        eyeWideRightIndex = FindBlendShapeIndex("eyeWideRight");
        mouthFrownLeftIndex = FindBlendShapeIndex("mouthFrownLeft");
        mouthFrownRightIndex = FindBlendShapeIndex("mouthFrownRight");
        cheekSquintLeftIndex = FindBlendShapeIndex("cheekSquintLeft");
        cheekSquintRightIndex = FindBlendShapeIndex("cheekSquintRight");

        if (logResolutionDetails)
        {
            Debug.Log(
                "[ReadyPlayerMeExpressionController] Using renderer '" + faceRenderer.name +
                "' with expression indices smile=" + smileIndex +
                ", smileLeft=" + smileLeftIndex +
                ", smileRight=" + smileRightIndex +
                ", browInnerUp=" + browInnerUpIndex +
                ", eyeWideLeft=" + eyeWideLeftIndex +
                ", mouthFrownLeft=" + mouthFrownLeftIndex + ".");
        }
    }

    private int FindBlendShapeIndex(string blendShapeName)
    {
        if (faceRenderer == null || faceRenderer.sharedMesh == null)
        {
            return -1;
        }

        return faceRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
    }

    private SkinnedMeshRenderer FindBestFaceRenderer(GameObject root)
    {
        SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(searchInactiveChildren);
        SkinnedMeshRenderer best = null;
        int bestScore = int.MinValue;

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer.sharedMesh == null)
            {
                continue;
            }

            int score = 0;
            score += renderer.sharedMesh.GetBlendShapeIndex("mouthSmile") >= 0 ? 10 : 0;
            score += renderer.sharedMesh.GetBlendShapeIndex("mouthSmileLeft") >= 0 ? 6 : 0;
            score += renderer.sharedMesh.GetBlendShapeIndex("mouthSmileRight") >= 0 ? 6 : 0;
            score += renderer.sharedMesh.GetBlendShapeIndex("browInnerUp") >= 0 ? 10 : 0;
            score += renderer.sharedMesh.GetBlendShapeIndex("eyeWideLeft") >= 0 ? 10 : 0;
            score += renderer.sharedMesh.GetBlendShapeIndex("mouthFrownLeft") >= 0 ? 4 : 0;
            score += renderer.sharedMesh.GetBlendShapeIndex("cheekSquintLeft") >= 0 ? 4 : 0;

            string rendererName = renderer.name.ToLowerInvariant();
            if (rendererName.Contains("head"))
            {
                score += 8;
            }
            else if (rendererName.Contains("teeth"))
            {
                score += 4;
            }

            if (score > bestScore)
            {
                best = renderer;
                bestScore = score;
            }
        }

        if (logResolutionDetails && best != null)
        {
            Debug.Log("[ReadyPlayerMeExpressionController] Auto-selected face renderer '" + best.name + "'.");
        }

        return best;
    }
}
