using System;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    /// Статический класс для работы с системой сохранения данных.
    /// Предоставляет API, аналогичный PlayerPrefs, с поддержкой различных провайдеров сохранения.
    /// </summary>
    public static class SaveProvider
    {
        private const string SettingsResourcePath = "SaveProviderSettings";
        
        private static ISaveProvider _provider;
        private static SaveProviderSettings _settings;
        private static bool _isInitialized;
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// Событие, вызываемое после сохранения данных.
        /// </summary>
        public static event Action OnDataSaved;
        
        /// <summary>
        /// Событие, вызываемое после загрузки данных.
        /// </summary>
        public static event Action OnDataLoaded;
        
        /// <summary>
        /// Событие, вызываемое при изменении значения ключа.
        /// </summary>
        public static event Action<string> OnKeyChanged;
        
        /// <summary>
        /// Получает текущий провайдер сохранения.
        /// </summary>
        public static ISaveProvider CurrentProvider
        {
            get
            {
                Initialize();
                return _provider;
            }
        }
        
        /// <summary>
        /// Инициализирует систему сохранения при первом вызове.
        /// </summary>
        private static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    return;
                }
                
                _isInitialized = true;
                
                // Пытаемся загрузить настройки из Resources
                _settings = Resources.Load<SaveProviderSettings>(SettingsResourcePath);
                
                if (_settings != null)
                {
                    _provider = _settings.CreateProvider();
                    Debug.Log($"[SaveProvider] Initialized with {_provider.ProviderType} provider from Settings");
                }
                else
                {
                    // Используем PlayerPrefs по умолчанию
                    _provider = new PlayerPrefsSaveProvider();
                    Debug.Log("[SaveProvider] Initialized with default PlayerPrefs provider");
                }
                
                // Подписываемся на события провайдера
                _provider.OnDataSaved += () => OnDataSaved?.Invoke();
                _provider.OnDataLoaded += () => OnDataLoaded?.Invoke();
                _provider.OnKeyChanged += (key) => OnKeyChanged?.Invoke(key);
            }
        }
        
        /// <summary>
        /// Устанавливает провайдер сохранения вручную.
        /// </summary>
        /// <param name="provider">Провайдер для установки</param>
        public static void SetProvider(ISaveProvider provider)
        {
            if (provider == null)
            {
                Debug.LogError("[SaveProvider] Cannot set null provider");
                return;
            }
            
            lock (_lockObject)
            {
                // Отписываемся от событий старого провайдера
                if (_provider != null)
                {
                    _provider.OnDataSaved -= () => OnDataSaved?.Invoke();
                    _provider.OnDataLoaded -= () => OnDataLoaded?.Invoke();
                    _provider.OnKeyChanged -= (key) => OnKeyChanged?.Invoke(key);
                }
                
                _provider = provider;
                _isInitialized = true;
                
                // Подписываемся на события нового провайдера
                _provider.OnDataSaved += () => OnDataSaved?.Invoke();
                _provider.OnDataLoaded += () => OnDataLoaded?.Invoke();
                _provider.OnKeyChanged += (key) => OnKeyChanged?.Invoke(key);
                
                Debug.Log($"[SaveProvider] Provider set to {_provider.ProviderType}");
            }
        }
        
        /// <summary>
        /// Получает целочисленное значение по ключу.
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            Initialize();
            return _provider.GetInt(key, defaultValue);
        }
        
        /// <summary>
        /// Устанавливает целочисленное значение по ключу.
        /// </summary>
        public static void SetInt(string key, int value)
        {
            Initialize();
            _provider.SetInt(key, value);
        }
        
        /// <summary>
        /// Получает значение с плавающей точкой по ключу.
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            Initialize();
            return _provider.GetFloat(key, defaultValue);
        }
        
        /// <summary>
        /// Устанавливает значение с плавающей точкой по ключу.
        /// </summary>
        public static void SetFloat(string key, float value)
        {
            Initialize();
            _provider.SetFloat(key, value);
        }
        
        /// <summary>
        /// Получает строковое значение по ключу.
        /// </summary>
        public static string GetString(string key, string defaultValue = "")
        {
            Initialize();
            return _provider.GetString(key, defaultValue);
        }
        
        /// <summary>
        /// Устанавливает строковое значение по ключу.
        /// </summary>
        public static void SetString(string key, string value)
        {
            Initialize();
            _provider.SetString(key, value);
        }
        
        /// <summary>
        /// Получает булево значение по ключу.
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            Initialize();
            return _provider.GetBool(key, defaultValue);
        }
        
        /// <summary>
        /// Устанавливает булево значение по ключу.
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            Initialize();
            _provider.SetBool(key, value);
        }
        
        /// <summary>
        /// Проверяет, существует ли ключ в хранилище.
        /// </summary>
        public static bool HasKey(string key)
        {
            Initialize();
            return _provider.HasKey(key);
        }
        
        /// <summary>
        /// Удаляет ключ и его значение из хранилища.
        /// </summary>
        public static void DeleteKey(string key)
        {
            Initialize();
            _provider.DeleteKey(key);
        }
        
        /// <summary>
        /// Удаляет все ключи из хранилища.
        /// </summary>
        public static void DeleteAll()
        {
            Initialize();
            _provider.DeleteAll();
        }
        
        /// <summary>
        /// Принудительно сохраняет данные в хранилище.
        /// </summary>
        public static void Save()
        {
            Initialize();
            _provider.Save();
        }
        
        /// <summary>
        /// Принудительно загружает данные из хранилища.
        /// </summary>
        public static void Load()
        {
            Initialize();
            _provider.Load();
        }
    }
}







