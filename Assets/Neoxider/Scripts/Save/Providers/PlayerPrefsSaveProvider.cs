using System;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    /// Реализация провайдера сохранения через PlayerPrefs Unity.
    /// </summary>
    public class PlayerPrefsSaveProvider : ISaveProvider
    {
        /// <summary>
        /// Тип провайдера - PlayerPrefs.
        /// </summary>
        public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
        
        /// <summary>
        /// Событие, вызываемое после сохранения данных.
        /// </summary>
        public event Action OnDataSaved;
        
        /// <summary>
        /// Событие, вызываемое после загрузки данных.
        /// </summary>
        public event Action OnDataLoaded;
        
        /// <summary>
        /// Событие, вызываемое при изменении значения ключа.
        /// </summary>
        public event Action<string> OnKeyChanged;
        
        private const string BoolPrefix = "Bool_";
        
        /// <summary>
        /// Получает целочисленное значение по ключу.
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }
        
        /// <summary>
        /// Устанавливает целочисленное значение по ключу.
        /// </summary>
        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            OnKeyChanged?.Invoke(key);
        }
        
        /// <summary>
        /// Получает значение с плавающей точкой по ключу.
        /// </summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }
        
        /// <summary>
        /// Устанавливает значение с плавающей точкой по ключу.
        /// </summary>
        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            OnKeyChanged?.Invoke(key);
        }
        
        /// <summary>
        /// Получает строковое значение по ключу.
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }
        
        /// <summary>
        /// Устанавливает строковое значение по ключу.
        /// </summary>
        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            OnKeyChanged?.Invoke(key);
        }
        
        /// <summary>
        /// Получает булево значение по ключу.
        /// Реализовано через int (0 = false, 1 = true).
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
        /// Устанавливает булево значение по ключу.
        /// Реализовано через int (0 = false, 1 = true).
        /// </summary>
        public void SetBool(string key, bool value)
        {
            string boolKey = BoolPrefix + key;
            PlayerPrefs.SetInt(boolKey, value ? 1 : 0);
            OnKeyChanged?.Invoke(key);
        }
        
        /// <summary>
        /// Проверяет, существует ли ключ в хранилище.
        /// </summary>
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key) || PlayerPrefs.HasKey(BoolPrefix + key);
        }
        
        /// <summary>
        /// Удаляет ключ и его значение из хранилища.
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
        /// Удаляет все ключи из хранилища.
        /// </summary>
        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
        
        /// <summary>
        /// Принудительно сохраняет данные в хранилище.
        /// Вызывает PlayerPrefs.Save() и событие OnDataSaved.
        /// </summary>
        public void Save()
        {
            PlayerPrefs.Save();
            OnDataSaved?.Invoke();
        }
        
        /// <summary>
        /// Принудительно загружает данные из хранилища.
        /// Для PlayerPrefs это пустая операция, так как данные загружаются автоматически.
        /// Вызывает событие OnDataLoaded.
        /// </summary>
        public void Load()
        {
            OnDataLoaded?.Invoke();
        }
    }
}







