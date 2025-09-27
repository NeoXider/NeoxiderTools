using UnityEngine;
using UnityEngine.UI;

namespace Neo.Audio
{
    [AddComponentMenu("Neoxider/Audio/Audio Control")]
    public class AudioControl : MonoBehaviour
    {
        public enum ControlType
        {
            Master,
            Music,
            Efx
        }

        public enum UIType
        {
            Auto,
            Toggle,
            Slider
        }

        [Tooltip("Тип управления: Master, Music или Efx.")] [SerializeField]
        private ControlType controlType;

        [Tooltip("Тип UI элемента. 'Auto' определит автоматически.")] [SerializeField]
        private UIType uiType = UIType.Auto;

        private AMSettings settings;
        private Slider slider;

        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            slider = GetComponent<Slider>();

            if (uiType == UIType.Auto)
            {
                if (toggle != null)
                    uiType = UIType.Toggle;
                else if (slider != null)
                    uiType = UIType.Slider;
                else
                    Debug.LogError("[AudioControl] Не найден компонент Toggle или Slider!", this);
            }
        }

        private void Start()
        {
            settings = AMSettings.I;
            if (settings == null)
            {
                Debug.LogError("[AudioControl] AMSettings не найден на сцене!", this);
                if (toggle != null) toggle.interactable = false;
                if (slider != null) slider.interactable = false;
                return;
            }

            if (uiType == UIType.Toggle && toggle != null)
            {
                SyncToggleState();
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
                settings.OnMuteMusic.AddListener(_ => SyncToggleState());
                settings.OnMuteEfx.AddListener(_ => SyncToggleState());
            }
            else if (uiType == UIType.Slider && slider != null)
            {
                SyncSliderState();
                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (settings != null)
            {
                settings.OnMuteMusic.RemoveListener(_ => SyncToggleState());
                settings.OnMuteEfx.RemoveListener(_ => SyncToggleState());
            }

            if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        private void OnToggleValueChanged(bool value)
        {
            switch (controlType)
            {
                case ControlType.Master:
                    settings.SetMusicAndEfx(value);
                    break;
                case ControlType.Music:
                    settings.SetMusic(value);
                    break;
                case ControlType.Efx:
                    settings.SetEfx(value);
                    break;
            }
        }

        private void OnSliderValueChanged(float value)
        {
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

        private void SyncToggleState()
        {
            if (toggle == null || settings == null) return;

            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);

            switch (controlType)
            {
                case ControlType.Master:
                    toggle.isOn = settings.IsActiveMusic && settings.IsActiveEfx;
                    break;
                case ControlType.Music:
                    toggle.isOn = settings.IsActiveMusic;
                    break;
                case ControlType.Efx:
                    toggle.isOn = settings.IsActiveEfx;
                    break;
            }

            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void SyncSliderState()
        {
            if (slider == null || settings == null || settings.music == null || settings.efx == null) return;

            slider.onValueChanged.RemoveListener(OnSliderValueChanged);

            switch (controlType)
            {
                case ControlType.Music:
                    slider.value = settings.music.volume / settings.startMusicVolume;
                    break;
                case ControlType.Efx:
                    slider.value = settings.efx.volume / settings.startEfxVolume;
                    break;
                case ControlType.Master:
                    var avgPercent = (settings.music.volume / settings.startMusicVolume +
                                      settings.efx.volume / settings.startEfxVolume) / 2f;
                    slider.value = avgPercent;
                    break;
            }

            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }
}