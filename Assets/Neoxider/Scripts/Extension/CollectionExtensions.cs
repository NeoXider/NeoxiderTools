using System.Collections.Generic;

namespace Neo
{
    /// <summary>
    /// Extension methods for working with collections safely
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Safely gets an element from the collection at the specified index
        /// </summary>
        /// <typeparam name="T">Type of elements in collection</typeparam>
        /// <param name="collection">Source collection</param>
        /// <param name="index">Index to get element from</param>
        /// <param name="defaultValue">Value to return if index is invalid</param>
        /// <returns>Element at index or default value if index is invalid</returns>
        public static T GetSafe<T>(this IList<T> collection, int index, T defaultValue = default)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                return defaultValue;

            return collection[index];
        }

        /// <summary>
        /// Gets an element using modulo operation (wraps around if index exceeds collection size)
        /// </summary>
        /// <typeparam name="T">Type of elements in collection</typeparam>
        /// <param name="collection">Source collection</param>
        /// <param name="index">Index to get element from</param>
        /// <returns>Element at wrapped index</returns>
        /// <exception cref="System.ArgumentException">Thrown when collection is null or empty</exception>
        public static T GetWrapped<T>(this IList<T> collection, int index)
        {
            if (collection == null || collection.Count == 0)
                throw new System.ArgumentException("Collection cannot be null or empty.");

            return collection[index % collection.Count];
        }


        /// <summary>
        /// Checks if the specified index is valid for this collection
        /// </summary>
        /// <typeparam name="T">Type of elements in collection</typeparam>
        /// <param name="collection">Source collection</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if index is valid, false otherwise or if collection is null</returns>
        public static bool IsValidIndex<T>(this IList<T> collection, int index)
        {
            return collection != null && index >= 0 && index < collection.Count;
        }
    }
}