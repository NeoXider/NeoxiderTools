using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Save provider implementation backed by a JSON file.
    ///     All data is stored in a single JSON file.
    /// </summary>
    public class FileSaveProvider : ISaveProvider
    {
        private string _filePath;
        private Dictionary<string, SaveValue> _data;
        private bool _isDirty;

        /// <summary>
        ///     Creates a new FileSaveProvider instance.
        /// </summary>
        /// <param name="fileName">
        ///     File name for persistence (e.g. "save.json"). Written under
        ///     Application.persistentDataPath.
        /// </param>
        public FileSaveProvider(string fileName = "save.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
            _data = new Dictionary<string, SaveValue>();
            Load();
        }

        /// <summary>
        ///     Switches the active save slot by changing the underlying file.
        /// </summary>
        /// <param name="fileName">New file name for persistence.</param>
        public void ChangeSlot(string fileName)
        {
            if (_isDirty)
            {
                Save();
            }

            _filePath = Path.Combine(Application.persistentDataPath, fileName);
            Load();
        }

        /// <summary>
        ///     Provider type — File.
        /// </summary>
        public SaveProviderType ProviderType => SaveProviderType.File;

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
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == "int")
            {
                if (int.TryParse(saveValue.value, out int result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets an integer value by key.
        /// </summary>
        public void SetInt(string key, int value)
        {
            _data[key] = new SaveValue { type = "int", value = value.ToString() };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Gets a floating-point value by key.
        /// </summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == "float")
            {
                if (float.TryParse(saveValue.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets a floating-point value by key.
        /// </summary>
        public void SetFloat(string key, float value)
        {
            _data[key] = new SaveValue { type = "float", value = value.ToString(CultureInfo.InvariantCulture) };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Gets a string value by key.
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == "string")
            {
                return saveValue.value ?? defaultValue;
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets a string value by key.
        /// </summary>
        public void SetString(string key, string value)
        {
            _data[key] = new SaveValue { type = "string", value = value ?? "" };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Gets a Boolean value by key.
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == "bool")
            {
                if (bool.TryParse(saveValue.value, out bool result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets a Boolean value by key.
        /// </summary>
        public void SetBool(string key, bool value)
        {
            _data[key] = new SaveValue { type = "bool", value = value.ToString() };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Returns whether the key exists in storage.
        /// </summary>
        public bool HasKey(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        ///     Removes the key and its value from storage.
        /// </summary>
        public void DeleteKey(string key)
        {
            if (_data.Remove(key))
            {
                _isDirty = true;
            }
        }

        /// <summary>
        ///     Removes all keys from storage.
        /// </summary>
        public void DeleteAll()
        {
            _data.Clear();
            _isDirty = true;
        }

        /// <summary>
        ///     Flushes data to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                SaveData saveData = new();
                foreach (KeyValuePair<string, SaveValue> kvp in _data)
                {
                    saveData.items.Add(new KeyValuePair { key = kvp.Key, value = kvp.Value });
                }

                string json = JsonUtility.ToJson(saveData, true);

                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_filePath, json);
                _isDirty = false;
                OnDataSaved?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveProvider] Failed to save data to {_filePath}: {ex.Message}");
            }
        }

        /// <summary>
        ///     Reloads data from disk.
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                    _data = new Dictionary<string, SaveValue>();
                    if (saveData != null && saveData.items != null)
                    {
                        foreach (KeyValuePair item in saveData.items)
                        {
                            if (!string.IsNullOrEmpty(item.key))
                            {
                                _data[item.key] = item.value;
                            }
                        }
                    }
                }
                else
                {
                    _data = new Dictionary<string, SaveValue>();
                }

                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveProvider] Failed to load data from {_filePath}: {ex.Message}");
                _data = new Dictionary<string, SaveValue>();
                OnDataLoaded?.Invoke();
            }
        }

        /// <summary>
        ///     Persists dirty data when the provider is finalized (if autosave-on-change was not used elsewhere).
        ///     Invoked when the object is destroyed by the GC.
        /// </summary>
        ~FileSaveProvider()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        /// <summary>
        ///     Typed value container for serialization.
        /// </summary>
        [Serializable]
        private class SaveValue
        {
            public string type;
            public string value;
        }

        /// <summary>
        ///     Key-value pair for JSON serialization.
        /// </summary>
        [Serializable]
        private class KeyValuePair
        {
            public string key;
            public SaveValue value;
        }

        /// <summary>
        ///     Root JSON structure for all stored entries.
        /// </summary>
        [Serializable]
        private class SaveData
        {
            public List<KeyValuePair> items = new();
        }
    }
}
