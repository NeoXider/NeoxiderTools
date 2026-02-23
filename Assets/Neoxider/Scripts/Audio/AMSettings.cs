using Neo;
using Neo.Reactive;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace Neo.Audio
{
    [NeoDoc("Audio/AMSettings.md")]
    [CreateFromMenu("Neoxider/Audio/AMSettings")]
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AMSettings))]
    public class AMSettings : Singleton<AMSettings>
    {
        public const int AudioGroupMaster = 0;
        public const int AudioGroupMusic = 1;
        public const int AudioGroupSfx = 2;

        [Tooltip("Optional mixer for volume control.")]
        public AudioMixer audioMixer;

        [Tooltip("Mixer parameter name for effects volume.")]
        public string EfxVolume = "EfxVolume";

        [Header("Mixer Parameters")] [Tooltip("Mixer parameter name for master volume.")]
        public string MasterVolume = "MasterVolume";

        [Tooltip("Mixer parameter name for music volume.")]
        public string MusicVolume = "MusicVolume";

        [Tooltip("Reactive mute state; subscribe via MuteEfx.OnChanged")]
        public ReactivePropertyBool MuteEfx = new();
        [Tooltip("Reactive mute state; subscribe via MuteMusic.OnChanged")]
        public ReactivePropertyBool MuteMusic = new();
        [Tooltip("Reactive mute state; subscribe via MuteMaster.OnChanged")]
        public ReactivePropertyBool MuteMaster = new();

        /// <summary>Текущее состояние mute (для NeoCondition и рефлексии).</summary>
        public bool MuteEfxValue => MuteEfx.CurrentValue;
        public bool MuteMusicValue => MuteMusic.CurrentValue;
        public bool MuteMasterValue => MuteMaster.CurrentValue;

        public float startEfxVolume = 1f;
        public float startMusicVolume = 0.5f;
        private AM _am;
        private bool _masterMuted;
        private float _savedMasterVolume = 1f;

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
            if (efx == null)
            {
                return;
            }

            efx.mute = !active;
            MuteEfx.Value = efx.mute;
        }

        [Button]
        public void SetMusic(bool active)
        {
            if (music == null)
            {
                return;
            }

            music.mute = !active;
            MuteMusic.Value = music.mute;
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
            if (music == null)
            {
                return;
            }

            float volume = Mathf.Clamp01(percent);
            music.volume = volume;
            SetMixerVolume(audioMixer, MusicVolume, volume);
        }

        [Button]
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

        [Button]
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
        [Button]
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
        [Button]
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

        [Button]
        public void ToggleMusic()
        {
            if (music == null)
            {
                return;
            }

            SetMusic(music.mute);
        }

        [Button]
        public void ToggleEfx()
        {
            if (efx == null)
            {
                return;
            }

            SetEfx(efx.mute);
        }

        [Button]
        public void ToggleMusicAndEfx()
        {
            if (music == null)
            {
                return;
            }

            SetEfx(music.mute);
            SetMusic(music.mute);
        }

        /// <summary>
        ///     Включает или выключает общую громкость (Master) в микшере. Сохраняет текущую громкость при выключении.
        /// </summary>
        [Button("Toggle Master")]
        public void ToggleMaster()
        {
            if (audioMixer == null)
            {
                return;
            }

            if (_masterMuted)
            {
                SetMixerVolume(audioMixer, MasterVolume, _savedMasterVolume);
                _masterMuted = false;
                MuteMaster.Value = false;
            }
            else
            {
                if (audioMixer.GetFloat(MasterVolume, out float db))
                {
                    _savedMasterVolume = db > -80f ? Mathf.Pow(10f, db / 20f) : 1f;
                }

                audioMixer.SetFloat(MasterVolume, -80f);
                _masterMuted = true;
                MuteMaster.Value = true;
            }
        }

        /// <summary>
        ///     Переключает вкл/выкл по группе: 0 = Master, 1 = Music, 2 = Sfx (звуки). Для NeoCondition и кода (один int).
        /// </summary>
        [Button("Toggle Audio (0=Master, 1=Music, 2=Sfx)", 180)]
        public void ToggleAudio(int group)
        {
            switch (group)
            {
                case AudioGroupMaster:
                    ToggleMaster();
                    break;
                case AudioGroupMusic:
                    ToggleMusic();
                    break;
                case AudioGroupSfx:
                    ToggleEfx();
                    break;
                default:
                    Debug.LogWarning($"[AMSettings] ToggleAudio: неизвестная группа {group}. Используйте 0=Master, 1=Music, 2=Sfx.");
                    break;
            }
        }

        /// <summary>
        ///     Переключает все каналы сразу: Master, Music и Sfx (вкл или выкл).
        /// </summary>
        [Button("Toggle All Audio")]
        public void ToggleAllAudio()
        {
            bool anyMuted = _masterMuted || (music != null && music.mute);
            bool turnOn = anyMuted;

            if (!turnOn && audioMixer != null && !_masterMuted && audioMixer.GetFloat(MasterVolume, out float db))
            {
                _savedMasterVolume = db > -80f ? Mathf.Pow(10f, db / 20f) : 1f;
            }

            if (audioMixer != null)
            {
                SetMixerVolume(audioMixer, MasterVolume, turnOn ? _savedMasterVolume : 0f);
                _masterMuted = !turnOn;
                MuteMaster.Value = _masterMuted;
            }

            if (music != null)
            {
                music.mute = !turnOn;
                MuteMusic.Value = music.mute;
            }

            if (efx != null)
            {
                efx.mute = !turnOn;
                MuteEfx.Value = efx.mute;
            }
        }
    }
}