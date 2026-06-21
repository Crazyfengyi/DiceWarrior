using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardMainMenuItem : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private UICustomButton button;

    public UICustomButton Button => button;

    public void Init()
    {
    }

    public void Refresh(string title, bool selected)
    {
        titleText.text = title;
        titleText.color = selected ? Color.white : new Color(0.82f, 0.88f, 1f, 1f);

        buttonText.text = "确定";
        buttonText.color = selected ? Color.white : new Color(0.7f, 0.78f, 0.92f, 1f);

        background.color = selected
            ? new Color(0.24f, 0.42f, 0.78f, 1f)
            : new Color(0.12f, 0.21f, 0.38f, 1f);

        icon.color = selected
            ? new Color(0.95f, 0.98f, 1f, 0.28f)
            : new Color(0.62f, 0.75f, 0.94f, 0.16f);
    }

    public void ApplyPose(Vector2 anchoredPosition, float scale, float alpha, float yRotation, int siblingOrder)
    {
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one * scale;
        rectTransform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        rectTransform.SetSiblingIndex(siblingOrder);

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = alpha > 0.45f;
        canvasGroup.blocksRaycasts = alpha > 0.45f;
    }
}