using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Generates single or pair dice pieces from a value pool.
    /// </summary>
    public sealed class DicePieceGenerator
    {
        private readonly Func<int, int> _range;

        public DicePieceGenerator(Func<int, int> range = null)
        {
            _range = range ?? (max => UnityEngine.Random.Range(0, max));
        }

        public DicePiece Generate(IReadOnlyList<int> pool, bool? forcePair = null)
        {
            if (pool == null || pool.Count == 0)
            {
                throw new ArgumentException("Dice piece pool must contain at least one value.", nameof(pool));
            }

            bool pair = forcePair ?? _range(2) == 1;
            int firstValue = pool[_range(pool.Count)];
            if (!pair || pool.Count == 1)
            {
                return DicePiece.Single(firstValue);
            }

            List<int> remainingValues = new(pool.Count - 1);
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != firstValue)
                {
                    remainingValues.Add(pool[i]);
                }
            }

            return remainingValues.Count == 0
                ? DicePiece.Single(firstValue)
                : DicePiece.Pair(firstValue, remainingValues[_range(remainingValues.Count)]);
        }

        public DicePiece GenerateWeighted(IReadOnlyList<DiceValueWeight> weights, bool? forcePair = null)
        {
            List<DiceValueWeight> usableWeights = GetUsableWeights(weights);
            bool pair = forcePair ?? _range(2) == 1;
            if (!pair || usableWeights.Count == 1)
            {
                return DicePiece.Single(RollWeightedValue(usableWeights));
            }

            int firstIndex = RollWeightedIndex(usableWeights);
            int firstValue = usableWeights[firstIndex].Value;
            usableWeights.RemoveAt(firstIndex);
            int secondValue = RollWeightedValue(usableWeights);
            return DicePiece.Pair(firstValue, secondValue);
        }

        public static List<int> CreateDefaultPool()
        {
            return CreateSequentialPool(1, 5);
        }

        public static List<int> CreateD6Pool()
        {
            return CreateSequentialPool(1, 6);
        }

        public static List<int> CreateSequentialPool(int minValue, int maxValue)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentException("Maximum value must be greater than or equal to minimum value.", nameof(maxValue));
            }

            long count = (long)maxValue - minValue + 1;
            if (count > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), "The requested dice pool is too large.");
            }

            List<int> values = new((int)count);
            for (int index = 0; index < (int)count; index++)
            {
                values.Add(minValue + index);
            }

            return values;
        }

        private static List<DiceValueWeight> GetUsableWeights(IReadOnlyList<DiceValueWeight> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                throw new ArgumentException("Weighted dice pool must contain at least one positive weight.", nameof(weights));
            }

            Dictionary<int, int> mergedWeights = new();
            int totalWeight = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                DiceValueWeight entry = weights[i];
                if (entry.Weight > 0)
                {
                    if (entry.Weight > int.MaxValue - totalWeight)
                    {
                        throw new ArgumentException("The sum of positive dice weights must not exceed Int32.MaxValue.",
                            nameof(weights));
                    }

                    totalWeight += entry.Weight;
                    mergedWeights.TryGetValue(entry.Value, out int currentWeight);
                    mergedWeights[entry.Value] = currentWeight + entry.Weight;
                }
            }

            List<DiceValueWeight> usableWeights = new(mergedWeights.Count);
            foreach (KeyValuePair<int, int> entry in mergedWeights)
            {
                usableWeights.Add(new DiceValueWeight(entry.Key, entry.Value));
            }

            if (usableWeights.Count == 0)
            {
                throw new ArgumentException("Weighted dice pool must contain at least one positive weight.", nameof(weights));
            }

            return usableWeights;
        }

        private int RollWeightedValue(IReadOnlyList<DiceValueWeight> weights)
        {
            return weights[RollWeightedIndex(weights)].Value;
        }

        private int RollWeightedIndex(IReadOnlyList<DiceValueWeight> weights)
        {
            int totalWeight = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += weights[i].Weight;
            }

            int roll = _range(totalWeight);
            int cursor = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cursor += weights[i].Weight;
                if (roll < cursor)
                {
                    return i;
                }
            }

            return weights.Count - 1;
        }
    }
}
