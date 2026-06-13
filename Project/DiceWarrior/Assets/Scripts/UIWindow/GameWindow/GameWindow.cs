using System;
using System.Collections.Generic;
using DG.Tweening;
using GameMain;
using Manager;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YangTools;
using YangTools.Scripts.Core.YangAudio;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;
using EventData = YangTools.EventData;
using Sequence = DG.Tweening.Sequence;

/// <summary>
/// 游戏界面
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

    public UICustomButton checkOutBtn;
    public UICustomButton setBtn;

    public Image bar;
    public TextMeshProUGUI barText;
    public RectTransform barEffectParent;
    public RectTransform barEffect;

    public RectTransform moneyTipsNode;
    public TextMeshProUGUI moneyTipsText;

    public SkeletonGraphic startAni;
    public TextMeshProUGUI startAniText;
    
    [SerializeField] private UICustomButton GMButton;
    public GameObject gmNode;
    
    [SerializeField] private Transform contentRoot;
    [SerializeField] private UICustomButton okButton;
    [SerializeField] private UICustomButton clearButton;
    [SerializeField] private UICustomButton addButton;
    [SerializeField] private UICustomButton jumpButton;
    public TMP_Dropdown levelDropdown; 
    
    [SerializeField] private RectTransform moneyFlyEffectRoot;

    public GameRoot gameRoot;
    public List<ItemUI_UseBagProp> useBagPropsBtns;

    private EventInfo pressChangeListener;
    private EventInfo bagPropChangeListener;
    private EventInfo gameStartListener;
    
    private Sequence moneyFlySequence;
    private ItemUI_BagProp moneyFlyTargetProp;
    private int pendingCoinFlyEventSuppressCount;
    private readonly List<GameObject> moneyFlyIcons = new List<GameObject>();

    private void OnDestroy()
    {
        StopMoneyFlyEffect();
    }

    private void CheckOutBtn_OnClick()
    {
        UIMonoInstance.OpenPanel<GetMoneyWindow>(GroupType.弹窗1);
    }

    private void ClearBtn_OnClick()
    {
        YangSaveDataManager.Instance.ClearSaveData();
        FloatTipWindow.Show("清除存档成功");
    }
    
    private void AddBtn_OnClick()
    {
        BagMgr.Instance.AddBagProp(3,1000);
        BagMgr.Instance.AddBagProp(4,1000);
        BagMgr.Instance.AddBagProp(5,1000);
    }

    private void CoinBtnClick()
    {
        UIMonoInstance.OpenPanel<CoinGetWindow>(GroupType.弹窗1);
    }

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GMButton.AddListener(GMButton_OnClick);
        okButton.AddListener(OKBtn_OnClick);
        clearButton.AddListener(ClearBtn_OnClick);
        addButton.AddListener(AddBtn_OnClick);
        jumpButton.AddListener(JumpBtn_OnClick);

        List<TMP_Dropdown.OptionData> temp = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < GameTableManager.Instance.Tables.TBLevelCategory.DataList.Count; i++)
        {
            var levelData = GameTableManager.Instance.Tables.TBLevelCategory.DataList[i];
            temp.Add(new TMP_Dropdown.OptionData()
            {
                text = levelData.Id.ToString()
            });
        }
        levelDropdown.options = temp;
        
        useBagPropsBtns[0].Init(CanUseUndoProp, PropUseFailed, Prop1Btn_OnClick, false);
        useBagPropsBtns[1].Init(CanUseClearProp, PropUseFailed, Prop2Btn_OnClick, false);
        useBagPropsBtns[2].Init(CanUseShuffleProp, PropUseFailed, Prop3Btn_OnClick, false);

        moneyProp.RefreshBagPropUI(new ItemData_BagProp(MoneyPropId, BagMgr.Instance.GetBagPropCount(MoneyPropId)),
            false);
        goldProp.RefreshBagPropUI(new ItemData_BagProp(CoinPropId, BagMgr.Instance.GetBagPropCount(CoinPropId)), false);
        goldProp.clickBtn.AddListener(CoinBtnClick);
        setBtn.AddListener(SetBtn_OnClick);
        checkOutBtn.AddListener(CheckOutBtn_OnClick);
        
        UpdateBarShow();
        pressChangeListener = gameObject.AddEventListener<PressChange>(OnHandleEventMessage);
        bagPropChangeListener = gameObject.AddEventListener<BagPropChange>(OnHandleEventMessage);
        gameStartListener = gameObject.AddEventListener<GameStart>(OnHandleEventMessage);
        
        gameRoot.SetNormalMatchCoinRewardHandler(PlayCoinFlyEffectFromWorldPosition);
        gameRoot.Initialize();
        YangAudioManager.Instance.PlayBGM("level_bgm");
    }

    private void GMButton_OnClick()
    {
        gmNode.SetActive(!gmNode.activeSelf);
    }

    public override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
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

        gameRoot.SetNormalMatchCoinRewardHandler(null);
        YangAudioManager.Instance.StopBGM();
    }

    private void Update()
    {
        UpdateBarShow();
        RefreshUseBagPropButtonState();
    }

    private void OnHandleEventMessage(EventData eventData)
    {
        if (eventData.Args is PressChange)
        {
            UpdateBarShow();
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
        startAni.AnimationState.SetAnimation( 0,"jinchang2", false);

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

    public void UpdateBarShow()
    {
        float value = gameRoot.Progress;
        bar.fillAmount = value;
        barText.text = $"{(int) (value * 100)}%";
        barEffect.anchoredPosition = new Vector2(value * barEffectParent.rect.width, 0);
        barEffect.gameObject.SetActive(0.02f < value && value < 0.98f);
    }

    public void ShowTip()
    {
        moneyTipsNode.gameObject.SetActive(true);
        moneyTipsText.text = "获得1000金币";
        // DOTween.Sequence().set
    }

    /// <summary>
    /// 设置按钮
    /// </summary>
    public async void SetBtn_OnClick()
    { 
        (int id, SettingWindow panel) panel = await UIMonoInstance.OpenPanel<SettingWindow>(GroupType.弹窗2);
        panel.panel.ResetCallBack = () =>
        {
            gameRoot.RestartGame();
        };
    }

    /// <summary>
    /// 回退
    /// </summary>
    private bool Prop1Btn_OnClick(int id, bool isFreeUse)
    {
        if (gameRoot.inputLocked || gameRoot.HasActiveWaitingAreaOperation())
        {
            FloatTipWindow.Show("暂时无法使用");
            return false;
        }

        if (gameRoot.waitingBalls.Count == 0)
        {
            FloatTipWindow.Show("等待区为空");
            return false;
        }

        return gameRoot != null && gameRoot.UseUndoProp(GetUseBagPropWorldPosition(0));
    }

    /// <summary>
    /// 清理
    /// </summary>
    private bool Prop2Btn_OnClick(int id, bool isFreeUse)
    {
        if (gameRoot.inputLocked || gameRoot.HasActiveWaitingAreaOperation())
        {
            FloatTipWindow.Show("暂时无法使用");
            return false;
        }

        if (gameRoot.waitingBalls.Count == 0)
        {
            FloatTipWindow.Show("等待区为空");
            return false;
        }

        return gameRoot != null && gameRoot.UseClearProp();
    }

    /// <summary>
    /// 洗牌
    /// </summary>
    private bool Prop3Btn_OnClick(int id, bool isFreeUse)
    {
        if (gameRoot.inputLocked || gameRoot.HasActiveWaitingAreaOperation())
        {
            FloatTipWindow.Show("暂时无法使用");
            return false;
        }

        return gameRoot != null && gameRoot.UseShuffleProp(GetScreenCenterWorldPosition());
    }

    private Vector3 GetUseBagPropWorldPosition(int index)
    {
        if (useBagPropsBtns != null && index >= 0 && index < useBagPropsBtns.Count &&
            useBagPropsBtns[index] != null)
        {
            return useBagPropsBtns[index].transform.position;
        }

        return transform.position;
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
    /// 播放金币飞行特效
    /// </summary>
    /// <param name="targetProp">目标道具UI元素</param>
    private void PlayMoneyFlyEffect(ItemUI_BagProp targetProp)
    {
        PlayMoneyFlyEffect(targetProp, Vector2.zero);
    }

    /// <summary>
    /// 播放金币飞行特效
    /// </summary>
    /// <param name="targetProp"></param>
    /// <param name="startPosition"></param>
    private void PlayMoneyFlyEffect(ItemUI_BagProp targetProp, Vector2 startPosition)
    {
        YangAudioManager.Instance.PlaySoundAudio("Collect_Coins");
        // 停止当前正在播放的金币飞行特效，如果目标道具不同
        StopMoneyFlyEffect(moneyFlyTargetProp != null && moneyFlyTargetProp != targetProp);
        moneyFlyTargetProp = targetProp;

        // 获取特效根节点
        RectTransform effectRoot = GetMoneyFlyEffectRoot();
        // 检查特效根节点、目标道具及其图标是否有效
        if (effectRoot == null || targetProp == null || targetProp.mImgPropIcon == null)
        {
            // 如果无效，同步道具状态
            SyncPropWithPunch(targetProp);
            return;
        }

        // 设置初始位置、目标位置、金币图标和大小
        Vector2 targetPosition = GetMoneyFlyTargetPosition(effectRoot, targetProp);
        Sprite moneySprite = targetProp.mImgPropIcon.sprite;
        Vector2 iconSize = GetMoneyFlyIconSize(targetProp);

        // 创建DOTween序列
        moneyFlySequence = DOTween.Sequence().SetTarget(this);
        // 循环创建多个金币图标
        for (int i = 0; i < MoneyFlyIconCount; i++)
        {
            // 创建金币图标
            Image icon = CreateMoneyFlyIcon(effectRoot, moneySprite, iconSize, startPosition);
            if (icon == null)
            {
                continue;
            }

            // 将金币图标添加到列表
            moneyFlyIcons.Add(icon.gameObject);
            // 计算延迟时间
            float delay = i * MoneyFlyDelayStep;
            // 生成随机偏移量
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * MoneyFlySpawnRadius;
            Vector2 scatterPosition = startPosition + randomOffset;

            RectTransform iconTransform = icon.transform as RectTransform;
            // 添加缩放动画
            moneyFlySequence.Insert(delay, iconTransform.DOScale(Vector3.one, MoneyFlyScatterDuration)
                .SetEase(Ease.OutBack));
            // 添加位置动画
            moneyFlySequence.Insert(delay, iconTransform.DOAnchorPos(scatterPosition, MoneyFlyScatterDuration)
                .SetEase(Ease.OutCubic));
            // 添加向目标位置移动的动画
            moneyFlySequence.Insert(delay + MoneyFlyScatterDuration + MoneyFlyWaitDuration,
                iconTransform.DOAnchorPos(targetPosition, MoneyFlyDuration).SetEase(Ease.InCubic));
            // 添加缩小动画
            moneyFlySequence.Insert(delay + MoneyFlyScatterDuration + MoneyFlyWaitDuration,
                iconTransform.DOScale(Vector3.one * 0.45f, MoneyFlyDuration).SetEase(Ease.InCubic));
        }

        // 设置动画完成后的回调
        moneyFlySequence.OnComplete(() =>
        {
            // 清除金币图标
            ClearMoneyFlyIcons();
            // 同步道具状态
            SyncPropWithPunch(targetProp);
            // 重置序列和目标道具
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
        return gameRoot != null && gameRoot.CanUseUndoProp();
    }

    private bool CanUseClearProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseClearProp();
    }

    private bool CanUseShuffleProp(int id)
    {
        return gameRoot != null && gameRoot.CanUseShuffleProp();
    }

    private void PropUseFailed(int id)
    {
        FloatTipWindow.Show("暂时无法使用");
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

    private void OKBtn_OnClick()
    {
        gameRoot?.RestartGame();
    }
    
    private void JumpBtn_OnClick()
    {
        string target = levelDropdown.captionText.text;
        int levelID = int.Parse(target);
        if (GameTableManager.Instance.Tables.TBLevelCategory.DataMap.ContainsKey(levelID))
        {
            Save_GameData gameData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
            gameData.currentLevelId = levelID;
            gameRoot.ApplyCurrentLevelConfig();
            gameRoot?.RestartGame(); 
            FloatTipWindow.Show("跳转成功");
        }
        else
        {
            FloatTipWindow.Show("该关卡不存在");
        }
    }
}
