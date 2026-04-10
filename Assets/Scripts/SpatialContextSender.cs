using System.Text;
using UnityEngine;

public class SpatialContextSender : MonoBehaviour
{
    [Header("Context")]
    public string contextTitle;

    [TextArea(2, 6)]
    public string contextDescription;

    public bool includeGameObjectNameWhenTitleEmpty = true;

    [Header("Runtime Reference")]
    public AnatomyLiveClient liveClient;

    private void Awake()
    {
        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>();
        }
    }

    public void SetLiveClient(AnatomyLiveClient client)
    {
        liveClient = client;
    }

    public void SendContext()
    {
        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>();
            if (liveClient == null)
            {
                Debug.LogError("[SpatialContextSender] AnatomyLiveClient not found in scene.");
                return;
            }
        }

        string contextToSend = BuildContextString();
        if (string.IsNullOrWhiteSpace(contextToSend))
        {
            Debug.LogWarning("[SpatialContextSender] No chart/panel context configured on '" + gameObject.name + "'.");
            return;
        }

        liveClient.SendContextUpdate(contextToSend);
        Debug.Log("[SpatialContextSender] Sent spatial context: " + contextToSend);
    }

    private string BuildContextString()
    {
        string resolvedTitle = !string.IsNullOrWhiteSpace(contextTitle)
            ? contextTitle.Trim()
            : (includeGameObjectNameWhenTitleEmpty ? gameObject.name.Trim() : string.Empty);

        string resolvedDescription = contextDescription?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(resolvedTitle))
        {
            return resolvedDescription;
        }

        if (string.IsNullOrWhiteSpace(resolvedDescription))
        {
            return resolvedTitle;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(resolvedTitle);
        builder.Append(": ");
        builder.Append(resolvedDescription);
        return builder.ToString();
    }
}
