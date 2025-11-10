using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

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
        isSnapped = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log(" Rigidbody kinematic & stopped");
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Animate position and rotation to target
        float duration = 0.5f;

        transform.DOMove(targetTransform.position, duration).SetEase(Ease.InOutSine);
        transform.DORotateQuaternion(targetTransform.rotation, duration).SetEase(Ease.InOutSine);

        // Optional: Add scaling or slight delay if needed

        ScoreManager.Instance?.AddPoints(10);
    }



    public void ResetIfNotSnapped() {
        if (!isSnapped) {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }
}
