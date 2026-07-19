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

    public RectTransform RectTransform => transform as RectTransform;

    /// <summary>
    /// 绑定牌堆计数器的界面引用。
    /// </summary>
    public void Bind(UICustomButton bindButton, TextMeshProUGUI bindTitleText, TextMeshProUGUI bindCountText,
        Image bindBackground)
    {
        button = bindButton;
        titleText = bindTitleText;
        countText = bindCountText;
        background = bindBackground;
    }

    /// <summary>
    /// 初始化牌堆计数器的悬停事件回调。
    /// </summary>
    public void Init(Action<string, Vector2> onShowHover, Action onHideHover)
    {
        showHoverCallback = onShowHover;
        hideHoverCallback = onHideHover;
    }

    /// <summary>
    /// 刷新牌堆标题、数量和悬停提示文本。
    /// </summary>
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

    /// <summary>
    /// 显示牌堆的悬停提示。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        showHoverCallback?.Invoke(hoverText, eventData.position);
    }

    /// <summary>
    /// 隐藏牌堆的悬停提示。
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        hideHoverCallback?.Invoke();
    }
}
