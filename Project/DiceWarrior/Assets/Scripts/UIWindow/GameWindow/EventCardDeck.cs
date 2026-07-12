using System;
using System.Collections.Generic;
using cfg;

public sealed class EventCardDeck
{
    private const int EventPoolSize = 20;
    private const int ShownCardCount = 3;

    private readonly List<EventCard> drawPool = new List<EventCard>();
    private readonly List<EventCard> shownCards = new List<EventCard>();
    private readonly List<EventCard> graveyard = new List<EventCard>();
    private readonly List<EventCard> eligibleCards = new List<EventCard>();
    private readonly HashSet<int> selectedNonRepeatableIds = new HashSet<int>();

    public IReadOnlyList<EventCard> ShownCards => shownCards;
    public IReadOnlyList<EventCard> Graveyard => graveyard;
    public int DrawPileCount => drawPool.Count;
    public int DiscardPileCount => graveyard.Count;
    public int CurrentLevelId { get; private set; }

    /// <summary>
    /// 按当前关卡初始化事件池。
    /// </summary>
    public void Initialize(IReadOnlyList<EventCard> cards, int currentLevelId)
    {
        CurrentLevelId = currentLevelId;
        drawPool.Clear();
        shownCards.Clear();
        graveyard.Clear();
        eligibleCards.Clear();
        selectedNonRepeatableIds.Clear();

        if (cards == null)
        {
            UnityEngine.Debug.LogError("Event card table is null.");
            return;
        }

        CollectEligibleCards(cards, currentLevelId);
        BuildEventPool();
        FillShownCards();
    }

    /// <summary>
    /// 保留默认关卡初始化入口。
    /// </summary>
    public void Initialize(IReadOnlyList<EventCard> cards)
    {
        Initialize(cards, 1);
    }

    /// <summary>
    /// 获取当前展示区中的事件卡。
    /// </summary>
    public EventCard GetShownCard(int index)
    {
        if (index < 0 || index >= shownCards.Count)
        {
            return null;
        }

        return shownCards[index];
    }

    /// <summary>
    /// 提交选中的事件卡并补充下一张卡。
    /// </summary>
    public EventCard CommitSelectedCard(int index)
    {
        if (index < 0 || index >= shownCards.Count)
        {
            return null;
        }

        EventCard selectedCard = shownCards[index];
        shownCards.RemoveAt(index);
        if (selectedCard == null)
        {
            FillShownCards();
            return null;
        }

        graveyard.Add(selectedCard);
        if (selectedCard.Repeatable == 0)
        {
            selectedNonRepeatableIds.Add(selectedCard.Id);
        }
        else if (CanReinsert(selectedCard))
        {
            drawPool.Add(selectedCard);
        }

        FillShownCards();
        return selectedCard;
    }

    /// <summary>
    /// 提交事件卡的兼容入口。
    /// </summary>
    public EventCard SelectCard(int index)
    {
        return CommitSelectedCard(index);
    }

    /// <summary>
    /// 收集满足启用状态和关卡条件的事件卡。
    /// </summary>
    private void CollectEligibleCards(IReadOnlyList<EventCard> cards, int currentLevelId)
    {
        HashSet<int> cardIds = new HashSet<int>();
        for (int i = 0; i < cards.Count; i++)
        {
            EventCard card = cards[i];
            if (card == null || !card.Enabled || !IsAvailableAtLevel(card, currentLevelId) || !cardIds.Add(card.Id))
            {
                continue;
            }

            eligibleCards.Add(card);
        }
    }

    /// <summary>
    /// 生成本关最多 20 个不重复事件，并应用权重、必选和互斥规则。
    /// </summary>
    private void BuildEventPool()
    {
        List<EventCard> selectedCards = new List<EventCard>(Math.Min(EventPoolSize, eligibleCards.Count));
        HashSet<int> selectedIds = new HashSet<int>();
        HashSet<int> selectedMutexGroups = new HashSet<int>();

        List<EventCard> mandatoryCards = new List<EventCard>();
        for (int i = 0; i < eligibleCards.Count; i++)
        {
            if (eligibleCards[i].DrawWeight == -1)
            {
                mandatoryCards.Add(eligibleCards[i]);
            }
        }

        mandatoryCards.Sort(CompareFixedOrderThenId);
        for (int i = 0; i < mandatoryCards.Count && selectedCards.Count < EventPoolSize; i++)
        {
            TryAddCard(mandatoryCards[i], selectedCards, selectedIds, selectedMutexGroups);
        }

        AddWeightedCards(selectedCards, selectedIds, selectedMutexGroups);
        AddZeroWeightCards(selectedCards, selectedIds, selectedMutexGroups);
        OrderEventPool(selectedCards);
        drawPool.AddRange(selectedCards);

        if (selectedCards.Count < EventPoolSize)
        {
            UnityEngine.Debug.LogWarning(
                $"当前关卡可生成事件不足 {EventPoolSize} 个，实际生成 {selectedCards.Count} 个。");
        }
    }

