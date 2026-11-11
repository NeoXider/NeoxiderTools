using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    ///     Optional hand-view capability for reordering existing members with one layout pass.
    /// </summary>
    public interface IOrderedHandView : IHandView
    {
        /// <summary>
        ///     Applies an exact permutation of current members without add/remove callbacks or reparenting.
        ///     Invalid membership is rejected before mutation. Cancellation stops motion, not the committed order.
        /// </summary>
        UniTask SetOrderAsync(IReadOnlyList<ICardView> order, bool animate = true,
            CancellationToken cancellation = default);
    }
}
