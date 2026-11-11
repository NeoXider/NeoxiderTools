using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     The damage pipeline of one <see cref="AbilitySystem" />: guards (dead, no health pool, invulnerable, magic
    ///     immune) → before_deal_damage on the attacker → before_take_damage on the victim (hooks may scale, add,
    ///     absorb, reflect or cancel) → <see cref="AbilityRules.DamageFormula" /> (evasion, outgoing multiplier, crit,
    ///     mitigation, incoming multiplier) → hook absorb → shield modifiers → health → death → receipts
    ///     (take_damage, deal_damage, attack_received, attack_landed, critical_hit, kill) → lifesteal → reflection.
    ///     Every path in the domain applies damage through here.
    /// </summary>
    /// <remarks>
    ///     Hook order is deterministic: event-id handlers in subscription order, then modifier reactions by priority
    ///     (higher first), modifier application order and list order. Nested applications (reactions dealing damage,
    ///     reflection) are capped at <see cref="MaxDepth" />; reflected damage is never reflected again. Allocation-free
    ///     once the per-level pools are warm.
    /// </remarks>
    public sealed class DamagePipeline
    {
        /// <summary>Maximum nesting of damage applications (reflection, reactions that deal damage).</summary>
        public const int MaxDepth = 8;

        private readonly AbilitySystem _system;
        private readonly List<DamageContext> _contexts = new List<DamageContext>(4);

        // WHY: shield receipts run modifier reactions synchronously, which can re-enter Apply → ConsumeShields;
        // scratch lists are pooled per nesting level so the outer iteration survives.
        private readonly List<List<ModifierInstance>> _shieldScratch = new List<List<ModifierInstance>>(4);

        private int _depth;
        private int _shieldDepth;

        internal DamagePipeline(AbilitySystem system)
        {
            _system = system;
        }

        /// <summary>
        ///     The innermost damage application currently running its before hooks, or null. Valid only inside a
        ///     before_deal_damage / before_take_damage handler; never keep it.
        /// </summary>
        public DamageContext Active
        {
            get
            {
                if (_depth == 0)
                {
                    return null;
                }

                DamageContext context = _contexts[_depth - 1];
                return context.CanModify ? context : null;
            }
        }

        /// <summary>Current nesting depth of damage applications (0 when none runs).</summary>
        public int Depth => _depth;

        /// <summary>Applies damage through the full pipeline. See the class summary for the order of steps.</summary>
        public DamageResult Apply(UnitId source, UnitId target, float amount, string damageType,
            string abilityId = null, uint castId = 0, IRandomSource random = null,
            DamageFlags flags = DamageFlags.None)
        {
            if (amount <= 0f)
            {
                return DamageResult.None(amount);
            }

            AbilityUnit targetUnit = _system.GetUnit(target);
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                return DamageResult.None(amount);
            }

            if (targetUnit.MaxHealth <= 0f && targetUnit.Health <= 0f)
            {
                // WHY: utility units without a health pool are not damageable. Reading an absent
                // pool as zero must not turn an otherwise ineffective hit into a death/kill receipt.
                return DamageResult.None(amount);
            }

            if (targetUnit.HasState(AbilityStates.Invulnerable))
            {
                return DamageResult.None(amount);
            }

            if (AbilityIdComparer.Same(damageType, AbilityDamageTypes.Magical) &&
                targetUnit.HasState(AbilityStates.MagicImmune))
            {
                return DamageResult.None(amount);
            }

            if (_depth >= MaxDepth)
            {
                _system.TraceNote(AbilityTraceKind.DepthCap, target, source, amount, "damage", abilityId,
                    "damage nesting reached DamagePipeline.MaxDepth");
                return DamageResult.None(amount);
            }

            while (_contexts.Count <= _depth)
            {
                _contexts.Add(new DamageContext());
            }

            DamageContext d = _contexts[_depth];
            _depth++;
            d.Reset(_system, source, target, amount, damageType, abilityId, castId, random, flags, _depth);
            try
            {
                return Resolve(d, targetUnit);
            }
            finally
            {
                d.Clear();
                _depth--;
            }
        }

        private DamageResult Resolve(DamageContext d, AbilityUnit targetUnit)
        {
            UnitId source = d.Source;
            UnitId target = d.Target;
            float amount = d.RawAmount;
            AbilityUnit sourceUnit = _system.GetUnit(source);

            if (!d.HasFlag(DamageFlags.NoHooks) && RunHooks(d, sourceUnit, targetUnit))
            {
                if (d.IsCancelled)
                {
                    _system.TraceNote(AbilityTraceKind.DamageCancelled, target, source, amount, d.DamageType,
                        d.AbilityId, "a before hook cancelled the damage");
                    return DamageResult.Cancel(amount);
                }

                // WHY: a hook reaction may have killed the victim or converted the damage into an immune type.
                if (!targetUnit.IsAlive || targetUnit.HasState(AbilityStates.Invulnerable) ||
                    d.IsMagical && targetUnit.HasState(AbilityStates.MagicImmune))
                {
                    return DamageResult.None(amount);
                }
            }

            AbilityRules rules = _system.Rules;
            bool isPure = d.IsPure;
            float computed = rules.DamageFormula.Compute(d, sourceUnit, targetUnit, rules);
            if (d.Evaded)
            {
                _system.Events.Publish(new AbilityEventArgs(AbilityEvents.Evaded, target, source,
                    amount, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                return DamageResult.Evade(amount);
            }

            float damage = rules.Numeric.Quantize(computed);
            if (damage <= 0f)
            {
                return DamageResult.None(amount);
            }

            bool crit = d.Crit;
            float mitigated = damage;
            d.Mitigated = mitigated;

            float absorbed = 0f;
            if (d.Absorb > 0f)
            {
                float blocked = rules.Numeric.Quantize(MathF.Min(d.Absorb, damage));
                damage -= blocked;
                absorbed += blocked;
            }

            // WHY: shield absorption from modifier shield_hp contributions (per-instance mutable pools).
            if (!isPure && damage > 0f)
            {
                absorbed += ConsumeShields(targetUnit, ref damage);
            }

            float healthDamage = 0f;
            if (damage > 0f)
            {
                // WHY: The accepted operation delta stays valid when a resource callback changes health again.
                healthDamage = targetUnit.Resources.Decrease(AbilityResourceIds.Health, damage);
            }

            bool killed = targetUnit.IsAlive && targetUnit.Health <= 0f;
            if (killed)
            {
                _system.MarkDead(targetUnit, source, d.AbilityId, d.CastId);
            }

            d.Absorbed = absorbed;
            d.HealthDamage = healthDamage;
            d.Killed = killed;

            if (healthDamage > 0f || absorbed > 0f)
            {
                _system.Events.Publish(new AbilityEventArgs(AbilityEvents.TakeDamage, target, source,
                    healthDamage, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                if (source.IsValid)
                {
                    _system.Events.Publish(new AbilityEventArgs(AbilityEvents.DealDamage, source, target,
                        healthDamage, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                    if (d.HasFlag(DamageFlags.Hit))
                    {
                        if (_system.WantsEvent(AbilityEvents.AttackReceived, target))
                        {
                            _system.Events.Publish(new AbilityEventArgs(AbilityEvents.AttackReceived, target, source,
                                healthDamage, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                        }

                        if (_system.WantsEvent(AbilityEvents.AttackLanded, source))
                        {
                            _system.Events.Publish(new AbilityEventArgs(AbilityEvents.AttackLanded, source, target,
                                healthDamage, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                        }
                    }
                }

                if (crit && source.IsValid)
                {
                    _system.Events.Publish(new AbilityEventArgs(AbilityEvents.CriticalHit, source, target,
                        healthDamage, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
                }
            }

            if (killed && source.IsValid)
            {
                _system.Events.Publish(new AbilityEventArgs(AbilityEvents.Kill, source, target, healthDamage,
                    d.AbilityId, damageType: d.DamageType, castId: d.CastId));
            }

            // WHY: the attacker recovers a percentage of the health damage dealt. lifesteal_percent
            // is 0..100 (50 = half). Only applied when the attacker is a distinct, living unit.
            if (healthDamage > 0f && sourceUnit != null && sourceUnit.IsAlive && source != target &&
                !d.HasFlag(DamageFlags.NoLifesteal))
            {
                float lifestealPercent = sourceUnit.GetProperty(AbilityProperties.LifestealPercent);
                if (lifestealPercent > 0f)
                {
                    float heal = rules.Numeric.Quantize(lifestealPercent / 100f * healthDamage);
                    if (heal > 0f)
                    {
                        float healed = sourceUnit.Resources.Increase(AbilityResourceIds.Health, heal);
                        if (healed > 0f)
                        {
                            _system.Events.Publish(new AbilityEventArgs(AbilityEvents.HealReceived, source,
                                target, healed, d.AbilityId, castId: d.CastId));
                        }
                    }
                }
            }

            float reflected = 0f;
            if (d.ReflectFraction > 0f && !d.HasFlag(DamageFlags.Reflected) && !d.HasFlag(DamageFlags.NoReflect) &&
                sourceUnit != null && sourceUnit.IsAlive && source != target)
            {
                reflected = rules.Numeric.Quantize(mitigated * d.ReflectFraction);
                if (reflected > 0f)
                {
                    string reflectType = d.ReflectDamageType ?? AbilityDamageTypes.Pure;
                    _system.Events.Publish(new AbilityEventArgs(AbilityEvents.DamageReflected, target, source,
                        reflected, d.AbilityId, damageType: reflectType, castId: d.CastId));
                    Apply(target, source, reflected, reflectType, d.AbilityId, d.CastId, d.Random,
                        DamageFlags.Reflected);
                }
            }

            return new DamageResult(amount, mitigated, absorbed, healthDamage, killed, false, false, crit, false,
                reflected);
        }

        /// <summary>Publishes the before hooks that someone listens to; true when at least one ran.</summary>
        private bool RunHooks(DamageContext d, AbilityUnit sourceUnit, AbilityUnit targetUnit)
        {
            bool hooked = false;
            d.CanModify = true;
            if (d.Source.IsValid && sourceUnit != null && sourceUnit.IsAlive &&
                _system.WantsEvent(AbilityEvents.BeforeDealDamage, d.Source))
            {
                hooked = true;
                _system.Events.Publish(new AbilityEventArgs(AbilityEvents.BeforeDealDamage, d.Source, d.Target,
                    d.RawAmount, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
            }

            if (!d.IsCancelled && targetUnit.IsAlive && _system.WantsEvent(AbilityEvents.BeforeTakeDamage, d.Target))
            {
                hooked = true;
                _system.Events.Publish(new AbilityEventArgs(AbilityEvents.BeforeTakeDamage, d.Target, d.Source,
                    d.RawAmount, d.AbilityId, damageType: d.DamageType, castId: d.CastId));
            }

            d.CanModify = false;
            return hooked;
        }

        private float ConsumeShields(AbilityUnit target, ref float damage)
        {
            if (damage <= 0f)
            {
                return 0f;
            }

            while (_shieldScratch.Count <= _shieldDepth)
            {
                _shieldScratch.Add(new List<ModifierInstance>(8));
            }

            List<ModifierInstance> scratch = _shieldScratch[_shieldDepth];
            _shieldDepth++;
            try
            {
                _system.Modifiers.GetModifiers(target.Id, scratch);
                IAbilityNumeric numeric = _system.Rules.Numeric;

                float totalAbsorbed = 0f;
                for (int i = 0; i < scratch.Count && damage > 0f; i++)
                {
                    ModifierInstance m = scratch[i];
                    if (!m.IsActive || m.IsShieldBreaking)
                    {
                        // WHY: an earlier shield's receipt can synchronously dispel another shield.
                        continue;
                    }

                    float capacity = DamageService.ShieldCapacity(m);
                    if (capacity <= 0f)
                    {
                        continue;
                    }

                    float available = capacity - m.ShieldConsumed;
                    if (available <= 0f)
                    {
                        continue;
                    }

                    float take = numeric.Quantize(MathF.Min(available, damage));
                    if (take <= 0f)
                    {
                        continue;
                    }

                    m.ShieldConsumed += take;
                    damage -= take;
                    totalAbsorbed += take;

                    _system.Events.Publish(new AbilityEventArgs(AbilityEvents.ShieldAbsorbed, target.Id, m.Caster,
                        take, m.SourceAbilityId, m.Blueprint.Id));

                    if (!m.IsActive || m.IsShieldBreaking)
                    {
                        // WHY: this receipt may dispel the current instance or recursively consume
                        // and break it. Do not publish another break for an already removed shield.
                        continue;
                    }

                    // WHY: a receipt can stack the same instance and increase its remaining shield.
                    // Exhaustion must use its current capacity, not the value before the callback.
                    capacity = DamageService.ShieldCapacity(m);
                    if (m.ShieldConsumed >= capacity)
                    {
                        // WHY: preserve the active instance while its declarative break reaction runs,
                        // but keep refresh/stack application from reusing this exhausted instance.
                        m.IsShieldBreaking = true;
                        try
                        {
                            _system.Events.Publish(new AbilityEventArgs(AbilityEvents.ShieldBroken, target.Id,
                                m.Caster, capacity, m.SourceAbilityId, m.Blueprint.Id));
                        }
                        finally
                        {
                            _system.Modifiers.Remove(m);
                        }
                    }
                }

                return totalAbsorbed;
            }
            finally
            {
                // WHY: a filled scratch would keep the last victim's modifier graph alive until the next hit.
                scratch.Clear();
                _shieldDepth--;
            }
        }
    }
}
