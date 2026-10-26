using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoSettingsTests
    {
        private const string Prefix = "Audit20261023NeoSettings.";
        private readonly List<GameObject> _objects = new();
        private readonly Dictionary<FieldInfo, object> _previousSettings = new();
        private ISaveProvider _previousProvider;
        private MemoryProvider _provider;
        private int _quality;
        private int _vSync;
        private int _framerate;
        private bool _fullScreen;
        private FullScreenMode _fullScreenMode;
        private bool _warningsEnabled;
        private bool _scopeCaptured;
        private bool _ownsPlayMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (RequiresNativeLifecycle())
            {
                yield return new EnterPlayMode();
                _ownsPlayMode = true;
                Assert.That(Application.isPlaying, Is.True);
            }

            Assert.That(GameSettings.Context, Is.Null,
                "Settings regressions require an isolated host without a borrowed settings service.");
            foreach (FieldInfo field in typeof(GameSettings).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (!field.IsLiteral && !field.IsInitOnly)
                {
                    _previousSettings.Add(field, field.GetValue(null));
                }
            }

            _previousProvider = SaveProvider.CurrentProvider;
            _quality = QualitySettings.GetQualityLevel();
            _vSync = QualitySettings.vSyncCount;
            _framerate = Application.targetFrameRate;
            _fullScreen = Screen.fullScreen;
            _fullScreenMode = Screen.fullScreenMode;
            _warningsEnabled = NeoDiagnostics.RuntimeWarningsEnabled;
            _scopeCaptured = true;
            _provider = new MemoryProvider();
            SaveProvider.SetProvider(_provider);
            NeoDiagnostics.Configure(warnings: false);
            ResetSettings();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scopeCaptured)
            {
                try
                {
                    for (int i = _objects.Count - 1; i >= 0; i--)
                    {
                        if (_objects[i] != null)
                        {
                            Object.DestroyImmediate(_objects[i]);
                        }
                    }
                }
                finally
                {
                    _objects.Clear();
                    foreach (KeyValuePair<FieldInfo, object> previous in _previousSettings)
                    {
                        previous.Key.SetValue(null, previous.Value);
                    }

                    _previousSettings.Clear();
                    _scopeCaptured = false;
                    try
                    {
                        SaveProvider.SetProvider(_previousProvider);
                    }
                    finally
                    {
                        if (QualitySettings.names.Length > 0)
                        {
                            QualitySettings.SetQualityLevel(_quality, true);
                        }

                        QualitySettings.vSyncCount = _vSync;
                        Application.targetFrameRate = _framerate;
                        Screen.fullScreenMode = _fullScreenMode;
                        Screen.fullScreen = _fullScreen;
                        NeoDiagnostics.Configure(warnings: _warningsEnabled);
                    }
                }
            }

            if (_ownsPlayMode)
            {
                _ownsPlayMode = false;
                yield return new ExitPlayMode();
            }
        }

        [Test]
        public void MouseSensitivity_NaN_PreservesCurrentValueAndPersistsFiniteValue()
        {
            CreateService();
            GameSettings.SetMouseSensitivity(3.25f, SettingsPersistMode.SkipUntilFlush);

            GameSettings.SetMouseSensitivity(float.NaN);

            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(3.25f));
            Assert.That(_provider.GetFloat(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.EqualTo(3.25f));
        }

        [Test]
        public void LoadState_NaN_UsesConfiguredSensitivityDefault()
        {
            GameSettingsComponent service = CreateService();
            SetField(service, "_defaultMouseSensitivity", 4.25f);
            _provider.SetFloat(Prefix + GameSettingsSaveKeys.MouseSensitivity, float.NaN);

            GameSettings.LoadState();
            GameSettings.SaveState();

            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(4.25f));
            Assert.That(_provider.GetFloat(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.EqualTo(4.25f));
        }

        [TestCase(-10f, 0.01f)]
        [TestCase(100f, 50f)]
        [TestCase(float.NaN, 2f)]
        public void ResetInput_ValidatesSerializedDefault(float configured, float expected)
        {
            GameSettingsComponent service = CreateService();
            SetField(service, "_defaultMouseSensitivity", configured);

            GameSettings.ResetGroup(SettingsGroup.Input);

            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(expected));
            Assert.That(_provider.GetFloat(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LoadState_InvalidSavedOrDefaultFramerate_UsesUnlimited(bool saved)
        {
            GameSettingsComponent service = CreateService();
            if (saved)
            {
                _provider.SetInt(Prefix + GameSettingsSaveKeys.FramerateCap, -20);
            }
            else
            {
                SetField(service, "_defaultFramerateCap", -20);
            }

            GameSettings.LoadState();

            Assert.That(GameSettings.FramerateCap, Is.EqualTo(-1));
            Assert.That(Application.targetFrameRate, Is.EqualTo(-1));
        }

        [Test]
        public void ResetPerformance_ClampsSerializedFramerateDefault()
        {
            GameSettingsComponent service = CreateService();
            SetField(service, "_defaultFramerateCap", -20);

            GameSettings.ResetGroup(SettingsGroup.Performance);

            Assert.That(GameSettings.FramerateCap, Is.EqualTo(-1));
            Assert.That(_provider.GetInt(Prefix + GameSettingsSaveKeys.FramerateCap), Is.EqualTo(-1));
        }

        [Test]
        public void LoadState_InvalidSavedAndDefaultEnums_UsesValidFallbacks()
        {
            GameSettingsComponent service = CreateService();
            SetField(service, "_defaultGraphicsPreset", (GraphicsPreset)999);
            SetField(service, "_defaultFullScreenMode", (FullScreenMode)999);
            _provider.SetInt(Prefix + GameSettingsSaveKeys.GraphicsPreset, 999);
            _provider.SetInt(Prefix + GameSettingsSaveKeys.FullScreenMode, 999);

            GameSettings.LoadState();
            GameSettings.SaveState();

            Assert.That(GameSettings.GraphicsPreset, Is.EqualTo(GraphicsPreset.High));
            Assert.That(GameSettings.FullScreenModeValue, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(_provider.GetInt(Prefix + GameSettingsSaveKeys.GraphicsPreset),
                Is.EqualTo((int)GraphicsPreset.High));
            Assert.That(_provider.GetInt(Prefix + GameSettingsSaveKeys.FullScreenMode),
                Is.EqualTo((int)FullScreenMode.FullScreenWindow));
        }

        [Test]
        public void InvalidGraphicsPresetMutation_DoesNotChangeStateNotifyOrPersist()
        {
            CreateService();
            int changes = 0;
            GameSettings.OnSettingsChanged += () => changes++;

            GameSettings.SetGraphicsPreset((GraphicsPreset)999);

            Assert.That(GameSettings.GraphicsPreset, Is.EqualTo(GraphicsPreset.High));
            Assert.That(changes, Is.Zero);
            Assert.That(_provider.HasKey(Prefix + GameSettingsSaveKeys.GraphicsPreset), Is.False);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void QualityChange_WithOppositeProfileVSync_RestoresRequestBeforeNotificationAndSave(
            bool useNamedPreset, bool enabled)
        {
            if (QualitySettings.names.Length < 2)
            {
                Assert.Ignore("This acceptance requires two native quality levels.");
            }

            GameSettingsComponent service = CreateService();
            int sourceQuality = QualitySettings.GetQualityLevel();
            int targetQuality = (sourceQuality + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(targetQuality, true);
            int previousTargetVSync = QualitySettings.vSyncCount;
            Action observer = null;
            try
            {
                QualitySettings.vSyncCount = enabled ? 0 : 1;
                QualitySettings.SetQualityLevel(sourceQuality, true);
                GameSettings.SetVSync(enabled, SettingsPersistMode.SkipUntilFlush);
                SetField(service, "_presetToQuality", new[]
                {
                    new GraphicsPresetLevelMapping
                    {
                        Preset = GraphicsPreset.High, QualityLevelIndex = targetQuality
                    }
                });

                int notifications = 0;
                int notifiedEngineVSync = -1;
                bool notifiedRequestedVSync = !enabled;
                bool savedBeforeNotification = false;
                observer = () =>
                {
                    notifications++;
                    notifiedEngineVSync = QualitySettings.vSyncCount;
                    notifiedRequestedVSync = GameSettings.VSync;
                    savedBeforeNotification = _provider.HasKey(Prefix + GameSettingsSaveKeys.VSync);
                };
                GameSettings.OnSettingsChanged += observer;

                if (useNamedPreset)
                {
                    GameSettings.SetGraphicsPreset(GraphicsPreset.High);
                }
                else
                {
                    GameSettings.SetQualityLevel(targetQuality);
                }

                Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(targetQuality));
                Assert.That(GameSettings.QualityLevelIndex, Is.EqualTo(targetQuality));
                Assert.That(GameSettings.GraphicsPreset, Is.EqualTo(GraphicsPreset.High));
                Assert.That(GameSettings.VSync, Is.EqualTo(enabled));
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(enabled ? 1 : 0));
                Assert.That(notifications, Is.EqualTo(1));
                Assert.That(notifiedEngineVSync, Is.EqualTo(enabled ? 1 : 0));
                Assert.That(notifiedRequestedVSync, Is.EqualTo(enabled));
                Assert.That(savedBeforeNotification, Is.False);
                Assert.That(_provider.HasKey(Prefix + GameSettingsSaveKeys.VSync), Is.True);
                Assert.That(_provider.GetBool(Prefix + GameSettingsSaveKeys.VSync), Is.EqualTo(enabled));
                Assert.That(_provider.GetInt(Prefix + GameSettingsSaveKeys.QualityLevel), Is.EqualTo(targetQuality));
            }
            finally
            {
                GameSettings.OnSettingsChanged -= observer;
                QualitySettings.SetQualityLevel(targetQuality, true);
                QualitySettings.vSyncCount = previousTargetVSync;
                QualitySettings.SetQualityLevel(sourceQuality, true);
                QualitySettings.vSyncCount = _vSync;
            }
        }

        [Test]
        public void InvalidFullScreenModeMutation_DoesNotChangeStateNotifyOrPersist()
        {
            CreateService();
            int changes = 0;
            GameSettings.OnSettingsChanged += () => changes++;

            GameSettings.SetFullScreenMode((FullScreenMode)999);

            Assert.That(GameSettings.FullScreenModeValue, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(changes, Is.Zero);
            Assert.That(_provider.HasKey(Prefix + GameSettingsSaveKeys.FullScreenMode), Is.False);
        }

        [TestCase(false, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed)]
        [TestCase(true, FullScreenMode.Windowed, FullScreenMode.FullScreenWindow)]
        [TestCase(true, FullScreenMode.ExclusiveFullScreen, FullScreenMode.ExclusiveFullScreen)]
        public void LoadState_NormalizesFullScreenFlagAndMode(bool fullScreen, FullScreenMode stored,
            FullScreenMode expected)
        {
            CreateService();
            _provider.SetBool(Prefix + GameSettingsSaveKeys.FullScreen, fullScreen);
            _provider.SetInt(Prefix + GameSettingsSaveKeys.FullScreenMode, (int)stored);

            GameSettings.LoadState();

            Assert.That(GameSettings.FullScreen, Is.EqualTo(fullScreen));
            Assert.That(GameSettings.FullScreenModeValue, Is.EqualTo(expected));
        }

        [Test]
        public void WindowedToggle_ThenManualResolution_PersistsConsistentDisplayState()
        {
            CreateService();
            GameSettings.LoadState();

            GameSettings.SetFullScreen(false);
            GameSettings.SetResolutionIndex(0);

            Assert.That(GameSettings.FullScreen, Is.False);
            Assert.That(GameSettings.FullScreenModeValue, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(GameSettings.ResolutionAuto, Is.False);
            Assert.That(_provider.GetBool(Prefix + GameSettingsSaveKeys.FullScreen, true), Is.False);
            Assert.That(_provider.GetInt(Prefix + GameSettingsSaveKeys.FullScreenMode),
                Is.EqualTo((int)FullScreenMode.Windowed));
        }

        [Test]
        public void FullScreenToggle_AfterExplicitWindowedMode_SelectsFullscreenMode()
        {
            CreateService();
            GameSettings.SetFullScreenMode(FullScreenMode.Windowed, SettingsPersistMode.SkipUntilFlush);

            GameSettings.SetFullScreen(true);

            Assert.That(GameSettings.FullScreen, Is.True);
            Assert.That(GameSettings.FullScreenModeValue, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(_provider.GetInt(Prefix + GameSettingsSaveKeys.FullScreenMode),
                Is.EqualTo((int)FullScreenMode.FullScreenWindow));
        }

        [Test]
        public void ResetDisplay_NormalizesConflictingSerializedDefaults()
        {
            GameSettingsComponent service = CreateService();
            SetField(service, "_defaultFullScreen", false);
            SetField(service, "_defaultFullScreenMode", FullScreenMode.FullScreenWindow);

            GameSettings.ResetGroup(SettingsGroup.Display);

            Assert.That(GameSettings.FullScreen, Is.False);
            Assert.That(GameSettings.FullScreenModeValue, Is.EqualTo(FullScreenMode.Windowed));
        }

        [Test]
        public void Deferred_WithInactiveService_PersistsInsteadOfDroppingRequest()
        {
            CreateService();

            GameSettings.SetMouseSensitivity(6.25f, SettingsPersistMode.Deferred);

            Assert.That(_provider.GetFloat(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.EqualTo(6.25f));
        }

        [Test]
        public void Deferred_DisablingActiveService_FlushesPendingValue()
        {
            GameSettingsComponent service = CreateService();
            ActivateService(service);
            GameSettings.SetMouseSensitivity(6.25f, SettingsPersistMode.Deferred);
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(6.25f));
            Assert.That(_provider.HasKey(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.False);

            service.gameObject.SetActive(false);

            Assert.That(service.isActiveAndEnabled, Is.False);
            Assert.That(_provider.HasKey(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.True);
            Assert.That(_provider.GetFloat(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.EqualTo(6.25f));
        }

        [Test]
        public void DisablingFormerService_DoesNotSaveReplacementContext()
        {
            GameSettingsComponent former = CreateService();
            ActivateService(former);
            GameSettings.SetMouseSensitivity(6.25f, SettingsPersistMode.Deferred);
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(6.25f));
            Assert.That(_provider.HasKey(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.False);
            GameSettingsComponent replacement = CreateService("Audit20261023NeoSettings.Replacement.");
            Assert.That(GameSettings.Context, Is.SameAs(replacement));
            GameSettings.SetMouseSensitivity(7f, SettingsPersistMode.SkipUntilFlush);

            former.gameObject.SetActive(false);

            Assert.That(_provider.HasKey("Audit20261023NeoSettings.Replacement." +
                GameSettingsSaveKeys.MouseSensitivity), Is.False);
            Assert.That(_provider.HasKey(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.False);
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(7f));
        }

        [Test]
        public void ActiveView_TracksApiChangesWithoutUiCallbacksOrRebuildingOptions()
        {
            ActivateService(CreateService());
            SettingsView view = CreateView(out Slider slider, out Dropdown presets);
            view.gameObject.SetActive(true);
            AssertActiveView(view, slider, presets);
            Dropdown.OptionData firstOption = presets.options[0];
            int uiChanges = 0;
            slider.onValueChanged.AddListener(value => uiChanges++);

            GameSettings.SetMouseSensitivity(4.5f, SettingsPersistMode.SkipUntilFlush);

            Assert.That(slider.value, Is.EqualTo(4.5f));
            Assert.That(uiChanges, Is.Zero);
            Assert.That(presets.options[0], Is.SameAs(firstOption));
        }

        [Test]
        public void QualityControlChange_RefreshesNamedPresetControl()
        {
            ActivateService(CreateService());
            SettingsView view = CreateView(out Slider slider, out Dropdown presets);
            Dropdown quality = CreateObject("Quality", typeof(RectTransform)).AddComponent<Dropdown>();
            SetField(view, "_qualityDropdown", quality);
            view.gameObject.SetActive(true);
            AssertActiveView(view, slider, presets);
            Assert.That(QualitySettings.names, Is.Not.Empty);
            Assert.That(quality.options.Count, Is.EqualTo(QualitySettings.names.Length));
            Assert.That(quality.options[0].text, Is.EqualTo(QualitySettings.names[0]));

            quality.onValueChanged.Invoke(0);

            Assert.That(GameSettings.QualityLevelIndex, Is.Zero);
            Assert.That(GameSettings.GraphicsPreset, Is.EqualTo(GraphicsPreset.Minimal));
            Assert.That(presets.value, Is.EqualTo((int)GraphicsPreset.Minimal));
        }

        [Test]
        public void DisabledView_ReleasesControlsAndRebindsOnceWithoutRemovingExternalListeners()
        {
            ActivateService(CreateService());
            SettingsView view = CreateView(out Slider slider, out Dropdown presets);
            view.gameObject.SetActive(true);
            AssertActiveView(view, slider, presets);
            int externalChanges = 0;
            slider.onValueChanged.AddListener(value => externalChanges++);
            view.gameObject.SetActive(false);
            Assert.That(view.isActiveAndEnabled, Is.False);
            Assert.That(slider.isActiveAndEnabled, Is.True,
                "The independently active control must still be able to emit its event.");

            slider.value = 8f;

            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(2f));
            Assert.That(_provider.GetFloat(Prefix + GameSettingsSaveKeys.MouseSensitivity), Is.EqualTo(2f));
            Assert.That(externalChanges, Is.EqualTo(1));

            view.gameObject.SetActive(true);
            AssertActiveView(view, slider, presets);
            view.BindAll();
            int settingsChanges = 0;
            GameSettings.OnSettingsChanged += () => settingsChanges++;
            slider.value = 5f;

            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(5f));
            Assert.That(settingsChanges, Is.EqualTo(1));
            Assert.That(externalChanges, Is.EqualTo(2));
        }

        [Test]
        public void NullFrameratePresetArray_ShowsEmptyOptionsAndStillRefreshesOtherControls()
        {
            GameSettingsComponent service = CreateService();
            SetField(service, "_framerateCapPresets", null);
            ActivateService(service);
            GameSettings.SetMouseSensitivity(6f, SettingsPersistMode.SkipUntilFlush);
            SettingsView view = CreateView(out Slider slider, out Dropdown presets);
            Dropdown framerate = CreateObject("Framerate", typeof(RectTransform)).AddComponent<Dropdown>();
            SetField(view, "_framerateDropdown", framerate);

            view.gameObject.SetActive(true);
            AssertActiveView(view, slider, presets);

            Assert.That(framerate.options, Is.Empty);
            Assert.That(slider.value, Is.EqualTo(6f));
            Assert.That(presets.options.Count, Is.EqualTo(6));
        }

        private static bool RequiresNativeLifecycle()
        {
            string testName = TestContext.CurrentContext.Test.Name;
            return testName is nameof(Deferred_DisablingActiveService_FlushesPendingValue)
                or nameof(DisablingFormerService_DoesNotSaveReplacementContext)
                or nameof(ActiveView_TracksApiChangesWithoutUiCallbacksOrRebuildingOptions)
                or nameof(QualityControlChange_RefreshesNamedPresetControl)
                or nameof(DisabledView_ReleasesControlsAndRebindsOnceWithoutRemovingExternalListeners)
                or nameof(NullFrameratePresetArray_ShowsEmptyOptionsAndStillRefreshesOtherControls);
        }

        private static void ActivateService(GameSettingsComponent service)
        {
            Assert.That(Application.isPlaying, Is.True, "Lifecycle evidence requires native Play Mode.");
            service.gameObject.SetActive(true);
            Assert.That(service.isActiveAndEnabled, Is.True);
            Assert.That(GameSettings.Context, Is.SameAs(service));
        }

        private static void AssertActiveView(SettingsView view, Slider slider, Dropdown presets)
        {
            Assert.That(Application.isPlaying, Is.True, "Lifecycle evidence requires native Play Mode.");
            Assert.That(view.isActiveAndEnabled, Is.True);
            Assert.That(slider.isActiveAndEnabled, Is.True);
            Assert.That(slider.minValue, Is.EqualTo(0.01f));
            Assert.That(slider.maxValue, Is.EqualTo(50f));
            Assert.That(presets.options.Count, Is.EqualTo(6), "Native OnEnable must populate the preset list.");
            Assert.That(slider.value, Is.EqualTo(GameSettings.MouseSensitivity));
        }

        private GameSettingsComponent CreateService(string prefix = Prefix)
        {
            GameObject root = CreateObject("Settings service");
            root.SetActive(false);
            GameSettingsComponent service = root.AddComponent<GameSettingsComponent>();
            SetField(service, "_setInstanceOnAwake", false);
            SetField(service, "_saveKeyPrefix", prefix);
            SetField(service, "_customResolutionPresets", new[]
            {
                new ResolutionPresetEntry { Width = Mathf.Max(1, Screen.width), Height = Mathf.Max(1, Screen.height) }
            });
            GameSettings.Attach(service);
            return service;
        }

        private SettingsView CreateView(out Slider slider, out Dropdown presets)
        {
            GameObject root = CreateObject("Settings view");
            root.SetActive(false);
            SettingsView view = root.AddComponent<SettingsView>();
            slider = CreateObject("Sensitivity", typeof(RectTransform)).AddComponent<Slider>();
            slider.minValue = 0.01f;
            slider.maxValue = 50f;
            presets = CreateObject("Presets", typeof(RectTransform)).AddComponent<Dropdown>();
            SetField(view, "_mouseSensitivitySlider", slider);
            SetField(view, "_graphicsPresetDropdown", presets);
            return view;
        }

        private GameObject CreateObject(string name, params Type[] components)
        {
            GameObject root = new(name, components);
            _objects.Add(root);
            return root;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void ResetSettings()
        {
            MethodInfo reset = typeof(GameSettings).GetMethod("ResetStaticStateForTesting",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null);
            reset.Invoke(null, null);
        }

        private sealed class MemoryProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new();
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

            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll() => _values.Clear();
            public void Save() => OnDataSaved?.Invoke();
            public void Load() => OnDataLoaded?.Invoke();

            private T Read<T>(string key, T fallback)
            {
                return _values.TryGetValue(key, out object stored) && stored is T typed ? typed : fallback;
            }

            private void Write(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
