using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.YangUGUI;

public sealed class DiceBattleWindow : UGUIPanelBase<DiceBattleWindowData>
{
    private const int CoinPropId = 2;

    public TMP_FontAsset font;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI enemyHpText;
    [SerializeField] private TextMeshProUGUI diceText;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private UICustomButton rollButton;
    [SerializeField] private UICustomButton closeButton;

    private DiceBattleModel model;
    private bool resultHandled;

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        EnsureUI();

        if (windowData == null || windowData.BattleConfig == null)
        {
            FloatTipWindow.Show("战斗配置错误");
            CloseSelfPanel();
            return;
        }

        model = new DiceBattleModel(windowData.BattleConfig);
        resultHandled = false;
        rollButton.AddListener(OnRollClicked);
        closeButton.AddListener(OnCloseClicked);
        Refresh("点击掷骰开始战斗");
    }

    private void OnRollClicked()
    {
        if (model == null || resultHandled)
        {
            return;
        }

        string message = model.RollRound();
        Refresh(message);
        if (!model.IsFinished)
        {
            return;
        }

        resultHandled = true;
        if (model.IsPlayerWin)
        {
            if (model.CoinReward > 0)
            {
                BagMgr.Instance.AddBagProp(CoinPropId, model.CoinReward);
            }

            Refresh($"战斗胜利，金币+{model.CoinReward}");
        }
        else
        {
            Refresh("战斗失败");
        }

        SetRollInteractable(false);
    }

    private void OnCloseClicked()
    {
        bool isWin = model != null && model.IsPlayerWin;
        System.Action<bool> onBattleFinished = windowData?.OnBattleFinished;
        CloseSelfPanel();
        onBattleFinished?.Invoke(isWin);
    }

    private void Refresh(string message)
    {
        if (model == null)
        {
            return;
        }

        titleText.text = model.EnemyName;
        playerHpText.text = $"玩家 HP {model.PlayerHp}/{model.PlayerMaxHp}  攻击 {model.PlayerAttack}";
        enemyHpText.text = $"{model.EnemyName} HP {model.EnemyHp}/{model.EnemyMaxHp}  攻击 {model.EnemyAttack}";
        diceText.text = model.Round <= 0 ? $"D{model.DiceSides}" : $"你 {model.PlayerDice} : {model.EnemyDice} 敌";
        logText.text = message;
        SetRollInteractable(!model.IsFinished);
        closeButton.gameObject.SetActive(model.IsFinished);
    }

    private void SetRollInteractable(bool interactable)
    {
        if (rollButton != null && rollButton.TargetButton != null)
        {
            rollButton.TargetButton.interactable = interactable;
            rollButton.SetGray(!interactable);
        }
    }

    private void EnsureUI()
    {
        if (titleText != null)
        {
            return;
        }

        RectTransform root = transform as RectTransform;
        CreatePanel(root);
    }

    private void CreatePanel(RectTransform root)
    {
        GameObject mask = new GameObject("bgMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform maskRect = mask.GetComponent<RectTransform>();
        maskRect.SetParent(root, false);
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;
        Image maskImage = mask.GetComponent<Image>();
        maskImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject panel = new GameObject("WindowRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(root, false);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 520f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.96f);

        titleText = CreateText(panelRect, "Title", new Vector2(0f, 190f), new Vector2(640f, 56f), 40f);
        playerHpText = CreateText(panelRect, "PlayerHp", new Vector2(0f, 118f), new Vector2(640f, 40f), 28f);
        enemyHpText = CreateText(panelRect, "EnemyHp", new Vector2(0f, 70f), new Vector2(640f, 40f), 28f);
        diceText = CreateText(panelRect, "Dice", new Vector2(0f, 0f), new Vector2(640f, 70f), 46f);
        logText = CreateText(panelRect, "Log", new Vector2(0f, -84f), new Vector2(640f, 60f), 24f);
        rollButton = CreateButton(panelRect, "RollButton", "掷骰", new Vector2(-130f, -185f));
        closeButton = CreateButton(panelRect, "CloseButton", "结算", new Vector2(130f, -185f));
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string objectName, Vector2 position, Vector2 size,
        float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.font = font;
        text.color = Color.white;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private UICustomButton CreateButton(RectTransform parent, string objectName, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(UICustomButton));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(180f, 70f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.44f, 0.82f, 1f);
        UICustomButton button = buttonObject.GetComponent<UICustomButton>();
        button.btnImgList = new System.Collections.Generic.List<Image> {image};

        TextMeshProUGUI buttonText = CreateText(rect, "Text", Vector2.zero, new Vector2(160f, 48f), 30f);
        buttonText.text = label;
        return button;
    }
}
