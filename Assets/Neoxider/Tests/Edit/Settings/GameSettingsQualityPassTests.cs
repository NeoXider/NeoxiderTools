using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Editor.Tests;
using Neo.Save;
using Neo.Settings;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    /// <summary>
    ///     Cached save keys must follow a changed prefix, and Deferred persistence must keep one pending debounce
    ///     instead of restarting a coroutine per change, yet replace a pending coroutine that was stopped from outside.
    /// </summary>
    public sealed class GameSettingsQualityPassTests
    {
        private const string FirstPrefix = "Neo.Settings.QualityPass.A.";
        private const string SecondPrefix = "Neo.Settings.QualityPass.B.";

        private readonly List<GameObject> _objects = new();
        private Audit20261023NeoEditorTestsSaveScope _saveScope;
        private int _quality;
        private int _vSync;
        private int _framerate;

        [SetUp]
        public void SetUp()
        {
            _quality = QualitySettings.GetQualityLevel();
            _vSync = QualitySettings.vSyncCount;
            _framerate = Application.targetFrameRate;
            _saveScope = new Audit20261023NeoEditorTestsSaveScope();
            ResetSettings();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        GameSettings.Detach(_objects[i].GetComponent<GameSettingsComponent>());
                        Object.DestroyImmediate(_objects[i]);
                    }
                }

                _objects.Clear();
                ResetSettings();
            }
            finally
            {
                QualitySettings.SetQualityLevel(_quality, true);
                QualitySettings.vSyncCount = _vSync;
                Application.targetFrameRate = _framerate;
                _saveScope?.Dispose();
                _saveScope = null;
            }
        }

        [Test]
        public void SaveState_AfterPrefixChange_WritesUnderTheNewPrefix()
        {
            GameSettingsComponent service = CreateService(FirstPrefix);
            GameSettings.SetMouseSensitivity(3f, SettingsPersistMode.Immediate);
            Assert.That(SaveProvider.GetFloat(FirstPrefix + GameSettingsSaveKeys.MouseSensitivity, -1f),
                Is.EqualTo(3f).Within(0.0001f));

            SetField(service, "_saveKeyPrefix", SecondPrefix);
            GameSettings.SetMouseSensitivity(4f, SettingsPersistMode.Immediate);

            Assert.That(SaveProvider.GetFloat(SecondPrefix + GameSettingsSaveKeys.MouseSensitivity, -1f),
                Is.EqualTo(4f).Within(0.0001f));
            Assert.That(SaveProvider.GetFloat(FirstPrefix + GameSettingsSaveKeys.MouseSensitivity, -1f),
                Is.EqualTo(3f).Within(0.0001f), "the old prefix must not receive the new value");
        }

        [Test]
        public void DeferredChanges_KeepOnePendingDebounce_UntilFlushed()
        {
            GameSettingsComponent service = CreateService(FirstPrefix);
            GameSettings.SetMouseSensitivity(1.5f, SettingsPersistMode.Deferred);
            Assume.That(service.HasPendingDeferredSave, Is.True,
                "the host must start coroutines for this edit-mode check");
            object firstHandle = GetField(service, "_deferredCoroutine");

            GameSettings.SetMouseSensitivity(2.5f, SettingsPersistMode.Deferred);
            GameSettings.SetMouseSensitivity(3.5f, SettingsPersistMode.Deferred);

            Assert.That(GetField(service, "_deferredCoroutine"), Is.SameAs(firstHandle),
                "rescheduling must move the deadline, not restart the coroutine");
            Assert.That(SaveProvider.HasKey(FirstPrefix + GameSettingsSaveKeys.MouseSensitivity), Is.False);

            GameSettings.FlushPendingSettingsSave();

            Assert.That(service.HasPendingDeferredSave, Is.False);
            Assert.That(SaveProvider.GetFloat(FirstPrefix + GameSettingsSaveKeys.MouseSensitivity, -1f),
                Is.EqualTo(3.5f).Within(0.0001f));
        }

        [Test]
        public void DeferredChange_AfterPendingCoroutineWasStoppedFromOutside_StartsANewOne()
        {
            GameSettingsComponent service = CreateService(FirstPrefix);
            GameSettings.SetMouseSensitivity(1.5f, SettingsPersistMode.Deferred);
            Assume.That(service.HasPendingDeferredSave, Is.True,
                "the host must start coroutines for this edit-mode check");
            object deadHandle = GetField(service, "_deferredCoroutine");

            service.StopAllCoroutines();
            // WHY: edit mode does not advance frames inside one test; age the last stamp the way the frames after
            // StopAllCoroutines would, since a stopped coroutine no longer stamps.
            SetField(service, "_deferredTickFrame", Time.frameCount - 2);

            GameSettings.SetMouseSensitivity(2.5f, SettingsPersistMode.Deferred);

            object newHandle = GetField(service, "_deferredCoroutine");
            Assert.That(newHandle, Is.Not.Null);
            Assert.That(newHandle, Is.Not.SameAs(deadHandle),
                "a change after the pending coroutine died must start a new one instead of only moving the deadline");
            Assert.That(GetField(service, "_deferredTickFrame"), Is.EqualTo(Time.frameCount),
                "the new coroutine stamps the frame it started in");

            GameSettings.FlushPendingSettingsSave();

            Assert.That(service.HasPendingDeferredSave, Is.False);
            Assert.That(SaveProvider.GetFloat(FirstPrefix + GameSettingsSaveKeys.MouseSensitivity, -1f),
                Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void SaveState_WithCachedPrefixedKeys_AllocatesNothingWhenWarm()
        {
            CreateService(FirstPrefix);
            TypedMemorySaveProvider provider = new();
            SaveProvider.SetProvider(provider);
            GameSettings.SaveState();
            Assert.That(provider.KeyCount, Is.EqualTo(9), "every enabled group writes its keys");
            Assert.That(provider.HasKey(FirstPrefix + GameSettingsSaveKeys.VSync), Is.True);

            GcAlloc.AssertNone(() =>
            {
                for (int i = 0; i < 64; i++)
                {
                    GameSettings.SaveState();
                }
            }, "SaveState must reuse the prefixed key strings instead of concatenating them per write.");
        }

        [Test]
        public void QualityAndPresetChanges_ToTheActiveLevel_AllocateNothingWhenWarm()
        {
            CreateService(FirstPrefix);
            int level = QualitySettings.GetQualityLevel();
            GameSettings.SetQualityLevel(level, SettingsPersistMode.SkipUntilFlush);
            GraphicsPreset preset = GameSettings.GraphicsPreset;
            GameSettings.SetGraphicsPreset(preset, SettingsPersistMode.SkipUntilFlush);
            Assume.That(QualitySettings.GetQualityLevel(), Is.EqualTo(level),
                "the preset of the active level must map back to the same level");
            Assert.That(GameSettings.QualityLevelIndex, Is.EqualTo(level));

            GcAlloc.AssertNone(() =>
            {
                for (int i = 0; i < 64; i++)
                {
                    GameSettings.SetQualityLevel(level, SettingsPersistMode.SkipUntilFlush);
                    GameSettings.SetGraphicsPreset(preset, SettingsPersistMode.SkipUntilFlush);
                }
            }, "A quality or preset change to the active level must not read QualitySettings.names or box the preset.");
        }

        [Test]
        public void KnownEnumChecks_AcceptEveryDefinedValueAndRejectUndefinedOnes()
        {
            Func<GraphicsPreset, bool> isKnownPreset = CreateStaticPredicate<GraphicsPreset>("IsKnownPreset");
            Func<FullScreenMode, bool> isKnownMode = CreateStaticPredicate<FullScreenMode>("IsKnownFullScreenMode");

            // WHY: the checks list the members by hand to avoid boxing Enum.IsDefined; a new member must not be
            // silently treated as invalid by SetGraphicsPreset or reset to the default by LoadState.
            foreach (GraphicsPreset preset in Enum.GetValues(typeof(GraphicsPreset)))
            {
                Assert.That(isKnownPreset(preset), Is.True, "GraphicsPreset." + preset);
            }

            foreach (FullScreenMode mode in Enum.GetValues(typeof(FullScreenMode)))
            {
                Assert.That(isKnownMode(mode), Is.True, "FullScreenMode." + mode);
            }

            Assert.That(isKnownPreset((GraphicsPreset)99), Is.False);
            Assert.That(isKnownPreset((GraphicsPreset)(-1)), Is.False);
            Assert.That(isKnownMode((FullScreenMode)99), Is.False);
            Assert.That(isKnownMode((FullScreenMode)(-1)), Is.False);
        }

        private GameSettingsComponent CreateService(string prefix)
        {
            GameObject host = new GameObject("GameSettingsQualityPassTests.Service");
            _objects.Add(host);
            GameSettingsComponent service = host.AddComponent<GameSettingsComponent>();
            SetField(service, "_setInstanceOnAwake", false);
            SetField(service, "_saveKeyPrefix", prefix);
            MethodInfo init = typeof(GameSettingsComponent).GetMethod("Init",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(init, Is.Not.Null);
            init.Invoke(service, null);
            Assert.That(GameSettings.Context, Is.SameAs(service));
            return service;
        }

        private static Func<T, bool> CreateStaticPredicate<T>(string name)
        {
            MethodInfo method = typeof(GameSettings).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return (Func<T, bool>)Delegate.CreateDelegate(typeof(Func<T, bool>), method);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = FindField(target.GetType(), name);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string name)
        {
            FieldInfo field = FindField(target.GetType(), name);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(target);
        }

        private static FieldInfo FindField(System.Type type, string name)
        {
            for (System.Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static void ResetSettings()
        {
            typeof(GameSettings).GetMethod("ResetStaticStateForTesting", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);
        }

        /// <summary>Stores each value type in its own dictionary, so an overwrite neither boxes nor allocates.</summary>
        private sealed class TypedMemorySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, int> _ints = new(StringComparer.Ordinal);
            private readonly Dictionary<string, float> _floats = new(StringComparer.Ordinal);
            private readonly Dictionary<string, string> _strings = new(StringComparer.Ordinal);
            private readonly Dictionary<string, bool> _bools = new(StringComparer.Ordinal);

            public int KeyCount => _ints.Count + _floats.Count + _strings.Count + _bools.Count;
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _ints.TryGetValue(key, out int value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _ints[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _floats.TryGetValue(key, out float value) ? value : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _floats[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _strings.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _strings[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _bools.TryGetValue(key, out bool value) ? value : defaultValue;
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
