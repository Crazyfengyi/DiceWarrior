using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DiceBattleHoverTargetUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action pointerEnterCallback;
    private Action pointerExitCallback;

    /// <summary>
    /// 初始化悬停回调。
    /// </summary>
    public void Init(Action onPointerEnter, Action onPointerExit)
    {
        pointerEnterCallback = onPointerEnter;
        pointerExitCallback = onPointerExit;
    }

    /// <summary>
    /// 处理鼠标移入。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerEnterCallback?.Invoke();
    }

    /// <summary>
    /// 处理鼠标移出。
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        pointerExitCallback?.Invoke();
    }
}
