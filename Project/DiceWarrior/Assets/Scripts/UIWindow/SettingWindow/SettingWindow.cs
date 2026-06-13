using System;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.YangUGUI;

/// <summary>
/// 设置界面
/// </summary>
public class SettingWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton maskBtn;
    
    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton mBtnClose; // 关闭按钮组件

    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton mGmBtn; // GM(游戏管理员)按钮组件

    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public TextMeshProUGUI mTextUUid; // 显示UUID的文本组件

    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton copyBtn; // 复制按钮组件

    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton mainBtn; // 主菜单按钮组件

    [Sirenix.OdinInspector.FoldoutGroup("组件")]
    public UICustomButton reStartBtn; // 重新开始按钮组件

    /// <summary>
    /// 返回按钮
    /// </summary>
    public Action BackCallBack { get; set; }

    /// <summary>
    /// 重新开始按钮
    /// </summary>
    public Action ResetCallBack { get; set; }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    public Action CloseCallBack { get; set; }

    /// <summary>
    /// 显示方法
    /// </summary>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        BackCallBack = null;
        ResetCallBack = null;
        CloseCallBack = null;
        
        mTextUUid.text = $"UUID:{PlatformMgr.UserId}";
        //mGmBtn.gameObject.SetActive(GameEntrance.Instance.openGmWindow);
        //if (!GameEntrance.Instance.openGmWindow)
        //{
          //  mGmBtn.gameObject.SetActive(GameEntrance.IsShowGm(PlatformMgr.UserId));
        //}
        // mGmBtn.AddListener(() =>
        // {
        //     var panel = UIWindowMgr.Instance.GetWindow<SquarePuzzleWindow>();
        //     if (panel)
        //     {
        //         UIWindowMgr.Instance.OpenWindowAsync<GmWindow>(panel.mSquarePuzzleGameRoot.runtimeData);
        //     }
        //     else
        //     {
        //         UIWindowTool.ShowPromptBox("找不到数据");
        //     }
        // });
        // copyBtn.AddListener(() =>
        // {
        //     PlatformMgr.Instance.GetCopyToClipboard(mTextUUid.text,
        //         (result) => { UIWindowTool.ShowPromptBox(result ? "复制成功" : "复制失败"); });
        // });

        mainBtn.AddListener(Back_OnClick);
        reStartBtn.AddListener(Reset_OnClick);
        mBtnClose.AddListener(Close_OnClick);
        //maskBtn.AddListener(Close_OnClick);
    }
    /// <summary>
    /// 返回按钮
    /// </summary>
    private void Back_OnClick()
    {
        //BackCallBack?.Invoke();
        //CloseSelfPanel();
        Application.OpenURL("http://www.baidu.com");
    }
    /// <summary>
    /// 重新开始
    /// </summary>
    private void Reset_OnClick()
    {
        CloseSelfPanel();
        ResetCallBack?.Invoke();
    }
    /// <summary>
    /// 关闭按钮
    /// </summary>
    private void Close_OnClick()
    {
        CloseSelfPanel();
        CloseCallBack?.Invoke();
    }
}