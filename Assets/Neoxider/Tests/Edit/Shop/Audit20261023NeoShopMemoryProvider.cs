using System;
using System.Collections.Generic;
using Neo.Save;

namespace Neo.Editor.Tests
{
    internal sealed class Audit20261023NeoShopMemoryProvider : ISaveProvider
    {
        private readonly Dictionary<string, object> _values = new();

        public int StringWritesToFail { get; set; }
        public string FailingStringKey { get; set; } = "Shop";

        public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
        public event Action OnDataSaved;
        public event Action OnDataLoaded;
        public event Action<string> OnKeyChanged;

        public int GetInt(string key, int defaultValue = 0)
        {
            return _values.TryGetValue(key, out object value) && value is int number ? number : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _values[key] = value;
            OnKeyChanged?.Invoke(key);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _values.TryGetValue(key, out object value) && value is float number ? number : defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            _values[key] = value;
            OnKeyChanged?.Invoke(key);
        }

        public string GetString(string key, string defaultValue = "")
        {
            return _values.TryGetValue(key, out object value) && value is string text ? text : defaultValue;
        }

        public void SetString(string key, string value)
        {
            if (StringWritesToFail > 0 && key == FailingStringKey)
            {
                StringWritesToFail--;
                throw new InvalidOperationException("profile store rejected the write");
            }

            _values[key] = value;
            OnKeyChanged?.Invoke(key);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return _values.TryGetValue(key, out object value) && value is bool flag ? flag : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            _values[key] = value;
            OnKeyChanged?.Invoke(key);
        }

        public bool HasKey(string key) => _values.ContainsKey(key);

        public void DeleteKey(string key)
        {
            if (_values.Remove(key))
            {
                OnKeyChanged?.Invoke(key);
            }
        }

        public void DeleteAll() => _values.Clear();
        public void Save() => OnDataSaved?.Invoke();
        public void Load() => OnDataLoaded?.Invoke();
    }
}
