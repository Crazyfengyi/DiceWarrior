using System.Collections.Generic;
using System.Text;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using YangTools.Scripts.Core.YangUGUI;

/// <summary>
/// 骰子装备面板UI类
/// </summary>
public sealed class DiceEquipmentPanelUI : MonoBehaviour
{
    // UI组件引用
    [SerializeField] private TextMeshProUGUI titleText; // 标题文本
    [SerializeField] private TextMeshProUGUI descText; // 描述文本
    [SerializeField] private TextMeshProUGUI detailText; // 详细信息文本
    [SerializeField] private RectTransform probabilityRoot; // 概率列表根节点
    [SerializeField] private List<ProbabilityItemUI> probabilityItems = new List<ProbabilityItemUI>(); // 概率项列表

    /// <summary>
    /// 初始化弹窗交互。
    /// </summary>
    public void Init()
    {
        // 初始状态为隐藏
        Hide();
    }

    /// <summary>
    /// 显示当前选中骰子的详细信息。
    /// </summary>
    /// <param name="diceSlots">骰子槽位数据列表</param>
    /// <param name="selectedIndex">选中的骰子索引</param>
    public void Show(IReadOnlyList<EquippedDiceSlotData> diceSlots, int selectedIndex)
    {
        gameObject.SetActive(true);

        // 获取选中的骰子数据
        EquippedDiceSlotData selectedDice = diceSlots != null && selectedIndex >= 0 && selectedIndex < diceSlots.Count
            ? diceSlots[selectedIndex]
            : null;

        // 更新标题文本
        if (titleText != null)
        {
            titleText.text = selectedDice == null || selectedDice.IsEmpty
                ? "\u9ab0\u5b50\u88c5\u5907"
                : selectedDice.Name;
        }

        // 如果没有选中骰子或骰子为空，显示空状态
        if (selectedDice == null || selectedDice.IsEmpty)
        {
            RefreshEmptyState();
            return;
        }

        // 更新骰子基础信息和概率列表
        RefreshBasicInfo(selectedDice);
        RefreshProbabilityItems(selectedDice);
    }

    /// <summary>
    /// 显示当前选中骰子的详细信息，并将面板放到鼠标下方。
    /// </summary>
    /// <param name="diceSlots">骰子槽位数据列表</param>
    /// <param name="selectedIndex">选中的骰子索引</param>
    /// <param name="screenPosition">鼠标屏幕坐标</param>
    public void Show(IReadOnlyList<EquippedDiceSlotData> diceSlots, int selectedIndex, Vector2 screenPosition)
    {
        Show(diceSlots, selectedIndex);
        SetScreenPosition(screenPosition);
    }

