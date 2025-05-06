using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the endoscope grabbable and manages interaction with OVR controllers
/// </summary>
public class EndoscopeGrabbable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EndoscopeController endoscopeController;
    
    [Header("Grab Settings")]
    [SerializeField] private float grabThreshold = 0.1f;
    [SerializeField] private Transform primaryGrabPoint;
    
    // Private variables
    private bool isGrabbed = false;
    private Transform grabbingController = null;
    private Vector3 grabOffset;
    private Quaternion grabRotationOffset;
    
    // Input values
    private float extendInput = 0f;
    private float rotateInput = 0f;
    
    void Start()
    {
        // If no specific grab point defined, use the transform of this object
        if (primaryGrabPoint == null)
            primaryGrabPoint = transform;
            
        // If no controller reference, try to find it
        if (endoscopeController == null)
            endoscopeController = GetComponent<EndoscopeController>();
    }
    
    void Update()
    {
        // Check for grab input from OVR controllers
        CheckGrabInput();
        
        // If grabbed, update position based on the controller
        if (isGrabbed && grabbingController != null)
        {
            UpdatePosition();
            CheckFunctionalInput();
        }
    }
    
    private void CheckGrabInput()
    {
        // This is a simplified version. In a real application, you would use proper OVR input methods
        
        // Check right controller grip button for grab
        bool rightGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        Transform rightController = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) ? 
            GameObject.Find("RightHandAnchor")?.transform : null;
            
        // Check left controller grip button for grab
        bool leftGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        Transform leftController = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) ? 
            GameObject.Find("LeftHandAnchor")?.transform : null;
        
        // Handle right controller grab/release
        if (rightController != null)
        {
            if (rightGrip && !isGrabbed && 
                Vector3.Distance(rightController.position, primaryGrabPoint.position) < grabThreshold)
            {
                GrabWith(rightController);
            }
            else if (!rightGrip && isGrabbed && grabbingController == rightController)
            {
                Release();
            }
        }
        
        // Handle left controller grab/release
        if (leftController != null)
        {
            if (leftGrip && !isGrabbed && 
                Vector3.Distance(leftController.position, primaryGrabPoint.position) < grabThreshold)
            {
                GrabWith(leftController);
            }
            else if (!leftGrip && isGrabbed && grabbingController == leftController)
            {
                Release();
            }
        }
    }

    private void GrabWith(Transform controller)
    {
        isGrabbed = true;
        grabbingController = controller;

        // Calculate offsets in local space relative to the controller
        grabOffset = controller.InverseTransformPoint(primaryGrabPoint.position);
        grabRotationOffset = Quaternion.Inverse(controller.rotation) * transform.rotation;

        // Notify controller
        if (endoscopeController != null)
            endoscopeController.OnGrabbed();
    }

    private void Release()
    {
        isGrabbed = false;
        
        // Notify controller
        if (endoscopeController != null)
            endoscopeController.OnReleased();
            
        grabbingController = null;
    }

    private void UpdatePosition()
    {
        // Correct position update
        transform.position = grabbingController.TransformPoint(grabOffset);

        // Correct rotation update
        transform.rotation = grabbingController.rotation * grabRotationOffset;
    }

    private void CheckFunctionalInput()
    {
        // Get the controlling hand
        OVRInput.Controller activeController = 
            (grabbingController.name.Contains("Right")) ? 
            OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
        
        // Get thumbstick input for tube extension/retraction
        extendInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, activeController).y;
        
        // Get thumbstick input for tip rotation
        rotateInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, activeController).x;
        
        // Apply inputs to endoscope controller
        if (endoscopeController != null)
        {
            if (Mathf.Abs(extendInput) > 0.1f)
                endoscopeController.ExtendTube(extendInput);
                
            if (Mathf.Abs(rotateInput) > 0.1f)
                endoscopeController.RotateTip(rotateInput);
        }
    }
}
