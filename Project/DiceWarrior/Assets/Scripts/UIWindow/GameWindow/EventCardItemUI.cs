using System;
using cfg;
using cfg.eventcard;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.ResourceManager;

public sealed class EventCardItemUI : MonoBehaviour
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descText;

    private int cardIndex;
    private Action<int> clickCallback;

    /// <summary>
    /// 绑定事件卡项的界面引用。
    /// </summary>
    public void Bind(UICustomButton bindButton, Image bindIcon, TextMeshProUGUI bindTitleText,
        TextMeshProUGUI bindTypeText, TextMeshProUGUI bindDescText)
    {
        button = bindButton;
        icon = bindIcon;
        titleText = bindTitleText;
        typeText = bindTypeText;
        descText = bindDescText;
    }

    /// <summary>
    /// 初始化事件卡项并注册点击事件。
    /// </summary>
    public void Init(int index, Action<int> onClick)
    {
        cardIndex = index;
        clickCallback = onClick;

        if (button == null)
        {
            button = GetComponent<UICustomButton>();
        }

        if (button != null)
        {
            button.AddListener(OnClick);
        }
    }

    /// <summary>
    /// 刷新事件卡项的图标和文本显示。
    /// </summary>
    public void Refresh(EventCard card)
    {
        bool hasCard = card != null;
        gameObject.SetActive(hasCard);
        if (!hasCard)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = card.Name;
        }

        if (typeText != null)
        {
            typeText.text = GetTypeName(card.CardType);
        }

        if (descText != null)
        {
            descText.text = card.Desc;
        }

        if (icon != null)
        {
            icon.gameObject.SetActive(!string.IsNullOrEmpty(card.SpriteName));
            if (!string.IsNullOrEmpty(card.SpriteName))
            {
                ResourceManager.SetImageSprite(icon, card.SpriteName);
            }
        }

        if (button != null && button.TargetButton != null)
        {
            button.TargetButton.interactable = true;
        }
    }

    /// <summary>
    /// 转发当前事件卡项的点击事件。
    /// </summary>
    private void OnClick()
    {
        clickCallback?.Invoke(cardIndex);
    }

    /// <summary>
    /// 获取事件卡类型的显示名称。
    /// </summary>
    private static string GetTypeName(EEventCardType cardType)
    {
        switch (cardType)
        {
            case EEventCardType.Battle:
                return "\u6218\u6597";
            case EEventCardType.Neutral:
                return "\u4e2d\u7acb\u4e8b\u4ef6";
            case EEventCardType.Treasure:
                return "\u5b9d\u7bb1\u4e8b\u4ef6";
            default:
                return "\u672a\u77e5";
        }
    }
}
