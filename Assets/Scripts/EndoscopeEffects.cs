using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Handles visual effects and feedback for the endoscope
/// </summary>
public class EndoscopeEffects : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private Light tipLight;
    [SerializeField] private float tipLightIntensity = 1.5f;
    [SerializeField] private Color tipLightColor = Color.white;
    
    [Header("Screenshot")]
    [SerializeField] private KeyCode screenshotKey = KeyCode.Space;
    [SerializeField] private string screenshotFolder = "EndoscopeCaptures";
    
    [Header("References")]
    [SerializeField] private Camera endoscopeCamera;
    [SerializeField] private RenderTexture endoscopeView;
    
    // Optional effects
    private bool isRecording = false;
    private List<Texture2D> recordedFrames = new List<Texture2D>();
    
    void Start()
    {
        // Setup tip light
        if (tipLight != null)
        {
            tipLight.color = tipLightColor;
            tipLight.intensity = tipLightIntensity;
        }
        
        // Create screenshot directory if needed
        if (!System.IO.Directory.Exists(screenshotFolder))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/" + screenshotFolder);
        }
    }
    
    public void ToggleTipLight(bool enabled)
    {
        if (tipLight != null)
            tipLight.enabled = enabled;
    }
    
    public void AdjustTipLightIntensity(float intensity)
    {
        if (tipLight != null)
            tipLight.intensity = intensity;
    }
    
    public void CaptureScreenshot()
    {
        if (endoscopeCamera == null || endoscopeView == null)
            return;
            
        StartCoroutine(CaptureEndoscopeView());
    }
    
    private IEnumerator CaptureEndoscopeView()
    {
        // Wait for end of frame to ensure camera has rendered
        yield return new WaitForEndOfFrame();
        
        // Create texture and read pixels
        Texture2D screenshot = new Texture2D(endoscopeView.width, endoscopeView.height, TextureFormat.RGB24, false);
        
        // Current active render texture
        RenderTexture currentRT = RenderTexture.active;
        
        // Set endoscope view as active
        RenderTexture.active = endoscopeView;
        
        // Read pixels
        screenshot.ReadPixels(new Rect(0, 0, endoscopeView.width, endoscopeView.height), 0, 0);
        screenshot.Apply();
        
        // Restore active render texture
        RenderTexture.active = currentRT;
        
        // Save to file
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = Application.dataPath + "/" + screenshotFolder + "/Endoscope_" + timestamp + ".png";
        
        byte[] bytes = screenshot.EncodeToPNG();
        System.IO.File.WriteAllBytes(filename, bytes);
        
        Debug.Log("Endoscope screenshot saved to: " + filename);
    }
    
    public void ToggleRecording()
    {
        isRecording = !isRecording;
        
        if (isRecording)
        {
            StartCoroutine(RecordFrames());
            Debug.Log("Endoscope recording started");
        }
        else
        {
            StopAllCoroutines();
            SaveRecording();
            Debug.Log("Endoscope recording stopped and saved");
        }
    }
    
    private IEnumerator RecordFrames()
    {
        recordedFrames.Clear();
        
        while (isRecording)
        {
            // Wait for end of frame
            yield return new WaitForEndOfFrame();
            
            // Capture frame
            Texture2D frame = new Texture2D(endoscopeView.width, endoscopeView.height, TextureFormat.RGB24, false);
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = endoscopeView;
            frame.ReadPixels(new Rect(0, 0, endoscopeView.width, endoscopeView.height), 0, 0);
            frame.Apply();
            RenderTexture.active = currentRT;
            
            // Add to frames
            recordedFrames.Add(frame);
            
            // Don't capture every frame to save memory
            yield return new WaitForSeconds(0.1f); // 10 FPS recording
        }
    }
    
    private void SaveRecording()
    {
        // In a real application, this would save the frames as a video file
        // For this example, we'll just save the last frame as an image
        
        if (recordedFrames.Count > 0)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = Application.dataPath + "/" + screenshotFolder + "/EndoscopeRecording_" + timestamp + ".png";
            
            byte[] bytes = recordedFrames[recordedFrames.Count - 1].EncodeToPNG();
            System.IO.File.WriteAllBytes(filename, bytes);
            
            Debug.Log("Recording saved (last frame only): " + filename);
        }
        
        // Clear frames to free memory
        recordedFrames.Clear();
    }
}
