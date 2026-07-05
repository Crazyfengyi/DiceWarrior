using System.Collections.Generic;
using System.Text;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DiceEquipmentPanelUI : MonoBehaviour
{
    [System.Serializable]
    private sealed class ProbabilityItemUI
    {
        public GameObject root;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI percentText;
        public Image barFill;

        /// <summary>
        /// 刷新单条概率信息。
        /// </summary>
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

    [SerializeField] private UICustomButton closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private RectTransform probabilityRoot;
    [SerializeField] private List<ProbabilityItemUI> probabilityItems = new List<ProbabilityItemUI>();

    /// <summary>
    /// 绑定基础引用。
    /// </summary>
    public void Bind(UICustomButton bindCloseButton, TextMeshProUGUI bindTitleText,
        TextMeshProUGUI bindDescText, TextMeshProUGUI bindDetailText)
    {
        closeButton = bindCloseButton;
        titleText = bindTitleText;
        descText = bindDescText;
        detailText = bindDetailText;
    }

    /// <summary>
    /// 初始化弹窗交互。
    /// </summary>
    public void Init()
    {
        if (closeButton == null)
        {
            closeButton = GetComponentInChildren<UICustomButton>(true);
        }

        if (closeButton != null)
        {
            closeButton.AddListener(Hide);
        }

        Hide();
    }

    /// <summary>
    /// 显示当前选中骰子的详细信息。
    /// </summary>
    public void Show(IReadOnlyList<EquippedDiceSlotData> diceSlots, int selectedIndex)
    {
        gameObject.SetActive(true);

        EquippedDiceSlotData selectedDice = diceSlots != null && selectedIndex >= 0 && selectedIndex < diceSlots.Count
            ? diceSlots[selectedIndex]
            : null;

        if (titleText != null)
        {
            titleText.text = selectedDice == null || selectedDice.IsEmpty
                ? "\u9ab0\u5b50\u88c5\u5907"
                : selectedDice.Name;
        }

        if (selectedDice == null || selectedDice.IsEmpty)
        {
            RefreshEmptyState();
            return;
        }

        RefreshBasicInfo(selectedDice);
        RefreshProbabilityItems(selectedDice);
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
        if (descText != null)
        {
            descText.text = "\u8be5\u69fd\u4f4d\u6682\u672a\u88c5\u5907\u9ab0\u5b50";
        }

        if (detailText != null)
        {
            detailText.text = "\u6682\u65e0\u9ab0\u5b50\u57fa\u7840\u4fe1\u606f\n\u6682\u65e0\u6982\u7387\u6570\u636e";
        }

        if (probabilityRoot != null)
        {
            probabilityRoot.gameObject.SetActive(false);
        }

        HideAllProbabilityItems();
    }

    /// <summary>
    /// 刷新骰子的基础信息文案。
    /// </summary>
    private void RefreshBasicInfo(EquippedDiceSlotData selectedDice)
    {
        if (descText != null)
        {
            descText.text = $"\u5df2\u88c5\u5907 {selectedDice.Name}\uff0c\u5171 {selectedDice.DiceSides} \u9762";
        }

        if (detailText != null)
        {
            detailText.text = BuildDetailText(selectedDice);
        }
    }

    /// <summary>
    /// 刷新概率列表显示。
    /// </summary>
    private void RefreshProbabilityItems(EquippedDiceSlotData selectedDice)
    {
        if (probabilityRoot != null)
        {
            probabilityRoot.gameObject.SetActive(true);
        }

        HideAllProbabilityItems();
        List<KeyValuePair<int, float>> probabilities = BuildProbabilityData(selectedDice);
        int showCount = Mathf.Min(probabilities.Count, probabilityItems.Count);
        if (probabilities.Count > probabilityItems.Count)
        {
            Debug.LogError($"DiceEquipmentPanelUI probability item count is not enough. need={probabilities.Count}, has={probabilityItems.Count}");
        }

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
    private static string BuildDetailText(EquippedDiceSlotData dice)
    {
        if (dice == null || dice.IsEmpty || dice.Faces == null || dice.Faces.Count == 0)
        {
            return "\u6682\u65e0\u9ab0\u5b50\u57fa\u7840\u4fe1\u606f";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("\u5f53\u524d\u9762\u503c\uff1a");
        for (int i = 0; i < dice.Faces.Count; i++)
        {
            builder.Append(dice.Faces[i]);
            if (i < dice.Faces.Count - 1)
            {
                builder.Append(" / ");
            }
        }

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
    private static List<KeyValuePair<int, float>> BuildProbabilityData(EquippedDiceSlotData dice)
    {
        List<KeyValuePair<int, float>> result = new List<KeyValuePair<int, float>>();
        if (dice == null || dice.IsEmpty || dice.Faces == null || dice.Faces.Count == 0)
        {
            return result;
        }

        Dictionary<int, int> countMap = new Dictionary<int, int>();
        for (int i = 0; i < dice.Faces.Count; i++)
        {
            int value = dice.Faces[i];
            countMap.TryGetValue(value, out int count);
            countMap[value] = count + 1;
        }

        List<int> values = new List<int>(countMap.Keys);
        values.Sort();
        for (int i = 0; i < values.Count; i++)
        {
            int value = values[i];
            float percent = countMap[value] / (float) dice.Faces.Count;
            result.Add(new KeyValuePair<int, float>(value, percent));
        }

        return result;
    }
}
