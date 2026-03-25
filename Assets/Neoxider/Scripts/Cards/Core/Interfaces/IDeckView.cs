using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     View API for a deck stack.
    /// </summary>
    public interface IDeckView
    {
        /// <summary>
        ///     Spawn point for drawn cards.
        /// </summary>
        Transform SpawnPoint { get; }

        /// <summary>
        ///     Number of visible cards in the stack visual.
        /// </summary>
        int VisibleCardCount { get; set; }

        /// <summary>
        ///     Refreshes the deck visual.
        /// </summary>
        /// <param name="remainingCount">Cards remaining in the deck.</param>
        void UpdateVisual(int remainingCount);

        /// <summary>
        ///     Shows the top deck card (e.g. trump indicator).
        /// </summary>
        /// <param name="card">Card to display.</param>
        void ShowTopCard(CardData card);

        /// <summary>
        ///     Hides the top deck card display.
        /// </summary>
        void HideTopCard();
    }
}
