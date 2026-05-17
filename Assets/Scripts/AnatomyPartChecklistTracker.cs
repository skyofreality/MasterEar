using System;
using System.Collections.Generic;
using System.Linq;
using NudleNexus.Classroom;
using TMPro;
using UnityEngine;

public class AnatomyPartChecklistTracker : MonoBehaviour
{
    public event Action<AnatomyPartChecklistTracker> ObjectivesCompleted;

    [Header("Checklist Settings")]
    public string[] trackedParts = Array.Empty<string>();
    public bool trackUnlistedPartsWhenEmpty = true;

    [Header("Optional UI")]
    public TMP_Text checklistText;
    public TMP_Text summaryText;
    public TMP_Text completionText;

    [Header("Debug")]
    public bool logChecklistUpdates = false;

    private readonly HashSet<string> completedPartKeys = new HashSet<string>();
    private readonly List<string> orderedPartKeys = new List<string>();
    private readonly Dictionary<string, string> displayNameByKey = new Dictionary<string, string>();
    private bool hasRaisedObjectivesCompleted;

    public int CompletedCount => completedPartKeys.Count;
    public int TotalCount => orderedPartKeys.Count;
    public float Completion01 => TotalCount > 0 ? CompletedCount / (float)TotalCount : 0f;
    public bool IsComplete => TotalCount > 0 && CompletedCount >= TotalCount;

    private void Awake()
    {
        RebuildTrackedParts();
        RefreshUI();
    }

    private void OnEnable()
    {
        PickablePart.OnPickUpAnyPart += HandlePartPicked;
    }

    private void OnDisable()
    {
        PickablePart.OnPickUpAnyPart -= HandlePartPicked;
    }

    [ContextMenu("Refresh Checklist From Tracked Parts")]
    public void RebuildTrackedParts()
    {
        completedPartKeys.Clear();
        orderedPartKeys.Clear();
        displayNameByKey.Clear();
        hasRaisedObjectivesCompleted = false;

        if (trackedParts == null)
        {
            RefreshUI();
            return;
        }

        foreach (string trackedPart in trackedParts)
        {
            string displayName = trackedPart?.Trim() ?? string.Empty;
            string key = NormalizePartKey(displayName);
            if (string.IsNullOrWhiteSpace(key) || displayNameByKey.ContainsKey(key))
            {
                continue;
            }

            orderedPartKeys.Add(key);
            displayNameByKey[key] = displayName;
        }

        RefreshUI();
    }

    public void ResetChecklist()
    {
        completedPartKeys.Clear();
        hasRaisedObjectivesCompleted = false;
        RefreshUI();
    }

    private void HandlePartPicked(PickablePart part)
    {
        if (part == null)
        {
            return;
        }

        string selectedObject = part.Data != null && !string.IsNullOrWhiteSpace(part.Data.name)
            ? part.Data.name
            : part.gameObject.name;

        MarkPartCompleted(selectedObject);
    }

    public void MarkPartCompleted(string rawPartName)
    {
        string displayName = rawPartName?.Trim() ?? string.Empty;
        string key = NormalizePartKey(displayName);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        bool partIsTracked = orderedPartKeys.Contains(key);
        if (!partIsTracked)
        {
            if (!trackUnlistedPartsWhenEmpty || orderedPartKeys.Count > 0)
            {
                return;
            }

            orderedPartKeys.Add(key);
            displayNameByKey[key] = displayName;
        }
        else if (!displayNameByKey.ContainsKey(key))
        {
            displayNameByKey[key] = displayName;
        }

        bool added = completedPartKeys.Add(key);
        if (!added)
        {
            return;
        }

        if (logChecklistUpdates)
        {
            Debug.Log("[AnatomyPartChecklistTracker] Completed part: " + GetDisplayName(key));
        }

        RefreshUI();
        RaiseCompletionIfReady();
    }

    private void RefreshUI()
    {
        if (checklistText != null)
        {
            checklistText.text = BuildChecklistText();
        }

        if (summaryText != null)
        {
            summaryText.text = "Parts explored: " + CompletedCount + "/" + TotalCount;
        }

        if (completionText != null)
        {
            completionText.text = IsComplete
                ? "Objective complete: all target anatomy parts explored."
                : "Objective: interact with all target anatomy parts.";
        }
    }

    private void RaiseCompletionIfReady()
    {
        if (hasRaisedObjectivesCompleted || !IsComplete)
        {
            return;
        }

        hasRaisedObjectivesCompleted = true;
        ObjectivesCompleted?.Invoke(this);

        if (logChecklistUpdates)
        {
            Debug.Log("[AnatomyPartChecklistTracker] All target objectives completed.");
        }
    }

    private string BuildChecklistText()
    {
        if (orderedPartKeys.Count == 0)
        {
            return "No checklist parts configured.";
        }

        List<string> lines = new List<string>(orderedPartKeys.Count);
        foreach (string key in orderedPartKeys)
        {
            bool isCompleted = completedPartKeys.Contains(key);
            string prefix = isCompleted ? "[x]" : "[ ]";
            string displayName = GetDisplayName(key);
            if (isCompleted)
            {
                displayName = "<s>" + displayName + "</s>";
            }

            lines.Add(prefix + " " + displayName);
        }

        return string.Join("\n", lines);
    }

    private string GetDisplayName(string key)
    {
        if (displayNameByKey.TryGetValue(key, out string displayName) && !string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return key;
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
