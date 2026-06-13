using System;

public sealed class ReviveWindowData
{
    public ReviveWindowData(Action reviveAction, Action closeAction, int remainingReviveCount, int maxReviveCount)
    {
        ReviveAction = reviveAction;
        CloseAction = closeAction;
        RemainingReviveCount = remainingReviveCount;
        MaxReviveCount = maxReviveCount;
    }

    public Action ReviveAction { get; }
    public Action CloseAction { get; }
    public int RemainingReviveCount { get; }
    public int MaxReviveCount { get; }
}
