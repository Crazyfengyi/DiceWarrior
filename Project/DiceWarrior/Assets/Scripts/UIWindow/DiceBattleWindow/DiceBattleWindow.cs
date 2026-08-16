using System;
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
    private const string ScenePrefabAddress = "3DScene";
    private const string PlayerEntityPrefabAddress = "BattlePlayerPrefab";
    private const string EnemyEntityPrefabAddress = "BattleEnemyPrefab";
    private const int SceneRenderTextureFallbackSize = 512;
    private const float BattleEntitySpacing = 3.2f;
    private const float EntityAttackMoveDuration = 0.22f;
    private const float EntityAttackHitDuration = 0.12f;
    private const float EntityAttackDistance = 1.1f;

    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Image enemyHpFill;
    [SerializeField] private TextMeshProUGUI enemyHpText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI enemyResultText;
    [SerializeField] private TextMeshProUGUI playerResultText;
    [SerializeField] private RectTransform uiEnemyPos;
    [SerializeField] private RectTransform uiPlayerPos;
    [SerializeField] private TextMeshProUGUI roundsText;
    [SerializeField] private TextMeshProUGUI playerTotalText;
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
    [SerializeField] private RectTransform battleLogPanelRoot;
    [SerializeField] private ScrollRect battleLogScrollRect;
    [SerializeField] private TextMeshProUGUI battleLogText;
    [SerializeField] private UICustomButton battleLogCloseButton;
    [SerializeField] private UICustomButton battleLogOpenButton;
    [SerializeField] private RawImage sceneRawImage;
    [SerializeField] private float enemyRevealDelay = 0.65f;
    [SerializeField] private List<DiceBattlePlayerDieItemUI> playerDieItems = new List<DiceBattlePlayerDieItemUI>();
    [SerializeField] private List<DiceBattleDieFaceCellUI> enemyDieItems = new List<DiceBattleDieFaceCellUI>();
    [SerializeField] private List<DiceBattleStatusItemUI> enemyStatusItems = new List<DiceBattleStatusItemUI>();

    private readonly List<string> battleLogEntries = new List<string>();

    private DiceBattleModel model;
    private bool initialized;
    private bool resultHandled;
    private bool isEnemyRevealPlaying;
    private bool battleEndLogged;
    private int hoveredPlayerDieIndex = -1;
    private GameObject sceneInstance;
    private Camera sceneCamera;
    private RenderTexture sceneRenderTexture;
    private GameObject playerEntityInstance;
    private GameObject enemyEntityInstance;
    private int sceneLoadVersion;

    /// <summary>
    /// 打开骰子战斗窗口并初始化显示。
    /// </summary>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        if (windowData == null || windowData.EnemyData == null)
        {
            FloatTipWindow.Show("战斗配置错误");
            CloseSelfPanel();
            return;
        }

        model = new DiceBattleModel(windowData.EnemyData, windowData.PlayerDiceSlots);
        resultHandled = false;
        hoveredPlayerDieIndex = -1;
        battleEndLogged = false;
        RegisterEventsIfNeeded();
        HideAllHoverPanels();
        ResetBattleLogState();
        AppendBattleStartLog();
        RefreshAll();
        Start3DScenePreview();
    }

    /// <summary>
    /// 关闭窗口时清理悬停面板状态。
    /// </summary>
    public override void OnClose(bool isShutdown, object userData)
    {
        sceneLoadVersion++;
        Cleanup3DScenePreview();
        base.OnClose(isShutdown, userData);
        HideAllHoverPanels();
        ResetBattleLogState();
    }

    /// <summary>
    /// 销毁战斗窗口时清理3D场景预览资源。
    /// </summary>
    private void OnDestroy()
    {
        sceneLoadVersion++;
        Cleanup3DScenePreview();
    }

    /// <summary>
    /// 开始创建战斗窗口的3D场景预览。
    /// </summary>
    private void Start3DScenePreview()
    {
        Cleanup3DScenePreview();
        int requestVersion = ++sceneLoadVersion;
        Create3DScenePreviewAsync(requestVersion).Forget();
    }

    /// <summary>
    /// 异步加载3D场景，并让相机对准战斗场景中心。
    /// </summary>
    private async UniTask Create3DScenePreviewAsync(int requestVersion)
    {
        if (sceneRawImage == null)
        {
            Debug.LogError("DiceBattleWindow SceneRawImage 未绑定。", this);
            return;
        }

        try
        {
            GameObject scenePrefab = await ResourceManager.LoadAssetAsync<GameObject>(ScenePrefabAddress);
            if (requestVersion != sceneLoadVersion || !isActiveAndEnabled || scenePrefab == null)
            {
                return;
            }

            sceneInstance = Instantiate(scenePrefab);
            if (requestVersion != sceneLoadVersion || !isActiveAndEnabled)
            {
                Cleanup3DScenePreview();
                return;
            }

            sceneCamera = sceneInstance.GetComponentInChildren<Camera>(true);
            if (sceneCamera == null)
            {
                Debug.LogError("3D场景预制体中未找到相机。", sceneInstance);
                Cleanup3DScenePreview();
                return;
            }

            DisableSceneAudioListeners();
            PositionSceneCameraAtBattlePanel();
            Vector2 renderSize = GetSceneRenderSize();
            sceneRenderTexture = new RenderTexture(Mathf.CeilToInt(renderSize.x), Mathf.CeilToInt(renderSize.y), 24,
                RenderTextureFormat.ARGB32)
            {
                name = "DiceBattleWindowSceneRenderTexture"
            };
            sceneRenderTexture.Create();
            sceneCamera.targetTexture = sceneRenderTexture;
            sceneCamera.enabled = true;
            sceneRawImage.texture = sceneRenderTexture;
            CreateBattleEntitiesAsync(requestVersion).Forget();
        }
        catch (Exception exception)
        {
            if (requestVersion == sceneLoadVersion)
            {
                Debug.LogException(exception, this);
                Cleanup3DScenePreview();
            }
        }
    }

    /// <summary>
    /// 禁用预览场景中的音频监听器，避免与战斗窗口重复监听。
    /// </summary>
    private void DisableSceneAudioListeners()
    {
        AudioListener[] audioListeners = sceneInstance.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < audioListeners.Length; i++)
        {
            audioListeners[i].enabled = false;
        }
    }

    /// <summary>
    /// 将预览相机平移到BattlePanel中心，并调整朝向。
    /// </summary>
    private void PositionSceneCameraAtBattlePanel()
    {
        Transform battlePanel = sceneInstance.transform.Find("BattlePanel");
        if (battlePanel == null)
        {
            Debug.LogWarning("3D场景预制体中未找到BattlePanel，相机保持原位置。", sceneInstance);
            return;
        }

        Renderer battlePanelRenderer = battlePanel.GetComponent<Renderer>();
        Vector3 targetPosition = battlePanelRenderer != null
            ? battlePanelRenderer.bounds.center
            : battlePanel.position;
        Vector3 cameraOffset = sceneCamera.transform.position - sceneInstance.transform.position;
        sceneCamera.transform.position = targetPosition + cameraOffset;
        sceneCamera.transform.LookAt(targetPosition);
    }

    /// <summary>
    /// 加载并创建玩家和敌人的3D实体。
    /// </summary>
    private async UniTask CreateBattleEntitiesAsync(int requestVersion)
    {
        GameObject playerPrefab;
        GameObject enemyPrefab;
        try
        {
            playerPrefab = await ResourceManager.LoadAssetAsync<GameObject>(PlayerEntityPrefabAddress);
            enemyPrefab = await ResourceManager.LoadAssetAsync<GameObject>(EnemyEntityPrefabAddress);
        }
        catch (Exception exception)
        {
            if (requestVersion == sceneLoadVersion)
            {
                Debug.LogException(exception, this);
            }

            return;
        }

        if (requestVersion != sceneLoadVersion || !isActiveAndEnabled || sceneInstance == null ||
            playerPrefab == null || enemyPrefab == null)
        {
            return;
        }

        Transform battlePanel = sceneInstance.transform.Find("BattlePanel");
        if (battlePanel == null)
        {
            Debug.LogError("3D场景预制体中未找到BattlePanel，无法创建战斗实体。", sceneInstance);
            return;
        }

        Renderer battlePanelRenderer = battlePanel.GetComponent<Renderer>();
        Vector3 panelCenter = battlePanelRenderer != null ? battlePanelRenderer.bounds.center : battlePanel.position;
        float panelTop = battlePanelRenderer != null ? battlePanelRenderer.bounds.max.y : panelCenter.y;
        playerEntityInstance = Instantiate(playerPrefab, sceneInstance.transform);
        enemyEntityInstance = Instantiate(enemyPrefab, sceneInstance.transform);
        playerEntityInstance.name = "BattlePlayerEntity";
        enemyEntityInstance.name = "BattleEnemyEntity";

        Vector3 fallbackEntityCenter = new Vector3(panelCenter.x, panelTop, panelCenter.z);
        Vector3 screenRight = sceneCamera != null ? sceneCamera.transform.right : Vector3.right;
        Vector3 fallbackPlayerPosition = fallbackEntityCenter - screenRight * BattleEntitySpacing;
        Vector3 fallbackEnemyPosition = fallbackEntityCenter + screenRight * BattleEntitySpacing;
        playerEntityInstance.transform.position = GetEntityPositionForUi(
            uiPlayerPos, fallbackPlayerPosition, panelTop);
        enemyEntityInstance.transform.position = GetEntityPositionForUi(
            uiEnemyPos, fallbackEnemyPosition, panelTop);

    }

    /// <summary>
    /// 将 UI 锚点位置转换为战斗地面上的 3D 位置。
    /// </summary>
    private Vector3 GetEntityPositionForUi(RectTransform uiAnchor, Vector3 fallbackPosition, float groundHeight)
    {
        if (uiAnchor == null || sceneCamera == null || sceneRawImage == null)
        {
            return fallbackPosition;
        }

        Canvas canvas = sceneRawImage.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, uiAnchor.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sceneRawImage.rectTransform, screenPosition, uiCamera, out Vector2 localPosition))
        {
            return fallbackPosition;
        }

        Rect rawImageRect = sceneRawImage.rectTransform.rect;
        if (rawImageRect.width <= 0f || rawImageRect.height <= 0f)
        {
            return fallbackPosition;
        }

        float viewportX = Mathf.Clamp01((localPosition.x - rawImageRect.x) / rawImageRect.width);
        float viewportY = Mathf.Clamp01((localPosition.y - rawImageRect.y) / rawImageRect.height);
        Ray ray = sceneCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
        if (Mathf.Abs(ray.direction.y) <= Mathf.Epsilon)
        {
            return fallbackPosition;
        }

        float distance = (groundHeight - ray.origin.y) / ray.direction.y;
        return distance > 0f ? ray.GetPoint(distance) : fallbackPosition;
    }

    /// <summary>
    /// 获取战斗窗口3D画面的渲染纹理尺寸。
    /// </summary>
    private Vector2 GetSceneRenderSize()
    {
        Rect rect = sceneRawImage.rectTransform.rect;
        int width = Mathf.CeilToInt(rect.width);
        int height = Mathf.CeilToInt(rect.height);
        if (width <= 0 || height <= 0)
        {
            width = SceneRenderTextureFallbackSize;
            height = SceneRenderTextureFallbackSize;
        }

        return new Vector2(width, height);
    }

    /// <summary>
    /// 清理战斗窗口的3D场景实例和RenderTexture。
    /// </summary>
    private void Cleanup3DScenePreview()
    {
        CleanupBattleEntities();
        if (sceneCamera != null)
        {
            sceneCamera.targetTexture = null;
        }

        if (sceneRawImage != null && sceneRawImage.texture == sceneRenderTexture)
        {
            sceneRawImage.texture = null;
        }

        if (sceneInstance != null)
        {
            Destroy(sceneInstance);
        }

        if (sceneRenderTexture != null)
        {
            sceneRenderTexture.Release();
            Destroy(sceneRenderTexture);
        }

        sceneInstance = null;
        sceneCamera = null;
        sceneRenderTexture = null;
    }

    /// <summary>
    /// 清理玩家和敌人的3D实体实例。
    /// </summary>
    private void CleanupBattleEntities()
    {
        if (playerEntityInstance != null)
        {
            Destroy(playerEntityInstance);
        }

        if (enemyEntityInstance != null)
        {
            Destroy(enemyEntityInstance);
        }

        playerEntityInstance = null;
        enemyEntityInstance = null;
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
        battleLogCloseButton?.AddListener(OnCloseBattleLogClicked);
        battleLogOpenButton?.AddListener(OnOpenBattleLogClicked);
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
        RefreshStatusItems();
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

        if (roundsText != null)
        {
            roundsText.text = $"回合数:{model.CurrentRound}";
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
                playerDieItems[i].Refresh(dieState, model.CanRerollSingleDie(i),
                    !model.IsFinished && !isEnemyRevealPlaying);
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
    private void RefreshStatusItems()
    {
        for (int i = 0; i < enemyStatusItems.Count; i++)
        {
            BuffData status = i < model.EnemyStatuses.Count ? model.EnemyStatuses[i] : null;
            Sprite sprite = null;
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
        EnemySkillData skill = model.CurrentSkill;
        if (skillNameText != null)
        {
            skillNameText.text = skill == null ? "当前技能" : skill.SkillName;
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
        bool allowInteraction = !isEnemyRevealPlaying;
        SetButtonState(throwAllButton, !model.IsFinished, allowInteraction && model.CanThrowAll);
        SetButtonState(rerollAllButton, model.CanRerollAll, allowInteraction && model.CanRerollAll);
        SetButtonState(endTurnButton, !model.IsFinished, allowInteraction && model.CanEndTurn);
        SetButtonState(settleButton, model.IsFinished, allowInteraction && model.IsFinished);
        SetButtonState(setButton, true, allowInteraction);
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
    private async void OnEndTurnClicked()
    {
        if (model == null || !model.RollEnemyTurn())
        {
            return;
        }

        isEnemyRevealPlaying = true;
        RefreshEnemyDiceItems();
        RefreshRoundTexts();
        RefreshActionButtons();
        AppendBattleLog($"<color=#FFE7A3>敌方总和：{model.EnemyCurrentResult}</color>");
        await UniTask.Delay((int) (enemyRevealDelay * 1000f));
        isEnemyRevealPlaying = false;

        if (!model.ResolveRoundAfterEnemyReveal())
        {
            RefreshAll();
            return;
        }

        AppendRoundResultLog();

        if (model.RoundWinner != DiceBattleModel.RoundWinnerType.Draw)
        {
            await PlayEntityAttackAsync(model.RoundWinner == DiceBattleModel.RoundWinnerType.Player);
        }

        if (model.IsFinished && model.IsPlayerWin && model.CoinReward > 0)
        {
            BagMgr.Instance.AddBagProp(CoinPropId, model.CoinReward);
        }

        if (model.IsFinished)
        {
            AppendBattleEndLog();
        }

        if (!model.IsFinished)
        {
            model.AdvanceAfterRoundResolution();
            AppendBattleLog($"<color=#D7DCEB>进入第 {model.CurrentRound} 回合</color>");
        }

        RefreshAll();
        if (model.IsFinished)
        {
            OnSettleClicked();
        }
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
    /// 关闭战斗日志面板。
    /// </summary>
    private void OnCloseBattleLogClicked()
    {
        SetBattleLogVisible(false);
    }

    /// <summary>
    /// 打开战斗日志面板。
    /// </summary>
    private void OnOpenBattleLogClicked()
    {
        SetBattleLogVisible(true);
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

        BuffData status = model.EnemyStatuses[statusIndex];
        if (statusHoverTitleText != null)
        {
            statusHoverTitleText.text = status.BuffName;
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

        EnemySkillData skill = model.CurrentSkill;
        if (skillHoverTitleText != null)
        {
            skillHoverTitleText.text = skill == null ? "当前技能" : skill.SkillName;
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
    /// 追加一条战斗日志并刷新显示。
    /// </summary>
    private void AppendBattleLog(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        battleLogEntries.Add(message);
        RefreshBattleLogView();
        ScrollBattleLogToBottom().Forget();
    }

    /// <summary>
    /// 追加战斗开始日志。
    /// </summary>
    private void AppendBattleStartLog()
    {
        AppendBattleLog($"<color=#F3F5FA>战斗开始：玩家 VS {model?.EnemyName ?? "敌人"}</color>");
    }

    /// <summary>
    /// 追加当前回合的结算日志。
    /// </summary>
    private void AppendRoundResultLog()
    {
        if (model == null)
        {
            return;
        }

        switch (model.RoundWinner)
        {
            case DiceBattleModel.RoundWinnerType.Player:
                AppendBattleLog($"<color=#7EE6FF>{model.RoundResolvedMessage}</color>");
                break;
            case DiceBattleModel.RoundWinnerType.Enemy:
                AppendBattleLog($"<color=#FF8A8A>{model.RoundResolvedMessage}</color>");
                break;
            default:
                AppendBattleLog($"<color=#F3F5FA>{model.RoundResolvedMessage}</color>");
                break;
        }
    }

    /// <summary>
    /// 追加战斗结束日志。
    /// </summary>
    private void AppendBattleEndLog()
    {
        if (model == null || battleEndLogged)
        {
            return;
        }

        battleEndLogged = true;
        if (model.IsPlayerWin)
        {
            AppendBattleLog($"<color=#7EE6FF>战斗胜利，获得金币 {model.CoinReward}</color>");
            return;
        }

        AppendBattleLog("<color=#FF8A8A>战斗失败</color>");
    }

    /// <summary>
    /// 刷新战斗日志文本显示。
    /// </summary>
    private void RefreshBattleLogView()
    {
        if (battleLogText == null)
        {
            return;
        }

        battleLogText.text = string.Join("\n", battleLogEntries);
        RefreshBattleLogLayout();
    }

    /// <summary>
    /// 根据日志文本的实际高度更新滚动内容区域。
    /// </summary>
    private void RefreshBattleLogLayout()
    {
        if (battleLogText == null)
        {
            return;
        }

        battleLogText.ForceMeshUpdate();
        RectTransform textRect = battleLogText.rectTransform;
        float preferredHeight = battleLogText.preferredHeight;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

        if (battleLogScrollRect == null || battleLogScrollRect.content == null)
        {
            return;
        }

        RectTransform viewport = battleLogScrollRect.viewport;
        float viewportHeight = viewport == null ? 0f : viewport.rect.height;
        float contentHeight = Mathf.Max(viewportHeight, preferredHeight);
        battleLogScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(battleLogScrollRect.content);
    }

    /// <summary>
    /// 将战斗日志滚动到底部。
    /// </summary>
    private async UniTaskVoid ScrollBattleLogToBottom()
    {
        if (battleLogScrollRect == null)
        {
            return;
        }

        await UniTask.Yield();
        Canvas.ForceUpdateCanvases();
        battleLogScrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// 重置战斗日志的运行时状态。
    /// </summary>
    private void ResetBattleLogState()
    {
        battleLogEntries.Clear();
        if (battleLogText != null)
        {
            battleLogText.text = string.Empty;
        }

        if (battleLogScrollRect != null)
        {
            battleLogScrollRect.verticalNormalizedPosition = 1f;
        }

        SetBattleLogVisible(false);
    }

    /// <summary>
    /// 切换战斗日志面板显示状态。
    /// </summary>
    private void SetBattleLogVisible(bool visible)
    {
        if (battleLogPanelRoot != null)
        {
            battleLogPanelRoot.gameObject.SetActive(visible);
        }
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
    /// 播放一轮3D攻击动作：攻击者移动到目标前方，目标受击后攻击者返回原位。
    /// </summary>
    private async UniTask PlayEntityAttackAsync(bool isPlayerAttack)
    {
        GameObject attacker = isPlayerAttack ? playerEntityInstance : enemyEntityInstance;
        GameObject target = isPlayerAttack ? enemyEntityInstance : playerEntityInstance;
        bool isEnemyTarget = isPlayerAttack;
        if (attacker == null || target == null)
        {
            ShowDamageFloatTip(isEnemyTarget, model.RoundDamage);
            await PlayEntityHitFlashAsync(isEnemyTarget);
            return;
        }

        Transform attackerTransform = attacker.transform;
        Transform targetTransform = target.transform;
        Vector3 startPosition = attackerTransform.position;
        Quaternion startRotation = attackerTransform.rotation;
        Vector3 direction = targetTransform.position - startPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();
            attackerTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        Vector3 attackPosition = targetTransform.position - direction * EntityAttackDistance;
        attackPosition.y = startPosition.y;
        await MoveEntityAsync(attackerTransform, startPosition, attackPosition, EntityAttackMoveDuration);
        if (attacker == null || target == null)
        {
            return;
        }

        ShowDamageFloatTip(isEnemyTarget, model.RoundDamage);
        await PlayEntityHitFlashAsync(isEnemyTarget);
        await UniTask.Delay((int) (EntityAttackHitDuration * 1000f));
        await MoveEntityAsync(attackerTransform, attackPosition, startPosition, EntityAttackMoveDuration);
        if (attacker != null)
        {
            attackerTransform.rotation = startRotation;
        }
    }

    /// <summary>
    /// 平滑移动3D实体，并在窗口关闭后安全结束异步动作。
    /// </summary>
    private static async UniTask MoveEntityAsync(Transform entityTransform, Vector3 startPosition,
        Vector3 targetPosition, float duration)
    {
        if (entityTransform == null || duration <= 0f)
        {
            if (entityTransform != null)
            {
                entityTransform.position = targetPosition;
            }

            return;
        }

        float elapsed = 0f;
        while (elapsed < duration && entityTransform != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            entityTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
            await UniTask.Yield();
        }

        if (entityTransform != null)
        {
            entityTransform.position = targetPosition;
        }
    }

    /// <summary>
    /// 播放玩家或敌人的3D受击闪烁。
    /// </summary>
    private async UniTask PlayEntityHitFlashAsync(bool isEnemyTarget)
    {
        GameObject targetEntity = isEnemyTarget ? enemyEntityInstance : playerEntityInstance;
        if (targetEntity == null)
        {
            return;
        }

        Renderer[] renderers = targetEntity.GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        SetEntityFlashColor(renderers, propertyBlock, Color.white);
        await UniTask.Delay(70);
        if (targetEntity == null)
        {
            return;
        }

        ClearEntityFlashColor(renderers);
        await UniTask.Delay(55);
        if (targetEntity == null)
        {
            return;
        }

        SetEntityFlashColor(renderers, propertyBlock, Color.white);
        await UniTask.Delay(70);
        ClearEntityFlashColor(renderers);
    }

    /// <summary>
    /// 设置实体所有Renderer的临时闪烁颜色。
    /// </summary>
    private static void SetEntityFlashColor(Renderer[] renderers, MaterialPropertyBlock propertyBlock, Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>
    /// 清除实体Renderer的临时闪烁颜色覆盖。
    /// </summary>
    private static void ClearEntityFlashColor(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].SetPropertyBlock(null);
            }
        }
    }

    /// <summary>
    /// 使用通用飘字窗口在受伤实体的屏幕位置显示伤害。
    /// </summary>
    private void ShowDamageFloatTip(bool isEnemyTarget, int damage)
    {
        GameObject targetEntity = isEnemyTarget ? enemyEntityInstance : playerEntityInstance;
        if (targetEntity == null || sceneCamera == null || sceneRawImage == null)
        {
            FloatTipWindow.Show($"-{Mathf.Max(0, damage)}");
            return;
        }

        Renderer targetRenderer = targetEntity.GetComponentInChildren<Renderer>();
        Vector3 worldPosition = targetRenderer != null
            ? targetRenderer.bounds.center + Vector3.up * 0.6f
            : targetEntity.transform.position + Vector3.up * 1.5f;
        Vector3 viewportPosition = sceneCamera.WorldToViewportPoint(worldPosition);
        if (viewportPosition.z <= 0f)
        {
            FloatTipWindow.Show($"-{Mathf.Max(0, damage)}");
            return;
        }

        Rect rawImageRect = sceneRawImage.rectTransform.rect;
        Vector2 localPosition = new Vector2(rawImageRect.x + viewportPosition.x * rawImageRect.width,
            rawImageRect.y + viewportPosition.y * rawImageRect.height);
        Canvas canvas = sceneRawImage.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera,
            sceneRawImage.rectTransform.TransformPoint(localPosition));
        FloatTipWindow.Show($"-{Mathf.Max(0, damage)}", screenPosition, new Color(1f, 0.35f, 0.35f, 1f));
    }

}
