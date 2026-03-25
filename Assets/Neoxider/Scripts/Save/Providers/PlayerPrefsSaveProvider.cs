using System;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Save provider implementation backed by Unity PlayerPrefs.
    /// </summary>
    public class PlayerPrefsSaveProvider : ISaveProvider
    {
        private const string BoolPrefix = "Bool_";

        /// <summary>
        ///     Provider type — PlayerPrefs.
        /// </summary>
        public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;

        /// <summary>
        ///     Raised after data is saved.
        /// </summary>
        public event Action OnDataSaved;

        /// <summary>
        ///     Raised after data is loaded.
        /// </summary>
        public event Action OnDataLoaded;

        /// <summary>
        ///     Raised when a key's value changes.
        /// </summary>
        public event Action<string> OnKeyChanged;

        /// <summary>
        ///     Gets an integer value by key.
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        /// <summary>
        ///     Sets an integer value by key.
        /// </summary>
        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Gets a floating-point value by key.
        /// </summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        /// <summary>
        ///     Sets a floating-point value by key.
        /// </summary>
        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Gets a string value by key.
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        /// <summary>
        ///     Sets a string value by key.
        /// </summary>
        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Gets a Boolean value by key.
        ///     Stored as int (0 = false, 1 = true).
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            string boolKey = BoolPrefix + key;
            if (PlayerPrefs.HasKey(boolKey))
            {
                return PlayerPrefs.GetInt(boolKey, 0) == 1;
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets a Boolean value by key.
        ///     Stored as int (0 = false, 1 = true).
        /// </summary>
        public void SetBool(string key, bool value)
        {
            string boolKey = BoolPrefix + key;
            PlayerPrefs.SetInt(boolKey, value ? 1 : 0);
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Returns whether the key exists in storage.
        /// </summary>
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key) || PlayerPrefs.HasKey(BoolPrefix + key);
        }

        /// <summary>
        ///     Removes the key and its value from storage.
        /// </summary>
        public void DeleteKey(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }

            string boolKey = BoolPrefix + key;
            if (PlayerPrefs.HasKey(boolKey))
            {
                PlayerPrefs.DeleteKey(boolKey);
            }
        }

        /// <summary>
        ///     Removes all keys from storage.
        /// </summary>
        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        /// <summary>
        ///     Flushes PlayerPrefs to disk.
        ///     Calls PlayerPrefs.Save() and raises OnDataSaved.
        /// </summary>
        public void Save()
        {
            PlayerPrefs.Save();
            OnDataSaved?.Invoke();
        }

        /// <summary>
        ///     Reloads from storage.
        ///     For PlayerPrefs this is a no-op because values are read automatically;
        ///     still raises OnDataLoaded.
        /// </summary>
        public void Load()
        {
            OnDataLoaded?.Invoke();
        }
    }
}
