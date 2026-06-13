using System;
using DG.Tweening;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools;
using YangTools.Scripts.Core.ResourceManager;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public class ItemUI_UseBagProp : MonoBehaviour
{
    private const int DailyUseLimit = 3;

    public GameObject adRoot;
    public GameObject adImage;

    public GameObject numRoot;
    public TextMeshProUGUI numText;

    public Image icon;
    public BagPropId useBagPropId;
    public UICustomButton customButton;

    private YangEventGroup _eventGroup;
    private Func<int, bool> UseBefore { get; set; }
    private Action<int> UseFailed { get; set; }
    private Func<int, bool, bool> UseProp { get; set; }

    private bool isFreeUse;
    private bool? lastCanClick;
    private Vector3 startScale;

    private void Start()
    {
        startScale = transform.localScale;
        customButton.AddListener(OnUseBagPropClick);
        _eventGroup = new YangEventGroup();
        _eventGroup.AddListener<UseBagProp>(ProcessMessage);
        _eventGroup.AddListener<BagPropChange>(ProcessMessage);
    }

    private void OnDestroy()
    {
        _eventGroup?.RemoveAllListener();
    }

    public void Init(Func<int, bool> useBefore, Action<int> useFailed, Func<int, bool, bool> useProp,
        bool _isFreeUse = false)
    {
        if (useBagPropId == 0)
        {
            Debug.LogError($"不存在的道具 useBagPropId:{useBagPropId}");
            return;
        }

        isFreeUse = _isFreeUse;

        UseBefore = useBefore;
        UseProp = useProp;
        UseFailed = useFailed;
        UpdateUIShow();
    }

    private void OnUseBagPropClick()
    {
        if (UseBefore != null && UseBefore.Invoke(useBagPropId) == false)
        {
            UseFailed?.Invoke(useBagPropId);
            UpdateUIShow();
            return;
        }

        Save_PropDailyUse dailyUse = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_PropDailyUse>(true);
        if (!dailyUse.CanUse(useBagPropId, DailyUseLimit))
        {
            FloatTipWindow.Show("今日使用次数已达上限");
            UpdateUIShow();
            return;
        }

        if (isFreeUse)
        {
            if (UseProp?.Invoke(useBagPropId, true) == true)
            {
                dailyUse.RecordUse(useBagPropId);
            }

            UpdateUIShow();
            return;
        }

        var isEnough = BagMgr.Instance.BagPropEnough(useBagPropId, 1);
        if (isEnough)
        {
            if (UseProp?.Invoke(useBagPropId, false) == true)
            {
                BagMgr.Instance.RemoveBagProp(useBagPropId, 1);
                dailyUse.RecordUse(useBagPropId);
            }

            UpdateUIShow();
        }
        else
        {
            OpenPropGetWindow();
        }
    }

    private void ProcessMessage(EventData message)
    {
        UpdateUIShow();
        if (message.Args is BagPropChange data && data.propID == useBagPropId)
        {
            DOTween.Kill(gameObject);
            DOTween.Sequence()
                .Append(gameObject.transform.DOScale(startScale * 1.06f, 0.2f))
                .Append(gameObject.transform.DOScale(startScale, 0.2f))
                .SetEase(Ease.OutBack)
                .SetTarget(gameObject);
        }
    }

    public void RefreshUseState()
    {
        UpdateButtonState();
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUIShow()
    {
        adRoot.SetActive(false);
        numRoot.SetActive(false);
        var tableData = GameTableManager.Instance.Tables.TbItemCategory.GetOrDefault(useBagPropId);
        int bagPropNum = (int) Mathf.Clamp(BagMgr.Instance.GetBagPropCount(useBagPropId), 0, 99);
        if (bagPropNum > 0)
        {
            numRoot.SetActive(true);
            numText.text = bagPropNum.ToString();
        }
        else
        {
            adRoot.SetActive(true);
        }

        ResourceManager.SetImageSprite(icon, tableData.SpriteName);
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (customButton == null || customButton.TargetButton == null)
        {
            return;
        }

        bool canUseByGameState = UseBefore == null || UseBefore.Invoke(useBagPropId);
        bool canUseToday = YangSaveDataManager.Instance.DataCenter
            .GetLocalSave<Save_PropDailyUse>()
            .CanUse(useBagPropId, DailyUseLimit);
        bool canClick = canUseByGameState && canUseToday;

        customButton.TargetButton.interactable = canClick;
        if (lastCanClick != canClick)
        {
            lastCanClick = canClick;
            customButton.SetGray(!canClick);
        }
    }

    private async void OpenPropGetWindow()
    {
        PropGetWindowData data = new PropGetWindowData
        {
            PropId = useBagPropId,
            OnGetSuccess = UpdateUIShow
        };

        await UIMonoInstance.OpenPanel<PropGetWindow>(GroupType.弹窗1, data);
    }

    /// <summary>
    /// 隐藏
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        DOTween.Kill(gameObject);
        transform.DOScale(startScale, 0.5f).SetTarget(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        customButton = transform.GetComponent<UICustomButton>();
    }
#endif
}