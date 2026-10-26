using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Tools;

namespace Neo.Editor.Tests
{
    /// <summary>Isolates provider storage and preserves the exact pre-test trusted-clock cache.</summary>
    internal sealed class Audit20261023NeoToolsTimePersistenceScope : IDisposable
    {
        private readonly ISaveProvider _previousProvider;
        private readonly Dictionary<FieldInfo, object> _clockState = new();
        private bool _disposed;

        public Audit20261023NeoToolsTimePersistenceScope()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            // These fields are captured only to restore fixture isolation; behavior assertions use public APIs.
            string[] fields = { "_clock", "_restored", "_persistedHighWater" };
            foreach (string name in fields)
            {
                FieldInfo field = typeof(NeoTrustedTime).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                                  ?? throw new InvalidOperationException("Cannot preserve trusted clock field: " + name);
                _clockState.Add(field, field.GetValue(null));
            }

            Provider = new MemoryProvider();
            SaveProvider.SetProvider(Provider);
            NeoTrustedTime.ResetStaticState();
        }

        public ISaveProvider Provider { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                SaveProvider.SetProvider(_previousProvider);
            }
            finally
            {
                foreach (KeyValuePair<FieldInfo, object> entry in _clockState)
                {
                    entry.Key.SetValue(null, entry.Value);
                }
            }
        }

        private sealed class MemoryProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0) => Read(key, defaultValue);
            public float GetFloat(string key, float defaultValue = 0f) => Read(key, defaultValue);
            public string GetString(string key, string defaultValue = "") => Read(key, defaultValue);
            public bool GetBool(string key, bool defaultValue = false) => Read(key, defaultValue);
            public void SetInt(string key, int value) => Write(key, value);
            public void SetFloat(string key, float value) => Write(key, value);
            public void SetString(string key, string value) => Write(key, value);
            public void SetBool(string key, bool value) => Write(key, value);
            public bool HasKey(string key) => _values.ContainsKey(key);
            public void DeleteKey(string key) => _values.Remove(key);
            public void DeleteAll() => _values.Clear();
            public void Save() => OnDataSaved?.Invoke();
            public void Load() => OnDataLoaded?.Invoke();

            private T Read<T>(string key, T fallback)
            {
                return _values.TryGetValue(key, out object value) && value is T typed ? typed : fallback;
            }

            private void Write(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
