using System;
using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    ///     Deck model (logic only, no view).
    /// </summary>
    /// <remarks>
    ///     Shuffling uses an injectable <see cref="System.Random" /> so replays, tests and networked games
    ///     can reproduce the exact deck order: pass a seed or a shared random source to the constructor,
    ///     or assign <see cref="ShuffleRandom" /> later.
    /// </remarks>
    public class DeckModel : ICardContainer
    {
        private const int SuitCount = 4;

        private readonly CardContainerModel _cards = new(CardLocation.Deck);
        private readonly List<CardData> _discardPile = new();
        private Random _random;
        private List<CardData> _initialCards;

        /// <summary>
        ///     Creates a deck that shuffles with a fresh, time-seeded random source.
        /// </summary>
        public DeckModel() : this(null)
        {
        }

        /// <summary>
        ///     Creates a deck whose shuffles are reproducible for the given seed.
        /// </summary>
        /// <param name="seed">Seed for the shuffle random source.</param>
        public DeckModel(int seed) : this(new Random(seed))
        {
        }

        /// <summary>
        ///     Creates a deck that shuffles with the given random source (shared or seeded by the caller).
        /// </summary>
        /// <param name="random">Random source; null creates a fresh time-seeded one.</param>
        public DeckModel(Random random)
        {
            _random = random ?? new Random();
        }

        internal int Revision { get; private set; }

        /// <summary>
        ///     Random source used by <see cref="Shuffle()" />, <see cref="ReshuffleDiscardPile" /> and shuffling
        ///     initialization/reset. Replace it to reseed the deck without rebuilding it.
        /// </summary>
        /// <exception cref="ArgumentNullException">The assigned value is null.</exception>
        public Random ShuffleRandom
        {
            get => _random;
            set => _random = value ?? throw new ArgumentNullException(nameof(value));
        }

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
            _initialCards = null;
            _cards.Clear();
            _discardPile.Clear();

            Rank minRank = deckType.GetMinRank();

            for (int suit = 0; suit < SuitCount; suit++)
            {
                for (int r = (int)minRank; r <= (int)Rank.Ace; r++)
                {
                    _cards.Add(new CardData((Suit)suit, (Rank)r));
                }
            }

            if (deckType == DeckType.Standard54)
            {
                _cards.Add(CardData.CreateJoker(true));
                _cards.Add(CardData.CreateJoker(false));
            }

            if (shuffle)
            {
                ShuffleInternal();
            }

            NotifyDeckChanged();
        }

        /// <summary>
        ///     Initializes from an explicit card list.
        /// </summary>
        /// <param name="cards">Cards to load.</param>
        /// <param name="shuffle">Shuffle after load.</param>
        public void Initialize(IEnumerable<CardData> cards, bool shuffle = true)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            // WHY: snapshot before clearing; callers may pass this deck's Cards or a deferred query over it.
            List<CardData> initialCards = new(cards);
            _cards.Clear();
            _discardPile.Clear();
            _initialCards = initialCards;
            foreach (CardData card in initialCards)
            {
                _cards.Add(card);
            }

            if (shuffle)
            {
                ShuffleInternal();
            }

            NotifyDeckChanged();
        }

        /// <summary>
        ///     Shuffles the draw pile (Fisher–Yates).
        /// </summary>
        public void Shuffle()
        {
            ShuffleInternal(_random);
            NotifyDeckChanged();
        }

        /// <summary>
        ///     Shuffles the draw pile (Fisher–Yates) with a one-off random source, leaving
        ///     <see cref="ShuffleRandom" /> untouched.
        /// </summary>
        /// <param name="random">Random source for this shuffle only.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random" /> is null.</exception>
        public void Shuffle(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            ShuffleInternal(random);
            NotifyDeckChanged();
        }

        private void NotifyDeckChanged()
        {
            unchecked { Revision++; }
            OnDeckChanged?.Invoke();
        }

        private void ShuffleInternal()
        {
            ShuffleInternal(_random);
        }

        private void ShuffleInternal(Random random)
        {
            List<CardData> cards = _cards.Mutable;
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
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
            // WHY: CardData is a value-equality struct; List.Remove would delete the FIRST equal card,
            // corrupting order in combined decks with duplicates. Remove the drawn top card by index.
            _cards.Mutable.RemoveAt(lastIndex);

            NotifyDeckChanged();

            if (_cards.Count == 0)
            {
                OnDeckEmpty?.Invoke();
            }

            return card;
        }

        /// <summary>
        ///     Draws up to <paramref name="count" /> cards.
        /// </summary>
        /// <param name="count">How many; zero or negative returns an empty list.</param>
        /// <returns>Drawn cards.</returns>
        public List<CardData> Draw(int count)
        {
            // WHY: List capacity must be non-negative, and over-asking should not over-allocate.
            List<CardData> result = new(Math.Max(0, Math.Min(count, _cards.Count)));

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
            NotifyDeckChanged();
        }

        /// <summary>
        ///     Returns a card on top of the draw pile.
        /// </summary>
        /// <param name="card">Card.</param>
        public void ReturnToTop(CardData card)
        {
            _cards.Add(card);
            NotifyDeckChanged();
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
            if (_initialCards != null)
            {
                Initialize(_initialCards, shuffle);
            }
            else
            {
                Initialize(DeckType, shuffle);
            }
        }

        /// <summary>
        ///     Clears deck and discard.
        /// </summary>
        public void Clear()
        {
            _cards.Clear();
            _discardPile.Clear();
            NotifyDeckChanged();
        }

        #region ICardContainer explicit

        public bool CanAdd(CardData card)
        {
            return true;
        }

        bool ICardContainer.Remove(CardData card)
        {
            bool removed = _cards.Remove(card);
            if (removed)
            {
                NotifyDeckChanged();
                if (_cards.Count == 0)
                {
                    OnDeckEmpty?.Invoke();
                }
            }

            return removed;
        }

        List<CardData> ICardContainer.RemoveAll()
        {
            List<CardData> removed = _cards.RemoveAll();
            if (removed.Count > 0)
            {
                NotifyDeckChanged();
                OnDeckEmpty?.Invoke();
            }

            return removed;
        }

        void ICardContainer.Clear()
        {
            Clear();
        }

        void ICardContainer.Add(CardData card)
        {
            _cards.Add(card);
            NotifyDeckChanged();
        }

        #endregion
    }
}
