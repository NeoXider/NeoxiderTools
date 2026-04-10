using System;
using System.Collections.Generic;
using Neo.Save;
using Neo.Settings;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

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

            // Reset GameSettings static state
            typeof(GameSettings).GetMethod("ResetStaticStateForTesting",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
        }

        private GameSettingsComponent CreateComponent()
        {
            var go = new GameObject("SettingsService");
            GameSettingsComponent svc = go.AddComponent<GameSettingsComponent>();
            Type type = typeof(GameSettingsComponent);
            type.GetField("_saveKeyPrefix",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(svc, "Neo.Settings.");
            type.GetField("_persistInput",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(svc, true);
            type.GetField("_defaultMouseSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(svc, 2f);

            type.GetMethod("Init", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(svc, null);
            return svc;
        }

        [Test]
        public void MouseSensitivity_Immediate_Persists()
        {
            GameSettingsComponent svc = CreateComponent();

            GameSettings.SetMouseSensitivity(4.25f, SettingsPersistMode.Immediate);

            string key = "Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity;
            Assert.That(SaveProvider.GetFloat(key, 0f), Is.EqualTo(4.25f).Within(0.001f));
            Object.DestroyImmediate(svc.gameObject);
        }

        [Test]
        public void MouseSensitivity_Deferred_FlushtesSave()
        {
            GameSettingsComponent svc = CreateComponent();

            GameSettings.SetMouseSensitivity(1.75f, SettingsPersistMode.Deferred);

            string key = "Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity;
            Assert.That(SaveProvider.HasKey(key), Is.False);

            GameSettings.FlushPendingSettingsSave();
            float v = SaveProvider.GetFloat(key, -1f);
            Assert.That(v, Is.EqualTo(1.75f).Within(0.001f));
            Object.DestroyImmediate(svc.gameObject);
        }

        [Test]
        public void LoadState_Restores_MouseSensitivity()
        {
            var go = new GameObject("SettingsService");
            GameSettingsComponent svc = go.AddComponent<GameSettingsComponent>();
            Type type = typeof(GameSettingsComponent);
            type.GetField("_saveKeyPrefix",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(svc, "Neo.Settings.");
            type.GetField("_persistInput",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(svc, true);
            type.GetField("_defaultMouseSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(svc, 2f);

            // Set the saved data BEFORE calling Init! Because Init calls LoadState!
            string key = "Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity;
            SaveProvider.SetFloat(key, 5.5f);

            type.GetMethod("Init", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(svc, null);

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

            public int GetInt(string key, int defaultValue = 0)
            {
                return _ints.TryGetValue(key, out int v) ? v : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _ints[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _floats.TryGetValue(key, out float v) ? v : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _floats[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _strings.TryGetValue(key, out string v) ? v : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _strings[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _bools.TryGetValue(key, out bool v) ? v : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _bools[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return _ints.ContainsKey(key) || _floats.ContainsKey(key) || _strings.ContainsKey(key) ||
                       _bools.ContainsKey(key);
            }

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
