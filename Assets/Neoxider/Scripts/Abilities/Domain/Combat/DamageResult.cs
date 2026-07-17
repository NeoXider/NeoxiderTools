namespace Neo.Abilities
{
    /// <summary>
    ///     Receipt of one damage application after the full pipeline (mitigation, shields, HP).
    /// </summary>
    public readonly struct DamageResult
    {
        /// <summary>Damage requested before any mitigation.</summary>
        public readonly float RawAmount;

        /// <summary>Damage after outgoing/incoming multipliers, armor and resistances, before shields.</summary>
        public readonly float MitigatedAmount;

        /// <summary>Portion absorbed by shield modifiers.</summary>
        public readonly float Absorbed;

        /// <summary>Damage actually subtracted from the health pool.</summary>
        public readonly float HealthDamage;

        /// <summary>True when this damage brought the target to zero health.</summary>
        public readonly bool Killed;

        /// <summary>True when the damage was fully negated (invulnerable, immune, dead target...).</summary>
        public readonly bool Negated;

        /// <summary>True when a physical hit was dodged via evasion_chance (no damage, no take/deal events).</summary>
        public readonly bool Evaded;

        /// <summary>True when this application rolled a critical hit (crit_chance / crit_multiplier).</summary>
        public readonly bool Crit;

        public DamageResult(float rawAmount, float mitigatedAmount, float absorbed, float healthDamage,
            bool killed, bool negated, bool evaded = false, bool crit = false)
        {
            RawAmount = rawAmount;
            MitigatedAmount = mitigatedAmount;
            Absorbed = absorbed;
            HealthDamage = healthDamage;
            Killed = killed;
            Negated = negated;
            Evaded = evaded;
            Crit = crit;
        }

        public static DamageResult None(float raw)
        {
            return new DamageResult(raw, 0f, 0f, 0f, false, true);
        }

        /// <summary>An evaded physical hit: no damage, flagged so callers can present a dodge.</summary>
        public static DamageResult Evade(float raw)
        {
            return new DamageResult(raw, 0f, 0f, 0f, false, false, true);
        }
    }
}