    /// <summary>
    /// 按正权重随机补充事件卡。
    /// </summary>
    private void AddWeightedCards(List<EventCard> selectedCards, HashSet<int> selectedIds,
        HashSet<int> selectedMutexGroups)
    {
        while (selectedCards.Count < EventPoolSize)
        {
            List<EventCard> candidates = GetAvailableCandidates(selectedIds, selectedMutexGroups, true);
            EventCard card = DrawWeightedCard(candidates);
            if (card == null)
            {
                return;
            }

            TryAddCard(card, selectedCards, selectedIds, selectedMutexGroups);
        }
    }

    /// <summary>
    /// 在正权重事件不足时用零权重事件补足事件池。
    /// </summary>
    private void AddZeroWeightCards(List<EventCard> selectedCards, HashSet<int> selectedIds,
        HashSet<int> selectedMutexGroups)
    {
        List<EventCard> candidates = GetAvailableCandidates(selectedIds, selectedMutexGroups, false);
        Shuffle(candidates);
        for (int i = 0; i < candidates.Count && selectedCards.Count < EventPoolSize; i++)
        {
            TryAddCard(candidates[i], selectedCards, selectedIds, selectedMutexGroups);
        }
    }

    /// <summary>
    /// 尝试将事件加入本关事件池。
    /// </summary>
    private static bool TryAddCard(EventCard card, List<EventCard> selectedCards, HashSet<int> selectedIds,
        HashSet<int> selectedMutexGroups)
    {
        if (card == null || selectedIds.Contains(card.Id) ||
            card.MutexGroup != 0 && selectedMutexGroups.Contains(card.MutexGroup))
        {
            return false;
        }

        selectedCards.Add(card);
        selectedIds.Add(card.Id);
        if (card.MutexGroup != 0)
        {
            selectedMutexGroups.Add(card.MutexGroup);
        }

        return true;
    }

    /// <summary>
    /// 获取尚未选中且满足互斥条件的候选事件。
    /// </summary>
    private List<EventCard> GetAvailableCandidates(HashSet<int> selectedIds, HashSet<int> selectedMutexGroups,
        bool positiveWeightOnly)
    {
        List<EventCard> candidates = new List<EventCard>();
        for (int i = 0; i < eligibleCards.Count; i++)
        {
            EventCard card = eligibleCards[i];
            if (selectedIds.Contains(card.Id) || (positiveWeightOnly && card.DrawWeight <= 0) ||
                card.MutexGroup != 0 && selectedMutexGroups.Contains(card.MutexGroup))
            {
                continue;
            }

            candidates.Add(card);
        }

        return candidates;
    }

