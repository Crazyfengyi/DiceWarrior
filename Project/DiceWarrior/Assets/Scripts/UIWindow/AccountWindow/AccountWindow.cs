/*
 *Copyright(C) 2020 by xybt 
 *All rights reserved.
 *Author:PC-20260301BNFU 
 *UnityVersion：2022.3.62f3c1 
 *创建时间:2026-06-06 
 */  
using System;
using System.Collections;
using System.Collections.Generic;
using GameMain;
using UnityEngine;  
using UnityEngine.UI;
using TMPro;
using YangTools;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public class AccountWindow : UGUIPanelBase<DefaultUGUIDataBase> 
{  
	public List<UICustomToggle> mToggleList;
	public TMP_InputField mInputField;
	
	[Sirenix.OdinInspector.FoldoutGroup("组件")]
	public UICustomButton mBtnClose; // 关闭按钮组件
	[Sirenix.OdinInspector.FoldoutGroup("组件")]
	public UICustomButton OKBtn; 

	/// <summary>
	/// 显示方法
	/// </summary>
	public override void OnOpen(object userData)
	{
		base.OnOpen(userData);
        
		mBtnClose.AddListener(Close_OnClick);
		OKBtn.AddListener(OKBtn_OnClick);

		Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>();
		if (mInputField != null)
		{
			mInputField.onEndEdit.RemoveListener(OnInputEndEdit);
			mInputField.text = saveData.accountId ?? string.Empty;
			mInputField.onEndEdit.AddListener(OnInputEndEdit);
		}

		for (int i = 0; i < mToggleList.Count; i++)
		{
			mToggleList[i].SetToggle(saveData.lastSelectPayPassIndex == i);
			mToggleList[i].OnToggleClickCallback = OnToggleClick;
		}
	}

	public override void OnClose(bool isShutdown, object userData)
	{
		if (mInputField != null)
		{
			mInputField.onEndEdit.RemoveListener(OnInputEndEdit);
		}

		base.OnClose(isShutdown, userData);
	}

	private void OnToggleClick(UICustomToggle lickTarget,bool obj)
	{
		for (int i = 0; i < mToggleList.Count; i++)
		{
			mToggleList[i].SetToggle(lickTarget == mToggleList[i]);
		}
		Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
		saveData.lastSelectPayPassIndex = mToggleList.IndexOf(lickTarget);
	}

	private void OnInputEndEdit(string value)
	{
		SaveAccountId(value);
	}

	private bool SaveAccountId(string value)
	{
		string accountId = value?.Trim();
		if (string.IsNullOrEmpty(accountId))
		{
			return false;
		}

		Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
		if (saveData.accountId == accountId)
		{
			return false;
		}

		saveData.accountId = accountId;
		new AccountIdChange {accountId = accountId}.SendEvent();
		return true;
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
	private void OKBtn_OnClick()
	{
		SaveAccountId(mInputField != null ? mInputField.text : string.Empty);
		CloseSelfPanel();
	}
}
