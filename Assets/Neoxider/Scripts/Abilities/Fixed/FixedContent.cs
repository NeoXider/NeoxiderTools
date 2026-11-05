using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     A raw amount that may depend on a level and scale with a unit property:
    ///     base (or the per-level entry) + PerLevel × (level − 1) + ScalePerPoint × property.
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
        public int AmountRaw;
        public int[] AmountByLevelRaw;
        public string AmountKey;
        public string AmountScaleProperty;
        public int AmountScalePerPointRaw;
        public FixedScaleSource AmountScaleSource;
        public FixedLevelSource AmountLevelSource;
        public string DamageType = FixedDamageTypes.Magical;
        public string ModifierId;
        public string ResourceId;
        public string ArchetypeId;
        public string CustomParam;

        /// <summary>Raw chance in [0, one); <see cref="Always" /> (default) always runs.</summary>
        public int ChanceRaw = Always;

        public string AbilityId;

        /// <summary>Full cone angle in whole degrees (360 = full circle).</summary>
        public int AngleDegrees;

        public int DurationTicks;
        public int IntervalTicks;
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
                LevelFrom = AmountLevelSource
            };
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

        public FixedPropertyContribution(string propertyId, FixedPropertyOp op, int valueRaw, int perStackRaw = 0,
            int denominator = 0)
        {
            PropertyId = propertyId;
            Op = op;
            ValueRaw = valueRaw;
            PerStackRaw = perStackRaw;
            Denominator = denominator;
        }

        public int ValueForStacks(int stacks)
        {
            int extra = stacks > 1 ? stacks - 1 : 0;
            return FixedMath.SaturatingAdd(ValueRaw, FixedMath.SaturatingMultiply(PerStackRaw, extra));
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

    /// <summary>A raw resource cost of a fixed ability.</summary>
    public struct FixedAbilityCost
    {
        public string ResourceId;
        public int AmountRaw;

        public FixedAbilityCost(string resourceId, int amountRaw)
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
    ///     impact effects, named leveled values.
    /// </summary>
    public sealed class FixedAbilityBlueprint
    {
        public string Id;
        public FixedTargeting Targeting = FixedTargeting.NoTarget;
        public FixedTeamFilter TeamFilter = FixedTeamFilter.Enemies;
        public int RangeRaw;
        public List<FixedAbilityCost> Costs = new List<FixedAbilityCost>();
        public int CooldownTicks;
        public int MaxCharges = 1;
        public int ChargeRestoreTicks;
        public FixedDelivery Delivery = FixedDelivery.Instant;
        public string ProjectileArchetypeId;
        public int ProjectileSpeedRaw;
        public List<FixedEffectNode> CastEffects = new List<FixedEffectNode>();
        public List<FixedEffectNode> ImpactEffects = new List<FixedEffectNode>();
        public List<FixedAbilitySpecial> Specials = new List<FixedAbilitySpecial>();

        public bool UsesCharges => MaxCharges > 1;
    }
}