    /// <summary>
    /// 按事件权重随机选择一张事件卡。
    /// </summary>
    private static EventCard DrawWeightedCard(IReadOnlyList<EventCard> candidates)
    {
        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += candidates[i].DrawWeight;
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= candidates[i].DrawWeight;
            if (roll < 0)
            {
                return candidates[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 按固定最小位置约束排序事件池，其余位置随机填充。
    /// </summary>
    private static void OrderEventPool(List<EventCard> cards)
    {
        Shuffle(cards);
        if (cards.Count == 0)
        {
            return;
        }

        bool[] occupied = new bool[cards.Count];
        EventCard[] ordered = new EventCard[cards.Count];
        List<EventCard> fixedCards = new List<EventCard>();
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (GetMinimumPosition(cards[i], cards.Count) != int.MaxValue)
            {
                fixedCards.Add(cards[i]);
            }
        }

        fixedCards.Sort((left, right) =>
        {
            int positionCompare = GetMinimumPosition(right, cards.Count)
                .CompareTo(GetMinimumPosition(left, cards.Count));
            return positionCompare != 0 ? positionCompare : right.Id.CompareTo(left.Id);
        });

        for (int i = 0; i < fixedCards.Count; i++)
        {
            EventCard card = fixedCards[i];
            int startIndex = Math.Min(GetMinimumPosition(card, cards.Count) - 1, cards.Count - 1);
            int targetIndex = startIndex;
            while (targetIndex < cards.Count && occupied[targetIndex])
            {
                targetIndex++;
            }

            if (targetIndex >= cards.Count)
            {
                targetIndex = cards.Count - 1;
                while (targetIndex >= 0 && occupied[targetIndex])
                {
                    targetIndex--;
                }
            }

            if (targetIndex >= 0)
            {
                ordered[targetIndex] = card;
                occupied[targetIndex] = true;
            }
        }

        int randomIndex = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (ordered[i] != null)
            {
                continue;
            }

            while (randomIndex < cards.Count && Array.IndexOf(ordered, cards[randomIndex]) >= 0)
            {
                randomIndex++;
            }

            if (randomIndex < cards.Count)
            {
                ordered[i] = cards[randomIndex++];
            }
        }

        cards.Clear();
        cards.AddRange(ordered);
    }

    /// <summary>
    /// 按当前事件池顺序补充 3 张候选事件卡。
    /// </summary>
    private void FillShownCards()
    {
        while (shownCards.Count < ShownCardCount && drawPool.Count > 0)
        {
            EventCard card = drawPool[0];
            drawPool.RemoveAt(0);
            shownCards.Add(card);
        }
    }

    /// <summary>
    /// 判断可重复事件是否可以重新进入当前事件池。
    /// </summary>
    private bool CanReinsert(EventCard card)
    {
        if (selectedNonRepeatableIds.Contains(card.Id))
        {
            return false;
        }

        if (card.MutexGroup == 0)
        {
            return true;
        }

        for (int i = 0; i < drawPool.Count; i++)
        {
            if (drawPool[i].MutexGroup == card.MutexGroup)
            {
                return false;
            }
        }

        for (int i = 0; i < shownCards.Count; i++)
        {
            if (shownCards[i].MutexGroup == card.MutexGroup)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断事件是否满足当前关卡条件。
    /// </summary>
    private static bool IsAvailableAtLevel(EventCard card, int currentLevelId)
    {
        if (string.IsNullOrWhiteSpace(card.AvailableLayers))
        {
            return true;
        }

        string[] values = card.AvailableLayers.Split('/', ',', ' ', '|');
        for (int i = 0; i < values.Length; i++)
        {
            if (int.TryParse(values[i], out int levelId) && levelId == currentLevelId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取事件的最小排序位置。
    /// </summary>
    private static int GetMinimumPosition(EventCard card, int poolSize)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.FixedOrder))
        {
            return int.MaxValue;
        }

        string fixedOrder = card.FixedOrder.Trim();
        if (int.TryParse(fixedOrder, out int position))
        {
            return Math.Max(1, position);
        }

        if (!fixedOrder.StartsWith("最后", StringComparison.Ordinal))
        {
            return int.MaxValue;
        }

        int lastCount = 0;
        for (int i = 0; i < fixedOrder.Length; i++)
        {
            if (char.IsDigit(fixedOrder[i]))
            {
                lastCount = lastCount * 10 + fixedOrder[i] - '0';
            }
        }

        return lastCount <= 0 ? int.MaxValue : Math.Max(1, poolSize - lastCount + 1);
    }

    /// <summary>
    /// 按固定排序位置和事件 ID比较事件卡。
    /// </summary>
    private static int CompareFixedOrderThenId(EventCard left, EventCard right)
    {
        int leftPosition = GetMinimumPosition(left, EventPoolSize);
        int rightPosition = GetMinimumPosition(right, EventPoolSize);
        int positionCompare = leftPosition.CompareTo(rightPosition);
        return positionCompare != 0 ? positionCompare : left.Id.CompareTo(right.Id);
    }

    /// <summary>
    /// 随机打乱列表顺序。
    /// </summary>
    private static void Shuffle(List<EventCard> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            EventCard temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}
