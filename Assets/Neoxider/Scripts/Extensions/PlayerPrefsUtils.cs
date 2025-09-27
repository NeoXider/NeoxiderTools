using System;
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
            if (!PlayerPrefs.HasKey(key)) return defaultValue ?? new int[0];

            var arrayString = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(arrayString)) return defaultValue ?? new int[0];

            try
            {
                return arrayString.Split(SEPARATOR).Select(int.Parse).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading int array for key '{key}': {ex.Message}. Returning default value.");
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

            PlayerPrefs.SetString(key, string.Join(SEPARATOR.ToString(), array));
        }

        /// <summary>
        ///     Loads an array of floating-point numbers from PlayerPrefs.
        /// </summary>
        public static float[] GetFloatArray(string key, float[] defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue ?? new float[0];

            var arrayString = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(arrayString)) return defaultValue ?? new float[0];

            try
            {
                return arrayString.Split(SEPARATOR).Select(float.Parse).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading float array for key '{key}': {ex.Message}. Returning default value.");
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

            PlayerPrefs.SetString(key, string.Join(SEPARATOR.ToString(), array));
        }

        /// <summary>
        ///     Loads an array of strings from PlayerPrefs.
        /// </summary>
        public static string[] GetStringArray(string key, string[] defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue ?? new string[0];

            var arrayString = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(arrayString)) return defaultValue ?? new string[0];

            return arrayString.Split(SEPARATOR);
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

            var intArray = array.Select(b => b ? 1 : 0).ToArray();
            SetIntArray(key, intArray);
        }

        /// <summary>
        ///     Loads an array of booleans from PlayerPrefs.
        /// </summary>
        public static bool[] GetBoolArray(string key, bool[] defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue ?? new bool[0];

            try
            {
                var intArray = GetIntArray(key);
                return intArray.Select(i => i == 1).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading bool array for key '{key}': {ex.Message}. Returning default value.");
                return defaultValue ?? new bool[0];
            }
        }

        #endregion
    }
}