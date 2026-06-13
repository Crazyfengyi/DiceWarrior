using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FloatTipItem : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;
    public Image bg;
    public TextMeshProUGUI tipText;

    public async Task Play(string text, Vector2 anchoredPosition, Color color)
    {
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one * 1.12f;

        tipText.text = text;
        tipText.raycastTarget = false;
        tipText.color = color;

        Vector2 endPosition = anchoredPosition + new Vector2(0f, 90f);
        DOTween.Sequence()
            .SetTarget(gameObject)
            .Join(rectTransform.DOAnchorPos(endPosition, 0.75f).SetEase(Ease.OutCubic))
            .Join(rectTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack))
            .Join(canvasGroup.DOFade(0f, 0.75f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        DOTween.Kill(gameObject);
    }
}