    /// <summary>
    /// 根据鼠标屏幕坐标更新面板位置，并保持面板位于鼠标下方。
    /// </summary>
    /// <param name="screenPosition">鼠标屏幕坐标</param>
    public void SetScreenPosition(Vector2 screenPosition)
    {
        RectTransform panelRoot = transform as RectTransform;
        RectTransform parentRoot = panelRoot != null ? panelRoot.parent as RectTransform : null;
        if (panelRoot == null || parentRoot == null)
        {
            return;
        }

        Camera uiCamera = UIMonoInstance.Instance != null ? UIMonoInstance.Instance.uiCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRoot, screenPosition, uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float belowOffset = panelRoot.rect.height * (1f - panelRoot.pivot.y) + 12f;
        Vector2 targetPosition = localPoint + Vector2.down * belowOffset;
        float minX = parentRoot.rect.xMin + panelRoot.rect.width * panelRoot.pivot.x;
        float maxX = parentRoot.rect.xMax - panelRoot.rect.width * (1f - panelRoot.pivot.x);
        float minY = parentRoot.rect.yMin + panelRoot.rect.height * panelRoot.pivot.y;
        float maxY = parentRoot.rect.yMax - panelRoot.rect.height * (1f - panelRoot.pivot.y);
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        panelRoot.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// 隐藏弹窗。
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 刷新空槽显示状态。
    /// </summary>
    private void RefreshEmptyState()
    {
        // 更新描述和详细信息文本
        if (descText != null)
        {
            descText.text = "\u8be5\u69fd\u4f4d\u6682\u672a\u88c5\u5907\u9ab0\u5b50";
        }

        if (detailText != null)
        {
            detailText.text = "\u6682\u65e0\u9ab0\u5b50\u57fa\u7840\u4fe1\u606f\n\u6682\u65e0\u6982\u7387\u6570\u636e";
        }

        // 隐藏概率列表
        if (probabilityRoot != null)
        {
            probabilityRoot.gameObject.SetActive(false);
        }

        // 隐藏所有概率项
        HideAllProbabilityItems();
    }

    /// <summary>
    /// 刷新骰子的基础信息文案。
    /// </summary>
    /// <param name="selectedDice">选中的骰子数据</param>
    private void RefreshBasicInfo(EquippedDiceSlotData selectedDice)
    {
        // 更新描述文本
        if (descText != null)
        {
            descText.text = $"\u5df2\u88c5\u5907 {selectedDice.Name}\uff0c\u5171 {selectedDice.DiceSides} \u9762";
        }

        // 更新详细信息文本
        if (detailText != null)
        {
            detailText.text = BuildDetailText(selectedDice);
        }
    }

    /// <summary>
    /// 刷新概率列表显示。
    /// </summary>
    /// <param name="selectedDice">选中的骰子数据</param>
    private void RefreshProbabilityItems(EquippedDiceSlotData selectedDice)
    {
        // 显示概率列表
        if (probabilityRoot != null)
        {
            probabilityRoot.gameObject.SetActive(true);
        }

        // 隐藏所有概率项
        HideAllProbabilityItems();
        // 构建概率数据
        List<KeyValuePair<int, float>> probabilities = BuildProbabilityData(selectedDice);
        int showCount = Mathf.Min(probabilities.Count, probabilityItems.Count);
        // 检查概率项数量是否足够
        if (probabilities.Count > probabilityItems.Count)
        {
            Debug.LogError(
                $"DiceEquipmentPanelUI probability item count is not enough. need={probabilities.Count}, has={probabilityItems.Count}");
        }

        // 显示概率项
        for (int i = 0; i < showCount; i++)
        {
            if (probabilityItems[i] != null)
            {
                probabilityItems[i].Refresh(probabilities[i].Key, probabilities[i].Value);
            }
        }
    }

    /// <summary>
    /// 隐藏全部概率条目。
    /// </summary>
    private void HideAllProbabilityItems()
    {
        for (int i = 0; i < probabilityItems.Count; i++)
        {
            if (probabilityItems[i] != null)
            {
                probabilityItems[i].Hide();
            }
        }
    }

    /// <summary>
    /// 构建当前骰子的基础信息文本。
    /// </summary>
    /// <param name="dice">骰子数据</param>
    /// <returns>构建的详细信息文本</returns>
    private static string BuildDetailText(EquippedDiceSlotData dice)
    {
        if (dice == null || dice.IsEmpty || dice.Faces == null || dice.Faces.Count == 0)
        {
            return "\u6682\u65e0\u9ab0\u5b50\u57fa\u7840\u4fe1\u606f";
        }

        StringBuilder builder = new StringBuilder();
        // 添加当前面值信息
        builder.Append("\u5f53\u524d\u9762\u503c\uff1a");
        for (int i = 0; i < dice.Faces.Count; i++)
        {
            builder.Append(dice.Faces[i]);
            if (i < dice.Faces.Count - 1)
            {
                builder.Append(" / ");
            }
        }

        // 计算并添加最小值和最大值
        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = 0; i < dice.Faces.Count; i++)
        {
            min = Mathf.Min(min, dice.Faces[i]);
            max = Mathf.Max(max, dice.Faces[i]);
        }

        builder.AppendLine();
        builder.Append("\u6781\u9650\u533a\u95f4\uff1a").Append(min).Append("~").Append(max);
        return builder.ToString();
    }

    /// <summary>
    /// 构建按点数聚合后的概率数据。
    /// </summary>
    /// <param name="dice">骰子数据</param>
    /// <returns>构建的概率数据列表</returns>
    private static List<KeyValuePair<int, float>> BuildProbabilityData(EquippedDiceSlotData dice)
    {
        List<KeyValuePair<int, float>> result = new List<KeyValuePair<int, float>>();
        if (dice == null || dice.IsEmpty || dice.Faces == null || dice.Faces.Count == 0)
        {
            return result;
        }

        // 统计每个点数出现的次数
        Dictionary<int, int> countMap = new Dictionary<int, int>();
        for (int i = 0; i < dice.Faces.Count; i++)
        {
            int value = dice.Faces[i];
            countMap.TryGetValue(value, out int count);
            countMap[value] = count + 1;
        }

        // 对点数进行排序
        List<int> values = new List<int>(countMap.Keys);
        values.Sort();
        // 计算每个点数的概率
        for (int i = 0; i < values.Count; i++)
        {
            int value = values[i];
            float percent = countMap[value] / (float)dice.Faces.Count;
            result.Add(new KeyValuePair<int, float>(value, percent));
        }

        return result;
    }
}

/// <summary>
/// 概率项UI类，用于显示单个点数的概率信息
/// </summary>
[System.Serializable]
public sealed class ProbabilityItemUI
{
    // UI元素引用
    public GameObject root; // 根节点
    public TextMeshProUGUI valueText; // 数值文本
    public TextMeshProUGUI percentText; // 百分比文本
    public Image barFill; // 概率条填充

    /// <summary>
    /// 刷新单条概率信息。
    /// </summary>
    /// <param name="value">点数值</param>
    /// <param name="percent">出现概率</param>
    public void Refresh(int value, float percent)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        if (valueText != null)
        {
            valueText.text = value.ToString();
        }

        if (percentText != null)
        {
            percentText.text = $"{percent * 100f:0.#}%";
        }

        if (barFill != null)
        {
            RectTransform rect = barFill.transform as RectTransform;
            if (rect != null)
            {
                // 根据概率值调整概率条宽度
                rect.anchorMax = new Vector2(Mathf.Clamp01(percent), 1f);
            }
        }
    }

    /// <summary>
    /// 隐藏单条概率信息。
    /// </summary>
    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }
}
