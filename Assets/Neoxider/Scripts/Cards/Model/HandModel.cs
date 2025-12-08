using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Cards
{
    /// <summary>
    /// Модель руки игрока (набор карт)
    /// </summary>
    public class HandModel : ICardContainer
    {
        private readonly CardContainerModel _cards = new CardContainerModel(CardLocation.Hand);

        /// <summary>
        /// Карты в руке
        /// </summary>
        public IReadOnlyList<CardData> Cards => _cards.Data;

        /// <summary>
        /// Количество карт в руке
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        /// Пуста ли рука
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        public CardLocation Location => _cards.Location;
        IReadOnlyList<CardData> ICardContainer.Data => _cards.Data;

        /// <summary>
        /// Событие добавления карты
        /// </summary>
        public event Action<CardData> OnCardAdded;

        /// <summary>
        /// Событие удаления карты
        /// </summary>
        public event Action<CardData> OnCardRemoved;

        /// <summary>
        /// Событие изменения руки
        /// </summary>
        public event Action OnHandChanged;

        public event Action OnChanged
        {
            add => OnHandChanged += value;
            remove => OnHandChanged -= value;
        }

        /// <summary>
        /// Добавляет карту в руку
        /// </summary>
        /// <param name="card">Карта для добавления</param>
        public void Add(CardData card)
        {
            _cards.Add(card);
            OnCardAdded?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Добавляет несколько карт в руку
        /// </summary>
        /// <param name="cards">Карты для добавления</param>
        public void AddRange(IEnumerable<CardData> cards)
        {
            foreach (var card in cards)
            {
                _cards.Add(card);
                OnCardAdded?.Invoke(card);
            }
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Удаляет карту из руки
        /// </summary>
        /// <param name="card">Карта для удаления</param>
        /// <returns>true если карта была удалена</returns>
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
        /// Удаляет карту по индексу
        /// </summary>
        /// <param name="index">Индекс карты</param>
        /// <returns>Удалённая карта</returns>
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
        /// Очищает руку
        /// </summary>
        public void Clear()
        {
            _cards.Clear();
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Проверяет наличие карты в руке
        /// </summary>
        /// <param name="card">Карта для поиска</param>
        /// <returns>true если карта есть в руке</returns>
        public bool Contains(CardData card)
        {
            return _cards.Data.Contains(card);
        }

        /// <summary>
        /// Проверяет наличие карты с указанным рангом
        /// </summary>
        /// <param name="rank">Ранг для поиска</param>
        /// <returns>true если есть карта с таким рангом</returns>
        public bool ContainsRank(Rank rank)
        {
            return _cards.Data.Any(c => !c.IsJoker && c.Rank == rank);
        }

        /// <summary>
        /// Проверяет наличие карты с указанной мастью
        /// </summary>
        /// <param name="suit">Масть для поиска</param>
        /// <returns>true если есть карта с такой мастью</returns>
        public bool ContainsSuit(Suit suit)
        {
            return _cards.Data.Any(c => !c.IsJoker && c.Suit == suit);
        }

        /// <summary>
        /// Возвращает карту по индексу
        /// </summary>
        /// <param name="index">Индекс карты</param>
        /// <returns>Карта</returns>
        public CardData GetAt(int index)
        {
            return _cards.Data[index];
        }

        /// <summary>
        /// Находит индекс карты
        /// </summary>
        /// <param name="card">Карта для поиска</param>
        /// <returns>Индекс или -1 если не найдена</returns>
        public int IndexOf(CardData card)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards.Data[i].Equals(card))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Сортирует карты по рангу
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
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
        /// Сортирует карты по масти, затем по рангу
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
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
        /// Возвращает все карты указанной масти
        /// </summary>
        /// <param name="suit">Масть</param>
        /// <returns>Список карт</returns>
        public List<CardData> GetCardsBySuit(Suit suit)
        {
            return _cards.Data.Where(c => !c.IsJoker && c.Suit == suit).ToList();
        }

        /// <summary>
        /// Возвращает все карты указанного ранга
        /// </summary>
        /// <param name="rank">Ранг</param>
        /// <returns>Список карт</returns>
        public List<CardData> GetCardsByRank(Rank rank)
        {
            return _cards.Data.Where(c => !c.IsJoker && c.Rank == rank).ToList();
        }

        /// <summary>
        /// Находит карты, которыми можно побить указанную карту
        /// </summary>
        /// <param name="attackCard">Атакующая карта</param>
        /// <param name="trump">Козырная масть</param>
        /// <returns>Список карт, которыми можно побить</returns>
        public List<CardData> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            return _cards.Data.Where(c => c.CanCover(attackCard, trump)).ToList();
        }

        /// <summary>
        /// Находит карты с таким же рангом (для подкидывания в "Дураке")
        /// </summary>
        /// <param name="ranks">Ранги на столе</param>
        /// <returns>Список карт для подкидывания</returns>
        public List<CardData> GetCardsMatchingRanks(IEnumerable<Rank> ranks)
        {
            var rankSet = new HashSet<Rank>(ranks);
            return _cards.Data.Where(c => !c.IsJoker && rankSet.Contains(c.Rank)).ToList();
        }

        /// <summary>
        /// Возвращает минимальную карту в руке
        /// </summary>
        /// <param name="trump">Козырная масть (козыри считаются выше)</param>
        /// <returns>Минимальная карта или null если рука пуста</returns>
        public CardData? GetLowestCard(Suit? trump = null)
        {
            if (_cards.Count == 0)
                return null;

            return _cards.Data
                .Where(c => !c.IsJoker)
                .OrderBy(c => trump.HasValue && c.Suit == trump.Value ? 1 : 0)
                .ThenBy(c => c.Rank)
                .FirstOrDefault();
        }

        /// <summary>
        /// Возвращает максимальную карту в руке
        /// </summary>
        /// <param name="trump">Козырная масть</param>
        /// <returns>Максимальная карта или null если рука пуста</returns>
        public CardData? GetHighestCard(Suit? trump = null)
        {
            if (_cards.Count == 0)
                return null;

            return _cards.Data
                .Where(c => !c.IsJoker)
                .OrderByDescending(c => trump.HasValue && c.Suit == trump.Value ? 1 : 0)
                .ThenByDescending(c => c.Rank)
                .FirstOrDefault();
        }

        #region ICardContainer explicit
        public bool CanAdd(CardData card) => true;
        void ICardContainer.Add(CardData card) => Add(card);
        bool ICardContainer.Remove(CardData card) => Remove(card);
        List<CardData> ICardContainer.RemoveAll()
        {
            var snapshot = new List<CardData>(_cards.Data);
            _cards.Clear();
            return snapshot;
        }
        void ICardContainer.Clear() => Clear();
        #endregion
    }
}

