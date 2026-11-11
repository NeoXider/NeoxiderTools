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
            {
                throw new ArgumentNullException(nameof(collection),
                    $"Collection of type {typeof(T).Name} cannot be null.");
            }

            if (collection.Count == 0)
            {
                throw new ArgumentException("Collection cannot be empty", nameof(collection));
            }
        }

        private static void ShuffleInPlace<T>(IList<T> list, System.Random random)
        {
            for (int n = list.Count - 1; n > 0; n--)
            {
                int k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        private static void ShuffleInPlace<T>(IList<T> list, Func<int, int> nextIndex)
        {
            for (int n = list.Count - 1; n > 0; n--)
            {
                int k = nextIndex(n + 1);
                if (k < 0 || k > n)
                {
                    throw new ArgumentOutOfRangeException(nameof(nextIndex),
                        $"The random source returned {k}; expected a value in [0, {n}].");
                }

                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        private static int SelectWeightedIndex(IList<float> weights, double unitSample)
        {
            int count = weights.Count;
            double totalWeight = 0d;
            // WHY: an indexed loop; foreach over IList<float> boxes the enumerator on every call.
            for (int i = 0; i < count; i++)
            {
                float weight = weights[i];
                if (float.IsNaN(weight) || float.IsInfinity(weight))
                {
                    NeoDiagnostics.LogError("[RandomExtensions] Weights must be finite values.");
                    return -1;
                }

                if (weight < 0f)
                {
                    NeoDiagnostics.LogError("[RandomExtensions] Weights cannot contain negative values.");
                    return -1;
                }

                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                NeoDiagnostics.LogWarning("[RandomExtensions] Total weight must be greater than zero.");
                return -1;
            }

            double randomPoint = unitSample * totalWeight;

            for (int i = 0; i < count; i++)
            {
                if (randomPoint < weights[i])
                {
                    return i;
                }

                randomPoint -= weights[i];
            }

            // WHY: Random.value includes 1.0, so the walk can pass every bucket; fall back to the
            // last positive-weight index so a zero-weight (disabled) entry is never returned.
            for (int i = count - 1; i >= 0; i--)
            {
                if (weights[i] > 0f)
                {
                    return i;
                }
            }

            return count - 1;
        }

        private static class EnumValues<T> where T : Enum
        {
            public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
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

            IList<T> listToShuffle = inplace ? collection : new List<T>(collection);

            int n = listToShuffle.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (listToShuffle[k], listToShuffle[n]) = (listToShuffle[n], listToShuffle[k]);
            }

            return listToShuffle;
        }

        /// <summary>
        ///     Shuffles the elements of a list or array with an injected <see cref="System.Random" />, so a seeded
        ///     source produces the same order every run (deterministic tests, replays, daily seeds).
        /// </summary>
        /// <param name="collection">List or array to shuffle; must not be null or empty.</param>
        /// <param name="random">Random source; must not be null.</param>
        /// <param name="inplace">If true, modifies the original collection; if false, shuffles a copy.</param>
        /// <returns>The shuffled list (the original when <paramref name="inplace" /> is true).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random" /> is null.</exception>
        public static IList<T> Shuffle<T>(this IList<T> collection, System.Random random, bool inplace = true)
        {
            ValidateCollection(collection);
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            IList<T> listToShuffle = inplace ? collection : new List<T>(collection);
            ShuffleInPlace(listToShuffle, random);
            return listToShuffle;
        }

        /// <summary>
        ///     Shuffles the elements of a list or array with an injected index source, for random generators that are
        ///     not a <see cref="System.Random" /> (a seeded game RNG, a scripted sequence in a test).
        /// </summary>
        /// <param name="collection">List or array to shuffle; must not be null or empty.</param>
        /// <param name="nextIndex">
        ///     Receives an exclusive upper bound <c>n</c> and returns an index in <c>[0, n)</c>, e.g.
        ///     <c>max =&gt; rng.Next(max)</c>.
        /// </param>
        /// <param name="inplace">If true, modifies the original collection; if false, shuffles a copy.</param>
        /// <returns>The shuffled list (the original when <paramref name="inplace" /> is true).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nextIndex" /> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The index source returned a value outside the requested range.</exception>
        public static IList<T> Shuffle<T>(this IList<T> collection, Func<int, int> nextIndex, bool inplace = true)
        {
            ValidateCollection(collection);
            if (nextIndex == null)
            {
                throw new ArgumentNullException(nameof(nextIndex));
            }

            IList<T> listToShuffle = inplace ? collection : new List<T>(collection);
            ShuffleInPlace(listToShuffle, nextIndex);
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
                NeoDiagnostics.LogWarning(
                    $"[RandomExtensions] Requested {count} random elements (<=0) from collection of type {typeof(T).Name}");
                return Enumerable.Empty<T>();
            }

            if (collection.Count < count)
            {
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

        /// <summary>
        ///     Gets a random valid index using an injected <see cref="System.Random" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="random" /> is null.</exception>
        public static int GetRandomIndex<T>(this ICollection<T> collection, System.Random random)
        {
            ValidateCollection(collection);
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return random.Next(collection.Count);
        }

        /// <summary>
        ///     Gets a random element from a list or array using an injected <see cref="System.Random" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="random" /> is null.</exception>
        public static T GetRandomElement<T>(this IList<T> collection, System.Random random)
        {
            return collection[collection.GetRandomIndex(random)];
        }

        /// <summary>
        ///     Returns a random element chosen by a parallel list of weights (returns the element, not the index).
        ///     Returns <c>default</c> when the collections are empty/mismatched or all weights are non-positive.
        /// </summary>
        /// <param name="items">Elements to choose from.</param>
        /// <param name="weights">Weight per element; must be the same length as <paramref name="items" />.</param>
        public static T GetRandomWeighted<T>(this IList<T> items, IList<float> weights)
        {
            if (items == null || items.Count == 0)
            {
                NeoDiagnostics.LogWarning("[RandomExtensions] GetRandomWeighted called on a null/empty collection.");
                return default;
            }

            if (weights == null || weights.Count != items.Count)
            {
                NeoDiagnostics.LogError(
                    "[RandomExtensions] GetRandomWeighted requires a weights list of the same length as the items.");
                return default;
            }

            int index = weights.GetRandomWeightedIndex();
            return index < 0 ? default : items[index];
        }

        /// <summary>
        ///     Returns a random element using a weight selector (returns the element, not the index).
        ///     Returns <c>default</c> when the collection is empty or all weights are non-positive.
        /// </summary>
        /// <param name="items">Elements to choose from.</param>
        /// <param name="weightSelector">Maps each element to its (non-negative) weight.</param>
        public static T GetRandomWeighted<T>(this IList<T> items, Func<T, float> weightSelector)
        {
            if (items == null || items.Count == 0)
            {
                NeoDiagnostics.LogWarning("[RandomExtensions] GetRandomWeighted called on a null/empty collection.");
                return default;
            }

            if (weightSelector == null)
            {
                throw new ArgumentNullException(nameof(weightSelector));
            }

            float[] weights = new float[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                weights[i] = weightSelector(items[i]);
            }

            int index = weights.GetRandomWeightedIndex();
            return index < 0 ? default : items[index];
        }

        #endregion

        #region Primitive Extensions

        /// <summary>
        ///     Returns true with the given probability.
        /// </summary>
        /// <param name="probability">Probability between 0 and 1.</param>
        public static bool Chance(this float probability)
        {
            if (float.IsNaN(probability) || probability < 0f || probability > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0 and 1");
            }

            float sample = UnityEngine.Random.value;
            return probability == 1f || sample < probability;
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
        /// <exception cref="InvalidOperationException">The enum declares no values.</exception>
        public static T GetRandomEnumValue<T>() where T : Enum
        {
            // WHY: a typed per-enum cache avoids the dictionary lookup and the boxing of Array.GetValue.
            T[] values = EnumValues<T>.Values;
            if (values.Length == 0)
            {
                throw new InvalidOperationException($"Enum {typeof(T).Name} declares no values.");
            }

            return values[UnityEngine.Random.Range(0, values.Length)];
        }

        /// <summary>
        ///     Returns an index from a list of weights based on probability.
        ///     Weights must be finite and non-negative, with a positive total; otherwise returns -1.
        /// </summary>
        public static int GetRandomWeightedIndex(this IList<float> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                return -1;
            }

            return SelectWeightedIndex(weights, UnityEngine.Random.value);
        }

        /// <summary>
        ///     Returns an index from a list of weights using an injected <see cref="System.Random" />.
        ///     Weights must be finite and non-negative, with a positive total; otherwise returns -1.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="random" /> is null.</exception>
        public static int GetRandomWeightedIndex(this IList<float> weights, System.Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (weights == null || weights.Count == 0)
            {
                return -1;
            }

            return SelectWeightedIndex(weights, random.NextDouble());
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
        /// <remarks>For int.MinValue, samples the full representable int range.</remarks>
        public static int RandomizeBetween(this int value)
        {
            if (value == int.MinValue)
            {
                // WHY: The positive magnitude cannot fit in int. Sample a half-range, then choose
                // its sign half so both endpoints remain reachable without negation overflow.
                int sample = UnityEngine.Random.Range(int.MinValue, 0);
                return UnityEngine.Random.Range(0, 2) == 0 ? sample : sample - int.MinValue;
            }

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
