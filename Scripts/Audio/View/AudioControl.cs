using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Audio
{
    /// <summary>UI component that binds a Toggle or Slider to AMSettings (Master/Music/Efx volume or mute).</summary>
    [NeoDoc("Audio/View/AudioControl.md")]
    [CreateFromMenu("Neoxider/Audio/AudioControl")]
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AudioControl))]
    public class AudioControl : MonoBehaviour
    {
        /// <summary>Backend used for built-in channels (Master/Music/Efx).</summary>
        public enum BackendType
        {
            AudioSourceAndMixer,
            MixerOnly
        }

        /// <summary>Which channel to control: Master, Music, or Efx.</summary>
        public enum ControlType
        {
            Master,
            Music,
            Efx,
            Custom
        }

        /// <summary>UI element type. Auto detects Toggle or Slider on this GameObject.</summary>
        public enum UIType
        {
            Auto,
            Toggle,
            Slider
        }

        [Header("Settings")] [Tooltip("Control type: Master, Music, Efx or Custom.")] [SerializeField]
        private ControlType controlType;

        [Tooltip("UI element type. 'Auto' detects automatically.")] [SerializeField]
        private UIType uiType = UIType.Auto;

        [Tooltip("Backend for Master/Music/Efx. Use MixerOnly to control exposed mixer parameters directly.")]
        [SerializeField]
        private BackendType backendType = BackendType.MixerOnly;

        [Tooltip("For Custom type: called by Set(bool) and Toggle UI.")] [SerializeField]
        private BoolEvent onSetActiveCustom = new();

        [Tooltip("For Custom type: called by Set(float) and Slider UI. Value is normalized (0..1).")] [SerializeField]
        private FloatEvent onSetPercentCustom = new();

        [Tooltip("Cached active state for Custom type UI synchronization.")] [SerializeField]
        private bool customActive = true;

        [Tooltip("Cached normalized percent (0..1) for Custom type UI synchronization.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float customPercent = 1f;

        [Tooltip("Force Slider range to normalized percent (0..1).")] [SerializeField]
        private bool forceSliderNormalizedRange = true;

        [SerializeField] [Range(0f, 1f)] private float unmutePercent = 1f;

        private AMSettings settings;
        private Slider slider;

        private Toggle toggle;
        private bool _started;
        private IDisposable _muteSubscription;
        private bool _disabledToggle;
        private bool _disabledSlider;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            slider = GetComponent<Slider>();

            if (uiType == UIType.Auto)
            {
                if (toggle != null)
                {
                    uiType = UIType.Toggle;
                }
                else if (slider != null)
                {
                    uiType = UIType.Slider;
                }
                else
                {
                    NeoDiagnostics.LogError("[AudioControl] No Toggle or Slider component found on this GameObject.",
                        this);
                }
            }
        }

        private void OnEnable()
        {
            if (!_started)
            {
                return;
            }

            RefreshBinding();
        }

        private void Start()
        {
            if (_started)
            {
                return;
            }

            // UI input exists even when a dependency has not arrived yet; binding can be retried later.
            _started = true;
            if (uiType == UIType.Toggle && toggle != null)
            {
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
            else if (uiType == UIType.Slider && slider != null)
            {
                if (forceSliderNormalizedRange)
                {
                    slider.minValue = 0f;
                    slider.maxValue = 1f;
                }

                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            RefreshBinding();
            if (controlType != ControlType.Custom && settings == null)
            {
                NeoDiagnostics.LogError("[AudioControl] AMSettings was not found in the scene.", this);
            }
        }

        /// <summary>
        ///     Assigns settings explicitly. An active started control rebinds and refreshes silently;
        ///     before Start the reference is retained for normal initialization. Null restores singleton fallback.
        /// </summary>
        public void Initialize(AMSettings audioSettings)
        {
            if (!ReferenceEquals(settings, audioSettings))
            {
                UnsubscribeMuteEventsForToggle();
                settings = audioSettings;
            }

            if (_started && isActiveAndEnabled)
            {
                RefreshBinding();
            }
        }

        private void RefreshBinding()
        {
            TryResolveSettings();
            if (controlType != ControlType.Custom && settings == null)
            {
                DisableUnboundInput();
                return;
            }

            RestoreInput();
            SubscribeMuteEventsForToggle();
            if (uiType == UIType.Toggle && toggle != null)
            {
                SyncToggleState();
            }
            else if (uiType == UIType.Slider && slider != null)
            {
                SyncSliderState();
            }
        }

        private void DisableUnboundInput()
        {
            if (uiType == UIType.Toggle && toggle != null && toggle.interactable)
            {
                _disabledToggle = true;
                toggle.interactable = false;
            }
            else if (uiType == UIType.Slider && slider != null && slider.interactable)
            {
                _disabledSlider = true;
                slider.interactable = false;
            }
        }

        private void RestoreInput()
        {
            if (_disabledToggle && toggle != null)
            {
                toggle.interactable = true;
            }

            if (_disabledSlider && slider != null)
            {
                slider.interactable = true;
            }

            _disabledToggle = false;
            _disabledSlider = false;
        }

        private void TryResolveSettings()
        {
            if (controlType == ControlType.Custom)
            {
                return;
            }

            if (settings != null)
            {
                return;
            }

            UnsubscribeMuteEventsForToggle();
            settings = AMSettings.TryGetInstance(out AMSettings s) ? s : AMSettings.I;
            if (_started && isActiveAndEnabled && settings != null)
            {
                RestoreInput();
                SubscribeMuteEventsForToggle();
            }
        }

        private void SubscribeMuteEventsForToggle()
        {
            if (settings == null || _muteSubscription != null || !_started || !isActiveAndEnabled ||
                uiType != UIType.Toggle || toggle == null)
            {
                return;
            }

            switch (controlType)
            {
                case ControlType.Master:
                    _muteSubscription = settings.MuteMaster.Subscribe(OnMuteChanged);
                    break;
                case ControlType.Music:
                    _muteSubscription = settings.MuteMusic.Subscribe(OnMuteChanged);
                    break;
                case ControlType.Efx:
                    _muteSubscription = settings.MuteEfx.Subscribe(OnMuteChanged);
                    break;
            }
        }

        private void UnsubscribeMuteEventsForToggle()
        {
            _muteSubscription?.Dispose();
            _muteSubscription = null;
        }

        private void OnDisable()
        {
            UnsubscribeMuteEventsForToggle();
            RestoreInput();
        }

        private void OnDestroy()
        {
            UnsubscribeMuteEventsForToggle();
            RestoreInput();

            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }

            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
        }

        private void OnMuteChanged(bool _)
        {
            if (isActiveAndEnabled)
            {
                SyncToggleState();
            }
        }

        private void OnToggleValueChanged(bool value)
        {
            if (isActiveAndEnabled)
            {
                Set(value);
            }
        }

        private void OnSliderValueChanged(float value)
        {
            if (isActiveAndEnabled)
            {
                Set(value);
            }
        }

        /// <summary>Sets active state for selected control type.</summary>
        public void Set(bool active)
        {
            // WHY: UnityEvents can call Set before Start ran (execution order) — resolve lazily.
            TryResolveSettings();

            switch (controlType)
            {
                case ControlType.Master:
                    if (settings == null)
                    {
                        return;
                    }

                    ApplyActiveForBuiltIn(active);
                    break;
                case ControlType.Music:
                    if (settings == null)
                    {
                        return;
                    }

                    ApplyActiveForBuiltIn(active);
                    break;
                case ControlType.Efx:
                    if (settings == null)
                    {
                        return;
                    }

                    ApplyActiveForBuiltIn(active);
                    break;
                case ControlType.Custom:
                    customActive = active;
                    onSetActiveCustom?.Invoke(active);
                    break;
            }
        }

        /// <summary>Sets normalized value in range 0..1 for selected control type.</summary>
        public void Set(float percent)
        {
            TryResolveSettings();
            float normalizedPercent = Mathf.Clamp01(percent);

            switch (controlType)
            {
                case ControlType.Master:
                    if (settings == null)
                    {
                        return;
                    }

                    ApplyPercentForBuiltIn(normalizedPercent);
                    break;
                case ControlType.Music:
                    if (settings == null)
                    {
                        return;
                    }

                    ApplyPercentForBuiltIn(normalizedPercent);
                    break;
                case ControlType.Efx:
                    if (settings == null)
                    {
                        return;
                    }

                    ApplyPercentForBuiltIn(normalizedPercent);
                    break;
                case ControlType.Custom:
                    customPercent = normalizedPercent;
                    onSetPercentCustom?.Invoke(normalizedPercent);
                    break;
            }
        }

        private void SyncToggleState()
        {
            if (toggle == null)
            {
                return;
            }

            if (controlType != ControlType.Custom && settings == null)
            {
                return;
            }

            switch (controlType)
            {
                case ControlType.Master:
                    toggle.SetIsOnWithoutNotify(!settings.MuteMasterValue && GetBuiltInPercent() > 0f);
                    break;
                case ControlType.Music:
                    toggle.SetIsOnWithoutNotify(!settings.MuteMusicValue && GetBuiltInPercent() > 0f);
                    break;
                case ControlType.Efx:
                    toggle.SetIsOnWithoutNotify(!settings.MuteEfxValue && GetBuiltInPercent() > 0f);
                    break;
                case ControlType.Custom:
                    toggle.SetIsOnWithoutNotify(customActive);
                    break;
            }
        }

        private void SyncSliderState()
        {
            if (slider == null)
            {
                return;
            }

            switch (controlType)
            {
                case ControlType.Music:
                    slider.SetValueWithoutNotify(GetBuiltInPercent());
                    break;
                case ControlType.Efx:
                    slider.SetValueWithoutNotify(GetBuiltInPercent());
                    break;
                case ControlType.Master:
                    slider.SetValueWithoutNotify(GetBuiltInPercent());
                    break;
                case ControlType.Custom:
                    slider.SetValueWithoutNotify(Mathf.Clamp01(customPercent));
                    break;
            }
        }

        private void ApplyActiveForBuiltIn(bool active)
        {
            float targetPercent = active ? Mathf.Clamp01(unmutePercent) : 0f;
            if (active && controlType == ControlType.Master && settings.MuteMasterValue)
            {
                settings.ToggleMaster();
            }

            ApplyPercentForBuiltIn(targetPercent);
            if (active)
            {
                switch (controlType)
                {
                    case ControlType.Master:
                        if (backendType == BackendType.AudioSourceAndMixer)
                        {
                            settings.SetMusicAndEfx(true);
                        }

                        break;
                    case ControlType.Music:
                        settings.SetMusic(true);
                        break;
                    case ControlType.Efx:
                        settings.SetEfx(true);
                        break;
                }
            }
        }

        private void ApplyPercentForBuiltIn(float normalizedPercent)
        {
            float value = Mathf.Clamp01(normalizedPercent);
            if (value > 0f)
            {
                unmutePercent = value;
            }

            if (backendType == BackendType.MixerOnly)
            {
                switch (controlType)
                {
                    case ControlType.Master:
                        settings.SetMasterVolume(value);
                        break;
                    case ControlType.Music:
                        settings.SetMusicMixerVolume(value);
                        break;
                    case ControlType.Efx:
                        settings.SetEfxMixerVolume(value);
                        break;
                }

                return;
            }

            switch (controlType)
            {
                case ControlType.Master:
                    settings.SetMusicAndEfxVolume(value);
                    break;
                case ControlType.Music:
                    settings.SetMusicVolume(value);
                    break;
                case ControlType.Efx:
                    settings.SetEfxVolume(value);
                    break;
            }
        }

        private float GetBuiltInPercent()
        {
            if (settings == null)
            {
                return 0f;
            }

            if (backendType == BackendType.MixerOnly)
            {
                switch (controlType)
                {
                    case ControlType.Master:
                        return settings.GetMasterVolumeNormalized();
                    case ControlType.Music:
                        return settings.GetMusicVolumeNormalized();
                    case ControlType.Efx:
                        return settings.GetEfxVolumeNormalized();
                }
            }

            switch (controlType)
            {
                case ControlType.Master:
                    return (settings.GetMusicVolumeNormalized() + settings.GetEfxVolumeNormalized()) * 0.5f;
                case ControlType.Music:
                    return settings.GetMusicVolumeNormalized();
                case ControlType.Efx:
                    return settings.GetEfxVolumeNormalized();
                default:
                    return 0f;
            }
        }

        [Serializable]
        public class BoolEvent : UnityEvent<bool>
        {
        }

        [Serializable]
        public class FloatEvent : UnityEvent<float>
        {
        }
    }
}
