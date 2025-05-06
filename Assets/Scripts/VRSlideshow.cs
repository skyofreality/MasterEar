using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class VRSlideshow : MonoBehaviour {
    public Image displayImage;
    public Sprite[] slideImages;

    public float animationDuration = 0.5f;
    private int currentIndex = 0;
    private bool isAnimating = false;

    private CanvasGroup canvasGroup;

    void Awake() {
        canvasGroup = displayImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = displayImage.gameObject.AddComponent<CanvasGroup>();
    }

    void Start() {
        if (slideImages.Length > 0) {
            currentIndex = 0;
            displayImage.sprite = slideImages[currentIndex];
            canvasGroup.alpha = 1f;
        }
    }

    public void ShowNext() {
        if (isAnimating || slideImages.Length == 0) return;

        int nextIndex = (currentIndex + 1) % slideImages.Length;
        AnimateSlide(nextIndex, 1);
    }

    public void ShowPrevious() {
        if (isAnimating || slideImages.Length == 0) return;

        int prevIndex = (currentIndex - 1 + slideImages.Length) % slideImages.Length;
        AnimateSlide(prevIndex, -1);
    }

    private void AnimateSlide(int newIndex, int direction) {
        isAnimating = true;

        Sequence seq = DOTween.Sequence();

        // Fade out & slide out
        seq.Append(canvasGroup.DOFade(0f, animationDuration * 0.5f));
        seq.Join(displayImage.rectTransform.DOAnchorPosX(-300 * direction, animationDuration * 0.5f).SetEase(Ease.InOutQuad));

        // Midpoint: switch sprite and reset position
        seq.AppendCallback(() => {
            displayImage.sprite = slideImages[newIndex];
            displayImage.rectTransform.anchoredPosition = new Vector2(300 * direction, 0);
        });

        // Fade in & slide in
        seq.Append(canvasGroup.DOFade(1f, animationDuration * 0.5f));
        seq.Join(displayImage.rectTransform.DOAnchorPosX(0, animationDuration * 0.5f).SetEase(Ease.InOutQuad));

        seq.OnComplete(() => {
            currentIndex = newIndex;
            isAnimating = false;
        });
    }
}
