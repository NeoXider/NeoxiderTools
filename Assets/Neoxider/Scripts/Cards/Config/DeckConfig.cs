using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    /// Конфигурация колоды карт со спрайтами
    /// </summary>
    [CreateAssetMenu(fileName = "DeckConfig", menuName = "Neo/Cards/Deck Config")]
    public class DeckConfig : ScriptableObject
    {
        [Header("Deck Settings")]
        [Tooltip("Тип колоды для спрайтов (сколько карт загружено в конфиг)")]
        [SerializeField] private DeckType _deckType = DeckType.Standard52;
        
        [Tooltip("Тип колоды для игры (сколько карт использовать). Позволяет иметь все спрайты, но играть меньшим количеством карт.")]
        [SerializeField] private DeckType _gameDeckType = DeckType.Standard54;

        [Header("Card Back")]
        [SerializeField] private Sprite _backSprite;

        [Header("Hearts (от младшей к старшей)")]
        [SerializeField] private List<Sprite> _hearts = new();

        [Header("Diamonds (от младшей к старшей)")]
        [SerializeField] private List<Sprite> _diamonds = new();

        [Header("Clubs (от младшей к старшей)")]
        [SerializeField] private List<Sprite> _clubs = new();

        [Header("Spades (от младшей к старшей)")]
        [SerializeField] private List<Sprite> _spades = new();

        [Header("Jokers (для 54 карт)")]
        [SerializeField] private Sprite _redJoker;
        [SerializeField] private Sprite _blackJoker;

        /// <summary>
        /// Тип колоды для спрайтов (определяет количество загруженных спрайтов)
        /// </summary>
        public DeckType DeckType => _deckType;

        /// <summary>
        /// Тип колоды для игры (определяет сколько карт использовать в игре)
        /// </summary>
        public DeckType GameDeckType => _gameDeckType;

        /// <summary>
        /// Спрайт рубашки карты
        /// </summary>
        public Sprite BackSprite => _backSprite;

        /// <summary>
        /// Спрайт красного джокера
        /// </summary>
        public Sprite RedJoker => _redJoker;

        /// <summary>
        /// Спрайт чёрного джокера
        /// </summary>
        public Sprite BlackJoker => _blackJoker;

        /// <summary>
        /// Спрайты карт червей
        /// </summary>
        public IReadOnlyList<Sprite> Hearts => _hearts;

        /// <summary>
        /// Спрайты карт бубен
        /// </summary>
        public IReadOnlyList<Sprite> Diamonds => _diamonds;

        /// <summary>
        /// Спрайты карт треф
        /// </summary>
        public IReadOnlyList<Sprite> Clubs => _clubs;

        /// <summary>
        /// Спрайты карт пик
        /// </summary>
        public IReadOnlyList<Sprite> Spades => _spades;

        /// <summary>
        /// Возвращает спрайт для указанной карты
        /// </summary>
        /// <param name="card">Данные карты</param>
        /// <returns>Спрайт карты или null если не найден</returns>
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
        /// Возвращает ожидаемое количество карт для одной масти
        /// </summary>
        public int GetExpectedCardCountPerSuit()
        {
            return _deckType.GetCardsPerSuit();
        }

        /// <summary>
        /// Генерирует список карт для игры (использует GameDeckType)
        /// </summary>
        /// <returns>Список карт</returns>
        public List<CardData> GenerateDeck()
        {
            return GenerateDeck(_gameDeckType);
        }

        /// <summary>
        /// Генерирует список карт указанного типа колоды
        /// </summary>
        /// <param name="deckType">Тип колоды для генерации</param>
        /// <returns>Список карт</returns>
        public List<CardData> GenerateDeck(DeckType deckType)
        {
            var cards = new List<CardData>();
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
        /// Проверяет валидность конфигурации
        /// </summary>
        /// <param name="errors">Список ошибок</param>
        /// <returns>true если конфигурация валидна</returns>
        public bool Validate(out List<string> errors)
        {
            return Validate(out errors, out _);
        }

        /// <summary>
        /// Проверяет валидность конфигурации с разделением на ошибки и предупреждения
        /// </summary>
        /// <param name="errors">Список критических ошибок</param>
        /// <param name="warnings">Список предупреждений</param>
        /// <returns>true если нет критических ошибок</returns>
        public bool Validate(out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();
            int expectedCount = GetExpectedCardCountPerSuit();

            if (_backSprite == null)
            {
                errors.Add("Не указан спрайт рубашки карты");
            }

            ValidateSuit(_hearts, Suit.Hearts, expectedCount, errors);
            ValidateSuit(_diamonds, Suit.Diamonds, expectedCount, errors);
            ValidateSuit(_clubs, Suit.Clubs, expectedCount, errors);
            ValidateSuit(_spades, Suit.Spades, expectedCount, errors);

            if (_deckType == DeckType.Standard54 || _gameDeckType == DeckType.Standard54)
            {
                if (_redJoker == null)
                {
                    warnings.Add("Не указан спрайт красного джокера (для колоды 54)");
                }
                if (_blackJoker == null)
                {
                    warnings.Add("Не указан спрайт чёрного джокера (для колоды 54)");
                }
            }

            if (_gameDeckType.GetMinRank() < _deckType.GetMinRank())
            {
                errors.Add($"GameDeckType ({_gameDeckType}) требует карты от {_gameDeckType.GetMinRank()}, но DeckType ({_deckType}) начинается с {_deckType.GetMinRank()}. Увеличьте DeckType или уменьшите GameDeckType.");
            }

            return errors.Count == 0;
        }

        private void ValidateSuit(List<Sprite> sprites, Suit suit, int expectedCount, List<string> errors)
        {
            if (sprites == null || sprites.Count == 0)
            {
                errors.Add($"Не указаны спрайты для масти {suit.ToRussianName()}");
                return;
            }

            if (sprites.Count != expectedCount)
            {
                errors.Add($"Масть {suit.ToRussianName()}: ожидается {expectedCount} спрайтов, указано {sprites.Count}");
            }

            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] == null)
                {
                    errors.Add($"Масть {suit.ToRussianName()}: спрайт #{i + 1} не указан");
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

