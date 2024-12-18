using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Random = UnityEngine.Random;

namespace Neoxider
{
    public static class RandomExtensions
    {
        public static T GetRandomElement<T>(this T[] array)
        {
            ValidateArray(array);
            return array[GetRandomIndex(array.Length)];
        }

        public static T GetRandomElement<T>(this List<T> list)
        {
            ValidateList(list);
            return list[GetRandomIndex(list.Count)];
        }

        public static bool Chance(this float probability)
        {
            if (probability < 0f || probability > 1f)
                throw new ArgumentOutOfRangeException("Probability must be between 0 and 1.");

            return Random.value < probability;
        }

        public static IList<T> Shuffle<T>(this T[] array)
        {
            ValidateArray(array);
            return ShuffleCollection(array);
        }

        public static IList<T> Shuffle<T>(this List<T> list)
        {
            ValidateList(list);
            return ShuffleCollection(list);
        }

        public static Color RandomColor()
        {
            return new Color(Random.value, Random.value, Random.value);
        }

        public static int GetRandomIndex(int length)
        {
            return Random.Range(0, length);
        }

        public static int GetRandomIndex<T>(this T[] array)
        {
            return GetRandomIndex(array.Length);
        }

        public static IList<T> GetRandomElements<T>(this T[] array, int count)
        {
            if (array.Length < count)
            {
                throw new ArgumentException("Array length is less than required count");
            }

            return array.Shuffle().Take(count).ToArray();
        }

        public static IList<T> GetRandomElements<T>(this List<T> array, int count)
        {
            return array.ToArray().GetRandomElements(count);
        }

        private static IList<T> ShuffleCollection<T>(IList<T> collection)
        {
            for (int i = collection.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = collection[i];
                collection[i] = collection[j];
                collection[j] = temp;
            }

            return collection;
        }

        private static void ValidateArray<T>(T[] array)
        {
            if (array == null || array.Length == 0)
                throw new System.ArgumentException("Array cannot be null or empty.");
        }

        private static void ValidateList<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
                throw new System.ArgumentException("List cannot be null or empty.");
        }
    }
}