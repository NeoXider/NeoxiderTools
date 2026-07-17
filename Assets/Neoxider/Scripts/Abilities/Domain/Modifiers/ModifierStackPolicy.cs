namespace Neo.Abilities
{
    /// <summary>
    ///     How re-applying the same modifier blueprint to the same unit behaves.
    /// </summary>
    public enum ModifierStackPolicy
    {
        /// <summary>Every application creates an independent instance with its own duration.</summary>
        Independent = 0,

        /// <summary>A single instance per unit; re-applying refreshes its duration.</summary>
        Refresh = 1,

        /// <summary>A single instance per unit; re-applying adds a stack (up to MaxStacks) and refreshes duration.</summary>
        Stack = 2
    }
}
