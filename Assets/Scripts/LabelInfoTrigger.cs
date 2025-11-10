using UnityEngine;
using TMPro;

public class LabelInfoTrigger : MonoBehaviour {
    [TextArea]
    public string bodyText;

    public TextMeshProUGUI sharedBodyText;

    public void OnButtonClick() {
        if (sharedBodyText == null) {
            Debug.LogError($"[LabelInfoTrigger] {name}: sharedBodyText is NULL");
            return;
        }
        Debug.Log($"[LabelInfoTrigger] Clicked {name}. Before: '{sharedBodyText.text}'");
        sharedBodyText.text = bodyText;
        Debug.Log($"[LabelInfoTrigger] After:  '{sharedBodyText.text}'");
    }

}
