using System;
using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    /// Базовая модель контейнера карт.
    /// </summary>
    public class CardContainerModel : ICardContainer
    {
        protected readonly List<CardData> CardsInternal = new();
        public List<CardData> Mutable => CardsInternal;

        public CardLocation Location { get; }
        public int Count => CardsInternal.Count;
        public IReadOnlyList<CardData> Data => CardsInternal;

        public event Action OnChanged;

        public CardContainerModel(CardLocation location)
        {
            Location = location;
        }

        public virtual bool CanAdd(CardData card) => true;

        public virtual void Add(CardData card)
        {
            CardsInternal.Add(card);
            OnChanged?.Invoke();
        }

        public virtual bool Remove(CardData card)
        {
            bool removed = CardsInternal.Remove(card);
            if (removed) OnChanged?.Invoke();
            return removed;
        }

        public virtual List<CardData> RemoveAll()
        {
            var snapshot = new List<CardData>(CardsInternal);
            CardsInternal.Clear();
            OnChanged?.Invoke();
            return snapshot;
        }

        public virtual void Clear()
        {
            CardsInternal.Clear();
            OnChanged?.Invoke();
        }
    }
}

