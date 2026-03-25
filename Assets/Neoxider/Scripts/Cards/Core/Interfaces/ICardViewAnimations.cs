using System.Threading;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    ///     Optional: plays built-in card animations (one-shot and looped).
    /// </summary>
    public interface ICardViewAnimations
    {
        /// <summary>
        ///     Plays a one-shot animation.
        /// </summary>
        /// <param name="type">Animation kind.</param>
        /// <param name="duration">Seconds; null uses config/default.</param>
        /// <param name="cancellation">Cancellation token.</param>
        UniTask PlayOneShotAsync(CardViewAnimationType type, float? duration = null,
            CancellationToken cancellation = default);

        /// <summary>
        ///     Starts a looped animation.
        /// </summary>
        /// <param name="type">Animation kind (e.g. PulseLooped, Idle).</param>
        /// <param name="duration">Single loop duration; null uses config.</param>
        void PlayLooped(CardViewAnimationType type, float? duration = null);

        /// <summary>
        ///     Stops a looped animation of the given type.
        /// </summary>
        void StopLooped(CardViewAnimationType type);

        /// <summary>
        ///     Stops all looped animations.
        /// </summary>
        void StopAllLooped();
    }
}
