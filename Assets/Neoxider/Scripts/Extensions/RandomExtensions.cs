using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for random number generation and collection randomization.
    /// </summary>
    public static class RandomExtensions
    {
        #region Private Helper Methods

        private static void ValidateCollection<T>(ICollection<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection),
                    $"Collection of type {typeof(T).Name} cannot be null.");
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
        }

        #endregion

        #region Collection Extensions

        /// <summary>
        ///     Gets a random element from a list or array.
        /// </summary>
        public static T GetRandomElement<T>(this IList<T> collection)
        {
            ValidateCollection(collection);
            return collection[collection.GetRandomIndex()];
        }

        /// <summary>
        ///     Shuffles the elements of a list or array.
        /// </summary>
        /// <param name="inplace">If true, modifies the original collection; if false, creates a copy.</param>
        public static IList<T> Shuffle<T>(this IList<T> collection, bool inplace = true)
        {
            ValidateCollection(collection);

            var listToShuffle = inplace ? collection : new List<T>(collection);

            var n = listToShuffle.Count;
            while (n > 1)
            {
                n--;
                var k = UnityEngine.Random.Range(0, n + 1);
                (listToShuffle[k], listToShuffle[n]) = (listToShuffle[n], listToShuffle[k]); // Tuple swap
            }

            return listToShuffle;
        }

        /// <summary>
        ///     Gets a specified number of random elements from a list or array.
        /// </summary>
        public static IEnumerable<T> GetRandomElements<T>(this IList<T> collection, int count)
        {
            ValidateCollection(collection);
            if (count <= 0)
            {
                Debug.LogWarning(
                    $"[RandomExtensions] Requested {count} random elements (<=0) from collection of type {typeof(T).Name}");
                return Enumerable.Empty<T>();
            }

            if (collection.Count < count)
            {
                Debug.LogError(
                    $"[RandomExtensions] Collection of type {typeof(T).Name} with length {collection.Count} is less than required count {count}");
                throw new ArgumentException(
                    $"Collection count ({collection.Count}) is less than required count ({count})", nameof(count));
            }

            return collection.Shuffle(false).Take(count);
        }

        /// <summary>
        ///     Gets a random valid index for the given collection.
        /// </summary>
        public static int GetRandomIndex<T>(this ICollection<T> collection)
        {
            ValidateCollection(collection);
            return UnityEngine.Random.Range(0, collection.Count);
        }

        #endregion

        #region Primitive Extensions

        /// <summary>
        ///     Returns true with the given probability.
        /// </summary>
        /// <param name="probability">Probability between 0 and 1.</param>
        public static bool Chance(this float probability)
        {
            if (probability < 0f || probability > 1f)
                throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0 and 1");
            return UnityEngine.Random.value < probability;
        }

        /// <summary>
        ///     Gets a random bool value (50/50 chance).
        /// </summary>
        public static bool Random(this bool _)
        {
            return UnityEngine.Random.value >= 0.5f;
        }

        #endregion

        #region Static Utilities

        /// <summary>
        ///     Gets a random bool value (50/50 chance).
        /// </summary>
        public static bool RandomBool()
        {
            return UnityEngine.Random.value >= 0.5f;
        }

        /// <summary>
        ///     Creates a random color with RGB values between 0 and 1.
        /// </summary>
        /// <param name="alpha">Optional alpha value (defaults to 1).</param>
        public static Color RandomColor(float alpha = 1f)
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, alpha);
        }

        /// <summary>
        ///     Gets a random value from any enum.
        /// </summary>
        public static T GetRandomEnumValue<T>() where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }

        /// <summary>
        ///     Returns an index from a list of weights based on probability.
        /// </summary>
        public static int GetRandomWeightedIndex(this IList<float> weights)
        {
            if (weights == null || weights.Count == 0) return -1;

            float totalWeight = 0;
            foreach (var weight in weights) totalWeight += weight;

            var randomPoint = UnityEngine.Random.value * totalWeight;

            for (var i = 0; i < weights.Count; i++)
            {
                if (randomPoint < weights[i]) return i;
                randomPoint -= weights[i];
            }

            return weights.Count - 1;
        }

        #endregion

        #region Value Range Extensions

        /// <summary>
        ///     Gets a random value between -value and value.
        /// </summary>
        public static float RandomizeBetween(this float value)
        {
            return UnityEngine.Random.Range(-value, value);
        }

        /// <summary>
        ///     Gets a random value between -value and value.
        /// </summary>
        public static int RandomizeBetween(this int value)
        {
            return UnityEngine.Random.Range(-value, value);
        }

        /// <summary>
        ///     Gets a random value between start and value.
        /// </summary>
        public static float RandomFromValue(this float value, float start)
        {
            return UnityEngine.Random.Range(start, value);
        }

        /// <summary>
        ///     Gets a random value between start and value.
        /// </summary>
        public static int RandomFromValue(this int value, int start)
        {
            return UnityEngine.Random.Range(start, value);
        }

        /// <summary>
        ///     Gets a random value between value and end.
        /// </summary>
        public static float RandomToValue(this float value, float end)
        {
            return UnityEngine.Random.Range(value, end);
        }

        /// <summary>
        ///     Gets a random value between value and end.
        /// </summary>
        public static int RandomToValue(this int value, int end)
        {
            return UnityEngine.Random.Range(value, end);
        }

        /// <summary>
        ///     Gets a random value between x and y components.
        /// </summary>
        public static float RandomRange(this Vector2 vector)
        {
            return UnityEngine.Random.Range(vector.x, vector.y);
        }

        /// <summary>
        ///     Gets a random value between x and y components.
        /// </summary>
        public static int RandomRange(this Vector2Int vector)
        {
            return UnityEngine.Random.Range(vector.x, vector.y);
        }

        #endregion
    }
}