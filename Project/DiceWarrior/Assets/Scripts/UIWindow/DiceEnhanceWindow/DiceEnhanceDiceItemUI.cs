using System;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DiceEnhanceDiceItemUI : MonoBehaviour
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI facesText;
    [SerializeField] private TextMeshProUGUI descText;

    private int diceIndex;
    private Action<int> clickCallback;

    /// <summary>
    /// 绑定预制体中的控件引用。
    /// </summary>
    public void Bind(UICustomButton bindButton, Image bindBackground, TextMeshProUGUI bindTitleText,
        TextMeshProUGUI bindFacesText, TextMeshProUGUI bindDescText)
    {
        button = bindButton;
        background = bindBackground;
        titleText = bindTitleText;
        facesText = bindFacesText;
        descText = bindDescText;
    }

    /// <summary>
    /// 初始化点击回调。
    /// </summary>
    public void Init(int index, Action<int> onClick)
    {
        diceIndex = index;
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
    /// 刷新骰子项显示。
    /// </summary>
    public void Refresh(EquippedDiceSlotData data, bool selected, bool interactable)
    {
        if (titleText != null)
        {
            titleText.text = data == null || data.IsEmpty ? "空槽" : data.Name;
        }

        if (facesText != null)
        {
            facesText.text = data == null || data.IsEmpty ? "-" : string.Join(" ", data.Faces);
        }

        if (descText != null)
        {
            descText.text = data == null || data.IsEmpty ? "该槽位没有骰子" : $"区间 {GetMin(data)}~{GetMax(data)}";
        }

        if (background != null)
        {
            background.color = !interactable
                ? new Color(0.21f, 0.24f, 0.28f, 0.65f)
                : selected
                    ? new Color(0.45f, 0.76f, 0.23f, 1f)
                    : new Color(0.28f, 0.46f, 0.79f, 0.96f);
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
        clickCallback?.Invoke(diceIndex);
    }

    /// <summary>
    /// 获取当前骰子的最小面值。
    /// </summary>
    private static int GetMin(EquippedDiceSlotData data)
    {
        int min = int.MaxValue;
        for (int i = 0; i < data.Faces.Count; i++)
        {
            min = Mathf.Min(min, data.Faces[i]);
        }

        return min == int.MaxValue ? 0 : min;
    }

    /// <summary>
    /// 获取当前骰子的最大面值。
    /// </summary>
    private static int GetMax(EquippedDiceSlotData data)
    {
        int max = int.MinValue;
        for (int i = 0; i < data.Faces.Count; i++)
        {
            max = Mathf.Max(max, data.Faces[i]);
        }

        return max == int.MinValue ? 0 : max;
    }
}
