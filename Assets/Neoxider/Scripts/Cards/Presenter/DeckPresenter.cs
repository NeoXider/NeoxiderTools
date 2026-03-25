using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Cards
{
    /// <summary>
    ///     Bridges <see cref="DeckModel" /> and <see cref="IDeckView" />.
    /// </summary>
    public class DeckPresenter
    {
        private readonly List<CardPresenter> _activeCards = new();
        private readonly CardView _cardPrefab;
        private readonly DeckConfig _config;

        /// <summary>
        ///     Creates a deck presenter.
        /// </summary>
        /// <param name="model">Deck model.</param>
        /// <param name="view">Deck view.</param>
        /// <param name="config">Deck config.</param>
        /// <param name="cardPrefab">Card view prefab.</param>
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
        ///     Deck model.
        /// </summary>
        public DeckModel Model { get; }

        /// <summary>
        ///     Deck view.
        /// </summary>
        public IDeckView View { get; }

        /// <summary>
        ///     Cards remaining in the model.
        /// </summary>
        public int RemainingCount => Model.RemainingCount;

        /// <summary>
        ///     Whether the deck is empty.
        /// </summary>
        public bool IsEmpty => Model.IsEmpty;

        /// <summary>
        ///     Trump / bottom card of the deck.
        /// </summary>
        public CardData? TrumpCard => Model.PeekBottom();

        /// <summary>
        ///     Raised when the deck runs out.
        /// </summary>
        public event Action OnDeckEmpty;

        /// <summary>
        ///     Raised after a successful draw.
        /// </summary>
        public event Action<CardPresenter> OnCardDrawn;

        /// <summary>
        ///     Initializes the deck from config.
        /// </summary>
        /// <param name="shuffle">Shuffle after build.</param>
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
        ///     Shuffles the model.
        /// </summary>
        public void Shuffle()
        {
            Model.Shuffle();
        }

        /// <summary>
        ///     Draws one card and instantiates a view.
        /// </summary>
        /// <param name="faceUp">Face up.</param>
        /// <returns>Presenter or null.</returns>
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
        ///     Draws a card and animates it to a position.
        /// </summary>
        /// <param name="targetPosition">Target position.</param>
        /// <param name="faceUp">Face up.</param>
        /// <param name="moveDuration">Move duration.</param>
        /// <returns>Presenter or null.</returns>
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
        ///     Draws multiple cards.
        /// </summary>
        /// <param name="count">How many.</param>
        /// <param name="faceUp">Face up.</param>
        /// <returns>Presenters (may be fewer if deck runs out).</returns>
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
        ///     Returns a drawn card to the model and destroys its view.
        /// </summary>
        /// <param name="presenter">Presenter to return.</param>
        /// <param name="toTop">Top of deck if true, bottom if false.</param>
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
        ///     Destroys active views and resets the model.
        /// </summary>
        /// <param name="shuffle">Shuffle after reset.</param>
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
        ///     Unsubscribes and clears active card views.
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
