using System;
using System.Collections;
using System.Collections.Generic;
using Neo;
using Neo.Save;
using Neo.Tools;
using UnityEngine;

namespace Neo.Settings
{
    /// <summary>
    ///     Singleton bootstrap for <see cref="GameSettings"/>: inspector config, debounced save coroutines, SaveProvider key prefix.
    /// </summary>
    [NeoDoc("Settings/GameSettingsComponent.md")]
    [CreateFromMenu("Neoxider/Settings/Game Settings Service")]
    [AddComponentMenu("Neoxider/Settings/" + nameof(GameSettingsComponent))]
    [DefaultExecutionOrder(-200)]
    public sealed class GameSettingsComponent : Singleton<GameSettingsComponent>
    {
        [Header("Save keys")]
        [Tooltip("Prefix for all SaveProvider keys written by GameSettings. Change to avoid collisions.")]
        [SerializeField]
        private string _saveKeyPrefix = "Neo.Settings.";

        [Header("Persist groups")]
        [Tooltip("When enabled, input values (e.g. mouse sensitivity) are written via SaveProvider.")]
        [SerializeField]
        private bool _persistInput = true;

        [Tooltip("When enabled, graphics preset / quality level are persisted.")] [SerializeField]
        private bool _persistGraphics = true;

        [Tooltip("When enabled, fullscreen and resolution selection are persisted.")] [SerializeField]
        private bool _persistDisplay = true;

        [Tooltip("When enabled, framerate cap and VSync are persisted.")] [SerializeField]
        private bool _persistPerformance = true;

        [Header("Graphics preset → QualitySettings index")]
        [Tooltip("Map each named tier to a quality level index (Project Quality settings order).")]
        [SerializeField]
        private GraphicsPresetLevelMapping[] _presetToQuality =
        {
            new() { Preset = GraphicsPreset.Minimal, QualityLevelIndex = 0 },
            new() { Preset = GraphicsPreset.Low, QualityLevelIndex = 0 },
            new() { Preset = GraphicsPreset.Medium, QualityLevelIndex = 1 },
            new() { Preset = GraphicsPreset.High, QualityLevelIndex = 2 },
            new() { Preset = GraphicsPreset.Maximum, QualityLevelIndex = 3 }
        };

        [Header("Framerate cap presets")]
        [Tooltip("Dropdown values for Application.targetFrameRate. Include -1 for Unlimited (recommended first).")]
        [SerializeField]
        private int[] _framerateCapPresets = { -1, 30, 60, 120, 144, 240 };

        [Header("Resolution")]
        [Tooltip(
            "If empty, presets are built from Screen.resolutions at startup. Otherwise these entries are used (unique WxH).")]
        [SerializeField]
        private ResolutionPresetEntry[] _customResolutionPresets = Array.Empty<ResolutionPresetEntry>();

        [Header("Debounce")]
        [Tooltip("Delay in seconds before Deferred save writes to SaveProvider.")]
        [SerializeField]
        [Min(0.05f)]
        private float _deferredSaveDelaySeconds = 0.35f;

        [Header("Default values (Reset group)")] [SerializeField]
        private float _defaultMouseSensitivity = 2f;

        [SerializeField] private GraphicsPreset _defaultGraphicsPreset = GraphicsPreset.High;

        [SerializeField] private int _defaultCustomQualityLevel;

        [SerializeField] private bool _defaultFullScreen = true;

        [SerializeField] private FullScreenMode _defaultFullScreenMode = FullScreenMode.FullScreenWindow;

        [SerializeField] private bool _defaultResolutionAuto = true;

        [SerializeField] private int _defaultResolutionIndex;

        [SerializeField] private int _defaultFramerateCap = -1;

        [SerializeField] private bool _defaultVSync;

        private readonly List<ResolutionPresetEntry> _runtimeResolutions = new();
        private Coroutine _deferredCoroutine;

        internal string SaveKeyPrefix => _saveKeyPrefix;

        internal bool PersistInput => _persistInput;
        internal bool PersistGraphics => _persistGraphics;
        internal bool PersistDisplay => _persistDisplay;
        internal bool PersistPerformance => _persistPerformance;

        internal IReadOnlyList<ResolutionPresetEntry> RuntimeResolutions => _runtimeResolutions;
        internal IReadOnlyList<int> FramerateCapPresets => _framerateCapPresets;

        internal float DefaultMouseSensitivity => _defaultMouseSensitivity;
        internal GraphicsPreset DefaultGraphicsPreset => _defaultGraphicsPreset;
        internal int DefaultCustomQualityLevel => _defaultCustomQualityLevel;
        internal bool DefaultFullScreen => _defaultFullScreen;
        internal FullScreenMode DefaultFullScreenMode => _defaultFullScreenMode;
        internal bool DefaultResolutionAuto => _defaultResolutionAuto;
        internal int DefaultResolutionIndex => _defaultResolutionIndex;
        internal int DefaultFramerateCap => _defaultFramerateCap;
        internal bool DefaultVSync => _defaultVSync;

        internal float DeferredSaveDelaySeconds => _deferredSaveDelaySeconds;

