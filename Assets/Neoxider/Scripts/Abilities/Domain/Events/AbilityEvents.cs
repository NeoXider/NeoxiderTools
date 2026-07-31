namespace Neo.Abilities
{
    /// <summary>
    ///     Well-known gameplay event ids fired through <see cref="AbilityEventBus" />.
    ///     The registry is open: any string is a valid event id.
    /// </summary>
    public static class AbilityEvents
    {
        /// <summary>Fired on the victim after damage is applied. Amount = final damage dealt to HP.</summary>
        public const string TakeDamage = "take_damage";

        /// <summary>Fired on the attacker after damage is applied. Amount = final damage dealt.</summary>
        public const string DealDamage = "deal_damage";

        /// <summary>Fired on the healed unit. Amount = effective healing.</summary>
        public const string HealReceived = "heal_received";

        /// <summary>Fired on a unit when its health pool reaches zero.</summary>
        public const string Death = "death";

        /// <summary>Fired on the killer when its damage brings a unit to zero.</summary>
        public const string Kill = "kill";

        /// <summary>Fired on the caster when a cast is accepted (after costs are paid).</summary>
        public const string AbilityCast = "ability_cast";

        /// <summary>Fired on the owner when a modifier is applied to it.</summary>
        public const string ModifierApplied = "modifier_applied";

        /// <summary>Fired on the owner when a modifier is removed or expires.</summary>
        public const string ModifierRemoved = "modifier_removed";

        /// <summary>Fired on the owner when a shield contribution fully absorbs incoming damage.</summary>
        public const string ShieldAbsorbed = "shield_absorbed";

        /// <summary>Fired on the owner when the shield pool breaks (absorption exhausted).</summary>
        public const string ShieldBroken = "shield_broken";

        /// <summary>Fired on the attacker when its damage rolls a critical hit. Amount = damage dealt to HP.</summary>
        public const string CriticalHit = "critical_hit";

        /// <summary>Fired on the victim when a physical hit is evaded (no damage dealt). Amount = evaded raw amount.</summary>
        public const string Evaded = "evaded";
    }
}
