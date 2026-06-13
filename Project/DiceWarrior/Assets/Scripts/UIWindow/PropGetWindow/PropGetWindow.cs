using System;
using cfg;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools;
using YangTools.Scripts.Core.ResourceManager;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public class PropGetWindow : UGUIPanelBase<PropGetWindowData>
{
    private const int CoinPropId = 2;
    private const int RewardCount = 1;

    public TextMeshProUGUI LimetText;
    public TextMeshProUGUI priceText;
    
    public UICustomButton closeButton;
    public UICustomButton buyButton;
    public UICustomButton adButton;
    public UICustomButton coinButton;
    public Image propIcon;

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        RefreshShow();
        
        closeButton.AddListener(() => CloseSelfPanel());
        adButton.AddListener(AdButton_OnClick);
        buyButton.AddListener(BuyButton_OnClick);
        coinButton.AddListener(CoinButton_OnClick);
    }

    private void RefreshShow()
    {
        if (windowData == null)
        {
            Debug.LogError("PropGetWindowData is null.");
            CloseSelfPanel();
            return;
        }

        Item item = GameTableManager.Instance.Tables.TbItemCategory.GetOrDefault(windowData.PropId);
        if (item == null)
        {
            Debug.LogError($"不存在的道具:{windowData.PropId}");
            CloseSelfPanel();
            return;
        }
        Save_PropDailyUse dailyUse = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_PropDailyUse>(true);
        int num = dailyUse.GetRemainCount(windowData.PropId);
        LimetText.text =  $"LIMIT: <color=#F94E4E>{num}/{Save_PropDailyUse.DefaultDailyLimit}</color>";
        priceText.text = $"{item.Price}";
        ResourceManager.SetImageSprite(propIcon, item.SpriteName);
    }

    private void AdButton_OnClick()
    {
        if (windowData == null)
        {
            return;
        }

        PlatformMgr.Instance.LookAd((result) =>
        {
            if (result)
            {
                AddPropAndClose("获得道具+1");
            }
            else
            {
                FloatTipWindow.Show("观看广告失败");
            }
        }, $"道具获取_{windowData.PropId}");
    }

    private void BuyButton_OnClick()
    {
        if (windowData == null)
        {
            return;
        }

        var item = GameTableManager.Instance.Tables.TbItemCategory.GetOrDefault(windowData.PropId);
        if (item == null)
        {
            return;
        }

        int price = Mathf.Max(0, item.Price);
        if (!BagMgr.Instance.BagPropEnough(CoinPropId, price, false))
        {
            FloatTipWindow.Show("金币不足");
            return;
        }

        if (price > 0)
        {
            BagMgr.Instance.RemoveBagProp(CoinPropId, price);
        }

        AddPropAndClose("获得道具+1");
    }

    private void AddPropAndClose(string tip)
    {
        BagMgr.Instance.AddBagProp(windowData.PropId, RewardCount);
        FloatTipWindow.Show(tip);
        windowData.OnGetSuccess?.Invoke();
        CloseSelfPanel();
    }
    
    private void CoinButton_OnClick()
    {
        PlatformMgr.Instance.LookAd((result) =>
        {
            if (result)
            {
                BagMgr.Instance.AddBagProp(2, 100);
                FloatTipWindow.Show("金币+100");
            }
            else
            {
                FloatTipWindow.Show("观看广告失败");
            }
        }, "金币获取");
    }
}

public class PropGetWindowData : UGUIDataBase
{
    public int PropId;
    public Action OnGetSuccess;
}
