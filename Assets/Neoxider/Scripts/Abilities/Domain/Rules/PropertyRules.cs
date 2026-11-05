using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Combines a unit's base value with the active modifier contributions of one property. Must be pure:
    ///     results are cached per unit until modifiers, base values or <see cref="AbilityRules.Version" /> change.
    /// </summary>
    public interface IPropertyAggregator
    {
        float Compute(string propertyId, float baseValue, List<ResolvedContribution> contributions);
    }

    /// <summary>
    ///     Default layering: final = max((base + ΣAdd) × ΠMul, every Max floor). Multipliers compound
    ///     (two 1.5 multipliers give 2.25).
    /// </summary>
    public sealed class LayeredPropertyAggregator : IPropertyAggregator
    {
        public static readonly LayeredPropertyAggregator Instance = new LayeredPropertyAggregator();

        public float Compute(string propertyId, float baseValue, List<ResolvedContribution> contributions)
        {
            return PropertyAggregator.Compute(baseValue, contributions);
        }
    }

    /// <summary>
    ///     Additive-percent layering, common in action RPGs: final = max((base + ΣAdd) × (1 + Σ(Mul − 1)), Max floors).
    ///     Two 1.5 multipliers give ×2 instead of ×2.25.
    /// </summary>
    public sealed class AdditivePercentPropertyAggregator : IPropertyAggregator
    {
        public static readonly AdditivePercentPropertyAggregator Instance = new AdditivePercentPropertyAggregator();

        public float Compute(string propertyId, float baseValue, List<ResolvedContribution> contributions)
        {
            float add = 0f;
            float percent = 0f;
            bool hasFloor = false;
            float floor = float.MinValue;
            if (contributions != null)
            {
                for (int i = 0; i < contributions.Count; i++)
                {
                    ResolvedContribution c = contributions[i];
                    switch (c.Op)
                    {
                        case PropertyOp.Add:
                            add += c.Value;
                            break;
                        case PropertyOp.Mul:
                            percent += c.Value - 1f;
                            break;
                        case PropertyOp.Max:
                            hasFloor = true;
                            if (c.Value > floor)
                            {
                                floor = c.Value;
                            }

                            break;
                    }
                }
            }

            float result = (baseValue + add) * (1f + percent);
            return hasFloor && floor > result ? floor : result;
        }
    }
}
