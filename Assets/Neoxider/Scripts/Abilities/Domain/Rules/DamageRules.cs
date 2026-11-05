using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Turns the in-flight <see cref="DamageContext" /> into the amount that reaches shields and health. Set
    ///     <see cref="DamageContext.Evaded" /> (and return 0) for a dodge, <see cref="DamageContext.Crit" /> for a crit.
    /// </summary>
    public interface IDamageFormula
    {
        float Compute(DamageContext damage, AbilityUnit source, AbilityUnit target, AbilityRules rules);
    }

    /// <summary>Mitigation of an amount by the victim's defenses for the context's damage type.</summary>
    public interface IMitigationRule
    {
        float Mitigate(DamageContext damage, AbilityUnit target, float amount);
    }

    /// <summary>Critical hit roll of the attacker. Return true with the full damage multiplier.</summary>
    public interface ICriticalHitRule
    {
        bool TryRollCritical(DamageContext damage, AbilityUnit source, out float multiplier);
    }

    /// <summary>Evasion roll of the victim. Return true to dodge the hit.</summary>
    public interface IEvasionRule
    {
        bool RollEvasion(DamageContext damage, AbilityUnit target);
    }

    /// <summary>
    ///     Default formula, identical to the classic pipeline: (raw + flat add) × hook multiplier, evasion, outgoing
    ///     multiplier, crit, mitigation, incoming multiplier. Evasion is rolled before crit, as before, so seeded
    ///     outcomes of existing content do not change.
    /// </summary>
    public class StandardDamageFormula : IDamageFormula
    {
        public static readonly StandardDamageFormula Instance = new StandardDamageFormula();

        public virtual float Compute(DamageContext damage, AbilityUnit source, AbilityUnit target, AbilityRules rules)
        {
            float amount = (damage.RawAmount + damage.FlatAdd) * damage.Multiplier;
            if (!(amount > 0f))
            {
                return 0f;
            }

            if (!damage.HasFlag(DamageFlags.NoEvasion) && rules.Evasion.RollEvasion(damage, target))
            {
                damage.Evaded = true;
                return 0f;
            }

            if (source != null)
            {
                amount *= MathF.Max(0f, source.GetProperty(AbilityProperties.OutgoingDamageMul, 1f));
                if (!damage.HasFlag(DamageFlags.NoCrit) &&
                    rules.CriticalHit.TryRollCritical(damage, source, out float multiplier))
                {
                    damage.Crit = true;
                    damage.CritMultiplier = multiplier;
                    amount *= multiplier;
                }
            }

            amount = rules.Mitigation.Mitigate(damage, target, amount);
            return amount * MathF.Max(0f, target.GetProperty(AbilityProperties.IncomingDamageMul, 1f));
        }
    }

    /// <summary>
    ///     Default mitigation: physical damage by armor (Dota curve 0.06·armor / (1 + 0.06·|armor|)), magical damage by
    ///     magic_resist_percent (clamped -100..100). Pure and custom types are not mitigated.
    /// </summary>
    public class ArmorResistMitigationRule : IMitigationRule
    {
        public static readonly ArmorResistMitigationRule Instance = new ArmorResistMitigationRule();

        public virtual float Mitigate(DamageContext damage, AbilityUnit target, float amount)
        {
            if (damage.IsPhysical)
            {
                float armor = target.GetProperty(AbilityProperties.Armor);
                float reduction = 0.06f * armor / (1f + 0.06f * MathF.Abs(armor));
                return amount * (1f - reduction);
            }

            if (damage.IsMagical)
            {
                float resist = target.GetProperty(AbilityProperties.MagicResistPercent);
                return amount * (1f - Math.Clamp(resist, -100f, 100f) / 100f);
            }

            return amount;
        }
    }

    /// <summary>
    ///     Default crit: one roll per application on the context RNG against the attacker's crit_chance (0..1);
    ///     the multiplier is crit_multiplier floored at 1. No RNG, no crit.
    /// </summary>
    public class PropertyCriticalHitRule : ICriticalHitRule
    {
        public static readonly PropertyCriticalHitRule Instance = new PropertyCriticalHitRule();

        public virtual bool TryRollCritical(DamageContext damage, AbilityUnit source, out float multiplier)
        {
            multiplier = 1f;
            if (damage.Random == null)
            {
                return false;
            }

            float chance = source.GetProperty(AbilityProperties.CritChance);
            if (chance > 0f && damage.Random.NextFloat() < chance)
            {
                multiplier = MathF.Max(1f, source.GetProperty(AbilityProperties.CritMultiplier));
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Default evasion: physical damage only, one roll on the context RNG against the victim's evasion_chance
    ///     (0..1). No RNG, no evasion.
    /// </summary>
    public class PropertyEvasionRule : IEvasionRule
    {
        public static readonly PropertyEvasionRule Instance = new PropertyEvasionRule();

        public virtual bool RollEvasion(DamageContext damage, AbilityUnit target)
        {
            if (damage.Random == null || !damage.IsPhysical)
            {
                return false;
            }

            float evasion = target.GetProperty(AbilityProperties.EvasionChance);
            return evasion > 0f && damage.Random.NextFloat() < evasion;
        }
    }
}
