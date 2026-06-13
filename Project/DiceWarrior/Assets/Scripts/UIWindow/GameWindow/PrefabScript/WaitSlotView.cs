using System;
using System.Collections.Generic;
using GameMain;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.ResourceManager;

/// <summary>
/// WaitSlotView类，用于管理等待槽位的视图显示
/// </summary>
public sealed class WaitSlotView : MonoBehaviour
{
    //背景图片
    public Image bg;
    public Image lockIcon;
    public UICustomButton lockButton;
    
    public List<Sprite> spriteList;
    //索引
    public int Index { get; private set; }
    //自动备选区
    private bool isAuto;
    private bool isLocked;
    private Action<WaitSlotView> onLockedClick;
    public RectTransform RectTransform { get; private set; }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="index">槽位索引</param>
    public void Init(int index)
    {
        Index = index;
        RectTransform = transform as RectTransform; // 获取当前对象的RectTransform组件
        lockButton?.AddListener(HandleLockClicked);
        SetLocked(false);
        SetOccupied(false); // 初始化为未占用状态
    }

    /// <summary>
    /// 设置索引
    /// </summary>
    /// <param name="index">要设置的索引值</param>
    public void SetIndex(int index)
    {
        Index = index;
    }

    /// <summary>
    /// 设置是自动备选区
    /// </summary>
    public void SetIsAutoBackup(bool _isAuto)
    {
        isAuto = _isAuto;
        if (isAuto)
        {
            SetLocked(false);
        }
    }

    public void SetLockedClick(Action<WaitSlotView> callback)
    {
        onLockedClick = callback;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked && !isAuto;
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(isLocked);
        }
        bg.sprite = locked ? spriteList[1] : spriteList[0];
    }

    /// <summary>
    /// 设置槽位占用状态
    /// </summary>
    public void SetOccupied(bool occupied)
    {
        // 修改背景图片的透明度
        Color color = bg.color;
        color.a = occupied ? 1f : 0.88f; // 根据占用状态设置不同的透明度
        if (isLocked) color.a = 0.45f;
        if (isAuto) color.a = 0;
        bg.color = color;
    }

    /// <summary>
    /// 设置背景图片的透明度
    /// </summary>
    /// <param name="alpha">透明度值（0-1之间）</param>
    public void SetAlpha(float alpha)
    {
        // 修改背景图片的透明度
        Color oldColor = bg.color;
        oldColor.a = alpha;
        if (isLocked) oldColor.a = 0.45f;
        if (isAuto) oldColor.a = 0;
        bg.color = oldColor;
    }

    private void HandleLockClicked()
    {
        if (!isLocked)
        {
            return;
        }
        onLockedClick?.Invoke(this);
    }
}
