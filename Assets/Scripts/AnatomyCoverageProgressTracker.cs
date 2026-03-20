using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnatomyCoverageProgressTracker : MonoBehaviour
{
    [Header("References")]
    public AnatomyLiveClient liveClient;
    public Slider progressBar;
    public TMP_Text percentageText;
    public TMP_Text progressLabelText;
    public TMP_Text progressDetailsText;

    [Header("Coverage Settings")]
    [Min(1)]
    public int expectedPartCount = 8;
    [Min(1)]
    public int targetQuestionCount = 8;
    [Range(0f, 1f)]
    public float partCoverageWeight = 0.7f;
    [Range(0f, 1f)]
    public float questionDepthWeight = 0.3f;
    public string[] trackedParts = Array.Empty<string>();

    [Header("Debug")]
    public bool logProgressUpdates = false;

    private readonly HashSet<string> coveredPartKeys = new HashSet<string>();
    private readonly Dictionary<string, int> questionCountsByPart = new Dictionary<string, int>();
    private readonly Dictionary<string, string> displayNameByPartKey = new Dictionary<string, string>();
    private string currentSelectedPartKey = string.Empty;
    private int answeredQuestionCount;

    public float Progress01 { get; private set; }
    public int AnsweredQuestionCount => answeredQuestionCount;
    public IReadOnlyCollection<string> CoveredPartKeys => coveredPartKeys;

    private void Awake()
    {
        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>();
        }

        UpdateProgressUI();
    }

    private void OnEnable()
    {
        Subscribe();
        if (liveClient != null && !string.IsNullOrWhiteSpace(liveClient.CurrentSelectedObject))
        {
            HandleContextUpdated(liveClient.CurrentSelectedObject);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (liveClient == null)
        {
            return;
        }

        liveClient.ContextUpdated -= HandleContextUpdated;
        liveClient.TeacherTurnCompleted -= HandleTeacherTurnCompleted;

        liveClient.ContextUpdated += HandleContextUpdated;
        liveClient.TeacherTurnCompleted += HandleTeacherTurnCompleted;
    }

    private void Unsubscribe()
    {
        if (liveClient == null)
        {
            return;
        }

        liveClient.ContextUpdated -= HandleContextUpdated;
        liveClient.TeacherTurnCompleted -= HandleTeacherTurnCompleted;
    }

    private void HandleContextUpdated(string selectedPartName)
    {
        currentSelectedPartKey = NormalizePartKey(selectedPartName);
        if (string.IsNullOrWhiteSpace(currentSelectedPartKey))
        {
            return;
        }

        displayNameByPartKey[currentSelectedPartKey] = selectedPartName?.Trim() ?? string.Empty;
        UpdateProgressUI();
    }

    private void HandleTeacherTurnCompleted()
    {
        answeredQuestionCount++;

        if (!string.IsNullOrWhiteSpace(currentSelectedPartKey))
        {
            coveredPartKeys.Add(currentSelectedPartKey);

            if (questionCountsByPart.TryGetValue(currentSelectedPartKey, out int existingCount))
            {
                questionCountsByPart[currentSelectedPartKey] = existingCount + 1;
            }
            else
            {
                questionCountsByPart[currentSelectedPartKey] = 1;
            }
        }

        UpdateProgressUI();
    }

    public void ResetProgress()
    {
        coveredPartKeys.Clear();
        questionCountsByPart.Clear();
        displayNameByPartKey.Clear();
        currentSelectedPartKey = string.Empty;
        answeredQuestionCount = 0;
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        int trackedPartTarget = GetTrackedPartTargetCount();
        int coveredCount = GetCoveredTrackedPartCount();

        float normalizedPartWeight = Mathf.Max(0f, partCoverageWeight);
        float normalizedQuestionWeight = Mathf.Max(0f, questionDepthWeight);
        float totalWeight = normalizedPartWeight + normalizedQuestionWeight;
        if (totalWeight <= 0f)
        {
            normalizedPartWeight = 0.7f;
            normalizedQuestionWeight = 0.3f;
            totalWeight = 1f;
        }

        normalizedPartWeight /= totalWeight;
        normalizedQuestionWeight /= totalWeight;

        float partCoverage = trackedPartTarget > 0 ? coveredCount / (float)trackedPartTarget : 0f;
        float questionCoverage = Mathf.Clamp01(answeredQuestionCount / (float)Mathf.Max(1, targetQuestionCount));
        Progress01 = Mathf.Clamp01((partCoverage * normalizedPartWeight) + (questionCoverage * normalizedQuestionWeight));

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = Progress01;
        }

        if (percentageText != null)
        {
            percentageText.text = $"{Mathf.RoundToInt(Progress01 * 100f)}%";
        }

        if (progressLabelText != null)
        {
            progressLabelText.text = GetProgressLabel(Progress01);
        }

        if (progressDetailsText != null)
        {
            progressDetailsText.text = $"Questions: {answeredQuestionCount} | Parts: {coveredCount}/{trackedPartTarget}";
        }

        if (logProgressUpdates)
        {
            Debug.Log($"[AnatomyCoverageProgressTracker] Progress {Mathf.RoundToInt(Progress01 * 100f)}% | Questions {answeredQuestionCount} | Parts {coveredCount}/{trackedPartTarget}");
        }
    }

    private int GetTrackedPartTargetCount()
    {
        if (trackedParts != null)
        {
            int configuredCount = trackedParts
                .Select(NormalizePartKey)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct()
                .Count();

            if (configuredCount > 0)
            {
                return configuredCount;
            }
        }

        return Mathf.Max(1, expectedPartCount);
    }

    private int GetCoveredTrackedPartCount()
    {
        HashSet<string> trackedPartKeys = GetTrackedPartKeySet();
        if (trackedPartKeys.Count == 0)
        {
            return Mathf.Min(coveredPartKeys.Count, Mathf.Max(1, expectedPartCount));
        }

        return coveredPartKeys.Count(trackedPartKeys.Contains);
    }

    private HashSet<string> GetTrackedPartKeySet()
    {
        HashSet<string> trackedPartKeys = new HashSet<string>();
        if (trackedParts == null)
        {
            return trackedPartKeys;
        }

        foreach (string trackedPart in trackedParts)
        {
            string normalizedKey = NormalizePartKey(trackedPart);
            if (!string.IsNullOrWhiteSpace(normalizedKey))
            {
                trackedPartKeys.Add(normalizedKey);
            }
        }

        return trackedPartKeys;
    }

    private static string GetProgressLabel(float progress01)
    {
        if (progress01 >= 0.99f)
        {
            return "Inner Ear Complete";
        }

        if (progress01 >= 0.75f)
        {
            return "Strong Coverage";
        }

        if (progress01 >= 0.5f)
        {
            return "Good Progress";
        }

        if (progress01 >= 0.25f)
        {
            return "Exploring";
        }

        return "Getting Started";
    }

    private static string NormalizePartKey(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return new string(rawValue.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}
