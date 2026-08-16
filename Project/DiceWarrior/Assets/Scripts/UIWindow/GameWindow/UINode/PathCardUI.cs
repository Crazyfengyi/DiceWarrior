using System;
using cfg;
using cfg.eventcard;
using Cysharp.Threading.Tasks;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.ResourceManager;

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
    private int refreshVersion; // 卡牌刷新版本号
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> backgroundSpriteCache =
        new System.Collections.Generic.Dictionary<string, Sprite>(); // 背景图片缓存
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> iconSpriteCache =
        new System.Collections.Generic.Dictionary<string, Sprite>(); // 图标图片缓存

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
        int currentRefreshVersion = ++refreshVersion;
        pathTitleText.text = pathTitle;
        bool hasCard = card != null;

        cardNameText.text = hasCard ? card.Name : "事件卡牌";
        typeText.text = hasCard ? GetTypeName(card.CardType) : string.Empty;
        descText.text = hasCard ? card.Desc : "无描述";

        if (background != null)
        {
            if (hasCard)
            {
                SetBackgroundSpriteAsync(GetBackgroundSpriteName(card.CardType), currentRefreshVersion).Forget();
            }
            else
            {
                background.sprite = null;
            }
        }

        if (icon != null)
        {
            bool hasIcon = hasCard && !string.IsNullOrEmpty(card.SpriteName);
            icon.gameObject.SetActive(hasIcon);
            if (hasIcon)
            {
                SetIconSpriteAsync(card.SpriteName, currentRefreshVersion).Forget();
            }
            else
            {
                icon.sprite = null;
            }
        }
    }

    /// <summary>
    /// 异步设置路径卡背景，并忽略过期的加载结果。
    /// </summary>
    private async UniTask SetBackgroundSpriteAsync(string spriteName, int currentRefreshVersion)
    {
        if (backgroundSpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
        {
            if (currentRefreshVersion == refreshVersion && background != null)
            {
                background.sprite = cachedSprite;
            }

            return;
        }

        Sprite loadedSprite = await ResourceManager.LoadSprite(spriteName);
        if (loadedSprite == null)
        {
            return;
        }

        backgroundSpriteCache[spriteName] = loadedSprite;
        if (currentRefreshVersion == refreshVersion && background != null)
        {
            background.sprite = loadedSprite;
        }
    }

    /// <summary>
    /// 异步设置路径卡图标，并忽略过期的加载结果。
    /// </summary>
    private async UniTask SetIconSpriteAsync(string spriteName, int currentRefreshVersion)
    {
        if (iconSpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
        {
            if (currentRefreshVersion == refreshVersion && icon != null)
            {
                icon.sprite = cachedSprite;
            }

            return;
        }

        Sprite loadedSprite = await ResourceManager.LoadSprite(spriteName);
        if (loadedSprite == null)
        {
            return;
        }

        iconSpriteCache[spriteName] = loadedSprite;
        if (currentRefreshVersion == refreshVersion && icon != null)
        {
            icon.sprite = loadedSprite;
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

    /// <summary>
    /// 根据事件类型获取对应颜色的路径卡背景图片名称。
    /// </summary>
    private static string GetBackgroundSpriteName(EEventCardType cardType)
    {
        switch (cardType)
        {
            case EEventCardType.Battle:
                return "card_01";
            case EEventCardType.Neutral:
                return "card_02";
            case EEventCardType.Treasure:
                return "card_03";
            case EEventCardType.Spite:
                return "card_04";
            case EEventCardType.Friend:
                return "card_05";
            case EEventCardType.Special:
                return "card_06";
            default:
                return "card_01";
        }
    }
}
