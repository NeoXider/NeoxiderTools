using Neo.Reactive;
using Neo.Save;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Audio;

namespace Neo.Audio
{
    /// <summary>
    ///     Player-facing audio settings on top of <see cref="AM" />: Master/Music/Efx volume and mute, optional
    ///     <see cref="AudioMixer" /> exposed parameters, reactive mute state for UI, and persistence of volumes and
    ///     mutes through <see cref="SaveProvider" />. Binds to AM in <c>Start</c> or through <see cref="Initialize" />.
    /// </summary>
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

        [Tooltip(
            "Enabled by default. Also persists the Music/Efx/Master mute and the UI-channel mute set by Toggle All Audio, " +
            "so a player's 'sound off' survives a restart. Has no effect while Persist Volume is off.")]
        [SerializeField]
        private bool persistMute = true;

        [Tooltip("SaveProvider key for the mute state (int bit mask: 1 = Music, 2 = Efx, 4 = Master, 8 = UI channel).")]
        [SerializeField]
        private string saveKeyMute = "Neo.Audio.AMSettings.Mute";

        public float startEfxVolume = 1f;
        public float startMusicVolume = 0.5f;

        private const int MuteFlagMusic = 1;
        private const int MuteFlagEfx = 2;
        private const int MuteFlagMaster = 4;
        private const int MuteFlagUi = 8;

        private AM _am;
        private bool _masterMuted;
        private float _savedMasterVolume = 1f;
        private float? _pendingMasterFromSave;
        private bool _hadSavedMusic;
        private bool _hadSavedEfx;
        private bool _hadSavedMute;
        private int _savedMuteFlags;
        private bool? _requestedMusicActive;
        private bool? _requestedEfxActive;
        private bool _uiMuted;
        private bool _suppressVolumePersist;
        private bool _started;
        private bool _initializing;
        private AM _initializedAm;
        private bool _volumesLoaded;

        /// <summary>
        ///     Whether Master/Music/Efx volumes are written to and read from <see cref="SaveProvider" />.
        ///     Turning it off also turns mute persistence off.
        /// </summary>
        public bool PersistVolume
        {
            get => persistVolume;
            set => persistVolume = value;
        }

        /// <summary>
        ///     Whether the Music/Efx/Master/UI mute state is persisted. Effective only while
        ///     <see cref="PersistVolume" /> is on.
        /// </summary>
        public bool PersistMute
        {
            get => persistMute;
            set => persistMute = value;
        }

        private bool MutePersistenceEnabled => persistVolume && persistMute && !string.IsNullOrEmpty(saveKeyMute);

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
            if (_volumesLoaded)
            {
                return;
            }

            _volumesLoaded = true;
            if (!persistVolume)
            {
                return;
            }

            if (SaveProvider.HasKey(saveKeyMusic))
            {
                startMusicVolume = Mathf.Clamp01(SaveProvider.GetFloat(saveKeyMusic, startMusicVolume));
                _hadSavedMusic = true;
            }

            if (SaveProvider.HasKey(saveKeyEfx))
            {
                startEfxVolume = Mathf.Clamp01(SaveProvider.GetFloat(saveKeyEfx, startEfxVolume));
                _hadSavedEfx = true;
            }

            if (SaveProvider.HasKey(saveKeyMaster))
            {
                float m = Mathf.Clamp01(SaveProvider.GetFloat(saveKeyMaster, _savedMasterVolume));
                _pendingMasterFromSave = m;
                _savedMasterVolume = m;
            }

