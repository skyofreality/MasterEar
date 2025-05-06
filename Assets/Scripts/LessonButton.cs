using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LessonButton : MonoBehaviour
{
    // These will be set from the LessonManager
    public TextMeshProUGUI buttonTitleText;
    public TextMeshProUGUI buttonDescriptionText;

    // Method to set the button text dynamically
    public void SetButtonData(string title, string description)
    {
        if (buttonTitleText != null)
        {
            buttonTitleText.text = title; // Set the title of the button
        }

        if (buttonDescriptionText != null)
        {
            buttonDescriptionText.text = description; // Set the description of the button
        }
    }
}
