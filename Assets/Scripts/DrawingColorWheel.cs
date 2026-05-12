using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DrawingColorWheel : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Drawing Target")]
    [SerializeField] private XRHandDraw targetDraw;

    [Header("UI")]
    [SerializeField] private Image wheelImage;
    [SerializeField] private Image selectedColorPreview;
    [SerializeField] private int generatedTextureSize = 256;

    [Header("Selection")]
    [SerializeField] private Color initialColor = Color.white;
    [SerializeField] private bool generateWheelSpriteOnStart = true;

    private RectTransform wheelRect;
    private Color selectedColor;

    private void Awake()
    {
        wheelImage = wheelImage != null ? wheelImage : GetComponent<Image>();
        wheelRect = wheelImage.rectTransform;

        if (targetDraw == null)
        {
            targetDraw = FindObjectOfType<XRHandDraw>();
        }

        if (wheelImage != null)
        {
            wheelImage.raycastTarget = true;
        }
    }

    private void Start()
    {
        if (generateWheelSpriteOnStart && wheelImage != null)
        {
            wheelImage.sprite = GenerateColorWheelSprite(Mathf.Max(32, generatedTextureSize));
            wheelImage.type = Image.Type.Simple;
            wheelImage.preserveAspect = true;
        }

        SelectColor(initialColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TrySelectColor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        TrySelectColor(eventData);
    }

    public void SetTargetDraw(XRHandDraw draw)
    {
        targetDraw = draw;
        ApplySelectedColorToDraw();
    }

    public void SelectColor(Color color)
    {
        selectedColor = color;

        if (selectedColorPreview != null)
        {
            selectedColorPreview.color = selectedColor;
        }

        ApplySelectedColorToDraw();
    }

    private void TrySelectColor(PointerEventData eventData)
    {
        if (wheelRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                wheelRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Rect rect = wheelRect.rect;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        if (radius <= 0f)
        {
            return;
        }

        Vector2 normalized = localPoint / radius;
        float saturation = normalized.magnitude;
        if (saturation > 1f)
        {
            return;
        }

        float angle = Mathf.Atan2(normalized.y, normalized.x);
        float hue = Mathf.Repeat(angle / (Mathf.PI * 2f), 1f);
        Color color = Color.HSVToRGB(hue, saturation, 1f);
        color.a = 1f;
        SelectColor(color);
    }

    private void ApplySelectedColorToDraw()
    {
        if (targetDraw != null)
        {
            targetDraw.UpdateLineColor(selectedColor);
        }
    }

    private static Sprite GenerateColorWheelSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float saturation = Mathf.Sqrt(dx * dx + dy * dy);

                if (saturation > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float angle = Mathf.Atan2(dy, dx);
                float hue = Mathf.Repeat(angle / (Mathf.PI * 2f), 1f);
                Color color = Color.HSVToRGB(hue, saturation, 1f);
                color.a = 1f;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
    }
}
