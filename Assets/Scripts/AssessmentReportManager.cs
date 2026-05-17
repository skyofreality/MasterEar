using System;
using System.Collections.Generic;
using System.Linq;
using NudleNexus.Classroom;
using TMPro;
using UnityEngine;

public class AssessmentReportManager : MonoBehaviour
{
    [Header("Demo Control")]
    public bool enabledForDemo = true;
    public bool showReportOnCompletion = true;
    public bool hideReportOnStart = true;
    public bool generateIfAlreadyComplete = true;

    [Header("Scene References")]
    public AnatomyPartChecklistTracker checklistTracker;
    public AnatomyLiveClient liveClient;
    public GameObject reportPanel;

    [Header("Report Text")]
    public TMP_Text titleText;
    public TMP_Text overallScoreText;
    public TMP_Text verbalScoreText;
    public TMP_Text spatialScoreText;
    public TMP_Text proceduralScoreText;
    public TMP_Text summaryText;
    public TMP_Text recommendationText;
    public TMP_Text gradeText;
    public TMP_Text generatedAtText;

    [Header("Scoring Targets")]
    [Min(1)]
    public int targetQuestionCount = 4;
    [Min(1)]
    public int targetPartCount = 8;
    [Range(0f, 1f)]
    public float verbalWeight = 0.3f;
    [Range(0f, 1f)]
    public float spatialWeight = 0.3f;
    [Range(0f, 1f)]
    public float proceduralWeight = 0.4f;

    [Header("Placeholder Baseline")]
    public bool usePlaceholderScoreFloor = true;
    [Range(0f, 1f)]
    public float verbalScoreFloor = 0.78f;
    [Range(0f, 1f)]
    public float spatialScoreFloor = 0.82f;
    [Range(0f, 1f)]
    public float proceduralScoreFloor = 0.9f;

    [Header("Displayed Demo Evidence")]
    public bool usePlaceholderEvidenceCounts = true;
    [Min(0)]
    public int placeholderQuestionCount = 5;
    [Min(0)]
    public int placeholderExploredPartCount = 8;

    [Header("Report Copy")]
    public string lessonTitle = "Inner Ear Assessment Report";
    public bool useRichTextFormatting = true;
    public string headingColorHex = "#A3FFD8";
    public string scoreColorHex = "#8FB3FF";
    public string recommendationHeadingColorHex = "#FFD28F";
    public string bodyColorHex = "#FFFFFF";
    [TextArea(2, 4)]
    public string verbalEvidenceTemplate = "Asked {questions} questions. Maintained relevant anatomy context.";
    [TextArea(2, 4)]
    public string spatialEvidenceTemplate = "Explored {parts} anatomy parts. Manipulated cochlear and ossicle structures.";
    [TextArea(2, 4)]
    public string proceduralEvidenceTemplate = "Completed {completed}/{total} required objectives.";
    [TextArea(2, 4)]
    public string semanticSummary = "Learner maintained topic context and demonstrated relevant verbal engagement with the anatomy teacher.";
    [TextArea(2, 4)]
    public string spatialSummary = "Learner explored the required inner-ear structures and demonstrated spatial recognition through direct manipulation.";
    [TextArea(2, 4)]
    public string proceduralSummary = "Learner completed the configured lesson objectives for the inner-ear module.";
    [TextArea(2, 4)]
    public string recommendation = "Recommendation: review the distinction between cochlear and vestibular nerve functions to strengthen conceptual clarity.";

    [Header("Debug")]
    public bool logReportGeneration = false;

    private readonly HashSet<string> interactedPartKeys = new HashSet<string>();
    private int questionCount;
    private int teacherTurnCount;

