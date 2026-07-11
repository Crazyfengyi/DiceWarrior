using System;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using cfg;
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
    private bool routeHudValidated; // 路线HUD验证标志

    /// <summary>
    /// 析构函数，停止金币飞行效果
    /// </summary>
    private void OnDestroy()
    {
        StopMoneyFlyEffect();
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
        gameRoot.Initialize(this);
        // 播放背景音乐
        YangAudioManager.Instance.PlayBGM("level_bgm");
        // 强制更新画布
        Canvas.ForceUpdateCanvases();
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
    /// 每帧更新函数
    /// </summary>
    private void Update()
    {
        // 更新进度条显示
        UpdateBarShow(gameRoot != null ? gameRoot.Progress : 0f);
        // 刷新背包道具按钮状态
        RefreshUseBagPropButtonState();
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
            StartAni(gameStart);
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
        levelText.text = $"{gameStart.levelName}";

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
    /// 道具1按钮点击处理函数
    /// </summary>
    /// <param name="id">道具ID</param>
    /// <param name="isFreeUse">是否免费使用</param>
    /// <returns>是否使用成功</returns>
    private bool Prop1Btn_OnClick(int id, bool isFreeUse)
    {
        return gameRoot != null && gameRoot.UseUndoProp(id, isFreeUse);
    }

    /// <summary>
    /// 道具2按钮点击处理函数
    /// </summary>
    /// <param name="id">道具ID</param>
    /// <param name="isFreeUse">是否免费使用</param>
    /// <returns>是否使用成功</returns>
    private bool Prop2Btn_OnClick(int id, bool isFreeUse)
    {
        return gameRoot != null && gameRoot.UseClearProp(id, isFreeUse);
    }

    /// <summary>
    /// 道具3按钮点击处理函数
    /// </summary>
    /// <param name="id">道具ID</param>
    /// <param name="isFreeUse">是否免费使用</param>
    /// <returns>是否使用成功</returns>
    private bool Prop3Btn_OnClick(int id, bool isFreeUse)
    {
        return gameRoot != null && gameRoot.UseShuffleProp(id, isFreeUse, GetScreenCenterWorldPosition());
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
    /// 检查是否可以使用撤销道具
    /// </summary>
    /// <param name="id">道具ID</param>
    /// <returns>是否可以使用</returns>
    private bool CanUseUndoProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseUndoProp(id);
    }

    /// <summary>
    /// 检查是否可以使用清除道具
    /// </summary>
    /// <param name="id">道具ID</param>
    /// <returns>是否可以使用</returns>
    private bool CanUseClearProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseClearProp(id);
    }

    /// <summary>
    /// 检查是否可以使用洗牌道具
    /// </summary>
    /// <param name="id">道具ID</param>
    /// <returns>是否可以使用</returns>
    private bool CanUseShuffleProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseShuffleProp(id);
    }

    /// <summary>
    /// 道具使用失败
    /// </summary>
    /// <param name="id">道具ID</param>
    private void PropUseFailed(int id)
    {
        FloatTipWindow.Show("鏆傛椂鏃犳硶浣跨敤");
    }

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
                equippedDiceSlotItems[i].Init(i, ShowDiceEquipmentPanel);
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
            // 获取对应的事件卡，如果超出范围则设为null
            EventCard card = i < pendingEventCards.Count ? pendingEventCards[i] : null;
            if (pathCardItems[i] != null)
            {
                pathCardItems[i].Refresh($"\u8def\u5f84{i + 1}", card);
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
    /// 显示骰子装备面板
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    private void ShowDiceEquipmentPanel(int slotIndex)
    {
        diceEquipmentPanelUI?.Show(gameRoot != null ? gameRoot.EquippedDiceSlots : null, slotIndex);
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

    /// <summary>
    /// 刷新关卡下拉列表
    /// </summary>
    /// <param name="levelDatas">关卡数据列表</param>
    public void RefreshLevelDropdown(IReadOnlyList<TbLevelData> levelDatas)
    {
        if (levelDropdown == null || levelDatas == null) // 如果下拉列表或关卡数据不存在，则返回
        {
            return;
        }

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < levelDatas.Count; i++)
        {
            TbLevelData levelData = levelDatas[i];
            options.Add(new TMP_Dropdown.OptionData
            {
                text = levelData.Id.ToString()
            });
        }

        levelDropdown.options = options;
    }

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
    /// 选择事件卡
    /// </summary>
    /// <param name="index">事件卡索引</param>
    private void SelectEventCard(int index)
    {
        gameRoot?.SelectEventCard(index);
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