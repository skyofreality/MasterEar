using UnityEngine;

public class VideoPlayerLoader : MonoBehaviour
{
    // Public variable to assign the VideoPlayer prefab
    public GameObject videoPlayerPrefab;

    // Position to instantiate the VideoPlayer prefab
    public Vector3 spawnPosition = new Vector3(0, 1, 5);

    // Rotation to instantiate the VideoPlayer prefab
    public Vector3 spawnRotation = new Vector3(0, 0, 0);  // Default rotation (no rotation)

    // Start is called before the first frame update
    void Start()
    {
        // Check if the prefab is assigned
        if (videoPlayerPrefab != null)
        {
            // Instantiate the prefab at the specified position and rotation
            GameObject videoPlayerInstance = Instantiate(videoPlayerPrefab, spawnPosition, Quaternion.Euler(spawnRotation));

            // Ensure the instantiated prefab is active
            if (!videoPlayerInstance.activeSelf)
            {
                videoPlayerInstance.SetActive(true);
            }

            // Log confirmation
            Debug.Log("Video Player Prefab instantiated at: " + videoPlayerInstance.transform.position + " with rotation: " + videoPlayerInstance.transform.rotation.eulerAngles);
        }
        else
        {
            // Log a warning if the prefab is not assigned
            Debug.LogWarning("VideoPlayer Prefab not assigned! Please assign a prefab in the inspector.");
        }
    }
}
