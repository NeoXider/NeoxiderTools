using System;
using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    ///     Deck model (logic only, no view).
    /// </summary>
    public class DeckModel : ICardContainer
    {
        private readonly CardContainerModel _cards = new(CardLocation.Deck);
        private readonly List<CardData> _discardPile = new();
        private readonly Random _random = new();

        /// <summary>
        ///     Cards still in the deck.
        /// </summary>
        public IReadOnlyList<CardData> Cards => _cards.Data;

        /// <summary>
        ///     Discard pile.
        /// </summary>
        public IReadOnlyList<CardData> DiscardPile => _discardPile;

        /// <summary>
        ///     Cards remaining in the draw pile.
        /// </summary>
        public int RemainingCount => _cards.Count;

        /// <summary>
        ///     Whether the draw pile is empty.
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Deck type used for generation.
        /// </summary>
        public DeckType DeckType { get; private set; }

        public CardLocation Location => _cards.Location;
        public int Count => _cards.Count;
        public IReadOnlyList<CardData> Data => _cards.Data;

        public event Action OnChanged
        {
            add => OnDeckChanged += value;
            remove => OnDeckChanged -= value;
        }

        /// <summary>
        ///     Raised when the deck changes.
        /// </summary>
        public event Action OnDeckChanged;

        /// <summary>
        ///     Raised when the draw pile becomes empty.
        /// </summary>
        public event Action OnDeckEmpty;

        /// <summary>
        ///     Builds a deck of the given type.
        /// </summary>
        /// <param name="deckType">Deck type.</param>
        /// <param name="shuffle">Shuffle after build.</param>
        public void Initialize(DeckType deckType, bool shuffle = true)
        {
            DeckType = deckType;
            _cards.Clear();
            _discardPile.Clear();

            Rank minRank = deckType.GetMinRank();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (int r = (int)minRank; r <= (int)Rank.Ace; r++)
                {
                    _cards.Add(new CardData(suit, (Rank)r));
                }
            }

            if (deckType == DeckType.Standard54)
            {
                _cards.Add(CardData.CreateJoker(true));
                _cards.Add(CardData.CreateJoker(false));
            }

            if (shuffle)
            {
                Shuffle();
            }

            OnDeckChanged?.Invoke();
        }

        /// <summary>
        ///     Initializes from an explicit card list.
        /// </summary>
        /// <param name="cards">Cards to load.</param>
        /// <param name="shuffle">Shuffle after load.</param>
        public void Initialize(IEnumerable<CardData> cards, bool shuffle = true)
        {
            _cards.Clear();
            _discardPile.Clear();
            foreach (CardData card in cards)
            {
                _cards.Add(card);
            }

            if (shuffle)
            {
                Shuffle();
            }

            OnDeckChanged?.Invoke();
        }

        /// <summary>
        ///     Shuffles the draw pile (Fisher–Yates).
        /// </summary>
        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_cards.Mutable[i], _cards.Mutable[j]) = (_cards.Mutable[j], _cards.Mutable[i]);
            }

            OnDeckChanged?.Invoke();
        }

        /// <summary>
        ///     Draws the top card.
        /// </summary>
        /// <returns>Card or null if empty.</returns>
        public CardData? Draw()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            int lastIndex = _cards.Count - 1;
            CardData card = _cards.Mutable[lastIndex];
            _cards.Remove(card);

            OnDeckChanged?.Invoke();

            if (_cards.Count == 0)
            {
                OnDeckEmpty?.Invoke();
            }

            return card;
        }

        /// <summary>
        ///     Draws up to <paramref name="count" /> cards.
        /// </summary>
        /// <param name="count">How many.</param>
        /// <returns>Drawn cards.</returns>
        public List<CardData> Draw(int count)
        {
            List<CardData> result = new(count);

            for (int i = 0; i < count && _cards.Count > 0; i++)
            {
                CardData? card = Draw();
                if (card.HasValue)
                {
                    result.Add(card.Value);
                }
            }

            return result;
        }

        /// <summary>
        ///     Peeks top card without removing.
        /// </summary>
        /// <returns>Top card or null.</returns>
        public CardData? Peek()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            return _cards.Mutable[^1];
        }

        /// <summary>
        ///     Peeks bottom card without removing (often trump in Durak).
        /// </summary>
        /// <returns>Bottom card or null.</returns>
        public CardData? PeekBottom()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            return _cards.Mutable[0];
        }

        /// <summary>
        ///     Sends a card to the discard pile.
        /// </summary>
        /// <param name="card">Card.</param>
        public void Discard(CardData card)
        {
            _discardPile.Add(card);
        }

        /// <summary>
        ///     Discards multiple cards.
        /// </summary>
        /// <param name="cards">Cards.</param>
        public void Discard(IEnumerable<CardData> cards)
        {
            _discardPile.AddRange(cards);
        }

        /// <summary>
        ///     Returns a card under the draw pile (bottom).
        /// </summary>
        /// <param name="card">Card.</param>
        public void ReturnToBottom(CardData card)
        {
            _cards.Mutable.Insert(0, card);
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        ///     Returns a card on top of the draw pile.
        /// </summary>
        /// <param name="card">Card.</param>
        public void ReturnToTop(CardData card)
        {
            _cards.Add(card);
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        ///     Shuffles discard back into the deck.
        /// </summary>
        public void ReshuffleDiscardPile()
        {
            foreach (CardData card in _discardPile)
            {
                _cards.Add(card);
            }

            _discardPile.Clear();
            Shuffle();
        }

        /// <summary>
        ///     Resets to the current <see cref="DeckType" />.
        /// </summary>
        /// <param name="shuffle">Shuffle after reset.</param>
        public void Reset(bool shuffle = true)
        {
            Initialize(DeckType, shuffle);
        }

        /// <summary>
        ///     Clears deck and discard.
        /// </summary>
        public void Clear()
        {
            _cards.Clear();
            _discardPile.Clear();
            OnDeckChanged?.Invoke();
        }

        #region ICardContainer explicit

        public bool CanAdd(CardData card)
        {
            return true;
        }

        bool ICardContainer.Remove(CardData card)
        {
            return _cards.Remove(card);
        }

        List<CardData> ICardContainer.RemoveAll()
        {
            return _cards.RemoveAll();
        }

        void ICardContainer.Clear()
        {
            Clear();
        }

        void ICardContainer.Add(CardData card)
        {
            _cards.Add(card);
        }

        #endregion
    }
}
