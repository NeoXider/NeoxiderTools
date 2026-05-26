using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    public sealed class GameSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            ResetSettings();
            SaveProvider.SetProvider(new DictionarySaveProvider());
        }

        [TearDown]
        public void TearDown()
        {
            NeoDiagnostics.ResetStaticState();
            GameSettingsComponent[] all = Object.FindObjectsByType<GameSettingsComponent>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameSettingsComponent svc = all != null && all.Length > 0 ? all[0] : null;
            if (svc != null)
            {
                GameSettings.Detach(svc);
                Object.DestroyImmediate(svc.gameObject);
            }

            ResetSettings();
        }

        private GameSettingsComponent CreateComponent()
        {
            var go = new GameObject("SettingsService");
            GameSettingsComponent svc = go.AddComponent<GameSettingsComponent>();
            SetPrivate(svc, "_saveKeyPrefix", "Neo.Settings.");
            SetPrivate(svc, "_persistInput", true);
            SetPrivate(svc, "_defaultMouseSensitivity", 2f);

            InvokeInit(svc);
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
            SetPrivate(svc, "_saveKeyPrefix", "Neo.Settings.");
            SetPrivate(svc, "_persistInput", true);
            SetPrivate(svc, "_defaultMouseSensitivity", 2f);

            // Set the saved data BEFORE calling Init! Because Init calls LoadState!
            string key = "Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity;
            SaveProvider.SetFloat(key, 5.5f);

            InvokeInit(svc);

            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(5.5f).Within(0.001f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LoadState_WithoutComponent_KeepsStaticDefaults()
        {
            NeoDiagnostics.Configure(warnings: true);
            LogAssert.Expect(LogType.Warning, "[GameSettings] LoadState: no GameSettingsComponent attached.");

            Assert.DoesNotThrow(GameSettings.LoadState);

            Assert.That(GameSettings.Context, Is.Null);
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(2f));
            Assert.That(GameSettings.GraphicsPreset, Is.EqualTo(GraphicsPreset.High));
            Assert.That(GameSettings.FullScreen, Is.True);
            Assert.That(GameSettings.ResolutionAuto, Is.True);
            Assert.That(GameSettings.FramerateCap, Is.EqualTo(-1));
            Assert.That(GameSettings.VSync, Is.False);
        }

        [Test]
        public void LoadState_WithMissingSavedValues_UsesComponentDefaults()
        {
            GameSettingsComponent svc = CreateComponentWithDefaults(
                3.5f,
                GraphicsPreset.Low,
                0,
                false,
                true,
                1,
                120,
                true);

            try
            {
                Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(3.5f));
                Assert.That(GameSettings.GraphicsPreset, Is.EqualTo(GraphicsPreset.Low));
                Assert.That(GameSettings.FullScreen, Is.False);
                Assert.That(GameSettings.ResolutionAuto, Is.True);
                Assert.That(GameSettings.ResolutionIndex, Is.EqualTo(1));
                Assert.That(GameSettings.FramerateCap, Is.EqualTo(120));
                Assert.That(GameSettings.VSync, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(svc.gameObject);
            }
        }

        [Test]
        public void LoadState_WithInvalidSavedValues_ClampsOrFallsBackToDefaults()
        {
            NeoDiagnostics.Configure(warnings: true);
            const string prefix = "Neo.Settings.";
            SaveProvider.SetFloat(prefix + GameSettingsSaveKeys.MouseSensitivity, -10f);
            SaveProvider.SetInt(prefix + GameSettingsSaveKeys.GraphicsPreset, 999);
            SaveProvider.SetInt(prefix + GameSettingsSaveKeys.QualityLevel, 999);
            SaveProvider.SetInt(prefix + GameSettingsSaveKeys.ResolutionIndex, 99);
            LogAssert.Expect(LogType.Warning,
                $"[GameSettings] Quality level 999 clamped to [0,{Mathf.Max(0, QualitySettings.names.Length - 1)}].");
            LogAssert.Expect(LogType.Warning, "[GameSettings] Resolution index 99 clamped.");

            GameSettingsComponent svc = CreateComponentWithDefaults(
                2f,
                GraphicsPreset.Medium,
                0,
                true,
                true,
                0,
                -1,
                false);

            try
            {
                Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(0.01f));
                Assert.That(GameSettings.GraphicsPreset, Is.EqualTo(GraphicsPreset.Medium));
                Assert.That(GameSettings.QualityLevelIndex,
                    Is.InRange(0, Mathf.Max(0, QualitySettings.names.Length - 1)));
                Assert.That(GameSettings.ResolutionIndex, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(svc.gameObject);
            }
        }

        private static GameSettingsComponent CreateComponentWithDefaults(
            float mouseSensitivity,
            GraphicsPreset graphicsPreset,
            int qualityLevel,
            bool fullScreen,
            bool resolutionAuto,
            int resolutionIndex,
            int framerateCap,
            bool vSync)
        {
            var go = new GameObject("SettingsService");
            GameSettingsComponent svc = go.AddComponent<GameSettingsComponent>();
            SetPrivate(svc, "_saveKeyPrefix", "Neo.Settings.");
            SetPrivate(svc, "_defaultMouseSensitivity", mouseSensitivity);
            SetPrivate(svc, "_defaultGraphicsPreset", graphicsPreset);
            SetPrivate(svc, "_defaultCustomQualityLevel", qualityLevel);
            SetPrivate(svc, "_defaultFullScreen", fullScreen);
            SetPrivate(svc, "_defaultResolutionAuto", resolutionAuto);
            SetPrivate(svc, "_defaultResolutionIndex", resolutionIndex);
            SetPrivate(svc, "_defaultFramerateCap", framerateCap);
            SetPrivate(svc, "_defaultVSync", vSync);
            SetPrivate(svc, "_customResolutionPresets", new[]
            {
                new ResolutionPresetEntry { Width = 1280, Height = 720 },
                new ResolutionPresetEntry { Width = 1920, Height = 1080 }
            });
            InvokeInit(svc);
            return svc;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found.");
            field.SetValue(target, value);
        }

        private static void InvokeInit(GameSettingsComponent svc)
        {
            typeof(GameSettingsComponent)
                .GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(svc, null);
        }

        private static void ResetSettings()
        {
            typeof(GameSettings).GetMethod("ResetStaticStateForTesting",
                BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, bool> _bools = new();
            private readonly Dictionary<string, float> _floats = new();
            private readonly Dictionary<string, int> _ints = new();
            private readonly Dictionary<string, string> _strings = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;

            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

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
