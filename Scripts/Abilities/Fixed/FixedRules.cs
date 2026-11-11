using System;
using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>Raw damage before shields: formula, evasion, outgoing and incoming multipliers, crit, mitigation.</summary>
    public interface IFixedDamageFormula
    {
        int Compute(FixedDamageContext damage, FixedUnit source, FixedUnit target, FixedRules rules);
    }

    /// <summary>Armor / resistance step of the formula (raw in, raw out).</summary>
    public interface IFixedMitigationRule
    {
        int Mitigate(FixedDamageContext damage, FixedUnit target, int amountRaw);
    }

    /// <summary>Critical hit roll; the multiplier is raw (2.0 = two ones).</summary>
    public interface IFixedCriticalHitRule
    {
        bool TryRollCritical(FixedDamageContext damage, FixedUnit source, out int multiplierRaw);
    }

    /// <summary>Evasion roll of the target.</summary>
    public interface IFixedEvasionRule
    {
        bool RollEvasion(FixedDamageContext damage, FixedUnit target);
    }

    /// <summary>What re-applying a modifier does (existing = the oldest active instance of that id, or null).</summary>
    public interface IFixedStackingRule
    {
        FixedStackAction Decide(FixedModifierBlueprint blueprint, FixedModifierInstance existing, int activeInstances);
    }

    /// <summary>Combines a raw base value with the active contributions of one property. Must be pure.</summary>
    public interface IFixedPropertyAggregator
    {
        int Compute(string propertyId, int baseRaw, List<FixedResolvedContribution> contributions, FixedFormat format);
    }

    /// <summary>Fills the unit ids an effect node acts on.</summary>
    public interface IFixedTargetResolver
    {
        void Resolve(FixedEffectNode node, FixedEffectContext context, List<int> results);
    }

    /// <summary>Effective cooldown in ticks of a cast.</summary>
    public interface IFixedCooldownRule
    {
        int ResolveCooldownTicks(FixedUnit caster, FixedAbilityBlueprint ability, int baseTicks);
    }

    /// <summary>Effective raw amount of one authored cost.</summary>
    public interface IFixedCastCostRule
    {
        int ResolveCost(FixedUnit caster, FixedAbilityBlueprint ability, FixedAbilityCost cost);
    }

    /// <summary>
    ///     Replaceable rules of one fixed system, registered in one place: damage formula, mitigation, crit, evasion,
    ///     stacking, property aggregation, targeting, cooldowns, costs and the random factory, plus the Q format and
    ///     the tick rate. Setting a rule to null restores its default; every change bumps <see cref="Version" />,
    ///     which invalidates cached properties.
    /// </summary>
    public sealed class FixedRules
    {
        private IFixedDamageFormula _damageFormula = FixedDamageFormula.Instance;
        private IFixedMitigationRule _mitigation = FixedArmorResistMitigation.Instance;
        private IFixedCriticalHitRule _criticalHit = FixedPropertyCriticalHit.Instance;
        private IFixedEvasionRule _evasion = FixedPropertyEvasion.Instance;
        private IFixedStackingRule _stacking = FixedStandardStackingRule.Instance;
        private IFixedPropertyAggregator _aggregator = FixedLayeredPropertyAggregator.Instance;
        private IFixedTargetResolver _targeting = new FixedStandardTargetResolver();
        private IFixedCooldownRule _cooldowns = FixedStandardCooldownRule.Instance;
        private IFixedCastCostRule _costs = FixedStandardCastCostRule.Instance;
        private IFixedRandomFactory _random = FixedXorShiftRandomFactory.Instance;
        private FixedFormat _format = FixedFormat.Q8;
        private int _ticksPerSecond = 30;

        public int Version { get; private set; } = 1;

        public IFixedDamageFormula DamageFormula
        {
            get => _damageFormula;
            set => Set(ref _damageFormula, value ?? FixedDamageFormula.Instance);
        }

        public IFixedMitigationRule Mitigation
        {
            get => _mitigation;
            set => Set(ref _mitigation, value ?? FixedArmorResistMitigation.Instance);
        }

        public IFixedCriticalHitRule CriticalHit
        {
            get => _criticalHit;
            set => Set(ref _criticalHit, value ?? FixedPropertyCriticalHit.Instance);
        }

        public IFixedEvasionRule Evasion
        {
            get => _evasion;
            set => Set(ref _evasion, value ?? FixedPropertyEvasion.Instance);
        }

        public IFixedStackingRule Stacking
        {
            get => _stacking;
            set => Set(ref _stacking, value ?? FixedStandardStackingRule.Instance);
        }

        public IFixedPropertyAggregator Aggregator
        {
            get => _aggregator;
            set => Set(ref _aggregator, value ?? FixedLayeredPropertyAggregator.Instance);
        }

        public IFixedTargetResolver Targeting
        {
            get => _targeting;
            set => Set(ref _targeting, value ?? new FixedStandardTargetResolver());
        }

        public IFixedCooldownRule Cooldowns
        {
            get => _cooldowns;
            set => Set(ref _cooldowns, value ?? FixedStandardCooldownRule.Instance);
        }

        public IFixedCastCostRule Costs
        {
            get => _costs;
            set => Set(ref _costs, value ?? FixedStandardCastCostRule.Instance);
        }

        public IFixedRandomFactory Random
        {
            get => _random;
            set => Set(ref _random, value ?? FixedXorShiftRandomFactory.Instance);
        }

        /// <summary>Q format of every raw value; set it before creating units and content.</summary>
        public FixedFormat Format
        {
            get => _format;
            set
            {
                if (!_format.Equals(value))
                {
                    _format = value;
                    Version++;
                }
            }
        }

        /// <summary>Ticks per simulated second; converts per-second properties (regeneration) to ticks.</summary>
        public int TicksPerSecond
        {
            get => _ticksPerSecond;
            set
            {
                int clamped = value < 1 ? 1 : value;
                if (clamped != _ticksPerSecond)
                {
                    _ticksPerSecond = clamped;
                    Version++;
                }
            }
        }

        /// <summary>Restores every default rule (format and tick rate are kept).</summary>
        public void ResetToDefaults()
        {
            _damageFormula = FixedDamageFormula.Instance;
            _mitigation = FixedArmorResistMitigation.Instance;
            _criticalHit = FixedPropertyCriticalHit.Instance;
            _evasion = FixedPropertyEvasion.Instance;
            _stacking = FixedStandardStackingRule.Instance;
            _aggregator = FixedLayeredPropertyAggregator.Instance;
            _targeting = new FixedStandardTargetResolver();
            _cooldowns = FixedStandardCooldownRule.Instance;
            _costs = FixedStandardCastCostRule.Instance;
            _random = FixedXorShiftRandomFactory.Instance;
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

    /// <summary>
    ///     Default formula, the float StandardDamageFormula on raw values: (raw + flat) × multiplier → evasion →
    ///     × outgoing_damage_mul → crit → mitigation → × incoming_damage_mul. The multiplicative steps before
    ///     mitigation run with 16 extra bits and round once.
    /// </summary>
    public class FixedDamageFormula : IFixedDamageFormula
    {
        public static readonly FixedDamageFormula Instance = new FixedDamageFormula();

        private const int WideBits = 16;

        public virtual int Compute(FixedDamageContext damage, FixedUnit source, FixedUnit target, FixedRules rules)
        {
            int one = rules.Format.One;
            long amount = (long)damage.RawAmount + damage.FlatAddRaw;
            if (amount <= 0L || damage.MultiplierRaw <= 0)
            {
                return 0;
            }

            if (!damage.HasFlag(FixedDamageFlags.NoEvasion) && rules.Evasion.RollEvasion(damage, target))
            {
                damage.Evaded = true;
                return 0;
            }

            long wide = FixedMath.MulDiv(amount << WideBits, damage.MultiplierRaw, one);
            if (source != null)
            {
                int outgoing = FixedMath.Max(0, source.GetProperty(FixedProperties.OutgoingDamageMul, one));
                wide = FixedMath.MulDiv(wide, outgoing, one);
                if (!damage.HasFlag(FixedDamageFlags.NoCrit) &&
                    rules.CriticalHit.TryRollCritical(damage, source, out int multiplier))
                {
                    damage.Crit = true;
                    damage.CritMultiplierRaw = multiplier;
                    wide = FixedMath.MulDiv(wide, multiplier, one);
                }
            }

            int mitigated = rules.Mitigation.Mitigate(damage, target,
                FixedMath.DivRoundHalfAwayFromZeroToInt(wide, 1L << WideBits));
            int incoming = FixedMath.Max(0, target.GetProperty(FixedProperties.IncomingDamageMul, one));
            return FixedMath.MulDivToInt(mitigated, incoming, one);
        }
    }

    /// <summary>
    ///     Default mitigation, the float ArmorResistMitigationRule as exact ratios: physical × 100 / (100 + 6·armor)
    ///     (negative armor: × (100 + 12·|armor|) / (100 + 6·|armor|)), magical × (100 − resist%) / 100 with resist
    ///     clamped to ±100, pure unchanged.
    /// </summary>
    public class FixedArmorResistMitigation : IFixedMitigationRule
    {
        public static readonly FixedArmorResistMitigation Instance = new FixedArmorResistMitigation();

        public virtual int Mitigate(FixedDamageContext damage, FixedUnit target, int amountRaw)
        {
            int one = damage.System.Rules.Format.One;
            long hundred = 100L * one;
            if (damage.IsPhysical)
            {
                long armor = target.GetProperty(FixedProperties.Armor);
                if (armor >= 0L)
                {
                    return FixedMath.MulDivToInt(amountRaw, hundred, hundred + 6L * armor);
                }

                long magnitude = -armor;
                return FixedMath.MulDivToInt(amountRaw, hundred + 12L * magnitude, hundred + 6L * magnitude);
            }

            if (damage.IsMagical)
            {
                long resist = FixedMath.Clamp(target.GetProperty(FixedProperties.MagicResistPercent), -hundred, hundred);
                return FixedMath.MulDivToInt(amountRaw, hundred - resist, hundred);
            }

            return amountRaw;
        }
    }

    /// <summary>Default crit: chance from crit_chance (raw fraction), multiplier max(1, crit_multiplier).</summary>
    public class FixedPropertyCriticalHit : IFixedCriticalHitRule
    {
        public static readonly FixedPropertyCriticalHit Instance = new FixedPropertyCriticalHit();

        public virtual bool TryRollCritical(FixedDamageContext damage, FixedUnit source, out int multiplierRaw)
        {
            int one = damage.System.Rules.Format.One;
            multiplierRaw = one;
            if (damage.Random == null)
            {
                return false;
            }

            int chance = source.GetProperty(FixedProperties.CritChance);
            if (chance > 0 && damage.Random.NextRaw(one) < chance)
            {
                multiplierRaw = FixedMath.Max(one, source.GetProperty(FixedProperties.CritMultiplier));
                return true;
            }

            return false;
        }
    }

    /// <summary>Default evasion: physical hits only, chance from evasion_chance (raw fraction).</summary>
    public class FixedPropertyEvasion : IFixedEvasionRule
    {
        public static readonly FixedPropertyEvasion Instance = new FixedPropertyEvasion();

        public virtual bool RollEvasion(FixedDamageContext damage, FixedUnit target)
        {
            if (damage.Random == null || !damage.IsPhysical)
            {
                return false;
            }

            int evasion = target.GetProperty(FixedProperties.EvasionChance);
            return evasion > 0 && damage.Random.NextRaw(damage.System.Rules.Format.One) < evasion;
        }
    }

    /// <summary>Default stacking by <see cref="FixedModifierBlueprint.StackPolicy" />, identical to the float rule.</summary>
    public class FixedStandardStackingRule : IFixedStackingRule
    {
        public static readonly FixedStandardStackingRule Instance = new FixedStandardStackingRule();

        public virtual FixedStackAction Decide(FixedModifierBlueprint blueprint, FixedModifierInstance existing,
            int activeInstances)
        {
            switch (blueprint.StackPolicy)
            {
                case FixedStackPolicy.Independent:
                    return FixedStackAction.CreateNew;
                case FixedStackPolicy.IndependentCapped:
                    int cap = blueprint.MaxStacks > 0 ? blueprint.MaxStacks : 1;
                    return activeInstances >= cap ? FixedStackAction.ReplaceOldest : FixedStackAction.CreateNew;
            }

            if (existing == null)
            {
                return FixedStackAction.CreateNew;
            }

            switch (blueprint.StackPolicy)
            {
                case FixedStackPolicy.Stack:
                    return FixedStackAction.AddStack;
                case FixedStackPolicy.Unique:
                    return FixedStackAction.Ignore;
                case FixedStackPolicy.Prolong:
                    return FixedStackAction.Prolong;
                default:
                    return FixedStackAction.Refresh;
            }
        }
    }

    /// <summary>
    ///     Default aggregation: max((base + ΣAdd) × ΠMul, every Max floor), multipliers compounding. The product is
    ///     kept with 30 fraction bits (exact for raw multipliers, rationals to 2^-30) and rounded once.
    /// </summary>
    public sealed class FixedLayeredPropertyAggregator : IFixedPropertyAggregator
    {
        public static readonly FixedLayeredPropertyAggregator Instance = new FixedLayeredPropertyAggregator();

        public int Compute(string propertyId, int baseRaw, List<FixedResolvedContribution> contributions,
            FixedFormat format)
        {
            long sum = baseRaw;
            long product = FixedPropertyMath.UnitQ30;
            bool hasFloor = false;
            int floor = int.MinValue;
            for (int i = 0; i < contributions.Count; i++)
            {
                FixedResolvedContribution c = contributions[i];
                switch (c.Op)
                {
                    case FixedPropertyOp.Add:
                        sum = FixedMath.SaturatingAdd(sum, c.ValueRaw);
                        break;
                    case FixedPropertyOp.Mul:
                        product = c.Denominator > 0
                            ? FixedMath.MulDiv(product, c.ValueRaw, c.Denominator)
                            : FixedMath.MulDiv(product, c.ValueRaw, format.One);
                        break;
                    case FixedPropertyOp.Max:
                        hasFloor = true;
                        floor = c.ValueRaw > floor ? c.ValueRaw : floor;
                        break;
                }
            }

            int result = FixedMath.MulDivToInt(sum, product, FixedPropertyMath.UnitQ30);
            return hasFloor && floor > result ? floor : result;
        }
    }

    /// <summary>Additive percentages: max((base + ΣAdd) × (1 + Σ(Mul − 1)), floors); two ×1.5 give ×2.</summary>
    public sealed class FixedAdditivePercentPropertyAggregator : IFixedPropertyAggregator
    {
        public static readonly FixedAdditivePercentPropertyAggregator Instance =
            new FixedAdditivePercentPropertyAggregator();

        public int Compute(string propertyId, int baseRaw, List<FixedResolvedContribution> contributions,
            FixedFormat format)
        {
            long sum = baseRaw;
            long percent = 0L;
            bool hasFloor = false;
            int floor = int.MinValue;
            for (int i = 0; i < contributions.Count; i++)
            {
                FixedResolvedContribution c = contributions[i];
                switch (c.Op)
                {
                    case FixedPropertyOp.Add:
                        sum = FixedMath.SaturatingAdd(sum, c.ValueRaw);
                        break;
                    case FixedPropertyOp.Mul:
                        long numerator = c.Denominator > 0 ? (long)c.ValueRaw - c.Denominator : (long)c.ValueRaw - format.One;
                        long denominator = c.Denominator > 0 ? c.Denominator : format.One;
                        percent = FixedMath.SaturatingAdd(percent,
                            FixedMath.MulDiv(numerator, FixedPropertyMath.UnitQ30, denominator));
                        break;
                    case FixedPropertyOp.Max:
                        hasFloor = true;
                        floor = c.ValueRaw > floor ? c.ValueRaw : floor;
                        break;
                }
            }

            int result = FixedMath.MulDivToInt(sum, FixedPropertyMath.UnitQ30 + percent, FixedPropertyMath.UnitQ30);
            return hasFloor && floor > result ? floor : result;
        }
    }

    internal static class FixedPropertyMath
    {
        public const long UnitQ30 = 1L << 30;
    }

    /// <summary>
    ///     Default cooldown: base ticks × (100 − cooldown_reduction_percent clamped to 0..90) / 100, rounded up, so a
    ///     cooldown ends on the same step as in the float core.
    /// </summary>
    public class FixedStandardCooldownRule : IFixedCooldownRule
    {
        public static readonly FixedStandardCooldownRule Instance = new FixedStandardCooldownRule();

        public virtual int ResolveCooldownTicks(FixedUnit caster, FixedAbilityBlueprint ability, int baseTicks)
        {
            if (baseTicks <= 0)
            {
                return 0;
            }

            int one = caster.System.Rules.Format.One;
            long hundred = 100L * one;
            long cdr = FixedMath.Clamp(caster.GetProperty(FixedProperties.CooldownReductionPercent), 0L, 90L * one);
            long scaled = (long)baseTicks * (hundred - cdr);
            return FixedMath.ClampToInt((scaled + hundred - 1L) / hundred);
        }
    }

    /// <summary>Default cost: mana costs × max(0, mana_cost_mul); other costs unchanged.</summary>
    public class FixedStandardCastCostRule : IFixedCastCostRule
    {
        public static readonly FixedStandardCastCostRule Instance = new FixedStandardCastCostRule();

        public virtual int ResolveCost(FixedUnit caster, FixedAbilityBlueprint ability, FixedAbilityCost cost)
        {
            if (!FixedIdComparer.Same(cost.ResourceId, FixedResources.Mana))
            {
                return cost.AmountRaw;
            }

            int one = caster.System.Rules.Format.One;
            int multiplier = FixedMath.Max(0, caster.GetProperty(FixedProperties.ManaCostMul, one));
            return FixedMath.MulDivToInt(cost.AmountRaw, multiplier, one);
        }
    }

    /// <summary>
    ///     Default targeting, the float StandardEffectTargetResolver on raw coordinates: caster, primary targets,
    ///     areas around the target or caster and cones from the caster (whole-degree angles, integer cosine table).
    ///     With MaxTargets the nearest N are kept, by squared distance then unit id.
    /// </summary>
    public class FixedStandardTargetResolver : IFixedTargetResolver
    {
        private static readonly Comparison<AreaHit> AreaHitOrder = CompareAreaHits;
        private readonly List<int> _unitScratch = new List<int>(16);
        private readonly List<AreaHit> _areaScratch = new List<AreaHit>(16);

        public virtual void Resolve(FixedEffectNode node, FixedEffectContext context, List<int> results)
        {
            IFixedWorldAdapter world = context.System.World;
            switch (node.Target)
            {
                case FixedTargetSelector.Caster:
                    results.Add(context.Caster);
                    break;
                case FixedTargetSelector.Target:
                    for (int i = 0; i < context.PrimaryTargets.Count; i++)
                    {
                        results.Add(context.PrimaryTargets[i]);
                    }

                    break;
                case FixedTargetSelector.AreaAroundTarget:
                {
                    FixedVec3 center;
                    if (context.PrimaryTargets.Count > 0 &&
                        world.TryGetPosition(context.PrimaryTargets[0], out FixedVec3 targetPosition))
                    {
                        center = targetPosition;
                    }
                    else if (context.HasTargetPoint)
                    {
                        center = context.TargetPoint;
                    }
                    else
                    {
                        return;
                    }

                    CollectArea(center, node, context, results, false, FixedVec3.Zero, 0L);
                    break;
                }
                case FixedTargetSelector.AreaAroundCaster:
                    if (world.TryGetPosition(context.Caster, out FixedVec3 casterPosition))
                    {
                        CollectArea(casterPosition, node, context, results, false, FixedVec3.Zero, 0L);
                    }

                    break;
                case FixedTargetSelector.ConeFromCaster:
                {
                    if (node.AngleDegrees <= 0 || !world.TryGetPosition(context.Caster, out FixedVec3 origin) ||
                        !TryGetAim(context, world, origin, out FixedVec3 aim))
                    {
                        return;
                    }

                    if (node.AngleDegrees >= 360)
                    {
                        CollectArea(origin, node, context, results, false, FixedVec3.Zero, 0L);
                        return;
                    }

                    CollectArea(origin, node, context, results, true, aim, FixedCone.CosHalfQ30(node.AngleDegrees));
                    break;
                }
            }
        }

        private static bool TryGetAim(FixedEffectContext context, IFixedWorldAdapter world, FixedVec3 origin,
            out FixedVec3 aim)
        {
            if (context.HasTargetPoint)
            {
                aim = context.TargetPoint - origin;
            }
            else if (context.PrimaryTargets.Count > 0 && context.PrimaryTargets[0] != context.Caster &&
                     world.TryGetPosition(context.PrimaryTargets[0], out FixedVec3 targetPosition))
            {
                aim = targetPosition - origin;
            }
            else
            {
                aim = FixedVec3.Zero;
                return false;
            }

            return !aim.IsZero;
        }

        private void CollectArea(FixedVec3 center, FixedEffectNode node, FixedEffectContext context, List<int> results,
            bool cone, FixedVec3 aim, long cosHalfQ30)
        {
            FixedAbilitySystem system = context.System;
            int radius = node.ResolveRadiusRaw(context.AbilityLevel);
            if (radius <= 0)
            {
                return;
            }

            _unitScratch.Clear();
            system.World.QueryUnitsInRadius(center, radius, _unitScratch);
            FixedUnit caster = system.GetUnit(context.Caster);
            int maxTargets = node.ResolveMaxTargets(context.AbilityLevel);
            if (maxTargets <= 0)
            {
                for (int i = 0; i < _unitScratch.Count; i++)
                {
                    FixedUnit unit = system.GetUnit(_unitScratch[i]);
                    if (Admits(system, unit, caster, node.TeamFilter, cone, center, aim, cosHalfQ30))
                    {
                        results.Add(unit.Id);
                    }
                }

                _unitScratch.Clear();
                return;
            }

            _areaScratch.Clear();
            for (int i = 0; i < _unitScratch.Count; i++)
            {
                FixedUnit unit = system.GetUnit(_unitScratch[i]);
                if (!Admits(system, unit, caster, node.TeamFilter, cone, center, aim, cosHalfQ30))
                {
                    continue;
                }

                ulong distance = system.World.TryGetPosition(unit.Id, out FixedVec3 position)
                    ? FixedVec3.SquaredDistance(position, center)
                    : ulong.MaxValue;
                _areaScratch.Add(new AreaHit(unit.Id, distance));
            }

            _areaScratch.Sort(AreaHitOrder);
            int keep = maxTargets < _areaScratch.Count ? maxTargets : _areaScratch.Count;
            for (int i = 0; i < keep; i++)
            {
                results.Add(_areaScratch[i].Unit);
            }

            _areaScratch.Clear();
            _unitScratch.Clear();
        }

        private static bool Admits(FixedAbilitySystem system, FixedUnit unit, FixedUnit caster,
            FixedTeamFilter filter, bool cone, FixedVec3 origin, FixedVec3 aim, long cosHalfQ30)
        {
            if (unit == null || !unit.IsAlive)
            {
                return false;
            }

            if (caster != null && !FixedAbilitySystem.MatchesTeamFilter(caster, unit, filter))
            {
                return false;
            }

            if (!cone)
            {
                return true;
            }

            return system.World.TryGetPosition(unit.Id, out FixedVec3 position) &&
                   FixedCone.Contains(position - origin, aim, cosHalfQ30);
        }

        private static int CompareAreaHits(AreaHit a, AreaHit b)
        {
            int byDistance = a.DistanceSquared.CompareTo(b.DistanceSquared);
            return byDistance != 0 ? byDistance : a.Unit.CompareTo(b.Unit);
        }

        private readonly struct AreaHit
        {
            public readonly int Unit;
            public readonly ulong DistanceSquared;

            public AreaHit(int unit, ulong distanceSquared)
            {
                Unit = unit;
                DistanceSquared = distanceSquared;
            }
        }
    }
}
