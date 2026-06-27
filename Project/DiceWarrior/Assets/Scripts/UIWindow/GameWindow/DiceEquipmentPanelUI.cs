using System.Collections.Generic;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DiceEquipmentPanelUI : MonoBehaviour
{
    [SerializeField] private UICustomButton closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI listText;

    public void Bind(UICustomButton bindCloseButton, TextMeshProUGUI bindTitleText,
        TextMeshProUGUI bindDescText, TextMeshProUGUI bindListText)
    {
        closeButton = bindCloseButton;
        titleText = bindTitleText;
        descText = bindDescText;
        listText = bindListText;
    }

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

        if (descText != null)
        {
            descText.text = selectedDice == null || selectedDice.IsEmpty
                ? "\u8be5\u69fd\u4f4d\u6682\u672a\u88c5\u5907\u9ab0\u5b50"
                : $"\u5df2\u88c5\u5907 {selectedDice.Name}\uff0c\u5171 {selectedDice.DiceSides} \u9762";
        }

        if (listText != null)
        {
            listText.text = BuildDiceListText(diceSlots);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static string BuildDiceListText(IReadOnlyList<EquippedDiceSlotData> diceSlots)
    {
        if (diceSlots == null || diceSlots.Count == 0)
        {
            return "\u6682\u65e0\u88c5\u5907\u9ab0\u5b50";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < diceSlots.Count; i++)
        {
            EquippedDiceSlotData dice = diceSlots[i];
            string name = dice == null || dice.IsEmpty ? "\u7a7a" : dice.Name;
            builder.Append(i + 1).Append(". ").Append(name);
            if (dice != null && !dice.IsEmpty)
            {
                builder.Append("  D").Append(dice.DiceSides);
            }

            if (i < diceSlots.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}
