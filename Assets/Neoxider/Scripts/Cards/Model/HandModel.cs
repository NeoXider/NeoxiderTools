using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Cards
{
    /// <summary>
    ///     Hand model (player cards).
    /// </summary>
    public class HandModel : ICardContainer
    {
        private readonly CardContainerModel _cards = new(CardLocation.Hand);

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
            _cards.Add(card);
            OnCardAdded?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     Adds multiple cards.
        /// </summary>
        /// <param name="cards">Cards to add.</param>
        public void AddRange(IEnumerable<CardData> cards)
        {
            foreach (CardData card in cards)
            {
                _cards.Add(card);
                OnCardAdded?.Invoke(card);
            }

            OnHandChanged?.Invoke();
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
            _cards.Remove(card);
            OnCardRemoved?.Invoke(card);
            OnHandChanged?.Invoke();
            return card;
        }

        /// <summary>
        ///     Clears the hand.
        /// </summary>
        public void Clear()
        {
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
            return _cards.Data.Contains(card);
        }

        /// <summary>
        ///     Whether any non-joker card has the rank.
        /// </summary>
        /// <param name="rank">Rank.</param>
        /// <returns>True if a matching card exists.</returns>
        public bool ContainsRank(Rank rank)
        {
            return _cards.Data.Any(c => !c.IsJoker && c.Rank == rank);
        }

        /// <summary>
        ///     Whether any non-joker card has the suit.
        /// </summary>
        /// <param name="suit">Suit.</param>
        /// <returns>True if a matching card exists.</returns>
        public bool ContainsSuit(Suit suit)
        {
            return _cards.Data.Any(c => !c.IsJoker && c.Suit == suit);
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
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards.Data[i].Equals(card))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Sorts by rank.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        public void SortByRank(bool ascending = true)
        {
            if (ascending)
            {
                _cards.Mutable.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            }
            else
            {
                _cards.Mutable.Sort((a, b) => b.Rank.CompareTo(a.Rank));
            }

            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     Sorts by suit, then rank.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        public void SortBySuit(bool ascending = true)
        {
            if (ascending)
            {
                _cards.Mutable.Sort((a, b) =>
                {
                    int suitCompare = a.Suit.CompareTo(b.Suit);
                    return suitCompare != 0 ? suitCompare : a.Rank.CompareTo(b.Rank);
                });
            }
            else
            {
                _cards.Mutable.Sort((a, b) =>
                {
                    int suitCompare = b.Suit.CompareTo(a.Suit);
                    return suitCompare != 0 ? suitCompare : b.Rank.CompareTo(a.Rank);
                });
            }

            OnHandChanged?.Invoke();
        }

        /// <summary>
        ///     All non-joker cards of a suit.
        /// </summary>
        /// <param name="suit">Suit.</param>
        /// <returns>List of cards.</returns>
        public List<CardData> GetCardsBySuit(Suit suit)
        {
            return _cards.Data.Where(c => !c.IsJoker && c.Suit == suit).ToList();
        }

        /// <summary>
        ///     All non-joker cards of a rank.
        /// </summary>
        /// <param name="rank">Rank.</param>
        /// <returns>List of cards.</returns>
        public List<CardData> GetCardsByRank(Rank rank)
        {
            return _cards.Data.Where(c => !c.IsJoker && c.Rank == rank).ToList();
        }

        /// <summary>
        ///     Cards that can beat the attack card (Durak rules).
        /// </summary>
        /// <param name="attackCard">Attacking card.</param>
        /// <param name="trump">Trump suit.</param>
        /// <returns>Matching cards.</returns>
        public List<CardData> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            return _cards.Data.Where(c => c.CanCover(attackCard, trump)).ToList();
        }

        /// <summary>
        ///     Cards whose rank appears in the given set (discarding in Durak).
        /// </summary>
        /// <param name="ranks">Ranks on the table.</param>
        /// <returns>Cards that can be added.</returns>
        public List<CardData> GetCardsMatchingRanks(IEnumerable<Rank> ranks)
        {
            HashSet<Rank> rankSet = new(ranks);
            return _cards.Data.Where(c => !c.IsJoker && rankSet.Contains(c.Rank)).ToList();
        }

        /// <summary>
        ///     Lowest non-joker card (trump considered higher when set).
        /// </summary>
        /// <param name="trump">Optional trump suit.</param>
        /// <returns>Lowest card or null if empty.</returns>
        public CardData? GetLowestCard(Suit? trump = null)
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            return _cards.Data
                .Where(c => !c.IsJoker)
                .OrderBy(c => trump.HasValue && c.Suit == trump.Value ? 1 : 0)
                .ThenBy(c => c.Rank)
                .FirstOrDefault();
        }

        /// <summary>
        ///     Highest non-joker card (trump considered higher when set).
        /// </summary>
        /// <param name="trump">Optional trump suit.</param>
        /// <returns>Highest card or null if empty.</returns>
        public CardData? GetHighestCard(Suit? trump = null)
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            return _cards.Data
                .Where(c => !c.IsJoker)
                .OrderByDescending(c => trump.HasValue && c.Suit == trump.Value ? 1 : 0)
                .ThenByDescending(c => c.Rank)
                .FirstOrDefault();
        }

        #region ICardContainer explicit

        public bool CanAdd(CardData card)
        {
            return true;
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
            _cards.Clear();
            return snapshot;
        }

        void ICardContainer.Clear()
        {
            Clear();
        }

        #endregion
    }
}
