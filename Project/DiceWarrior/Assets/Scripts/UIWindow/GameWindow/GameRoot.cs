using System;
using System.Collections.Generic;
using cfg;
using cfg.eventcard;
using UnityEngine;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public sealed class GameRoot : MonoBehaviour
{
    private readonly EventCardDeck eventCardDeck = new EventCardDeck();
    private readonly List<EquippedDiceSlotData> equippedDiceSlots = new List<EquippedDiceSlotData>();

    private GameWindow view;
    private bool eventBattleOpening;
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

    public void Initialize(GameWindow gameWindow)
    {
        view = gameWindow;
        eventBattleOpening = false;
        InitializePlaceholderPlayerState();
        InitializeDefaultDiceSlots();
        ApplyCurrentLevelConfig();
        InitializeLevelDropdown();
        InitializeEventCards();
        RefreshRouteHud();
    }

    public void Dispose()
    {
        view = null;
        eventBattleOpening = false;
    }

    public void RestartGame()
    {
        InitializePlaceholderPlayerState();
        ApplyCurrentLevelConfig();
        InitializeEventCards();
        RefreshRouteHud();
    }

    public void ApplyCurrentLevelConfig()
    {
        progress = 0f;
        view?.UpdateBarShow(progress);
    }

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

        eventCardDeck.CommitSelectedCard(index);
        FloatTipWindow.Show($"\u9009\u62e9\u4e86\uff1a{selectedCard.Name}");
        RefreshEventCards();
    }

    public async void ForceLoseFromEventBattle()
    {
        LoseWindowData data = new LoseWindowData
        {
            RestartAction = RestartGame
        };

        await UIMonoInstance.OpenPanel<LoseWindow>(GroupType.弹窗1, data);
    }

    private void InitializeLevelDropdown()
    {
        if (GameTableManager.Instance?.Tables?.TBLevelCategory == null)
        {
            return;
        }

        view?.RefreshLevelDropdown(GameTableManager.Instance.Tables.TBLevelCategory.DataList);
    }

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

    private void RefreshEventCards()
    {
        view?.RefreshEventCards(eventCardDeck.ShownCards);
        RefreshRouteHud();
    }

    private void RefreshRouteHud()
    {
        view?.RefreshRouteHud();
    }

    private void InitializePlaceholderPlayerState()
    {
        playerMaxHp = 99;
        playerHp = playerMaxHp;
    }

    private void InitializeDefaultDiceSlots()
    {
        equippedDiceSlots.Clear();
        equippedDiceSlots.Add(new EquippedDiceSlotData("1d4", 4, new[] { 1, 2, 3, 4 }));
        equippedDiceSlots.Add(new EquippedDiceSlotData("1d6", 6, new[] { 1, 2, 3, 4, 5, 6 }));
        equippedDiceSlots.Add(new EquippedDiceSlotData("1d6", 6, new[] { 1, 2, 3, 4, 5, 6 }));
        equippedDiceSlots.Add(new EquippedDiceSlotData("\u7a7a", 0, Array.Empty<int>()));
    }

    private async void OpenDiceBattle(int cardIndex, EventCard card)
    {
        if (eventBattleOpening)
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

        eventBattleOpening = true;
        DiceBattleWindowData data = new DiceBattleWindowData(battleConfig, isWin =>
        {
            eventBattleOpening = false;
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
                eventBattleOpening = false;
                FloatTipWindow.Show("\u6218\u6597\u754c\u9762\u6253\u5f00\u5931\u8d25");
            }
        }
        catch (Exception e)
        {
            eventBattleOpening = false;
            Debug.LogError(e);
            FloatTipWindow.Show("\u6218\u6597\u754c\u9762\u6253\u5f00\u5931\u8d25");
        }
    }
}
