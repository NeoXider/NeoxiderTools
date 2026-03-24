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
                    Debug.LogError("[AudioControl] Не найден компонент Toggle или Slider!", this);
                }
            }
        }

        private void OnEnable()
        {
            if (!_started)
            {
                return;
            }

            TryResolveSettings();
            if (controlType == ControlType.Custom || settings == null)
            {
                return;
            }

            if (uiType == UIType.Toggle && toggle != null)
            {
                SyncToggleState();
            }
            else if (uiType == UIType.Slider && slider != null)
            {
                SyncSliderState();
            }
        }

        private void Start()
        {
            TryResolveSettings();
            if (controlType != ControlType.Custom && settings == null)
            {
                Debug.LogError("[AudioControl] AMSettings не найден на сцене!", this);
                if (toggle != null)
                {
                    toggle.interactable = false;
                }

                if (slider != null)
                {
                    slider.interactable = false;
                }

                return;
            }

            if (uiType == UIType.Toggle && toggle != null)
            {
                SyncToggleState();
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
                SubscribeMuteEventsForToggle();
            }
            else if (uiType == UIType.Slider && slider != null)
            {
                if (forceSliderNormalizedRange)
                {
                    slider.minValue = 0f;
                    slider.maxValue = 1f;
                }

                SyncSliderState();
                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            _started = true;
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

            settings = AMSettings.TryGetInstance(out AMSettings s) ? s : AMSettings.I;
        }

        private void SubscribeMuteEventsForToggle()
        {
            if (settings == null)
            {
                return;
            }

            switch (controlType)
            {
                case ControlType.Master:
                    settings.MuteMaster.OnChanged.AddListener(OnMuteChanged);
                    break;
                case ControlType.Music:
                    settings.MuteMusic.OnChanged.AddListener(OnMuteChanged);
                    break;
                case ControlType.Efx:
                    settings.MuteEfx.OnChanged.AddListener(OnMuteChanged);
                    break;
            }
        }

        private void UnsubscribeMuteEventsForToggle()
        {
            if (settings == null)
            {
                return;
            }

            switch (controlType)
            {
                case ControlType.Master:
                    settings.MuteMaster.OnChanged.RemoveListener(OnMuteChanged);
                    break;
                case ControlType.Music:
                    settings.MuteMusic.OnChanged.RemoveListener(OnMuteChanged);
                    break;
                case ControlType.Efx:
                    settings.MuteEfx.OnChanged.RemoveListener(OnMuteChanged);
                    break;
            }
        }

        private void OnDestroy()
        {
            if (uiType == UIType.Toggle)
            {
                UnsubscribeMuteEventsForToggle();
            }

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
            SyncToggleState();
        }

        private void OnToggleValueChanged(bool value)
        {
            Set(value);
        }

        private void OnSliderValueChanged(float value)
        {
            Set(value);
        }

        /// <summary>Sets active state for selected control type.</summary>
        public void Set(bool active)
        {
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

            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);

            switch (controlType)
            {
                case ControlType.Master:
                    toggle.isOn = GetBuiltInPercent() > 0f;
                    break;
                case ControlType.Music:
                    toggle.isOn = GetBuiltInPercent() > 0f;
                    break;
                case ControlType.Efx:
                    toggle.isOn = GetBuiltInPercent() > 0f;
                    break;
                case ControlType.Custom:
                    toggle.isOn = customActive;
                    break;
            }

            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void SyncSliderState()
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.RemoveListener(OnSliderValueChanged);

            switch (controlType)
            {
                case ControlType.Music:
                    slider.value = GetBuiltInPercent();
                    break;
                case ControlType.Efx:
                    slider.value = GetBuiltInPercent();
                    break;
                case ControlType.Master:
                    slider.value = GetBuiltInPercent();
                    break;
                case ControlType.Custom:
                    slider.value = Mathf.Clamp01(customPercent);
                    break;
            }

            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void ApplyActiveForBuiltIn(bool active)
        {
            float targetPercent = active ? Mathf.Clamp01(unmutePercent) : 0f;
            ApplyPercentForBuiltIn(targetPercent);
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
