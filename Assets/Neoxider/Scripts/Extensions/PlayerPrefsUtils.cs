using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Provides utility methods for saving and loading arrays using PlayerPrefs.
    ///     Note: You must call PlayerPrefs.Save() manually after using the Set methods.
    /// </summary>
    public static class PlayerPrefsUtils
    {
        [System.Serializable]
        private class StringArrayData
        {
            public string[] Value;
        }

        private const char SEPARATOR = ',';

        #region Int Array

        /// <summary>
        ///     Saves an array of integers to PlayerPrefs.
        /// </summary>
        public static void SetIntArray(string key, int[] array)
        {
            if (array == null || array.Length == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            PlayerPrefs.SetString(key, string.Join(SEPARATOR.ToString(), array));
        }

        /// <summary>
        ///     Loads an array of integers from PlayerPrefs.
        /// </summary>
        public static int[] GetIntArray(string key, int[] defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue ?? new int[0];
            }

            string arrayString = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? new int[0];
            }

            try
            {
                return arrayString.Split(SEPARATOR)
                    .Select(value => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture))
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error loading int array for key '{key}': {ex.Message}. Returning default value.");
                return defaultValue ?? new int[0];
            }
        }

        #endregion

        #region Float Array

        /// <summary>
        ///     Saves an array of floating-point numbers to PlayerPrefs.
        /// </summary>
        public static void SetFloatArray(string key, float[] array)
        {
            if (array == null || array.Length == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            PlayerPrefs.SetString(key, string.Join(SEPARATOR.ToString(),
                array.Select(value => value.ToString(CultureInfo.InvariantCulture))));
        }

        /// <summary>
        ///     Loads an array of floating-point numbers from PlayerPrefs.
        /// </summary>
        public static float[] GetFloatArray(string key, float[] defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue ?? new float[0];
            }

            string arrayString = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? new float[0];
            }

            try
            {
                return arrayString.Split(SEPARATOR)
                    .Select(value => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture))
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error loading float array for key '{key}': {ex.Message}. Returning default value.");
                return defaultValue ?? new float[0];
            }
        }

        #endregion

        #region String Array

        /// <summary>
        ///     Saves an array of strings to PlayerPrefs.
        /// </summary>
        public static void SetStringArray(string key, string[] array)
        {
            if (array == null || array.Length == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            if (array.Any(value => !string.IsNullOrEmpty(value) && value.Contains(SEPARATOR.ToString())))
            {
                throw new ArgumentException(
                    $"PlayerPrefs string array values cannot contain '{SEPARATOR}' because the legacy storage format uses it as a separator.",
                    nameof(array));
            }

            PlayerPrefs.SetString(key, string.Join(SEPARATOR.ToString(), array));
        }

        /// <summary>
        ///     Loads an array of strings from PlayerPrefs.
        /// </summary>
        public static string[] GetStringArray(string key, string[] defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue ?? new string[0];
            }

            string arrayString = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? new string[0];
            }

            try
            {
                string trimmedArrayString = arrayString.TrimStart();
                if (trimmedArrayString.StartsWith("{", StringComparison.Ordinal) &&
                    trimmedArrayString.Contains("\"Value\""))
                {
                    StringArrayData data = JsonUtility.FromJson<StringArrayData>(trimmedArrayString);
                    return data?.Value ?? defaultValue ?? new string[0];
                }

                return arrayString.Split(SEPARATOR);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error loading string array for key '{key}': {ex.Message}. Returning default value.");
                return defaultValue ?? new string[0];
            }
        }

        #endregion

        #region Bool Array

        /// <summary>
        ///     Saves an array of booleans to PlayerPrefs.
        /// </summary>
        public static void SetBoolArray(string key, bool[] array)
        {
            if (array == null || array.Length == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            int[] intArray = array.Select(b => b ? 1 : 0).ToArray();
            SetIntArray(key, intArray);
        }

        /// <summary>
        ///     Loads an array of booleans from PlayerPrefs.
        /// </summary>
        public static bool[] GetBoolArray(string key, bool[] defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue ?? new bool[0];
            }

            try
            {
                int[] intArray = GetIntArray(key);
                if (intArray.Any(i => i != 0 && i != 1))
                {
                    return defaultValue ?? new bool[0];
                }

                return intArray.Select(i => i == 1).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error loading bool array for key '{key}': {ex.Message}. Returning default value.");
                return defaultValue ?? new bool[0];
            }
        }

        #endregion
    }
}
