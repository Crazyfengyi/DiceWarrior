using System;
using System.Collections.Generic;
using cfg;
using cfg.eventcard;
using GameMain;
using UnityEngine;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;
using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// GameRoot类是一个密封的单例类，继承自MonoBehaviour，用于管理游戏的核心逻辑和状态。
/// </summary>
public sealed class GameRoot : MonoBehaviour
{
    // 私有字段
    private readonly EventCardDeck eventCardDeck = new EventCardDeck(); // 事件卡牌库
    private readonly List<EquippedDiceSlotData> equippedDiceSlots = new List<EquippedDiceSlotData>(); // 装备的骰子槽位数据

    private GameWindow view; // 游戏窗口视图
    private bool eventWindowOpening; // 事件窗口是否正在打开
    private float progress; // 游戏进度
    private int playerHp; // 玩家生命值
    private int playerMaxHp; // 玩家最大生命值

    // 公共属性
    public float Progress => progress; // 游戏进度属性
    public IReadOnlyList<EventCard> ShownEventCards => eventCardDeck.ShownCards; // 显示的事件卡牌
    public int DrawPileCount => eventCardDeck.DrawPileCount; // 抽牌堆数量
    public int DiscardPileCount => eventCardDeck.DiscardPileCount; // 弃牌堆数量
    public int CurrentLevelId => eventCardDeck.CurrentLevelId; // 当前关卡ID
    public int PlayerHp => playerHp; // 玩家生命值属性
    public int PlayerMaxHp => playerMaxHp; // 玩家最大生命值属性
    public IReadOnlyList<EquippedDiceSlotData> EquippedDiceSlots => equippedDiceSlots; // 装备的骰子槽位

    /// <summary>
    /// 初始化主玩法根控制器。
    /// </summary>
    public void Initialize(GameWindow gameWindow)
    {
        view = gameWindow;
        eventWindowOpening = false;
        InitializePlaceholderPlayerState();
        InitializeDefaultDiceSlots();
        ApplyCurrentLevelConfig();
        //InitializeLevelDropdown();
        InitializeEventCards();
        RefreshRouteHud();
    }

    /// <summary>
    /// 释放窗口引用和运行时状态。
    /// </summary>
    public void Dispose()
    {
        view = null;
        eventWindowOpening = false;
    }

    /// <summary>
    /// 重开当前局并恢复默认状态。
    /// </summary>
    public void RestartGame()
    {
        InitializePlaceholderPlayerState();
        InitializeDefaultDiceSlots();
        ApplyCurrentLevelConfig();
        InitializeEventCards();
        RefreshRouteHud();
    }

    /// <summary>
    /// 应用当前关卡配置。
    /// </summary>
    public void ApplyCurrentLevelConfig()
    {
        progress = 0f;
        view?.UpdateBarShow(progress);
    }

    /// <summary>
    /// 跳转到指定关卡并重开。
    /// </summary>
    public bool JumpToLevel(int levelId)
    {
        // if (GameTableManager.Instance?.Tables?.TBLevelCategory == null ||
        //     !GameTableManager.Instance.Tables.TBLevelCategory.DataMap.ContainsKey(levelId))
        // {
        //     FloatTipWindow.Show("\u8be5\u5173\u5361\u4e0d\u5b58\u5728");
        //     return false;
        // }
        //
        // Save_GameData gameData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
        // gameData.currentLevelId = levelId;
        // FloatTipWindow.Show("\u8df3\u8f6c\u6210\u529f");
        // RestartGame();
        return true;
    }

    /// <summary>
    /// 处理事件卡选择。
    /// </summary>
    public void SelectEventCard(int index)
    {
        if (view != null && view.IsEventCardTransitionPlaying)
        {
            return;
        }

        EventCard selectedCard = eventCardDeck.GetShownCard(index);
        if (selectedCard == null)
        {
            return;
        }

        if (selectedCard.CardType == EEventCardType.Battle)
        {
            OpenDiceBattle(index, selectedCard);
            return;
        }

        if (selectedCard.DiceEnhanceId > 0)
        {
            OpenDiceEnhance(index, selectedCard);
            return;
        }

        CompleteEventCard(index);
        FloatTipWindow.Show($"\u9009\u62e9\u4e86\uff1a{selectedCard.Name}");
    }

    /// <summary>
    /// 打开失败结算窗口。
    /// </summary>
    public async void ForceLoseFromEventBattle()
    {
        LoseWindowData data = new LoseWindowData
        {
            RestartAction = RestartGame
        };

        await UIMonoInstance.OpenPanel<LoseWindow>(GroupType.弹窗1, data);
    }

