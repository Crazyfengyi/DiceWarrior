using System;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using cfg;
using Cysharp.Threading.Tasks;
using GameMain;
using Manager;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools;
using YangTools.Scripts.Core.ResourceManager;
using YangTools.Scripts.Core.YangAudio;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;
using EventData = YangTools.EventData;
using Sequence = DG.Tweening.Sequence;

/// <summary>
/// 游戏窗口类，继承自基础UI面板类，用于管理游戏中的UI元素和交互逻辑
/// </summary>
public class GameWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    // 常量定义 - 物品ID和动画参数
    private const int MoneyPropId = 1; // 金钱道具ID
    private const int CoinPropId = 2; // 金币道具ID
    private const int MoneyFlyIconCount = 8; // 金币飞行图标数量
    private const float MoneyFlySpawnRadius = 120f; // 金币飞行生成半径
    private const float MoneyFlyScatterDuration = 0.14f; // 金币散射持续时间
    private const float MoneyFlyWaitDuration = 0.08f; // 金币等待持续时间
    private const float MoneyFlyDuration = 0.42f; // 金币飞行持续时间
    private const float MoneyFlyDelayStep = 0.035f; // 金币飞行延迟步长
    private const float MoneyFlyIconFallbackSize = 72f; // 金币图标默认大小

    private const float EventCardDiscardDuration = 0.4f; // 弃牌动画时长
    private const float EventCardDrawDuration = 0.6f; // 抽牌动画时长
    private const float EventCardDrawStartScale = 0.08f; // 抽牌起始缩放
    private const float EventCardDrawStartRotation = -18f; // 抽牌起始旋转角度
    private const float CardSelectionScale = 1.35f; // 选择卡牌的放大比例
    private const float CardSelectionButtonWidth = 220f; // 选择按钮宽度
    private const float CardSelectionButtonHeight = 78f; // 选择按钮高度
    private const string ScenePrefabAddress = "3DScene"; // 3D场景预制体地址
    private const string EventRewardItemPrefabAddress = "ItemPrefab"; // 事件奖励物体预制体地址
    private const int SceneRenderTextureFallbackSize = 512; // 3D场景渲染纹理默认尺寸
    private const float EventRewardItemSpacing = 1.4f; // 奖励物体之间的间距

    // 公共UI元素
    public ItemUI_BagProp moneyProp; // 金钱道具UI
    public ItemUI_BagProp goldProp; // 金币道具UI
    public UICustomButton setBtn; // 设置按钮
    public Image bar; // 进度条
    public TextMeshProUGUI barText; // 进度条文本
    public RectTransform barEffectParent; // 进度条效果父对象
    public RectTransform barEffect; // 进度条效果
    public RectTransform moneyTipsNode; // 金钱提示节点
    public TextMeshProUGUI moneyTipsText; // 金钱提示文本
    public SkeletonGraphic startAni; // 开始动画
    public TextMeshProUGUI startAniText; // 开始动画文本
    public GameObject gmNode; // GM模式节点
    public TMP_Dropdown levelDropdown; // 关卡下拉框
    public List<ItemUI_UseBagProp> useBagPropsBtns; // 使用道具按钮列表

    // 序列化私有字段
    [SerializeField] private UICustomButton GMButton; // GM按钮
    [SerializeField] private Transform contentRoot; // 内容根节点
    [SerializeField] private UICustomButton okButton; // 确认按钮
    [SerializeField] private UICustomButton clearButton; // 清除按钮
    [SerializeField] private UICustomButton addButton; // 添加按钮
    [SerializeField] private UICustomButton jumpButton; // 跳转按钮
    [SerializeField] private RectTransform moneyFlyEffectRoot; // 金币飞行效果根节点
    [SerializeField] private RectTransform eventCardRoot; // 事件卡片根节点
    [SerializeField] private List<EventCardItemUI> eventCardItems = new List<EventCardItemUI>(); // 事件卡片项列表
    [SerializeField] private RectTransform routeHudRoot; // 路线HUD根节点

    // 私有字段
    private EventInfo pressChangeListener; // 按键变化监听器
    private EventInfo bagPropChangeListener; // 道具变化监听器
    private EventInfo gameStartListener; // 游戏开始监听器
    private Sequence moneyFlySequence; // 金币飞行动画序列
    private ItemUI_BagProp moneyFlyTargetProp; // 金币飞行目标道具
    private int pendingCoinFlyEventSuppressCount; // 待处理的金币飞行事件抑制计数
    private readonly List<GameObject> moneyFlyIcons = new List<GameObject>(); // 金币飞行图标列表
    [SerializeField] private GameRoot gameRoot; // 游戏根节点
    private IReadOnlyList<EventCard> pendingEventCards = Array.Empty<EventCard>(); // 待处理的事件卡片
    private bool eventCardItemsCreating; // 事件卡片项创建中标志

    [SerializeField]
    private List<EquippedDiceSlotUI> equippedDiceSlotItems = new List<EquippedDiceSlotUI>(); // 装备骰子槽位项列表

    [SerializeField] private List<PathCardUI> pathCardItems = new List<PathCardUI>(); // 路径卡片项列表
    [SerializeField] private PileCounterUI discardPileUI; // 弃牌堆计数器UI
    [SerializeField] private PileCounterUI drawPileUI; // 抽牌堆计数器UI
    [SerializeField] private DiceEquipmentPanelUI diceEquipmentPanelUI; // 骰子装备面板UI
    [SerializeField] private RectTransform hoverTipRoot; // 悬浮提示根节点
    [SerializeField] private TextMeshProUGUI hoverTipText; // 悬浮提示文本
    [SerializeField] private Image hpFillImage; // 生命值填充图片
    [SerializeField] private TextMeshProUGUI hpText; // 生命值文本
    [SerializeField] private TextMeshProUGUI levelText; // 关卡标题文本
    [SerializeField] private RawImage sceneRawImage; // 3D场景显示区域
    [SerializeField] private UICustomButton leaveRewardButton; // 离开奖励按钮
    private bool routeHudValidated; // 路线HUD验证标志
    private Sequence eventCardTransitionSequence;
    private readonly List<GameObject> eventCardAnimationObjects = new List<GameObject>();
    private Sequence cardSelectionSequence;
    private GameObject cardSelectionPreviewObject;
    private PathCardUI cardSelectionSource;
    private int cardSelectionIndex = -1;
    private int cardSelectionParentSiblingIndex = -1;
    private bool cardSelectionClosing;
    private GameObject sceneInstance;
    private Camera sceneCamera;
    private RenderTexture sceneRenderTexture;
    private int sceneLoadVersion;
    private readonly List<EventRewardItem> eventRewardItems = new List<EventRewardItem>();
    private IReadOnlyList<RewardItemData> pendingEventRewards;
    private Action eventRewardLeaveCallback;
    private bool isShowingEventRewards;
    private int eventRewardLoadVersion;
    private bool leaveRewardButtonInitialized;

    public bool IsEventCardTransitionPlaying =>
        eventCardTransitionSequence != null && eventCardTransitionSequence.IsActive();

    /// <summary>
    /// 预加载游戏窗口使用的3D场景预制体。
    /// </summary>
    public static async UniTask Preload3DSceneAsync()
    {
        try
        {
            await ResourceManager.LoadAssetAsync<GameObject>(ScenePrefabAddress);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// 析构函数，停止金币飞行效果
    /// </summary>
    private void OnDestroy()
    {
        StopMoneyFlyEffect();
        StopEventCardTransition();
        CleanupEventRewards(false);
        sceneLoadVersion++;
        Cleanup3DScenePreview();
    }

    /// <summary>
    /// 清除按钮点击事件处理
    /// </summary>
    private void ClearBtn_OnClick()
    {
        YangSaveDataManager.Instance.ClearSaveData();
        FloatTipWindow.Show("xxx");
    }

    /// <summary>
    /// 添加按钮点击事件处理函数
    /// </summary>
    private void AddBtn_OnClick()
    {
        // 向背包中添加3种道具，每种1000个
        BagMgr.Instance.AddBagProp(3, 1000);
        BagMgr.Instance.AddBagProp(4, 1000);
        BagMgr.Instance.AddBagProp(5, 1000);
    }

    /// <summary>
    /// 金币按钮点击事件处理函数
    /// </summary>
    private void CoinBtnClick()
    {
        // 打开金币获取窗口
        UIMonoInstance.OpenPanel<CoinGetWindow>(GroupType.弹窗1);
    }

    /// <summary>
    /// 面板打开时的回调函数
    /// </summary>
    /// <param name="userData">用户数据</param>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        // 添加各个按钮的点击事件监听
        GMButton.AddListener(GMButton_OnClick);
        okButton.AddListener(OKBtn_OnClick);
        clearButton.AddListener(ClearBtn_OnClick);
        addButton.AddListener(AddBtn_OnClick);
        jumpButton.AddListener(JumpBtn_OnClick);
        if (!leaveRewardButtonInitialized && leaveRewardButton != null)
        {
            leaveRewardButton.AddListener(LeaveReward);
            leaveRewardButtonInitialized = true;
        }

        // 初始化背包道具按钮（已被注释）
        // useBagPropsBtns[0].Init(CanUseUndoProp, PropUseFailed, Prop1Btn_OnClick, false);
        // useBagPropsBtns[1].Init(CanUseClearProp, PropUseFailed, Prop2Btn_OnClick, false);
        // useBagPropsBtns[2].Init(CanUseShuffleProp, PropUseFailed, Prop3Btn_OnClick, false);

        // 刷新金钱和金币道具的UI显示
        // moneyProp.RefreshBagPropUI(new ItemData_BagProp(MoneyPropId, BagMgr.Instance.GetBagPropCount(MoneyPropId)),
        //     false);
        // goldProp.RefreshBagPropUI(new ItemData_BagProp(CoinPropId, BagMgr.Instance.GetBagPropCount(CoinPropId)), false);
        // // 添加金币按钮点击事件
        // goldProp.clickBtn.AddListener(CoinBtnClick);
        // 添加设置按钮点击事件
        setBtn.AddListener(SetBtn_OnClick);

        // 更新进度条显示
        UpdateBarShow(gameRoot != null ? gameRoot.Progress : 0f);
        // 添加事件监听器
        pressChangeListener = gameObject.AddEventListener<PressChange>(OnHandleEventMessage);
        bagPropChangeListener = gameObject.AddEventListener<BagPropChange>(OnHandleEventMessage);
        gameStartListener = gameObject.AddEventListener<GameStart>(OnHandleEventMessage);

        // 验证路线HUD绑定
        ValidateRouteHudBindings();
        CleanupEventRewards(true);
        gameRoot.Initialize(this);
        // 播放背景音乐
        YangAudioManager.Instance.PlayBGM("level_bgm");
        // 强制更新画布
        Canvas.ForceUpdateCanvases();
        Start3DScenePreview();
    }

    /// <summary>
    /// GM按钮点击事件处理函数
    /// </summary>
    private void GMButton_OnClick()
    {
        // 切换GM面板的显示状态
        gmNode.SetActive(!gmNode.activeSelf);
    }

    /// <summary>
    /// 面板关闭时的回调函数
    /// </summary>
    /// <param name="isShutdown">是否是关闭应用</param>
    /// <param name="userData">用户数据</param>
    public override void OnClose(bool isShutdown, object userData)
    {
        CleanupEventRewards(true);
        HideCardSelection();
        StopEventCardTransition();
        sceneLoadVersion++;
        Cleanup3DScenePreview();
        base.OnClose(isShutdown, userData);
        // 释放游戏根对象
        gameRoot?.Dispose();
        // 隐藏提示框
        HideHoverTip();
        // 隐藏骰子装备面板
        if (diceEquipmentPanelUI != null)
        {
            diceEquipmentPanelUI.Hide();
        }

        // 移除事件监听器
        if (pressChangeListener != null)
        {
            Extend.RemoveEventListener(pressChangeListener);
        }

        if (bagPropChangeListener != null)
        {
            Extend.RemoveEventListener(bagPropChangeListener);
        }

        if (gameStartListener != null)
        {
            Extend.RemoveEventListener(gameStartListener);
        }

        // 停止背景音乐
        YangAudioManager.Instance.StopBGM();
    }

    /// <summary>
    /// 开始创建3D场景预览。
    /// </summary>
    private void Start3DScenePreview()
    {
        Cleanup3DScenePreview();
        int requestVersion = ++sceneLoadVersion;
        Create3DScenePreviewAsync(requestVersion).Forget();
    }

    /// <summary>
    /// 异步加载并绑定3D场景预览。
    /// </summary>
    private async UniTask Create3DScenePreviewAsync(int requestVersion)
    {
        if (sceneRawImage == null)
        {
            Debug.LogError("GameWindow SceneRawImage 未绑定。", this);
            return;
        }

        try
        {
            GameObject scenePrefab = await ResourceManager.LoadAssetAsync<GameObject>(ScenePrefabAddress);
            if (requestVersion != sceneLoadVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (scenePrefab == null)
            {
                Debug.LogError($"3D场景预制体加载失败：{ScenePrefabAddress}", this);
                return;
            }

            GameObject instance = Instantiate(scenePrefab);
            if (requestVersion != sceneLoadVersion || !isActiveAndEnabled)
            {
                Destroy(instance);
                return;
            }

            sceneInstance = instance;
            sceneCamera = sceneInstance.GetComponentInChildren<Camera>(true);
            if (sceneCamera == null)
            {
                Debug.LogError("3D场景预制体中未找到相机。", sceneInstance);
                Cleanup3DScenePreview();
                return;
            }

            AudioListener[] audioListeners = sceneInstance.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < audioListeners.Length; i++)
            {
                audioListeners[i].enabled = false;
            }

            Vector2 renderSize = GetSceneRenderSize();
            sceneRenderTexture = new RenderTexture((int) renderSize.x, (int) renderSize.y, 24,
                RenderTextureFormat.ARGB32)
            {
                name = "GameWindowSceneRenderTexture"
            };
            sceneRenderTexture.Create();
            sceneCamera.targetTexture = sceneRenderTexture;
            sceneCamera.enabled = true;
            sceneRawImage.texture = sceneRenderTexture;
            SetScenePreviewVisible(true);
            if (isShowingEventRewards && pendingEventRewards != null && eventRewardItems.Count == 0)
            {
                int rewardRequestVersion = ++eventRewardLoadVersion;
                CreateEventRewardItemsAsync(pendingEventRewards, rewardRequestVersion).Forget();
            }
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
    /// 获取3D场景渲染纹理尺寸。
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
    /// 清理3D场景预览实例和渲染纹理。
    /// </summary>
    private void Cleanup3DScenePreview()
    {
        SetScenePreviewVisible(false);
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
    /// 控制3D场景预览区域的显示状态，避免异步加载期间出现白色占位。
    /// </summary>
    private void SetScenePreviewVisible(bool visible)
    {
        if (sceneRawImage == null)
        {
            return;
        }

        Color color = sceneRawImage.color;
        color.a = visible ? 1f : 0f;
        sceneRawImage.color = color;
    }

    /// <summary>
    /// 显示事件卡奖励并在3D场景中创建奖励物体。
    /// </summary>
    public void ShowEventRewards(IReadOnlyList<RewardItemData> rewards)
    {
        ShowEventRewards(rewards, null);
    }

    /// <summary>
    /// 显示事件卡奖励，并在点击离开后执行后续卡牌流程。
    /// </summary>
    public void ShowEventRewards(IReadOnlyList<RewardItemData> rewards, Action onLeave)
    {
        CleanupEventRewards(false);
        if (rewards == null || rewards.Count == 0)
        {
            return;
        }

        isShowingEventRewards = true;
        pendingEventRewards = rewards;
        eventRewardLeaveCallback = onLeave;
        SetPathCardsVisible(false);
        if (leaveRewardButton != null)
        {
            leaveRewardButton.gameObject.SetActive(true);
        }

        int requestVersion = ++eventRewardLoadVersion;
        CreateEventRewardItemsAsync(rewards, requestVersion).Forget();
    }

    /// <summary>
    /// 异步加载奖励物体预制体并创建奖励实例。
    /// </summary>
    private async UniTask CreateEventRewardItemsAsync(IReadOnlyList<RewardItemData> rewards, int requestVersion)
    {
        GameObject itemPrefab = await ResourceManager.LoadAssetAsync<GameObject>(EventRewardItemPrefabAddress);
        if (requestVersion != eventRewardLoadVersion || !isShowingEventRewards || !isActiveAndEnabled ||
            sceneInstance == null || itemPrefab == null)
        {
            return;
        }

        int count = 0;
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] != null && rewards[i].BagId > 0 && rewards[i].Num > 0f)
            {
                count++;
            }
        }

        float startX = -(count - 1) * EventRewardItemSpacing * 0.5f;
        int createdCount = 0;
        for (int i = 0; i < rewards.Count; i++)
        {
            RewardItemData reward = rewards[i];
            if (reward == null || reward.BagId <= 0 || reward.Num <= 0f)
            {
                continue;
            }

            GameObject instance = Instantiate(itemPrefab, sceneInstance.transform);
            instance.name = $"EventRewardItem_{reward.BagId}_{i}";
            instance.transform.localPosition = new Vector3(startX + createdCount * EventRewardItemSpacing, 1f, 0f);
            EventRewardItem rewardItem = instance.GetComponent<EventRewardItem>();
            if (rewardItem == null)
            {
                rewardItem = instance.AddComponent<EventRewardItem>();
            }

            rewardItem.Initialize(reward, ClaimEventReward);
            eventRewardItems.Add(rewardItem);
            createdCount++;
        }
    }

    /// <summary>
    /// 处理奖励物体点击并发放对应奖励。
    /// </summary>
    private void ClaimEventReward(EventRewardItem rewardItem)
    {
        if (rewardItem == null || rewardItem.RewardItemData == null || gameRoot == null)
        {
            return;
        }

        gameRoot.GrantEventReward(rewardItem.RewardItemData);
        eventRewardItems.Remove(rewardItem);
        Destroy(rewardItem.gameObject);
        if (eventRewardItems.Count == 0)
        {
            LeaveReward();
        }
    }

    /// <summary>
    /// 将屏幕中的奖励区域转换为3D射线并处理命中物体。
    /// </summary>
    private void HandleEventRewardPointer()
    {
        if (!isShowingEventRewards || !Input.GetMouseButtonDown(0) || sceneRawImage == null || sceneCamera == null)
        {
            return;
        }

        RectTransform rawImageRect = sceneRawImage.rectTransform;
        Canvas canvas = rawImageRect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, Input.mousePosition, uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Rect rect = rawImageRect.rect;
        if (!rect.Contains(localPoint) || rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        Vector2 viewportPoint = new Vector2((localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height);
        Ray ray = sceneCamera.ViewportPointToRay(viewportPoint);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            EventRewardItem rewardItem = hit.collider.GetComponentInParent<EventRewardItem>();
            rewardItem?.HandleClick();
        }
    }

    /// <summary>
    /// 离开奖励展示并恢复路径事件卡。
    /// </summary>
    private void LeaveReward()
    {
        Action onLeave = eventRewardLeaveCallback;
        CleanupEventRewards(true);
        onLeave?.Invoke();
    }

    /// <summary>
    /// 清理奖励实例、按钮和奖励展示状态。
    /// </summary>
    private void CleanupEventRewards(bool restorePathCards)
    {
        eventRewardLoadVersion++;
        for (int i = eventRewardItems.Count - 1; i >= 0; i--)
        {
            if (eventRewardItems[i] != null)
            {
                Destroy(eventRewardItems[i].gameObject);
            }
        }

        eventRewardItems.Clear();
        pendingEventRewards = null;
        eventRewardLeaveCallback = null;
        isShowingEventRewards = false;
        if (leaveRewardButton != null)
        {
            leaveRewardButton.gameObject.SetActive(false);
        }

        if (restorePathCards)
        {
            SetPathCardsVisible(true);
        }
    }

    /// <summary>
    /// 设置所有路径事件卡的显示状态。
    /// </summary>
    private void SetPathCardsVisible(bool visible)
    {
        for (int i = 0; i < pathCardItems.Count; i++)
        {
            if (pathCardItems[i] != null)
            {
                pathCardItems[i].gameObject.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// 每帧更新函数
    /// </summary>
    private void Update()
    {
        // 更新进度条显示
        UpdateBarShow(gameRoot != null ? gameRoot.Progress : 0f);
        // 刷新背包道具按钮状态
        RefreshUseBagPropButtonState();
        HandleEventRewardPointer();
    }

    /// <summary>
    /// 事件消息处理函数
    /// </summary>
    /// <param name="eventData">事件数据</param>
    private void OnHandleEventMessage(EventData eventData)
    {
        // 处理按压变化事件
        if (eventData.Args is PressChange)
        {
            UpdateBarShow(gameRoot != null ? gameRoot.Progress : 0f);
            return;
        }

        // 处理背包道具变化事件
        if (eventData.Args is BagPropChange propChange)
        {
            HandleBagPropChange(propChange);
        }

        // 处理游戏开始事件
        if (eventData.Args is GameStart gameStart)
        {
            //StartAni(gameStart);
        }
    }

    /// <summary>
    /// 处理背包道具变化
    /// </summary>
    /// <param name="propChange">道具变化数据</param>
    private void HandleBagPropChange(BagPropChange propChange)
    {
        // 获取目标道具UI
        ItemUI_BagProp targetProp = GetFlyTargetProp(propChange.propID);
        if (targetProp == null)
        {
            return;
        }

        // 如果是增加道具
        if (propChange.num > 0f)
        {
            // 如果是金币道具且有待处理的金币飞行事件，则跳过
            if (propChange.propID == CoinPropId && ConsumePendingCoinFlyEventSuppress())
            {
                return;
            }

            // 播放金币飞行效果
            PlayMoneyFlyEffect(targetProp);
            return;
        }

        // 同步道具数量
        targetProp.SyncBagPropCount();
    }

    /// <summary>
    /// 开始游戏动画
    /// </summary>
    /// <param name="gameStart">游戏开始数据</param>
    public void StartAni(GameStart gameStart)
    {
        // 设置开始动画文本
        startAniText.text = $"{gameStart.levelName}";
        levelText.text = gameRoot != null ? gameRoot.CurrentLevelId.ToString() : gameStart.levelName;

        // 显示并播放动画
        startAni.gameObject.SetActive(true);
        startAni.transform.SetAsLastSibling();
        startAni.AnimationState.SetAnimation(0, "jinchang2", false);

        // 设置动画完成回调
        void OnAnimationStateOnComplete(TrackEntry trackEntry)
        {
            startAni.gameObject.SetActive(false);
            YangAudioManager.Instance.PlaySoundAudio("LevelBegin");
            startAni.AnimationState.Complete -= OnAnimationStateOnComplete;
        }

        startAni.AnimationState.Complete += OnAnimationStateOnComplete;
    }

    /// <summary>
    /// 获取飞行目标道具
    /// </summary>
    /// <param name="propId">道具ID</param>
    /// <returns>道具UI对象</returns>
    private ItemUI_BagProp GetFlyTargetProp(int propId)
    {
        // 根据道具ID返回对应的道具UI
        if (propId == MoneyPropId)
        {
            return moneyProp;
        }

        if (propId == CoinPropId)
        {
            return goldProp;
        }

        return null;
    }

    /// <summary>
    /// 更新进度条显示
    /// </summary>
    /// <param name="value">进度值</param>
    public void UpdateBarShow(float value)
    {
        // 更新进度条填充和文本（已被注释）
        // bar.fillAmount = value;
        // barText.text = $"{(int) (value * 100)}%";
        // barEffect.anchoredPosition = new Vector2(value * barEffectParent.rect.width, 0);
        // barEffect.gameObject.SetActive(0.02f < value && value < 0.98f);
    }

    /// <summary>
    /// 显示提示信息
    /// </summary>
    public void ShowTip()
    {
        // 显示提示节点和文本
        moneyTipsNode.gameObject.SetActive(true);
        moneyTipsText.text = "鑾峰緱1000閲戝竵";
        // DOTween序列动画（已被注释）
        // DOTween.Sequence().set
    }

    /// <summary>
    /// 设置按钮点击事件处理函数
    /// </summary>
    /// <returns>异步任务</returns>
    public async void SetBtn_OnClick()
    {
        // 打开设置窗口
        (int id, SettingWindow panel) panel = await UIMonoInstance.OpenPanel<SettingWindow>(GroupType.弹窗2);
        // 设置重置回调函数
        panel.panel.ResetCallBack = () => { gameRoot?.RestartGame(); };
    }

    /// <summary>
    /// 获取屏幕中心的世界坐标
    /// </summary>
    /// <returns>世界坐标</returns>
    private Vector3 GetScreenCenterWorldPosition()
    {
        // 获取画布和矩形变换
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform rect = canvas != null ? canvas.transform as RectTransform : transform as RectTransform;
        if (rect != null)
        {
            // 返回矩形中心的世界坐标
            return rect.TransformPoint(rect.rect.center);
        }

        return transform.position;
    }

    /// <summary>
    /// 播放金币飞行效果
    /// </summary>
    /// <param name="targetProp">目标道具</param>
    private void PlayMoneyFlyEffect(ItemUI_BagProp targetProp)
    {
        // 调用重载方法，默认起始位置为原点
        PlayMoneyFlyEffect(targetProp, Vector2.zero);
    }

    /// <summary>
    /// 播放金币飞行效果（重载）
    /// </summary>
    /// <param name="targetProp">目标道具</param>
    /// <param name="startPosition">起始位置</param>
    private void PlayMoneyFlyEffect(ItemUI_BagProp targetProp, Vector2 startPosition)
    {
        YangAudioManager.Instance.PlaySoundAudio("Collect_Coins");
        // 鍋滄褰撳墠姝ｅ湪鎾斁鐨勯噾甯侀琛岀壒鏁堬紝濡傛灉鐩爣閬撳叿涓嶅悓
        StopMoneyFlyEffect(moneyFlyTargetProp != null && moneyFlyTargetProp != targetProp);
        moneyFlyTargetProp = targetProp;

        RectTransform effectRoot = GetMoneyFlyEffectRoot();
        if (effectRoot == null || targetProp == null || targetProp.mImgPropIcon == null)
        {
            SyncPropWithPunch(targetProp);
            return;
        }

        // 璁剧疆鍒濆浣嶇疆銆佺洰鏍囦綅缃€侀噾甯佸浘鏍囧拰澶у皬
        Vector2 targetPosition = GetMoneyFlyTargetPosition(effectRoot, targetProp);
        Sprite moneySprite = targetProp.mImgPropIcon.sprite;
        Vector2 iconSize = GetMoneyFlyIconSize(targetProp);

        // 鍒涘缓DOTween搴忓垪
        moneyFlySequence = DOTween.Sequence().SetTarget(this);
        // 寰幆鍒涘缓澶氫釜閲戝竵鍥炬爣
        for (int i = 0; i < MoneyFlyIconCount; i++)
        {
            // 鍒涘缓閲戝竵鍥炬爣
            Image icon = CreateMoneyFlyIcon(effectRoot, moneySprite, iconSize, startPosition);
            if (icon == null)
            {
                continue;
            }

            // 灏嗛噾甯佸浘鏍囨坊鍔犲埌鍒楄〃
            moneyFlyIcons.Add(icon.gameObject);
            // 璁＄畻寤惰繜鏃堕棿
            float delay = i * MoneyFlyDelayStep;
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * MoneyFlySpawnRadius;
            Vector2 scatterPosition = startPosition + randomOffset;

            RectTransform iconTransform = icon.transform as RectTransform;
            // 娣诲姞缂╂斁鍔ㄧ敾
            moneyFlySequence.Insert(delay, iconTransform.DOScale(Vector3.one, MoneyFlyScatterDuration)
                .SetEase(Ease.OutBack));
            // 娣诲姞浣嶇疆鍔ㄧ敾
            moneyFlySequence.Insert(delay, iconTransform.DOAnchorPos(scatterPosition, MoneyFlyScatterDuration)
                .SetEase(Ease.OutCubic));
            // 娣诲姞鍚戠洰鏍囦綅缃Щ鍔ㄧ殑鍔ㄧ敾
            moneyFlySequence.Insert(delay + MoneyFlyScatterDuration + MoneyFlyWaitDuration,
                iconTransform.DOAnchorPos(targetPosition, MoneyFlyDuration).SetEase(Ease.InCubic));
            // 娣诲姞缂╁皬鍔ㄧ敾
            moneyFlySequence.Insert(delay + MoneyFlyScatterDuration + MoneyFlyWaitDuration,
                iconTransform.DOScale(Vector3.one * 0.45f, MoneyFlyDuration).SetEase(Ease.InCubic));
        }

        // 璁剧疆鍔ㄧ敾瀹屾垚鍚庣殑鍥炶皟
        moneyFlySequence.OnComplete(() =>
        {
            // 娓呴櫎閲戝竵鍥炬爣
            ClearMoneyFlyIcons();
            SyncPropWithPunch(targetProp);
            moneyFlySequence = null;
            moneyFlyTargetProp = null;
        });
    }

    /// <summary>
    /// 从世界坐标位置播放金币飞行效果
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    private void PlayCoinFlyEffectFromWorldPosition(Vector3 worldPosition)
    {
        // 获取金币飞行效果的根节点
        RectTransform effectRoot = GetMoneyFlyEffectRoot();
        // 检查根节点和金币属性是否存在
        if (effectRoot == null || goldProp == null)
        {
            return;
        }

        // 获取UI相机
        Camera uiCamera = UIMonoInstance.Instance != null ? UIMonoInstance.Instance.uiCamera : null;
        // 将世界坐标转换为屏幕坐标
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
        // 将屏幕坐标转换为局部坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screenPoint, uiCamera,
                out Vector2 startPosition))
        {
            startPosition = Vector2.zero;
        }

        // 增加待处理金币飞行事件抑制计数
        pendingCoinFlyEventSuppressCount++;
        // 播放金币飞行效果
        PlayMoneyFlyEffect(goldProp, startPosition);
    }

    /// <summary>
    /// 消耗待处理金币飞行事件抑制
    /// </summary>
    /// <returns>是否成功消耗</returns>
    private bool ConsumePendingCoinFlyEventSuppress()
    {
        // 检查计数是否大于0
        if (pendingCoinFlyEventSuppressCount <= 0)
        {
            return false;
        }

        // 减少计数
        pendingCoinFlyEventSuppressCount--;
        return true;
    }

    /// <summary>
    /// 获取金币飞行效果的根节点
    /// </summary>
    /// <returns>金币飞行效果的根节点</returns>
    private RectTransform GetMoneyFlyEffectRoot()
    {
        // 如果根节点存在，将其设置为最后一个子节点并返回
        if (moneyFlyEffectRoot != null)
        {
            moneyFlyEffectRoot.SetAsLastSibling();
            return moneyFlyEffectRoot;
        }

        // 否则返回当前对象的RectTransform
        return transform as RectTransform;
    }

    /// <summary>
    /// 获取金币飞行的目标位置
    /// </summary>
    /// <param name="effectRoot">效果根节点</param>
    /// <param name="targetProp">目标道具UI</param>
    /// <returns>目标位置</returns>
    private Vector2 GetMoneyFlyTargetPosition(RectTransform effectRoot, ItemUI_BagProp targetProp)
    {
        // 检查参数有效性
        if (effectRoot == null || targetProp == null || targetProp.mImgPropIcon == null)
        {
            return Vector2.zero;
        }

        // 获取UI相机
        Camera uiCamera = UIMonoInstance.Instance != null ? UIMonoInstance.Instance.uiCamera : null;
        // 将目标图标的世界坐标转换为屏幕坐标
        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(uiCamera, targetProp.mImgPropIcon.transform.position);
        // 将屏幕坐标转换为局部坐标
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screenPoint, uiCamera,
                out Vector2 localPoint))
        {
            return localPoint;
        }

        return Vector2.zero;
    }

    /// <summary>
    /// 获取金币图标的大小
    /// </summary>
    /// <param name="targetProp">目标道具UI</param>
    /// <returns>图标大小</returns>
    private Vector2 GetMoneyFlyIconSize(ItemUI_BagProp targetProp)
    {
        // 获取图标的RectTransform
        RectTransform iconRect = targetProp != null && targetProp.mImgPropIcon != null
            ? targetProp.mImgPropIcon.transform as RectTransform
            : null;
        // 检查图标大小是否有效
        if (iconRect != null && iconRect.rect.width > 0f && iconRect.rect.height > 0f)
        {
            return iconRect.rect.size;
        }

        // 返回默认大小
        return Vector2.one * MoneyFlyIconFallbackSize;
    }

    /// <summary>
    /// 创建金币图标
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="sprite">精灵图片</param>
    /// <param name="size">图标大小</param>
    /// <param name="anchoredPosition">锚点位置</param>
    /// <returns>创建的图片组件</returns>
    private Image CreateMoneyFlyIcon(RectTransform parent, Sprite sprite, Vector2 size, Vector2 anchoredPosition)
    {
        // 创建游戏对象
        GameObject iconObject =
            new GameObject("MoneyFlyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        // 设置图层
        iconObject.layer = LayerMask.NameToLayer("UI");
        // 获取RectTransform组件
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        // 设置父节点
        rect.SetParent(parent, false);
        // 设置锚点
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        // 设置大小
        rect.sizeDelta = size;
        // 设置锚点位置
        rect.anchoredPosition = anchoredPosition;
        // 初始缩放为0
        rect.localScale = Vector3.zero;
        // 设置为最后一个子节点
        rect.SetAsLastSibling();

        // 获取Image组件
        Image icon = iconObject.GetComponent<Image>();
        // 设置是否接收射线
        icon.raycastTarget = false;
        // 保持宽高比
        icon.preserveAspect = true;
        // 设置精灵图片
        icon.sprite = sprite;
        return icon;
    }

    /// <summary>
    /// 同步道具与脉冲效果
    /// </summary>
    /// <param name="targetProp">目标道具UI</param>
    private void SyncPropWithPunch(ItemUI_BagProp targetProp)
    {
        // 检查目标道具是否存在
        if (targetProp == null)
        {
            return;
        }

        // 同步道具数量
        targetProp.SyncBagPropCount();
        // 脉冲目标变换
        Transform punchTarget = targetProp.mTxtPropCount != null
            ? targetProp.mTxtPropCount.transform
            : targetProp.transform;
        // 停止之前的动画
        punchTarget.DOKill();
        // 播放脉冲缩放动画
        punchTarget.DOPunchScale(Vector3.one * 0.18f, 0.24f, 1, 0.2f)
            .SetEase(Ease.OutBack)
            .SetTarget(this);
    }

    /// <summary>
    /// 停止金币飞行效果
    /// </summary>
    /// <param name="syncInterruptedTarget">是否同步中断的目标</param>
    private void StopMoneyFlyEffect(bool syncInterruptedTarget = false)
    {
        // 获取被中断的目标道具
        ItemUI_BagProp interruptedTarget = moneyFlyTargetProp;
        // 停止动画序列
        if (moneyFlySequence != null)
        {
            moneyFlySequence.Kill();
            moneyFlySequence = null;
        }

        // 清除金币图标
        ClearMoneyFlyIcons();
        // 清除目标道具引用
        moneyFlyTargetProp = null;

        // 如果需要同步中断的目标
        if (syncInterruptedTarget)
        {
            SyncPropWithPunch(interruptedTarget);
        }
    }

    /// <summary>
    /// 清除金币图标
    /// </summary>
    private void ClearMoneyFlyIcons()
    {
        // 遍历并销毁所有金币图标
        for (int i = 0; i < moneyFlyIcons.Count; i++)
        {
            if (moneyFlyIcons[i] != null)
            {
                Destroy(moneyFlyIcons[i]);
            }
        }

        // 清空列表
        moneyFlyIcons.Clear();
    }

    /// <summary>
    /// 刷新背包道具按钮的可用状态。
    /// </summary>
    private void RefreshUseBagPropButtonState()
    {
        for (int i = 0; i < useBagPropsBtns.Count; i++)
        {
            if (useBagPropsBtns[i] != null)
            {
                useBagPropsBtns[i].RefreshUseState();
            }
        }
    }

    /// <summary>
    /// 刷新路线HUD显示
    /// </summary>
    public void RefreshRouteHud()
    {
        ValidateRouteHudBindings(); // 验证并初始化HUD绑定
        RefreshEquippedDiceSlots(); // 刷新已装备的骰子槽位
        ApplyPathCards(); // 应用路径卡牌
        RefreshPileCounters(); // 刷新牌堆计数器
        RefreshHpBar(); // 刷新血条显示
        RefreshLevelText(); // 刷新关卡ID显示
    }

    /// <summary>
    /// 验证路线HUD绑定是否有效
    /// </summary>
    private void ValidateRouteHudBindings()
    {
        if (routeHudValidated) // 如果已经验证过，直接返回
        {
            return;
        }

        if (eventCardRoot != null) // 如果事件卡根节点存在，则隐藏它
        {
            eventCardRoot.gameObject.SetActive(false);
        }

        routeHudRoot.SetAsLastSibling(); // 将HUD根节点设置为最后层级
        InitRouteHudBindings(); // 初始化HUD绑定
        routeHudValidated = true; // 标记为已验证
    }

    /// <summary>
    /// 初始化路线HUD的绑定关系
    /// </summary>
    private void InitRouteHudBindings()
    {
        // 初始化已装备的骰子槽位
        for (int i = 0; i < equippedDiceSlotItems.Count; i++)
        {
            if (equippedDiceSlotItems[i] != null)
            {
                equippedDiceSlotItems[i].Init(i, ShowDiceEquipmentPanel, HideDiceEquipmentPanel);
            }
        }

        // 初始化路径卡牌
        for (int i = 0; i < pathCardItems.Count; i++)
        {
            if (pathCardItems[i] != null)
            {
                pathCardItems[i].Init(i, SelectEventCard);
            }
        }

        // 初始化弃牌堆、抽牌堆和骰子装备面板
        discardPileUI?.Init(ShowHoverTip, HideHoverTip);
        drawPileUI?.Init(ShowHoverTip, HideHoverTip);
        diceEquipmentPanelUI?.Init();
    }

    /// <summary>
    /// 刷新已装备的骰子槽位显示
    /// </summary>
    private void RefreshEquippedDiceSlots()
    {
        // 获取已装备的骰子槽位数据
        IReadOnlyList<EquippedDiceSlotData> slots = gameRoot != null ? gameRoot.EquippedDiceSlots : null;
        for (int i = 0; i < equippedDiceSlotItems.Count; i++)
        {
            // 获取对应槽位的数据，如果超出范围则设为null
            EquippedDiceSlotData data = slots != null && i < slots.Count ? slots[i] : null;
            if (equippedDiceSlotItems[i] != null)
            {
                equippedDiceSlotItems[i].Refresh(data);
            }
        }
    }

    /// <summary>
    /// 应用路径卡牌到UI显示
    /// </summary>
    private void ApplyPathCards()
    {
        for (int i = 0; i < pathCardItems.Count; i++)
        {
            //获取对应的事件卡，如果超出范围则设为null
            EventCard card = i < pendingEventCards.Count ? pendingEventCards[i] : null;
            if (pathCardItems[i] != null)
            {
                pathCardItems[i].Refresh($"事件{i + 1}", card);
            }
        }
    }

    /// <summary>
    /// 刷新牌堆计数器显示
    /// </summary>
    private void RefreshPileCounters()
    {
        // 获取弃牌堆和抽牌堆的数量
        int discardCount = gameRoot != null ? gameRoot.DiscardPileCount : 0;
        int drawCount = gameRoot != null ? gameRoot.DrawPileCount : 0;
        // 刷新弃牌堆UI
        discardPileUI?.Refresh("\u5f03\u724c\u5806", discardCount,
            $"\u5f03\u724c\u5806\u5269\u4f59\uff1a{discardCount}");
        // 刷新抽牌堆UI
        drawPileUI?.Refresh("\u62bd\u724c\u5806", drawCount, $"\u62bd\u724c\u5806\u5269\u4f59\uff1a{drawCount}");
    }

    /// <summary>
    /// 刷新血条显示
    /// </summary>
    private void RefreshHpBar()
    {
        if (gameRoot == null || hpFillImage == null) // 如果游戏根节点或血条填充图片不存在，则返回
        {
            return;
        }

        // 计算血量百分比
        float percent = gameRoot.PlayerMaxHp <= 0 ? 0f : Mathf.Clamp01((float)gameRoot.PlayerHp / gameRoot.PlayerMaxHp);
        // 设置填充区域的锚点
        hpFillImage.fillAmount = percent;
        // 更新血量文本显示
        hpText.text = $"{gameRoot.PlayerHp}/{gameRoot.PlayerMaxHp}";
    }

    /// <summary>
    /// 刷新当前关卡ID文本。
    /// </summary>
    private void RefreshLevelText()
    {
        if (levelText != null && gameRoot != null)
        {
            levelText.text = gameRoot.CurrentLevelId.ToString();
        }
    }

    /// <summary>
    /// 在鼠标下方显示骰子装备面板。
    /// </summary>
    /// <param name="slotIndex">骰子槽位索引</param>
    /// <param name="screenPosition">鼠标屏幕坐标</param>
    private void ShowDiceEquipmentPanel(int slotIndex, Vector2 screenPosition)
    {
        diceEquipmentPanelUI?.Show(gameRoot != null ? gameRoot.EquippedDiceSlots : null, slotIndex,
            screenPosition);
    }

    /// <summary>
    /// 隐藏鼠标悬停时显示的骰子装备面板。
    /// </summary>
    private void HideDiceEquipmentPanel()
    {
        diceEquipmentPanelUI?.Hide();
    }

    /// <summary>
    /// 显示悬停提示
    /// </summary>
    /// <param name="text">提示文本</param>
    /// <param name="screenPosition">屏幕位置</param>
    private void ShowHoverTip(string text, Vector2 screenPosition)
    {
        if (hoverTipRoot == null || hoverTipText == null) // 如果悬停提示根节点或文本不存在，则返回
        {
            return;
        }

        hoverTipText.text = text;
        Camera uiCamera = UIMonoInstance.Instance != null ? UIMonoInstance.Instance.uiCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(routeHudRoot, screenPosition, uiCamera,
                out Vector2 localPoint))
        {
            // 设置悬停提示的位置
            hoverTipRoot.anchoredPosition = localPoint + new Vector2(0f, 56f);
        }

        hoverTipRoot.gameObject.SetActive(true);
        hoverTipRoot.SetAsLastSibling();
    }

    /// <summary>
    /// 隐藏悬停提示
    /// </summary>
    private void HideHoverTip()
    {
        if (hoverTipRoot != null)
        {
            hoverTipRoot.gameObject.SetActive(false);
        }
    }

    // /// <summary>
    // /// 刷新关卡下拉列表
    // /// </summary>
    // /// <param name="levelDatas">关卡数据列表</param>
    // public void RefreshLevelDropdown(IReadOnlyList<TbLevelData> levelDatas)
    // {
    //     if (levelDropdown == null || levelDatas == null) // 如果下拉列表或关卡数据不存在，则返回
    //     {
    //         return;
    //     }
    //
    //     List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
    //     for (int i = 0; i < levelDatas.Count; i++)
    //     {
    //         TbLevelData levelData = levelDatas[i];
    //         options.Add(new TMP_Dropdown.OptionData
    //         {
    //             text = levelData.Id.ToString()
    //         });
    //     }
    //
    //     levelDropdown.options = options;
    // }

    /// <summary>
    /// 刷新事件卡显示
    /// </summary>
    /// <param name="shownCards">显示的事件卡列表</param>
    public void RefreshEventCards(IReadOnlyList<EventCard> shownCards)
    {
        pendingEventCards = shownCards ?? Array.Empty<EventCard>();
        ApplyPathCards();
    }

    /// <summary>
    /// 提交事件卡并播放弃牌和补牌动画。
    /// </summary>
    /// <summary>
    /// 完成事件卡牌的过渡动画
    /// </summary>
    /// <param name="index">要处理的卡牌索引</param>
    /// <param name="commitAction">执行完成后的回调动作</param>
    public void CompleteEventCardTransition(int index, Action commitAction)
    {
        // 检查是否有过渡动画正在播放，或索引无效，或卡牌为空，或回调动作为空
        if (IsEventCardTransitionPlaying || index < 0 || index >= pathCardItems.Count ||
            pathCardItems[index] == null || commitAction == null)
        {
            // 如果有条件不满足，直接执行回调并返回
            commitAction?.Invoke();
            return;
        }

        // 获取源卡牌及其RectTransform
        PathCardUI sourceCard = pathCardItems[index];
        RectTransform sourceRect = sourceCard.RectTransform;
        // 如果源卡牌的RectTransform为空，执行回调并返回
        if (sourceRect == null)
        {
            commitAction.Invoke();
            return;
        }

        // 创建用于动画的弃牌对象
        GameObject discardObject = CreateEventCardAnimationObject(sourceCard.gameObject, sourceRect.position,
            sourceRect.localScale);
        // 隐藏源卡牌
        sourceCard.gameObject.SetActive(false);
        // 执行回调
        commitAction.Invoke();

        // 获取目标卡牌及其RectTransform
        PathCardUI targetCard = index < pathCardItems.Count ? pathCardItems[index] : null;
        // 如果目标卡牌或其RectTransform为空，销毁弃牌对象并返回
        if (targetCard == null || targetCard.RectTransform == null)
        {
            DestroyEventCardAnimationObject(discardObject);
            return;
        }

        // 获取目标卡牌的位置和缩放
        RectTransform targetRect = targetCard.RectTransform;
        Vector3 targetPosition = targetRect.position;
        Vector3 targetScale = targetRect.localScale;
        // 创建用于动画的抽牌对象
        // 获取抽牌堆的位置，如果抽牌堆不存在则使用源卡牌位置
        Vector3 drawPilePosition = drawPileUI != null && drawPileUI.RectTransform != null
            ? drawPileUI.RectTransform.position
            : sourceRect.position;
        GameObject drawObject = CreateEventCardAnimationObject(targetCard.gameObject, drawPilePosition,
            targetScale * EventCardDrawStartScale);
        // 设置抽牌对象的初始旋转
        drawObject.transform.localRotation = Quaternion.Euler(0f, 0f, EventCardDrawStartRotation);

        // 获取弃牌堆的位置，如果弃牌堆不存在则使用源卡牌位置
        Vector3 discardPosition = discardPileUI != null && discardPileUI.RectTransform != null
            ? discardPileUI.RectTransform.position
            : sourceRect.position;
        // 创建DOTween动画序列
        eventCardTransitionSequence = DOTween.Sequence().SetTarget(this)
            // 第一部分：弃牌动画
            .Append(discardObject.transform.DOMove(discardPosition, EventCardDiscardDuration).SetEase(Ease.InCubic))
            .Join(discardObject.transform.DOScale(targetScale * 0.2f, EventCardDiscardDuration).SetEase(Ease.InCubic))
            .AppendCallback(() =>
            {
                DestroyEventCardAnimationObject(discardObject);
            })
            // 第二部分：抽牌动画
            .Append(drawObject.transform.DOMove(targetPosition, EventCardDrawDuration).SetEase(Ease.OutCubic))
            .Join(drawObject.transform.DOScale(targetScale, EventCardDrawDuration).SetEase(Ease.OutBack))
            .Join(drawObject.transform.DORotate(Vector3.zero, EventCardDrawDuration).SetEase(Ease.OutBack))
            // 动画完成后的回调
            .OnComplete(() =>
            {
                targetCard.gameObject.SetActive(!isShowingEventRewards);
                DestroyEventCardAnimationObject(discardObject);
                DestroyEventCardAnimationObject(drawObject);
                eventCardTransitionSequence = null;
            })
            // 动画被中断时的回调
            .OnKill(() =>
            {
                targetCard.gameObject.SetActive(!isShowingEventRewards);
                DestroyEventCardAnimationObject(discardObject);
                DestroyEventCardAnimationObject(drawObject);
                eventCardTransitionSequence = null;
            });
    }

    /// <summary>
    /// 停止事件卡动画并清理临时对象。
    /// </summary>
    private void StopEventCardTransition()
    {
        eventCardTransitionSequence?.Kill();
        eventCardTransitionSequence = null;
        for (int i = eventCardAnimationObjects.Count - 1; i >= 0; i--)
        {
            if (eventCardAnimationObjects[i] != null)
            {
                Destroy(eventCardAnimationObjects[i]);
            }
        }

        eventCardAnimationObjects.Clear();
    }

    /// <summary>
    /// 创建事件卡动画副本并关闭其交互。
    /// </summary>
    private GameObject CreateEventCardAnimationObject(GameObject source, Vector3 position, Vector3 scale)
    {
        GameObject animationObject = Instantiate(source, routeHudRoot, true);
        animationObject.SetActive(true);
        animationObject.transform.position = position;
        animationObject.transform.localScale = scale;
        animationObject.transform.SetAsLastSibling();
        CanvasGroup canvasGroup = animationObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = animationObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        eventCardAnimationObjects.Add(animationObject);
        return animationObject;
    }

    /// <summary>
    /// 销毁事件卡动画副本。
    /// </summary>
    private void DestroyEventCardAnimationObject(GameObject animationObject)
    {
        if (animationObject == null)
        {
            return;
        }

        eventCardAnimationObjects.Remove(animationObject);
        Destroy(animationObject);
    }

    /// <summary>
    /// 应用事件卡项到UI显示
    /// </summary>
    private void ApplyEventCardItems()
    {
        for (int i = 0; i < eventCardItems.Count; i++)
        {
            EventCard card = i < pendingEventCards.Count ? pendingEventCards[i] : null;
            if (eventCardItems[i] != null)
            {
                eventCardItems[i].Refresh(card);
            }
        }
    }

    /// <summary>
    /// 开始预览事件卡。
    /// </summary>
    /// <param name="index">事件卡索引</param>
    private void SelectEventCard(int index)
    {
        if (isShowingEventRewards)
        {
            return;
        }

        ShowCardSelection(index);
    }

    /// <summary>
    /// 创建并显示事件卡选择遮罩。
    /// </summary>
    /// <param name="index">事件卡索引</param>
    private void ShowCardSelection(int index)
    {
        if (cardSelectionSource != null || cardSelectionClosing || index < 0 || index >= pathCardItems.Count ||
            pathCardItems[index] == null || !pathCardItems[index].gameObject.activeSelf)
        {
            return;
        }

        EnsureCardSelectionOverlay();
        if (eventCardRoot == null)
        {
            return;
        }

        cardSelectionSource = pathCardItems[index];
        cardSelectionIndex = index;
        RectTransform sourceRect = cardSelectionSource.RectTransform;
        if (sourceRect == null)
        {
            HideCardSelection();
            return;
        }

        cardSelectionPreviewObject = Instantiate(cardSelectionSource.gameObject, eventCardRoot, true);
        cardSelectionPreviewObject.name = "CardSelectionPreview";
        cardSelectionPreviewObject.transform.position = sourceRect.position;
        cardSelectionPreviewObject.transform.localScale = sourceRect.localScale;
        CanvasGroup previewCanvasGroup = cardSelectionPreviewObject.GetComponent<CanvasGroup>();
        if (previewCanvasGroup == null)
        {
            previewCanvasGroup = cardSelectionPreviewObject.AddComponent<CanvasGroup>();
        }

        previewCanvasGroup.interactable = false;
        previewCanvasGroup.blocksRaycasts = false;
        cardSelectionSource.gameObject.SetActive(false);
        eventCardRoot.gameObject.SetActive(true);
        eventCardRoot.SetAsLastSibling();
        Transform overlayParent = eventCardRoot.parent;
        if (overlayParent != null)
        {
            cardSelectionParentSiblingIndex = overlayParent.GetSiblingIndex();
            overlayParent.SetAsLastSibling();
        }

        Vector3 targetPosition = eventCardRoot.TransformPoint(eventCardRoot.rect.center);
        Vector3 targetScale = sourceRect.localScale * CardSelectionScale;
        cardSelectionPreviewObject.transform.SetAsLastSibling();
        CreateCardSelectionButtons();
        cardSelectionSequence = DOTween.Sequence().SetTarget(this)
            .Append(cardSelectionPreviewObject.transform.DOMove(targetPosition, EventCardDrawDuration)
                .SetEase(Ease.OutCubic))
            .Join(cardSelectionPreviewObject.transform.DOScale(targetScale, EventCardDrawDuration)
                .SetEase(Ease.OutBack))
            .OnComplete(() => cardSelectionSequence = null);
    }

    /// <summary>
    /// 初始化事件卡选择遮罩和按钮。
    /// </summary>
    private void EnsureCardSelectionOverlay()
    {
        if (eventCardRoot == null)
        {
            return;
        }

        HorizontalOrVerticalLayoutGroup layoutGroup = eventCardRoot.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        Image mask = eventCardRoot.GetComponent<Image>();
        if (mask == null)
        {
            mask = eventCardRoot.gameObject.AddComponent<Image>();
        }

        mask.color = new Color(0f, 0f, 0f, 0.78f);
        mask.raycastTarget = true;
    }

    /// <summary>
    /// 创建事件卡选择按钮并绑定确认和取消操作。
    /// </summary>
    private void CreateCardSelectionButtons()
    {
        RemoveCardSelectionButtons();
        float buttonY = -Mathf.Min(330f, eventCardRoot.rect.height * 0.5f - 80f);
        UICustomButton confirmButton = CreateCardSelectionButton("ConfirmCardButton", "确定",
            new Vector2(-130f, buttonY), new Color(0.16f, 0.48f, 0.78f, 1f));
        UICustomButton cancelButton = CreateCardSelectionButton("CancelCardButton", "取消",
            new Vector2(130f, buttonY), new Color(0.32f, 0.36f, 0.44f, 1f));
        confirmButton.AddListener(ConfirmCardSelection);
        cancelButton.AddListener(CancelCardSelection);
    }

    /// <summary>
    /// 创建单个事件卡选择按钮。
    /// </summary>
    /// <param name="objectName">按钮对象名称</param>
    /// <param name="text">按钮文本</param>
    /// <param name="position">按钮位置</param>
    /// <param name="color">按钮颜色</param>
    /// <returns>创建的按钮组件</returns>
    private UICustomButton CreateCardSelectionButton(string objectName, string text, Vector2 position, Color color)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(UICustomButton));
        buttonObject.layer = LayerMask.NameToLayer("UI");
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(eventCardRoot, false);
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(CardSelectionButtonWidth, CardSelectionButtonHeight);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;

        TextMeshProUGUI label = CreateCardSelectionText(buttonRect, text);
        label.color = Color.white;
        UICustomButton customButton = buttonObject.GetComponent<UICustomButton>();
        customButton.needClickAudio = true;
        customButton.needAni = true;
        return customButton;
    }

    /// <summary>
    /// 创建事件卡选择按钮文本。
    /// </summary>
    /// <param name="parent">文本父节点</param>
    /// <param name="text">文本内容</param>
    /// <returns>创建的文本组件</returns>
    private static TextMeshProUGUI CreateCardSelectionText(RectTransform parent, string text)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = LayerMask.NameToLayer("UI");
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = GameManager.Instance.font;
        label.text = text;
        label.fontSize = 32f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>
    /// 确认当前预览的事件卡并继续原有事件处理。
    /// </summary>
    private void ConfirmCardSelection()
    {
        if (cardSelectionClosing)
        {
            return;
        }

        int index = cardSelectionIndex;
        HideCardSelection();
        if (index >= 0)
        {
            gameRoot?.SelectEventCard(index);
        }
    }

    /// <summary>
    /// 取消当前事件卡预览。
    /// </summary>
    private void CancelCardSelection()
    {
        if (cardSelectionClosing || cardSelectionPreviewObject == null || cardSelectionSource == null)
        {
            return;
        }

        RectTransform sourceRect = cardSelectionSource.RectTransform;
        if (sourceRect == null)
        {
            HideCardSelection();
            return;
        }

        cardSelectionClosing = true;
        RemoveCardSelectionButtons();
        cardSelectionSequence?.Kill();
        cardSelectionSequence = DOTween.Sequence().SetTarget(this)
            .Append(cardSelectionPreviewObject.transform.DOMove(sourceRect.position, EventCardDrawDuration)
                .SetEase(Ease.InOutCubic))
            .Join(cardSelectionPreviewObject.transform.DOScale(sourceRect.localScale, EventCardDrawDuration)
                .SetEase(Ease.InOutCubic))
            .OnComplete(HideCardSelection);
    }

    /// <summary>
    /// 清理事件卡选择状态并恢复原卡牌。
    /// </summary>
    private void HideCardSelection()
    {
        cardSelectionSequence?.Kill();
        cardSelectionSequence = null;
        RemoveCardSelectionButtons();
        if (cardSelectionSource != null)
        {
            cardSelectionSource.gameObject.SetActive(true);
        }

        if (cardSelectionPreviewObject != null)
        {
            Destroy(cardSelectionPreviewObject);
        }

        cardSelectionPreviewObject = null;
        cardSelectionSource = null;
        cardSelectionIndex = -1;
        if (eventCardRoot != null && eventCardRoot.parent != null && eventCardRoot.parent.parent != null &&
            cardSelectionParentSiblingIndex >= 0)
        {
            int siblingIndex = Mathf.Min(cardSelectionParentSiblingIndex, eventCardRoot.parent.parent.childCount - 1);
            eventCardRoot.parent.SetSiblingIndex(siblingIndex);
        }

        cardSelectionParentSiblingIndex = -1;
        cardSelectionClosing = false;
        if (eventCardRoot != null)
        {
            eventCardRoot.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 移除事件卡选择遮罩下创建的按钮。
    /// </summary>
    private void RemoveCardSelectionButtons()
    {
        if (eventCardRoot == null)
        {
            return;
        }

        for (int i = eventCardRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = eventCardRoot.GetChild(i);
            if (child.gameObject == cardSelectionPreviewObject)
            {
                continue;
            }

            if (child.name == "ConfirmCardButton" || child.name == "CancelCardButton")
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 确认按钮点击事件处理
    /// </summary>
    private void OKBtn_OnClick()
    {
        gameRoot?.RestartGame();
    }

    /// <summary>
    /// 跳转按钮点击事件处理
    /// </summary>
    private void JumpBtn_OnClick()
    {
        string target = levelDropdown.captionText.text;
        if (int.TryParse(target, out int levelID))
        {
            gameRoot?.JumpToLevel(levelID);
        }
        else
        {
            FloatTipWindow.Show("该关卡不存在");
        }
    }
}
