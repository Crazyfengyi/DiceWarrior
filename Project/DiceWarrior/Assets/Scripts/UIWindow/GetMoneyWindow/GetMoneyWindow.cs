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
using Manager;
using UnityEngine;  
using UnityEngine.UI;
using TMPro;
using YangTools;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public class GetMoneyWindow : UGUIPanelBase<DefaultUGUIDataBase> 
{  
	private const string AccountPlaceholder = "please enter your account";

	[Sirenix.OdinInspector.FoldoutGroup("组件")]
	public UICustomButton mBtnClose; // 关闭按钮组件

	[Sirenix.OdinInspector.FoldoutGroup("组件")]
	public UICustomButton IDBtn; 

	[Sirenix.OdinInspector.FoldoutGroup("组件")]
	public TextMeshProUGUI accountText;

	private EventInfo accountIdListener;

	/// <summary>
	/// 显示方法
	/// </summary>
	public override void OnOpen(object userData)
	{
		base.OnOpen(userData);
        
		mBtnClose.AddListener(Close_OnClick);
		IDBtn.AddListener(ID_OnClick);
		RemoveAccountIdListener();
		accountIdListener = gameObject.AddEventListener<AccountIdChange>(OnHandleEventMessage);
		RefreshAccountText();
	}

	public override void OnClose(bool isShutdown, object userData)
	{
		RemoveAccountIdListener();
		base.OnClose(isShutdown, userData);
	}
   
	/// <summary>
	/// 关闭按钮
	/// </summary>
	private void Close_OnClick()
	{
		CloseSelfPanel();
	}
	/// <summary>
	/// ID
	/// </summary>
	private void ID_OnClick()
	{
		UIMonoInstance.OpenPanel<AccountWindow>(GroupType.弹窗2);
	}

	private void OnHandleEventMessage(EventData eventData)
	{
		if (eventData.Args is AccountIdChange accountChange)
		{
			RefreshAccountText(accountChange.accountId);
		}
	}

	private void RefreshAccountText(string accountId = null)
	{
		if (accountText == null)
		{
			return;
		}

		if (accountId == null)
		{
			Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>();
			accountId = saveData.accountId;
		}

		accountText.text = string.IsNullOrWhiteSpace(accountId) ? AccountPlaceholder : accountId;
	}

	private void RemoveAccountIdListener()
	{
		if (accountIdListener == null)
		{
			return;
		}

		Extend.RemoveEventListener(accountIdListener);
		accountIdListener = null;
	}
}
