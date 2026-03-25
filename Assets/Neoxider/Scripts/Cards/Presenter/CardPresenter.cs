using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Bridges <see cref="CardData" /> and <see cref="ICardView" />.
    /// </summary>
    public class CardPresenter
    {
        private readonly DeckConfig _config;

        /// <summary>
        ///     Creates a card presenter.
        /// </summary>
        /// <param name="view">Card view.</param>
        /// <param name="config">Deck config.</param>
        public CardPresenter(ICardView view, DeckConfig config)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            View.OnClicked += HandleClick;
        }

        /// <summary>
        ///     Card data.
        /// </summary>
        public CardData Data { get; private set; }

        /// <summary>
        ///     Card view.
        /// </summary>
        public ICardView View { get; }

        /// <summary>
        ///     Whether the card is face up.
        /// </summary>
        public bool IsFaceUp { get; private set; }

        /// <summary>
        ///     View transform.
        /// </summary>
        public Transform Transform => View.Transform;

        /// <summary>
        ///     Raised when the card is clicked.
        /// </summary>
        public event Action<CardPresenter> OnClicked;

        /// <summary>
        ///     Sets card data on the view.
        /// </summary>
        /// <param name="data">Card data.</param>
        /// <param name="faceUp">Face up.</param>
        public void SetData(CardData data, bool faceUp = true)
        {
            Data = data;
            IsFaceUp = faceUp;
            View.SetData(data, faceUp);
        }

        /// <summary>
        ///     Flips instantly.
        /// </summary>
        public void Flip()
        {
            IsFaceUp = !IsFaceUp;
            View.Flip();
        }

        /// <summary>
        ///     Flips with animation.
        /// </summary>
        /// <param name="duration">Duration.</param>
        public async UniTask FlipAsync(float duration = 0.3f)
        {
            IsFaceUp = !IsFaceUp;
            await View.FlipAsync(duration);
        }

        /// <summary>
        ///     Moves to a world position.
        /// </summary>
        /// <param name="position">Target position.</param>
        /// <param name="duration">Move duration.</param>
        public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
        {
            await View.MoveToAsync(position, duration);
        }

        /// <summary>
        ///     Sets whether the view accepts input.
        /// </summary>
        /// <param name="interactable">Interactable.</param>
        public void SetInteractable(bool interactable)
        {
            View.SetInteractable(interactable);
        }

        /// <summary>
        ///     Unsubscribes view events.
        /// </summary>
        public void Dispose()
        {
            View.OnClicked -= HandleClick;
        }

        private void HandleClick(ICardView view)
        {
            OnClicked?.Invoke(this);
        }
    }
}
