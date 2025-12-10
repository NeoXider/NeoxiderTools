using Neo.Tools;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
#if ODIN_INSPECTOR
#endif

namespace Neo.Audio
{
    [AddComponentMenu("Neo/" + "Audio/" + nameof(AMSettings))]
    public class AMSettings : Singleton<AMSettings>
    {
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
        private AM _am;

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
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetEfx(bool active)
        {
            if (efx == null)
            {
                return;
            }

            efx.mute = !active;
            OnMuteEfx?.Invoke(efx.mute);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetMusic(bool active)
        {
            if (music == null)
            {
                return;
            }

            music.mute = !active;
            OnMuteMusic?.Invoke(music.mute);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetMusicAndEfx(bool active)
        {
            SetEfx(active);
            SetMusic(active);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetMusicAndEfxVolume(float percent)
        {
            SetEfxVolume(percent);
            SetMusicVolume(percent);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetMusicVolume(float percent)
        {
            if (music == null)
            {
                return;
            }

            float volume = Mathf.Clamp01(percent);
            music.volume = volume;
            SetMixerVolume(audioMixer, MusicVolume, volume);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetEfxVolume(float percent)
        {
            if (efx == null)
            {
                return;
            }

            float volume = Mathf.Clamp01(percent);
            efx.volume = volume;
            SetMixerVolume(audioMixer, EfxVolume, volume);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetMasterVolume(float percent)
        {
            float volume = Mathf.Clamp01(percent);
            SetMixerVolume(audioMixer, MasterVolume, volume);
        }

        /// <summary>
        ///     Устанавливает громкость для любого параметра микшера по имени (нормализованное значение 0-1)
        /// </summary>
        /// <param name="parameterName">Имя параметра в микшере</param>
        /// <param name="normalizedVolume">Нормализованное значение громкости (0-1)</param>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetMixerParameter(string parameterName, float normalizedVolume)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                Debug.LogWarning("[AMSettings] Имя параметра микшера не указано!");
                return;
            }

            SetMixerVolume(audioMixer, parameterName, normalizedVolume);
        }

        /// <summary>
        ///     Устанавливает значение для любого параметра микшера напрямую в дБ
        /// </summary>
        /// <param name="parameterName">Имя параметра в микшере</param>
        /// <param name="dbValue">Значение в децибелах (-80 до 20)</param>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SetMixerParameterDB(string parameterName, float dbValue)
        {
            if (audioMixer == null)
            {
                Debug.LogWarning("[AMSettings] AudioMixer не установлен!");
                return;
            }

            if (string.IsNullOrEmpty(parameterName))
            {
                Debug.LogWarning("[AMSettings] Имя параметра микшера не указано!");
                return;
            }

            audioMixer.SetFloat(parameterName, Mathf.Clamp(dbValue, -80f, 20f));
        }

        private void SetMixerVolume(AudioMixer mixer, string parameterName, float normalizedVolume)
        {
            if (mixer == null)
            {
                return;
            }

            float db = normalizedVolume > 0 ? Mathf.Log10(normalizedVolume) * 20 : -80;
            mixer.SetFloat(parameterName, db);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void ToggleMusic()
        {
            if (music == null)
            {
                return;
            }

            SetMusic(music.mute);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void ToggleEfx()
        {
            if (efx == null)
            {
                return;
            }

            SetEfx(efx.mute);
        }
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void ToggleMusicAndEfx()
        {
            if (music == null)
            {
                return;
            }

            SetEfx(music.mute);
            SetMusic(music.mute);
        }
    }
}