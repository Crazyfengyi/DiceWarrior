using System;
using System.Collections.Generic;
using cfg;
using YangTools.Scripts.Core.YangUGUI;

public sealed class DiceBattleWindowData : DefaultUGUIDataBase
{
    /// <summary>
    /// 创建骰子战斗窗口数据。
    /// </summary>
    public DiceBattleWindowData(EnemyData enemyData, IReadOnlyList<EquippedDiceSlotData> playerDiceSlots,
        Action<bool> onBattleFinished)
    {
        EnemyData = enemyData;
        PlayerDiceSlots = playerDiceSlots;
        OnBattleFinished = onBattleFinished;
    }

    public EnemyData EnemyData { get; }
    public IReadOnlyList<EquippedDiceSlotData> PlayerDiceSlots { get; }
    public Action<bool> OnBattleFinished { get; }
}
