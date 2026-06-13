using System;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.YangUGUI;

public sealed class ReviveWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    public TextMeshProUGUI limitText;
    public UICustomButton coinReviveButton;
    public UICustomButton adReviveButton;
    public UICustomButton closeButton;
    public UICustomButton getCoinButton;

    private ReviveWindowData reviveData;

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        reviveData = userData as ReviveWindowData;

        coinReviveButton.AddListener(CoinReviveButton_OnClick);
        adReviveButton.AddListener(AdReviveButton_OnClick);
        closeButton.AddListener(OnClose_OnClick);
        getCoinButton.AddListener(GetCoinButton_OnClick);
        UpdateUIShow();
    }

    public override void OnClose(bool isShutdown, object userData)
    {
        reviveData = null;
        base.OnClose(isShutdown, userData);
    }

    public void UpdateUIShow()
    {
        int remainingCount = reviveData != null ? Mathf.Max(0, reviveData.RemainingReviveCount) : 0;
        int maxCount = reviveData != null ? Mathf.Max(0, reviveData.MaxReviveCount) : 0;
        if (limitText != null)
        {
            limitText.text = $"LIMIT:<color=#F94E4E>{remainingCount}/{maxCount}</color>";
        }

        bool canRevive = remainingCount > 0;
        var haveCoin = BagMgr.Instance.GetBagPropCount(2) >= 100;
        SetButtonInteractable(coinReviveButton, canRevive &&　haveCoin);
        SetButtonInteractable(adReviveButton, canRevive);
    }

    private void CoinReviveButton_OnClick()
    {
        if (!CanRevive())
        {
            return;
        }

        if (BagMgr.Instance.GetBagPropCount(2) >= 100)
        {
            BagMgr.Instance.AddBagProp(2, -100);
            reviveData?.ReviveAction?.Invoke();
            CloseSelfPanel();
        }
        else
        {
            FloatTipWindow.Show("金币不足");
        }
    }

    private void AdReviveButton_OnClick()
    {
        if (!CanRevive())
        {
            return;
        }

        PlatformMgr.Instance.LookAd((result) =>
        {
            if (result)
            {
                reviveData?.ReviveAction?.Invoke();
                CloseSelfPanel();
            }
            else
            {
                FloatTipWindow.Show("观看广告失败");
            }
        }, "广告复活");
    }

    private void OnClose_OnClick()
    {
        Action giveUpAction = reviveData?.CloseAction;
        CloseSelfPanel();
        giveUpAction?.Invoke();
    }

    private void GetCoinButton_OnClick()
    {
        PlatformMgr.Instance.LookAd((result) =>
        {
            if (result)
            {
                BagMgr.Instance.AddBagProp(2, 100);
                FloatTipWindow.Show("金币+100");
                UpdateUIShow();
            }
            else
            {
                FloatTipWindow.Show("观看广告失败");
            }
        }, "金币获取");
    }

    private bool CanRevive()
    {
        return reviveData != null && reviveData.RemainingReviveCount > 0;
    }

    private static void SetButtonInteractable(UICustomButton button, bool interactable)
    {
        if (button != null && button.TargetButton != null)
        {
            button.TargetButton.interactable = interactable;
            button.TargetButton.image.material = interactable ? null : UIMonoInstance.Instance.gray;
        }
    }
}