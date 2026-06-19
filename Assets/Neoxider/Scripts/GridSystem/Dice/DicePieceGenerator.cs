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
            if (!pair || pool.Count == 1)
            {
                return DicePiece.Single(pool[_range(pool.Count)]);
            }

            int firstIndex = _range(pool.Count);
            int secondIndex = _range(pool.Count - 1);
            if (secondIndex >= firstIndex)
            {
                secondIndex++;
            }

            return DicePiece.Pair(pool[firstIndex], pool[secondIndex]);
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

            List<int> values = new(maxValue - minValue + 1);
            for (int value = minValue; value <= maxValue; value++)
            {
                values.Add(value);
            }

            return values;
        }
    }
}
