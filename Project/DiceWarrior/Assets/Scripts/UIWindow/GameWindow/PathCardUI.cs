using System;
using cfg;
using cfg.eventcard;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.ResourceManager;

public sealed class PathCardUI : MonoBehaviour
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI pathTitleText;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descText;

    private int cardIndex;
    private Action<int> clickCallback;

    public void Bind(UICustomButton bindButton, Image bindBackground, Image bindIcon,
        TextMeshProUGUI bindPathTitleText, TextMeshProUGUI bindCardNameText,
        TextMeshProUGUI bindTypeText, TextMeshProUGUI bindDescText)
    {
        button = bindButton;
        background = bindBackground;
        icon = bindIcon;
        pathTitleText = bindPathTitleText;
        cardNameText = bindCardNameText;
        typeText = bindTypeText;
        descText = bindDescText;
    }

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

    public void Refresh(string pathTitle, EventCard card)
    {
        if (pathTitleText != null)
        {
            pathTitleText.text = pathTitle;
        }

        bool hasCard = card != null;
        if (cardNameText != null)
        {
            cardNameText.text = hasCard ? card.Name : "\u6682\u65e0\u4e8b\u4ef6";
        }

        if (typeText != null)
        {
            typeText.text = hasCard ? GetTypeName(card.CardType) : string.Empty;
        }

        if (descText != null)
        {
            descText.text = hasCard ? card.Desc : "\u5361\u6c60\u4e0d\u8db3";
        }

        if (background != null)
        {
            background.color = hasCard
                ? new Color(0.28f, 0.46f, 0.79f, 1f)
                : new Color(0.18f, 0.25f, 0.36f, 0.8f);
        }

        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(hasCard && !string.IsNullOrEmpty(card.SpriteName));
        if (hasCard && !string.IsNullOrEmpty(card.SpriteName))
        {
            ResourceManager.SetImageSprite(icon, card.SpriteName);
        }
    }

    private void OnClick()
    {
        clickCallback?.Invoke(cardIndex);
    }

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
