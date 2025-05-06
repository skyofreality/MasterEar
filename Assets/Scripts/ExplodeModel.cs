using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

[System.Serializable]
public class PartData {
    public Transform partTransform;
    public Vector3 explosionDirection;
}

public class ExplodeModel : MonoBehaviour {
    public List<PartData> parts = new List<PartData>();
    public float explodeDistance = 1f;
    public float explodeDuration = 1f;

    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();


    private void Start() {
        foreach (var part in parts) {
            if (part.partTransform != null)
                originalPositions[part.partTransform] = part.partTransform.localPosition;
            originalRotations[part.partTransform] = part.partTransform.localRotation;
        }
    }

    public void Explode() {
        foreach (var part in parts) {
            if (part.partTransform != null) {
                // Convert direction from local to world based on the part's transform
                Vector3 worldDirection = part.partTransform.TransformDirection(part.explosionDirection.normalized);

                // Convert world direction to local space relative to the parent
                Vector3 localDirection = transform.InverseTransformDirection(worldDirection);

                // Apply movement based on original local position + direction
                Vector3 targetPos = originalPositions[part.partTransform] + part.explosionDirection * explodeDistance;

                part.partTransform.DOLocalMove(targetPos, explodeDuration).SetEase(Ease.OutExpo);
            }
        }
    }



    public void ResetModel() {
        foreach (var part in parts) {
            if (part.partTransform != null) {
                part.partTransform.DOLocalMove(originalPositions[part.partTransform], explodeDuration).SetEase(Ease.InOutExpo);
                part.partTransform.DOLocalRotateQuaternion(originalRotations[part.partTransform], explodeDuration).SetEase(Ease.InOutExpo);
            }
        }
    }

}