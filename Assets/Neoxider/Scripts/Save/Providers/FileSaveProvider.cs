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
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";

        private readonly string _rootDirectory;
        private readonly FileSaveEncryptionConfig _encryption;
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
        public FileSaveProvider(string fileName = "save.json", FileSaveProviderOptions options = null)
        {
            options ??= new FileSaveProviderOptions();
            _rootDirectory = string.IsNullOrEmpty(options.PersistenceRoot)
                ? Application.persistentDataPath
                : options.PersistenceRoot;
            _encryption = options.Encryption;
            _filePath = Path.Combine(_rootDirectory, fileName);
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

            _filePath = Path.Combine(_rootDirectory, fileName);
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

                string payload = json;
                if (_encryption != null && _encryption.Enabled)
                {
                    if (!SaveFileEncryption.TryEncrypt(json, _encryption.Key, _encryption.Iv, out string cipher) ||
                        string.IsNullOrEmpty(cipher))
                    {
                        SaveProvider.LogError("[FileSaveProvider] Encryption failed; save aborted.");
                        return;
                    }

                    payload = cipher;
                }

                if (!TryCommitPayload(payload))
                {
                    return;
                }

                _isDirty = false;
                OnDataSaved?.Invoke();
            }
            catch (Exception ex)
            {
                SaveProvider.LogCritical($"[FileSaveProvider] Failed to save data to {_filePath}: {ex.Message}");
            }
        }

        /// <summary>
        ///     Reloads data from disk. Falls back to the rotating backup when the main file is missing,
        ///     empty, or unparsable.
        /// </summary>
        public void Load()
        {
            DeleteQuietly(TempPath);
            _data = ReadDataOrEmpty();
            OnDataLoaded?.Invoke();
        }

        /// <summary>Path of the staging file used to make a save commit atomic.</summary>
        private string TempPath => _filePath + TempSuffix;

        /// <summary>Path of the previous complete save, rotated in by every successful commit.</summary>
        private string BackupPath => _filePath + BackupSuffix;

        /// <summary>
        ///     Writes the payload to a temporary file and swaps it in, so an interrupted write can never leave a
        ///     truncated save behind. Returns false when the data did not reach disk.
        /// </summary>
        private bool TryCommitPayload(string payload)
        {
            string tempPath = TempPath;
            try
            {
                File.WriteAllText(tempPath, payload);
            }
            catch (Exception ex)
            {
                SaveProvider.LogCritical(
                    $"[FileSaveProvider] Could not stage save data at {tempPath}: {ex.Message}. Existing save kept.");
                return false;
            }

            try
            {
                if (File.Exists(_filePath))
                {
                    // WHY: File.Replace swaps the file and rotates the previous one into the backup in a single
                    // operation, so a crash at any point still leaves one complete save on disk.
                    File.Replace(tempPath, _filePath, BackupPath);
                }
                else
                {
                    File.Move(tempPath, _filePath);
                }

                return true;
            }
            catch (Exception replaceException)
            {
                return TryCommitWithoutReplace(tempPath, replaceException);
            }
        }

        /// <summary>
        ///     Fallback commit for filesystems that do not implement <see cref="File.Replace(string,string,string)" />
        ///     (some Android and WebGL backends). Still keeps a backup while the target is briefly absent.
        /// </summary>
        private bool TryCommitWithoutReplace(string tempPath, Exception replaceException)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Copy(_filePath, BackupPath, true);
                    File.Delete(_filePath);
                }

                File.Move(tempPath, _filePath);
                return true;
            }
            catch (Exception ex)
            {
                SaveProvider.LogCritical(
                    $"[FileSaveProvider] Failed to commit save to {_filePath}: {ex.Message} " +
                    $"(atomic replace failed with: {replaceException.Message}).");
                DeleteQuietly(tempPath);
                return false;
            }
        }

        private Dictionary<string, SaveValue> ReadDataOrEmpty()
        {
            if (TryReadFile(_filePath, out Dictionary<string, SaveValue> primary))
            {
                return primary;
            }

            // WHY: a backup only exists after a successful commit, so it is always a complete save.
            // Preferring it over an empty dictionary is what turns a crash mid-write into a lost session
            // instead of a lost profile.
            if (TryReadFile(BackupPath, out Dictionary<string, SaveValue> backup))
            {
                SaveProvider.LogCritical(
                    $"[FileSaveProvider] Save file {_filePath} was unreadable; restored from {BackupPath}.");
                return backup;
            }

            if (File.Exists(_filePath) || File.Exists(BackupPath))
            {
                SaveProvider.LogCritical(
                    $"[FileSaveProvider] Save file {_filePath} and its backup are unreadable; starting from empty data.");
            }

            return new Dictionary<string, SaveValue>();
        }

        private bool TryReadFile(string path, out Dictionary<string, SaveValue> data)
        {
            data = null;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                string raw = File.ReadAllText(path).Trim().TrimStart('\ufeff');
                if (string.IsNullOrEmpty(raw))
                {
                    // WHY: a successful commit always writes a JSON document, so an empty file means the write
                    // was cut short. Treat it as damaged so the backup gets its chance.
                    return false;
                }

                return TryBuildDictionaryFromFilePayload(raw, out data);
            }
            catch (Exception ex)
            {
                SaveProvider.LogError($"[FileSaveProvider] Failed to read {path}: {ex.Message}");
                return false;
            }
        }

        private static void DeleteQuietly(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                SaveProvider.LogError($"[FileSaveProvider] Failed to delete {path}: {ex.Message}");
            }
        }

        private bool TryBuildDictionaryFromFilePayload(string raw, out Dictionary<string, SaveValue> data)
        {
            data = new Dictionary<string, SaveValue>();
            if (string.IsNullOrEmpty(raw))
            {
                return true;
            }

            if (TryDeserializeSaveDataJson(raw, out SaveData fromPlain))
            {
                return CopyItemsToDictionary(fromPlain, ref data);
            }

            if (_encryption != null && _encryption.Enabled &&
                SaveFileEncryption.TryDecrypt(raw, _encryption.Key, _encryption.Iv, out string decrypted) &&
                TryDeserializeSaveDataJson(decrypted, out SaveData fromCipher))
            {
                return CopyItemsToDictionary(fromCipher, ref data);
            }

            return false;
        }

        private static bool TryDeserializeSaveDataJson(string json, out SaveData saveData)
        {
            saveData = null;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            json = json.Trim();
            if (json.Length == 0 || json[0] != '{')
            {
                return false;
            }

            try
            {
                saveData = JsonUtility.FromJson<SaveData>(json);
                return saveData != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool CopyItemsToDictionary(SaveData saveData, ref Dictionary<string, SaveValue> data)
        {
            data = new Dictionary<string, SaveValue>();
            if (saveData?.items == null)
            {
                return true;
            }

            foreach (KeyValuePair item in saveData.items)
            {
                if (!string.IsNullOrEmpty(item.key))
                {
                    data[item.key] = item.value;
                }
            }

            return true;
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
