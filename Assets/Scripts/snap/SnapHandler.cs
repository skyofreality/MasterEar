using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SnapHandler : MonoBehaviour {
    public string partName;
    public float snapThreshold = 0.2f;
    private bool isSnapped = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Rigidbody rb;
    private Collider cachedCollider;

    private void Start() {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
        cachedCollider = GetComponent<Collider>();
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

        isSnapped = true;

        if (rb != null) {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cachedCollider != null)
            cachedCollider.enabled = false;

        transform.DOMove(targetTransform.position, 0.5f).SetEase(Ease.InOutSine);
        transform.DORotateQuaternion(targetTransform.rotation, 0.5f).SetEase(Ease.InOutSine);

        ScoreManager.Instance?.AddPoints(10);
    }

    public void ResetIfNotSnapped() {
        if (!isSnapped) {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }

    public void ResetSnapState() {
        isSnapped = false;

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        if (rb != null) {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cachedCollider != null) {
            cachedCollider.enabled = true;
        }
    }
}
