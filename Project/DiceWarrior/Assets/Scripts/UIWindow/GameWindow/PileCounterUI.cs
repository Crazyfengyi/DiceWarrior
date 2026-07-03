using System;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PileCounterUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image background;

    private string hoverText;
    private Action<string, Vector2> showHoverCallback;
    private Action hideHoverCallback;

    public void Bind(UICustomButton bindButton, TextMeshProUGUI bindTitleText, TextMeshProUGUI bindCountText,
        Image bindBackground)
    {
        button = bindButton;
        titleText = bindTitleText;
        countText = bindCountText;
        background = bindBackground;
    }

    public void Init(Action<string, Vector2> onShowHover, Action onHideHover)
    {
        showHoverCallback = onShowHover;
        hideHoverCallback = onHideHover;
    }

    public void Refresh(string title, int count, string countHoverText)
    {
        hoverText = countHoverText;
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (countText != null)
        {
            countText.text = count.ToString();
        }

        if (background != null)
        {
            background.color = new Color(0.27f, 0.45f, 0.78f, 1f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        showHoverCallback?.Invoke(hoverText, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hideHoverCallback?.Invoke();
    }
}
