namespace Neo.Abilities
{
    /// <summary>
    ///     The mutable state of one damage application while it runs through the <see cref="DamagePipeline" />.
    ///     Hooks of <c>before_deal_damage</c> and <c>before_take_damage</c> (event handlers reading
    ///     <see cref="AbilitySystem.ActiveDamage" />, or modifier reactions running <c>modify_damage</c> / <c>reflect</c>)
    ///     adjust <see cref="Multiplier" />, <see cref="FlatAdd" />, <see cref="Absorb" />, <see cref="ReflectFraction" />
    ///     or <see cref="Cancel" /> it; the rules then compute the final numbers.
    /// </summary>
    /// <remarks>
    ///     Contexts are pooled per nesting level and recycled when the application returns: never keep one. Hook
    ///     adjustments are accepted only while <see cref="CanModify" /> is true (the before hooks).
    /// </remarks>
    public sealed class DamageContext
    {
        internal DamageContext()
        {
        }

        public AbilitySystem System { get; private set; }

        /// <summary>Attacker (may be <see cref="UnitId.None" />).</summary>
        public UnitId Source { get; private set; }

        /// <summary>Victim.</summary>
        public UnitId Target { get; private set; }

        /// <summary>Ability the damage belongs to, if any.</summary>
        public string AbilityId { get; private set; }

        /// <summary>Cast the damage belongs to (0 outside a cast).</summary>
        public uint CastId { get; private set; }

        public DamageFlags Flags { get; private set; }

        /// <summary>Amount requested before hooks and mitigation.</summary>
        public float RawAmount { get; private set; }

        /// <summary>Seeded random source of the application (null for direct calls without one: no crit / evasion).</summary>
        public IRandomSource Random { get; private set; }

        /// <summary>Nesting level (1 = outermost application).</summary>
        public int Depth { get; private set; }

        /// <summary>Damage type; hooks may convert it (e.g. physical to magical).</summary>
        public string DamageType { get; set; }

        /// <summary>Multiplier applied to (RawAmount + FlatAdd) before the formula. 1 = unchanged.</summary>
        public float Multiplier { get; set; }

        /// <summary>Flat bonus added to the raw amount before the formula (mitigation still applies).</summary>
        public float FlatAdd { get; set; }

        /// <summary>Flat amount blocked after mitigation, before shields (counted as absorbed).</summary>
        public float Absorb { get; set; }

        /// <summary>Fraction of the mitigated damage dealt back to the attacker after the hit.</summary>
        public float ReflectFraction { get; set; }

        /// <summary>Damage type of the reflected part (default pure).</summary>
        public string ReflectDamageType { get; set; }

        /// <summary>True once a hook cancelled the application: nothing is dealt and no receipts follow.</summary>
        public bool IsCancelled { get; private set; }

        /// <summary>True while the before hooks run; adjustments made later have no effect.</summary>
        public bool CanModify { get; internal set; }

        /// <summary>Set by the evasion rule: the hit was dodged.</summary>
        public bool Evaded { get; set; }

        /// <summary>Set by the critical hit rule: the hit was critical.</summary>
        public bool Crit { get; set; }

        /// <summary>Critical multiplier the formula applied (1 without a crit).</summary>
        public float CritMultiplier { get; set; }

        /// <summary>Damage after the formula, before hook absorb and shields.</summary>
        public float Mitigated { get; internal set; }

        /// <summary>Damage blocked by hook absorb plus shield modifiers.</summary>
        public float Absorbed { get; internal set; }

        /// <summary>Damage actually removed from the health pool.</summary>
        public float HealthDamage { get; internal set; }

        public bool Killed { get; internal set; }

        public bool IsPhysical => AbilityIdComparer.Same(DamageType, AbilityDamageTypes.Physical);
        public bool IsMagical => AbilityIdComparer.Same(DamageType, AbilityDamageTypes.Magical);
        public bool IsPure => AbilityIdComparer.Same(DamageType, AbilityDamageTypes.Pure);
        public bool HasFlag(DamageFlags flag)
        {
            return (Flags & flag) != 0;
        }

        /// <summary>Cancels the application (only while <see cref="CanModify" />).</summary>
        public void Cancel()
        {
            if (CanModify)
            {
                IsCancelled = true;
            }
        }

        internal void Reset(AbilitySystem system, UnitId source, UnitId target, float amount, string damageType,
            string abilityId, uint castId, IRandomSource random, DamageFlags flags, int depth)
        {
            // WHY: reference fields are only written when they change; every write of a reference into a heap object
            // pays a GC write barrier, and this runs for every damage application.
            if (!ReferenceEquals(System, system))
            {
                System = system;
            }

            if (!ReferenceEquals(DamageType, damageType))
            {
                DamageType = damageType;
            }

            if (!ReferenceEquals(AbilityId, abilityId))
            {
                AbilityId = abilityId;
            }

            if (!ReferenceEquals(Random, random))
            {
                Random = random;
            }

            if (!ReferenceEquals(ReflectDamageType, AbilityDamageTypes.Pure))
            {
                ReflectDamageType = AbilityDamageTypes.Pure;
            }

            Source = source;
            Target = target;
            RawAmount = amount;
            CastId = castId;
            Flags = flags;
            Depth = depth;
            Multiplier = 1f;
            FlatAdd = 0f;
            Absorb = 0f;
            ReflectFraction = 0f;
            IsCancelled = false;
            CanModify = false;
            Evaded = false;
            Crit = false;
            CritMultiplier = 1f;
            Mitigated = 0f;
            Absorbed = 0f;
            HealthDamage = 0f;
            Killed = false;
        }

        /// <summary>Ends the application; references are kept for the next reset (they are long-lived ids and rules).</summary>
        internal void Clear()
        {
            CanModify = false;
        }
    }
}