            if (MutePersistenceEnabled && SaveProvider.HasKey(saveKeyMute))
            {
                _savedMuteFlags = SaveProvider.GetInt(saveKeyMute, 0);
                _hadSavedMute = true;
            }
        }

        private void Start()
        {
            _started = true;
            if (!TryInitializeManager())
            {
                NeoDiagnostics.LogError("[AMSettings] Audio manager (AM) is not assigned!");
            }
        }

        private void OnEnable()
        {
            if (_started)
            {
                TryInitializeManager();
            }
        }

        private bool TryInitializeManager()
        {
            AM manager = _am != null ? _am : AM.TryGetInstance(out AM current) ? current : AM.I;
            if (manager == null)
            {
                return false;
            }

            Initialize(manager);
            return true;
        }

        /// <summary>
        ///     Binds an explicit manager, including one created after Start. Startup values are applied once
        ///     per manager; re-enabling or initializing the same live binding does not reset playback settings.
        ///     A null manager or nested initialization leaves the current binding unchanged.
        ///     <para>
        ///         Mute on the first binding: a <see cref="SetMusic" /> / <see cref="SetEfx" /> call made before
        ///         any manager was bound (for example from a consumer's <c>Awake</c>) wins, then the persisted mute
        ///         (see <see cref="PersistMute" />), then the state the manager's sources already have. A later
        ///         binding to a replacement manager carries the current Music, Efx and UI-channel mute over.
        ///     </para>
        /// </summary>
        public void Initialize(AM manager)
        {
            if (manager == null || _initializing ||
                (ReferenceEquals(_initializedAm, manager) && efx != null && music != null))
            {
                return;
            }

            _initializing = true;
            bool previousSuppress = _suppressVolumePersist;
            _suppressVolumePersist = true;
            try
            {
                LoadPersistedVolumesFromSave();
                bool firstBinding = ReferenceEquals(_initializedAm, null);
                if (!firstBinding && _am != null)
                {
                    // WHY: the UI channel lives on the manager itself; while the previous one still exists it is
                    // the truth, and a destroyed one leaves the last state recorded in _uiMuted.
                    _uiMuted = _am.IsChannelMuted(SoundChannel.Ui);
                }

                _am = manager;
                manager.StartVolumeEfx = startEfxVolume;
                manager.StartVolumeMusic = startMusicVolume;
                manager.ApplyStartVolumes();

                efx = manager.Efx;
                music = manager.Music;
                ApplyBindingMuteState(manager, firstBinding);

                if (audioMixer != null)
                {
                    // WHY: mixer reads must receive saved levels as well as the newly bound sources.
                    if (_hadSavedMusic)
                    {
                        SetMixerVolume(audioMixer, MusicVolume, Mathf.Clamp01(startMusicVolume));
                    }

                    if (_hadSavedEfx)
                    {
                        SetMixerVolume(audioMixer, EfxVolume, Mathf.Clamp01(startEfxVolume));
                    }

                    if (_pendingMasterFromSave.HasValue)
                    {
                        SetMasterVolume(_pendingMasterFromSave.Value);
                    }

                    if (firstBinding && SavedMuteFlag(MuteFlagMaster) && !_masterMuted)
                    {
                        MuteMasterOnMixer();
                    }
                }

                _pendingMasterFromSave = null;
                _initializedAm = manager;
            }
            finally
            {
                _suppressVolumePersist = previousSuppress;
                _initializing = false;
            }
        }

        /// <summary>
        ///     WHY not a plain unmute: forcing both channels on at startup overwrote a mute the player had chosen
        ///     (persisted) or a consumer had applied in Awake, so "sound off" came back on every launch.
        /// </summary>
        private void ApplyBindingMuteState(AM manager, bool firstBinding)
        {
            bool musicMuted;
            bool efxMuted;
            if (firstBinding)
            {
                musicMuted = _requestedMusicActive.HasValue ? !_requestedMusicActive.Value
                    : _hadSavedMute ? SavedMuteFlag(MuteFlagMusic)
                    : music != null && music.mute;
                efxMuted = _requestedEfxActive.HasValue ? !_requestedEfxActive.Value
                    : _hadSavedMute ? SavedMuteFlag(MuteFlagEfx)
                    : manager.IsChannelMuted(SoundChannel.Sfx);
            }
            else
            {
                musicMuted = _requestedMusicActive.HasValue ? !_requestedMusicActive.Value : MuteMusic.CurrentValue;
                efxMuted = _requestedEfxActive.HasValue ? !_requestedEfxActive.Value : MuteEfx.CurrentValue;
            }

            _requestedMusicActive = null;
            _requestedEfxActive = null;
            SetEfx(!efxMuted);
            SetMusic(!musicMuted);

            if (firstBinding)
            {
                _uiMuted = SavedMuteFlag(MuteFlagUi) || manager.IsChannelMuted(SoundChannel.Ui);
            }

            // WHY on every binding: a replacement manager starts with its UI channel on, so "all audio off"
            // would bring interface clicks back and the next mute change would drop the saved UI bit.
            if (manager.IsChannelMuted(SoundChannel.Ui) != _uiMuted)
            {
                manager.SetChannelMute(SoundChannel.Ui, _uiMuted);
            }
        }

        private bool SavedMuteFlag(int flag)
        {
            return _hadSavedMute && (_savedMuteFlags & flag) != 0;
        }

        private void PersistMuteState()
        {
            // WHY not before the first binding: until a manager is bound the other channels' real state is
            // unknown, and writing them would overwrite the mute the player saved last session.
            if (!MutePersistenceEnabled || _suppressVolumePersist || ReferenceEquals(_initializedAm, null))
            {
                return;
            }

            int flags = 0;
            bool musicMuted = music != null ? music.mute : SavedMuteFlag(MuteFlagMusic);
            bool efxMuted = _am != null || efx != null ? ReadEfxMute() : SavedMuteFlag(MuteFlagEfx);
            bool uiMuted = _am != null ? _am.IsChannelMuted(SoundChannel.Ui) : _uiMuted;
            _uiMuted = uiMuted;
            if (musicMuted)
            {
                flags |= MuteFlagMusic;
            }

            if (efxMuted)
            {
                flags |= MuteFlagEfx;
            }

            if (_masterMuted)
            {
                flags |= MuteFlagMaster;
            }

            if (uiMuted)
            {
                flags |= MuteFlagUi;
            }

            SaveProvider.SetInt(saveKeyMute, flags);
            _savedMuteFlags = flags;
            _hadSavedMute = true;
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

        /// <summary>
        ///     Turns the effects channel on or off. Called before a manager is bound (for example from a consumer's
        ///     <c>Awake</c>), the request is kept and applied by <see cref="Initialize" /> instead of being lost.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void SetEfx(bool active)
        {
            if (efx == null)
            {
                _requestedEfxActive = active;
                MuteEfx.Value = !active;
                return;
            }

            ApplyEfxMute(!active);
            MuteEfx.Value = ReadEfxMute();
            PersistMuteState();
        }

        /// <summary>
        ///     Turns the music channel on or off. Called before a manager is bound, the request is kept and applied
        ///     by <see cref="Initialize" /> instead of being lost.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void SetMusic(bool active)
        {
            if (music == null)
            {
                _requestedMusicActive = active;
                MuteMusic.Value = !active;
                return;
            }

            music.mute = !active;
            MuteMusic.Value = music.mute;
            PersistMuteState();
        }

        [Button(PlayModeOnly = true)]
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
            if (_am != null)
            {
                // WHY: AM owns the music channel. Writing the source directly left the ambience at the old
                // level and was undone by the next track, which AM plays at its own stale channel volume.
                _am.SetMusicVolume(volume);
            }
            else
            {
                music.volume = volume;
            }

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
            if (_am != null)
            {
                // WHY: AM owns the Sfx channel. Writing the source directly left pitched and positional voices
                // that were still ringing at the old level until their next shot.
                _am.SetEfxVolume(volume);
            }
            else
            {
                efx.volume = volume;
            }

            SetMixerVolume(audioMixer, EfxVolume, volume);
            PersistVolumeState();
        }

        [Button]
        public void SetEfxMixerVolume(float percent)
        {
            SetMixerVolume(audioMixer, EfxVolume, Mathf.Clamp01(percent));
            PersistVolumeState();
        }

        /// <summary>
        ///     Sets the Master mixer volume (0..1). While Master is muted the value becomes the level that
        ///     <see cref="ToggleMaster" /> restores, and the mixer stays silent.
        /// </summary>
        [Button]
        public void SetMasterVolume(float percent)
        {
            float volume = Mathf.Clamp01(percent);
            // WHY: writing the mixer while muted made Master audible while MuteMaster still read true, and the
            // next unmute then restored the stale pre-mute level instead of this one.
            if (_masterMuted)
            {
                _savedMasterVolume = volume;
            }
            else
            {
                SetMixerVolume(audioMixer, MasterVolume, volume);
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

            // WHY: the source carries channel x music entry trim; the slider and the save key hold the channel.
            if (_am != null)
            {
                return Mathf.Clamp01(_am.MusicVolume);
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
        ///     Sets volume for any mixer exposed parameter by name (normalized 0-1).
        /// </summary>
        /// <param name="parameterName">Exposed parameter name on the mixer.</param>
        /// <param name="normalizedVolume">Normalized volume (0-1).</param>
        [Button]
        public void SetMixerParameter(string parameterName, float normalizedVolume)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                NeoDiagnostics.LogWarning("[AMSettings] Mixer parameter name is empty.");
                return;
            }

            SetMixerVolume(audioMixer, parameterName, normalizedVolume);
        }

        /// <summary>
        ///     Sets any mixer exposed parameter directly in decibels.
        /// </summary>
        /// <param name="parameterName">Exposed parameter name on the mixer.</param>
        /// <param name="dbValue">Volume in decibels (-80 to 20).</param>
        [Button]
        public void SetMixerParameterDB(string parameterName, float dbValue)
        {
            if (audioMixer == null)
            {
                NeoDiagnostics.LogWarning("[AMSettings] AudioMixer is not assigned.");
                return;
            }

            if (string.IsNullOrEmpty(parameterName))
            {
                NeoDiagnostics.LogWarning("[AMSettings] Mixer parameter name is empty.");
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

        /// <summary>Flips the music mute. Before a manager is bound it flips the pending request instead.</summary>
        [Button(PlayModeOnly = true)]
        public void ToggleMusic()
        {
            SetMusic(music != null ? music.mute : MuteMusic.CurrentValue);
        }

        /// <summary>Flips the effects mute. Before a manager is bound it flips the pending request instead.</summary>
        [Button(PlayModeOnly = true)]
        public void ToggleEfx()
        {
            SetEfx(efx != null ? ReadEfxMute() : MuteEfx.CurrentValue);
        }

        /// <summary>Sets music and effects to the opposite of the current music mute.</summary>
        [Button(PlayModeOnly = true)]
        public void ToggleMusicAndEfx()
        {
            bool turnOn = music != null ? music.mute : MuteMusic.CurrentValue;
            SetEfx(turnOn);
            SetMusic(turnOn);
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
                MuteMasterOnMixer();
            }

            PersistVolumeState();
            PersistMuteState();
        }

        private void MuteMasterOnMixer()
        {
            if (audioMixer.GetFloat(MasterVolume, out float db))
            {
                _savedMasterVolume = db > -80f ? Mathf.Pow(10f, db / 20f) : 1f;
            }

            audioMixer.SetFloat(MasterVolume, -80f);
            _masterMuted = true;
            MuteMaster.Value = true;
        }

        /// <summary>
        ///     Toggles mute by group: 0 = Master, 1 = Music, 2 = Sfx. For NeoCondition and code (single int).
        /// </summary>
        [Button("Toggle Audio (0=Master, 1=Music, 2=Sfx)", 180, PlayModeOnly = true)]
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
                    NeoDiagnostics.LogWarning(
                        $"[AMSettings] ToggleAudio: unknown group {group}. Use 0=Master, 1=Music, 2=Sfx.");
                    break;
            }
        }

        /// <summary>
        ///     Toggles all channels at once: Master, Music, Sfx and AM's UI sound channel (on or off).
        /// </summary>
        [Button("Toggle All Audio")]
        public void ToggleAllAudio()
        {
            bool anyMuted = _masterMuted || (music != null && music.mute) || ReadEfxMute() ||
                            (_am != null ? _am.IsChannelMuted(SoundChannel.Ui) : _uiMuted);
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
                ApplyEfxMute(!turnOn);
                MuteEfx.Value = ReadEfxMute();
            }

            // WHY: "all audio" includes the UI sound channel. Without a mixer - always the case on WebGL - nothing
            // else would silence it, and interface clicks would keep sounding with everything "off".
            _uiMuted = !turnOn;
            if (_am != null)
            {
                _am.SetChannelMute(SoundChannel.Ui, !turnOn);
            }

            PersistVolumeState();
            PersistMuteState();
        }

        /// <summary>
        ///     Mutes the effects channel through AM when it is known, so pitched and positional voices that are
        ///     still ringing go quiet with it; falls back to the source itself before AM is wired.
        /// </summary>
        private void ApplyEfxMute(bool muted)
        {
            if (_am != null)
            {
                _am.SetChannelMute(SoundChannel.Sfx, muted);
            }
            else if (efx != null)
            {
                efx.mute = muted;
            }
        }

        /// <summary>
        ///     The effects mute as AM sees it. WHY not the cached <see cref="efx" />: that reference is taken once in
        ///     Start, and if AM's effects source is replaced later the reactive state would report the old one.
        /// </summary>
        private bool ReadEfxMute()
        {
            if (_am != null)
            {
                return _am.IsChannelMuted(SoundChannel.Sfx);
            }

            return efx != null && efx.mute;
        }
    }
}
