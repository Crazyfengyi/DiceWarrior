using System;
using System.Collections.Generic;
using cfg;
using cfg.diceenhance;
using cfg.eventcard;
using UnityEngine;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public sealed class GameRoot : MonoBehaviour
{
    private readonly EventCardDeck eventCardDeck = new EventCardDeck();
    private readonly List<EquippedDiceSlotData> equippedDiceSlots = new List<EquippedDiceSlotData>();

    private GameWindow view;
    private bool eventWindowOpening;
    private float progress;
    private int playerHp;
    private int playerMaxHp;

    public float Progress => progress;
    public IReadOnlyList<EventCard> ShownEventCards => eventCardDeck.ShownCards;
    public int DrawPileCount => eventCardDeck.DrawPileCount;
    public int DiscardPileCount => eventCardDeck.DiscardPileCount;
    public int PlayerHp => playerHp;
    public int PlayerMaxHp => playerMaxHp;
    public IReadOnlyList<EquippedDiceSlotData> EquippedDiceSlots => equippedDiceSlots;

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
        InitializeLevelDropdown();
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
        if (GameTableManager.Instance?.Tables?.TBLevelCategory == null ||
            !GameTableManager.Instance.Tables.TBLevelCategory.DataMap.ContainsKey(levelId))
        {
            FloatTipWindow.Show("\u8be5\u5173\u5361\u4e0d\u5b58\u5728");
            return false;
        }

        Save_GameData gameData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
        gameData.currentLevelId = levelId;
        FloatTipWindow.Show("\u8df3\u8f6c\u6210\u529f");
        RestartGame();
        return true;
    }

    public bool CanUseUndoProp(int id)
    {
        return false;
    }

    public bool CanUseClearProp(int id)
    {
        return false;
    }

    public bool CanUseShuffleProp(int id)
    {
        return false;
    }

    public bool UseUndoProp(int id, bool isFreeUse)
    {
        FloatTipWindow.Show("\u6682\u65f6\u65e0\u6cd5\u4f7f\u7528");
        return false;
    }

    public bool UseClearProp(int id, bool isFreeUse)
    {
        FloatTipWindow.Show("\u6682\u65f6\u65e0\u6cd5\u4f7f\u7528");
        return false;
    }

    public bool UseShuffleProp(int id, bool isFreeUse, Vector3 startWorldPosition)
    {
        FloatTipWindow.Show("\u6682\u65f6\u65e0\u6cd5\u4f7f\u7528");
        return false;
    }

    /// <summary>
    /// 处理事件卡选择。
    /// </summary>
    public void SelectEventCard(int index)
    {
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

        eventCardDeck.CommitSelectedCard(index);
        FloatTipWindow.Show($"\u9009\u62e9\u4e86\uff1a{selectedCard.Name}");
        RefreshEventCards();
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
    public void ApplyWholeDiceEnhance(int diceIndex, DiceEnhanceConfig config)
    {
        if (!TryGetDiceSlot(diceIndex, out EquippedDiceSlotData dice) || config == null)
        {
            return;
        }

        if (config.EffectType == EDiceEnhanceEffectType.AddValue)
        {
            dice.ApplyWholeDiceDelta(config.ValueDelta);
        }

        RefreshRouteHud();
    }

    /// <summary>
    /// 对单个骰面应用强化。
    /// </summary>
    public void ApplySingleFaceEnhance(int diceIndex, int faceIndex, DiceEnhanceConfig config)
    {
        if (!TryGetDiceSlot(diceIndex, out EquippedDiceSlotData dice) || config == null)
        {
            return;
        }

        if (config.EffectType == EDiceEnhanceEffectType.AddValue)
        {
            dice.ApplySingleFaceDelta(faceIndex, config.ValueDelta);
        }

        RefreshRouteHud();
    }

    /// <summary>
    /// 初始化关卡下拉框内容。
    /// </summary>
    private void InitializeLevelDropdown()
    {
        if (GameTableManager.Instance?.Tables?.TBLevelCategory == null)
        {
            return;
        }

        view?.RefreshLevelDropdown(GameTableManager.Instance.Tables.TBLevelCategory.DataList);
    }

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

        eventCardDeck.Initialize(category.DataList);
        RefreshEventCards();
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

        if (card.BattleId <= 0)
        {
            FloatTipWindow.Show("\u6218\u6597\u5361\u914d\u7f6e\u9519\u8bef");
            return;
        }

        DiceBattle battleConfig = GameTableManager.Instance?.Tables?.DiceBattleCategory?.GetOrDefault(card.BattleId);
        if (battleConfig == null || !battleConfig.Enabled)
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

        DiceBattleWindowData data = new DiceBattleWindowData(battleConfig, playerDiceSlots, isWin =>
        {
            eventWindowOpening = false;
            eventCardDeck.CommitSelectedCard(cardIndex);
            RefreshEventCards();
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
    /// 打开骰子强化弹窗。
    /// </summary>
    private async void OpenDiceEnhance(int cardIndex, EventCard card)
    {
        if (eventWindowOpening)
        {
            return;
        }

        DiceEnhanceConfig config =
            GameTableManager.Instance?.Tables?.DiceEnhanceConfigCategory?.GetOrDefault(card.DiceEnhanceId);
        if (config == null || !config.Enabled)
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
            eventCardDeck.CommitSelectedCard(cardIndex);
            RefreshEventCards();
        };

        DiceEnhanceWindowData data = new DiceEnhanceWindowData(config, equippedDiceSlots, FindFirstEnhanceableDiceIndex(),
            (diceIndex, faceIndex) =>
            {
                if (config.TargetMode == EDiceEnhanceTargetMode.WholeDice)
                {
                    ApplyWholeDiceEnhance(diceIndex, config);
                }
                else if (faceIndex.HasValue)
                {
                    ApplySingleFaceEnhance(diceIndex, faceIndex.Value, config);
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
