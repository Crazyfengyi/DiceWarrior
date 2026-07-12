using System;
using System.Collections.Generic;
using cfg;
using YangTools.Scripts.Core.YangUGUI;

public sealed class DiceEnhanceWindowData : DefaultUGUIDataBase
{
    /// <summary>
    /// 创建强化页打开参数。
    /// </summary>
    public DiceEnhanceWindowData(EnhancementTypeData enhancementData, IReadOnlyList<EquippedDiceSlotData> diceSlots,
        int initialSelectedDiceIndex, Action<int, int?> onConfirm, Action onCancel)
    {
        EnhancementData = enhancementData;
        DiceSlots = diceSlots;
        InitialSelectedDiceIndex = initialSelectedDiceIndex;
        OnConfirm = onConfirm;
        OnCancel = onCancel;
    }

    public EnhancementTypeData EnhancementData { get; }
    public IReadOnlyList<EquippedDiceSlotData> DiceSlots { get; }
    public int InitialSelectedDiceIndex { get; }
    public Action<int, int?> OnConfirm { get; }
    public Action OnCancel { get; }
}
