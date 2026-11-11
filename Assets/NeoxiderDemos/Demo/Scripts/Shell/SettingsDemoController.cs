using System;
using System.Globalization;
using Neo.Save;
using Neo.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Settings</b> demo scene. The whole UI (frame, rows, log feed, info
    ///     card) is saved in SettingsDemo.unity; buttons, the preset dropdown and the
    ///     VSync toggle call the public methods below through persistent UnityEvents, and the sensitivity slider
    ///     calls <see cref="GameSettingsComponent.SetMouseSensitivityForMenu" /> on the service directly (the
    ///     NoCode path). This component only forwards to the real <see cref="GameSettings" /> API, mirrors the
    ///     live values into the authored labels and logs every call. While the slider is dragged it shows the
    ///     Deferred debounce (<see cref="GameSettingsComponent.HasPendingDeferredSave" />) and the value stored in
    ///     <see cref="SaveProvider" />, so one write per burst of changes is visible. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Settings Demo")]
    public sealed class SettingsDemoController : MonoBehaviour
    {
        [Header("Service")]
        [Tooltip("Scene settings service. Created as a singleton on play when left empty.")]
        [SerializeField] private GameSettingsComponent _settings;

        [Tooltip("Must match the service's save key prefix; used to read the stored sensitivity back.")]
        [SerializeField] private string _saveKeyPrefix = "Neo.Settings.";

        [Header("Live values")]
        [SerializeField] private TMP_Text _sensitivityValue;
        [SerializeField] private TMP_Text _presetValue;
        [SerializeField] private TMP_Text _framerateValue;
        [SerializeField] private TMP_Text _vSyncValue;

        [Header("Deferred save")]
        [SerializeField] private TMP_Text _deferredValue;
        [SerializeField] private TMP_Text _storedValue;

        [Header("Controls synced on start")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private TMP_Dropdown _presetDropdown;
        [SerializeField] private Toggle _vSyncToggle;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private bool _subscribed;
        private bool _warnedUnbuilt;
        private bool _watchingDeferredSave;
        private int _burstChanges;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _log != null && _presetValue != null && _framerateValue != null &&
                                    _vSyncValue != null && _sensitivityValue != null && _deferredValue != null &&
                                    _storedValue != null;

        public GameSettingsComponent Settings => _settings;

        /// <summary>Slider changes collected by the running Deferred debounce (0 when idle).</summary>
        public int PendingSliderChanges => _watchingDeferredSave ? _burstChanges : 0;

        private void OnEnable()
        {
            if (!_subscribed)
            {
                GameSettings.OnSettingsChanged += RefreshLabels;
                _subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                GameSettings.OnSettingsChanged -= RefreshLabels;
                _subscribed = false;
            }
        }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            EnsureService();
            SyncControls();
            RefreshLabels();
            RefreshStored();
            SetDeferredText("idle · drag the slider");
            Log("GameSettings live - every control calls the real Set* API");
        }

        private void Update()
        {
            // WHY: the service exposes the debounce as a property only; poll it while a burst is running.
            if (!_watchingDeferredSave || (_settings != null && _settings.HasPendingDeferredSave))
            {
                return;
            }

            _watchingDeferredSave = false;
            SetDeferredText("saved · " + _burstChanges + (_burstChanges == 1 ? " change -> 1 write" : " changes -> 1 write"));
            RefreshStored();
            Log("Deferred save wrote MouseSensitivity once for " + _burstChanges + " slider changes");
            _burstChanges = 0;
        }

        /// <summary>Dropdown target: applies a <see cref="GraphicsPreset" /> by its enum value.</summary>
        public void SetGraphicsPreset(int preset)
        {
            if (!Enum.IsDefined(typeof(GraphicsPreset), preset))
            {
                Log("Ignored unknown graphics preset " + preset);
                return;
            }

            GraphicsPreset value = (GraphicsPreset)preset;
            GameSettings.SetGraphicsPreset(value);
            Log("GameSettings.SetGraphicsPreset(" + value + ")");
        }

        /// <summary>Button target: caps the framerate (-1 = unlimited).</summary>
        public void SetFramerateCap(int framesPerSecond)
        {
            GameSettings.SetFramerateCap(framesPerSecond);
            Log("GameSettings.SetFramerateCap(" + framesPerSecond + ") -> Application.targetFrameRate");
        }

        /// <summary>Toggle target.</summary>
        public void SetVSync(bool enabled)
        {
            GameSettings.SetVSync(enabled);
            Log("GameSettings.SetVSync(" + enabled + ") -> QualitySettings.vSyncCount");
        }

        /// <summary>
        ///     Slider listener next to the NoCode service call: counts the changes the Deferred debounce collects and
        ///     logs the first one of each burst (logging every drag frame would flood the feed).
        /// </summary>
        public void LogSensitivity(float value)
        {
            if (!_watchingDeferredSave)
            {
                _watchingDeferredSave = true;
                _burstChanges = 0;
                Log("GameSettingsComponent.SetMouseSensitivityForMenu(" + value.ToString("0.0", CultureInfo.InvariantCulture) +
                    ") · Deferred save scheduled");
            }

            _burstChanges++;
            SetDeferredText("saving… " + _burstChanges + (_burstChanges == 1 ? " change queued" : " changes queued"));
        }

        /// <summary>Button target: writes pending values through SaveProvider.</summary>
        public void SaveSettings()
        {
            GameSettings.FlushPendingSettingsSave();
            SaveProvider.Save();
            if (_watchingDeferredSave)
            {
                _watchingDeferredSave = false;
                _burstChanges = 0;
                SetDeferredText("flushed now · debounce cancelled");
            }

            RefreshStored();
            Log("GameSettings.FlushPendingSettingsSave() -> persisted");
        }

        /// <summary>Button target: restores the Input, Graphics and Performance defaults.</summary>
        public void ResetDefaults()
        {
            GameSettings.ResetGroup(SettingsGroup.Input);
            GameSettings.ResetGroup(SettingsGroup.Graphics);
            GameSettings.ResetGroup(SettingsGroup.Performance);
            SyncControls();
            RefreshStored();
            Log("GameSettings.ResetGroup(Input / Graphics / Performance)");
        }

        private void EnsureService()
        {
            if (_settings != null)
            {
                return;
            }

            if (GameSettingsComponent.HasInstance)
            {
                _settings = GameSettingsComponent.I;
                return;
            }

            // WHY: the service is gameplay state, not UI; creating it keeps the controller usable in an empty scene.
            GameSettingsComponent.CreateInstance = true;
            _settings = GameSettingsComponent.I;
            Log("Created GameSettingsComponent singleton");
        }

        private void SyncControls()
        {
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            }

            if (_presetDropdown != null)
            {
                int preset = (int)GameSettings.GraphicsPreset;
                if (preset >= 0 && preset < _presetDropdown.options.Count)
                {
                    _presetDropdown.SetValueWithoutNotify(preset);
                }
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            }
        }

        private void RefreshLabels()
        {
            if (_sensitivityValue != null)
            {
                _sensitivityValue.text = GameSettings.MouseSensitivity.ToString("0.0", CultureInfo.InvariantCulture);
            }

            if (_presetValue != null)
            {
                _presetValue.text = GameSettings.GraphicsPreset.ToString();
            }

            if (_framerateValue != null)
            {
                _framerateValue.text = GameSettings.FramerateCap < 0 ? "Unlimited" : GameSettings.FramerateCap + " fps";
            }

            if (_vSyncValue != null)
            {
                _vSyncValue.text = GameSettings.VSync ? "ON" : "OFF";
            }
        }

        private void RefreshStored()
        {
            if (_storedValue == null)
            {
                return;
            }

            string key = _saveKeyPrefix + GameSettingsSaveKeys.MouseSensitivity;
            _storedValue.text = SaveProvider.HasKey(key)
                ? SaveProvider.GetFloat(key).ToString("0.0", CultureInfo.InvariantCulture)
                : "not saved yet";
        }

        private void SetDeferredText(string text)
        {
            if (_deferredValue != null)
            {
                _deferredValue.text = text;
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[SettingsDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
