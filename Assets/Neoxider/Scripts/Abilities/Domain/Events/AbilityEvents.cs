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

        /// <summary>Fired on a unit when it is brought back from death. Amount = restored health.</summary>
        public const string Revive = "revive";

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

        /// <summary>
        ///     Hook on the attacker before damage is computed (Target = attacker, Source = victim, Amount = raw).
        ///     Handlers adjust <see cref="AbilitySystem.ActiveDamage" />; modifier reactions use modify_damage / reflect.
        /// </summary>
        public const string BeforeDealDamage = "before_deal_damage";

        /// <summary>Hook on the victim before damage is computed (Target = victim, Source = attacker, Amount = raw).</summary>
        public const string BeforeTakeDamage = "before_take_damage";

        /// <summary>
        ///     Fired on the attacker when a direct hit of a cast or projectile lands (Source = victim, Amount = health
        ///     damage), including a hit fully absorbed by shields. Damage-over-time ticks, reactions and reflection do not
        ///     count. deal_damage still fires for every damage application.
        /// </summary>
        public const string AttackLanded = "attack_landed";

        /// <summary>Fired on the victim of a direct hit (the counterpart of attack_landed; Source = attacker).</summary>
        public const string AttackReceived = "attack_received";

        /// <summary>Fired on the unit that reflected damage (Source = attacker, Amount = reflected amount).</summary>
        public const string DamageReflected = "damage_reflected";

        /// <summary>
        ///     An animation moment raised by the presentation layer through
        ///     <see cref="AbilitySystem.RaiseAnimationEvent" /> (Tag = moment name, e.g. "hit_frame").
        /// </summary>
        public const string AnimationEvent = "animation_event";

        /// <summary>Fired on a unit that gained a boolean state (Tag = state id) watched by a StateEnter trigger.</summary>
        public const string StateEntered = "state_entered";

        /// <summary>Fired on a unit that lost a boolean state (Tag = state id) watched by a StateExit trigger.</summary>
        public const string StateExited = "state_exited";

        /// <summary>Fired on the caster when a zone appears (CastId = zone id, Amount = radius, Tag = archetype id).</summary>
        public const string ZoneSpawned = "zone_spawned";

        /// <summary>Fired on the caster when a zone ends (CastId = zone id, Tag = archetype id).</summary>
        public const string ZoneExpired = "zone_expired";
    }
}
