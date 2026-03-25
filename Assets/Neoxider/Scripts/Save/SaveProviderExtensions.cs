using System;
using System.Linq;

namespace Neo.Save
{
    /// <summary>
    ///     Extension methods for save providers.
    ///     Adds helpers for arrays and simple collections.
    /// </summary>
    public static class SaveProviderExtensions
    {
        private const char SEPARATOR = ',';

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

            provider.SetString(key, string.Join(SEPARATOR.ToString(), array));
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
                return defaultValue ?? new int[0];
            }

            string arrayString = provider.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? new int[0];
            }

            try
            {
                return arrayString.Split(SEPARATOR).Select(int.Parse).ToArray();
            }
            catch (Exception)
            {
                return defaultValue ?? new int[0];
            }
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

            provider.SetString(key, string.Join(SEPARATOR.ToString(), array));
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
                return defaultValue ?? new float[0];
            }

            string arrayString = provider.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? new float[0];
            }

            try
            {
                return arrayString.Split(SEPARATOR).Select(float.Parse).ToArray();
            }
            catch (Exception)
            {
                return defaultValue ?? new float[0];
            }
        }
    }
}
