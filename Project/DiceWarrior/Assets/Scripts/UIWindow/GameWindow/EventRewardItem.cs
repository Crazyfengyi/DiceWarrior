using System;
using cfg;
using TMPro;
using UnityEngine;

/// <summary>
/// 事件奖励物体，保存奖励数据并负责防止重复领取。
/// </summary>
public sealed class EventRewardItem : MonoBehaviour
{
    public TextMeshProUGUI text;
    private RewardItemData rewardItemData;
    private Action<EventRewardItem> clickCallback;
    private bool claimed;

    /// <summary>
    /// 获取当前奖励数据。
    /// </summary>
    public RewardItemData RewardItemData => rewardItemData;

    /// <summary>
    /// 初始化奖励数据和点击回调。
    /// </summary>
    public void Initialize(RewardItemData data, Action<EventRewardItem> onClick)
    {
        rewardItemData = data;
        clickCallback = onClick;
        claimed = false;
        text.text = data.Name.ToString();
    }

    /// <summary>
    /// 尝试标记奖励已领取，并触发点击回调。
    /// </summary>
    public void HandleClick()
    {
        if (claimed || rewardItemData == null)
        {
            return;
        }

        claimed = true;
        clickCallback?.Invoke(this);
    }
}
