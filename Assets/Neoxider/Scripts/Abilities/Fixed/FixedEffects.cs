using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     Execution context of fixed effects: caster, ability, level, primary targets, target point, origin, the
    ///     seeded random source and the damage in flight (for hook reactions). Contexts are pooled by the system;
    ///     never keep one after <see cref="IFixedEffectOperation.Execute" /> returns.
    /// </summary>
    public sealed class FixedEffectContext
    {
        /// <summary>Maximum nesting of effect executions (reactions inside reactions...), as in the float core.</summary>
        public const int MaxDepth = 3;

        private IFixedRandomSource _ownedRandom;
        private IFixedRandomFactory _ownedFactory;

        // WHY: what the cast paid drives spent-scaled amounts; pooled, reused, allocated on first use.
        private List<FixedSpentResource> _spent;

        internal FixedEffectContext()
        {
        }

        public FixedAbilitySystem System { get; private set; }
        public int Caster { get; internal set; }
        public string AbilityId { get; internal set; }
        public uint CastId { get; set; }
        public int AbilityLevel { get; set; } = 1;
        public List<FixedAbilitySpecial> Specials { get; set; }
        public FixedEffectOrigin Origin { get; set; }
        public int SourceStacks { get; set; } = 1;
        public int Depth { get; set; }
        public List<int> PrimaryTargets { get; } = new List<int>(4);
        public FixedVec3 TargetPoint { get; set; }
        public bool HasTargetPoint { get; set; }

        /// <summary>The damage whose hooks triggered this execution, else null.</summary>
        public FixedDamageContext Damage { get; set; }

        public IFixedRandomSource Random { get; private set; }

        public bool IsDirectHit => Origin == FixedEffectOrigin.Cast || Origin == FixedEffectOrigin.ProjectileHit;
        public FixedDamageFlags HitFlags => IsDirectHit ? FixedDamageFlags.Hit : FixedDamageFlags.None;

        /// <summary>Number of raw amounts the cast of this context paid (see the float EffectContext.SpentCount).</summary>
        public int SpentCount => _spent != null ? _spent.Count : 0;

        public FixedSpentResource GetSpentAt(int index)
        {
            return _spent[index];
        }

        /// <summary>Total raw amount of <paramref name="resourceId" /> the cast paid; 0 when none.</summary>
        public int GetSpent(string resourceId)
        {
            if (_spent == null || string.IsNullOrEmpty(resourceId))
            {
                return 0;
            }

            long total = 0L;
            for (int i = 0; i < _spent.Count; i++)
            {
                if (FixedIdComparer.Same(_spent[i].ResourceId, resourceId))
                {
                    total += _spent[i].AmountRaw;
                }
            }

            return FixedMath.ClampToInt(total);
        }

        /// <summary>Records a paid raw amount; non-positive amounts and empty ids are ignored.</summary>
        public void AddSpent(string resourceId, int amountRaw)
        {
            if (string.IsNullOrEmpty(resourceId) || amountRaw <= 0)
            {
                return;
            }

            _spent ??= new List<FixedSpentResource>(2);
            _spent.Add(new FixedSpentResource(resourceId, amountRaw));
        }

        internal void CopySpentFrom(FixedEffectContext source)
        {
            int count = source != null ? source.SpentCount : 0;
            for (int i = 0; i < count; i++)
            {
                FixedSpentResource paid = source._spent[i];
                AddSpent(paid.ResourceId, paid.AmountRaw);
            }
        }

        public bool TryGetSpecial(string name, out FixedLeveledValue value)
        {
            List<FixedAbilitySpecial> specials = Specials;
            if (specials != null && !string.IsNullOrEmpty(name))
            {
                for (int i = 0; i < specials.Count; i++)
                {
                    if (FixedIdComparer.Same(specials[i].Name, name))
                    {
                        value = specials[i].Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        internal void Reset(FixedAbilitySystem system, int caster, string abilityId, uint seed)
        {
            IFixedRandomFactory factory = system.Rules.Random;
            if (_ownedRandom == null || !ReferenceEquals(_ownedFactory, factory))
            {
                _ownedRandom = factory.Create(seed) ?? new FixedXorShiftRandom(seed);
                _ownedFactory = factory;
            }
            else
            {
                _ownedRandom.Reseed(seed);
            }

            System = system;
            Random = _ownedRandom;
            Caster = caster;
            AbilityId = abilityId;
            CastId = 0u;
            AbilityLevel = 1;
            Specials = null;
            Origin = FixedEffectOrigin.Direct;
            SourceStacks = 1;
            Depth = 0;
            PrimaryTargets.Clear();
            TargetPoint = FixedVec3.Zero;
            HasTargetPoint = false;
            Damage = null;
            _spent?.Clear();
        }

        internal void Clear()
        {
            PrimaryTargets.Clear();
            Specials = null;
            Damage = null;
            AbilityId = null;
            _spent?.Clear();
        }
    }

    /// <summary>An effect operation of the fixed core; must not allocate per call or keep the context.</summary>
    public interface IFixedEffectOperation
    {
        string Id { get; }

        void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets);
    }

    /// <summary>Open registry of fixed effect operations by id (case-insensitive).</summary>
    public sealed class FixedEffectOpRegistry
    {
        private readonly Dictionary<string, IFixedEffectOperation> _ops =
            new Dictionary<string, IFixedEffectOperation>(FixedIdComparer.Instance);

        public int Count => _ops.Count;

        public void Register(IFixedEffectOperation op)
        {
            if (op != null && !string.IsNullOrEmpty(op.Id))
            {
                _ops[op.Id] = op;
            }
        }

        public bool Unregister(string id)
        {
            return !string.IsNullOrEmpty(id) && _ops.Remove(id);
        }

        public bool TryGet(string id, out IFixedEffectOperation op)
        {
            op = null;
            return !string.IsNullOrEmpty(id) && _ops.TryGetValue(id, out op);
        }

        public bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && _ops.ContainsKey(id);
        }
    }

    /// <summary>Resolves leveled raw amounts of effect nodes (the float LeveledValueResolver on raw values).</summary>
    public static class FixedValueResolver
    {
        public static int SampleByLevel(int[] byLevel, int level, int fallback)
        {
            if (byLevel == null || byLevel.Length == 0)
            {
                return fallback;
            }

            int index = level - 1;
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= byLevel.Length)
            {
                index = byLevel.Length - 1;
            }

            return byLevel[index];
        }

        /// <summary>
        ///     Resolves a leveled raw value in a context: level → base, + spent term (what the context's cast paid), then
        ///     the scaling term added or multiplied (the float LeveledValueResolver on raw values).
        /// </summary>
        public static int Resolve(in FixedLeveledValue value, FixedEffectContext context, int target)
        {
            return ResolveCore(in value, context.System, context.Caster, context.AbilityLevel, target, context);
        }

        /// <summary>Resolves a leveled raw value outside an effect execution (cast costs): no spent term.</summary>
        public static int Resolve(in FixedLeveledValue value, FixedAbilitySystem system, int caster, int abilityLevel,
            int target)
        {
            return ResolveCore(in value, system, caster, abilityLevel, target, null);
        }

        /// <summary>True when one of the nodes scales with a spent resource, inline or through a special of <paramref name="specials" />.</summary>
        public static bool UsesSpent(List<FixedEffectNode> nodes, List<FixedAbilitySpecial> specials)
        {
            if (nodes == null)
            {
                return false;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                FixedEffectNode node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(node.AmountSpentResource))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(node.AmountKey) && specials != null)
                {
                    for (int s = 0; s < specials.Count; s++)
                    {
                        if (FixedIdComparer.Same(specials[s].Name, node.AmountKey))
                        {
                            if (!string.IsNullOrEmpty(specials[s].Value.SpentResource))
                            {
                                return true;
                            }

                            break;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>True when the tick effects or a reaction of <paramref name="modifier" /> scale with a spent resource.</summary>
        public static bool UsesSpent(FixedModifierBlueprint modifier, List<FixedAbilitySpecial> specials)
        {
            if (modifier == null)
            {
                return false;
            }

            if (UsesSpent(modifier.TickEffects, specials))
            {
                return true;
            }

            List<FixedModifierReaction> reactions = modifier.EventReactions;
            if (reactions == null)
            {
                return false;
            }

            for (int i = 0; i < reactions.Count; i++)
            {
                if (reactions[i] != null && UsesSpent(reactions[i].Effects, specials))
                {
                    return true;
                }
            }

            return false;
        }

        private static int ResolveCore(in FixedLeveledValue value, FixedAbilitySystem system, int caster,
            int abilityLevel, int target, FixedEffectContext spentSource)
        {
            int level = LevelFor(value.LevelFrom, system, caster, abilityLevel, target);
            long result = value.LevelsRaw != null && value.LevelsRaw.Length > 0
                ? SampleByLevel(value.LevelsRaw, level, value.BaseRaw)
                : value.BaseRaw + (long)value.PerLevelRaw * (level - 1);
            if (system == null)
            {
                return FixedMath.ClampToInt(result);
            }

            int one = system.Rules.Format.One;
            if (spentSource != null && !string.IsNullOrEmpty(value.SpentResource) && value.SpentCoefficientRaw != 0)
            {
                result += FixedMath.MulDiv(value.SpentCoefficientRaw, spentSource.GetSpent(value.SpentResource), one);
            }

            if (!string.IsNullOrEmpty(value.ScaleProperty) && value.ScalePerPointRaw != 0)
            {
                int scaleUnit = value.ScaleFrom == FixedScaleSource.Target ? target : caster;
                FixedUnit unit = system.GetUnit(scaleUnit);
                if (unit != null)
                {
                    long term = FixedMath.MulDiv(value.ScalePerPointRaw, ReadStat(unit, value.ScaleStat, value.ScaleProperty),
                        one);
                    result = value.ScaleMode == FixedScaleMode.Multiply
                        ? FixedMath.SaturatingAdd(result, FixedMath.MulDiv(result, term, one))
                        : result + term;
                }
            }

            return FixedMath.ClampToInt(result);
        }

        private static int ReadStat(FixedUnit unit, FixedScaleStat stat, string id)
        {
            switch (stat)
            {
                case FixedScaleStat.ResourceCurrent:
                    return unit.GetCurrent(id);
                case FixedScaleStat.ResourceMax:
                    return unit.GetMax(id);
                case FixedScaleStat.ResourceMissing:
                    return FixedMath.ClampToInt(FixedMath.Max(0L, (long)unit.GetMax(id) - unit.GetCurrent(id)));
                default:
                    return unit.GetProperty(id);
            }
        }

        /// <summary>The node's amount: its special (AmountKey) or inline leveled value, plus AmountPerStack per extra stack.</summary>
        public static int ResolveAmount(FixedEffectNode node, FixedEffectContext context, int target)
        {
            int amount;
            if (!string.IsNullOrEmpty(node.AmountKey) && context.TryGetSpecial(node.AmountKey, out FixedLeveledValue special))
            {
                amount = Resolve(in special, context, target);
            }
            else
            {
                FixedLeveledValue inline = node.ToLeveledValue();
                amount = Resolve(in inline, context, target);
            }

            if (node.AmountPerStackRaw != 0 && context.SourceStacks > 1)
            {
                amount = FixedMath.ClampToInt(amount + (long)node.AmountPerStackRaw * (context.SourceStacks - 1));
            }

            return amount;
        }

        private static int LevelFor(FixedLevelSource source, FixedAbilitySystem system, int caster, int abilityLevel,
            int target)
        {
            switch (source)
            {
                case FixedLevelSource.AbilityLevel:
                    return abilityLevel;
                case FixedLevelSource.CasterUnitLevel:
                    return LevelOf(system, caster);
                case FixedLevelSource.TargetUnitLevel:
                    return LevelOf(system, target);
                default:
                    return 1;
            }
        }

        private static int LevelOf(FixedAbilitySystem system, int unitId)
        {
            FixedUnit unit = system != null ? system.GetUnit(unitId) : null;
            return unit != null ? unit.Level : 1;
        }
    }
}
