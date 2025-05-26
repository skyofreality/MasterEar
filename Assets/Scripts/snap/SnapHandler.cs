using UnityEngine;
using UnityEngine.SceneManagement;

public class SnapHandler : MonoBehaviour {
    public string partName;
    public float snapThreshold = 0.2f;
    private bool isSnapped = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Start() {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    private void OnTriggerStay(Collider other) {
        if (isSnapped) return;

        var snapTarget = other.GetComponent<SnapTargetIdentifier>();
        if (snapTarget != null && snapTarget.partName == partName) {
            float dist = Vector3.Distance(transform.position, other.transform.position);
            Debug.Log($"[STAY] Target: {snapTarget.partName}, My part: {partName}, Distance: {dist}");

            if (dist <= snapThreshold) {
                Debug.Log(" [STAY] Snapping to: " + other.name);
                SnapToTarget(other.transform);
            }
        }
    }


    private void SnapToTarget(Transform targetTransform) {
        if (isSnapped) return;

        Debug.Log($"Snapping to: {targetTransform.name}");

        transform.position = targetTransform.position;
        transform.rotation = targetTransform.rotation;

        isSnapped = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = true;
            Debug.Log(" Rigidbody set to kinematic");
        }

        var col = GetComponent<Collider>();
        if (col != null) {
            col.enabled = false;
            Debug.Log(" Collider disabled");
        }

        ScoreManager.Instance?.AddPoints(10);
    }



    public void ResetIfNotSnapped() {
        if (!isSnapped) {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }
}
