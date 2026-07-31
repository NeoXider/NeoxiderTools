using System;
using System.Collections.Generic;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for <see cref="IDictionary{TKey,TValue}" /> covering the most-repeated
    ///     get-or-create and counter patterns so games don't re-implement them per call site.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        ///     Returns the existing value for <paramref name="key" />, or creates one with <c>new TValue()</c>,
        ///     stores it, and returns it. Ideal for buckets like <c>dict.GetOrCreate(id).Add(item)</c>.
        /// </summary>
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = new TValue();
                dictionary[key] = value;
            }

            return value;
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" />, or creates one via
        ///     <paramref name="factory" />, stores it, and returns it.
        /// </summary>
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> factory)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = factory(key);
                dictionary[key] = value;
            }

            return value;
        }

        /// <summary>
        ///     Adds <paramref name="amount" /> (default 1) to the integer counter at <paramref name="key" />,
        ///     creating it at zero first. Returns the new total. Replaces the
        ///     <c>dict[k] = dict.GetValueOrDefault(k) + 1</c> boilerplate.
        /// </summary>
        public static int Increment<TKey>(this IDictionary<TKey, int> dictionary, TKey key, int amount = 1)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            int newValue = dictionary.TryGetValue(key, out int current) ? current + amount : amount;
            dictionary[key] = newValue;
            return newValue;
        }

        /// <summary>
        ///     Adds <paramref name="amount" /> (default 1) to the float counter at <paramref name="key" />,
        ///     creating it at zero first. Returns the new total.
        /// </summary>
        public static float Increment<TKey>(this IDictionary<TKey, float> dictionary, TKey key, float amount = 1f)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            float newValue = dictionary.TryGetValue(key, out float current) ? current + amount : amount;
            dictionary[key] = newValue;
            return newValue;
        }
    }
}
