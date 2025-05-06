using UnityEngine;

public class ModelLoader : MonoBehaviour
{
    // Public variable to assign the 3D model prefab
    public GameObject modelPrefab;

    // Position to instantiate the model prefab
    public Vector3 spawnPosition = new Vector3(0, 1, 0);

    // Rotation to instantiate the model prefab
    public Vector3 spawnRotation = new Vector3(0, 0, 0); // Default rotation (no rotation)

    // Start is called before the first frame update
    void Start()
    {
        // Check if the prefab is assigned
        if (modelPrefab != null)
        {
            // Instantiate the prefab at the specified position and rotation
            GameObject modelInstance = Instantiate(modelPrefab, spawnPosition, Quaternion.Euler(spawnRotation));

            // Ensure the instantiated prefab is active
            if (!modelInstance.activeSelf)
            {
                modelInstance.SetActive(true);
            }

            // Log confirmation
            Debug.Log("3D Model Prefab instantiated at: " + modelInstance.transform.position + " with rotation: " + modelInstance.transform.rotation.eulerAngles);
        }
        else
        {
            // Log a warning if the prefab is not assigned
            Debug.LogWarning("3D Model Prefab not assigned! Please assign a prefab in the inspector.");
        }
    }
}