    /// <summary>
    /// 对整颗骰子应用强化。
    /// </summary>
    public void ApplyWholeDiceEnhance(int diceIndex, EnhancementTypeData enhancementData)
    {
        if (!TryGetDiceSlot(diceIndex, out EquippedDiceSlotData dice) || enhancementData == null)
        {
            return;
        }

        if (DiceEnhancePreviewModel.IsWholeDiceEnhancement(enhancementData))
        {
            dice.ApplyWholeDiceDelta(DiceEnhancePreviewModel.GetValueDelta(enhancementData));
        }

        RefreshRouteHud();
    }

    /// <summary>
    /// 对单个骰面应用强化。
    /// </summary>
    public void ApplySingleFaceEnhance(int diceIndex, int faceIndex, EnhancementTypeData enhancementData)
    {
        if (!TryGetDiceSlot(diceIndex, out EquippedDiceSlotData dice) || enhancementData == null)
        {
            return;
        }

        if (!DiceEnhancePreviewModel.IsWholeDiceEnhancement(enhancementData))
        {
            dice.ApplySingleFaceDelta(faceIndex, DiceEnhancePreviewModel.GetValueDelta(enhancementData));
        }

        RefreshRouteHud();
    }

    // /// <summary>
    // /// 初始化关卡下拉框内容。
    // /// </summary>
    // private void InitializeLevelDropdown()
    // {
    //     if (GameTableManager.Instance?.Tables?.TBLevelCategory == null)
    //     {
    //         return;
    //     }
    //
    //     view?.RefreshLevelDropdown(GameTableManager.Instance.Tables.TBLevelCategory.DataList);
    // }

    /// <summary>
    /// 初始化事件卡牌库与候选区。
    /// </summary>
    private void InitializeEventCards()
    {
        TBEventCardCategory category = GameTableManager.Instance?.Tables?.TBEventCardCategory;
        if (category == null)
        {
            Debug.LogError("TBEventCardCategory is null.");
            view?.RefreshEventCards(eventCardDeck.ShownCards);
            RefreshRouteHud();
            return;
        }

        Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>();
        int currentLevelId = saveData == null ? 1 : saveData.currentLevelId;
        eventCardDeck.Initialize(category.DataList, currentLevelId);
        RefreshEventCards();
        GameStart temp = new GameStart();
        temp.levelName = "未知";
        temp.SendEvent();
    }

    /// <summary>
    /// 刷新事件卡和路线 HUD。
    /// </summary>
    private void RefreshEventCards()
    {
        view?.RefreshEventCards(eventCardDeck.ShownCards);
        RefreshRouteHud();
    }

    /// <summary>
    /// 提交事件卡并触发卡牌过渡动画。
    /// </summary>
    private void CompleteEventCard(int cardIndex)
    {
        EventCard selectedCard = eventCardDeck.GetShownCard(cardIndex);
        List<RewardItemData> rewardItems = BuildEventCardRewards(selectedCard);
        if (rewardItems.Count > 0 && view != null)
        {
            eventCardDeck.CommitSelectedCard(cardIndex);
            GameWindow rewardView = view;
            rewardView.ShowEventRewards(rewardItems, () =>
            {
                rewardView.CompleteEventCardTransition(cardIndex, RefreshEventCards);
            });
            return;
        }

        if (view == null)
        {
            eventCardDeck.CommitSelectedCard(cardIndex);
            GrantEventRewards(rewardItems);
            RefreshEventCards();
            return;
        }

        view.CompleteEventCardTransition(cardIndex, () =>
        {
            eventCardDeck.CommitSelectedCard(cardIndex);
            GrantEventRewards(rewardItems);
            RefreshEventCards();
        });
    }

    /// <summary>
    /// 发放奖励列表中的全部奖励。
    /// </summary>
    private void GrantEventRewards(IReadOnlyList<RewardItemData> rewardItems)
    {
        if (rewardItems == null)
        {
            return;
        }

        for (int i = 0; i < rewardItems.Count; i++)
        {
            GrantEventReward(rewardItems[i]);
        }
    }

