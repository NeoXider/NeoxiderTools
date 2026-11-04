using System;
using System.Collections.Generic;

namespace Neo.Bonus
{
    /// <summary>
    ///     Read-only view of a <see cref="CollectionSet" />: queries and C# events, no mutators. Views and UI bind to this
    ///     so they work with a standalone set and with the state owned by a <see cref="Collection" /> component, whose
    ///     changes must go through the component to be persisted and to raise its UnityEvents.
    /// </summary>
    public interface IReadOnlyCollectionSet
    {
        /// <summary>Raised after a slot becomes unlocked, with its index.</summary>
        event Action<int> ItemUnlocked;

        /// <summary>Raised after a slot becomes locked again, with its index.</summary>
        event Action<int> ItemLocked;

        /// <summary>Raised after any change of composition: (unlockedCount, itemCount).</summary>
        event Action<int, int> CompletionChanged;

        /// <summary>Number of slots.</summary>
        int ItemCount { get; }

        /// <summary>Number of unlocked slots.</summary>
        int UnlockedCount { get; }

        /// <summary>Number of locked slots.</summary>
        int LockedCount { get; }

        /// <summary>Completion ratio 0..1; 0 for an empty set.</summary>
        float CompletionRatio { get; }

        /// <summary>True when the set has slots and every claimable slot is unlocked.</summary>
        bool IsComplete { get; }

        /// <summary>True when <paramref name="index" /> is in range and unlocked.</summary>
        bool IsUnlocked(int index);

        /// <summary>True when <paramref name="index" /> is in range and accepted by the claimable filter.</summary>
        bool IsClaimable(int index);

        /// <summary>Picks a locked, claimable slot without changing state; -1 when none is left.</summary>
        int PickLockedIndex(bool random = true, Func<int, int> randomIndex = null);

        /// <summary>Appends unlocked slot indices to <paramref name="results" /> without allocating. Returns the count added.</summary>
        int CopyUnlockedIndices(List<int> results);
    }
}