    private void Awake()
    {
        ResolveReferences();

        if (hideReportOnStart && reportPanel != null)
        {
            reportPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();

        if (generateIfAlreadyComplete &&
            enabledForDemo &&
            checklistTracker != null &&
            checklistTracker.IsComplete)
        {
            GenerateReport();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (checklistTracker == null)
        {
            checklistTracker = FindObjectOfType<AnatomyPartChecklistTracker>(true);
        }

        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>(true);
        }
    }

    private void Subscribe()
    {
        if (checklistTracker != null)
        {
            checklistTracker.ObjectivesCompleted -= HandleObjectivesCompleted;
            checklistTracker.ObjectivesCompleted += HandleObjectivesCompleted;
        }

        if (liveClient != null)
        {
            liveClient.QuestionSent -= HandleQuestionSent;
            liveClient.TeacherTurnCompleted -= HandleTeacherTurnCompleted;
            liveClient.QuestionSent += HandleQuestionSent;
            liveClient.TeacherTurnCompleted += HandleTeacherTurnCompleted;
        }

        PickablePart.OnPickUpAnyPart -= HandlePartPicked;
        PickablePart.OnPickUpAnyPart += HandlePartPicked;
    }

    private void Unsubscribe()
    {
        if (checklistTracker != null)
        {
            checklistTracker.ObjectivesCompleted -= HandleObjectivesCompleted;
        }

        if (liveClient != null)
        {
            liveClient.QuestionSent -= HandleQuestionSent;
            liveClient.TeacherTurnCompleted -= HandleTeacherTurnCompleted;
        }

        PickablePart.OnPickUpAnyPart -= HandlePartPicked;
    }

    private void HandlePartPicked(PickablePart part)
    {
        if (part == null)
        {
            return;
        }

        string partName = part.Data != null && !string.IsNullOrWhiteSpace(part.Data.name)
            ? part.Data.name
            : part.gameObject.name;

        string key = NormalizeKey(partName);
        if (!string.IsNullOrWhiteSpace(key))
        {
            interactedPartKeys.Add(key);
        }
    }

    private void HandleQuestionSent(string question)
    {
        if (!string.IsNullOrWhiteSpace(question))
        {
            questionCount++;
        }
    }

    private void HandleTeacherTurnCompleted()
    {
        teacherTurnCount++;
    }

    private void HandleObjectivesCompleted(AnatomyPartChecklistTracker tracker)
    {
        if (!enabledForDemo || !showReportOnCompletion)
        {
            return;
        }

        GenerateReport();
    }

    [ContextMenu("Generate Assessment Report")]
    public void GenerateReport()
    {
        if (!enabledForDemo)
        {
            return;
        }

        AssessmentScores scores = CalculateScores();
        ApplyReport(scores);

        if (reportPanel != null)
        {
            reportPanel.SetActive(true);
        }

        if (logReportGeneration)
        {
            Debug.Log($"[AssessmentReportManager] Generated report. Overall: {FormatPercent(scores.OverallScore)}");
        }
    }

    [ContextMenu("Reset Assessment Report")]
    public void ResetReport()
    {
        questionCount = 0;
        teacherTurnCount = 0;
        interactedPartKeys.Clear();

        if (reportPanel != null && hideReportOnStart)
        {
            reportPanel.SetActive(false);
        }
    }

    private AssessmentScores CalculateScores()
    {
        int completedParts = checklistTracker != null ? checklistTracker.CompletedCount : interactedPartKeys.Count;
        int totalParts = checklistTracker != null && checklistTracker.TotalCount > 0
            ? checklistTracker.TotalCount
            : Mathf.Max(1, targetPartCount);

        float verbal = Mathf.Clamp01((questionCount + teacherTurnCount) / (float)Mathf.Max(1, targetQuestionCount * 2));
        float spatial = Mathf.Clamp01(interactedPartKeys.Count / (float)Mathf.Max(1, totalParts));
        float procedural = checklistTracker != null
            ? Mathf.Clamp01(checklistTracker.Completion01)
            : Mathf.Clamp01(completedParts / (float)Mathf.Max(1, totalParts));

        if (usePlaceholderScoreFloor)
        {
            verbal = Mathf.Max(verbal, verbalScoreFloor);
            spatial = Mathf.Max(spatial, spatialScoreFloor);
            procedural = Mathf.Max(procedural, proceduralScoreFloor);
        }

        float normalizedVerbalWeight = Mathf.Max(0f, verbalWeight);
        float normalizedSpatialWeight = Mathf.Max(0f, spatialWeight);
        float normalizedProceduralWeight = Mathf.Max(0f, proceduralWeight);
        float totalWeight = normalizedVerbalWeight + normalizedSpatialWeight + normalizedProceduralWeight;

        if (totalWeight <= 0f)
        {
            normalizedVerbalWeight = 0.3f;
            normalizedSpatialWeight = 0.3f;
            normalizedProceduralWeight = 0.4f;
            totalWeight = 1f;
        }

        float overall =
            (verbal * normalizedVerbalWeight +
             spatial * normalizedSpatialWeight +
             procedural * normalizedProceduralWeight) / totalWeight;

        return new AssessmentScores
        {
            VerbalScore = Mathf.Clamp01(verbal),
            SpatialScore = Mathf.Clamp01(spatial),
            ProceduralScore = Mathf.Clamp01(procedural),
            OverallScore = Mathf.Clamp01(overall),
            CompletedParts = completedParts,
            TotalParts = totalParts,
            UniqueInteractedParts = interactedPartKeys.Count,
            QuestionCount = questionCount,
            TeacherTurnCount = teacherTurnCount
        };
    }

    private void ApplyReport(AssessmentScores scores)
    {
        int displayedQuestionCount = GetDisplayedQuestionCount(scores);
        int displayedPartCount = GetDisplayedPartCount(scores);

        SetText(titleText, lessonTitle);
        SetText(overallScoreText, "Overall Score: " + FormatPercent(scores.OverallScore));

        SetText(
            verbalScoreText,
            BuildMetricText(
                "Verbal / Semantic",
                scores.VerbalScore,
                FormatEvidenceText(verbalEvidenceTemplate, scores, displayedQuestionCount, displayedPartCount)));

        SetText(
            spatialScoreText,
            BuildMetricText(
                "Spatial / Physical",
                scores.SpatialScore,
                FormatEvidenceText(spatialEvidenceTemplate, scores, displayedQuestionCount, displayedPartCount)));

        SetText(
            proceduralScoreText,
            BuildMetricText(
                "Procedural",
                scores.ProceduralScore,
                FormatEvidenceText(proceduralEvidenceTemplate, scores, displayedQuestionCount, displayedPartCount)));

        SetText(
            summaryText,
            semanticSummary.Trim() + "\n\n" +
            spatialSummary.Trim() + "\n\n" +
            proceduralSummary.Trim());

        SetText(recommendationText, BuildRecommendationText());
        SetText(gradeText, GetGrade(scores.OverallScore));
        SetText(generatedAtText, "Generated: " + DateTime.Now.ToString("HH:mm"));
    }

    private string BuildMetricText(string heading, float score, string evidence)
    {
        string formattedScore = FormatPercent(score);
        if (!useRichTextFormatting)
        {
            return heading + "\nScore: " + formattedScore + "\n" + evidence;
        }

        return Colorize(heading, headingColorHex) +
               "\n" + Colorize("Score: " + formattedScore, scoreColorHex) +
               "\n" + Colorize(evidence, bodyColorHex);
    }

    private string BuildRecommendationText()
    {
        if (string.IsNullOrWhiteSpace(recommendation))
        {
            return string.Empty;
        }

        string trimmedRecommendation = recommendation.Trim();
        if (!useRichTextFormatting)
        {
            return trimmedRecommendation;
        }

        const string prefix = "Recommendation:";
        if (trimmedRecommendation.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            string body = trimmedRecommendation.Substring(prefix.Length).Trim();
            return Colorize(prefix, recommendationHeadingColorHex) + " " + Colorize(body, bodyColorHex);
        }

        return Colorize("Recommendation", recommendationHeadingColorHex) + "\n" + Colorize(trimmedRecommendation, bodyColorHex);
    }

    private int GetDisplayedQuestionCount(AssessmentScores scores)
    {
        if (!usePlaceholderEvidenceCounts)
        {
            return scores.QuestionCount;
        }

        return Mathf.Max(scores.QuestionCount, placeholderQuestionCount);
    }

    private int GetDisplayedPartCount(AssessmentScores scores)
    {
        if (!usePlaceholderEvidenceCounts)
        {
            return scores.UniqueInteractedParts;
        }

        return Mathf.Min(
            Mathf.Max(scores.UniqueInteractedParts, placeholderExploredPartCount),
            Mathf.Max(1, scores.TotalParts));
    }

    private static string FormatEvidenceText(
        string template,
        AssessmentScores scores,
        int displayedQuestionCount,
        int displayedPartCount)
    {
        string resolvedTemplate = string.IsNullOrWhiteSpace(template)
            ? string.Empty
            : template.Trim();

        return resolvedTemplate
            .Replace("{questions}", displayedQuestionCount.ToString())
            .Replace("{parts}", displayedPartCount.ToString())
            .Replace("{completed}", scores.CompletedParts.ToString())
            .Replace("{total}", scores.TotalParts.ToString())
            .Replace("{teacherTurns}", scores.TeacherTurnCount.ToString());
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.richText = true;
            text.text = value;
        }
    }

    private static string Colorize(string value, string colorHex)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(colorHex))
        {
            return value;
        }

        return "<color=" + colorHex.Trim() + ">" + value + "</color>";
    }

    private static string FormatPercent(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private static string GetGrade(float value)
    {
        float score = Mathf.Clamp01(value);
        if (score >= 0.9f)
        {
            return "A";
        }

        if (score >= 0.8f)
        {
            return "B";
        }

        if (score >= 0.7f)
        {
            return "C";
        }

        return "D";
    }

    private static string NormalizeKey(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return new string(rawValue.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private struct AssessmentScores
    {
        public float VerbalScore;
        public float SpatialScore;
        public float ProceduralScore;
        public float OverallScore;
        public int CompletedParts;
        public int TotalParts;
        public int UniqueInteractedParts;
        public int QuestionCount;
        public int TeacherTurnCount;
    }
}
