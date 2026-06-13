/*
 *Copyright(C) 2020 by xybt
 *All rights reserved.
 *Author:PC-20260301BNFU
 *UnityVersion：2022.3.62f3c1
 *创建时间:2026-06-06
 */

using System;
using System.Collections;
using GameMain;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YangTools;
using YangTools.Scripts.Core.YangUGUI;

public class UnLockWaitAreaWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton mBtnClose; // 关闭按钮组件

    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton getBtn;

    private UnLockWaitAreaWindowData data;
    /// <summary>
    /// 显示方法
    /// </summary>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        data = userData as UnLockWaitAreaWindowData;

        mBtnClose.AddListener(Close_OnClick);
        getBtn.AddListener(Get_OnClick);
    }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    private void Close_OnClick()
    {
        data?.closeCallBack?.Invoke();
        CloseSelfPanel();
    }

    /// <summary>
    /// 获取金币
    /// </summary>
    private void Get_OnClick()
    {
        PlatformMgr.Instance.LookAd((result) =>
        {
            if (result)
            {
                data?.getCallBack?.Invoke();
                CloseSelfPanel();
            }
            else
            {
                FloatTipWindow.Show("观看广告失败");
            }
        }, "等待区解锁");
    }
}

public class UnLockWaitAreaWindowData : DefaultUGUIDataBase
{
    public Action closeCallBack;
    public Action getCallBack;
} 