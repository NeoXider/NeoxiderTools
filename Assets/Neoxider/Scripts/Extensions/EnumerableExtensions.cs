using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for working with IEnumerable, IList and other collections.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        ///     Performs an action on each item in the enumeration.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            if (enumeration == null || action == null) return;
            foreach (var item in enumeration) action(item);
        }

        /// <summary>
        ///     Safely gets an element from the collection at the specified index.
        /// </summary>
        public static T GetSafe<T>(this IList<T> collection, int index, T defaultValue = default)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                return defaultValue;

            return collection[index];
        }

        /// <summary>
        ///     Gets an element using modulo operation (wraps around if index exceeds collection size).
        /// </summary>
        public static T GetWrapped<T>(this IList<T> collection, int index)
        {
            if (collection == null || collection.Count == 0)
                throw new ArgumentException("Collection cannot be null or empty.");

            return collection[index % collection.Count];
        }

        /// <summary>
        ///     Checks if the specified index is valid for this collection.
        /// </summary>
        public static bool IsValidIndex<T>(this ICollection<T> collection, int index)
        {
            return collection != null && index >= 0 && index < collection.Count;
        }

        /// <summary>
        ///     Converts collection to formatted string with indices.
        /// </summary>
        public static string ToIndexedString<T>(this IEnumerable<T> collection)
        {
            var sb = new StringBuilder();
            var index = 0;
            foreach (var item in collection) sb.AppendLine($"[{index++}] {item}");
            return sb.ToString();
        }

        /// <summary>
        ///     Checks if collection is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }

        /// <summary>
        ///     Converts collection to string with custom separator.
        /// </summary>
        public static string ToStringJoined<T>(this IEnumerable<T> collection, string separator = ", ")
        {
            return string.Join(separator, collection);
        }

        /// <summary>
        ///     Finds duplicate elements in collection.
        /// </summary>
        public static IEnumerable<T> FindDuplicates<T>(this IEnumerable<T> collection)
        {
            return collection.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
        }

        /// <summary>
        ///     Converts list to a debug-friendly string with indices.
        /// </summary>
        public static string ToDebugString<T>(this IList<T> list)
        {
            var sb = new StringBuilder("[");
            for (var i = 0; i < list.Count; i++)
            {
                sb.Append(i + ": ");
                sb.Append(list[i]);
                if (i < list.Count - 1) sb.Append(", ");
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        ///     Counts null elements in an array.
        /// </summary>
        public static int CountEmptyElements<T>(this T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var emptyCount = 0;
            foreach (var element in array)
                if (element == null)
                    emptyCount++;

            return emptyCount;
        }
    }
}