using System.Collections.Generic;
using cfg;
using cfg.eventcard;
using UnityEngine;

public sealed class EventCardDeck
{
    private const int ShownCardCount = 3;
    private const int BattleCategoryWeight = 50;
    private const int NeutralCategoryWeight = 35;
    private const int TreasureCategoryWeight = 15;

    private readonly List<EventCard> drawPool = new List<EventCard>();
    private readonly List<EventCard> shownCards = new List<EventCard>();
    private readonly List<EventCard> graveyard = new List<EventCard>();
    private readonly List<EventCard> enabledCards = new List<EventCard>();

    public IReadOnlyList<EventCard> ShownCards => shownCards;
    public IReadOnlyList<EventCard> Graveyard => graveyard;
    public int DrawPileCount => drawPool.Count;
    public int DiscardPileCount => graveyard.Count;

    public void Initialize(IReadOnlyList<EventCard> cards)
    {
        drawPool.Clear();
        shownCards.Clear();
        graveyard.Clear();
        enabledCards.Clear();

        if (cards == null)
        {
            Debug.LogError("Event card table is null.");
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            EventCard card = cards[i];
            if (card == null || !card.Enabled)
            {
                continue;
            }

            enabledCards.Add(card);
            drawPool.Add(card);
        }

        if (enabledCards.Count < ShownCardCount)
        {
            Debug.LogError($"Event card count is less than {ShownCardCount}. Current count:{enabledCards.Count}");
        }

        FillShownCards();
    }

    public EventCard SelectCard(int index)
    {
        return CommitSelectedCard(index);
    }

    public EventCard GetShownCard(int index)
    {
        if (index < 0 || index >= shownCards.Count)
        {
            return null;
        }

        return shownCards[index];
    }

    public EventCard CommitSelectedCard(int index)
    {
        if (index < 0 || index >= shownCards.Count)
        {
            return null;
        }

        EventCard selectedCard = shownCards[index];
        shownCards.RemoveAt(index);
        if (selectedCard != null)
        {
            graveyard.Add(selectedCard);
        }

        FillShownCards();
        return selectedCard;
    }

    private void FillShownCards()
    {
        int guardCount = 0;
        while (shownCards.Count < ShownCardCount && enabledCards.Count > shownCards.Count && guardCount < enabledCards.Count * 4)
        {
            guardCount++;
            EventCard card = DrawCard();
            if (card == null)
            {
                break;
            }

            shownCards.Add(card);
        }
    }

    private EventCard DrawCard()
    {
        if (drawPool.Count == 0)
        {
            RecycleGraveyard();
        }

        if (drawPool.Count == 0)
        {
            return null;
        }

        EEventCardType selectedType = RollCardType();
        EventCard card = DrawCardByType(selectedType);
        if (card == null)
        {
            card = DrawAnyCard();
        }

        if (card != null)
        {
            drawPool.Remove(card);
        }

        return card;
    }

    private void RecycleGraveyard()
    {
        if (graveyard.Count == 0)
        {
            return;
        }

        drawPool.AddRange(graveyard);
        graveyard.Clear();
    }

    private EventCard DrawCardByType(EEventCardType cardType)
    {
        int totalWeight = 0;
        for (int i = 0; i < drawPool.Count; i++)
        {
            EventCard card = drawPool[i];
            if (card.CardType == cardType)
            {
                totalWeight += Mathf.Max(0, card.DrawWeight);
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < drawPool.Count; i++)
        {
            EventCard card = drawPool[i];
            if (card.CardType != cardType)
            {
                continue;
            }

            roll -= Mathf.Max(0, card.DrawWeight);
            if (roll < 0)
            {
                return card;
            }
        }

        return null;
    }

    private EventCard DrawAnyCard()
    {
        int totalWeight = 0;
        for (int i = 0; i < drawPool.Count; i++)
        {
            totalWeight += Mathf.Max(1, drawPool[i].DrawWeight);
        }

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < drawPool.Count; i++)
        {
            EventCard card = drawPool[i];
            roll -= Mathf.Max(1, card.DrawWeight);
            if (roll < 0)
            {
                return card;
            }
        }

        return drawPool[0];
    }

    private static EEventCardType RollCardType()
    {
        int totalWeight = BattleCategoryWeight + NeutralCategoryWeight + TreasureCategoryWeight;
        int roll = Random.Range(0, totalWeight);
        if (roll < BattleCategoryWeight)
        {
            return EEventCardType.Battle;
        }

        roll -= BattleCategoryWeight;
        if (roll < NeutralCategoryWeight)
        {
            return EEventCardType.Neutral;
        }

        return EEventCardType.Treasure;
    }
}
