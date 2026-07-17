namespace Neo.Abilities
{
    /// <summary>
    ///     Payload of a gameplay event on the <see cref="AbilityEventBus" />.
    ///     Immutable value carrier; reference payloads stay out of the domain on purpose.
    /// </summary>
    public readonly struct AbilityEventArgs
    {
        /// <summary>Event id (see <see cref="AbilityEvents" />).</summary>
        public readonly string EventId;

        /// <summary>Unit the event happened to (victim, healed unit, caster of a cast...).</summary>
        public readonly UnitId Target;

        /// <summary>Unit that caused the event (attacker, healer...). May be None.</summary>
        public readonly UnitId Source;

        /// <summary>Ability involved, if any.</summary>
        public readonly string AbilityId;

        /// <summary>Modifier involved, if any.</summary>
        public readonly string ModifierId;

        /// <summary>Primary numeric payload (damage amount, heal amount...).</summary>
        public readonly float Amount;

        /// <summary>Damage type for damage events (see <see cref="AbilityDamageTypes" />).</summary>
        public readonly string DamageType;

        /// <summary>Cast id this event belongs to (0 when outside a cast). Correlates receipts per cast.</summary>
        public readonly uint CastId;

        public AbilityEventArgs(string eventId, UnitId target, UnitId source, float amount = 0f,
            string abilityId = null, string modifierId = null, string damageType = null, uint castId = 0)
        {
            EventId = eventId;
            Target = target;
            Source = source;
            Amount = amount;
            AbilityId = abilityId;
            ModifierId = modifierId;
            DamageType = damageType;
            CastId = castId;
        }
    }
}
