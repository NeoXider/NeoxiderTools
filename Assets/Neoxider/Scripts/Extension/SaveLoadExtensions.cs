using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Provides extension methods for saving and loading arrays using PlayerPrefs.
/// </summary>
namespace Neo
{
    public static class SaveLoadExtensions
    {
        private const char SEPARATOR = ',';

        /// <summary>
        /// Saves an array of integers to PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to save the array under.</param>
        /// <param name="array">The array of integers to save.</param>
        /// <remarks>
        /// If the array is null or empty, the key will be deleted from PlayerPrefs.
        /// The array is stored as a comma-separated string.
        /// </remarks>
        public static void SetIntArray(string key, int[] array)
        {
            if (array == null || array.Length == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            string arrayString = string.Join(SEPARATOR.ToString(), array.Select(i => i.ToString()).ToArray());
            PlayerPrefs.SetString(key, arrayString);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads an array of integers from PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to load the array from.</param>
        /// <param name="defaultValue">The default value to return if the key is not found or the data is invalid.</param>
        /// <returns>The loaded array of integers, or the default value if the key is not found or the data is invalid.</returns>
        /// <remarks>
        /// If the key is not found or the data is invalid, returns the default value or an empty array if no default value is provided.
        /// </remarks>
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
                return arrayString.Split(SEPARATOR).Select(int.Parse).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading array for key '{key}': {ex.Message}. Returning default value.");
                return defaultValue ?? new int[0];
            }
        }

        /// <summary>
        /// Saves an array of floating-point numbers to PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to save the array under.</param>
        /// <param name="array">The array of floating-point numbers to save.</param>
        /// <remarks>
        /// If the array is null or empty, the key will be deleted from PlayerPrefs.
        /// The array is stored as a comma-separated string.
        /// </remarks>
        public static void SetFloatArray(string key, float[] array)
        {
            if (array == null || array.Length == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            string arrayString = string.Join(SEPARATOR.ToString(), array.Select(f => f.ToString()).ToArray());
            PlayerPrefs.SetString(key, arrayString);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads an array of floating-point numbers from PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to load the array from.</param>
        /// <param name="defaultValue">The default value to return if the key is not found or the data is invalid.</param>
        /// <returns>The loaded array of floating-point numbers, or the default value if the key is not found or the data is invalid.</returns>
        /// <remarks>
        /// If the key is not found or the data is invalid, returns the default value or an empty array if no default value is provided.
        /// </remarks>
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
                return arrayString.Split(SEPARATOR).Select(float.Parse).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading array for key '{key}': {ex.Message}. Returning default value.");
                return defaultValue ?? new float[0];
            }
        }

        /// <summary>
        /// Saves an array of strings to PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to save the array under.</param>
        /// <param name="array">The array of strings to save.</param>
        /// <remarks>
        /// If the array is null or empty, the key will be deleted from PlayerPrefs.
        /// The array is stored as a comma-separated string.
        /// </remarks>
        public static void SetStringArray(string key, string[] array)
        {
            if (array == null || array.Length == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            string arrayString = string.Join(SEPARATOR.ToString(), array);
            PlayerPrefs.SetString(key, arrayString);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads an array of strings from PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to load the array from.</param>
        /// <param name="defaultValue">The default value to return if the key is not found or the data is invalid.</param>
        /// <returns>The loaded array of strings, or the default value if the key is not found or the data is invalid.</returns>
        /// <remarks>
        /// If the key is not found or the data is invalid, returns the default value or an empty array if no default value is provided.
        /// </remarks>
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

            return arrayString.Split(SEPARATOR);
        }
    }
}