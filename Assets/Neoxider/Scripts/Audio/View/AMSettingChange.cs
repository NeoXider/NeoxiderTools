using System;
using UnityEngine;

namespace Neo.Audio
{
    public class AMSettingChange : MonoBehaviour
    {
        [FindAllInScene]
        [SerializeField]
        private SettingsAudio _settingsAudio;

        [Space]
        [SerializeField]
        private UnityEngine.UI.Toggle[] _toggleEfx;

        [SerializeField]
        private UnityEngine.UI.Toggle[] _toggleMusic;

        [SerializeField]
        private UnityEngine.UI.Toggle[] _toggleAll;

        [Space]
        [SerializeField]
        private UnityEngine.UI.Slider[] _sliderVolumeEfx;

        [SerializeField]
        private UnityEngine.UI.Slider[] _sliderVolumeMusic;

        [SerializeField]
        private UnityEngine.UI.Slider[] _sliderVolumeAll;

        private void Awake()
        {
            if (_settingsAudio != null)
            {
                SubscribeToggles();
                SubscribeSliders();
                SyncValues();
            }
        }
        private void OnDestroy()
        {
            UnsubscribeToggles();
            UnsubscribeSliders();
        }

        private void SubscribeToggles()
        {
            foreach (var toggle in _toggleEfx) toggle.onValueChanged.AddListener(SetToggleEfx);
            foreach (var toggle in _toggleMusic) toggle.onValueChanged.AddListener(SetToggleMusic);
            foreach (var toggle in _toggleAll) toggle.onValueChanged.AddListener(SetToggleAll);
        }

        private void UnsubscribeToggles()
        {
            foreach (var toggle in _toggleEfx) toggle.onValueChanged.RemoveListener(SetToggleEfx);
            foreach (var toggle in _toggleMusic) toggle.onValueChanged.RemoveListener(SetToggleMusic);
            foreach (var toggle in _toggleAll) toggle.onValueChanged.RemoveListener(SetToggleAll);
        }

        private void SubscribeSliders()
        {
            foreach (var slider in _sliderVolumeEfx) slider.onValueChanged.AddListener(SetSliderVolumeEfx);
            foreach (var slider in _sliderVolumeMusic) slider.onValueChanged.AddListener(SetSliderVolumeMusic);
            foreach (var slider in _sliderVolumeAll)
                slider.onValueChanged.AddListener(SetSliderVolumeAll);
        }

        private void UnsubscribeSliders()
        {
            foreach (var slider in _sliderVolumeEfx) slider.onValueChanged.RemoveListener(SetSliderVolumeEfx);
            foreach (var slider in _sliderVolumeMusic) slider.onValueChanged.RemoveListener(SetSliderVolumeMusic);
            foreach (var slider in _sliderVolumeAll)
                slider.onValueChanged.RemoveListener(SetSliderVolumeAll);
        }

        private void SyncValues()
        {
            foreach (var toggle in _toggleEfx) toggle.isOn = !_settingsAudio.efx.mute;
            foreach (var toggle in _toggleMusic) toggle.isOn = !_settingsAudio.music.mute;
            foreach (var toggle in _toggleAll)
                toggle.isOn = !(_settingsAudio.efx.mute && _settingsAudio.music.mute);

            foreach (var slider in _sliderVolumeEfx)
                slider.value = _settingsAudio.startEfxVolume;
            foreach (var slider in _sliderVolumeMusic)
                slider.value = _settingsAudio.startMusicVolume;
            foreach (var slider in _sliderVolumeAll)
                slider.value = (!_settingsAudio.efx.mute && !_settingsAudio.music.mute) ? 1f : 0f;
        }

        private void SetSliderVolumeEfx(float arg0)
        {
            _settingsAudio.SetEfxVolume(arg0);
            foreach (var slider in _sliderVolumeEfx) slider.value = arg0;
        }

        private void SetSliderVolumeMusic(float arg0)
        {
            _settingsAudio.SetMusicVolume(arg0);
            foreach (var slider in _sliderVolumeMusic) slider.value = arg0;
        }

        private void SetSliderVolumeAll(float arg0)
        {
            _settingsAudio.SetMusicAndEfxVolume(arg0);

            foreach (var slider in _sliderVolumeEfx) slider.value = arg0;
            foreach (var slider in _sliderVolumeMusic) slider.value = arg0;
        }

        private void SetToggleEfx(bool arg0)
        {
            _settingsAudio.SetEfx(arg0);

            foreach (var toggle in _toggleEfx) toggle.isOn = arg0;
        }

        private void SetToggleMusic(bool arg0)
        {
            _settingsAudio.SetMusic(arg0);

            foreach (var toggle in _toggleMusic) toggle.isOn = arg0;
        }

        private void SetToggleAll(bool arg0)
        {
            if (arg0)
            {
                foreach (var toggle in _toggleEfx) toggle.isOn = true;
                foreach (var toggle in _toggleMusic) toggle.isOn = true;
                foreach (var slider in _sliderVolumeEfx) slider.value = 1f;
                foreach (var slider in _sliderVolumeMusic) slider.value = 1f;

                _settingsAudio.SetMusicAndEfx(true);
            }
            else
            {
                foreach (var toggle in _toggleEfx) toggle.isOn = false;
                foreach (var toggle in _toggleMusic) toggle.isOn = false;
                foreach (var slider in _sliderVolumeEfx) slider.value = 0f;
                foreach (var slider in _sliderVolumeMusic) slider.value = 0f;

                _settingsAudio.SetMusicAndEfx(false);
            }
        }
    }
}
