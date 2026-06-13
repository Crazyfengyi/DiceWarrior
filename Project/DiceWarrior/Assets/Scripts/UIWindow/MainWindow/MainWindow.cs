/*
 *Copyright(C) 2020 by xybt 
 *All rights reserved.
 *Author:PC-20260301BNFU 
 *UnityVersion：2022.3.62f3c1 
 *创建时间:2026-06-10 
 */  
using System;
using System.Collections;
using GameMain;
using UnityEngine;  
using UnityEngine.UI;
using TMPro;
using YangTools;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public class MainWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    public UICustomButton startBtn;

    public TextMeshProUGUI levelText;
    
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        startBtn.AddListener(OnClickStartBtn);
        
        int saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>().currentLevelId;
        levelText.text = GameTableManager.Instance.Tables.TBLevelCategory.GetOrDefault(saveData).LevelName;
    }

    private void OnClickStartBtn()
    {
        
    }
} 