namespace Neo.Progression
{
    /// <summary>
    ///     Evaluation mode for progression condition checks.
    /// </summary>
    public enum ProgressionConditionEvaluationMode
    {
        HasUnlockedNode,
        HasPurchasedPerk,
        LevelAtLeast,
        XpAtLeast,
        PerkPointsAtLeast
    }
}