    /// <summary>
    /// 根据事件卡配置解析奖励表，保留奖励项中的重复奖励。
    /// </summary>
    private static List<RewardItemData> BuildEventCardRewards(EventCard card)
    {
        List<RewardItemData> rewardItems = new List<RewardItemData>();
        if (card == null || card.RewardList == null || card.RewardList.Count == 0)
        {
            return rewardItems;
        }

        Tables tables = GameTableManager.Instance?.Tables;
        if (tables == null || tables.RewardDataCategory == null || tables.RewardItemDataCategory == null)
        {
            Debug.LogWarning($"事件卡奖励表未加载，无法解析奖励。cardId={card.Id}");
            return rewardItems;
        }

        int rewardDataId = SelectWeightedRewardId(card.RewardList);
        if (rewardDataId <= 0)
        {
            Debug.LogWarning($"事件卡奖励权重无效，无法选择奖励表。cardId={card.Id}");
            return rewardItems;
        }

        RewardData rewardData = tables.RewardDataCategory.GetOrDefault(rewardDataId);
        if (rewardData == null || rewardData.RewardList == null)
        {
            Debug.LogWarning($"奖励表不存在。rewardDataId={rewardDataId}, cardId={card.Id}");
            return rewardItems;
        }

        for (int i = 0; i < rewardData.RewardList.Count; i++)
        {
            int rewardItemId = rewardData.RewardList[i];
            RewardItemData rewardItem = tables.RewardItemDataCategory.GetOrDefault(rewardItemId);
            if (rewardItem == null || rewardItem.BagId <= 0 || rewardItem.Num <= 0f)
            {
                Debug.LogWarning($"奖励项无效。rewardItemId={rewardItemId}, rewardDataId={rewardDataId}");
                continue;
            }

            rewardItems.Add(rewardItem);
        }

        return rewardItems;
    }

    /// <summary>
    /// 发放单个事件卡奖励。
    /// </summary>
    public void GrantEventReward(RewardItemData rewardItem)
    {
        if (rewardItem == null || rewardItem.BagId <= 0 || rewardItem.Num <= 0f)
        {
            return;
        }

        BagMgr.Instance.AddBagProp(rewardItem.BagId, rewardItem.Num, true, "事件卡奖励");
        string rewardName = string.IsNullOrWhiteSpace(rewardItem.Name)
            ? $"物品{rewardItem.BagId}"
            : rewardItem.Name;
        FloatTipWindow.Show($"{rewardName}+{rewardItem.Num:0.##}");
    }