        protected override void Init()
        {
            base.Init();
            RebuildResolutionList();
            GameSettings.Attach(this);
            GameSettings.LoadState();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameSettings.Detach(this);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                GameSettings.FlushPendingSettingsSave();
            }
        }

        internal void RebuildResolutionList()
        {
            _runtimeResolutions.Clear();
            if (_customResolutionPresets != null && _customResolutionPresets.Length > 0)
            {
                var seen = new HashSet<long>();
                foreach (ResolutionPresetEntry e in _customResolutionPresets)
                {
                    if (e.Width < 1 || e.Height < 1)
                    {
                        continue;
                    }

                    long k = ((long)e.Width << 32) | (uint)e.Height;
                    if (seen.Add(k))
                    {
                        _runtimeResolutions.Add(e);
                    }
                }

                _runtimeResolutions.Sort((a, b) =>
                {
                    int c = b.Width.CompareTo(a.Width);
                    return c != 0 ? c : b.Height.CompareTo(a.Height);
                });
                return;
            }

            Resolution[] resolutions = Screen.resolutions;
            var set = new HashSet<long>();
            foreach (Resolution r in resolutions)
            {
                long k = ((long)r.width << 32) | (uint)r.height;
                if (set.Add(k))
                {
                    _runtimeResolutions.Add(new ResolutionPresetEntry { Width = r.width, Height = r.height });
                }
            }

            _runtimeResolutions.Sort((a, b) =>
            {
                int c = b.Width.CompareTo(a.Width);
                return c != 0 ? c : b.Height.CompareTo(a.Height);
            });
        }

        internal int GetQualityLevelForPreset(GraphicsPreset preset)
        {
            if (preset is GraphicsPreset.Custom)
            {
                return Mathf.Clamp(_defaultCustomQualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            }

            if (_presetToQuality == null)
            {
                return 0;
            }

            foreach (GraphicsPresetLevelMapping m in _presetToQuality)
            {
                if (m.Preset == preset)
                {
                    return Mathf.Clamp(m.QualityLevelIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                }
            }

            return 0;
        }

        /// <summary>Tries to map a quality level index to a named preset; returns Custom if no mapping matches.</summary>
        internal GraphicsPreset GetPresetForQualityLevel(int qualityLevelIndex)
        {
            if (_presetToQuality == null || QualitySettings.names.Length == 0)
            {
                return GraphicsPreset.Custom;
            }

            qualityLevelIndex = Mathf.Clamp(qualityLevelIndex, 0, QualitySettings.names.Length - 1);
            foreach (GraphicsPresetLevelMapping m in _presetToQuality)
            {
                if (m.Preset != GraphicsPreset.Custom && m.QualityLevelIndex == qualityLevelIndex)
                {
                    return m.Preset;
                }
            }

            return GraphicsPreset.Custom;
        }

        internal bool TryGetFrameratePresetValue(int dropdownIndex, out int targetFrameRate)
        {
            targetFrameRate = -1;
            if (_framerateCapPresets == null || _framerateCapPresets.Length == 0)
            {
                return false;
            }

            if (dropdownIndex < 0 || dropdownIndex >= _framerateCapPresets.Length)
            {
                return false;
            }

            targetFrameRate = _framerateCapPresets[dropdownIndex];
            return true;
        }

        internal int IndexOfFramerateValue(int targetFrameRate)
        {
            if (_framerateCapPresets == null)
            {
                return 0;
            }

            for (int i = 0; i < _framerateCapPresets.Length; i++)
            {
                if (_framerateCapPresets[i] == targetFrameRate)
                {
                    return i;
                }
            }

            return 0;
        }

        internal void ScheduleDeferredSave()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_deferredCoroutine != null)
            {
                StopCoroutine(_deferredCoroutine);
            }

            _deferredCoroutine = StartCoroutine(DeferredSaveCo());
        }

        private IEnumerator DeferredSaveCo()
        {
            yield return new WaitForSecondsRealtime(_deferredSaveDelaySeconds);
            _deferredCoroutine = null;
            GameSettings.SaveState();
        }

        /// <summary>Forces any pending Deferred save to run immediately.</summary>
        public void FlushDeferredSaveCoroutine()
        {
            if (_deferredCoroutine != null)
            {
                StopCoroutine(_deferredCoroutine);
                _deferredCoroutine = null;
            }
        }

        /// <summary>Reload from SaveProvider and apply (same as static API).</summary>
        public void ReloadFromDisk()
        {
            GameSettings.LoadState();
        }

        /// <summary>Writes current GameSettings to SaveProvider.</summary>
        public void SaveNow()
        {
            GameSettings.SaveState();
        }

        /// <summary>Menu/UI: set mouse sensitivity with Deferred persistence.</summary>
        public void SetMouseSensitivityForMenu(float value)
        {
            GameSettings.SetMouseSensitivity(value, SettingsPersistMode.Deferred);
        }

        /// <summary>Menu/UI: set mouse sensitivity when drag ends (Immediate).</summary>
        public void SetMouseSensitivityForMenuImmediate(float value)
        {
            GameSettings.SetMouseSensitivity(value, SettingsPersistMode.Immediate);
        }
    }
}
