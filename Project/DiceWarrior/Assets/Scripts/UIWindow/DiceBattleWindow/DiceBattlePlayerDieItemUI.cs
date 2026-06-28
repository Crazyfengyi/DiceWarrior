using System;
using System.Collections.Generic;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DiceBattlePlayerDieItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private UICustomButton throwButton;
    [SerializeField] private UICustomButton rerollButton;
    [SerializeField] private List<DiceBattleDieFaceCellUI> faceCells = new List<DiceBattleDieFaceCellUI>();

    private int dieIndex;
    private Action<int> throwCallback;
    private Action<int> rerollCallback;
    private Action<int> hoverEnterCallback;
    private Action hoverExitCallback;

    /// <summary>
    /// 初始化单颗玩家骰子行的交互。
    /// </summary>
    public void Init(int index, Action<int> onThrow, Action<int> onReroll, Action<int> onHoverEnter, Action onHoverExit)
    {
        dieIndex = index;
        throwCallback = onThrow;
        rerollCallback = onReroll;
        hoverEnterCallback = onHoverEnter;
        hoverExitCallback = onHoverExit;

        ValidateBindings();
        throwButton?.AddListener(OnThrowClicked);
        rerollButton?.AddListener(OnRerollClicked);
    }

    /// <summary>
    /// 刷新单颗玩家骰子行显示。
    /// </summary>
    public void Refresh(DiceBattleModel.PlayerDieState dieState, bool canSingleReroll, bool allowAction)
    {
        ValidateBindings();

        bool hasDie = dieState != null && !dieState.IsEmpty;
        gameObject.SetActive(true);

        if (nameText != null)
        {
            nameText.text = hasDie ? dieState.DieName : "空";
        }

        if (rangeText != null)
        {
            rangeText.text = hasDie ? dieState.GetRangeText() : "-";
        }

        if (background != null)
        {
            background.color = !hasDie
                ? new Color(0.7f, 0.7f, 0.72f, 0.7f)
                : dieState.HasThrown
                    ? new Color(0.58f, 0.58f, 0.6f, 1f)
                    : new Color(0.92f, 0.92f, 0.94f, 1f);
        }

        for (int i = 0; i < faceCells.Count; i++)
        {
            bool visible = hasDie && i < dieState.Faces.Count;
            int faceValue = visible ? dieState.Faces[i] : 0;
            bool highlighted = visible && dieState.HasThrown && dieState.CurrentFaceIndex == i;
            faceCells[i].Refresh(faceValue, visible, highlighted);
        }

        RefreshButtonState(throwButton, allowAction && hasDie && !dieState.HasThrown, true);
        RefreshButtonState(rerollButton, allowAction && hasDie && dieState.HasThrown, canSingleReroll);
    }

    /// <summary>
    /// 处理鼠标移入。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverEnterCallback?.Invoke(dieIndex);
    }

    /// <summary>
    /// 处理鼠标移出。
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        hoverExitCallback?.Invoke();
    }

    /// <summary>
    /// 处理投出按钮点击。
    /// </summary>
    private void OnThrowClicked()
    {
        throwCallback?.Invoke(dieIndex);
    }

    /// <summary>
    /// 处理重投按钮点击。
    /// </summary>
    private void OnRerollClicked()
    {
        rerollCallback?.Invoke(dieIndex);
    }

    /// <summary>
    /// 刷新按钮显示与可点击状态。
    /// </summary>
    private static void RefreshButtonState(UICustomButton button, bool visible, bool interactable)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(visible);
        if (!visible || button.TargetButton == null)
        {
            return;
        }

        button.TargetButton.interactable = interactable;
        button.SetGray(!interactable);
    }

    /// <summary>
    /// 校验本行的预制体引用是否完整。
    /// </summary>
    private void ValidateBindings()
    {
        if (background == null || nameText == null || rangeText == null || throwButton == null || rerollButton == null)
        {
            Debug.LogError($"DiceBattlePlayerDieItemUI 引用未绑定完整：{name}", this);
        }

        if (faceCells.Count == 0)
        {
            Debug.LogError($"DiceBattlePlayerDieItemUI 骰面格子引用为空：{name}", this);
        }
    }
}