    /// <summary>
    /// 按奖励表权重随机选择一个奖励表 ID。
    /// </summary>
    /// <param name="rewardList">奖励表 ID 和权重</param>
    /// <returns>选中的奖励表 ID</returns>
    private static int SelectWeightedRewardId(IReadOnlyDictionary<int, int> rewardList)
    {
        int totalWeight = 0;
        foreach (KeyValuePair<int, int> reward in rewardList)
        {
            if (reward.Key > 0 && reward.Value > 0)
            {
                totalWeight += reward.Value;
            }
        }

        if (totalWeight <= 0)
        {
            return 0;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach (KeyValuePair<int, int> reward in rewardList)
        {
            if (reward.Key <= 0 || reward.Value <= 0)
            {
                continue;
            }

            roll -= reward.Value;
            if (roll < 0)
            {
                return reward.Key;
            }
        }

        return 0;
    }

    /// <summary>
    /// 刷新主界面路线 HUD。
    /// </summary>
    private void RefreshRouteHud()
    {
        view?.RefreshRouteHud();
    }

    /// <summary>
    /// 初始化本局玩家占位生命值。
    /// </summary>
    private void InitializePlaceholderPlayerState()
    {
        playerMaxHp = 99;
        playerHp = playerMaxHp;
    }

    /// <summary>
    /// 初始化本局默认装备骰子。
    /// </summary>
    private void InitializeDefaultDiceSlots()
    {
        equippedDiceSlots.Clear();
        equippedDiceSlots.Add(new EquippedDiceSlotData("1d4", 4, new[] { 1, 2, 3, 4 }));
        equippedDiceSlots.Add(new EquippedDiceSlotData("1d6", 6, new[] { 1, 2, 3, 4, 5, 6 }));
        equippedDiceSlots.Add(new EquippedDiceSlotData("1d6", 6, new[] { 1, 0, 2, 1, 0, 2 }));
        equippedDiceSlots.Add(new EquippedDiceSlotData("\u7a7a", 0, Array.Empty<int>()));
    }

    /// <summary>
    /// 打开战斗事件弹窗。
    /// </summary>
    private async void OpenDiceBattle(int cardIndex, EventCard card)
    {
        if (eventWindowOpening)
        {
            return;
        }

        if (!TryGetEnemyId(card, out int enemyId))
        {
            FloatTipWindow.Show("\u6218\u6597\u5361\u914d\u7f6e\u9519\u8bef");
            return;
        }

        EnemyData enemyData = GameTableManager.Instance?.Tables?.EnemyDataCategory?.GetOrDefault(enemyId);
        if (enemyData == null)
        {
            FloatTipWindow.Show("\u6218\u6597\u914d\u7f6e\u4e0d\u5b58\u5728");
            return;
        }

        eventWindowOpening = true;
        List<EquippedDiceSlotData> playerDiceSlots = new List<EquippedDiceSlotData>(equippedDiceSlots.Count);
        for (int i = 0; i < equippedDiceSlots.Count; i++)
        {
            playerDiceSlots.Add(equippedDiceSlots[i].Clone());
        }

        DiceBattleWindowData data = new DiceBattleWindowData(enemyData, playerDiceSlots, isWin =>
        {
            eventWindowOpening = false;
            CompleteEventCard(cardIndex);
            if (!isWin)
            {
                ForceLoseFromEventBattle();
            }
        });

        try
        {
            (int id, DiceBattleWindow panel) result =
                await UIMonoInstance.OpenPanel<DiceBattleWindow>(GroupType.弹窗1, data);
            if (result.panel == null)
            {
                eventWindowOpening = false;
                FloatTipWindow.Show("\u6218\u6597\u754c\u9762\u6253\u5f00\u5931\u8d25");
            }
        }
        catch (Exception e)
        {
            eventWindowOpening = false;
            Debug.LogError(e);
            FloatTipWindow.Show("\u6218\u6597\u754c\u9762\u6253\u5f00\u5931\u8d25");
        }
    }

    /// <summary>
    /// 从事件卡敌人列表中读取第一个敌人 ID。
    /// </summary>
    private static bool TryGetEnemyId(EventCard card, out int enemyId)
    {
        enemyId = 0;
        if (card == null || string.IsNullOrWhiteSpace(card.EnemyList))
        {
            return false;
        }

        string[] values = card.EnemyList.Split(',', ' ', '/', '|');
        for (int i = 0; i < values.Length; i++)
        {
            if (int.TryParse(values[i], out enemyId))
            {
                return enemyId > 0;
            }
        }

        enemyId = 0;
        return false;
    }

    /// <summary>
    /// 打开骰子强化弹窗。
    /// </summary>
    private async void OpenDiceEnhance(int cardIndex, EventCard card)
    {
        if (eventWindowOpening)
        {
            return;
        }

        EnhancementTypeData enhancementData =
            GameTableManager.Instance?.Tables?.EnhancementTypeDataCategory?.GetOrDefault(card.DiceEnhanceId);
        if (enhancementData == null)
        {
            FloatTipWindow.Show("\u9ab0\u5b50\u5f3a\u5316\u914d\u7f6e\u4e0d\u5b58\u5728");
            return;
        }

        eventWindowOpening = true;
        bool callbackHandled = false;
        Action finishEventCard = () =>
        {
            if (callbackHandled)
            {
                return;
            }

            callbackHandled = true;
            eventWindowOpening = false;
            CompleteEventCard(cardIndex);
        };

        DiceEnhanceWindowData data = new DiceEnhanceWindowData(enhancementData, equippedDiceSlots,
            FindFirstEnhanceableDiceIndex(),
            (diceIndex, faceIndex) =>
            {
                if (DiceEnhancePreviewModel.IsWholeDiceEnhancement(enhancementData))
                {
                    ApplyWholeDiceEnhance(diceIndex, enhancementData);
                }
                else if (faceIndex.HasValue)
                {
                    ApplySingleFaceEnhance(diceIndex, faceIndex.Value, enhancementData);
                }

                finishEventCard();
            },
            finishEventCard);

        try
        {
            (int id, DiceEnhanceWindow panel) result =
                await UIMonoInstance.OpenPanel<DiceEnhanceWindow>(GroupType.弹窗1, data);
            if (result.panel == null)
            {
                eventWindowOpening = false;
                FloatTipWindow.Show("\u9ab0\u5b50\u5f3a\u5316\u754c\u9762\u6253\u5f00\u5931\u8d25");
            }
        }
        catch (Exception e)
        {
            eventWindowOpening = false;
            Debug.LogError(e);
            FloatTipWindow.Show("\u9ab0\u5b50\u5f3a\u5316\u754c\u9762\u6253\u5f00\u5931\u8d25");
        }
    }

    /// <summary>
    /// 查找第一颗可强化的骰子。
    /// </summary>
    private int FindFirstEnhanceableDiceIndex()
    {
        for (int i = 0; i < equippedDiceSlots.Count; i++)
        {
            if (!equippedDiceSlots[i].IsEmpty)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// 尝试获取有效的骰子槽数据。
    /// </summary>
    private bool TryGetDiceSlot(int diceIndex, out EquippedDiceSlotData dice)
    {
        if (diceIndex >= 0 && diceIndex < equippedDiceSlots.Count)
        {
            dice = equippedDiceSlots[diceIndex];
            return dice != null && !dice.IsEmpty;
        }

        dice = null;
        return false;
    }
}
