using Neo.Reactive;
using Neo.Save;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Audio;

namespace Neo.Audio
{
    [NeoDoc("Audio/AMSettings.md")]
    [CreateFromMenu("Neoxider/Audio/AMSettings")]
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AMSettings))]
    [DefaultExecutionOrder(-100)]
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

        [Header("Persist (0..1)")]
        [Tooltip(
            "Enabled by default. Persists and loads Master/Music/Efx volume (0..1) via Neo.Save.SaveProvider. Clear to skip writing and reading save keys.")]
        [SerializeField]
        private bool persistVolume = true;

        [Tooltip(
            "SaveProvider key for Master (float 0..1). Change the prefix to avoid collisions with other projects.")]
        [SerializeField]
        private string saveKeyMaster = "Neo.Audio.AMSettings.MasterVolume";

        [Tooltip("SaveProvider key for Music (float 0..1).")] [SerializeField]
        private string saveKeyMusic = "Neo.Audio.AMSettings.MusicVolume";

        [Tooltip("SaveProvider key for Efx/SFX (float 0..1).")] [SerializeField]
        private string saveKeyEfx = "Neo.Audio.AMSettings.EfxVolume";

        public float startEfxVolume = 1f;
        public float startMusicVolume = 0.5f;
        private AM _am;
        private bool _masterMuted;
        private float _savedMasterVolume = 1f;
        private float? _pendingMasterFromSave;
        private bool _suppressVolumePersist;

        /// <summary>Current mute state (for NeoCondition and reflection).</summary>
        public bool MuteEfxValue => MuteEfx.CurrentValue;

        /// <summary>Current mute state for music (for NeoCondition and reflection).</summary>
        public bool MuteMusicValue => MuteMusic.CurrentValue;

        /// <summary>Current mute state for master (for NeoCondition and reflection).</summary>
        public bool MuteMasterValue => MuteMaster.CurrentValue;

        public AudioSource efx { get; private set; }
        public AudioSource music { get; private set; }

        public bool IsActiveEfx => efx != null && !efx.mute;
        public bool IsActiveMusic => music != null && !music.mute;

        protected override void Init()
        {
            base.Init();
            LoadPersistedVolumesFromSave();
        }

        private void LoadPersistedVolumesFromSave()
        {
            if (!persistVolume)
            {
                return;
            }

            if (SaveProvider.HasKey(saveKeyMusic))
            {
                startMusicVolume = Mathf.Clamp01(SaveProvider.GetFloat(saveKeyMusic, startMusicVolume));
            }

            if (SaveProvider.HasKey(saveKeyEfx))
            {
                startEfxVolume = Mathf.Clamp01(SaveProvider.GetFloat(saveKeyEfx, startEfxVolume));
            }

            if (SaveProvider.HasKey(saveKeyMaster))
            {
                float m = Mathf.Clamp01(SaveProvider.GetFloat(saveKeyMaster, _savedMasterVolume));
                _pendingMasterFromSave = m;
                _savedMasterVolume = m;
            }
        }

        private void Start()
        {
            _am = AM.I;

            if (_am == null)
            {
                Debug.LogError("[AMSettings] Audio manager (AM) is not assigned!");
                return;
            }

            efx = _am.Efx;
            music = _am.Music;

            _am.startVolumeEfx = startEfxVolume;
            _am.startVolumeMusic = startMusicVolume;

            SetEfx(true);
            SetMusic(true);

            // Apply startup volumes to AudioSources
            _am.ApplyStartVolumes();

            _suppressVolumePersist = true;
            if (_pendingMasterFromSave.HasValue && audioMixer != null)
            {
                SetMasterVolume(_pendingMasterFromSave.Value);
            }

            _pendingMasterFromSave = null;
            _suppressVolumePersist = false;
        }

        private void PersistVolumeState()
        {
            if (!persistVolume || _suppressVolumePersist)
            {
                return;
            }

            SaveProvider.SetFloat(saveKeyMusic, Mathf.Clamp01(GetMusicVolumeNormalized()));
            SaveProvider.SetFloat(saveKeyEfx, Mathf.Clamp01(GetEfxVolumeNormalized()));
            if (audioMixer != null)
            {
                float masterNorm = _masterMuted ? _savedMasterVolume : GetMasterVolumeNormalized();
                SaveProvider.SetFloat(saveKeyMaster, Mathf.Clamp01(masterNorm));
            }
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
            PersistVolumeState();
        }

        [Button]
        public void SetMusicMixerVolume(float percent)
        {
            SetMixerVolume(audioMixer, MusicVolume, Mathf.Clamp01(percent));
            PersistVolumeState();
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
            PersistVolumeState();
        }

        [Button]
        public void SetEfxMixerVolume(float percent)
        {
            SetMixerVolume(audioMixer, EfxVolume, Mathf.Clamp01(percent));
            PersistVolumeState();
        }

        [Button]
        public void SetMasterVolume(float percent)
        {
            float volume = Mathf.Clamp01(percent);
            SetMixerVolume(audioMixer, MasterVolume, volume);
            if (!_masterMuted)
            {
                _savedMasterVolume = volume;
            }

            PersistVolumeState();
        }

        public float GetMasterVolumeNormalized()
        {
            return GetMixerVolumeNormalized(MasterVolume, 1f);
        }

        public float GetMusicVolumeNormalized()
        {
            if (audioMixer != null)
            {
                return GetMixerVolumeNormalized(MusicVolume, Mathf.Clamp01(startMusicVolume));
            }

            if (music != null)
            {
                return Mathf.Clamp01(music.volume);
            }

            return Mathf.Clamp01(startMusicVolume);
        }

        public float GetEfxVolumeNormalized()
        {
            if (audioMixer != null)
            {
                return GetMixerVolumeNormalized(EfxVolume, Mathf.Clamp01(startEfxVolume));
            }

            if (efx != null)
            {
                return Mathf.Clamp01(efx.volume);
            }

            return Mathf.Clamp01(startEfxVolume);
        }

        /// <summary>
        ///     Sets volume for any mixer exposed parameter by name (normalized 0–1).
        /// </summary>
        /// <param name="parameterName">Exposed parameter name on the mixer.</param>
        /// <param name="normalizedVolume">Normalized volume (0–1).</param>
        [Button]
        public void SetMixerParameter(string parameterName, float normalizedVolume)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                Debug.LogWarning("[AMSettings] Mixer parameter name is empty.");
                return;
            }

            SetMixerVolume(audioMixer, parameterName, normalizedVolume);
        }

        /// <summary>
        ///     Sets any mixer exposed parameter directly in decibels.
        /// </summary>
        /// <param name="parameterName">Exposed parameter name on the mixer.</param>
        /// <param name="dbValue">Volume in decibels (−80 to 20).</param>
        [Button]
        public void SetMixerParameterDB(string parameterName, float dbValue)
        {
            if (audioMixer == null)
            {
                Debug.LogWarning("[AMSettings] AudioMixer is not assigned.");
                return;
            }

            if (string.IsNullOrEmpty(parameterName))
            {
                Debug.LogWarning("[AMSettings] Mixer parameter name is empty.");
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

        private float GetMixerVolumeNormalized(string parameterName, float fallback)
        {
            if (audioMixer == null || string.IsNullOrEmpty(parameterName))
            {
                return Mathf.Clamp01(fallback);
            }

            if (!audioMixer.GetFloat(parameterName, out float db))
            {
                return Mathf.Clamp01(fallback);
            }

            if (db <= -80f)
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
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
        ///     Mutes or unmutes Master on the mixer. Stores the current volume while muted.
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

            PersistVolumeState();
        }

        /// <summary>
        ///     Toggles mute by group: 0 = Master, 1 = Music, 2 = Sfx. For NeoCondition and code (single int).
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
                    Debug.LogWarning(
                        $"[AMSettings] ToggleAudio: unknown group {group}. Use 0=Master, 1=Music, 2=Sfx.");
                    break;
            }
        }

        /// <summary>
        ///     Toggles all channels at once: Master, Music, and Sfx (on or off).
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

            PersistVolumeState();
        }
    }
}
