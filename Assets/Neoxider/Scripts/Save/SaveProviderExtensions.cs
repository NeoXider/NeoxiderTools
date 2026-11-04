using System;
using System.Globalization;
using System.Text;

namespace Neo.Save
{
    /// <summary>
    ///     Extension methods for save providers.
    ///     Adds 64-bit integers and helpers for arrays and simple collections.
    /// </summary>
    public static class SaveProviderExtensions
    {
        private const char SEPARATOR = ',';

        /// <summary>
        ///     Gets a 64-bit integer by key. Uses <see cref="ILongSaveProvider" /> when the provider implements it;
        ///     otherwise reads an invariant-culture integer string. A key written earlier with
        ///     <see cref="ISaveProvider.SetInt" /> is widened, so a counter can move from <c>int</c> to <c>long</c>
        ///     without migrating existing saves.
        /// </summary>
        /// <param name="provider">Save provider.</param>
        /// <param name="key">Storage key.</param>
        /// <param name="defaultValue">Fallback if the key is missing or holds a non-integer value.</param>
        /// <returns>Stored value or <paramref name="defaultValue" />.</returns>
        public static long GetLong(this ISaveProvider provider, string key, long defaultValue = 0L)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (provider is ILongSaveProvider longProvider)
            {
                return longProvider.GetLong(key, defaultValue);
            }

            if (!provider.HasKey(key))
            {
                return defaultValue;
            }

            if (TryParseInteger(provider.GetString(key, string.Empty), out long parsed))
            {
                return parsed;
            }

            // WHY: GetInt has no "exists" signal; two different defaults agreeing proves an int is stored.
            int stored = provider.GetInt(key, 0);
            if (stored != 0 || provider.GetInt(key, 1) == 0)
            {
                return stored;
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets a 64-bit integer by key. Uses <see cref="ILongSaveProvider" /> when the provider implements it;
        ///     otherwise stores the value as an invariant-culture string, so saves stay readable on any device locale.
        /// </summary>
        /// <param name="provider">Save provider.</param>
        /// <param name="key">Storage key.</param>
        /// <param name="value">Value to store.</param>
        public static void SetLong(this ISaveProvider provider, string key, long value)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (provider is ILongSaveProvider longProvider)
            {
                longProvider.SetLong(key, value);
                return;
            }

            provider.SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Saves an int array under the given key.
        /// </summary>
        /// <param name="provider">Save provider</param>
        /// <param name="key">Storage key</param>
        /// <param name="array">Values to store</param>
        public static void SetIntArray(this ISaveProvider provider, string key, int[] array)
        {
            if (array == null || array.Length == 0)
            {
                provider.DeleteKey(key);
                return;
            }

            StringBuilder builder = new(array.Length * 4);
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(SEPARATOR);
                }

                builder.Append(array[i].ToString(CultureInfo.InvariantCulture));
            }

            provider.SetString(key, builder.ToString());
        }

        /// <summary>
        ///     Loads an int array from the given key.
        /// </summary>
        /// <param name="provider">Save provider</param>
        /// <param name="key">Storage key</param>
        /// <param name="defaultValue">Fallback if the key is missing or invalid</param>
        /// <returns>Int array</returns>
        public static int[] GetIntArray(this ISaveProvider provider, string key, int[] defaultValue = null)
        {
            if (!provider.HasKey(key))
            {
                return defaultValue ?? Array.Empty<int>();
            }

            string arrayString = provider.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? Array.Empty<int>();
            }

            string[] parts = arrayString.Split(SEPARATOR);
            int[] result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!TryParseInteger(parts[i], out long value) || value < int.MinValue || value > int.MaxValue)
                {
                    return defaultValue ?? Array.Empty<int>();
                }

                result[i] = (int)value;
            }

            return result;
        }

        /// <summary>
        ///     Saves a float array under the given key.
        /// </summary>
        /// <param name="provider">Save provider</param>
        /// <param name="key">Storage key</param>
        /// <param name="array">Values to store</param>
        public static void SetFloatArray(this ISaveProvider provider, string key, float[] array)
        {
            if (array == null || array.Length == 0)
            {
                provider.DeleteKey(key);
                return;
            }

            // WHY: culture-default float formatting uses ',' as decimal separator on ru-RU/de-DE etc.,
            // which collides with the array separator and silently corrupts the data on round-trip.
            StringBuilder builder = new(array.Length * 8);
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(SEPARATOR);
                }

                builder.Append(array[i].ToString(CultureInfo.InvariantCulture));
            }

            provider.SetString(key, builder.ToString());
        }

        /// <summary>
        ///     Loads a float array from the given key.
        /// </summary>
        /// <param name="provider">Save provider</param>
        /// <param name="key">Storage key</param>
        /// <param name="defaultValue">Fallback if the key is missing or invalid</param>
        /// <returns>Float array</returns>
        public static float[] GetFloatArray(this ISaveProvider provider, string key, float[] defaultValue = null)
        {
            if (!provider.HasKey(key))
            {
                return defaultValue ?? Array.Empty<float>();
            }

            string arrayString = provider.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? Array.Empty<float>();
            }

            string[] parts = arrayString.Split(SEPARATOR);
            float[] result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                {
                    return defaultValue ?? Array.Empty<float>();
                }
            }

            return result;
        }

        /// <summary>
        ///     Parses an integer written in the invariant culture, falling back to the current culture for values that
        ///     an older build wrote with culture-default formatting.
        /// </summary>
        internal static bool TryParseInteger(string text, out long value)
        {
            if (string.IsNullOrEmpty(text))
            {
                value = 0L;
                return false;
            }

            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                   || long.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
        }
    }
}
