using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     A raw amount that may depend on a level, on what the cast spent and on a unit stat:
    ///     base (or the per-level entry) + PerLevel × (level − 1) + SpentCoefficient × spent, then
    ///     + ScalePerPoint × stat (Add) or × (1 + ScalePerPoint × stat) (Multiply). The stat is a property or a
    ///     resource pool's current / max / missing amount.
    /// </summary>
    public struct FixedLeveledValue
    {
        public int BaseRaw;
        public int[] LevelsRaw;
        public int PerLevelRaw;
        public string ScaleProperty;
        public int ScalePerPointRaw;
        public FixedScaleSource ScaleFrom;
        public FixedLevelSource LevelFrom;

        /// <summary>What the scaling term reads; <see cref="ScaleProperty" /> names a resource for the resource stats.</summary>
        public FixedScaleStat ScaleStat;

        public FixedScaleMode ScaleMode;

        /// <summary>Resource id whose spent amount adds the spent term; null or "" = off.</summary>
        public string SpentResource;

        /// <summary>Raw coefficient of the spent term: value += SpentCoefficientRaw × spent / one.</summary>
        public int SpentCoefficientRaw;

        public static FixedLeveledValue Flat(int raw)
        {
            return new FixedLeveledValue { BaseRaw = raw };
        }
    }

    /// <summary>
    ///     One effect of a fixed ability or modifier: the operation id, its targets and raw parameters. Times are
    ///     ticks, amounts / radii / fractions are raw in the system's format.
    /// </summary>
    public sealed class FixedEffectNode
    {
        /// <summary>Raw chance meaning "always"; any value at or above one raw always runs.</summary>
        public const int Always = int.MaxValue;

        public string OpId = FixedOps.Damage;
        public FixedTargetSelector Target = FixedTargetSelector.Target;
        public FixedTeamFilter TeamFilter = FixedTeamFilter.Enemies;
        public int RadiusRaw;
        public int[] RadiusByLevelRaw;
        public int MaxTargets;
        public int[] MaxTargetsByLevel;
        public int AmountRaw;
        public int[] AmountByLevelRaw;
        public string AmountKey;
        public string AmountScaleProperty;
        public int AmountScalePerPointRaw;
        public FixedScaleSource AmountScaleSource;
        public FixedLevelSource AmountLevelSource;
        public FixedScaleStat AmountScaleStat;
        public FixedScaleMode AmountScaleMode;
        public string AmountSpentResource;
        public int AmountSpentCoefficientRaw;
        public string DamageType = FixedDamageTypes.Magical;
        public string ModifierId;
        public string ResourceId;
        public string ArchetypeId;
        public string CustomParam;

        /// <summary>Raw chance in [0, one); <see cref="Always" /> (default) always runs.</summary>
        public int ChanceRaw = Always;

        /// <summary>Per-ability-level raw chances (same rule as <see cref="ChanceRaw" />); null or empty = ChanceRaw.</summary>
        public int[] ChanceByLevelRaw;

        public string AbilityId;

        /// <summary>Full cone angle in whole degrees (360 = full circle).</summary>
        public int AngleDegrees;

        public int DurationTicks;
        public int IntervalTicks;

        /// <summary>Per-ability-level zone lifetimes in ticks; null or empty = DurationTicks.</summary>
        public int[] DurationByLevelTicks;

        /// <summary>Per-ability-level zone pulse intervals in ticks; null or empty = IntervalTicks.</summary>
        public int[] IntervalByLevelTicks;

        public int AmountPerStackRaw;

        public FixedLeveledValue ToLeveledValue()
        {
            return new FixedLeveledValue
            {
                BaseRaw = AmountRaw,
                LevelsRaw = AmountByLevelRaw,
                ScaleProperty = AmountScaleProperty,
                ScalePerPointRaw = AmountScalePerPointRaw,
                ScaleFrom = AmountScaleSource,
                LevelFrom = AmountLevelSource,
                ScaleStat = AmountScaleStat,
                ScaleMode = AmountScaleMode,
                SpentResource = AmountSpentResource,
                SpentCoefficientRaw = AmountSpentCoefficientRaw
            };
        }

        public int ResolveChanceRaw(int abilityLevel)
        {
            return FixedValueResolver.SampleByLevel(ChanceByLevelRaw, abilityLevel, ChanceRaw);
        }

        public int ResolveMaxTargets(int abilityLevel)
        {
            return FixedValueResolver.SampleByLevel(MaxTargetsByLevel, abilityLevel, MaxTargets);
        }

        public int ResolveRadiusRaw(int abilityLevel)
        {
            return FixedValueResolver.SampleByLevel(RadiusByLevelRaw, abilityLevel, RadiusRaw);
        }

        public int ResolveDurationTicks(int abilityLevel)
        {
            return FixedValueResolver.SampleByLevel(DurationByLevelTicks, abilityLevel, DurationTicks);
        }

        public int ResolveIntervalTicks(int abilityLevel)
        {
            return FixedValueResolver.SampleByLevel(IntervalByLevelTicks, abilityLevel, IntervalTicks);
        }
    }

    /// <summary>
    ///     A modifier's contribution to a unit property. A Mul with a positive <see cref="Denominator" /> is the exact
    ///     rational ValueRaw / Denominator (e.g. 110 / 100) instead of a raw multiplier.
    /// </summary>
    public struct FixedPropertyContribution
    {
        public string PropertyId;
        public FixedPropertyOp Op;
        public int ValueRaw;
        public int PerStackRaw;

        /// <summary>0 = ValueRaw is a raw number; above 0 = ValueRaw / Denominator is an exact ratio (Mul only).</summary>
        public int Denominator;

        /// <summary>
        ///     Per-ability-level values in the same unit as <see cref="ValueRaw" /> (raw, or over <see cref="Denominator" />),
        ///     indexed by the level captured on the modifier; null or empty = ValueRaw.
        /// </summary>
        public int[] ValueByLevelRaw;

        public FixedPropertyContribution(string propertyId, FixedPropertyOp op, int valueRaw, int perStackRaw = 0,
            int denominator = 0)
        {
            PropertyId = propertyId;
            Op = op;
            ValueRaw = valueRaw;
            PerStackRaw = perStackRaw;
            Denominator = denominator;
            ValueByLevelRaw = null;
        }

        /// <summary>ValueRaw + PerStackRaw × (stacks − 1), ignoring <see cref="ValueByLevelRaw" />; the engine uses <see cref="ValueFor" />.</summary>
        public int ValueForStacks(int stacks)
        {
            int extra = stacks > 1 ? stacks - 1 : 0;
            return FixedMath.SaturatingAdd(ValueRaw, FixedMath.SaturatingMultiply(PerStackRaw, extra));
        }

        /// <summary>(ValueByLevelRaw sampled by the captured level, else ValueRaw) + PerStackRaw × (stacks − 1).</summary>
        public int ValueFor(int stacks, int abilityLevel)
        {
            int extra = stacks > 1 ? stacks - 1 : 0;
            int value = FixedValueResolver.SampleByLevel(ValueByLevelRaw, abilityLevel, ValueRaw);
            return FixedMath.SaturatingAdd(value, FixedMath.SaturatingMultiply(PerStackRaw, extra));
        }
    }

    /// <summary>A contribution resolved for the current stack count, as the aggregator sees it.</summary>
    public readonly struct FixedResolvedContribution
    {
        public readonly FixedPropertyOp Op;
        public readonly int ValueRaw;
        public readonly int Denominator;

        public FixedResolvedContribution(FixedPropertyOp op, int valueRaw, int denominator = 0)
        {
            Op = op;
            ValueRaw = valueRaw;
            Denominator = denominator;
        }
    }

    /// <summary>
    ///     Declarative reaction of a fixed modifier (the float ModifierEventReaction with ticks and raw values):
    ///     event, every Nth, health threshold crossing, state enter / exit, applied / removed; charges, internal
    ///     cooldown in ticks, priority, tag / ability / hit filters.
    /// </summary>
    public sealed class FixedModifierReaction
    {
        public string EventId;
        public List<FixedEffectNode> Effects = new List<FixedEffectNode>();
        public bool TargetEventSource;
        public FixedModifierTrigger Trigger = FixedModifierTrigger.Event;
        public int EveryN = 1;

        /// <summary>HealthBelow: raw fraction of max health (0.3 in Q8.8 = 77).</summary>
        public int ThresholdRaw;

        public string StateId;
        public string TagFilter;
        public string AbilityFilter;
        public bool HitsOnly;
        public int MaxTriggers;
        public bool RemoveWhenSpent;
        public int CooldownTicks;
        public int Priority;

        public bool IsEventBound =>
            Trigger == FixedModifierTrigger.Event || Trigger == FixedModifierTrigger.EveryNth ||
            Trigger == FixedModifierTrigger.HealthBelow;

        public bool NeedsState =>
            Trigger == FixedModifierTrigger.EveryNth || Trigger == FixedModifierTrigger.StateEnter ||
            Trigger == FixedModifierTrigger.StateExit || MaxTriggers > 0 || CooldownTicks > 0;
    }

    /// <summary>
    ///     A fixed modifier: duration and tick interval in ticks, raw property contributions, states, tick effects
    ///     and reactions. <see cref="StackingKind" /> is free for a host's own <see cref="IFixedStackingRule" />.
    /// </summary>
    public sealed class FixedModifierBlueprint
    {
        public string Id;

        /// <summary>Lifetime in ticks; 0 or less = permanent until removed.</summary>
        public int DurationTicks;

        public int[] DurationByLevelTicks;
        public FixedStackPolicy StackPolicy = FixedStackPolicy.Refresh;
        public int MaxStacks = 1;

        /// <summary>Host-defined stacking kind, read only by custom stacking rules (the default rule ignores it).</summary>
        public int StackingKind;

        public List<FixedPropertyContribution> Properties = new List<FixedPropertyContribution>();
        public List<string> States = new List<string>();
        public int TickIntervalTicks;
        public bool TickOnApply;

        /// <summary>
        ///     A re-application that refreshes, stacks or prolongs the modifier restarts the tick phase: the next tick
        ///     comes <see cref="TickIntervalTicks" /> after it. False (default): the phase continues.
        /// </summary>
        public bool RestartTicksOnRefresh;
        public List<FixedEffectNode> TickEffects = new List<FixedEffectNode>();
        public List<FixedModifierReaction> EventReactions = new List<FixedModifierReaction>();
        public bool IsDebuff;
        public bool Dispellable = true;

        public bool HasTicks => TickIntervalTicks > 0 && TickEffects != null && TickEffects.Count > 0;

        public int ResolveDurationTicks(int abilityLevel)
        {
            return FixedValueResolver.SampleByLevel(DurationByLevelTicks, abilityLevel, DurationTicks);
        }

        public FixedModifierBlueprint WithProperty(string propertyId, FixedPropertyOp op, int valueRaw,
            int perStackRaw = 0, int denominator = 0)
        {
            Properties.Add(new FixedPropertyContribution(propertyId, op, valueRaw, perStackRaw, denominator));
            return this;
        }
    }

    /// <summary>
    ///     A raw resource cost of a fixed ability: a flat raw amount, per-level raw amounts or a named special
    ///     (<see cref="AmountKey" />), taken exactly (Fixed), up to the amount (UpTo) or entirely (All).
    /// </summary>
    public struct FixedAbilityCost
    {
        public string ResourceId;
        public int AmountRaw;

        /// <summary>Per-ability-level raw amounts; null or empty = AmountRaw.</summary>
        public int[] AmountByLevelRaw;

        /// <summary>Named special of the ability giving the amount; null or "" = AmountRaw / AmountByLevelRaw.</summary>
        public string AmountKey;

        public FixedCostMode Mode;

        /// <summary>UpTo / All: raw amount that must be available for the cast to proceed.</summary>
        public int MinAmountRaw;

        public FixedAbilityCost(string resourceId, int amountRaw)
        {
            ResourceId = resourceId;
            AmountRaw = amountRaw;
            AmountByLevelRaw = null;
            AmountKey = null;
            Mode = FixedCostMode.Fixed;
            MinAmountRaw = 0;
        }

        public FixedAbilityCost(string resourceId, int amountRaw, FixedCostMode mode, int minAmountRaw = 0)
            : this(resourceId, amountRaw)
        {
            Mode = mode;
            MinAmountRaw = minAmountRaw;
        }

        /// <summary>
        ///     Raw amount at an ability level, before the cost rule and the spend mode: the named special when found, else
        ///     AmountByLevelRaw sampled by the level, else AmountRaw.
        /// </summary>
        public int ResolveAmountRaw(List<FixedAbilitySpecial> specials, FixedAbilitySystem system, int caster,
            int abilityLevel, int target)
        {
            if (!string.IsNullOrEmpty(AmountKey) && specials != null)
            {
                for (int i = 0; i < specials.Count; i++)
                {
                    if (FixedIdComparer.Same(specials[i].Name, AmountKey))
                    {
                        FixedLeveledValue special = specials[i].Value;
                        return FixedValueResolver.Resolve(in special, system, caster, abilityLevel, target);
                    }
                }
            }

            return FixedValueResolver.SampleByLevel(AmountByLevelRaw, abilityLevel, AmountRaw);
        }
    }

    /// <summary>How much of one resource a fixed cast actually paid (raw).</summary>
    public readonly struct FixedSpentResource
    {
        public readonly string ResourceId;
        public readonly int AmountRaw;

        public FixedSpentResource(string resourceId, int amountRaw)
        {
            ResourceId = resourceId;
            AmountRaw = amountRaw;
        }
    }

    /// <summary>A named leveled value an effect reads through <see cref="FixedEffectNode.AmountKey" />.</summary>
    public struct FixedAbilitySpecial
    {
        public string Name;
        public FixedLeveledValue Value;
    }

    /// <summary>
    ///     A fixed ability: targeting, raw range and costs, cooldown and charge restore in ticks, delivery, cast and
    ///     impact effects, named leveled values. Every cast parameter may have a per-level array (index = level − 1,
    ///     clamped; null or empty = the flat field), resolved at the slot level.
    /// </summary>
    public sealed class FixedAbilityBlueprint
    {
        public string Id;
        public FixedTargeting Targeting = FixedTargeting.NoTarget;
        public FixedTeamFilter TeamFilter = FixedTeamFilter.Enemies;
        public int RangeRaw;
        public int[] RangeByLevelRaw;
        public List<FixedAbilityCost> Costs = new List<FixedAbilityCost>();
        public int CooldownTicks;
        public int[] CooldownByLevelTicks;
        public int MaxCharges = 1;
        public int[] MaxChargesByLevel;
        public int ChargeRestoreTicks;
        public int[] ChargeRestoreByLevelTicks;
        public FixedDelivery Delivery = FixedDelivery.Instant;
        public string ProjectileArchetypeId;
        public int ProjectileSpeedRaw;
        public int[] ProjectileSpeedByLevelRaw;
        public List<FixedEffectNode> CastEffects = new List<FixedEffectNode>();
        public List<FixedEffectNode> ImpactEffects = new List<FixedEffectNode>();
        public List<FixedAbilitySpecial> Specials = new List<FixedAbilitySpecial>();

        /// <summary>True when the flat <see cref="MaxCharges" /> uses charges; level-aware code uses <see cref="UsesChargesAt" />.</summary>
        public bool UsesCharges => MaxCharges > 1;

        public int ResolveRangeRaw(int level)
        {
            return FixedValueResolver.SampleByLevel(RangeByLevelRaw, level, RangeRaw);
        }

        public int ResolveCooldownTicks(int level)
        {
            return FixedValueResolver.SampleByLevel(CooldownByLevelTicks, level, CooldownTicks);
        }

        public int ResolveMaxCharges(int level)
        {
            return FixedValueResolver.SampleByLevel(MaxChargesByLevel, level, MaxCharges);
        }

        public bool UsesChargesAt(int level)
        {
            return ResolveMaxCharges(level) > 1;
        }

        /// <summary>Ticks to restore one charge at a level: the restore entry when positive, else the level's cooldown.</summary>
        public int ResolveChargeRestoreTicks(int level)
        {
            int restore = FixedValueResolver.SampleByLevel(ChargeRestoreByLevelTicks, level, ChargeRestoreTicks);
            return restore > 0 ? restore : ResolveCooldownTicks(level);
        }

        public int ResolveProjectileSpeedRaw(int level)
        {
            return FixedValueResolver.SampleByLevel(ProjectileSpeedByLevelRaw, level, ProjectileSpeedRaw);
        }
    }
}
