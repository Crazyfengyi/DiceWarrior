using System.Collections.Generic;
using System.Text;
using cfg;
using Cysharp.Threading.Tasks;
using GameMain;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.ResourceManager;
using YangTools.Scripts.Core.YangUGUI;

public sealed class DiceBattleWindow : UGUIPanelBase<DiceBattleWindowData>
{
    private const int CoinPropId = 2;

    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Image enemyHpFill;
    [SerializeField] private TextMeshProUGUI enemyHpText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private Image enemyImage;
    [SerializeField] private TextMeshProUGUI enemyResultText;
    [SerializeField] private TextMeshProUGUI playerResultText;
    [SerializeField] private TextMeshProUGUI playerTotalText;
    [SerializeField] private TextMeshProUGUI roundSummaryText;
    [SerializeField] private TextMeshProUGUI probabilityTitleText;
    [SerializeField] private TextMeshProUGUI probabilityRangeText;
    [SerializeField] private TextMeshProUGUI probabilityDetailText;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescText;
    [SerializeField] private TextMeshProUGUI statusHoverTitleText;
    [SerializeField] private TextMeshProUGUI statusHoverDescText;
    [SerializeField] private TextMeshProUGUI skillHoverTitleText;
    [SerializeField] private TextMeshProUGUI skillHoverDescText;
    [SerializeField] private RectTransform probabilityPanelRoot;
    [SerializeField] private RectTransform statusHoverPanelRoot;
    [SerializeField] private RectTransform skillHoverPanelRoot;
    [SerializeField] private UICustomButton throwAllButton;
    [SerializeField] private UICustomButton rerollAllButton;
    [SerializeField] private UICustomButton endTurnButton;
    [SerializeField] private UICustomButton settleButton;
    [SerializeField] private UICustomButton setButton;
    [SerializeField] private DiceBattleHoverTargetUI skillHoverTarget;
    [SerializeField] private List<DiceBattlePlayerDieItemUI> playerDieItems = new List<DiceBattlePlayerDieItemUI>();
    [SerializeField] private List<DiceBattleDieFaceCellUI> enemyDieItems = new List<DiceBattleDieFaceCellUI>();
    [SerializeField] private List<DiceBattleStatusItemUI> enemyStatusItems = new List<DiceBattleStatusItemUI>();

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private DiceBattleModel model;
    private bool initialized;
    private bool resultHandled;
    private int hoveredPlayerDieIndex = -1;

    /// <summary>
    /// 打开骰子战斗窗口并初始化显示。
    /// </summary>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ValidateBindings();

        if (windowData == null || windowData.BattleConfig == null)
        {
            FloatTipWindow.Show("战斗配置错误");
            CloseSelfPanel();
            return;
        }

