namespace Neo.Abilities
{
    /// <summary>
    ///     The one place where a game swaps the rules of an <see cref="AbilitySystem" />: damage formula, mitigation,
    ///     critical hits, evasion, modifier stacking, property aggregation, effect targeting, cooldown and cost
    ///     resolution, the time source, the numeric backend and the random source. Every rule has a default that
    ///     reproduces the classic Neo.Abilities behaviour; assigning <c>null</c> restores that default.
    ///     A deterministic lockstep profile is just another set of implementations
    ///     (<see cref="DeterministicAbilityProfile" />).
    /// </summary>
    /// <remarks>
    ///     Rules are read on hot paths (every damage application, cast, tick and property lookup), so implementations
    ///     must not allocate per call. Swapping a rule bumps <see cref="Version" />, which invalidates cached property
    ///     values; change rules between ticks, not from inside an effect.
    /// </remarks>
    public sealed class AbilityRules
    {
        private IDamageFormula _damageFormula = StandardDamageFormula.Instance;
        private IMitigationRule _mitigation = ArmorResistMitigationRule.Instance;
        private ICriticalHitRule _criticalHit = PropertyCriticalHitRule.Instance;
        private IEvasionRule _evasion = PropertyEvasionRule.Instance;
        private IModifierStackingRule _stacking = StandardModifierStackingRule.Instance;
        private IPropertyAggregator _aggregator = LayeredPropertyAggregator.Instance;
        private IEffectTargetResolver _targeting;
        private ICooldownRule _cooldowns = StandardCooldownRule.Instance;
        private ICastCostRule _costs = StandardCastCostRule.Instance;
        private IAbilityTimeSource _time = new VariableStepTimeSource();
        private IAbilityNumeric _numeric = FloatNumeric.Instance;
        private IRandomSourceFactory _random = XorShiftRandomFactory.Instance;

        public AbilityRules()
        {
            _targeting = new StandardEffectTargetResolver();
        }

        /// <summary>Bumped by every rule change; caches derived from rules compare it for equality.</summary>
        public int Version { get; private set; }

        /// <summary>Turns a requested amount plus hook adjustments into the damage that reaches shields and health.</summary>
        public IDamageFormula DamageFormula
        {
            get => _damageFormula;
            set => Set(ref _damageFormula, value ?? StandardDamageFormula.Instance);
        }

        /// <summary>Armor / resistance mitigation by damage type, used by the standard damage formula.</summary>
        public IMitigationRule Mitigation
        {
            get => _mitigation;
            set => Set(ref _mitigation, value ?? ArmorResistMitigationRule.Instance);
        }

        /// <summary>Critical hit roll of the attacker, used by the standard damage formula.</summary>
        public ICriticalHitRule CriticalHit
        {
            get => _criticalHit;
            set => Set(ref _criticalHit, value ?? PropertyCriticalHitRule.Instance);
        }

        /// <summary>Evasion roll of the victim, used by the standard damage formula.</summary>
        public IEvasionRule Evasion
        {
            get => _evasion;
            set => Set(ref _evasion, value ?? PropertyEvasionRule.Instance);
        }

        /// <summary>What re-applying a modifier does (refresh, stack, prolong, ignore, a new instance...).</summary>
        public IModifierStackingRule Stacking
        {
            get => _stacking;
            set => Set(ref _stacking, value ?? StandardModifierStackingRule.Instance);
        }

        /// <summary>How base values and Add / Mul / Max contributions combine into a property value.</summary>
        public IPropertyAggregator Aggregator
        {
            get => _aggregator;
            set => Set(ref _aggregator, value ?? LayeredPropertyAggregator.Instance);
        }

        /// <summary>Resolves the units an effect node applies to (selectors, areas, cones, caps).</summary>
        public IEffectTargetResolver Targeting
        {
            get => _targeting;
            set => Set(ref _targeting, value ?? new StandardEffectTargetResolver());
        }

        /// <summary>Effective cooldown / charge restore time of a cast.</summary>
        public ICooldownRule Cooldowns
        {
            get => _cooldowns;
            set => Set(ref _cooldowns, value ?? StandardCooldownRule.Instance);
        }

        /// <summary>Effective resource cost of a cast.</summary>
        public ICastCostRule Costs
        {
            get => _costs;
            set => Set(ref _costs, value ?? StandardCastCostRule.Instance);
        }

        /// <summary>How host time becomes domain steps (variable seconds or fixed ticks); read by <see cref="AbilitySystem.Advance" />.</summary>
        public IAbilityTimeSource Time
        {
            get => _time;
            set => Set(ref _time, value ?? new VariableStepTimeSource());
        }

        /// <summary>Numeric backend: how computed amounts and property values are rounded (float or fixed point).</summary>
        public IAbilityNumeric Numeric
        {
            get => _numeric;
            set => Set(ref _numeric, value ?? FloatNumeric.Instance);
        }

        /// <summary>Creates the seeded random sources of casts, ticks, reactions and zones.</summary>
        public IRandomSourceFactory Random
        {
            get => _random;
            set => Set(ref _random, value ?? XorShiftRandomFactory.Instance);
        }

        /// <summary>Restores every default rule.</summary>
        public void ResetToDefaults()
        {
            _damageFormula = StandardDamageFormula.Instance;
            _mitigation = ArmorResistMitigationRule.Instance;
            _criticalHit = PropertyCriticalHitRule.Instance;
            _evasion = PropertyEvasionRule.Instance;
            _stacking = StandardModifierStackingRule.Instance;
            _aggregator = LayeredPropertyAggregator.Instance;
            _targeting = new StandardEffectTargetResolver();
            _cooldowns = StandardCooldownRule.Instance;
            _costs = StandardCastCostRule.Instance;
            _time = new VariableStepTimeSource();
            _numeric = FloatNumeric.Instance;
            _random = XorShiftRandomFactory.Instance;
            Version++;
        }

        private void Set<T>(ref T field, T value) where T : class
        {
            if (!ReferenceEquals(field, value))
            {
                field = value;
                Version++;
            }
        }
    }
}
