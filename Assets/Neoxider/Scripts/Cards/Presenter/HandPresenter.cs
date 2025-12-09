using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    ///     Презентер руки - связывает модель руки с визуальным представлением
    /// </summary>
    public class HandPresenter
    {
        private readonly List<CardPresenter> _cardPresenters = new();

        /// <summary>
        ///     Создаёт презентер руки
        /// </summary>
        /// <param name="model">Модель руки</param>
        /// <param name="view">Визуальное представление</param>
        public HandPresenter(HandModel model, IHandView view)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            View = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>
        ///     Модель руки
        /// </summary>
        public HandModel Model { get; }

        /// <summary>
        ///     Визуальное представление
        /// </summary>
        public IHandView View { get; }

        /// <summary>
        ///     Количество карт в руке
        /// </summary>
        public int Count => Model.Count;

        /// <summary>
        ///     Пуста ли рука
        /// </summary>
        public bool IsEmpty => Model.IsEmpty;

        /// <summary>
        ///     Список презентеров карт
        /// </summary>
        public IReadOnlyList<CardPresenter> CardPresenters => _cardPresenters;

        /// <summary>
        ///     Событие добавления карты
        /// </summary>
        public event Action<CardPresenter> OnCardAdded;

        /// <summary>
        ///     Событие удаления карты
        /// </summary>
        public event Action<CardPresenter> OnCardRemoved;

        /// <summary>
        ///     Событие клика по карте
        /// </summary>
        public event Action<CardPresenter> OnCardClicked;

        /// <summary>
        ///     Добавляет карту в руку
        /// </summary>
        /// <param name="cardPresenter">Презентер карты</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask AddCardAsync(CardPresenter cardPresenter, bool animate = true)
        {
            if (cardPresenter == null)
            {
                return;
            }

            Model.Add(cardPresenter.Data);
            _cardPresenters.Add(cardPresenter);

            cardPresenter.OnClicked += HandleCardClicked;

            await View.AddCardAsync(cardPresenter.View, animate);

            OnCardAdded?.Invoke(cardPresenter);
        }

        /// <summary>
        ///     Удаляет карту из руки
        /// </summary>
        /// <param name="cardPresenter">Презентер карты</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask RemoveCardAsync(CardPresenter cardPresenter, bool animate = true)
        {
            if (cardPresenter == null)
            {
                return;
            }

            if (!_cardPresenters.Contains(cardPresenter))
            {
                return;
            }

            Model.Remove(cardPresenter.Data);
            _cardPresenters.Remove(cardPresenter);

            cardPresenter.OnClicked -= HandleCardClicked;

            await View.RemoveCardAsync(cardPresenter.View, animate);

            OnCardRemoved?.Invoke(cardPresenter);
        }

        /// <summary>
        ///     Удаляет карту по индексу
        /// </summary>
        /// <param name="index">Индекс карты</param>
        /// <param name="animate">Анимировать</param>
        /// <returns>Удалённый презентер карты</returns>
        public async UniTask<CardPresenter> RemoveAtAsync(int index, bool animate = true)
        {
            if (index < 0 || index >= _cardPresenters.Count)
            {
                return null;
            }

            CardPresenter presenter = _cardPresenters[index];
            await RemoveCardAsync(presenter, animate);
            return presenter;
        }

        /// <summary>
        ///     Находит презентер карты по данным
        /// </summary>
        /// <param name="cardData">Данные карты</param>
        /// <returns>Презентер карты или null</returns>
        public CardPresenter FindCard(CardData cardData)
        {
            return _cardPresenters.FirstOrDefault(p => p.Data.Equals(cardData));
        }

        /// <summary>
        ///     Находит карты, которыми можно побить указанную
        /// </summary>
        /// <param name="attackCard">Атакующая карта</param>
        /// <param name="trump">Козырная масть</param>
        /// <returns>Список презентеров карт</returns>
        public List<CardPresenter> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            return _cardPresenters
                .Where(p => p.Data.CanCover(attackCard, trump))
                .ToList();
        }

        /// <summary>
        ///     Находит карты с указанным рангом
        /// </summary>
        /// <param name="ranks">Ранги для поиска</param>
        /// <returns>Список презентеров карт</returns>
        public List<CardPresenter> GetCardsMatchingRanks(IEnumerable<Rank> ranks)
        {
            HashSet<Rank> rankSet = new(ranks);
            return _cardPresenters
                .Where(p => !p.Data.IsJoker && rankSet.Contains(p.Data.Rank))
                .ToList();
        }

        /// <summary>
        ///     Сортирует карты по рангу
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask SortByRankAsync(bool ascending = true, bool animate = true)
        {
            Model.SortByRank(ascending);

            if (ascending)
            {
                _cardPresenters.Sort((a, b) => a.Data.Rank.CompareTo(b.Data.Rank));
            }
            else
            {
                _cardPresenters.Sort((a, b) => b.Data.Rank.CompareTo(a.Data.Rank));
            }

            await View.ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Сортирует карты по масти
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask SortBySuitAsync(bool ascending = true, bool animate = true)
        {
            Model.SortBySuit(ascending);

            if (ascending)
            {
                _cardPresenters.Sort((a, b) =>
                {
                    int suitCompare = a.Data.Suit.CompareTo(b.Data.Suit);
                    return suitCompare != 0 ? suitCompare : a.Data.Rank.CompareTo(b.Data.Rank);
                });
            }
            else
            {
                _cardPresenters.Sort((a, b) =>
                {
                    int suitCompare = b.Data.Suit.CompareTo(a.Data.Suit);
                    return suitCompare != 0 ? suitCompare : b.Data.Rank.CompareTo(a.Data.Rank);
                });
            }

            await View.ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Очищает руку
        /// </summary>
        public void Clear()
        {
            foreach (CardPresenter presenter in _cardPresenters)
            {
                presenter.OnClicked -= HandleCardClicked;
            }

            _cardPresenters.Clear();
            Model.Clear();
            View.Clear();
        }

        /// <summary>
        ///     Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            Clear();
        }

        private void HandleCardClicked(CardPresenter presenter)
        {
            OnCardClicked?.Invoke(presenter);
        }
    }
}