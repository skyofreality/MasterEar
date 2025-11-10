using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public void LoadSceneByName(string sceneName) {
        if (!string.IsNullOrEmpty(sceneName)) {
            SceneManager.LoadScene(sceneName);
        } else {
            Debug.LogWarning("Scene name is not provided.");
        }
    }
}
