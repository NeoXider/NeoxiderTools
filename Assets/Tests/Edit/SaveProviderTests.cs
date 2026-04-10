using System;
using NUnit.Framework;

namespace Neo.Save.Tests
{
    public class SaveProviderTests
    {
        [Test]
        public void SetProvider_DetachesOldProviderEvents()
        {
            FakeSaveProvider oldProvider = new();
            FakeSaveProvider newProvider = new();
            int savedCount = 0;
            int loadedCount = 0;
            string changedKey = null;

            void OnSaved()
            {
                savedCount++;
            }

            void OnLoaded()
            {
                loadedCount++;
            }

            void OnKeyChanged(string key)
            {
                changedKey = key;
            }

            SaveProvider.OnDataSaved += OnSaved;
            SaveProvider.OnDataLoaded += OnLoaded;
            SaveProvider.OnKeyChanged += OnKeyChanged;

            try
            {
                SaveProvider.SetProvider(oldProvider);
                oldProvider.RaiseSaved();
                oldProvider.RaiseLoaded();
                oldProvider.RaiseKeyChanged("old");

                SaveProvider.SetProvider(newProvider);
                oldProvider.RaiseSaved();
                oldProvider.RaiseLoaded();
                oldProvider.RaiseKeyChanged("stale");
                newProvider.RaiseSaved();
                newProvider.RaiseLoaded();
                newProvider.RaiseKeyChanged("fresh");

                Assert.That(savedCount, Is.EqualTo(2));
                Assert.That(loadedCount, Is.EqualTo(2));
                Assert.That(changedKey, Is.EqualTo("fresh"));
            }
            finally
            {
                SaveProvider.OnDataSaved -= OnSaved;
                SaveProvider.OnDataLoaded -= OnLoaded;
                SaveProvider.OnKeyChanged -= OnKeyChanged;
            }
        }

        private sealed class FakeSaveProvider : ISaveProvider
        {
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return defaultValue;
            }

            public void SetInt(string key, int value)
            {
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return defaultValue;
            }

            public void SetFloat(string key, float value)
            {
            }

            public string GetString(string key, string defaultValue = "")
            {
                return defaultValue;
            }

            public void SetString(string key, string value)
            {
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return defaultValue;
            }

            public void SetBool(string key, bool value)
            {
            }

            public bool HasKey(string key)
            {
                return false;
            }

            public void DeleteKey(string key)
            {
            }

            public void DeleteAll()
            {
            }

            public void Save()
            {
            }

            public void Load()
            {
            }

            public void RaiseSaved()
            {
                OnDataSaved?.Invoke();
            }

            public void RaiseLoaded()
            {
                OnDataLoaded?.Invoke();
            }

            public void RaiseKeyChanged(string key)
            {
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
