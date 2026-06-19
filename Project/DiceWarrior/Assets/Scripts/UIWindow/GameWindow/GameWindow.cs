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
/// 娓告垙鐣岄潰
/// </summary>
/// <summary>
/// 娓告垙绐楀彛绫伙紝缁ф壙鑷猆GUIPanelBase锛屼娇鐢―efaultUGUIDataBase浣滀负鏁版嵁鍩虹被
/// </summary>
public class GameWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    private const int MoneyPropId = 1;
    private const int CoinPropId = 2;
    private const int MoneyFlyIconCount = 8;
    private const float MoneyFlySpawnRadius = 120f;
    private const float MoneyFlyScatterDuration = 0.14f;
    private const float MoneyFlyWaitDuration = 0.08f;
    private const float MoneyFlyDuration = 0.42f;
    private const float MoneyFlyDelayStep = 0.035f;
    private const float MoneyFlyIconFallbackSize = 72f;

    public ItemUI_BagProp moneyProp;
    public ItemUI_BagProp goldProp;
    public UICustomButton setBtn;
    public Image bar;
    public TextMeshProUGUI barText;
    public RectTransform barEffectParent;
    public RectTransform barEffect;
    public RectTransform moneyTipsNode;
    public TextMeshProUGUI moneyTipsText;
    public SkeletonGraphic startAni;
    public TextMeshProUGUI startAniText;
    public GameObject gmNode;
    public TMP_Dropdown levelDropdown;
    public List<ItemUI_UseBagProp> useBagPropsBtns;

    [SerializeField] private UICustomButton GMButton;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private UICustomButton okButton;
    [SerializeField] private UICustomButton clearButton;
    [SerializeField] private UICustomButton addButton;
    [SerializeField] private UICustomButton jumpButton;
    [SerializeField] private RectTransform moneyFlyEffectRoot;
    [SerializeField] private RectTransform eventCardRoot;
    [SerializeField] private List<EventCardItemUI> eventCardItems = new List<EventCardItemUI>();

    private EventInfo pressChangeListener;
    private EventInfo bagPropChangeListener;
    private EventInfo gameStartListener;
    private Sequence moneyFlySequence;
    private ItemUI_BagProp moneyFlyTargetProp;
    private int pendingCoinFlyEventSuppressCount;
    private readonly List<GameObject> moneyFlyIcons = new List<GameObject>();
    [SerializeField] private GameRoot gameRoot;
    private IReadOnlyList<EventCard> pendingEventCards = Array.Empty<EventCard>();
    private bool eventCardItemsCreating;

    /// <summary>
    /// 鏋愭瀯鍑芥暟锛屽仠姝㈤噾甯侀琛岀壒鏁?    /// </summary>
    private void OnDestroy()
    {
        StopMoneyFlyEffect();
    }

    // 鎸夐挳鐐瑰嚮浜嬩欢澶勭悊鍑芥暟
    private void ClearBtn_OnClick()
    {
        YangSaveDataManager.Instance.ClearSaveData();
        FloatTipWindow.Show("娓呴櫎瀛樻。鎴愬姛");
    }

    private void AddBtn_OnClick()
    {
        BagMgr.Instance.AddBagProp(3, 1000);
        BagMgr.Instance.AddBagProp(4, 1000);
        BagMgr.Instance.AddBagProp(5, 1000);
    }

    private void CoinBtnClick()
    {
        UIMonoInstance.OpenPanel<CoinGetWindow>(GroupType.弹窗1);
    }

    /// <summary>
    /// 鎵撳紑绐楀彛鏃剁殑鍒濆鍖?    /// </summary>
    /// <param name="userData">鐢ㄦ埛鏁版嵁</param>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GMButton.AddListener(GMButton_OnClick);
        okButton.AddListener(OKBtn_OnClick);
        clearButton.AddListener(ClearBtn_OnClick);
        addButton.AddListener(AddBtn_OnClick);
        jumpButton.AddListener(JumpBtn_OnClick);

        useBagPropsBtns[0].Init(CanUseUndoProp, PropUseFailed, Prop1Btn_OnClick, false);
        useBagPropsBtns[1].Init(CanUseClearProp, PropUseFailed, Prop2Btn_OnClick, false);
        useBagPropsBtns[2].Init(CanUseShuffleProp, PropUseFailed, Prop3Btn_OnClick, false);

        // 鍒濆鍖栭噾甯佸拰閲戝竵閬撳叿UI
        moneyProp.RefreshBagPropUI(new ItemData_BagProp(MoneyPropId, BagMgr.Instance.GetBagPropCount(MoneyPropId)),
            false);
        goldProp.RefreshBagPropUI(new ItemData_BagProp(CoinPropId, BagMgr.Instance.GetBagPropCount(CoinPropId)), false);
        goldProp.clickBtn.AddListener(CoinBtnClick);
        setBtn.AddListener(SetBtn_OnClick);

        UpdateBarShow(gameRoot != null ? gameRoot.Progress : 0f);
        pressChangeListener = gameObject.AddEventListener<PressChange>(OnHandleEventMessage);
        bagPropChangeListener = gameObject.AddEventListener<BagPropChange>(OnHandleEventMessage);
        gameStartListener = gameObject.AddEventListener<GameStart>(OnHandleEventMessage);

        EnsureEventCardItems();
        EnsureGameRoot().Initialize(this);
        YangAudioManager.Instance.PlayBGM("level_bgm");
        Canvas.ForceUpdateCanvases();
    }

    private void GMButton_OnClick()
    {
        gmNode.SetActive(!gmNode.activeSelf);
    }

    public override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        gameRoot?.Dispose();
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

        YangAudioManager.Instance.StopBGM();
    }

    private void Update()
    {
        UpdateBarShow(gameRoot != null ? gameRoot.Progress : 0f);
        RefreshUseBagPropButtonState();
    }

    private void OnHandleEventMessage(EventData eventData)
    {
        if (eventData.Args is PressChange)
        {
            UpdateBarShow(gameRoot != null ? gameRoot.Progress : 0f);
            return;
        }

        if (eventData.Args is BagPropChange propChange)
        {
            HandleBagPropChange(propChange);
        }

        if (eventData.Args is GameStart gameStart)
        {
            StartAni(gameStart);
        }
    }

    private void HandleBagPropChange(BagPropChange propChange)
    {
        ItemUI_BagProp targetProp = GetFlyTargetProp(propChange.propID);
        if (targetProp == null)
        {
            return;
        }

        if (propChange.num > 0f)
        {
            if (propChange.propID == CoinPropId && ConsumePendingCoinFlyEventSuppress())
            {
                return;
            }

            PlayMoneyFlyEffect(targetProp);
            return;
        }

        targetProp.SyncBagPropCount();
    }

    public void StartAni(GameStart gameStart)
    {
        startAniText.text = $"{gameStart.levelName}";

        startAni.gameObject.SetActive(true);
        startAni.AnimationState.SetAnimation(0, "jinchang2", false);

        void OnAnimationStateOnComplete(TrackEntry trackEntry)
        {
            startAni.gameObject.SetActive(false);
            YangAudioManager.Instance.PlaySoundAudio("LevelBegin");
            startAni.AnimationState.Complete -= OnAnimationStateOnComplete;
        }

        startAni.AnimationState.Complete += OnAnimationStateOnComplete;
    }

    private ItemUI_BagProp GetFlyTargetProp(int propId)
    {
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

    public void UpdateBarShow(float value)
    {
        // bar.fillAmount = value;
        // barText.text = $"{(int) (value * 100)}%";
        // barEffect.anchoredPosition = new Vector2(value * barEffectParent.rect.width, 0);
        // barEffect.gameObject.SetActive(0.02f < value && value < 0.98f);
    }

    public void ShowTip()
    {
        moneyTipsNode.gameObject.SetActive(true);
        moneyTipsText.text = "鑾峰緱1000閲戝竵";
        // DOTween.Sequence().set
    }

    /// <summary>
    /// 璁剧疆鎸夐挳
    /// </summary>
    public async void SetBtn_OnClick()
    {
        (int id, SettingWindow panel) panel = await UIMonoInstance.OpenPanel<SettingWindow>(GroupType.弹窗2);
        panel.panel.ResetCallBack = () => { gameRoot?.RestartGame(); };
    }

    /// <summary>
    /// 鍥為€€
    /// </summary>
    private bool Prop1Btn_OnClick(int id, bool isFreeUse)
    {
        return gameRoot != null && gameRoot.UseUndoProp(id, isFreeUse);
    }

    /// <summary>
    /// 娓呯悊
    /// </summary>
    private bool Prop2Btn_OnClick(int id, bool isFreeUse)
    {
        return gameRoot != null && gameRoot.UseClearProp(id, isFreeUse);
    }

    /// <summary>
    /// 娲楃墝
    /// </summary>
    private bool Prop3Btn_OnClick(int id, bool isFreeUse)
    {
        return gameRoot != null && gameRoot.UseShuffleProp(id, isFreeUse, GetScreenCenterWorldPosition());
    }

    private Vector3 GetScreenCenterWorldPosition()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform rect = canvas != null ? canvas.transform as RectTransform : transform as RectTransform;
        if (rect != null)
        {
            return rect.TransformPoint(rect.rect.center);
        }

        return transform.position;
    }

    /// <summary>
    /// 鎾斁閲戝竵椋炶鐗规晥
    /// </summary>
    /// <param name="targetProp">鐩爣閬撳叿UI鍏冪礌</param>
    private void PlayMoneyFlyEffect(ItemUI_BagProp targetProp)
    {
        PlayMoneyFlyEffect(targetProp, Vector2.zero);
    }

    /// <summary>
    /// 鎾斁閲戝竵椋炶鐗规晥
    /// </summary>
    /// <param name="targetProp"></param>
    /// <param name="startPosition"></param>
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

    private void PlayCoinFlyEffectFromWorldPosition(Vector3 worldPosition)
    {
        RectTransform effectRoot = GetMoneyFlyEffectRoot();
        if (effectRoot == null || goldProp == null)
        {
            return;
        }

        Camera uiCamera = UIMonoInstance.Instance != null ? UIMonoInstance.Instance.uiCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screenPoint, uiCamera,
                out Vector2 startPosition))
        {
            startPosition = Vector2.zero;
        }

        pendingCoinFlyEventSuppressCount++;
        PlayMoneyFlyEffect(goldProp, startPosition);
    }

    private bool ConsumePendingCoinFlyEventSuppress()
    {
        if (pendingCoinFlyEventSuppressCount <= 0)
        {
            return false;
        }

        pendingCoinFlyEventSuppressCount--;
        return true;
    }

    private RectTransform GetMoneyFlyEffectRoot()
    {
        if (moneyFlyEffectRoot != null)
        {
            moneyFlyEffectRoot.SetAsLastSibling();
            return moneyFlyEffectRoot;
        }

        return transform as RectTransform;
    }

    private Vector2 GetMoneyFlyTargetPosition(RectTransform effectRoot, ItemUI_BagProp targetProp)
    {
        if (effectRoot == null || targetProp == null || targetProp.mImgPropIcon == null)
        {
            return Vector2.zero;
        }

        Camera uiCamera = UIMonoInstance.Instance != null ? UIMonoInstance.Instance.uiCamera : null;
        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(uiCamera, targetProp.mImgPropIcon.transform.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screenPoint, uiCamera,
                out Vector2 localPoint))
        {
            return localPoint;
        }

        return Vector2.zero;
    }

    private Vector2 GetMoneyFlyIconSize(ItemUI_BagProp targetProp)
    {
        RectTransform iconRect = targetProp != null && targetProp.mImgPropIcon != null
            ? targetProp.mImgPropIcon.transform as RectTransform
            : null;
        if (iconRect != null && iconRect.rect.width > 0f && iconRect.rect.height > 0f)
        {
            return iconRect.rect.size;
        }

        return Vector2.one * MoneyFlyIconFallbackSize;
    }

    private Image CreateMoneyFlyIcon(RectTransform parent, Sprite sprite, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject iconObject =
            new GameObject("MoneyFlyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.zero;
        rect.SetAsLastSibling();

        Image icon = iconObject.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.sprite = sprite;
        return icon;
    }

    private void SyncPropWithPunch(ItemUI_BagProp targetProp)
    {
        if (targetProp == null)
        {
            return;
        }

        targetProp.SyncBagPropCount();
        Transform punchTarget = targetProp.mTxtPropCount != null
            ? targetProp.mTxtPropCount.transform
            : targetProp.transform;
        punchTarget.DOKill();
        punchTarget.DOPunchScale(Vector3.one * 0.18f, 0.24f, 1, 0.2f)
            .SetEase(Ease.OutBack)
            .SetTarget(this);
    }

    private void StopMoneyFlyEffect(bool syncInterruptedTarget = false)
    {
        ItemUI_BagProp interruptedTarget = moneyFlyTargetProp;
        if (moneyFlySequence != null)
        {
            moneyFlySequence.Kill();
            moneyFlySequence = null;
        }

        ClearMoneyFlyIcons();
        moneyFlyTargetProp = null;

        if (syncInterruptedTarget)
        {
            SyncPropWithPunch(interruptedTarget);
        }
    }

    private void ClearMoneyFlyIcons()
    {
        for (int i = 0; i < moneyFlyIcons.Count; i++)
        {
            if (moneyFlyIcons[i] != null)
            {
                Destroy(moneyFlyIcons[i]);
            }
        }

        moneyFlyIcons.Clear();
    }

    private bool CanUseUndoProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseUndoProp(id);
    }

    private bool CanUseClearProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseClearProp(id);
    }

    private bool CanUseShuffleProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseShuffleProp(id);
    }

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
    /// 纭繚浜嬩欢鍗＄墖椤圭洰鍒楄〃鐨勬纭€у拰瀹屾暣鎬?    /// 璇ユ柟娉曚細妫€鏌ュ苟鍒濆鍖栦簨浠跺崱鐗囩殑鏍硅妭鐐瑰拰椤圭洰鍒楄〃锛?    /// 绉婚櫎绌哄紩鐢ㄧ殑椤圭洰锛屽苟纭繚鑷冲皯鏈?涓湁鏁堢殑浜嬩欢鍗＄墖椤圭洰
    /// </summary>
    private async void EnsureEventCardItems()
    {
        if (eventCardItemsCreating)
        {
            return;
        }

        // 妫€鏌ヤ簨浠跺崱鐗囬」鐩垪琛ㄦ槸鍚﹀垵濮嬪寲锛岃嫢鏈垵濮嬪寲鍒欏垱寤烘柊鍒楄〃
        if (eventCardItems == null)
        {
            eventCardItems = new List<EventCardItemUI>();
        }

        for (int i = eventCardItems.Count - 1; i >= 0; i--)
        {
            if (eventCardItems[i] == null)
            {
                eventCardItems.RemoveAt(i);
            }
        }

        eventCardItemsCreating = true;
        try
        {
            while (eventCardItems.Count < 3)
            {
                EventCardItemUI temp = await CreateEventCardItem(eventCardRoot, eventCardItems.Count);
                eventCardItems.Add(temp);
            }

            for (int i = 0; i < eventCardItems.Count; i++)
            {
                if (eventCardItems[i] != null)
                {
                    eventCardItems[i].Init(i, SelectEventCard);
                }
            }
        }
        finally
        {
            eventCardItemsCreating = false;
        }

        ApplyEventCardItems();
    }

    private async Task<EventCardItemUI> CreateEventCardItem(RectTransform parent, int index)
    {
        GameObject itemObject = new GameObject($"EventCardItem_{index + 1}", typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(UICustomButton), typeof(EventCardItemUI));
        itemObject.layer = GetUiLayer();
        RectTransform rect = itemObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(198f, 160f);

        Image background = itemObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.11f, 0.1f, 0.92f);

        UICustomButton customButton = itemObject.GetComponent<UICustomButton>();
        customButton.btnImgList = new List<Image> { background };

        Image icon = CreateCardImage(rect);
        TextMeshProUGUI typeText = await CreateCardText(rect, "Type", new Vector2(0f, 52f), 22f, TextAlignmentOptions.Center);
        TextMeshProUGUI titleText =
           await CreateCardText(rect, "Title", new Vector2(0f, 18f), 25f, TextAlignmentOptions.Center);
        TextMeshProUGUI descText =
            await CreateCardText(rect, "Desc", new Vector2(0f, -38f), 18f, TextAlignmentOptions.Center);
        descText.rectTransform.sizeDelta = new Vector2(170f, 58f);

        EventCardItemUI item = itemObject.GetComponent<EventCardItemUI>();
        item.Bind(customButton, icon, titleText, typeText, descText);
        return item;
    }

    private Image CreateCardImage(RectTransform parent)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = GetUiLayer();
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(46f, 46f);
        rect.anchoredPosition = new Vector2(0f, 52f);

        Image image = iconObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.gameObject.SetActive(false);
        return image;
    }

    private async Task<TextMeshProUGUI> CreateCardText(RectTransform parent, string objectName, Vector2 position, float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = GetUiLayer();
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(176f, 32f);
        rect.anchoredPosition = position;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = await ResourceManager.LoadAssetAsync<TMP_FontAsset>("wulinjianghuti SDF");
        text.raycastTarget = false;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static int GetUiLayer()
    {
        int layer = LayerMask.NameToLayer("UI");
        return layer >= 0 ? layer : 0;
    }
    public void RefreshLevelDropdown(IReadOnlyList<TbLevelData> levelDatas)
    {
        if (levelDropdown == null || levelDatas == null)
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

    public void RefreshEventCards(IReadOnlyList<EventCard> shownCards)
    {
        pendingEventCards = shownCards ?? Array.Empty<EventCard>();
        EnsureEventCardItems();
        if (eventCardItemsCreating)
        {
            return;
        }

        ApplyEventCardItems();
    }

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

    private void SelectEventCard(int index)
    {
        gameRoot?.SelectEventCard(index);
    }

    private void OKBtn_OnClick()
    {
        gameRoot?.RestartGame();
    }
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

    private GameRoot EnsureGameRoot()
    {
        if (gameRoot != null)
        {
            return gameRoot;
        }

        gameRoot = GetComponentInChildren<GameRoot>(true);
        if (gameRoot == null)
        {
            gameRoot = gameObject.AddComponent<GameRoot>();
        }

        return gameRoot;
    }
}
