using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PlayPause : MonoBehaviour
{
    public VideoPlayer videoPlayer;  // Reference to the VideoPlayer component
    public Button playPauseButton;   // Button to toggle play/pause

    void Start()
    {
        // Ensure the VideoPlayer is assigned
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer not assigned!");
            return;
        }

        // Set up the Play/Pause button listener
        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(TogglePlayPause);
    }

    // Toggle between Play and Pause
    void TogglePlayPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();  // Pause the video if it's playing
        }
        else
        {
            videoPlayer.Play();   // Play the video if it's paused
        }
    }
}
