namespace Neo.Rpg
{
    /// <summary>
    /// Common combat receiver contract used by RPG attacks, projectiles, and abilities.
    /// </summary>
    public interface IRpgCombatReceiver
    {
        float CurrentHp { get; }
        float MaxHp { get; }
        int Level { get; }
        bool IsDead { get; }
        bool IsInvulnerable { get; }
        bool CanPerformActions { get; }
        float TakeDamage(float amount);
        float Heal(float amount);
        /// <summary>Spend resource (e.g. Mana, HP). When no resource provider, returns false.</summary>
        bool TrySpendResource(string resourceId, float amount, out string failReason);
        bool TryApplyBuff(string buffId, out string failReason);
        bool TryApplyStatus(string statusId, out string failReason);
        void AddInvulnerabilityLock();
        void RemoveInvulnerabilityLock();
        float GetOutgoingDamageMultiplier();
        float GetMovementSpeedMultiplier();
    }
}
