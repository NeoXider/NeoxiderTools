namespace Neo.Abilities
{
    /// <summary>
    ///     How re-applying the same modifier blueprint to the same unit behaves. Read by the default
    ///     <see cref="StandardModifierStackingRule" />; a custom <see cref="IModifierStackingRule" /> may ignore it.
    /// </summary>
    public enum ModifierStackPolicy
    {
        /// <summary>Every application creates an independent instance with its own duration.</summary>
        Independent = 0,

        /// <summary>A single instance per unit; re-applying refreshes its duration.</summary>
        Refresh = 1,

        /// <summary>A single instance per unit; re-applying adds a stack (up to MaxStacks) and refreshes duration.</summary>
        Stack = 2,

        /// <summary>A single instance per unit; re-applying while it is active does nothing.</summary>
        Unique = 3,

        /// <summary>A single instance per unit; re-applying adds the full duration to the remaining time.</summary>
        Prolong = 4,

        /// <summary>Independent instances up to MaxStacks at once; one more replaces the oldest.</summary>
        IndependentCapped = 5
    }
}
