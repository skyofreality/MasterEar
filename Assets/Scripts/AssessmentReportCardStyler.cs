using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class AssessmentReportCardStyler : MonoBehaviour
{
    [Header("Targets")]
    public Image panelBackground;
    public Image[] dividerLines;
    public TMP_Text[] textElements;

    [Header("Palette")]
    public Color backgroundColor = new Color(0.05f, 0.17f, 0.2f, 0.86f);
    public Color titleColor = Color.white;
    public Color accentColor = new Color(0.64f, 1f, 0.84f, 1f);
    public Color bodyColor = new Color(0.82f, 0.92f, 0.94f, 1f);
    public Color dividerColor = new Color(0.55f, 0.9f, 1f, 0.35f);

    [Header("Typography")]
    public float titleFontSize = 30f;
    public float overallFontSize = 26f;
    public float sectionFontSize = 18f;
    public float bodyFontSize = 15f;
    public float gradeFontSize = 42f;
    public float generatedAtFontSize = 12f;

    [Header("Behavior")]
    public bool autoFindChildren = true;
    public bool applyOnAwake = true;
    public bool applyInEditor = true;

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyStyle();
        }
    }

    private void OnValidate()
    {
        if (applyInEditor)
        {
            ApplyStyle();
        }
    }

    [ContextMenu("Apply Report Card Style")]
    public void ApplyStyle()
    {
        if (autoFindChildren)
        {
            FindTargets();
        }

        if (panelBackground != null)
        {
            panelBackground.color = backgroundColor;
        }

        foreach (Image divider in dividerLines)
        {
            if (divider != null)
            {
                divider.color = dividerColor;
            }
        }

        foreach (TMP_Text text in textElements)
        {
            ApplyTextStyle(text);
        }
    }

    private void FindTargets()
    {
        if (panelBackground == null)
        {
            panelBackground = GetComponent<Image>();
        }

        textElements = GetComponentsInChildren<TMP_Text>(true);

        Image[] images = GetComponentsInChildren<Image>(true);
        dividerLines = System.Array.FindAll(images, image =>
            image != null &&
            image.gameObject != gameObject &&
            IsDividerName(image.gameObject.name));
    }

    private void ApplyTextStyle(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        string objectName = text.gameObject.name.ToLowerInvariant();
        string textValue = text.text != null ? text.text.ToLowerInvariant() : string.Empty;

        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Truncate;

        if (objectName.Contains("title"))
        {
            text.color = titleColor;
            text.fontSize = titleFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }
        else if (objectName.Contains("overall"))
        {
            text.color = accentColor;
            text.fontSize = overallFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }
        else if (objectName.Contains("grade") || IsGradeText(textValue))
        {
            text.color = accentColor;
            text.fontSize = gradeFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }
        else if (objectName.Contains("generated") || objectName.Contains("time"))
        {
            text.color = bodyColor;
            text.fontSize = generatedAtFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Normal;
        }
        else if (IsSectionText(objectName, textValue))
        {
            text.color = bodyColor;
            text.fontSize = sectionFontSize;
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
        }
        else
        {
            text.color = bodyColor;
            text.fontSize = bodyFontSize;
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Normal;
        }
    }

    private static bool IsDividerName(string rawName)
    {
        string name = rawName.ToLowerInvariant();
        return name.Contains("divider") ||
               name.Contains("separator") ||
               name.Contains("line");
    }

    private static bool IsSectionText(string objectName, string textValue)
    {
        return objectName.Contains("verbal") ||
               objectName.Contains("spatial") ||
               objectName.Contains("procedural") ||
               objectName.Contains("recommendation") ||
               textValue.Contains("verbal / semantic") ||
               textValue.Contains("spatial / physical") ||
               textValue.Contains("procedural");
    }

    private static bool IsGradeText(string textValue)
    {
        string normalizedValue = textValue.Trim().ToUpperInvariant();
        return normalizedValue == "A" ||
               normalizedValue == "B" ||
               normalizedValue == "C" ||
               normalizedValue == "D";
    }
}
