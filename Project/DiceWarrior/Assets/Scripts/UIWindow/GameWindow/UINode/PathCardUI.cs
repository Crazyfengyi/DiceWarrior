using System;
using cfg;
using cfg.eventcard;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.ResourceManager;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 路径卡牌UI类
/// </summary>
public sealed class PathCardUI : MonoBehaviour
{
    [SerializeField] private UICustomButton button; // 自定义按钮组件
    [SerializeField] private Image background; // 背景图片
    [SerializeField] private Image icon; // 卡牌图标
    [SerializeField] private TextMeshProUGUI pathTitleText; // 路径标题文本
    [SerializeField] private TextMeshProUGUI cardNameText; // 卡牌名称文本
    [SerializeField] private TextMeshProUGUI typeText; // 卡牌类型文本
    [SerializeField] private TextMeshProUGUI descText; // 卡牌描述文本

    private int cardIndex; // 卡牌索引
    private Action<int> clickCallback; // 点击回调函数

    /// <summary>
    /// 获取RectTransform组件
    /// </summary>
    public RectTransform RectTransform => transform as RectTransform;

    /// <summary>
    /// 初始化路径卡牌并注册点击事件。
    /// </summary>
    /// <param name="index">卡牌索引</param>
    /// <param name="onClick">点击回调函数</param>
    public void Init(int index, Action<int> onClick)
    {
        cardIndex = index;
        clickCallback = onClick;

        button.AddListener(OnClick);
    }

    /// <summary>
    /// 刷新路径标题、事件卡文本和图标
    /// </summary>
    public void Refresh(string pathTitle, EventCard card)
    {
        pathTitleText.text = pathTitle;
        bool hasCard = card != null;

        cardNameText.text = hasCard ? card.Name : "事件卡牌";
        typeText.text = hasCard ? GetTypeName(card.CardType) : string.Empty;
        descText.text = hasCard ? card.Desc : "无描述";

        icon.gameObject.SetActive(hasCard && !string.IsNullOrEmpty(card.SpriteName));
        if (hasCard && !string.IsNullOrEmpty(card.SpriteName))
        {
            ResourceManager.SetImageSprite(icon, card.SpriteName);
        }
    }

    /// <summary>
    /// 转发当前路径卡牌的点击事件。
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
                return cardType.ToString();
            case EEventCardType.Neutral:
                return cardType.ToString();
            case EEventCardType.Treasure:
                return cardType.ToString();
            default:
                return cardType.ToString();
        }
    }
}