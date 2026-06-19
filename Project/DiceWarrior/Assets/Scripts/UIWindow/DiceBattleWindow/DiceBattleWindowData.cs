using System;
using cfg;
using YangTools.Scripts.Core.YangUGUI;

public sealed class DiceBattleWindowData : DefaultUGUIDataBase
{
    public DiceBattleWindowData(DiceBattle battleConfig, Action<bool> onBattleFinished)
    {
        BattleConfig = battleConfig;
        OnBattleFinished = onBattleFinished;
    }

    public DiceBattle BattleConfig { get; }
    public Action<bool> OnBattleFinished { get; }
}
