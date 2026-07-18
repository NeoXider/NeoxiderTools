using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     The damage pipeline: outgoing multipliers → armor/resist by damage type → incoming multipliers
    ///     → shield absorption (modifier shield_hp pools) → health pool → death/kill events.
    ///     Every path in the system (effect ops, basic attacks, projectiles, contact damage) applies
    ///     damage through here so events and receipts stay consistent.
    /// </summary>
    public static class DamageService
    {
        // WHY: shield events run modifier reactions synchronously, which can re-enter ApplyDamage →
        // ConsumeShields; scratch lists are pooled per nesting level so the outer iteration survives.
        private static readonly List<List<ModifierInstance>> ModifierScratchPool =
            new List<List<ModifierInstance>>(4);

        private static int _shieldDepth;

        public static DamageResult ApplyDamage(AbilitySystem system, UnitId source, UnitId target,
            float amount, string damageType, string abilityId = null, uint castId = 0,
            IRandomSource random = null)
        {
            if (system == null || amount <= 0f)
            {
                return DamageResult.None(amount);
            }

            AbilityUnit targetUnit = system.GetUnit(target);
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                return DamageResult.None(amount);
            }

            if (targetUnit.HasState(AbilityStates.Invulnerable))
            {
                return DamageResult.None(amount);
            }

            bool isMagical = string.Equals(damageType, AbilityDamageTypes.Magical, StringComparison.OrdinalIgnoreCase);
            bool isPhysical = string.Equals(damageType, AbilityDamageTypes.Physical, StringComparison.OrdinalIgnoreCase);
            bool isPure = string.Equals(damageType, AbilityDamageTypes.Pure, StringComparison.OrdinalIgnoreCase);

            if (isMagical && targetUnit.HasState(AbilityStates.MagicImmune))
            {
                return DamageResult.None(amount);
            }

            // WHY: evasion is rolled pre-mitigation on the victim, physical only (Dota-style), and only when
            // an RNG is supplied — direct/legacy callers pass none and never evade (keeps existing math stable).
            // evasion_chance is a fraction in [0..1].
            if (isPhysical && random != null)
            {
                float evasion = targetUnit.GetProperty(AbilityProperties.EvasionChance);
                if (evasion > 0f && random.NextFloat() < evasion)
                {
                    system.Events.Publish(new AbilityEventArgs(AbilityEvents.Evaded, target, source,
                        amount, abilityId, damageType: damageType, castId: castId));
                    return DamageResult.Evade(amount);
                }
            }

            AbilityUnit sourceUnit = system.GetUnit(source);

            float damage = amount;
            if (sourceUnit != null)
            {
                damage *= MathF.Max(0f, sourceUnit.GetProperty(AbilityProperties.OutgoingDamageMul, 1f));
            }

            // WHY: one crit roll per application, pre-mitigation, on the attacker. crit_chance is a
            // fraction in [0..1]; crit_multiplier is the full multiplier (2 = double), floored at 1.
            bool crit = false;
            if (sourceUnit != null && random != null)
            {
                float critChance = sourceUnit.GetProperty(AbilityProperties.CritChance);
                if (critChance > 0f && random.NextFloat() < critChance)
                {
                    crit = true;
                    damage *= MathF.Max(1f, sourceUnit.GetProperty(AbilityProperties.CritMultiplier));
                }
            }

            if (isPhysical)
            {
                float armor = targetUnit.GetProperty(AbilityProperties.Armor);
                float reduction = 0.06f * armor / (1f + 0.06f * MathF.Abs(armor));
                damage *= 1f - reduction;
            }
            else if (isMagical)
            {
                float resist = targetUnit.GetProperty(AbilityProperties.MagicResistPercent);
                damage *= 1f - Math.Clamp(resist, -100f, 100f) / 100f;
            }
            // WHY: pure and custom types intentionally skip built-in mitigation above.
            damage *= MathF.Max(0f, targetUnit.GetProperty(AbilityProperties.IncomingDamageMul, 1f));

            if (damage <= 0f)
            {
                return DamageResult.None(amount);
            }

            float mitigated = damage;

            // WHY: shield absorption from modifier shield_hp contributions (per-instance mutable pools).
            float absorbed = 0f;
            if (!isPure)
            {
                absorbed = ConsumeShields(system, targetUnit, ref damage);
            }

            float healthBefore = targetUnit.Health;
            float healthDamage = 0f;
            if (damage > 0f)
            {
                targetUnit.Resources.Decrease(AbilityResourceIds.Health, damage);
                healthDamage = healthBefore - targetUnit.Health;
            }

            bool killed = targetUnit.IsAlive && targetUnit.Health <= 0f;
            if (killed)
            {
                system.MarkDead(targetUnit, source, abilityId, castId);
            }

            var result = new DamageResult(amount, mitigated, absorbed, healthDamage, killed, false, false, crit);

            if (healthDamage > 0f || absorbed > 0f)
            {
                system.Events.Publish(new AbilityEventArgs(AbilityEvents.TakeDamage, target, source,
                    healthDamage, abilityId, damageType: damageType, castId: castId));
                if (source.IsValid)
                {
                    system.Events.Publish(new AbilityEventArgs(AbilityEvents.DealDamage, source, target,
                        healthDamage, abilityId, damageType: damageType, castId: castId));
                }

                if (crit && source.IsValid)
                {
                    system.Events.Publish(new AbilityEventArgs(AbilityEvents.CriticalHit, source, target,
                        healthDamage, abilityId, damageType: damageType, castId: castId));
                }
            }

            if (killed && source.IsValid)
            {
                system.Events.Publish(new AbilityEventArgs(AbilityEvents.Kill, source, target, healthDamage,
                    abilityId, damageType: damageType, castId: castId));
            }

            // WHY: the attacker recovers a percentage of the health damage dealt. lifesteal_percent
            // is 0..100 (50 = half). Only applied when the attacker is a distinct, living unit.
            if (healthDamage > 0f && sourceUnit != null && sourceUnit.IsAlive && source != target)
            {
                float lifestealPercent = sourceUnit.GetProperty(AbilityProperties.LifestealPercent);
                if (lifestealPercent > 0f)
                {
                    float heal = lifestealPercent / 100f * healthDamage;
                    if (heal > 0f)
                    {
                        float before = sourceUnit.Health;
                        sourceUnit.Resources.Increase(AbilityResourceIds.Health, heal);
                        float healed = sourceUnit.Health - before;
                        if (healed > 0f)
                        {
                            system.Events.Publish(new AbilityEventArgs(AbilityEvents.HealReceived, source,
                                target, healed, abilityId, castId: castId));
                        }
                    }
                }
            }

            return result;
        }

        private static float ConsumeShields(AbilitySystem system, AbilityUnit target, ref float damage)
        {
            if (damage <= 0f)
            {
                return 0f;
            }

            while (ModifierScratchPool.Count <= _shieldDepth)
            {
                ModifierScratchPool.Add(new List<ModifierInstance>(8));
            }

            List<ModifierInstance> scratch = ModifierScratchPool[_shieldDepth];
            _shieldDepth++;
            try
            {
                system.Modifiers.GetModifiers(target.Id, scratch);

                float totalAbsorbed = 0f;
                for (int i = 0; i < scratch.Count && damage > 0f; i++)
                {
                    ModifierInstance m = scratch[i];
                    float capacity = ShieldCapacity(m);
                    if (capacity <= 0f)
                    {
                        continue;
                    }

                    float available = capacity - m.ShieldConsumed;
                    if (available <= 0f)
                    {
                        continue;
                    }

                    float take = MathF.Min(available, damage);
                    m.ShieldConsumed += take;
                    damage -= take;
                    totalAbsorbed += take;

                    system.Events.Publish(new AbilityEventArgs(AbilityEvents.ShieldAbsorbed, target.Id, m.Caster,
                        take, m.SourceAbilityId, m.Blueprint.Id));

                    if (m.ShieldConsumed >= capacity)
                    {
                        system.Events.Publish(new AbilityEventArgs(AbilityEvents.ShieldBroken, target.Id, m.Caster,
                            capacity, m.SourceAbilityId, m.Blueprint.Id));
                        system.Modifiers.Remove(m);
                    }
                }

                return totalAbsorbed;
            }
            finally
            {
                _shieldDepth--;
            }
        }

        /// <summary>Total shield HP a modifier instance grants (shield_hp contributions, stack-scaled).</summary>
        public static float ShieldCapacity(ModifierInstance instance)
        {
            if (instance == null)
            {
                return 0f;
            }

            List<PropertyContribution> props = instance.Blueprint.Properties;
            if (props == null)
            {
                return 0f;
            }

            float capacity = 0f;
            for (int i = 0; i < props.Count; i++)
            {
                PropertyContribution c = props[i];
                if (string.Equals(c.PropertyId, AbilityProperties.ShieldHp, StringComparison.OrdinalIgnoreCase))
                {
                    capacity += c.ValueForStacks(instance.Stacks);
                }
            }

            return capacity;
        }
    }
}
