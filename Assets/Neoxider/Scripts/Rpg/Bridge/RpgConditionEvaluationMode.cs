namespace Neo.Rpg
{
    /// <summary>
    /// Evaluation mode for RPG condition checks.
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
        AttackReady
    }
}
