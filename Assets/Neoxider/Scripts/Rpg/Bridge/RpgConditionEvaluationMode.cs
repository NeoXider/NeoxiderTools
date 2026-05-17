namespace Neo.Rpg
{
    /// <summary>
    ///     Evaluation mode for RPG condition checks.
    /// </summary>
    public enum RpgConditionEvaluationMode
    {
        HpAtLeast,
        HpPercentAtLeast,
        LevelAtLeast,
        IsDead,
        HasBuff,
        HasStatus,
        CanPerformActions,
        IsInvulnerable,
        CanEvade,
        AttackReady,
        ResourceAtLeast,
        ResourceBelow,
        ResourcePercentAtLeast,
        ResourcePercentBelow,
        StatAtLeast,
        StatBelow,
        UpgradePointsAtLeast,
        UpgradeLevelAtLeast,
        XpAtLeast
    }
}
