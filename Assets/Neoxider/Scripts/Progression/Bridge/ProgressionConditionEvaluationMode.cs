namespace Neo.Progression
{
    /// <summary>
    ///     Evaluation mode for progression condition checks. Values are serialized; append new members only.
    /// </summary>
    public enum ProgressionConditionEvaluationMode
    {
        /// <summary>Node Id has been unlocked.</summary>
        HasUnlockedNode,

        /// <summary>Perk Id has been purchased.</summary>
        HasPurchasedPerk,

        /// <summary>Current Core level is at least Threshold.</summary>
        LevelAtLeast,

        /// <summary>Total Core XP is at least Threshold.</summary>
        XpAtLeast,

        /// <summary>Unspent perk points are at least Threshold.</summary>
        PerkPointsAtLeast,

        /// <summary>The profile owns the premium track.</summary>
        HasPremium
    }
}
