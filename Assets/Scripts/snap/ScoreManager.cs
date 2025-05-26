using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour {
    public static ScoreManager Instance;

    public int score = 0;

    public TMP_Text scoreText;

private void Awake() {
        // Ensure only one instance exists
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persists across scenes
    }

    public void AddPoints(int points) {
        score += points;
        if (scoreText != null) {
            scoreText.text = "Score: " + score;
        }
    }
}
