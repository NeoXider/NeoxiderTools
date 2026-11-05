using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Static entry of the damage pipeline: forwards to the <see cref="DamagePipeline" /> of the given system
    ///     (<see cref="AbilitySystem.Damage" />). Every path in the system (effect ops, basic attacks, projectiles,
    ///     contact damage) applies damage through it so hooks, events and receipts stay consistent.
    /// </summary>
    public static class DamageService
    {
        public static DamageResult ApplyDamage(AbilitySystem system, UnitId source, UnitId target,
            float amount, string damageType, string abilityId = null, uint castId = 0,
            IRandomSource random = null)
        {
            return system == null
                ? DamageResult.None(amount)
                : system.Damage.Apply(source, target, amount, damageType, abilityId, castId, random);
        }

        /// <summary>Applies damage with explicit <see cref="DamageFlags" /> (a direct <see cref="DamageFlags.Hit" />...).</summary>
        public static DamageResult ApplyDamage(AbilitySystem system, UnitId source, UnitId target,
            float amount, string damageType, DamageFlags flags, string abilityId = null, uint castId = 0,
            IRandomSource random = null)
        {
            return system == null
                ? DamageResult.None(amount)
                : system.Damage.Apply(source, target, amount, damageType, abilityId, castId, random, flags);
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
                if (AbilityIdComparer.Same(c.PropertyId, AbilityProperties.ShieldHp))
                {
                    capacity += c.ValueForStacks(instance.Stacks);
                }
            }

            return capacity;
        }
    }
}
