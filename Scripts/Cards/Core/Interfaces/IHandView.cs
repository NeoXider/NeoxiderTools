using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    ///     View API for a hand (player cards).
    /// </summary>
    public interface IHandView
    {
        /// <summary>
        ///     Card views in the hand.
        /// </summary>
        IReadOnlyList<ICardView> CardViews { get; }

        /// <summary>
        ///     Number of cards in the hand.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Adds a card to the hand.
        /// </summary>
        /// <param name="cardView">Card view.</param>
        /// <param name="animate">Animate the add.</param>
        UniTask AddCardAsync(ICardView cardView, bool animate = true);

        /// <summary>
        ///     Removes a card from the hand.
        /// </summary>
        /// <param name="cardView">Card view.</param>
        /// <param name="animate">Animate the remove.</param>
        UniTask RemoveCardAsync(ICardView cardView, bool animate = true);

        /// <summary>
        ///     Re-layouts cards in the hand.
        /// </summary>
        /// <param name="animate">Animate rearrange.</param>
        UniTask ArrangeCardsAsync(bool animate = true);

        /// <summary>
        ///     Clears all cards from the hand.
        /// </summary>
        void Clear();
    }
}
