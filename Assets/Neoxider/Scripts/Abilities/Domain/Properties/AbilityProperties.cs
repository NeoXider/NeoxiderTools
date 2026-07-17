namespace Neo.Abilities
{
    /// <summary>
    ///     Well-known property ids. The registry is open: any string is a valid property id,
    ///     these constants only standardize the names the built-in systems read.
    /// </summary>
    public static class AbilityProperties
    {
        #region Movement

        public const string MoveSpeed = "move_speed";

        #endregion

        #region Offense

        public const string AttackDamage = "attack_damage";
        public const string AttackSpeed = "attack_speed";
        public const string AttackRange = "attack_range";
        public const string CritChance = "crit_chance";
        public const string CritMultiplier = "crit_multiplier";
        public const string OutgoingDamageMul = "outgoing_damage_mul";
        public const string SpellPower = "spell_power";
        public const string LifestealPercent = "lifesteal_percent";

        #endregion

        #region Defense

        public const string Armor = "armor";
        public const string MagicResistPercent = "magic_resist_percent";
        public const string IncomingDamageMul = "incoming_damage_mul";
        public const string ShieldHp = "shield_hp";
        public const string EvasionChance = "evasion_chance";

        #endregion

        #region Resources

        public const string MaxHealthBonus = "max_health_bonus";
        public const string MaxManaBonus = "max_mana_bonus";
        public const string HealthRegen = "health_regen";
        public const string ManaRegen = "mana_regen";
        public const string HealingReceivedMul = "healing_received_mul";

        #endregion

        #region Casting

        public const string CooldownReductionPercent = "cooldown_reduction_percent";
        public const string CastRangeBonus = "cast_range_bonus";
        public const string ManaCostMul = "mana_cost_mul";

        #endregion
    }
}
