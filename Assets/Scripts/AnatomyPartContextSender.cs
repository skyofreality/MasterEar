using UnityEngine;

public class AnatomyPartContextSender : MonoBehaviour
{
    [Header("Context")]
    public string partId;

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
                Debug.LogError("[AnatomyPartContextSender] AnatomyLiveClient not found in scene.");
                return;
            }
        }

        string idToSend = string.IsNullOrWhiteSpace(partId) ? gameObject.name : partId;
        liveClient.SendContextUpdate(idToSend);
    }
}
