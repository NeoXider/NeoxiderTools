using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Deck configuration with sprites.
    /// </summary>
    [CreateAssetMenu(fileName = "DeckConfig", menuName = "Neoxider/Cards/Deck Config")]
    public class DeckConfig : ScriptableObject
    {
        [Header("Settings")] [Tooltip("Deck type for sprites (how many cards loaded in config)")] [SerializeField]
        private DeckType _deckType = DeckType.Standard52;

        [Tooltip(
            "Deck type used in play (how many cards to use). Lets you keep full sprites while playing a smaller deck.")]
        [SerializeField]
        private DeckType _gameDeckType = DeckType.Standard54;

        [Header("Card Back")] [SerializeField] private Sprite _backSprite;

        [Header("Hearts (Low to High)")] [SerializeField]
        private List<Sprite> _hearts = new();

        [Header("Diamonds (Low to High)")] [SerializeField]
        private List<Sprite> _diamonds = new();

        [Header("Clubs (Low to High)")] [SerializeField]
        private List<Sprite> _clubs = new();

        [Header("Spades (Low to High)")] [SerializeField]
        private List<Sprite> _spades = new();

        [Header("Jokers (54-card)")] [SerializeField]
        private Sprite _redJoker;

        [SerializeField] private Sprite _blackJoker;

        /// <summary>
        ///     Sprite deck type (how many sprites are expected in the asset).
        /// </summary>
        public DeckType DeckType => _deckType;

        /// <summary>
        ///     Play deck type (how many cards are used in game logic).
        /// </summary>
        public DeckType GameDeckType => _gameDeckType;

        /// <summary>
        ///     Card back sprite.
        /// </summary>
        public Sprite BackSprite => _backSprite;

        /// <summary>
        ///     Red joker sprite.
        /// </summary>
        public Sprite RedJoker => _redJoker;

        /// <summary>
        ///     Black joker sprite.
        /// </summary>
        public Sprite BlackJoker => _blackJoker;

        /// <summary>
        ///     Heart suit sprites.
        /// </summary>
        public IReadOnlyList<Sprite> Hearts => _hearts;

        /// <summary>
        ///     Diamond suit sprites.
        /// </summary>
        public IReadOnlyList<Sprite> Diamonds => _diamonds;

        /// <summary>
        ///     Club suit sprites.
        /// </summary>
        public IReadOnlyList<Sprite> Clubs => _clubs;

        /// <summary>
        ///     Spade suit sprites.
        /// </summary>
        public IReadOnlyList<Sprite> Spades => _spades;

        /// <summary>
        ///     Returns the sprite for a card.
        /// </summary>
        /// <param name="card">Card data.</param>
        /// <returns>Sprite, or null if missing.</returns>
        public Sprite GetSprite(CardData card)
        {
            if (card.IsJoker)
            {
                return card.IsRedJoker ? _redJoker : _blackJoker;
            }

            List<Sprite> suitSprites = GetSuitSprites(card.Suit);
            if (suitSprites == null || suitSprites.Count == 0)
            {
                return null;
            }

            int index = GetSpriteIndex(card.Rank);
            if (index < 0 || index >= suitSprites.Count)
            {
                return null;
            }

            return suitSprites[index];
        }

        /// <summary>
        ///     Expected number of cards per suit for this sprite deck type.
        /// </summary>
        public int GetExpectedCardCountPerSuit()
        {
            return _deckType.GetCardsPerSuit();
        }

        /// <summary>
        ///     Generates the play deck using <see cref="GameDeckType" />.
        /// </summary>
        /// <returns>List of cards.</returns>
        public List<CardData> GenerateDeck()
        {
            return GenerateDeck(_gameDeckType);
        }

        /// <summary>
        ///     Generates a deck of the given type.
        /// </summary>
        /// <param name="deckType">Deck type to generate.</param>
        /// <returns>List of cards.</returns>
        public List<CardData> GenerateDeck(DeckType deckType)
        {
            List<CardData> cards = new();
            Rank minRank = deckType.GetMinRank();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (int r = (int)minRank; r <= (int)Rank.Ace; r++)
                {
                    cards.Add(new CardData(suit, (Rank)r));
                }
            }

            if (deckType == DeckType.Standard54)
            {
                cards.Add(CardData.CreateJoker(true));
                cards.Add(CardData.CreateJoker(false));
            }

            return cards;
        }

        /// <summary>
        ///     Validates configuration.
        /// </summary>
        /// <param name="errors">Error messages.</param>
        /// <returns>True if valid.</returns>
        public bool Validate(out List<string> errors)
        {
            return Validate(out errors, out _);
        }

        /// <summary>
        ///     Validates configuration, splitting errors and warnings.
        /// </summary>
        /// <param name="errors">Critical errors.</param>
        /// <param name="warnings">Warnings.</param>
        /// <returns>True if there are no critical errors.</returns>
        public bool Validate(out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();
            int expectedCount = GetExpectedCardCountPerSuit();

            if (_backSprite == null)
            {
                errors.Add("Card back sprite is not assigned");
            }

            ValidateSuit(_hearts, Suit.Hearts, expectedCount, errors);
            ValidateSuit(_diamonds, Suit.Diamonds, expectedCount, errors);
            ValidateSuit(_clubs, Suit.Clubs, expectedCount, errors);
            ValidateSuit(_spades, Suit.Spades, expectedCount, errors);

            if (_deckType == DeckType.Standard54 || _gameDeckType == DeckType.Standard54)
            {
                if (_redJoker == null)
                {
                    warnings.Add("Red joker sprite is not assigned (for 54-card deck)");
                }

                if (_blackJoker == null)
                {
                    warnings.Add("Black joker sprite is not assigned (for 54-card deck)");
                }
            }

            if (_gameDeckType.GetMinRank() < _deckType.GetMinRank())
            {
                errors.Add(
                    $"GameDeckType ({_gameDeckType}) requires cards from {_gameDeckType.GetMinRank()}, but DeckType ({_deckType}) starts at {_deckType.GetMinRank()}. Increase DeckType or lower GameDeckType.");
            }

            return errors.Count == 0;
        }

        private void ValidateSuit(List<Sprite> sprites, Suit suit, int expectedCount, List<string> errors)
        {
            if (sprites == null || sprites.Count == 0)
            {
                errors.Add($"No sprites assigned for suit {suit.ToEnglishName()}");
                return;
            }

            if (sprites.Count != expectedCount)
            {
                errors.Add(
                    $"Suit {suit.ToEnglishName()}: expected {expectedCount} sprites, found {sprites.Count}");
            }

            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] == null)
                {
                    errors.Add($"Suit {suit.ToEnglishName()}: sprite #{i + 1} is not assigned");
                }
            }
        }

        private List<Sprite> GetSuitSprites(Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => _hearts,
                Suit.Diamonds => _diamonds,
                Suit.Clubs => _clubs,
                Suit.Spades => _spades,
                _ => null
            };
        }

        private int GetSpriteIndex(Rank rank)
        {
            Rank minRank = _deckType.GetMinRank();
            return (int)rank - (int)minRank;
        }
    }
}
