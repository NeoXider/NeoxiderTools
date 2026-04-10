using System;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Provides a PlayerPrefs-like static API on top of the active save provider.
    /// </summary>
    public static class SaveProvider
    {
        private const string SettingsResourcePath = "SaveProviderSettings";

        private static ISaveProvider _provider;
        private static SaveProviderSettings _settings;
        private static bool _isInitialized;
        private static readonly object _lockObject = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _provider = null;
            _settings = null;
            _isInitialized = false;
        }

        /// <summary>
        ///     Gets the currently active save provider.
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
        ///     Raised after the active provider completes a save operation.
        /// </summary>
        public static event Action OnDataSaved;

        /// <summary>
        ///     Raised after the active provider completes a load operation.
        /// </summary>
        public static event Action OnDataLoaded;

        /// <summary>
        ///     Raised when a key changes in the active provider.
        /// </summary>
        public static event Action<string> OnKeyChanged;

        private static void HandleProviderDataSaved()
        {
            OnDataSaved?.Invoke();
        }

        private static void HandleProviderDataLoaded()
        {
            OnDataLoaded?.Invoke();
        }

        private static void HandleProviderKeyChanged(string key)
        {
            OnKeyChanged?.Invoke(key);
        }

        private static void AttachProviderEvents(ISaveProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            provider.OnDataSaved += HandleProviderDataSaved;
            provider.OnDataLoaded += HandleProviderDataLoaded;
            provider.OnKeyChanged += HandleProviderKeyChanged;
        }

        private static void DetachProviderEvents(ISaveProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            provider.OnDataSaved -= HandleProviderDataSaved;
            provider.OnDataLoaded -= HandleProviderDataLoaded;
            provider.OnKeyChanged -= HandleProviderKeyChanged;
        }

        /// <summary>
        ///     Initializes the save system on first use.
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

                // Try to load settings from Resources
                _settings = Resources.Load<SaveProviderSettings>(SettingsResourcePath);

                if (_settings != null)
                {
                    _provider = _settings.CreateProvider();
                    Debug.Log($"[SaveProvider] Initialized with {_provider.ProviderType} provider from Settings");
                }
                else
                {
                    // Use PlayerPrefs by default
                    _provider = new PlayerPrefsSaveProvider();
                    Debug.Log("[SaveProvider] Initialized with default PlayerPrefs provider");
                }

                AttachProviderEvents(_provider);
            }
        }

        /// <summary>
        ///     Replaces the active save provider.
        /// </summary>
        /// <param name="provider">Provider instance to activate.</param>
        public static void SetProvider(ISaveProvider provider)
        {
            if (provider == null)
            {
                Debug.LogError("[SaveProvider] Cannot set null provider");
                return;
            }

            lock (_lockObject)
            {
                DetachProviderEvents(_provider);

                _provider = provider;
                _isInitialized = true;

                AttachProviderEvents(_provider);

                Debug.Log($"[SaveProvider] Provider set to {_provider.ProviderType}");
            }
        }

        /// <summary>
        ///     Gets an integer value by key.
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            Initialize();
            return _provider.GetInt(key, defaultValue);
        }

        /// <summary>
        ///     Sets an integer value by key.
        /// </summary>
        public static void SetInt(string key, int value)
        {
            Initialize();
            _provider.SetInt(key, value);
        }

        /// <summary>
        ///     Gets a floating-point value by key.
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            Initialize();
            return _provider.GetFloat(key, defaultValue);
        }

        /// <summary>
        ///     Sets a floating-point value by key.
        /// </summary>
        public static void SetFloat(string key, float value)
        {
            Initialize();
            _provider.SetFloat(key, value);
        }

        /// <summary>
        ///     Gets a string value by key.
        /// </summary>
        public static string GetString(string key, string defaultValue = "")
        {
            Initialize();
            return _provider.GetString(key, defaultValue);
        }

        /// <summary>
        ///     Sets a string value by key.
        /// </summary>
        public static void SetString(string key, string value)
        {
            Initialize();
            _provider.SetString(key, value);
        }

        /// <summary>
        ///     Gets a Boolean value by key.
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            Initialize();
            return _provider.GetBool(key, defaultValue);
        }

        /// <summary>
        ///     Sets a Boolean value by key.
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            Initialize();
            _provider.SetBool(key, value);
        }

        /// <summary>
        ///     Returns whether the specified key exists in the active provider.
        /// </summary>
        public static bool HasKey(string key)
        {
            Initialize();
            return _provider.HasKey(key);
        }

        /// <summary>
        ///     Deletes a key from the active provider.
        /// </summary>
        public static void DeleteKey(string key)
        {
            Initialize();
            _provider.DeleteKey(key);
        }

        /// <summary>
        ///     Deletes all keys from the active provider.
        /// </summary>
        public static void DeleteAll()
        {
            Initialize();
            _provider.DeleteAll();
        }

        /// <summary>
        ///     Forces the active provider to persist its data.
        /// </summary>
        public static void Save()
        {
            Initialize();
            _provider.Save();
        }

        /// <summary>
        ///     Forces the active provider to refresh its data.
        /// </summary>
        public static void Load()
        {
            Initialize();
            _provider.Load();
        }

        /// <summary>
        ///     Switches the active save slot if the provider supports it (like FileSaveProvider).
        ///     This allows true multi-slot persistence.
        /// </summary>
        /// <param name="slotName">New slot name (e.g., "save2.json").</param>
        public static void SetSlot(string slotName)
        {
            Initialize();
            if (_provider is FileSaveProvider fsp)
            {
                fsp.ChangeSlot(slotName);
                Debug.Log($"[SaveProvider] Switched to slot: {slotName}");
            }
            else
            {
                Debug.LogWarning(
                    $"[SaveProvider] SetSlot is not supported for provider type: {_provider.ProviderType}");
            }
        }
    }
}
