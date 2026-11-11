using System;
using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    ///     Hand model (player cards).
    /// </summary>
    public class HandModel : ICardContainer
    {
        private readonly CardContainerModel _cards = new(CardLocation.Hand);
        private int _capacity;

        /// <summary>
        ///     Cards in the hand.
        /// </summary>
        public IReadOnlyList<CardData> Cards => _cards.Data;

        /// <summary>
        ///     Whether the hand is empty.
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Number of cards.
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        ///     Maximum number of cards in the hand. Zero means unlimited.
        /// </summary>
        public int Capacity
        {
            get => _capacity;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be negative.");
                }

                _capacity = value;
            }
        }

        /// <summary>
        ///     Remaining free slots. Unlimited hands return int.MaxValue.
        /// </summary>
        public int RemainingCapacity => Capacity <= 0 ? int.MaxValue : Math.Max(0, Capacity - Count);

        /// <summary>
        ///     Whether this hand has a finite capacity and no free slots.
        /// </summary>
        public bool IsFull => Capacity > 0 && Count >= Capacity;

        public CardLocation Location => _cards.Location;
        IReadOnlyList<CardData> ICardContainer.Data => _cards.Data;

        public event Action OnChanged
        {
            add => OnHandChanged += value;
            remove => OnHandChanged -= value;
        }

        /// <summary>
        ///     Raised when a card is added.
        /// </summary>
        public event Action<CardData> OnCardAdded;

        /// <summary>
        ///     Raised when a card is removed.
        /// </summary>
        public event Action<CardData> OnCardRemoved;

        /// <summary>
        ///     Raised when the hand changes.
        /// </summary>
        public event Action OnHandChanged;

        /// <summary>
        ///     Adds a card.
        /// </summary>
        /// <param name="card">Card to add.</param>
        public void Add(CardData card)
        {
            Add(card, null);
        }

        // WHY: scene adapters release prior cleanup ownership after commitment, before public observers.
        internal void Add(CardData card, Action onAccepted)
        {
            if (!CanAdd(card))
            {
                throw new InvalidOperationException("Cannot add card because the hand is full.");
            }

            _cards.Add(card);
            onAccepted?.Invoke();
            OnCardAdded?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     Attempts to add a card without throwing when the hand is full.
        /// </summary>
        /// <param name="card">Card to add.</param>
        /// <returns>True when the card was added.</returns>
        public bool TryAdd(CardData card)
        {
            if (!CanAdd(card))
            {
                return false;
            }

            _cards.Add(card);
            OnCardAdded?.Invoke(card);
            OnHandChanged?.Invoke();
            return true;
        }

        /// <summary>
        ///     Inserts a card at the given index (keeps visual/model order in sync for bottom-insert hands).
        /// </summary>
        /// <param name="index">Target index.</param>
        /// <param name="card">Card to insert.</param>
        public void Insert(int index, CardData card)
        {
            Insert(index, card, null);
        }

        internal void Insert(int index, CardData card, Action onAccepted)
        {
            if (index < 0 || index > _cards.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (!CanAdd(card))
            {
                throw new InvalidOperationException("Cannot insert card because the hand is full.");
            }

            _cards.Mutable.Insert(index, card);
            onAccepted?.Invoke();
            OnCardAdded?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     Adds multiple cards.
        /// </summary>
        /// <param name="cards">Cards to add.</param>
        public void AddRange(IEnumerable<CardData> cards)
        {
            List<CardData> pending = new(cards);
            if (Capacity > 0 && Count + pending.Count > Capacity)
            {
                throw new InvalidOperationException("Cannot add all cards because the hand capacity would be exceeded.");
            }

            if (pending.Count == 0)
            {
                return;
            }

            foreach (CardData card in pending)
            {
                _cards.Add(card);
                OnCardAdded?.Invoke(card);
            }

            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     Adds cards until the hand is full.
        /// </summary>
        /// <param name="cards">Cards to add.</param>
        /// <returns>Number of cards added.</returns>
        public int AddRangeUntilFull(IEnumerable<CardData> cards)
        {
            if (IsFull)
            {
                return 0;
            }

            int added = 0;
            using IEnumerator<CardData> enumerator = cards.GetEnumerator();
            while (!IsFull && enumerator.MoveNext())
            {
                CardData card = enumerator.Current;
                _cards.Add(card);
                OnCardAdded?.Invoke(card);
                added++;
            }

            if (added > 0)
            {
                OnHandChanged?.Invoke();
            }

            return added;
        }

        /// <summary>
        ///     Removes a card.
        /// </summary>
        /// <param name="card">Card to remove.</param>
        /// <returns>True if removed.</returns>
        public bool Remove(CardData card)
        {
            if (_cards.Remove(card))
            {
                OnCardRemoved?.Invoke(card);
                OnHandChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Removes a card by index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>Removed card.</returns>
        public CardData RemoveAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            CardData card = _cards.Mutable[index];
            _cards.Mutable.RemoveAt(index);
            OnCardRemoved?.Invoke(card);
            OnHandChanged?.Invoke();
            return card;
        }

        /// <summary>
        ///     Clears the hand.
        /// </summary>
        public void Clear()
        {
            if (_cards.Count == 0)
            {
                return;
            }

            _cards.Clear();
            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     Whether the hand contains the card.
        /// </summary>
        /// <param name="card">Card to find.</param>
        /// <returns>True if present.</returns>
        public bool Contains(CardData card)
        {
            return IndexOf(card) >= 0;
        }

        /// <summary>
        ///     Whether any non-joker card has the rank.
        /// </summary>
        /// <param name="rank">Rank.</param>
        /// <returns>True if a matching card exists.</returns>
        public bool ContainsRank(Rank rank)
        {
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (!card.IsJoker && card.Rank == rank)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Whether any non-joker card has the suit.
        /// </summary>
        /// <param name="suit">Suit.</param>
        /// <returns>True if a matching card exists.</returns>
        public bool ContainsSuit(Suit suit)
        {
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (!card.IsJoker && card.Suit == suit)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Card at index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>Card data.</returns>
        public CardData GetAt(int index)
        {
            return _cards.Data[index];
        }

        /// <summary>
        ///     Index of a card, or -1.
        /// </summary>
        /// <param name="card">Card to find.</param>
        /// <returns>Index or -1.</returns>
        public int IndexOf(CardData card)
        {
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Equals(card))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Sorts by rank. The sort is stable: equal ranks keep their current relative order.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        public void SortByRank(bool ascending = true)
        {
            Sort(ascending ? CardOrdering.RankAscending : CardOrdering.RankDescending);
        }

        /// <summary>
        ///     Sorts by suit, then rank. The sort is stable.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        public void SortBySuit(bool ascending = true)
        {
            Sort(ascending ? CardOrdering.SuitThenRankAscending : CardOrdering.SuitThenRankDescending);
        }

        /// <summary>
        ///     Sorts the hand with any comparer (for example one of <see cref="CardComparer" />, or a custom
        ///     comparer for custom card data). The sort is stable and raises <see cref="OnHandChanged" /> once.
        /// </summary>
        /// <param name="comparer">Card comparer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer" /> is null.</exception>
        public void Sort(IComparer<CardData> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            Sort(comparer.Compare);
        }

        /// <summary>
        ///     Sorts the hand with a comparison delegate. The sort is stable and raises
        ///     <see cref="OnHandChanged" /> once.
        /// </summary>
        /// <param name="comparison">Card comparison.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison" /> is null.</exception>
        public void Sort(Comparison<CardData> comparison)
        {
            if (comparison == null)
            {
                throw new ArgumentNullException(nameof(comparison));
            }

            CardOrdering.StableSort(_cards.Mutable, comparison);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     All non-joker cards of a suit.
        /// </summary>
        /// <param name="suit">Suit.</param>
        /// <returns>List of cards.</returns>
        public List<CardData> GetCardsBySuit(Suit suit)
        {
            List<CardData> result = new();
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (!card.IsJoker && card.Suit == suit)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        ///     All non-joker cards of a rank.
        /// </summary>
        /// <param name="rank">Rank.</param>
        /// <returns>List of cards.</returns>
        public List<CardData> GetCardsByRank(Rank rank)
        {
            List<CardData> result = new();
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (!card.IsJoker && card.Rank == rank)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        ///     Cards that can beat the attack card (Durak rules).
        /// </summary>
        /// <param name="attackCard">Attacking card.</param>
        /// <param name="trump">Trump suit.</param>
        /// <returns>Matching cards.</returns>
        public List<CardData> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            List<CardData> result = new();
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card.CanCover(attackCard, trump))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        ///     Cards whose rank appears in the given set (discarding in Durak).
        /// </summary>
        /// <param name="ranks">Ranks on the table.</param>
        /// <returns>Cards that can be added.</returns>
        public List<CardData> GetCardsMatchingRanks(IEnumerable<Rank> ranks)
        {
            HashSet<Rank> rankSet = new(ranks);
            List<CardData> result = new();
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (!card.IsJoker && rankSet.Contains(card.Rank))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        ///     Lowest non-joker card (trump considered higher when set).
        /// </summary>
        /// <param name="trump">Optional trump suit.</param>
        /// <returns>Lowest card, or null when the hand holds no non-joker card.</returns>
        public CardData? GetLowestCard(Suit? trump = null)
        {
            // WHY: one linear pass instead of OrderBy; the first card wins ties, as the old stable sort did.
            CardData? best = null;
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (!card.IsJoker && (!best.HasValue || CompareTrumpThenRank(card, best.Value, trump) < 0))
                {
                    best = card;
                }
            }

            return best;
        }

        /// <summary>
        ///     Highest non-joker card (trump considered higher when set).
        /// </summary>
        /// <param name="trump">Optional trump suit.</param>
        /// <returns>Highest card, or null when the hand holds no non-joker card.</returns>
        public CardData? GetHighestCard(Suit? trump = null)
        {
            CardData? best = null;
            List<CardData> cards = _cards.Mutable;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (!card.IsJoker && (!best.HasValue || CompareTrumpThenRank(card, best.Value, trump) > 0))
                {
                    best = card;
                }
            }

            return best;
        }

        private static int CompareTrumpThenRank(CardData a, CardData b, Suit? trump)
        {
            int aTrump = trump.HasValue && a.Suit == trump.Value ? 1 : 0;
            int bTrump = trump.HasValue && b.Suit == trump.Value ? 1 : 0;
            int trumpCompare = aTrump.CompareTo(bTrump);
            return trumpCompare != 0 ? trumpCompare : ((int)a.Rank).CompareTo((int)b.Rank);
        }

        #region ICardContainer explicit

        public bool CanAdd(CardData card)
        {
            return !IsFull;
        }

        void ICardContainer.Add(CardData card)
        {
            Add(card);
        }

        bool ICardContainer.Remove(CardData card)
        {
            return Remove(card);
        }

        List<CardData> ICardContainer.RemoveAll()
        {
            List<CardData> snapshot = new(_cards.Data);
            if (snapshot.Count == 0)
            {
                return snapshot;
            }

            _cards.Clear();
            foreach (CardData card in snapshot)
            {
                OnCardRemoved?.Invoke(card);
            }

            OnHandChanged?.Invoke();
            return snapshot;
        }

        void ICardContainer.Clear()
        {
            Clear();
        }

        #endregion
    }
}
