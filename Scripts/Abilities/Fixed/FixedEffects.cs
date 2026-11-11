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
        }

        internal void Clear()
        {
            PrimaryTargets.Clear();
            Specials = null;
            Damage = null;
            AbilityId = null;
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

        public static int Resolve(in FixedLeveledValue value, FixedEffectContext context, int target)
        {
            int level = LevelFor(value.LevelFrom, context, target);
            long result = value.LevelsRaw != null && value.LevelsRaw.Length > 0
                ? SampleByLevel(value.LevelsRaw, level, value.BaseRaw)
                : value.BaseRaw + (long)value.PerLevelRaw * (level - 1);
            if (!string.IsNullOrEmpty(value.ScaleProperty) && value.ScalePerPointRaw != 0)
            {
                int scaleUnit = value.ScaleFrom == FixedScaleSource.Target ? target : context.Caster;
                FixedUnit unit = context.System.GetUnit(scaleUnit);
                if (unit != null)
                {
                    result += FixedMath.MulDiv(value.ScalePerPointRaw, unit.GetProperty(value.ScaleProperty),
                        context.System.Rules.Format.One);
                }
            }

            return FixedMath.ClampToInt(result);
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

        private static int LevelFor(FixedLevelSource source, FixedEffectContext context, int target)
        {
            switch (source)
            {
                case FixedLevelSource.AbilityLevel:
                    return context.AbilityLevel;
                case FixedLevelSource.CasterUnitLevel:
                    return LevelOf(context, context.Caster);
                case FixedLevelSource.TargetUnitLevel:
                    return LevelOf(context, target);
                default:
                    return 1;
            }
        }

        private static int LevelOf(FixedEffectContext context, int unitId)
        {
            FixedUnit unit = context.System.GetUnit(unitId);
            return unit != null ? unit.Level : 1;
        }
    }
}
