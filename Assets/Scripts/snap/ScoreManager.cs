using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour {
    public static ScoreManager Instance;

    public int score;
    public TMP_Text scoreText; // drag in inspector OR auto-find

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateScoreUI();
    }

    private void OnDestroy() {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //  Re-find the UI text in the newly loaded scene
        if (scoreText == null) {
            // Option 1: Find by exact object name (recommended: rename your TMP to "ScoreText")
            var texts = FindObjectsOfType<TMP_Text>(true);
            foreach (var t in texts) {
                if (t.name == "ScoreText") {
                    scoreText = t;
                    break;
                }
            }
        }

        UpdateScoreUI();
    }

    public void AddPoints(int points) {
        score += points;
        UpdateScoreUI();
    }

    public void ResetScore() {
        score = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI() {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
}