        model = new DiceBattleModel(windowData.BattleConfig, windowData.PlayerDiceSlots);
        resultHandled = false;
        hoveredPlayerDieIndex = -1;
        RegisterEventsIfNeeded();
        HideAllHoverPanels();
        RefreshAll();
        RefreshEnemyVisualsAsync().Forget();
    }

    /// <summary>
    /// 关闭窗口时清理悬停面板状态。
    /// </summary>
    public override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        HideAllHoverPanels();
    }

    /// <summary>
    /// 注册界面上的交互事件。
    /// </summary>
    private void RegisterEventsIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        throwAllButton?.AddListener(OnThrowAllClicked);
        rerollAllButton?.AddListener(OnRerollAllClicked);
        endTurnButton?.AddListener(OnEndTurnClicked);
        settleButton?.AddListener(OnSettleClicked);
        setButton?.AddListener(OnSettingClicked);
        skillHoverTarget?.Init(ShowSkillHoverPanel, HideSkillHoverPanel);

        for (int i = 0; i < playerDieItems.Count; i++)
        {
            if (playerDieItems[i] != null)
            {
                playerDieItems[i].Init(i, OnThrowDieClicked, OnRerollDieClicked, ShowProbabilityPanel, HideProbabilityPanel);
            }
        }

        for (int i = 0; i < enemyStatusItems.Count; i++)
        {
            int statusIndex = i;
            if (enemyStatusItems[i] != null)
            {
                enemyStatusItems[i].Init(() => ShowStatusHoverPanel(statusIndex), HideStatusHoverPanel);
            }
        }

        initialized = true;
    }

    /// <summary>
    /// 刷新整页显示。
    /// </summary>
    private void RefreshAll()
    {
        if (model == null)
        {
            return;
        }

        RefreshRoundTexts();
        RefreshEnemyHp();
        RefreshPlayerDiceItems();
        RefreshEnemyDiceItems();
        RefreshStatusItemsAsync().Forget();
        RefreshCurrentSkill();
        RefreshActionButtons();
        RefreshProbabilityPanelByState();
    }

    /// <summary>
    /// 刷新回合和结果文案。
    /// </summary>
    private void RefreshRoundTexts()
    {
        if (roundText != null)
        {
            roundText.text = $"第 {model.CurrentRound} 回合";
        }

        if (enemyNameText != null)
        {
            enemyNameText.text = model.EnemyName;
        }

        if (enemyResultText != null)
        {
            enemyResultText.text = $"当前结果：{model.EnemyCurrentResult}";
        }

        if (playerResultText != null)
        {
            playerResultText.text = $"当前结果：{model.PlayerCurrentResult}";
        }

        if (playerTotalText != null)
        {
            playerTotalText.text =
                $"单骰重投 {model.RemainingSingleDieRerolls}/1    全部重投 {model.RemainingAllDiceRerolls}/1";
        }

        if (roundSummaryText != null)
        {
            roundSummaryText.text = model.IsFinished && model.IsPlayerWin
                ? $"{model.LastMessage}\n战斗胜利，获得金币 {model.CoinReward}"
                : model.LastMessage;
        }
    }

    /// <summary>
    /// 刷新敌人血量区域。
    /// </summary>
    private void RefreshEnemyHp()
    {
        if (enemyHpFill != null)
        {
            enemyHpFill.fillAmount = model.EnemyMaxHp <= 0 ? 0f : (float) model.EnemyHp / model.EnemyMaxHp;
        }

        if (enemyHpText != null)
        {
            enemyHpText.text = $"{model.EnemyHp}/{model.EnemyMaxHp}";
        }

        if (playerHpText != null)
        {
            playerHpText.text = $"玩家生命：{model.PlayerHp}/{model.PlayerMaxHp}";
        }
    }

    /// <summary>
    /// 刷新左侧玩家骰子条目。
    /// </summary>
    private void RefreshPlayerDiceItems()
    {
        for (int i = 0; i < playerDieItems.Count; i++)
        {
            DiceBattleModel.PlayerDieState dieState =
                i < model.PlayerDiceStates.Count ? model.PlayerDiceStates[i] : null;
            if (playerDieItems[i] != null)
            {
                playerDieItems[i].Refresh(dieState, model.CanRerollSingleDie(i), !model.IsFinished);
            }
        }
    }

    /// <summary>
    /// 刷新敌方骰子显示。
    /// </summary>
    private void RefreshEnemyDiceItems()
    {
        for (int i = 0; i < enemyDieItems.Count; i++)
        {
            bool visible = i < model.EnemyDiceStates.Count;
            int value = visible ? model.EnemyDiceStates[i].CurrentRoll : 0;
            if (enemyDieItems[i] != null)
            {
                enemyDieItems[i].RefreshEnemy(value, visible);
            }
        }
    }

    /// <summary>
    /// 刷新敌方状态图标。
    /// </summary>
    private async UniTaskVoid RefreshStatusItemsAsync()
    {
        for (int i = 0; i < enemyStatusItems.Count; i++)
        {
            DiceBattleEnemyStatusConfig status = i < model.EnemyStatuses.Count ? model.EnemyStatuses[i] : null;
            Sprite sprite = status == null ? null : await LoadSpriteSafe(status.IconSpriteName);
            if (enemyStatusItems[i] != null)
            {
                enemyStatusItems[i].Refresh(sprite, status != null);
            }
        }
    }

    /// <summary>
    /// 刷新当前技能卡片显示。
    /// </summary>
    private void RefreshCurrentSkill()
    {
        DiceBattleEnemySkillConfig skill = model.CurrentSkill;
        if (skillNameText != null)
        {
            skillNameText.text = skill == null ? "当前技能" : skill.Name;
        }

        if (skillDescText != null)
        {
            skillDescText.text = skill == null ? "暂无技能说明" : skill.Desc;
        }
    }

    /// <summary>
    /// 刷新底部操作按钮状态。
    /// </summary>
    private void RefreshActionButtons()
    {
        SetButtonState(throwAllButton, !model.IsFinished, model.CanThrowAll);
        SetButtonState(rerollAllButton, model.CanRerollAll, model.CanRerollAll);
        SetButtonState(endTurnButton, !model.IsFinished, model.CanEndTurn);
        SetButtonState(settleButton, model.IsFinished, model.IsFinished);
    }

    /// <summary>
    /// 点击单颗骰子的投出按钮。
    /// </summary>
    private void OnThrowDieClicked(int dieIndex)
    {
        if (model == null || !model.ThrowPlayerDie(dieIndex))
        {
            return;
        }

        RefreshAll();
    }

    /// <summary>
    /// 点击单颗骰子的重投按钮。
    /// </summary>
    private void OnRerollDieClicked(int dieIndex)
    {
        if (model == null || !model.RerollPlayerDie(dieIndex))
        {
            return;
        }

        RefreshAll();
    }

    /// <summary>
    /// 点击全部投出按钮。
    /// </summary>
    private void OnThrowAllClicked()
    {
        if (model == null || !model.ThrowAllPlayerDice())
        {
            return;
        }

        RefreshAll();
    }

    /// <summary>
    /// 点击全部重投按钮。
    /// </summary>
    private void OnRerollAllClicked()
    {
        if (model == null || !model.RerollAllPlayerDice())
        {
            return;
        }

        RefreshAll();
    }

    /// <summary>
    /// 点击结束行动按钮。
    /// </summary>
    private void OnEndTurnClicked()
    {
        if (model == null || !model.EndPlayerTurn())
        {
            return;
        }

        if (model.IsFinished && model.IsPlayerWin && model.CoinReward > 0)
        {
            BagMgr.Instance.AddBagProp(CoinPropId, model.CoinReward);
        }

        RefreshAll();
    }

    /// <summary>
    /// 点击战斗结算按钮。
    /// </summary>
    private void OnSettleClicked()
    {
        if (resultHandled)
        {
            return;
        }

        resultHandled = true;
        bool isWin = model != null && model.IsPlayerWin;
        System.Action<bool> onBattleFinished = windowData?.OnBattleFinished;
        CloseSelfPanel();
        onBattleFinished?.Invoke(isWin);
    }

    /// <summary>
    /// 点击设置按钮。
    /// </summary>
    private async void OnSettingClicked()
    {
        await UIMonoInstance.OpenPanel<SettingWindow>(GroupType.弹窗2);
    }

    /// <summary>
    /// 显示单颗骰子的概率面板。
    /// </summary>
    private void ShowProbabilityPanel(int dieIndex)
    {
        hoveredPlayerDieIndex = dieIndex;
        RefreshProbabilityPanelByState();
    }

    /// <summary>
    /// 隐藏概率面板。
    /// </summary>
    private void HideProbabilityPanel()
    {
        hoveredPlayerDieIndex = -1;
        RefreshProbabilityPanelByState();
    }

    /// <summary>
    /// 根据当前悬停状态刷新概率面板。
    /// </summary>
    private void RefreshProbabilityPanelByState()
    {
        if (probabilityPanelRoot == null)
        {
            return;
        }

        if (hoveredPlayerDieIndex < 0 || hoveredPlayerDieIndex >= model.PlayerDiceStates.Count)
        {
            probabilityPanelRoot.gameObject.SetActive(false);
            return;
        }

        DiceBattleModel.PlayerDieState dieState = model.PlayerDiceStates[hoveredPlayerDieIndex];
        if (dieState == null || dieState.IsEmpty)
        {
            probabilityPanelRoot.gameObject.SetActive(false);
            return;
        }

        if (probabilityTitleText != null)
        {
            probabilityTitleText.text = $"{dieState.DieName} 极限区间";
        }

        if (probabilityRangeText != null)
        {
            probabilityRangeText.text = dieState.GetRangeText();
        }

        if (probabilityDetailText != null)
        {
            probabilityDetailText.text = BuildProbabilityText(dieState.Faces);
        }

        probabilityPanelRoot.gameObject.SetActive(true);
    }

    /// <summary>
    /// 显示状态说明面板。
    /// </summary>
    private void ShowStatusHoverPanel(int statusIndex)
    {
        if (statusHoverPanelRoot == null || statusIndex < 0 || statusIndex >= model.EnemyStatuses.Count)
        {
            return;
        }

        DiceBattleEnemyStatusConfig status = model.EnemyStatuses[statusIndex];
        if (statusHoverTitleText != null)
        {
            statusHoverTitleText.text = status.Name;
        }

        if (statusHoverDescText != null)
        {
            statusHoverDescText.text = status.Desc;
        }

        statusHoverPanelRoot.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏状态说明面板。
    /// </summary>
    private void HideStatusHoverPanel()
    {
        if (statusHoverPanelRoot != null)
        {
            statusHoverPanelRoot.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示技能说明面板。
    /// </summary>
    private void ShowSkillHoverPanel()
    {
        if (skillHoverPanelRoot == null)
        {
            return;
        }

        DiceBattleEnemySkillConfig skill = model.CurrentSkill;
        if (skillHoverTitleText != null)
        {
            skillHoverTitleText.text = skill == null ? "当前技能" : skill.Name;
        }

        if (skillHoverDescText != null)
        {
            skillHoverDescText.text = skill == null ? "暂无技能说明" : skill.Desc;
        }

        skillHoverPanelRoot.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏技能说明面板。
    /// </summary>
    private void HideSkillHoverPanel()
    {
        if (skillHoverPanelRoot != null)
        {
            skillHoverPanelRoot.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 隐藏所有悬停说明面板。
    /// </summary>
    private void HideAllHoverPanels()
    {
        probabilityPanelRoot?.gameObject.SetActive(false);
        statusHoverPanelRoot?.gameObject.SetActive(false);
        skillHoverPanelRoot?.gameObject.SetActive(false);
    }

    /// <summary>
    /// 异步刷新敌人图片。
    /// </summary>
    private async UniTaskVoid RefreshEnemyVisualsAsync()
    {
        if (enemyImage == null)
        {
            return;
        }

        Sprite sprite = await LoadSpriteSafe(model.EnemySpriteName);
        if (enemyImage == null)
        {
            return;
        }

        enemyImage.sprite = sprite;
        enemyImage.color = sprite == null ? new Color(0.26f, 0.43f, 0.76f, 1f) : Color.white;
    }

    /// <summary>
    /// 安全加载精灵资源。
    /// </summary>
    private async UniTask<Sprite> LoadSpriteSafe(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return null;
        }

        if (spriteCache.TryGetValue(spriteName, out Sprite sprite))
        {
            return sprite;
        }

        sprite = await ResourceManager.LoadAssetAsync<Sprite>(spriteName);
        spriteCache[spriteName] = sprite;
        return sprite;
    }

    /// <summary>
    /// 计算概率面板的文案。
    /// </summary>
    private static string BuildProbabilityText(IReadOnlyList<int> faces)
    {
        if (faces == null || faces.Count == 0)
        {
            return "暂无概率";
        }

        Dictionary<int, int> countMap = new Dictionary<int, int>();
        for (int i = 0; i < faces.Count; i++)
        {
            countMap.TryGetValue(faces[i], out int count);
            countMap[faces[i]] = count + 1;
        }

        List<int> values = new List<int>(countMap.Keys);
        values.Sort();
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < values.Count; i++)
        {
            int value = values[i];
            float percent = countMap[value] * 100f / faces.Count;
            builder.Append(value).Append("  ").Append(percent.ToString("0.#")).Append('%');
            if (i < values.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 设置按钮显示和可点击状态。
    /// </summary>
    private static void SetButtonState(UICustomButton button, bool visible, bool interactable)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(visible);
        if (!visible || button.TargetButton == null)
        {
            return;
        }

        button.TargetButton.interactable = interactable;
        button.SetGray(!interactable);
    }

    /// <summary>
    /// 校验 prefab 绑定是否完整。
    /// </summary>
    private void ValidateBindings()
    {
        if (roundText == null || enemyNameText == null || enemyHpFill == null || enemyHpText == null ||
            playerHpText == null || enemyImage == null || enemyResultText == null || playerResultText == null ||
            playerTotalText == null || roundSummaryText == null || probabilityTitleText == null ||
            probabilityRangeText == null || probabilityDetailText == null || skillNameText == null ||
            skillDescText == null || statusHoverTitleText == null || statusHoverDescText == null ||
            skillHoverTitleText == null || skillHoverDescText == null || probabilityPanelRoot == null ||
            statusHoverPanelRoot == null || skillHoverPanelRoot == null || throwAllButton == null ||
            rerollAllButton == null || endTurnButton == null || settleButton == null || setButton == null ||
            skillHoverTarget == null)
        {
            Debug.LogError("DiceBattleWindow 预制体引用未绑定完整", this);
        }

        if (playerDieItems.Count == 0 || enemyDieItems.Count == 0 || enemyStatusItems.Count == 0)
        {
            Debug.LogError("DiceBattleWindow 列表型预制体引用未绑定完整", this);
        }
    }
}
