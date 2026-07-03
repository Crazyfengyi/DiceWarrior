using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using TMPro;
using UnityEngine;
using YangTools.Scripts.Core;
using YangTools.Scripts.Core.YangUGUI;

public class GameManager : MonoSingleton<GameManager>
{
    public TMP_FontAsset font;
    
    protected override void Awake()
    {
        base.Awake();
        _ = GameManager.Instance;
    }
    
    public void ShowTip(string tip)
    {
        FloatTipWindow.Show(tip);
    }

    #region 通用界面

    private int openMaskNum;

    public async void OpenCommonMaskWindow()
    {
        (int id, CommonMaskWindow panel) panel = await UIMonoInstance.OpenPanel<CommonMaskWindow>(GroupType.Top);
        openMaskNum++;
    }

    public async void CloseCommonMaskWindow()
    {
        openMaskNum--;
        await UniTask.WaitUntil(() => openMaskNum <= 0);
        if (openMaskNum <= 0)
        {
            UIMonoInstance.ClosePanel<CommonMaskWindow>();
        }
    }

    #endregion
}