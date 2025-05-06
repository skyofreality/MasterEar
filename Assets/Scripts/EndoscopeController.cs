using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the endoscope functionality in VR
/// </summary>
public class EndoscopeController : MonoBehaviour
{
    [Header("Endoscope Components")]
    [SerializeField] private Transform endoscopeHandle;
    [SerializeField] private Transform endoscopeTube;
    [SerializeField] private Transform endoscopeTip;
    [SerializeField] private Camera endoscopeCamera;
    [SerializeField] private MeshRenderer endoscopeDisplay;
    
    [Header("Endoscope Settings")]
    [SerializeField] private float maxTubeLength = 0.5f;
    [SerializeField] private float minTubeLength = 0.1f;
    [SerializeField] private float tubeExtendSpeed = 0.5f;
    [SerializeField] private float tipRotationSpeed = 100f;
    [SerializeField] private int renderTextureSize = 512;
    
    // Private variables
    private RenderTexture endoscopeTexture;
    private float currentTubeLength = 0.2f;
    private Material displayMaterial;
    private bool isActive = false;
    
    void Start()
    {
        // Create render texture for endoscope camera
        endoscopeTexture = new RenderTexture(renderTextureSize, renderTextureSize, 24);
        
        // Set up camera
        if (endoscopeCamera != null)
        {
            endoscopeCamera.targetTexture = endoscopeTexture;
            endoscopeCamera.fieldOfView = 60f;
        }
        
        // Set up display
        if (endoscopeDisplay != null)
        {
            displayMaterial = new Material(Shader.Find("Unlit/Texture"));
            displayMaterial.mainTexture = endoscopeTexture;
            endoscopeDisplay.material = displayMaterial;
        }
        
        // Initially deactivate endoscope view
        SetEndoscopeActive(false);
    }
    
    public void SetEndoscopeActive(bool active)
    {
        isActive = active;
        
        // Enable/disable camera
        if (endoscopeCamera != null)
            endoscopeCamera.enabled = active;
            
        // Show/hide display
        if (endoscopeDisplay != null)
            endoscopeDisplay.gameObject.SetActive(active);
    }
    
    public void ExtendTube(float amount)
    {
        if (!isActive) return;
        
        // Calculate new tube length
        currentTubeLength = Mathf.Clamp(currentTubeLength + amount * tubeExtendSpeed * Time.deltaTime, 
                                        minTubeLength, maxTubeLength);
        
        // Update tube scale
        if (endoscopeTube != null)
        {
            Vector3 scale = endoscopeTube.localScale;
            scale.y = currentTubeLength;
            endoscopeTube.localScale = scale;
            
            // Position tube so one end stays at handle
            Vector3 tubePos = endoscopeTube.localPosition;
            tubePos.z = currentTubeLength / 2;
            endoscopeTube.localPosition = tubePos;
        }
        
        // Update tip position
        if (endoscopeTip != null)
        {
            Vector3 tipPos = endoscopeTip.localPosition;
            tipPos.z = currentTubeLength;
            endoscopeTip.localPosition = tipPos;
        }
    }
    
    public void RotateTip(float amount)
    {
        if (!isActive || endoscopeTip == null) return;
        
        // Rotate the tip around its Y axis
        endoscopeTip.Rotate(0, amount * tipRotationSpeed * Time.deltaTime, 0, Space.Self);
    }
    
    public void OnGrabbed()
    {
        SetEndoscopeActive(true);
    }
    
    public void OnReleased()
    {
        // Optionally deactivate when released
        // SetEndoscopeActive(false);
    }
}
