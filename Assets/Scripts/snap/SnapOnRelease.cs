using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class SnapOnRelease : MonoBehaviour {
    private HandGrabInteractable grabInteractable;
    private SnapHandler snapHandler;

    private void Awake() {
        grabInteractable = GetComponent<HandGrabInteractable>();
        snapHandler = GetComponentInParent<SnapHandler>(); // Finds on parent if needed

        grabInteractable.WhenPointerEventRaised += OnPointerEventRaised;
    }

    private void OnDestroy() {
        grabInteractable.WhenPointerEventRaised -= OnPointerEventRaised;
    }

    private void OnPointerEventRaised(PointerEvent pointerEvent) {
        if (pointerEvent.Type == PointerEventType.Unselect) {
            snapHandler?.ResetIfNotSnapped();
        }
    }
}
