using System;

namespace Neo.Abilities
{
    /// <summary>Effective cooldown (or charge restore time) in seconds of a cast. Must stay finite and non-negative.</summary>
    public interface ICooldownRule
    {
        float ResolveCooldown(AbilityUnit caster, AbilityBlueprint ability, float baseSeconds);
    }

    /// <summary>
    ///     Effective amount of one authored cost. Return NaN or infinity to reject the cast as unaffordable;
    ///     non-positive amounts are not paid.
    /// </summary>
    public interface ICastCostRule
    {
        float ResolveCost(AbilityUnit caster, AbilityBlueprint ability, AbilityCost cost);
    }

    /// <summary>Default: base × (1 − cooldown_reduction_percent / 100), the reduction clamped to 0..90.</summary>
    public class StandardCooldownRule : ICooldownRule
    {
        public static readonly StandardCooldownRule Instance = new StandardCooldownRule();

        public virtual float ResolveCooldown(AbilityUnit caster, AbilityBlueprint ability, float baseSeconds)
        {
            float cdr = Math.Clamp(caster.GetProperty(AbilityProperties.CooldownReductionPercent), 0f, 90f);
            return baseSeconds * (1f - cdr / 100f);
        }
    }

    /// <summary>Default: mana costs scale by mana_cost_mul (floored at 0); a non-finite multiplier rejects the cast.</summary>
    public class StandardCastCostRule : ICastCostRule
    {
        public static readonly StandardCastCostRule Instance = new StandardCastCostRule();

        public virtual float ResolveCost(AbilityUnit caster, AbilityBlueprint ability, AbilityCost cost)
        {
            float amount = cost.Amount;
            if (AbilityIdComparer.Same(cost.ResourceId, AbilityResourceIds.Mana))
            {
                float multiplier = caster.GetProperty(AbilityProperties.ManaCostMul, 1f);
                if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
                {
                    return float.NaN;
                }

                amount *= MathF.Max(0f, multiplier);
            }

            return amount;
        }
    }
}
