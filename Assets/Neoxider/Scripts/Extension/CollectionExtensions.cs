using System.Collections.Generic;

namespace Neoxider
{
    public static class CollectionExtensions
    {
        public static T SafeGetElement<T>(this IList<T> collection, int index, T defaultValue = default)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                return defaultValue;

            return collection[index];
        }

        public static T GetElementByModulo<T>(this IList<T> collection, int index)
        {
            if (collection == null || collection.Count == 0)
                throw new System.ArgumentException("Collection cannot be null or empty.");

            return collection[index % collection.Count];
        }

        public static bool ContainsElement<T>(this IList<T> collection, T element)
        {
            if (collection == null)
                return false;

            return collection.Contains(element);
        }

        public static bool IsIndexValid<T>(this IList<T> collection, int index)
        {
            return collection != null && index >= 0 && index < collection.Count;
        }
    }
}