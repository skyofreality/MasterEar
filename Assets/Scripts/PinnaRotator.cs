using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

public class PinnaRotator : MonoBehaviour {
    [Header("Rotation Settings")]
    public float rotationSpeed = 50f; // degrees per second

    void Update() {
        // Get joystick X-axis (right controller)
        float inputX = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x;

        if (Mathf.Abs(inputX) > 0.1f) // Deadzone check
        {
            float rotationAmount = inputX * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, -rotationAmount); // Y-axis rotation
        }
    }
}
