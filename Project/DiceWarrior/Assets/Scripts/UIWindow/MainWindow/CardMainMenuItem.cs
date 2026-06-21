using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardMainMenuItem : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image background;
    [SerializeField] private Image imagePlaceholder;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button button;

    public Button Button => button;

    public void Bind(RectTransform bindRectTransform, CanvasGroup bindCanvasGroup, Image bindBackground,
        Image bindImagePlaceholder, TextMeshProUGUI bindTitleText, TextMeshProUGUI bindButtonText, Button bindButton)
    {
        rectTransform = bindRectTransform;
        canvasGroup = bindCanvasGroup;
        background = bindBackground;
        imagePlaceholder = bindImagePlaceholder;
        titleText = bindTitleText;
        buttonText = bindButtonText;
        button = bindButton;
    }

    public void Refresh(string title, bool selected)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (buttonText != null)
        {
            buttonText.text = title;
        }

        if (background != null)
        {
            background.color = selected
                ? new Color(0.24f, 0.45f, 0.86f, 1f)
                : new Color(0.16f, 0.31f, 0.64f, 1f);
        }

        if (imagePlaceholder != null)
        {
            imagePlaceholder.color = selected
                ? new Color(0.94f, 0.48f, 0.15f, 1f)
                : new Color(0.62f, 0.34f, 0.18f, 1f);
        }
    }

    public void ApplyPose(Vector2 anchoredPosition, float scale, float alpha, float yRotation, int siblingOrder)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one * scale;
        rectTransform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        rectTransform.SetSiblingIndex(siblingOrder);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = alpha > 0.65f;
            canvasGroup.blocksRaycasts = alpha > 0.65f;
        }
    }
}
