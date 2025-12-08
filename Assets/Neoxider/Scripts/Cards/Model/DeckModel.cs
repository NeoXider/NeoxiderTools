using System;
using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    /// Модель колоды карт (логика без визуализации)
    /// </summary>
    public class DeckModel : ICardContainer
    {
        private readonly CardContainerModel _cards = new CardContainerModel(CardLocation.Deck);
        private readonly List<CardData> _discardPile = new();
        private readonly Random _random = new();

        /// <summary>
        /// Карты в колоде
        /// </summary>
        public IReadOnlyList<CardData> Cards => _cards.Data;

        /// <summary>
        /// Карты в сбросе
        /// </summary>
        public IReadOnlyList<CardData> DiscardPile => _discardPile;

        /// <summary>
        /// Количество карт в колоде
        /// </summary>
        public int RemainingCount => _cards.Count;

        /// <summary>
        /// Пуста ли колода
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        /// Тип колоды
        /// </summary>
        public DeckType DeckType { get; private set; }

        public CardLocation Location => _cards.Location;
        public int Count => _cards.Count;
        public IReadOnlyList<CardData> Data => _cards.Data;

        /// <summary>
        /// Событие при изменении колоды
        /// </summary>
        public event Action OnDeckChanged;

        /// <summary>
        /// Событие когда колода опустела
        /// </summary>
        public event Action OnDeckEmpty;

        public event Action OnChanged
        {
            add => OnDeckChanged += value;
            remove => OnDeckChanged -= value;
        }

        /// <summary>
        /// Инициализирует колоду указанного типа
        /// </summary>
        /// <param name="deckType">Тип колоды</param>
        /// <param name="shuffle">Перемешать после создания</param>
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
        /// Инициализирует колоду из заданного списка карт
        /// </summary>
        /// <param name="cards">Список карт</param>
        /// <param name="shuffle">Перемешать после создания</param>
        public void Initialize(IEnumerable<CardData> cards, bool shuffle = true)
        {
            _cards.Clear();
            _discardPile.Clear();
            foreach (var card in cards)
                _cards.Add(card);

            if (shuffle)
            {
                Shuffle();
            }

            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// Перемешивает колоду (алгоритм Фишера-Йетса)
        /// </summary>
        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                ( _cards.Mutable[i], _cards.Mutable[j]) = ( _cards.Mutable[j], _cards.Mutable[i]);
            }

            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// Берёт верхнюю карту из колоды
        /// </summary>
        /// <returns>Карта или null если колода пуста</returns>
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
        /// Берёт несколько карт из колоды
        /// </summary>
        /// <param name="count">Количество карт</param>
        /// <returns>Список взятых карт</returns>
        public List<CardData> Draw(int count)
        {
            var result = new List<CardData>(count);

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
        /// Возвращает верхнюю карту без извлечения
        /// </summary>
        /// <returns>Верхняя карта или null если колода пуста</returns>
        public CardData? Peek()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            return _cards.Mutable[^1];
        }

        /// <summary>
        /// Возвращает нижнюю карту без извлечения (часто используется как козырь в "Дураке")
        /// </summary>
        /// <returns>Нижняя карта или null если колода пуста</returns>
        public CardData? PeekBottom()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            return _cards.Mutable[0];
        }

        /// <summary>
        /// Добавляет карту в сброс
        /// </summary>
        /// <param name="card">Карта для сброса</param>
        public void Discard(CardData card)
        {
            _discardPile.Add(card);
        }

        /// <summary>
        /// Добавляет несколько карт в сброс
        /// </summary>
        /// <param name="cards">Карты для сброса</param>
        public void Discard(IEnumerable<CardData> cards)
        {
            _discardPile.AddRange(cards);
        }

        /// <summary>
        /// Возвращает карту в колоду (вниз)
        /// </summary>
        /// <param name="card">Карта</param>
        public void ReturnToBottom(CardData card)
        {
            _cards.Mutable.Insert(0, card);
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// Возвращает карту в колоду (наверх)
        /// </summary>
        /// <param name="card">Карта</param>
        public void ReturnToTop(CardData card)
        {
            _cards.Add(card);
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// Перемешивает сброс и добавляет в колоду
        /// </summary>
        public void ReshuffleDiscardPile()
        {
            foreach (var card in _discardPile)
            {
                _cards.Add(card);
            }
            _discardPile.Clear();
            Shuffle();
        }

        /// <summary>
        /// Сбрасывает колоду в начальное состояние
        /// </summary>
        /// <param name="shuffle">Перемешать после сброса</param>
        public void Reset(bool shuffle = true)
        {
            Initialize(DeckType, shuffle);
        }

        /// <summary>
        /// Очищает колоду и сброс
        /// </summary>
        public void Clear()
        {
            _cards.Clear();
            _discardPile.Clear();
            OnDeckChanged?.Invoke();
        }

        #region ICardContainer explicit
        public bool CanAdd(CardData card) => true;
        bool ICardContainer.Remove(CardData card) => _cards.Remove(card);
        List<CardData> ICardContainer.RemoveAll() => _cards.RemoveAll();
        void ICardContainer.Clear() => Clear();
        void ICardContainer.Add(CardData card) => _cards.Add(card);
        #endregion
    }
}

