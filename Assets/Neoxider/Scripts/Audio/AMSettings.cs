using Neo.Tools;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace Neo.Audio
{
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AMSettings))]
    public class AMSettings : Singleton<AMSettings>
    {
        private AM _am;

        [Tooltip("Опциональный микшер для управления громкостью.")]
        public AudioMixer audioMixer;

        [Tooltip("Имя параметра для громкости эффектов в микшере.")]
        public string EfxVolume = "EfxVolume";

        [Header("Mixer Parameters")] [Tooltip("Имя параметра для общей громкости в микшере.")]
        public string MasterVolume = "MasterVolume";

        [Tooltip("Имя параметра для громкости музыки в микшере.")]
        public string MusicVolume = "MusicVolume";

        public UnityEvent<bool> OnMuteEfx;
        public UnityEvent<bool> OnMuteMusic;

        public float startEfxVolume = 1;
        public float startMusicVolume = 0.5f;

        public AudioSource efx { get; private set; }
        public AudioSource music { get; private set; }

        public bool IsActiveEfx => efx != null && !efx.mute;
        public bool IsActiveMusic => music != null && !music.mute;


        private void Start()
        {
            _am = AM.I;

            if (_am == null)
            {
                Debug.LogError("[AMSettings] Аудио-менеджер (AM) не назначен!");
                return;
            }

            efx = _am.Efx;
            music = _am.Music;

            _am.startVolumeEfx = startEfxVolume;
            _am.startVolumeMusic = startMusicVolume;

            SetEfx(true);
            SetMusic(true);

            // Применяем стартовые громкости к AudioSource'ам
            _am.ApplyStartVolumes();
        }

        private void OnValidate()
        {
            if (_am != null)
            {
                efx = _am.Efx;
                music = _am.Music;
            }
        }

        [Button]
        public void SetEfx(bool active)
        {
            if (efx == null) return;
            efx.mute = !active;
            OnMuteEfx?.Invoke(efx.mute);
        }

        [Button]
        public void SetMusic(bool active)
        {
            if (music == null) return;
            music.mute = !active;
            OnMuteMusic?.Invoke(music.mute);
        }

        [Button]
        public void SetMusicAndEfx(bool active)
        {
            SetEfx(active);
            SetMusic(active);
        }

        [Button]
        public void SetMusicAndEfxVolume(float percent)
        {
            SetEfxVolume(percent);
            SetMusicVolume(percent);
        }

        [Button]
        public void SetMusicVolume(float percent)
        {
            if (music == null) return;
            var volume = Mathf.Clamp01(percent);
            music.volume = volume;
            SetMixerVolume(audioMixer, MusicVolume, volume);
        }

        [Button]
        public void SetEfxVolume(float percent)
        {
            if (efx == null) return;
            var volume = Mathf.Clamp01(percent);
            efx.volume = volume;
            SetMixerVolume(audioMixer, EfxVolume, volume);
        }

        [Button]
        public void SetMasterVolume(float percent)
        {
            var volume = Mathf.Clamp01(percent);
            SetMixerVolume(audioMixer, MasterVolume, volume);
        }

        private void SetMixerVolume(AudioMixer mixer, string parameterName, float normalizedVolume)
        {
            if (mixer == null) return;

            var db = normalizedVolume > 0 ? Mathf.Log10(normalizedVolume) * 20 : -80;
            mixer.SetFloat(parameterName, db);
        }

        [Button]
        public void ToggleMusic()
        {
            if (music == null) return;
            SetMusic(music.mute);
        }

        [Button]
        public void ToggleEfx()
        {
            if (efx == null) return;
            SetEfx(efx.mute);
        }

        [Button]
        public void ToggleMusicAndEfx()
        {
            if (music == null) return;
            SetEfx(music.mute);
            SetMusic(music.mute);
        }
    }
}