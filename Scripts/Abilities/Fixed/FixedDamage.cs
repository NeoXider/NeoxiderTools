using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     One damage application in flight (pooled per nesting level). While the before_deal_damage /
    ///     before_take_damage hooks run, <see cref="CanModify" /> is true and reactions adjust the raw multiplier,
    ///     flat add, absorb and reflect fraction, or cancel it. Never keep a reference after the hook.
    /// </summary>
    public sealed class FixedDamageContext
    {
        internal FixedDamageContext()
        {
        }

        public FixedAbilitySystem System { get; private set; }
        public int Source { get; private set; }
        public int Target { get; private set; }
        public string AbilityId { get; private set; }
        public uint CastId { get; private set; }
        public FixedDamageFlags Flags { get; private set; }
        public int RawAmount { get; private set; }
        public IFixedRandomSource Random { get; private set; }
        public int Depth { get; private set; }
        public string DamageType { get; set; }

        /// <summary>Raw multiplier, one = ×1.</summary>
        public int MultiplierRaw { get; set; }

        public int FlatAddRaw { get; set; }
        public int AbsorbRaw { get; set; }

        /// <summary>Raw fraction of the mitigated damage sent back to the source.</summary>
        public int ReflectFractionRaw { get; set; }

        public string ReflectDamageType { get; set; }
        public bool IsCancelled { get; private set; }
        public bool CanModify { get; internal set; }
        public bool Evaded { get; set; }
        public bool Crit { get; set; }
        public int CritMultiplierRaw { get; set; }
        public int MitigatedRaw { get; internal set; }
        public int AbsorbedRaw { get; internal set; }
        public int HealthDamageRaw { get; internal set; }
        public bool Killed { get; internal set; }

        public bool IsPhysical => FixedIdComparer.Same(DamageType, FixedDamageTypes.Physical);
        public bool IsMagical => FixedIdComparer.Same(DamageType, FixedDamageTypes.Magical);
        public bool IsPure => FixedIdComparer.Same(DamageType, FixedDamageTypes.Pure);

        public bool HasFlag(FixedDamageFlags flag)
        {
            return (Flags & flag) != 0;
        }

        /// <summary>Cancels the damage (only while the hooks run).</summary>
        public void Cancel()
        {
            if (CanModify)
            {
                IsCancelled = true;
            }
        }

        internal void Reset(FixedAbilitySystem system, int source, int target, int amountRaw, string damageType,
            string abilityId, uint castId, IFixedRandomSource random, FixedDamageFlags flags, int depth)
        {
            System = system;
            Source = source;
            Target = target;
            RawAmount = amountRaw;
            DamageType = damageType;
            AbilityId = abilityId;
            CastId = castId;
            Random = random;
            Flags = flags;
            Depth = depth;
            MultiplierRaw = system.Rules.Format.One;
            FlatAddRaw = 0;
            AbsorbRaw = 0;
            ReflectFractionRaw = 0;
            ReflectDamageType = FixedDamageTypes.Pure;
            IsCancelled = false;
            CanModify = false;
            Evaded = false;
            Crit = false;
            CritMultiplierRaw = system.Rules.Format.One;
            MitigatedRaw = 0;
            AbsorbedRaw = 0;
            HealthDamageRaw = 0;
            Killed = false;
        }
    }

    /// <summary>Outcome of one fixed damage application (raw amounts).</summary>
    public readonly struct FixedDamageResult
    {
        public readonly int RawAmount;
        public readonly int MitigatedRaw;
        public readonly int AbsorbedRaw;
        public readonly int HealthDamageRaw;
        public readonly bool Killed;
        public readonly bool Negated;
        public readonly bool Evaded;
        public readonly bool Crit;
        public readonly bool Cancelled;
        public readonly int ReflectedRaw;

        public FixedDamageResult(int rawAmount, int mitigatedRaw, int absorbedRaw, int healthDamageRaw, bool killed,
            bool negated, bool evaded, bool crit, bool cancelled, int reflectedRaw)
        {
            RawAmount = rawAmount;
            MitigatedRaw = mitigatedRaw;
            AbsorbedRaw = absorbedRaw;
            HealthDamageRaw = healthDamageRaw;
            Killed = killed;
            Negated = negated;
            Evaded = evaded;
            Crit = crit;
            Cancelled = cancelled;
            ReflectedRaw = reflectedRaw;
        }

        public static FixedDamageResult None(int raw)
        {
            return new FixedDamageResult(raw, 0, 0, 0, false, true, false, false, false, 0);
        }

        public static FixedDamageResult Cancel(int raw)
        {
            return new FixedDamageResult(raw, 0, 0, 0, false, true, false, false, true, 0);
        }

        public static FixedDamageResult Evade(int raw)
        {
            return new FixedDamageResult(raw, 0, 0, 0, false, true, true, false, false, 0);
        }
    }

    /// <summary>
    ///     Damage pipeline of a fixed system, the float DamagePipeline on raw values: immunity checks, before hooks,
    ///     the formula (evasion, crit, mitigation), hook absorb, shield_hp shields, health, death, receipts
    ///     (take / deal damage, attack landed / received, crit, kill), lifesteal and reflection. Depth-capped and
    ///     allocation-free.
    /// </summary>
    public sealed class FixedDamagePipeline
    {
        public const int MaxDepth = 8;

        private readonly FixedAbilitySystem _system;
        private readonly List<FixedDamageContext> _contexts = new List<FixedDamageContext>(4);
        private readonly List<List<FixedModifierInstance>> _shieldScratch = new List<List<FixedModifierInstance>>(4);
        private int _depth;
        private int _shieldDepth;

        internal FixedDamagePipeline(FixedAbilitySystem system)
        {
            _system = system;
        }

        /// <summary>The damage whose before hooks are running, else null.</summary>
        public FixedDamageContext Active
        {
            get
            {
                if (_depth == 0)
                {
                    return null;
                }

                FixedDamageContext context = _contexts[_depth - 1];
                return context.CanModify ? context : null;
            }
        }

        public int Depth => _depth;

        public FixedDamageResult Apply(int source, int target, int amountRaw, string damageType, string abilityId = null,
            uint castId = 0u, IFixedRandomSource random = null, FixedDamageFlags flags = FixedDamageFlags.None)
        {
            if (amountRaw <= 0)
            {
                return FixedDamageResult.None(amountRaw);
            }

            FixedUnit targetUnit = _system.GetUnit(target);
            if (targetUnit == null || !targetUnit.IsAlive || targetUnit.MaxHealthRaw <= 0 && targetUnit.HealthRaw <= 0 ||
                targetUnit.HasState(FixedStates.Invulnerable) ||
                FixedIdComparer.Same(damageType, FixedDamageTypes.Magical) && targetUnit.HasState(FixedStates.MagicImmune) ||
                _depth >= MaxDepth)
            {
                return FixedDamageResult.None(amountRaw);
            }

            while (_contexts.Count <= _depth)
            {
                _contexts.Add(new FixedDamageContext());
            }

            FixedDamageContext d = _contexts[_depth];
            _depth++;
            d.Reset(_system, source, target, amountRaw, damageType, abilityId, castId, random, flags, _depth);
            try
            {
                return Resolve(d, targetUnit);
            }
            finally
            {
                d.CanModify = false;
                _depth--;
            }
        }

        /// <summary>Shield HP an instance grants (shield_hp contributions for its stacks).</summary>
        public static int ShieldCapacity(FixedModifierInstance instance)
        {
            if (instance == null || instance.Blueprint.Properties == null)
            {
                return 0;
            }

            List<FixedPropertyContribution> properties = instance.Blueprint.Properties;
            long capacity = 0L;
            for (int i = 0; i < properties.Count; i++)
            {
                FixedPropertyContribution c = properties[i];
                if (FixedIdComparer.Same(c.PropertyId, FixedProperties.ShieldHp))
                {
                    capacity += c.ValueForStacks(instance.Stacks);
                }
            }

            return FixedMath.ClampToInt(capacity);
        }

        /// <summary>Shield HP an instance can still absorb.</summary>
        public static int RemainingShield(FixedModifierInstance instance)
        {
            if (instance == null || !instance.IsActive)
            {
                return 0;
            }

            int remaining = ShieldCapacity(instance) - instance.ShieldConsumedRaw;
            return remaining > 0 ? remaining : 0;
        }

        private FixedDamageResult Resolve(FixedDamageContext d, FixedUnit targetUnit)
        {
            int source = d.Source;
            int target = d.Target;
            int amount = d.RawAmount;
            FixedUnit sourceUnit = _system.GetUnit(source);
            if (!d.HasFlag(FixedDamageFlags.NoHooks) && RunHooks(d, sourceUnit, targetUnit))
            {
                if (d.IsCancelled)
                {
                    return FixedDamageResult.Cancel(amount);
                }

                if (!targetUnit.IsAlive || targetUnit.HasState(FixedStates.Invulnerable) ||
                    d.IsMagical && targetUnit.HasState(FixedStates.MagicImmune))
                {
                    return FixedDamageResult.None(amount);
                }
            }

            FixedRules rules = _system.Rules;
            int one = rules.Format.One;
            bool isPure = d.IsPure;
            int damage = rules.DamageFormula.Compute(d, sourceUnit, targetUnit, rules);
            if (d.Evaded)
            {
                _system.Events.Publish(new FixedEventArgs(FixedEvents.Evaded, target, source, amount, d.AbilityId,
                    damageType: d.DamageType, castId: d.CastId));
                return FixedDamageResult.Evade(amount);
            }

            if (damage <= 0)
            {
                return FixedDamageResult.None(amount);
            }

            bool crit = d.Crit;
            int mitigated = damage;
            d.MitigatedRaw = mitigated;
            int absorbed = 0;
            if (d.AbsorbRaw > 0)
            {
                int blocked = d.AbsorbRaw < damage ? d.AbsorbRaw : damage;
                damage -= blocked;
                absorbed += blocked;
            }

            if (!isPure && damage > 0)
            {
                absorbed += ConsumeShields(targetUnit, ref damage);
            }

            int healthDamage = damage > 0 ? targetUnit.Decrease(FixedResources.Health, damage) : 0;
            bool killed = targetUnit.IsAlive && targetUnit.HealthRaw <= 0;
            if (killed)
            {
                _system.MarkDead(targetUnit, source, d.AbilityId, d.CastId);
            }

            d.AbsorbedRaw = absorbed;
            d.HealthDamageRaw = healthDamage;
            d.Killed = killed;
            if (healthDamage > 0 || absorbed > 0)
            {
                _system.Events.Publish(new FixedEventArgs(FixedEvents.TakeDamage, target, source, healthDamage,
                    d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                if (source != 0)
                {
                    _system.Events.Publish(new FixedEventArgs(FixedEvents.DealDamage, source, target, healthDamage,
                        d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                    if (d.HasFlag(FixedDamageFlags.Hit))
                    {
                        if (_system.WantsEvent(FixedEvents.AttackReceived, target))
                        {
                            _system.Events.Publish(new FixedEventArgs(FixedEvents.AttackReceived, target, source,
                                healthDamage, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                        }

                        if (_system.WantsEvent(FixedEvents.AttackLanded, source))
                        {
                            _system.Events.Publish(new FixedEventArgs(FixedEvents.AttackLanded, source, target,
                                healthDamage, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                        }
                    }
                }

                if (crit && source != 0)
                {
                    _system.Events.Publish(new FixedEventArgs(FixedEvents.CriticalHit, source, target, healthDamage,
                        d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                }
            }

            if (killed && source != 0)
            {
                _system.Events.Publish(new FixedEventArgs(FixedEvents.Kill, source, target, healthDamage, d.AbilityId,
                    damageType: d.DamageType, castId: d.CastId));
            }

            if (healthDamage > 0 && sourceUnit != null && sourceUnit.IsAlive && source != target &&
                !d.HasFlag(FixedDamageFlags.NoLifesteal))
            {
                int lifesteal = sourceUnit.GetProperty(FixedProperties.LifestealPercent);
                if (lifesteal > 0)
                {
                    int heal = FixedMath.MulDivToInt(healthDamage, lifesteal, 100L * one);
                    int healed = heal > 0 ? sourceUnit.Increase(FixedResources.Health, heal) : 0;
                    if (healed > 0)
                    {
                        _system.Events.Publish(new FixedEventArgs(FixedEvents.HealReceived, source, target, healed,
                            d.AbilityId, castId: d.CastId));
                    }
                }
            }

            int reflected = 0;
            if (d.ReflectFractionRaw > 0 && !d.HasFlag(FixedDamageFlags.Reflected) &&
                !d.HasFlag(FixedDamageFlags.NoReflect) && sourceUnit != null && sourceUnit.IsAlive && source != target)
            {
                reflected = FixedMath.MulDivToInt(mitigated, d.ReflectFractionRaw, one);
                if (reflected > 0)
                {
                    string reflectType = d.ReflectDamageType ?? FixedDamageTypes.Pure;
                    _system.Events.Publish(new FixedEventArgs(FixedEvents.DamageReflected, target, source, reflected,
                        d.AbilityId, damageType: reflectType, castId: d.CastId));
                    Apply(target, source, reflected, reflectType, d.AbilityId, d.CastId, d.Random,
                        FixedDamageFlags.Reflected);
                }
            }

            return new FixedDamageResult(amount, mitigated, absorbed, healthDamage, killed, false, false, crit, false,
                reflected);
        }

        private bool RunHooks(FixedDamageContext d, FixedUnit sourceUnit, FixedUnit targetUnit)
        {
            bool hooked = false;
            d.CanModify = true;
            if (d.Source != 0 && sourceUnit != null && sourceUnit.IsAlive &&
                _system.WantsEvent(FixedEvents.BeforeDealDamage, d.Source))
            {
                hooked = true;
                _system.Events.Publish(new FixedEventArgs(FixedEvents.BeforeDealDamage, d.Source, d.Target, d.RawAmount,
                    d.AbilityId, damageType: d.DamageType, castId: d.CastId));
            }

            if (!d.IsCancelled && targetUnit.IsAlive && _system.WantsEvent(FixedEvents.BeforeTakeDamage, d.Target))
            {
                hooked = true;
                _system.Events.Publish(new FixedEventArgs(FixedEvents.BeforeTakeDamage, d.Target, d.Source, d.RawAmount,
                    d.AbilityId, damageType: d.DamageType, castId: d.CastId));
            }

            d.CanModify = false;
            return hooked;
        }

        private int ConsumeShields(FixedUnit target, ref int damage)
        {
            while (_shieldScratch.Count <= _shieldDepth)
            {
                _shieldScratch.Add(new List<FixedModifierInstance>(8));
            }

            List<FixedModifierInstance> scratch = _shieldScratch[_shieldDepth];
            _shieldDepth++;
            try
            {
                _system.Modifiers.GetModifiers(target.Id, scratch);
                int total = 0;
                for (int i = 0; i < scratch.Count && damage > 0; i++)
                {
                    FixedModifierInstance m = scratch[i];
                    if (!m.IsActive || m.IsShieldBreaking)
                    {
                        continue;
                    }

                    int capacity = ShieldCapacity(m);
                    int available = capacity - m.ShieldConsumedRaw;
                    if (capacity <= 0 || available <= 0)
                    {
                        continue;
                    }

                    int take = available < damage ? available : damage;
                    m.ShieldConsumedRaw += take;
                    damage -= take;
                    total += take;
                    _system.Events.Publish(new FixedEventArgs(FixedEvents.ShieldAbsorbed, target.Id, m.Caster, take,
                        m.SourceAbilityId, m.Blueprint.Id));
                    if (!m.IsActive || m.IsShieldBreaking)
                    {
                        continue;
                    }

                    capacity = ShieldCapacity(m);
                    if (m.ShieldConsumedRaw >= capacity)
                    {
                        m.IsShieldBreaking = true;
                        try
                        {
                            _system.Events.Publish(new FixedEventArgs(FixedEvents.ShieldBroken, target.Id, m.Caster,
                                capacity, m.SourceAbilityId, m.Blueprint.Id));
                        }
                        finally
                        {
                            _system.Modifiers.Remove(m);
                        }
                    }
                }

                return total;
            }
            finally
            {
                scratch.Clear();
                _shieldDepth--;
            }
        }
    }
}
