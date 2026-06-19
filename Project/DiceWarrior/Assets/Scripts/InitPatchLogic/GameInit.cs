/*
 *Copyright(C) 2020 by Test 
 *All rights reserved.
 *Author:WIN-VJ19D9AB7HB 
 *UnityVersion：6000.0.23f1c1 
 *创建时间:2025-06-28 
 */  
using System;
using System.Collections;
using GameMain;
using Sirenix.OdinInspector;
using UnityEngine;  
using YangTools;
using YooAsset;

public class GameInit : MonoBehaviour
{
    public static GameInit Instance;
    public EPlayMode playMode;
    [LabelText("版本号(修改AS)")] [GUIColor("red")]
    public string appVersion;
    
    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
    }
    
    private IEnumerator Start()
    {
        //初始化资源系统
        YooAssets.Initialize();

#if !UNITY_EDITOR
        playMode = EPlayMode.OfflinePlayMode;
#endif
        //开始补丁更新流程
        PatchManager.Create("DefaultPackage", playMode);
        PatchManager.Start();
        yield return null;
        GameSceneManager.Instance.Load("Init");
    }

    private void Update()
    {
        PatchManager.Update();
    }

    public void InitializeAfterManagers()
    {
    }
} 