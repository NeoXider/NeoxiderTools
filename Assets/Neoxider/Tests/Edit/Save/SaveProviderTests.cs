using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Save.Tests
{
    public class SaveProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            ResetSaveProvider();
        }

        [TearDown]
        public void TearDown()
        {
            ResetSaveProvider();
        }

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

        [Test]
        public void MissingSaveProviderSettings_FallsBackToPlayerPrefsWithoutLogs()
        {
            SaveProvider.DebugLoggingEnabled = false;

            ISaveProvider provider = SaveProvider.CurrentProvider;

            Assert.That(provider, Is.TypeOf<PlayerPrefsSaveProvider>());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SettingsComponentWithoutSettings_UsesFallbackProvider()
        {
            GameObject go = new("SaveProviderSettingsComponentTests");
            SaveProvider.DebugLoggingEnabled = false;

            try
            {
                go.AddComponent<SaveProviderSettingsComponent>();

                Assert.That(SaveProvider.CurrentProvider, Is.TypeOf<PlayerPrefsSaveProvider>());
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeDiagnostics_AreSilentUntilDebugLoggingEnabled()
        {
            SaveProvider.DebugLoggingEnabled = false;
            SaveProvider.SetProvider(new FakeSaveProvider());

            SaveProvider.SetSlot("slot.json");
            LogAssert.NoUnexpectedReceived();

            SaveProvider.DebugLoggingEnabled = true;
            LogAssert.Expect(LogType.Warning,
                "[SaveProvider] SetSlot is not supported for provider type: PlayerPrefs");

            SaveProvider.SetSlot("slot.json");
        }

        private static void ResetSaveProvider()
        {
            typeof(SaveProvider)
                .GetMethod("ResetStaticState", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);
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
