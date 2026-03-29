using System.Collections.Generic;
using Neo.Save;
using Neo.Settings;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tools.Tests
{
    public sealed class GameSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveProvider.SetProvider(new DictionarySaveProvider());
        }

        [TearDown]
        public void TearDown()
        {
            GameSettingsComponent[] all = Object.FindObjectsByType<GameSettingsComponent>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameSettingsComponent svc = all != null && all.Length > 0 ? all[0] : null;
            if (svc != null)
            {
                GameSettings.Detach(svc);
                Object.DestroyImmediate(svc.gameObject);
            }
        }

        [Test]
        public void MouseSensitivity_Immediate_Persists()
        {
            var go = new GameObject("SettingsService");
            go.AddComponent<GameSettingsComponent>();

            GameSettings.SetMouseSensitivity(4.25f, SettingsPersistMode.Immediate);

            string key = "Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity;
            Assert.That(SaveProvider.GetFloat(key, 0f), Is.EqualTo(4.25f).Within(0.001f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MouseSensitivity_Deferred_FlushtesSave()
        {
            var go = new GameObject("SettingsService");
            go.AddComponent<GameSettingsComponent>();

            GameSettings.SetMouseSensitivity(1.75f, SettingsPersistMode.Deferred);
            string key = "Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity;
            Assert.That(SaveProvider.HasKey(key), Is.False);

            GameSettings.FlushPendingSettingsSave();
            Assert.That(SaveProvider.GetFloat(key, 0f), Is.EqualTo(1.75f).Within(0.001f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LoadState_Restores_MouseSensitivity()
        {
            var go = new GameObject("SettingsService");
            go.AddComponent<GameSettingsComponent>();
            string key = "Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity;
            SaveProvider.SetFloat(key, 5.5f);

            GameSettings.LoadState();
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(5.5f).Within(0.001f));
            Object.DestroyImmediate(go);
        }

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, bool> _bools = new();
            private readonly Dictionary<string, float> _floats = new();
            private readonly Dictionary<string, int> _ints = new();
            private readonly Dictionary<string, string> _strings = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;

            public event System.Action OnDataSaved;
            public event System.Action OnDataLoaded;
            public event System.Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0) =>
                _ints.TryGetValue(key, out int v) ? v : defaultValue;

            public void SetInt(string key, int value)
            {
                _ints[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f) =>
                _floats.TryGetValue(key, out float v) ? v : defaultValue;

            public void SetFloat(string key, float value)
            {
                _floats[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "") =>
                _strings.TryGetValue(key, out string v) ? v : defaultValue;

            public void SetString(string key, string value)
            {
                _strings[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false) =>
                _bools.TryGetValue(key, out bool v) ? v : defaultValue;

            public void SetBool(string key, bool value)
            {
                _bools[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key) =>
                _ints.ContainsKey(key) || _floats.ContainsKey(key) || _strings.ContainsKey(key) ||
                _bools.ContainsKey(key);

            public void DeleteKey(string key)
            {
                _ints.Remove(key);
                _floats.Remove(key);
                _strings.Remove(key);
                _bools.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll()
            {
                _ints.Clear();
                _floats.Clear();
                _strings.Clear();
                _bools.Clear();
            }

            public void Save()
            {
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }
        }
    }
}
