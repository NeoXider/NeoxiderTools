using System.Collections.Generic;
using Neo;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Settings
{
    /// <summary>How slider-heavy settings commit to <see cref="GameSettings"/>.</summary>
    public enum SettingsViewCommitMode
    {
        /// <summary>Slider uses Deferred save; other controls Immediate.</summary>
        DebouncedLive = 0,

        /// <summary>Reserved: batch apply (same as DebouncedLive if Apply unused).</summary>
        ApplyButton = 1
    }

    /// <summary>
    ///     Binds Unity UI controls to <see cref="GameSettings"/>. Requires an active
    ///     <see cref="GameSettingsComponent"/> for preset lists and persistence.
    /// </summary>
    [NeoDoc("Settings/SettingsView.md")]
    [CreateFromMenu("Neoxider/Settings/Settings View Panel")]
    [AddComponentMenu("Neoxider/Settings/" + nameof(SettingsView))]
    public sealed class SettingsView : MonoBehaviour
    {
        [Header("Behaviour")] [SerializeField]
        private SettingsViewCommitMode _commitMode = SettingsViewCommitMode.DebouncedLive;

        [Tooltip(
            "Optional root objects hidden on platforms where custom resolution is not offered (see RefreshFromSettings).")]
        [SerializeField]
        private GameObject _resolutionBlockRoot;

        [Header("Input")] [SerializeField] private Slider _mouseSensitivitySlider;

        [Header("Graphics")] [SerializeField] private Dropdown _graphicsPresetDropdown;

        [SerializeField] private Dropdown _qualityDropdown;

        [Header("Display")] [SerializeField] private Toggle _fullScreenToggle;

        [SerializeField] private Dropdown _resolutionDropdown;

        [Header("Performance")] [SerializeField]
        private Dropdown _framerateDropdown;

        [SerializeField] private Toggle _vSyncToggle;

        [Header("Reset (optional)")] [SerializeField]
        private Button _resetGraphicsButton;

        [SerializeField] private Button _resetInputButton;

        [SerializeField] private Button _resetDisplayButton;

        [SerializeField] private Button _resetPerformanceButton;

        private bool _suspendHandlers;

        private void OnEnable()
        {
            GameSettings.OnAfterSettingsLoaded += OnSettingsReloaded;
            BindAll();
            RefreshFromSettings();
        }

        private void OnDisable()
        {
            GameSettings.OnAfterSettingsLoaded -= OnSettingsReloaded;
            GameSettings.FlushPendingSettingsSave();
        }

        private void OnSettingsReloaded()
        {
            RefreshFromSettings();
        }

        /// <summary>Wires UI callbacks once.</summary>
        public void BindAll()
        {
            if (_mouseSensitivitySlider != null)
            {
                _mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSlider);
                _mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSlider);
            }

            if (_graphicsPresetDropdown != null)
            {
                _graphicsPresetDropdown.onValueChanged.RemoveListener(OnGraphicsPreset);
                _graphicsPresetDropdown.onValueChanged.AddListener(OnGraphicsPreset);
            }

            if (_qualityDropdown != null)
            {
                _qualityDropdown.onValueChanged.RemoveListener(OnQuality);
                _qualityDropdown.onValueChanged.AddListener(OnQuality);
            }

            if (_fullScreenToggle != null)
            {
                _fullScreenToggle.onValueChanged.RemoveListener(OnFullScreen);
                _fullScreenToggle.onValueChanged.AddListener(OnFullScreen);
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.onValueChanged.RemoveListener(OnResolution);
                _resolutionDropdown.onValueChanged.AddListener(OnResolution);
            }

            if (_framerateDropdown != null)
            {
                _framerateDropdown.onValueChanged.RemoveListener(OnFramerate);
                _framerateDropdown.onValueChanged.AddListener(OnFramerate);
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.onValueChanged.RemoveListener(OnVSync);
                _vSyncToggle.onValueChanged.AddListener(OnVSync);
            }

            if (_resetGraphicsButton != null)
            {
                _resetGraphicsButton.onClick.RemoveListener(OnResetGraphics);
                _resetGraphicsButton.onClick.AddListener(OnResetGraphics);
            }

            if (_resetInputButton != null)
            {
                _resetInputButton.onClick.RemoveListener(OnResetInput);
                _resetInputButton.onClick.AddListener(OnResetInput);
            }

            if (_resetDisplayButton != null)
            {
                _resetDisplayButton.onClick.RemoveListener(OnResetDisplay);
                _resetDisplayButton.onClick.AddListener(OnResetDisplay);
            }

            if (_resetPerformanceButton != null)
            {
                _resetPerformanceButton.onClick.RemoveListener(OnResetPerformance);
                _resetPerformanceButton.onClick.AddListener(OnResetPerformance);
            }
        }

        /// <summary>Syncs control visuals from <see cref="GameSettings"/> without raising handlers.</summary>
        public void RefreshFromSettings()
        {
            _suspendHandlers = true;
            try
            {
                UpdatePlatformResolutionVisibility();
                PopulateGraphicsPresetDropdown();
                PopulateQualityDropdown();
                PopulateResolutionDropdown();
                PopulateFramerateDropdown();

                if (_mouseSensitivitySlider != null)
                {
                    _mouseSensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
                }

                if (_graphicsPresetDropdown != null)
                {
                    _graphicsPresetDropdown.SetValueWithoutNotify(PresetToDropdownIndex(GameSettings.GraphicsPreset));
                }

                if (_qualityDropdown != null && QualitySettings.names.Length > 0)
                {
                    _qualityDropdown.SetValueWithoutNotify(
                        Mathf.Clamp(GameSettings.QualityLevelIndex, 0, QualitySettings.names.Length - 1));
                }

                if (_fullScreenToggle != null)
                {
                    _fullScreenToggle.SetIsOnWithoutNotify(GameSettings.FullScreen);
                }

                if (_resolutionDropdown != null)
                {
                    int options = _resolutionDropdown.options.Count;
                    if (options > 0)
                    {
                        int v = GameSettings.ResolutionAuto
                            ? 0
                            : Mathf.Clamp(GameSettings.ResolutionIndex + 1, 0, options - 1);
                        _resolutionDropdown.SetValueWithoutNotify(v);
                    }
                }

                if (_framerateDropdown != null && _framerateDropdown.options.Count > 0)
                {
                    int idx = GameSettings.Context != null
                        ? GameSettings.Context.IndexOfFramerateValue(GameSettings.FramerateCap)
                        : 0;
                    _framerateDropdown.SetValueWithoutNotify(
                        Mathf.Clamp(idx, 0, _framerateDropdown.options.Count - 1));
                }

                if (_vSyncToggle != null)
                {
                    _vSyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
                }
            }
            finally
            {
                _suspendHandlers = false;
            }
        }

        private void UpdatePlatformResolutionVisibility()
        {
            if (_resolutionBlockRoot == null)
            {
                return;
            }

            RuntimePlatform p = Application.platform;
            bool limited = p == RuntimePlatform.WebGLPlayer
                           || p == RuntimePlatform.PS4 || p == RuntimePlatform.PS5
                           || p == RuntimePlatform.XboxOne
                           || p == RuntimePlatform.Switch;
            _resolutionBlockRoot.SetActive(!limited);
        }

        private void PopulateGraphicsPresetDropdown()
        {
            if (_graphicsPresetDropdown == null)
            {
                return;
            }

            _graphicsPresetDropdown.ClearOptions();
            var opts = new List<string>();
            opts.Add(GameSettings.Localize("settings.preset_minimal"));
            opts.Add(GameSettings.Localize("settings.preset_low"));
            opts.Add(GameSettings.Localize("settings.preset_medium"));
            opts.Add(GameSettings.Localize("settings.preset_high"));
            opts.Add(GameSettings.Localize("settings.preset_maximum"));
            opts.Add(GameSettings.Localize("settings.preset_custom"));
            _graphicsPresetDropdown.AddOptions(opts);
        }

        private void PopulateQualityDropdown()
        {
            if (_qualityDropdown == null)
            {
                return;
            }

            _qualityDropdown.ClearOptions();
            var opts = new List<string>();
            foreach (string n in QualitySettings.names)
            {
                opts.Add(n);
            }

            if (opts.Count == 0)
            {
                opts.Add("-");
            }

            _qualityDropdown.AddOptions(opts);
        }

        private void PopulateResolutionDropdown()
        {
            if (_resolutionDropdown == null)
            {
                return;
            }

            _resolutionDropdown.ClearOptions();
            var opts = new List<string> { GameSettings.Localize("settings.resolution_auto") };
            int n = GameSettings.GetRuntimeResolutionCount();
            for (int i = 0; i < n; i++)
            {
                if (GameSettings.TryGetRuntimeResolution(i, out int w, out int h))
                {
                    opts.Add($"{w} × {h}");
                }
            }

            _resolutionDropdown.AddOptions(opts);
        }

        private void PopulateFramerateDropdown()
        {
            if (_framerateDropdown == null || GameSettings.Context == null)
            {
                return;
            }

            _framerateDropdown.ClearOptions();
            var opts = new List<string>();
            IReadOnlyList<int> presets = GameSettings.Context.FramerateCapPresets;
            for (int i = 0; i < presets.Count; i++)
            {
                int v = presets[i];
                opts.Add(v < 0
                    ? GameSettings.Localize("settings.fps_unlimited")
                    : $"{v} {GameSettings.Localize("settings.fps_suffix")}");
            }

            _framerateDropdown.AddOptions(opts);
        }

        private static int PresetToDropdownIndex(GraphicsPreset preset)
        {
            return preset switch
            {
                GraphicsPreset.Minimal => 0,
                GraphicsPreset.Low => 1,
                GraphicsPreset.Medium => 2,
                GraphicsPreset.High => 3,
                GraphicsPreset.Maximum => 4,
                _ => 5
            };
        }

        private static GraphicsPreset DropdownIndexToPreset(int index)
        {
            return index switch
            {
                0 => GraphicsPreset.Minimal,
                1 => GraphicsPreset.Low,
                2 => GraphicsPreset.Medium,
                3 => GraphicsPreset.High,
                4 => GraphicsPreset.Maximum,
                _ => GraphicsPreset.Custom
            };
        }

        private void OnMouseSlider(float v)
        {
            if (_suspendHandlers)
            {
                return;
            }

            SettingsPersistMode mode = _commitMode == SettingsViewCommitMode.DebouncedLive
                ? SettingsPersistMode.Deferred
                : SettingsPersistMode.Immediate;
            GameSettings.SetMouseSensitivity(v, mode);
        }

        private void OnGraphicsPreset(int index)
        {
            if (_suspendHandlers)
            {
                return;
            }

            GameSettings.SetGraphicsPreset(DropdownIndexToPreset(index), SettingsPersistMode.Immediate);
        }

        private void OnQuality(int index)
        {
            if (_suspendHandlers)
            {
                return;
            }

            GameSettings.SetQualityLevel(index, SettingsPersistMode.Immediate);
        }

        private void OnFullScreen(bool v)
        {
            if (_suspendHandlers)
            {
                return;
            }

            GameSettings.SetFullScreen(v, SettingsPersistMode.Immediate);
        }

        private void OnResolution(int dropdownIndex)
        {
            if (_suspendHandlers)
            {
                return;
            }

            if (dropdownIndex <= 0)
            {
                GameSettings.SetResolutionAuto(true, SettingsPersistMode.Immediate);
            }
            else
            {
                GameSettings.SetResolutionIndex(dropdownIndex - 1, SettingsPersistMode.Immediate);
            }
        }

        private void OnFramerate(int index)
        {
            if (_suspendHandlers || GameSettings.Context == null)
            {
                return;
            }

            if (GameSettings.Context.TryGetFrameratePresetValue(index, out int cap))
            {
                GameSettings.SetFramerateCap(cap, SettingsPersistMode.Immediate);
            }
        }

        private void OnVSync(bool v)
        {
            if (_suspendHandlers)
            {
                return;
            }

            GameSettings.SetVSync(v, SettingsPersistMode.Immediate);
        }

        private void OnResetGraphics()
        {
            GameSettings.ResetGroup(SettingsGroup.Graphics);
            RefreshFromSettings();
        }

        private void OnResetInput()
        {
            GameSettings.ResetGroup(SettingsGroup.Input);
            RefreshFromSettings();
        }

        private void OnResetDisplay()
        {
            GameSettings.ResetGroup(SettingsGroup.Display);
            RefreshFromSettings();
        }

        private void OnResetPerformance()
        {
            GameSettings.ResetGroup(SettingsGroup.Performance);
            RefreshFromSettings();
        }
    }
}
