using UnityEngine;
using TMPro;

public class PartInfoUIManager : MonoBehaviour {
    public static PartInfoUIManager Instance;

    [Header("UI Elements")]
    public GameObject infoPanel;
    public TMP_Text titleText;
    public TMP_Text detailText;

    private void Awake() {
        Instance = this;
        // No longer hiding the panel at start
        // infoPanel.SetActive(false); 
    }

    private void OnEnable() {
        PickablePart.OnPickUpAnyPart += ShowInfo;
        // We don’t need to hide the info anymore
        // PickablePart.OnReleaseAnyPart += HideInfo;
    }

    private void OnDisable() {
        PickablePart.OnPickUpAnyPart -= ShowInfo;
        // PickablePart.OnReleaseAnyPart -= HideInfo;
    }

    private void ShowInfo(PickablePart part) {
        if (part != null && part.Data != null) {
            titleText.text = part.Data.name;
            detailText.text = part.Data.detail;
            // infoPanel.SetActive(true); // not needed
        }
    }

    // You can delete this method or leave it empty
    private void HideInfo(PickablePart part) {
        // Do nothing or remove this method entirely
    }
}
