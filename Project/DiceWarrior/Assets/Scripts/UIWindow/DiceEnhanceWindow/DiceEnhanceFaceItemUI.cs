using System;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DiceEnhanceFaceItemUI : MonoBehaviour
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI valueText;

    private int faceIndex;
    private Action<int> clickCallback;

    /// <summary>
    /// 绑定预制体中的控件引用。
    /// </summary>
    public void Bind(UICustomButton bindButton, Image bindBackground, TextMeshProUGUI bindValueText)
    {
        button = bindButton;
        background = bindBackground;
        valueText = bindValueText;
    }

    /// <summary>
    /// 初始化点击回调。
    /// </summary>
    public void Init(int index, Action<int> onClick)
    {
        faceIndex = index;
        clickCallback = onClick;
        if (button == null)
        {
            button = GetComponent<UICustomButton>();
        }

        if (button != null)
        {
            button.AddListener(OnClick);
        }
    }

    /// <summary>
    /// 刷新骰面格子显示。
    /// </summary>
    public void Refresh(int value, bool selected, bool visible, bool interactable)
    {
        gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        if (valueText != null)
        {
            valueText.text = value.ToString();
        }

        if (background != null)
        {
            background.color = selected
                ? new Color(0.45f, 0.76f, 0.23f, 1f)
                : new Color(0.28f, 0.46f, 0.79f, 1f);
        }

        if (button != null && button.TargetButton != null)
        {
            button.TargetButton.interactable = interactable;
            button.SetGray(!interactable);
        }
    }

    /// <summary>
    /// 处理点击事件。
    /// </summary>
    private void OnClick()
    {
        clickCallback?.Invoke(faceIndex);
    }
}
