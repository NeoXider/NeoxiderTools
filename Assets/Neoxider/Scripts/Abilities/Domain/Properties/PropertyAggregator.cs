using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Pure deterministic property math: final = max((base + sum(Add)) * product(Mul), all Max floors).
    ///     Stateless — callers pass the active contributions; caching lives on the unit.
    /// </summary>
    public static class PropertyAggregator
    {
        public static float Compute(float baseValue, List<ResolvedContribution> contributions)
        {
            float add = 0f;
            float mul = 1f;
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
                            mul *= c.Value;
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

            float result = (baseValue + add) * mul;
            if (hasFloor && floor > result)
            {
                result = floor;
            }

            return result;
        }
    }
}
