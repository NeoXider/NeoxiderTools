using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Neo
{
    /// <summary>
    /// Extension methods for random number generation and collection randomization
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Gets a random element from an array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Source array</param>
        /// <returns>Random element from the array</returns>
        /// <exception cref="ArgumentException">Thrown when array is null or empty</exception>
        public static T GetRandomElement<T>(this T[] array)
        {
            ValidateArray(array);
            return array[GetRandomIndex(array.Length)];
        }

        /// <summary>
        /// Gets a random element from a list
        /// </summary>
        /// <typeparam name="T">Type of list elements</typeparam>
        /// <param name="list">Source list</param>
        /// <returns>Random element from the list</returns>
        /// <exception cref="ArgumentException">Thrown when list is null or empty</exception>
        public static T GetRandomElement<T>(this List<T> list)
        {
            ValidateList(list);
            return list[GetRandomIndex(list.Count)];
        }

        /// <summary>
        /// Returns true with the given probability
        /// </summary>
        /// <param name="probability">Probability between 0 and 1</param>
        /// <returns>True if random value is less than probability</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when probability is not between 0 and 1</exception>
        public static bool Chance(this float probability)
        {
            if (probability < 0f || probability > 1f)
                throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0 and 1");

            return UnityEngine.Random.value < probability;
        }

        /// <summary>
        /// Shuffles the elements of an array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Array to shuffle</param>
        /// <param name="inplace">If true, modifies the original array; if false, creates a copy</param>
        /// <returns>Shuffled array</returns>
        public static IList<T> Shuffle<T>(this T[] array, bool inplace = true)
        {
            ValidateArray(array);
            return ShuffleCollection(array, inplace);
        }

        /// <summary>
        /// Shuffles the elements of a list
        /// </summary>
        /// <typeparam name="T">Type of list elements</typeparam>
        /// <param name="list">List to shuffle</param>
        /// <param name="inplace">If true, modifies the original list; if false, creates a copy</param>
        /// <returns>Shuffled list</returns>
        public static IList<T> Shuffle<T>(this List<T> list, bool inplace = true)
        {
            ValidateList(list);
            return ShuffleCollection(list, inplace);
        }

        /// <summary>
        /// Creates a random color with RGB values between 0 and 1
        /// </summary>
        /// <param name="alpha">Optional alpha value (defaults to 1)</param>
        /// <returns>Random color</returns>
        public static Color RandomColor(float alpha = 1f)
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, alpha);
        }

        /// <summary>
        /// Gets a random index within the specified length
        /// </summary>
        /// <param name="length">Maximum exclusive value</param>
        /// <returns>Random index between 0 (inclusive) and length (exclusive)</returns>
        public static int GetRandomIndex(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than 0", nameof(length));
                
            return UnityEngine.Random.Range(0, length);
        }

        /// <summary>
        /// Gets a random valid index for the array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Source array</param>
        /// <returns>Random valid index for the array</returns>
        public static int GetRandomIndex<T>(this T[] array)
        {
            ValidateArray(array);
            return GetRandomIndex(array.Length);
        }

        /// <summary>
        /// Gets a specified number of random elements from an array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Source array</param>
        /// <param name="count">Number of elements to get</param>
        /// <returns>Array of random elements</returns>
        public static IList<T> GetRandomElements<T>(this T[] array, int count)
        {
            ValidateArray(array);
            if (count <= 0)
                throw new ArgumentException("Count must be greater than 0", nameof(count));
            if (array.Length < count)
                throw new ArgumentException($"Array length ({array.Length}) is less than required count ({count})", nameof(count));

            return array.Shuffle().Take(count).ToArray();
        }

        /// <summary>
        /// Gets a specified number of random elements from a list
        /// </summary>
        /// <typeparam name="T">Type of list elements</typeparam>
        /// <param name="list">Source list</param>
        /// <param name="count">Number of elements to get</param>
        /// <returns>List of random elements</returns>
        public static IList<T> GetRandomElements<T>(this List<T> list, int count)
        {
            ValidateList(list);
            return list.ToArray().GetRandomElements(count);
        }

        #region Value Range Extensions

        /// <summary>
        /// Gets a random value between -value and value
        /// </summary>
        public static float RandomizeBetween(this float value)
        {
            return UnityEngine.Random.Range(-value, value);
        }

        /// <summary>
        /// Gets a random value between -value and value
        /// </summary>
        public static int RandomizeBetween(this int value)
        {
            return UnityEngine.Random.Range(-value, value);
        }

        /// <summary>
        /// Gets a random value between start and value
        /// </summary>
        public static float RandomFromValue(this float value, float start)
        {
            return UnityEngine.Random.Range(start, value);
        }

        /// <summary>
        /// Gets a random value between start and value
        /// </summary>
        public static int RandomFromValue(this int value, int start)
        {
            return UnityEngine.Random.Range(start, value);
        }

        /// <summary>
        /// Gets a random value between value and end
        /// </summary>
        public static float RandomToValue(this float value, float end)
        {
            return UnityEngine.Random.Range(value, end);
        }

        /// <summary>
        /// Gets a random value between value and end
        /// </summary>
        public static int RandomToValue(this int value, int end)
        {
            return UnityEngine.Random.Range(value, end);
        }

        /// <summary>
        /// Gets a random value between x and y components
        /// </summary>
        public static float RandomRange(this Vector2 vector)
        {
            return UnityEngine.Random.Range(vector.x, vector.y);
        }

        /// <summary>
        /// Gets a random value between x and y components
        /// </summary>
        public static int RandomRange(this Vector2Int vector)
        {
            return UnityEngine.Random.Range(vector.x, vector.y);
        }

        #endregion

        #region Private Helper Methods

        private static IList<T> ShuffleCollection<T>(IList<T> collection, bool inplace = true)
        {
            if (!inplace)
            {
                collection = new List<T>(collection);
            }

            int n = collection.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T temp = collection[k];
                collection[k] = collection[n];
                collection[n] = temp;
            }

            return collection;
        }

        private static void ValidateArray<T>(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), "Array cannot be null");
            if (array.Length == 0)
                throw new ArgumentException("Array cannot be empty", nameof(array));
        }

        private static void ValidateList<T>(List<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "List cannot be null");
            if (list.Count == 0)
                throw new ArgumentException("List cannot be empty", nameof(list));
        }

        #endregion
    }
}