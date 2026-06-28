using System;
using GameMain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DiceBattleStatusItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private Image iconImage;

    private Action pointerEnterCallback;
    private Action pointerExitCallback;

    /// <summary>
    /// 初始化状态图标悬停回调。
    /// </summary>
    public void Init(Action onPointerEnter, Action onPointerExit)
    {
        pointerEnterCallback = onPointerEnter;
        pointerExitCallback = onPointerExit;
        ValidateBindings();
        if (button != null && button.TargetButton != null)
        {
            button.TargetButton.interactable = false;
        }
    }

    /// <summary>
    /// 刷新状态图标显示。
    /// </summary>
    public void Refresh(Sprite sprite, bool visible)
    {
        ValidateBindings();
        gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = sprite == null ? new Color(0.26f, 0.43f, 0.76f, 1f) : Color.white;
        }
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

    /// <summary>
    /// 校验状态图标预制体引用是否完整。
    /// </summary>
    private void ValidateBindings()
    {
        if (button == null || iconImage == null)
        {
            Debug.LogError($"DiceBattleStatusItemUI 引用未绑定完整：{name}", this);
        }
    }
}
