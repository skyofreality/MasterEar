using UnityEngine;
using DG.Tweening;

public class FaceUser : MonoBehaviour {
    public Transform cameraToFace;
    public float smoothTime = 0.2f;

    void LateUpdate() {
        if (cameraToFace == null) return;

        Quaternion targetRotation = Quaternion.LookRotation(transform.position - cameraToFace.position);
        transform.DORotateQuaternion(targetRotation, smoothTime).SetEase(Ease.OutSine);
    }
}
