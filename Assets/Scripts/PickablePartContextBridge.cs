using NudleNexus.Classroom;
using UnityEngine;

public class PickablePartContextBridge : MonoBehaviour
{
    public AnatomyLiveClient liveClient;

    private void Awake()
    {
        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>();
        }
    }

    private void OnEnable()
    {
        PickablePart.OnPickUpAnyPart += HandlePartPicked;
    }

    private void OnDisable()
    {
        PickablePart.OnPickUpAnyPart -= HandlePartPicked;
    }

    private void HandlePartPicked(PickablePart part)
    {
        if (part == null)
        {
            return;
        }

        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>();
            if (liveClient == null)
            {
                Debug.LogWarning("[PickablePartContextBridge] AnatomyLiveClient not found.");
                return;
            }
        }

        string selectedObject = part.Data != null && !string.IsNullOrWhiteSpace(part.Data.name)
            ? part.Data.name
            : part.gameObject.name;

        liveClient.SendContextUpdate(selectedObject);
        Debug.Log($"[PickablePartContextBridge] Sent context from grab: {selectedObject}");
    }
}
