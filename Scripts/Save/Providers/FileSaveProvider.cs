using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Save provider implementation backed by a JSON file.
    ///     All data is stored in a single JSON file. Values are staged in memory and reach disk only on
    ///     <see cref="Save" />; a flush with no pending change does not rewrite the file.
    /// </summary>
    public class FileSaveProvider : ILongSaveProvider
    {
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";

        private const string IntType = "int";
        private const string LongType = "long";
        private const string FloatType = "float";
        private const string StringType = "string";
        private const string BoolType = "bool";

        private readonly string _rootDirectory;
        private readonly FileSaveEncryptionConfig _encryption;
        private readonly SaveData _writeBuffer = new() { items = new List<KeyValuePair>() };
        private readonly List<KeyValuePair> _pairPool = new();
        private string _filePath;
        private Dictionary<string, SaveValue> _data;
        private bool _isDirty;
        private bool _loadedFromBackup;
        private bool _rewritePending;

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
            TryChangeSlot(fileName);
        }

        /// <summary>
        ///     Switches files only after pending data is successfully saved. On failure, keeps the current
        ///     slot and its in-memory data so the caller can retry.
        /// </summary>
        /// <returns>Whether the new slot was activated. False for an empty file name.</returns>
        public bool TryChangeSlot(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                SaveProvider.LogWarning("[FileSaveProvider] TryChangeSlot: file name is empty; slot kept.");
                return false;
            }

            string nextPath = Path.Combine(_rootDirectory, fileName);
            if (_isDirty)
            {
                Save();
                if (_isDirty)
                {
                    return false;
                }
            }

            _filePath = nextPath;
            Load();
            return true;
        }

        /// <summary>
        ///     Provider type — File.
        /// </summary>
        public SaveProviderType ProviderType => SaveProviderType.File;

        /// <summary>
        ///     Full path of the active slot file.
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        ///     Whether values changed since the last successful <see cref="Save" /> or <see cref="Load" />.
        /// </summary>
        public bool HasUnsavedChanges => _isDirty;

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
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == IntType
                && SaveProviderExtensions.TryParseInteger(saveValue.value, out long result)
                && result >= int.MinValue && result <= int.MaxValue)
            {
                return (int)result;
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets an integer value by key.
        /// </summary>
        public void SetInt(string key, int value)
        {
            SetValue(key, IntType, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Gets a 64-bit integer by key. Entries written by <see cref="SetInt" /> or as an integer string by
        ///     <see cref="SetString" /> are widened, so existing saves keep their values.
        /// </summary>
        public long GetLong(string key, long defaultValue = 0L)
        {
            if (_data.TryGetValue(key, out SaveValue saveValue)
                && (saveValue.type == LongType || saveValue.type == IntType || saveValue.type == StringType)
                && SaveProviderExtensions.TryParseInteger(saveValue.value, out long result))
            {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        ///     Sets a 64-bit integer by key, stored with the <c>long</c> type tag in invariant culture.
        /// </summary>
        public void SetLong(string key, long value)
        {
            SetValue(key, LongType, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Gets a floating-point value by key.
        /// </summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == FloatType)
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
            SetValue(key, FloatType, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Gets a string value by key.
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == StringType)
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
            SetValue(key, StringType, value ?? "");
        }

        /// <summary>
        ///     Gets a Boolean value by key.
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_data.TryGetValue(key, out SaveValue saveValue) && saveValue.type == BoolType)
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
            SetValue(key, BoolType, value ? bool.TrueString : bool.FalseString);
        }

        /// <summary>
        ///     Stages a typed value. Writing the value a key already holds keeps the provider clean, so repeated
        ///     writes of unchanged settings do not force the next <see cref="Save" /> to rewrite the file.
        /// </summary>
        private void SetValue(string key, string type, string value)
        {
            if (!_data.TryGetValue(key, out SaveValue current) || current == null
                || !string.Equals(current.type, type, StringComparison.Ordinal)
                || !string.Equals(current.value, value, StringComparison.Ordinal))
            {
                _data[key] = new SaveValue { type = type, value = value };
                _isDirty = true;
            }

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
        ///     Flushes data to disk. When nothing changed since the last commit and the file is intact, the write is
        ///     skipped and <see cref="OnDataSaved" /> is still raised.
        /// </summary>
        public void Save()
        {
            // WHY: SaveManager, the SaveProvider facade and focus/pause hooks may all flush in the same frame;
            // rewriting an unchanged file also rotates the backup, so skip it unless recovery or format migration
            // needs a fresh commit.
            if (!_isDirty && !_loadedFromBackup && !_rewritePending && File.Exists(_filePath))
            {
                OnDataSaved?.Invoke();
                return;
            }

            try
            {
                string json = SerializeData();

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
                        SaveProvider.LogCritical(
                            $"[FileSaveProvider] Encryption failed; save to {_filePath} aborted and pending data kept.");
                        return;
                    }

                    payload = cipher;
                }

                if (!TryCommitPayload(payload))
                {
                    return;
                }

                _isDirty = false;
                _loadedFromBackup = false;
                _rewritePending = false;
                OnDataSaved?.Invoke();
            }
            catch (Exception ex)
            {
                SaveProvider.LogCritical($"[FileSaveProvider] Failed to save data to {_filePath}: {ex.Message}");
            }
        }

        /// <summary>
        ///     Serializes the staged data with reused wrapper objects instead of a new key/value list per flush.
        /// </summary>
        private string SerializeData()
        {
            List<KeyValuePair> items = _writeBuffer.items;
            items.Clear();
            int used = 0;
            try
            {
                foreach (KeyValuePair<string, SaveValue> entry in _data)
                {
                    KeyValuePair pair;
                    if (used < _pairPool.Count)
                    {
                        pair = _pairPool[used];
                    }
                    else
                    {
                        pair = new KeyValuePair();
                        _pairPool.Add(pair);
                    }

                    used++;
                    pair.key = entry.Key;
                    pair.value = entry.Value;
                    items.Add(pair);
                }

                return JsonUtility.ToJson(_writeBuffer, true);
            }
            finally
            {
                items.Clear();
                for (int i = 0; i < used; i++)
                {
                    _pairPool[i].key = null;
                    _pairPool[i].value = null;
                }
            }
        }

        /// <summary>
        ///     Reloads data from disk. Falls back to the rotating backup when the main file is missing,
        ///     empty, or unparsable.
        /// </summary>
        public void Load()
        {
            DeleteQuietly(TempPath);
            _loadedFromBackup = false;
            _rewritePending = false;
            _data = ReadDataOrEmpty();
            _isDirty = false;
            OnDataLoaded?.Invoke();
        }

        /// <summary>Path of the staging file used to make a save commit atomic.</summary>
        private string TempPath => _filePath + TempSuffix;

        /// <summary>Path of the previous complete save, preserved during the first commit after recovery.</summary>
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
                    File.Replace(tempPath, _filePath, _loadedFromBackup ? null : BackupPath);
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
                    if (!_loadedFromBackup)
                    {
                        File.Copy(_filePath, BackupPath, true);
                    }

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
            if (TryReadFile(_filePath, out Dictionary<string, SaveValue> primary, out bool plaintextWithEncryption))
            {
                // WHY: a legacy plaintext file read while encryption is enabled must be rewritten encrypted on the
                // next flush even when no value changes.
                _rewritePending = plaintextWithEncryption;
                return primary;
            }

            // WHY: a backup only exists after a successful commit, so it is always a complete save.
            // Preferring it over an empty dictionary is what turns a crash mid-write into a lost session
            // instead of a lost profile.
            if (TryReadFile(BackupPath, out Dictionary<string, SaveValue> backup, out _))
            {
                // Keep the known-good backup when the next save replaces the damaged primary.
                _loadedFromBackup = true;
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

        private bool TryReadFile(string path, out Dictionary<string, SaveValue> data, out bool plaintextWithEncryption)
        {
            data = null;
            plaintextWithEncryption = false;
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

                return TryBuildDictionaryFromFilePayload(raw, out data, out plaintextWithEncryption);
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

        private bool TryBuildDictionaryFromFilePayload(string raw, out Dictionary<string, SaveValue> data,
            out bool plaintextWithEncryption)
        {
            data = new Dictionary<string, SaveValue>();
            plaintextWithEncryption = false;
            if (string.IsNullOrEmpty(raw))
            {
                return true;
            }

            if (TryDeserializeSaveDataJson(raw, out SaveData fromPlain))
            {
                plaintextWithEncryption = _encryption != null && _encryption.Enabled;
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
            if (json.Length == 0 || json[0] != '{' || !HasRootItemsArray(json))
            {
                return false;
            }

            try
            {
                saveData = JsonUtility.FromJson<SaveData>(json);
                return saveData?.items != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasRootItemsArray(string json)
        {
            // JsonUtility can materialize missing/null lists as empty, even without a field initializer.
            // Check the wire shape first; JsonUtility still handles typed deserialization and value syntax.
            int position = 1;
            bool foundItems = false;
            SkipJsonWhitespace(json, ref position);
            if (position == json.Length || json[position] == '}')
            {
                return false;
            }

            while (position < json.Length)
            {
                if (!TryReadJsonString(json, ref position, out bool isItems))
                {
                    return false;
                }

                SkipJsonWhitespace(json, ref position);
                if (position == json.Length || json[position++] != ':')
                {
                    return false;
                }

                SkipJsonWhitespace(json, ref position);
                if (position == json.Length)
                {
                    return false;
                }

                if (isItems)
                {
                    // Duplicate root members are ambiguous across serializers; never use them as an empty save.
                    if (foundItems || json[position] != '[')
                    {
                        return false;
                    }

                    foundItems = true;
                }

                if (!TrySkipJsonValue(json, ref position))
                {
                    return false;
                }

                SkipJsonWhitespace(json, ref position);
                if (position == json.Length)
                {
                    return false;
                }

                char separator = json[position++];
                if (separator == '}')
                {
                    SkipJsonWhitespace(json, ref position);
                    return foundItems && position == json.Length;
                }

                if (separator != ',')
                {
                    return false;
                }

                SkipJsonWhitespace(json, ref position);
            }

            return false;
        }

        private static bool TrySkipJsonValue(string json, ref int position)
        {
            if (json[position] == '"')
            {
                return TryReadJsonString(json, ref position, out _);
            }

            if (json[position] != '{' && json[position] != '[')
            {
                int start = position;
                while (position < json.Length && !char.IsWhiteSpace(json[position]) &&
                       json[position] != ',' && json[position] != '}' && json[position] != ']')
                {
                    position++;
                }

                return position > start;
            }

            Stack<char> closingTokens = new Stack<char>();
            closingTokens.Push(json[position++] == '{' ? '}' : ']');
            while (position < json.Length && closingTokens.Count > 0)
            {
                char token = json[position];
                if (token == '"')
                {
                    if (!TryReadJsonString(json, ref position, out _))
                    {
                        return false;
                    }

                    continue;
                }

                position++;
                if (token == '{' || token == '[')
                {
                    closingTokens.Push(token == '{' ? '}' : ']');
                }
                else if ((token == '}' || token == ']') && closingTokens.Pop() != token)
                {
                    return false;
                }
            }

            return closingTokens.Count == 0;
        }

        private static bool TryReadJsonString(string json, ref int position, out bool isItems)
        {
            isItems = false;
            if (position == json.Length || json[position++] != '"')
            {
                return false;
            }

            const string requiredName = "items";
            int characterCount = 0;
            bool matches = true;
            while (position < json.Length)
            {
                char character = json[position++];
                if (character == '"')
                {
                    isItems = matches && characterCount == requiredName.Length;
                    return true;
                }

                if (character < ' ')
                {
                    return false;
                }

                if (character == '\\')
                {
                    if (position == json.Length)
                    {
                        return false;
                    }

                    character = json[position++];
                    switch (character)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            break;
                        case 'b':
                            character = '\b';
                            break;
                        case 'f':
                            character = '\f';
                            break;
                        case 'n':
                            character = '\n';
                            break;
                        case 'r':
                            character = '\r';
                            break;
                        case 't':
                            character = '\t';
                            break;
                        case 'u':
                            if (json.Length - position < 4 ||
                                !int.TryParse(json.Substring(position, 4), NumberStyles.AllowHexSpecifier,
                                    CultureInfo.InvariantCulture, out int codePoint))
                            {
                                return false;
                            }

                            character = (char)codePoint;
                            position += 4;
                            break;
                        default:
                            return false;
                    }
                }

                if (characterCount >= requiredName.Length || character != requiredName[characterCount])
                {
                    matches = false;
                }

                characterCount++;
            }

            return false;
        }

        private static void SkipJsonWhitespace(string json, ref int position)
        {
            while (position < json.Length && char.IsWhiteSpace(json[position]))
            {
                position++;
            }
        }

        private static bool CopyItemsToDictionary(SaveData saveData, ref Dictionary<string, SaveValue> data)
        {
            data = new Dictionary<string, SaveValue>();
            if (saveData?.items == null)
            {
                return false;
            }

            foreach (KeyValuePair item in saveData.items)
            {
                if (item == null || item.key == null || item.value == null || string.IsNullOrEmpty(item.value.type))
                {
                    return false;
                }

                data[item.key] = item.value;
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
            public List<KeyValuePair> items;
        }
    }
}
