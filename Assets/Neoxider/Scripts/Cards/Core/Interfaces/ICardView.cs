using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     View API for a single card.
    /// </summary>
    public interface ICardView
    {
        /// <summary>
        ///     Card data.
        /// </summary>
        CardData Data { get; }

        /// <summary>
        ///     Whether the card is face up.
        /// </summary>
        bool IsFaceUp { get; }

        /// <summary>
        ///     Root transform.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        ///     Raised when the card is clicked.
        /// </summary>
        event Action<ICardView> OnClicked;

        /// <summary>
        ///     Raised when the pointer enters the card.
        /// </summary>
        event Action<ICardView> OnHovered;

        /// <summary>
        ///     Raised when the pointer exits the card.
        /// </summary>
        event Action<ICardView> OnUnhovered;

        /// <summary>
        ///     Sets card data and refreshes visuals.
        /// </summary>
        /// <param name="data">Card data.</param>
        /// <param name="faceUp">Show face up.</param>
        void SetData(CardData data, bool faceUp = true);

        /// <summary>
        ///     Flips the card instantly.
        /// </summary>
        void Flip();

        /// <summary>
        ///     Flips the card with animation.
        /// </summary>
        /// <param name="duration">Duration in seconds.</param>
        UniTask FlipAsync(float duration = 0.3f);

        /// <summary>
        ///     Moves the card with animation.
        /// </summary>
        /// <param name="position">World position.</param>
        /// <param name="duration">Duration in seconds.</param>
        UniTask MoveToAsync(Vector3 position, float duration = 0.2f);

        /// <summary>
        ///     Enables or disables interaction.
        /// </summary>
        /// <param name="interactable">Whether the card accepts input.</param>
        void SetInteractable(bool interactable);
    }
}
