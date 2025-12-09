using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Реализация провайдера сохранения через JSON файл.
    ///     Все данные сохраняются в один файл в формате JSON.
    /// </summary>
    public class FileSaveProvider : ISaveProvider
    {
        private readonly string _filePath;
        private Dictionary<string, SaveValue> _data;
        private bool _isDirty;

        /// <summary>
        ///     Создает новый экземпляр FileSaveProvider.
        /// </summary>
        /// <param name="fileName">
        ///     Имя файла для сохранения (например, "save.json"). Будет сохранен в
        ///     Application.persistentDataPath.
        /// </param>
        public FileSaveProvider(string fileName = "save.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
            _data = new Dictionary<string, SaveValue>();
            Load();
        }

        /// <summary>
        ///     Тип провайдера - File.
        /// </summary>
        public SaveProviderType ProviderType => SaveProviderType.File;

        /// <summary>
        ///     Событие, вызываемое после сохранения данных.
        /// </summary>
        public event Action OnDataSaved;

        /// <summary>
        ///     Событие, вызываемое после загрузки данных.
        /// </summary>
        public event Action OnDataLoaded;

        /// <summary>
        ///     Событие, вызываемое при изменении значения ключа.
        /// </summary>
        public event Action<string> OnKeyChanged;

        /// <summary>
        ///     Получает целочисленное значение по ключу.
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
        ///     Устанавливает целочисленное значение по ключу.
        /// </summary>
        public void SetInt(string key, int value)
        {
            _data[key] = new SaveValue { type = "int", value = value.ToString() };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Получает значение с плавающей точкой по ключу.
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
        ///     Устанавливает значение с плавающей точкой по ключу.
        /// </summary>
        public void SetFloat(string key, float value)
        {
            _data[key] = new SaveValue { type = "float", value = value.ToString(CultureInfo.InvariantCulture) };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Получает строковое значение по ключу.
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
        ///     Устанавливает строковое значение по ключу.
        /// </summary>
        public void SetString(string key, string value)
        {
            _data[key] = new SaveValue { type = "string", value = value ?? "" };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Получает булево значение по ключу.
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
        ///     Устанавливает булево значение по ключу.
        /// </summary>
        public void SetBool(string key, bool value)
        {
            _data[key] = new SaveValue { type = "bool", value = value.ToString() };
            _isDirty = true;
            OnKeyChanged?.Invoke(key);
        }

        /// <summary>
        ///     Проверяет, существует ли ключ в хранилище.
        /// </summary>
        public bool HasKey(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        ///     Удаляет ключ и его значение из хранилища.
        /// </summary>
        public void DeleteKey(string key)
        {
            if (_data.Remove(key))
            {
                _isDirty = true;
            }
        }

        /// <summary>
        ///     Удаляет все ключи из хранилища.
        /// </summary>
        public void DeleteAll()
        {
            _data.Clear();
            _isDirty = true;
        }

        /// <summary>
        ///     Принудительно сохраняет данные в файл.
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
        ///     Принудительно загружает данные из файла.
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
        ///     Автоматически сохраняет данные при изменении, если включен автосохранение.
        ///     Вызывается при уничтожении объекта.
        /// </summary>
        ~FileSaveProvider()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        /// <summary>
        ///     Структура для хранения значения с типом.
        /// </summary>
        [Serializable]
        private class SaveValue
        {
            public string type;
            public string value;
        }

        /// <summary>
        ///     Пара ключ-значение для сериализации.
        /// </summary>
        [Serializable]
        private class KeyValuePair
        {
            public string key;
            public SaveValue value;
        }

        /// <summary>
        ///     Структура для сериализации всех данных в JSON.
        /// </summary>
        [Serializable]
        private class SaveData
        {
            public List<KeyValuePair> items = new();
        }
    }
}