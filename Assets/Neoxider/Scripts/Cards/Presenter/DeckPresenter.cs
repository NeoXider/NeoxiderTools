using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Cards
{
    /// <summary>
    ///     Презентер колоды - связывает модель колоды с визуальным представлением
    /// </summary>
    public class DeckPresenter
    {
        private readonly List<CardPresenter> _activeCards = new();
        private readonly CardView _cardPrefab;
        private readonly DeckConfig _config;

        /// <summary>
        ///     Создаёт презентер колоды
        /// </summary>
        /// <param name="model">Модель колоды</param>
        /// <param name="view">Визуальное представление</param>
        /// <param name="config">Конфигурация колоды</param>
        /// <param name="cardPrefab">Префаб карты</param>
        public DeckPresenter(DeckModel model, IDeckView view, DeckConfig config, CardView cardPrefab)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            View = view ?? throw new ArgumentNullException(nameof(view));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cardPrefab = cardPrefab ?? throw new ArgumentNullException(nameof(cardPrefab));

            Model.OnDeckChanged += HandleDeckChanged;
            Model.OnDeckEmpty += HandleDeckEmpty;
        }

        /// <summary>
        ///     Модель колоды
        /// </summary>
        public DeckModel Model { get; }

        /// <summary>
        ///     Визуальное представление
        /// </summary>
        public IDeckView View { get; }

        /// <summary>
        ///     Количество оставшихся карт
        /// </summary>
        public int RemainingCount => Model.RemainingCount;

        /// <summary>
        ///     Пуста ли колода
        /// </summary>
        public bool IsEmpty => Model.IsEmpty;

        /// <summary>
        ///     Козырная карта (нижняя карта колоды)
        /// </summary>
        public CardData? TrumpCard => Model.PeekBottom();

        /// <summary>
        ///     Событие когда колода опустела
        /// </summary>
        public event Action OnDeckEmpty;

        /// <summary>
        ///     Событие при взятии карты
        /// </summary>
        public event Action<CardPresenter> OnCardDrawn;

        /// <summary>
        ///     Инициализирует колоду
        /// </summary>
        /// <param name="shuffle">Перемешать</param>
        public void Initialize(bool shuffle = true)
        {
            Model.Initialize(_config.GameDeckType, shuffle);

            if (_config.GameDeckType == DeckType.Standard36)
            {
                CardData? trumpCard = Model.PeekBottom();
                if (trumpCard.HasValue)
                {
                    View.ShowTopCard(trumpCard.Value);
                }
            }
        }

        /// <summary>
        ///     Перемешивает колоду
        /// </summary>
        public void Shuffle()
        {
            Model.Shuffle();
        }

        /// <summary>
        ///     Берёт карту из колоды
        /// </summary>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Презентер взятой карты или null</returns>
        public CardPresenter DrawCard(bool faceUp = true)
        {
            CardData? cardData = Model.Draw();
            if (!cardData.HasValue)
            {
                return null;
            }

            CardView cardView = Object.Instantiate(_cardPrefab, View.SpawnPoint.position, Quaternion.identity);
            cardView.Initialize(_config);

            CardPresenter presenter = new(cardView, _config);
            presenter.SetData(cardData.Value, faceUp);

            _activeCards.Add(presenter);
            OnCardDrawn?.Invoke(presenter);

            return presenter;
        }

        /// <summary>
        ///     Берёт карту из колоды с анимацией
        /// </summary>
        /// <param name="targetPosition">Целевая позиция</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <param name="moveDuration">Длительность перемещения</param>
        /// <returns>Презентер взятой карты</returns>
        public async UniTask<CardPresenter> DrawCardAsync(Vector3 targetPosition, bool faceUp = true,
            float moveDuration = 0.3f)
        {
            CardPresenter presenter = DrawCard(faceUp);
            if (presenter == null)
            {
                return null;
            }

            await presenter.MoveToAsync(targetPosition, moveDuration);
            return presenter;
        }

        /// <summary>
        ///     Берёт несколько карт из колоды
        /// </summary>
        /// <param name="count">Количество карт</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Список презентеров карт</returns>
        public List<CardPresenter> DrawCards(int count, bool faceUp = true)
        {
            List<CardPresenter> presenters = new();

            for (int i = 0; i < count; i++)
            {
                CardPresenter presenter = DrawCard(faceUp);
                if (presenter != null)
                {
                    presenters.Add(presenter);
                }
            }

            return presenters;
        }

        /// <summary>
        ///     Возвращает карту в колоду
        /// </summary>
        /// <param name="presenter">Презентер карты</param>
        /// <param name="toTop">В начало колоды (true) или в конец (false)</param>
        public void ReturnCard(CardPresenter presenter, bool toTop = false)
        {
            if (presenter == null)
            {
                return;
            }

            _activeCards.Remove(presenter);

            if (toTop)
            {
                Model.ReturnToTop(presenter.Data);
            }
            else
            {
                Model.ReturnToBottom(presenter.Data);
            }

            presenter.Dispose();

            if (presenter.View is CardView cardView)
            {
                Object.Destroy(cardView.gameObject);
            }
        }

        /// <summary>
        ///     Сбрасывает колоду
        /// </summary>
        /// <param name="shuffle">Перемешать</param>
        public void Reset(bool shuffle = true)
        {
            foreach (CardPresenter presenter in _activeCards)
            {
                presenter.Dispose();
                if (presenter.View is CardView cardView)
                {
                    Object.Destroy(cardView.gameObject);
                }
            }

            _activeCards.Clear();

            Model.Reset(shuffle);
        }

        /// <summary>
        ///     Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            Model.OnDeckChanged -= HandleDeckChanged;
            Model.OnDeckEmpty -= HandleDeckEmpty;

            foreach (CardPresenter presenter in _activeCards)
            {
                presenter.Dispose();
            }

            _activeCards.Clear();
        }

        private void HandleDeckChanged()
        {
            View.UpdateVisual(Model.RemainingCount);
        }

        private void HandleDeckEmpty()
        {
            View.HideTopCard();
            OnDeckEmpty?.Invoke();
        }
    }
}