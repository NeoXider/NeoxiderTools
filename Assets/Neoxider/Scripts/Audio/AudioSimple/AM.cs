using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Audio;

namespace Neo.Audio
{
        /// <summary>
        ///     Legacy sound record: one clip plus a volume. Superseded by <see cref="SoundEntry" />, which adds
        ///     an optional id, several clips per entry and per-entry pitch. Existing <c>_sounds</c> arrays are
        ///     migrated automatically and keep working - see <see cref="AM" />.
        /// </summary>
        [Serializable]
        public class Sound
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1;
        }

        /// <summary>
        ///     Central audio manager for sound effects and music.
        ///     <para>
        ///         <b>The record contract.</b> Effects and music share one shape - <see cref="AudioEntry" />:
        ///         an optional <c>id</c>, a <b>set</b> of clips (one is picked at random per play), a volume
        ///         multiplier and an optional pitch range. Entries are addressable by index <i>and</i> by id.
        ///     </para>
        ///     <para>
        ///         <b>Volume multiplies.</b> What you hear is <c>channel volume x entry volume</c>. A music
        ///         channel at <c>0.3</c> playing an entry at <c>1</c> comes out at <c>0.3</c>.
        ///     </para>
        ///     <para>
        ///         <b>Music entries are pools.</b> A pool with several clips starts on a random one; whether it
        ///         then holds that track (<see cref="MusicPoolMode.Loop" />, the default) or rolls on to another
        ///         at the end of the clip (<see cref="MusicPoolMode.Shuffle" />) is a per-pool setting. A
        ///         menu / gameplay / boss set-up is three entries with three ids, and the game only ever says
        ///         <c>AM.I.PlayMusicPool("boss")</c> or <c>AM.I.NextMusicTrack()</c>.
        ///     </para>
        ///     <para>
        ///         <b>Sound channels.</b> Every sound entry plays on a <see cref="SoundChannel" />:
        ///         <see cref="SoundChannel.Sfx" /> (the Efx source, the default) or <see cref="SoundChannel.Ui" />,
        ///         each with its own volume, mute and optional mixer group - see
        ///         <see cref="SetChannelVolume" />. Positional one-shots go through <see cref="PlayAt(string,Vector3)" />,
        ///         and <see cref="TryPlay(string)" /> plays an optional cue without logging.
        ///     </para>
        ///     <para>
        ///         <b>Music changes crossfade by default.</b> Pass <see cref="MusicTransition.Instant" /> for a
        ///         hard cut or <see cref="MusicTransition.Fade" /> for a one-off length. Fades run on unscaled
        ///         time, so <c>Time.timeScale = 0</c> does not freeze them.
        ///     </para>
        /// </summary>
        [NeoDoc("Audio/AM.md")]
        [CreateFromMenu("Neoxider/Audio/AM")]
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AM))]
        public class AM : Singleton<AM>, ISerializationCallbackReceiver
        {
            /// <summary>Id given to the pool migrated from the legacy <c>_randomMusicTracks</c> array.</summary>
            public const string LegacyRandomPoolId = "Random";

            private const int CurrentDataVersion = 1;

            [SerializeField] private AudioSource _efx;
            [Space] [SerializeField] private AudioSource _music;

            [Header("Ambience")]
            [Tooltip("Optional looping bed under the music (night lake, rain, crowd). Created on first use when " +
                     "empty. Plays on the music source's mixer group, follows the music channel volume and mute, " +
                     "at its own relative level below.")]
            [SerializeField]
            private AudioSource _ambience;

            [Tooltip("Ambience level relative to the music channel. Keep it well under the music (0.2 - 0.4).")]
            [Range(0f, 1f)]
            [SerializeField]
            private float _ambienceVolume = 0.3f;

            [Tooltip("Default fade in / out of the ambience, seconds.")]
            [Range(0f, 10f)]
            [SerializeField]
            private float _ambienceFadeDuration = 1.5f;

            [Header("Sounds")]
            [Tooltip("Sound-effect entries. Each has an optional id, one or more clips (a random one is " +
                     "picked per play), a volume multiplier and its own pitch range.")]
            [SerializeField]
            private SoundEntry[] _soundEntries = Array.Empty<SoundEntry>();

            [Header("Music")]
            [Tooltip("Music entries. Each entry is a pool: several clips, one picked at random, then held " +
                     "(Loop) or rolled on at the end of the clip (Shuffle).")]
            [SerializeField]
            private MusicEntry[] _musicEntries = Array.Empty<MusicEntry>();

            [Tooltip("Play music as soon as the manager starts.")]
            [SerializeField]
            private bool _playMusicOnStart = true;

            [Tooltip("Which music entry to start with. Empty = the first entry in the list.")]
            [SerializeField]
            private string _startupMusicId = string.Empty;

            [Header("Music Transitions")]
            [Tooltip("Crossfade music changes instead of cutting. Off makes every change a hard cut unless " +
                     "the call passes MusicTransition.Fade explicitly.")]
            [SerializeField]
            private bool _crossfadeMusic = true;

            [Tooltip("Default crossfade length in seconds. A pool can override it.")]
            [Range(0f, 10f)]
            [SerializeField]
            private float _musicFadeDuration = 0.8f;

            [Header("Sound Pitch (legacy / direct clips)")]
            [Tooltip("Vary the pitch of one-shots played from the legacy _sounds array or straight from an " +
                     "AudioClip. Entries in the Sounds list carry their own pitch settings instead.")]
            [SerializeField]
            private bool _randomizePitch;

            [Tooltip("Lowest pitch multiplier. 1 = the clip's own pitch.")]
            [Range(0.1f, 3f)]
            [SerializeField]
            private float _pitchMin = 0.94f;

            [Tooltip("Highest pitch multiplier.")]
            [Range(0.1f, 3f)]
            [SerializeField]
            private float _pitchMax = 1.06f;

            [Tooltip("How many extra AudioSources may be spawned for pitched one-shots. Pitch on a "
                     + "shared source retunes the sounds already playing on it, so each pitched "
                     + "one-shot needs a source of its own.")]
            [Range(1, 32)]
            [SerializeField]
            private int _pitchVoices = 8;

            [Header("Channels")]
            [Tooltip("Volume of the UI channel (0..1). Entries whose Channel is Ui play at this x their own " +
                     "volume. The Sfx channel volume is the Efx AudioSource volume.")]
            [Range(0f, 1f)]
            [SerializeField]
            private float _uiVolume = 1f;

            [Tooltip("Optional mixer group for the UI channel. Empty = the Efx AudioSource's group, so a project " +
                     "that routes effects through a mixer keeps UI sounds on that bus until it says otherwise.")]
            [SerializeField]
            private AudioMixerGroup _uiMixerGroup;

            [Header("Positional One-Shots")]
            [Tooltip("How many pooled AudioSources PlayAt may use for sounds at a world position. A new " +
                     "positional sound takes a free voice; when all are busy, the next voice in rotation is " +
                     "stopped and reused.")]
            [Range(1, 32)]
            [SerializeField]
            private int _positionalVoices = 8;

            // ---- Legacy serialized data. Kept so old scenes lose nothing; migrated into the entry lists
            // ---- on load (see OnAfterDeserialize) and hidden from the inspector.
            [HideInInspector] [SerializeField] private AudioClip[] _musicClips;
            [HideInInspector] [SerializeField] private Sound[] _sounds;
            [HideInInspector] [SerializeField] private bool _useRandomMusic;
            [HideInInspector] [SerializeField] private AudioClip[] _randomMusicTracks;
            [HideInInspector] [SerializeField] private int _dataVersion;

            // WHY one rotation per channel: a voice can still be ringing an earlier one-shot when the rotation
            // comes back to it, and re-routing it to another channel would drag that tail to the other channel's
            // volume, mute and group (an explosion tail jumping to the UI level). Within one channel a re-mirror
            // writes the same values, so sharing a voice is harmless.
            private AudioSource[] _pitchSources;
            private int _pitchVoiceIndex;
            private AudioSource[] _uiPitchSources;
            private int _uiPitchVoiceIndex;
            private bool _runtimeInitialized;

            // WHY not serialized: the UI channel is a source AM owns, created on first use (also in edit mode,
            // from an inspector button) with DontSave like the pitch voices. Serializing it would add a child to
            // every existing AM scene and prefab the moment the package updates; its authored state lives in
            // _uiVolume / _uiMixerGroup.
            private AudioSource _uiSource;
            private bool _uiMuted;

            // The source that took the most recent one-shot (a channel source or a pitch voice). Read by tests
            // to prove where a plain one-shot was routed, since EditMode cannot listen to it.
            private AudioSource _lastOneShotSource;

            private AudioSource[] _positionalSources;
            private SoundChannel[] _positionalVoiceChannels;
            private float[] _positionalVoiceGains;
            private int _positionalVoiceIndex = -1;

            private SoundEntry _legacySoundAdapter;
            private readonly AudioClip[] _legacySoundAdapterClips = new AudioClip[1];

            // WHY two: one outgoing voice is enough for a single crossfade, but a pool switch landing while
            // the previous one is still fading would have to cut that track dead to reuse the voice - an
            // audible click exactly when the game is at its busiest. Two voices let back-to-back switches
            // each fade out properly; a third switch inside one fade length is rare enough to reuse.
            private readonly AudioSource[] _musicFadeSources = new AudioSource[2];
            private readonly float[] _musicFadeGains = new float[2];
            private readonly float[] _musicFadeEnvelopes = new float[2];
            private int _musicFadeSourceIndex;
            private Coroutine _musicFadeRoutine;
            private Coroutine _musicShuffleRoutine;
            private MusicEntry _currentMusicEntry;
            private MusicEntry _legacyRandomPool;
            private float _currentMusicEntryVolume = 1f;
            private float _currentMusicClipVolume = 1f;
            private float _musicChannelVolume = 1f;
            private float _musicSourceGain = 1f;
            private float _musicEnvelope = 1f;

            /// <summary>Initial volume applied to the sound-effects AudioSource via <see cref="ApplyStartVolumes"/>.</summary>
            public float StartVolumeEfx { get; set; } = 1f;

            /// <summary>Initial volume applied to the music AudioSource via <see cref="ApplyStartVolumes"/>.</summary>
            public float StartVolumeMusic { get; set; } = 1f;

            /// <inheritdoc cref="StartVolumeEfx"/>
            [Obsolete("Use StartVolumeEfx")]
            public float startVolumeEfx { get => StartVolumeEfx; set => StartVolumeEfx = value; }

            /// <inheritdoc cref="StartVolumeMusic"/>
            [Obsolete("Use StartVolumeMusic")]
            public float startVolumeMusic { get => StartVolumeMusic; set => StartVolumeMusic = value; }

#if UNITY_EDITOR
            private bool _editorEnsureSourcesQueued;
#endif

            /// <summary>AudioSource for sound effects.</summary>
            public AudioSource Efx => _efx;

            /// <summary>AudioSource carrying the music that is currently playing.</summary>
            public AudioSource Music => _music;

            /// <summary>Sound-effect entries, in inspector order.</summary>
            public IReadOnlyList<SoundEntry> SoundEntries => _soundEntries ??= Array.Empty<SoundEntry>();

            /// <summary>Music entries (pools), in inspector order.</summary>
            public IReadOnlyList<MusicEntry> MusicEntries => _musicEntries ??= Array.Empty<MusicEntry>();

            /// <summary>The music entry playing right now, or null.</summary>
            public MusicEntry CurrentMusicEntry => _currentMusicEntry;

            /// <summary>Id of the music entry playing right now, or an empty string.</summary>
            public string CurrentMusicId => _currentMusicEntry != null ? _currentMusicEntry.Id : string.Empty;

            /// <summary>
            ///     Music channel volume. The audible level is this multiplied by the current entry's volume.
            /// </summary>
            public float MusicVolume
            {
                get => _musicChannelVolume;
                set => SetVolume(value, false);
            }

            /// <summary>
            ///     Sound-effects channel volume (<see cref="SoundChannel.Sfx" />), multiplied by each entry's own
            ///     volume. Same value as <c>GetChannelVolume(SoundChannel.Sfx)</c>.
            /// </summary>
            public float EfxVolume
            {
                get => _efx != null ? _efx.volume : 1f;
                set => SetVolume(value, true);
            }

            /// <summary>
            ///     UI channel volume (<see cref="SoundChannel.Ui" />), multiplied by each UI entry's own volume.
            ///     Same value as <c>GetChannelVolume(SoundChannel.Ui)</c>.
            /// </summary>
            public float UiVolume
            {
                get => Mathf.Clamp01(_uiVolume);
                set => SetChannelVolume(SoundChannel.Ui, value);
            }

            /// <summary>Default crossfade length in seconds used when a call does not name its own.</summary>
            public float MusicFadeDuration
            {
                get => _musicFadeDuration;
                set => _musicFadeDuration = Mathf.Max(0f, value);
            }

            /// <summary>Whether music changes crossfade by default.</summary>
            public bool CrossfadeMusic
            {
                get => _crossfadeMusic;
                set => _crossfadeMusic = value;
            }

            /// <summary>
            ///     Whether one-shots played from the legacy <c>_sounds</c> array or straight from an
            ///     <see cref="AudioClip" /> are detuned. Entries in <see cref="SoundEntries" /> carry their own
            ///     <see cref="AudioEntry.RandomizePitch" /> instead.
            /// </summary>
            public bool RandomizePitch { get => _randomizePitch; set => _randomizePitch = value; }

            /// <summary>Pitch range used when <see cref="RandomizePitch"/> is on.</summary>
            public void SetPitchRange(float min, float max)
            {
                _pitchMin = Mathf.Min(min, max);
                _pitchMax = Mathf.Max(min, max);
            }

            /// <summary>Raised when music starts playing.</summary>
            public event Action<AudioClip> OnMusicStarted;

            /// <summary>Raised when music stops.</summary>
            public event Action OnMusicStopped;

            /// <summary>
            ///     Raised whenever the track changes <i>inside</i> the current pool - a shuffle advance or a
            ///     <see cref="NextMusicTrack" /> call. Switching pool raises <see cref="OnMusicStarted" />.
            /// </summary>
            public event Action<AudioClip> OnRandomMusicTrackChanged;

            #region Serialization / migration

            /// <inheritdoc />
            public void OnBeforeSerialize()
            {
                // WHY: stamping the version on write is what makes the migration a one-way trip. Without it
                // a user who deliberately empties the new lists would get the legacy arrays re-imported on
                // every load, and there would be no way to say "yes, I meant zero entries".
                _dataVersion = CurrentDataVersion;
            }

            /// <inheritdoc />
            public void OnAfterDeserialize()
            {
                if (_dataVersion >= CurrentDataVersion)
                {
                    return;
                }

                MigrateLegacyData();
                _dataVersion = CurrentDataVersion;
            }

            /// <summary>
            ///     Rebuilds the entry lists from the pre-10.13 fields. Runs once per component, on the first
            ///     load of data saved by an older version; the legacy arrays are left in place untouched, so
            ///     nothing is lost and rolling the package back is still possible.
            /// </summary>
            private void MigrateLegacyData()
            {
                if ((_soundEntries == null || _soundEntries.Length == 0) && _sounds != null && _sounds.Length > 0)
                {
                    SoundEntry[] migrated = new SoundEntry[_sounds.Length];
                    for (int index = 0; index < _sounds.Length; index++)
                    {
                        Sound legacy = _sounds[index];
                        SoundEntry entry = new SoundEntry
                        {
                            Id = string.Empty,
                            Clips = legacy?.clip != null ? new[] { legacy.clip } : Array.Empty<AudioClip>(),
                            // WHY: the old Play(int) read `volume == 0 ? 1 : volume`, so a zeroed record meant
                            // "full", not "silent". Fold that quirk into the data instead of carrying it in
                            // the new playback path, where 0 has to mean 0.
                            Volume = legacy == null || legacy.volume == 0f ? 1f : legacy.volume,
                            // WHY: new entries default to pitch ON, but a migrated project must sound exactly
                            // as it did - so migrated entries inherit the manager's old global switch.
                            RandomizePitch = _randomizePitch
                        };
                        entry.SetPitchRange(_pitchMin, _pitchMax);
                        migrated[index] = entry;
                    }

                    _soundEntries = migrated;
                }

                if (_musicEntries != null && _musicEntries.Length > 0)
                {
                    return;
                }

                List<MusicEntry> music = new List<MusicEntry>();

                // WHY: one entry per clip, in the original order, so PlayMusic(int) keeps resolving to the
                // same track it always did.
                if (_musicClips != null)
                {
                    for (int index = 0; index < _musicClips.Length; index++)
                    {
                        AudioClip clip = _musicClips[index];
                        MusicEntry entry = new MusicEntry
                        {
                            Id = clip != null ? clip.name : string.Empty,
                            Clips = clip != null ? new[] { clip } : Array.Empty<AudioClip>(),
                            Mode = MusicPoolMode.Loop
                        };
                        music.Add(entry);
                    }
                }

                // WHY: the random-track array is exactly a shuffle pool, appended after the indexed clips so
                // it cannot shift their indices.
                if (_randomMusicTracks != null && _randomMusicTracks.Length > 0)
                {
                    music.Add(new MusicEntry
                    {
                        Id = LegacyRandomPoolId,
                        Clips = (AudioClip[])_randomMusicTracks.Clone(),
                        Mode = MusicPoolMode.Shuffle
                    });

                    if (_useRandomMusic && string.IsNullOrEmpty(_startupMusicId))
                    {
                        _startupMusicId = LegacyRandomPoolId;
                    }
                }

                if (music.Count > 0)
                {
                    _musicEntries = music.ToArray();
                }
            }

            #endregion

            #region Lifecycle

            private void OnValidate()
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    QueueEnsureSourcesInEditor();
                    return;
                }
#endif

                EnsureSources();

                // WHY: the UI channel volume and group are AM fields, not a source the inspector edits directly,
                // so a Play Mode tweak has to be pushed onto the live source and the voices ringing on it.
                if (_uiSource != null)
                {
                    RefreshChannelVoices(SoundChannel.Ui);
                }
            }

            protected override void Init()
            {
                base.Init();
            }

            private void Start()
            {
                EnsureRuntimeInitialized();
                PlayStartupMusic();
            }

            protected override void OnDestroy()
            {
                if (_ambienceFadeRoutine != null)
                {
                    StopCoroutine(_ambienceFadeRoutine);
                    _ambienceFadeRoutine = null;
                }

                if (_ambienceOwned)
                {
                    DestroyVoice(_ambience);
                    _ambience = null;
                    _ambienceOwned = false;
                }

                StopMusicRoutines();
                DestroyOwnedVoices();
                base.OnDestroy();
            }

            /// <summary>
            ///     Destroys the helper sources AM created for itself (pitch voices, positional voices, the UI channel
            ///     source, the music crossfade voices). They are DontSave children, and removing only the AM
            ///     component would otherwise leave them behind in the hierarchy.
            /// </summary>
            private void DestroyOwnedVoices()
            {
                DestroyVoices(_pitchSources);
                _pitchSources = null;
                DestroyVoices(_uiPitchSources);
                _uiPitchSources = null;
                DestroyVoices(_positionalSources);
                _positionalSources = null;
                _positionalVoiceChannels = null;
                _positionalVoiceGains = null;
                DestroyVoices(_musicFadeSources);
                DestroyVoice(_uiSource);
                _uiSource = null;
                _lastOneShotSource = null;
            }

            private void EnsureRuntimeInitialized()
            {
                if (_runtimeInitialized)
                {
                    return;
                }

                _runtimeInitialized = true;
                EnsureSources();

                // WHY: the music AudioSource volume authored in the inspector IS the music channel. Before
                // this, the channel started at 1 regardless, so a project that had turned the source down to
                // 0.3 and relied on EnableRandomMusic - which never touched the volume - suddenly played its
                // soundtrack at full. Adopting the source value makes the inspector setting mean what it
                // looks like it means, and makes SetMusicVolume's later writes land on the same number.
                if (_music != null)
                {
                    _musicChannelVolume = Mathf.Clamp01(_music.volume);
                }
            }

            private void PlayStartupMusic()
            {
                if (!_playMusicOnStart)
                {
                    return;
                }

                // WHY: script execution order is not guaranteed. Another component's Start() may already have
                // asked for a specific pool; the startup track must not stamp over a deliberate choice.
                if (_currentMusicEntry != null || (_music != null && _music.isPlaying))
                {
                    return;
                }

                if (_musicEntries != null && _musicEntries.Length > 0)
                {
                    if (!string.IsNullOrEmpty(_startupMusicId))
                    {
                        PlayMusicPool(_startupMusicId, MusicTransition.Instant);
                        return;
                    }

                    PlayMusicPool(0, MusicTransition.Instant);
                    return;
                }

                // WHY: components whose data was injected at runtime (or by a test) never went through
                // deserialization, so migration never ran. Honour the raw legacy fields too.
                if (_useRandomMusic && _randomMusicTracks != null && _randomMusicTracks.Length > 0)
                {
#pragma warning disable CS0618
                    EnableRandomMusic();
#pragma warning restore CS0618
                    return;
                }

                if (_musicClips != null && _musicClips.Length > 0)
                {
                    PlayMusic(0);
                }
            }

            private bool EnsureSources()
            {
                bool created = false;

                if (_music == null)
                {
                    CreateMusic();
                    created = true;
                }

                if (_efx == null)
                {
                    CreateEfx();
                    created = true;
                }

                return created;
            }

#if UNITY_EDITOR
            private void QueueEnsureSourcesInEditor()
            {
                if (_editorEnsureSourcesQueued)
                {
                    return;
                }

                _editorEnsureSourcesQueued = true;
                UnityEditor.EditorApplication.delayCall += EnsureSourcesInEditorDelayed;
            }

            private void EnsureSourcesInEditorDelayed()
            {
                _editorEnsureSourcesQueued = false;

                if (this == null || Application.isPlaying ||
                    UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (EnsureSources())
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
#endif

            private void CreateMusic()
            {
                GameObject obj = new("Music");
                obj.transform.SetParent(transform, false);

                _music = obj.AddComponent<AudioSource>();
                _music.loop = true;
                _music.volume = .7f;
                _music.priority = 126;
            }

            private void CreateEfx()
            {
                GameObject obj = new("Efx");
                obj.transform.SetParent(transform, false);

                _efx = obj.AddComponent<AudioSource>();
                _efx.playOnAwake = false;
                _efx.loop = false;
                _efx.volume = 1;
                _efx.priority = 127;
            }

            #endregion

            #region Sound effects

            /// <summary>Plays a sound-effect entry by index at the given volume.</summary>
            /// <param name="id">Entry index in the Sounds list.</param>
            /// <param name="volume">Playback volume (0-1), replacing the entry's own volume multiplier.</param>
            [Button]
            public void Play(int id, float volume)
            {
                Play(id, SoundOptions.Volume(volume));
            }

            /// <summary>Plays a sound-effect entry by index using its own volume multiplier.</summary>
            /// <param name="id">Entry index in the Sounds list.</param>
            [Button]
            public void Play(int id)
            {
                Play(id, default(SoundOptions));
            }

            /// <summary>Plays a sound-effect entry by index with per-call overrides.</summary>
            /// <param name="id">Entry index in the Sounds list.</param>
            /// <param name="options">Overrides for this play only; see <see cref="SoundOptions" />.</param>
            public void Play(int id, SoundOptions options)
            {
                SoundEntry entry = ResolveSoundEntry(id);
                if (entry == null)
                {
                    return;
                }

                PlayEntryOneShot(entry, options, false);
            }

            /// <summary>
            ///     Plays a sound-effect entry by its id, using the entry's own volume multiplier. Single
            ///     string argument on purpose: a UnityEvent can call this with no code.
            /// </summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            public void Play(string id)
            {
                Play(id, default(SoundOptions));
            }

            /// <summary>Plays a sound-effect entry by its id at the given volume.</summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <param name="volume">Entry-volume override (0-1), still multiplied by the effects channel.</param>
            public void Play(string id, float volume)
            {
                Play(id, SoundOptions.Volume(volume));
            }

            /// <summary>Plays a sound-effect entry by its id with per-call overrides.</summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <param name="options">Overrides for this play only; see <see cref="SoundOptions" />.</param>
            public void Play(string id, SoundOptions options)
            {
                EnsureRuntimeInitialized();

                if (_efx == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return;
                }

                SoundEntry entry = GetSound(id);
                if (entry == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] No sound entry with id '{id}'.");
                    return;
                }

                PlayEntryOneShot(entry, options, false);
            }

            /// <summary>
            ///     Plays a sound-effect entry by id when there is something to play, and says whether there was.
            ///     Unlike <see cref="Play(string)" /> it never logs: an empty id, an id with no entry and an entry
            ///     without a usable clip all simply return false. Use it for cues that are optional by design;
            ///     for an entry that exists but is waiting for its sound, the entry's
            ///     <see cref="AudioEntry.IsPlaceholder" /> flag silences every call path instead.
            /// </summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <returns>True when a clip was started.</returns>
            public bool TryPlay(string id)
            {
                return TryPlay(id, default(SoundOptions));
            }

            /// <summary><see cref="TryPlay(string)" /> with an entry-volume override.</summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <param name="volume">Entry-volume override (0..2), still multiplied by the channel.</param>
            /// <returns>True when a clip was started.</returns>
            public bool TryPlay(string id, float volume)
            {
                return TryPlay(id, SoundOptions.Volume(volume));
            }

            /// <summary><see cref="TryPlay(string)" /> with per-call overrides.</summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <param name="options">Overrides for this play only; see <see cref="SoundOptions" />.</param>
            /// <returns>True when a clip was started.</returns>
            public bool TryPlay(string id, SoundOptions options)
            {
                if (string.IsNullOrEmpty(id))
                {
                    return false;
                }

                EnsureRuntimeInitialized();
                if (_efx == null)
                {
                    return false;
                }

                SoundEntry entry = GetSound(id);
                return entry != null && PlayEntryOneShot(entry, options, true);
            }

            /// <summary>Plays a clip directly, without adding it to the Sounds list.</summary>
            /// <param name="clip">Clip to play.</param>
            /// <param name="volume">Playback volume (0-1).</param>
            public void Play(AudioClip clip, float volume)
            {
                EnsureRuntimeInitialized();

                if (_efx == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return;
                }

                if (clip == null)
                {
                    NeoDiagnostics.LogWarning("[AM] AudioClip is null.");
                    return;
                }

                PlayOneShotInternal(clip, Mathf.Clamp(volume, 0f, AudioEntry.MaxVolume), _randomizePitch,
                    _pitchMin, _pitchMax, SoundChannel.Sfx);
            }

            /// <summary>Plays a clip directly at full volume.</summary>
            /// <param name="clip">Clip to play.</param>
            public void Play(AudioClip clip)
            {
                Play(clip, 1f);
            }

            /// <summary>
            ///     Plays a sound-effect entry at a world position - an impact, an explosion, a door - using the
            ///     entry's clips, volume, pitch range, channel and <see cref="SoundEntry.SpatialBlend" />.
            ///     <para>
            ///         The sound takes a voice from a pool of <c>_positionalVoices</c> AudioSources: a free one when
            ///         there is one, otherwise the next one in rotation is cut and reused. Nothing is allocated per
            ///         call once the pool is warm. The audible level is <c>channel x entry x clip trim</c>, capped
            ///         at 1 (the voice's <see cref="AudioSource.volume" />), and follows later channel changes.
            ///     </para>
            /// </summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <param name="position">World position the sound comes from.</param>
            /// <returns>
            ///     The pooled voice playing the sound, or null when nothing played. Later positional plays reuse
            ///     it, so read it now and do not keep the reference.
            /// </returns>
            public AudioSource PlayAt(string id, Vector3 position)
            {
                return PlayAt(id, position, default(SoundOptions));
            }

            /// <summary><see cref="PlayAt(string,Vector3)" /> with an entry-volume override.</summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <param name="position">World position the sound comes from.</param>
            /// <param name="volume">Entry-volume override (0..2), still multiplied by the channel.</param>
            /// <returns>The pooled voice playing the sound, or null when nothing played.</returns>
            public AudioSource PlayAt(string id, Vector3 position, float volume)
            {
                return PlayAt(id, position, SoundOptions.Volume(volume));
            }

            /// <summary>
            ///     <see cref="PlayAt(string,Vector3)" /> with per-call overrides. <see cref="SoundOptions.Spatial" />
            ///     replaces the entry's spatial blend for this play.
            /// </summary>
            /// <param name="id">Entry id, as typed in the inspector.</param>
            /// <param name="position">World position the sound comes from.</param>
            /// <param name="options">Overrides for this play only; see <see cref="SoundOptions" />.</param>
            /// <returns>The pooled voice playing the sound, or null when nothing played.</returns>
            public AudioSource PlayAt(string id, Vector3 position, SoundOptions options)
            {
                EnsureRuntimeInitialized();

                if (_efx == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return null;
                }

                SoundEntry entry = GetSound(id);
                if (entry == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] No sound entry with id '{id}'.");
                    return null;
                }

                return PlayEntryAt(entry, position, options, false);
            }

            /// <summary>Plays a clip directly at a world position, without an entry.</summary>
            /// <param name="clip">Clip to play.</param>
            /// <param name="position">World position the sound comes from.</param>
            /// <param name="volume">Volume multiplier (0..2) of the channel.</param>
            /// <param name="spatialBlend">0 = 2D, 1 = fully 3D.</param>
            /// <param name="channel">Volume channel.</param>
            /// <returns>The pooled voice playing the sound, or null when nothing played.</returns>
            public AudioSource PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f,
                SoundChannel channel = SoundChannel.Sfx)
            {
                EnsureRuntimeInitialized();

                if (_efx == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return null;
                }

                if (clip == null)
                {
                    NeoDiagnostics.LogWarning("[AM] AudioClip is null.");
                    return null;
                }

                return PlayPositionalInternal(clip, position, Mathf.Clamp(volume, 0f, AudioEntry.MaxVolume),
                    _randomizePitch, _pitchMin, _pitchMax, spatialBlend, channel);
            }

            /// <summary>Finds a sound entry by id, or null.</summary>
            public SoundEntry GetSound(string id)
            {
                return FindById(_soundEntries, id);
            }

            /// <summary>Replaces the sound-effect entries at runtime.</summary>
            public void SetSoundEntries(params SoundEntry[] entries)
            {
                _soundEntries = entries ?? Array.Empty<SoundEntry>();
            }

            /// <summary>
            ///     Resolves an index against the entry list, falling back to the legacy <c>_sounds</c> array
            ///     for components whose data was never deserialized (runtime-built managers, tests).
            /// </summary>
            private SoundEntry ResolveSoundEntry(int index)
            {
                EnsureRuntimeInitialized();

                if (_efx == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return null;
                }

                if (_soundEntries != null && _soundEntries.Length > 0)
                {
                    if (index < 0 || index >= _soundEntries.Length)
                    {
                        NeoDiagnostics.LogWarning($"[AM] Sound ID {index} is out of range.");
                        return null;
                    }

                    SoundEntry entry = _soundEntries[index];
                    if (entry == null || entry.IsEmpty)
                    {
                        if (entry == null || !entry.IsPlaceholder)
                        {
                            NeoDiagnostics.LogWarning($"[AM] Sound clip at ID {index} is null.");
                        }

                        return null;
                    }

                    return entry;
                }

                if (_sounds == null || index < 0 || index >= _sounds.Length)
                {
                    NeoDiagnostics.LogWarning($"[AM] Sound ID {index} is out of range.");
                    return null;
                }

                Sound legacy = _sounds[index];
                if (legacy == null || legacy.clip == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] Sound clip at ID {index} is null.");
                    return null;
                }

                // WHY: one reused adapter rather than a fresh one per call. Play(int) is a hot path - a hit
                // sound can fire dozens of times a second - and this legacy branch would otherwise allocate
                // an entry plus a clip array on every shot. The adapter is consumed synchronously inside
                // this call, so reusing it is safe.
                _legacySoundAdapter ??= new SoundEntry();
                _legacySoundAdapterClips[0] = legacy.clip;
                _legacySoundAdapter.Clips = _legacySoundAdapterClips;
                _legacySoundAdapter.Volume = legacy.volume == 0f ? 1f : legacy.volume;
                _legacySoundAdapter.RandomizePitch = _randomizePitch;
                _legacySoundAdapter.SetPitchRange(_pitchMin, _pitchMax);
                return _legacySoundAdapter;
            }

            /// <summary>
            ///     Applies the per-call overrides on top of the entry and fires the shot. The entry itself is
            ///     never written to, so an override cannot leak into the next play.
            /// </summary>
            /// <param name="quiet">True for the TryPlay path: report a missing clip by the return value only.</param>
            /// <returns>True when a clip was started.</returns>
            private bool PlayEntryOneShot(SoundEntry entry, SoundOptions options, bool quiet)
            {
                AudioClip clip = PickClip(entry, options);
                if (clip == null)
                {
                    WarnNoUsableClip(entry, quiet);
                    return false;
                }

                bool randomizePitch = options.RandomizePitchOverride ?? entry.RandomizePitch;
                float pitchMin = options.PitchMinOverride ?? entry.PitchMin;
                float pitchMax = options.PitchMaxOverride ?? entry.PitchMax;

                return PlayOneShotInternal(clip, EntryGain(entry, options), randomizePitch, pitchMin, pitchMax,
                    options.ChannelOverride ?? entry.Channel);
            }

            /// <summary>The positional counterpart of <see cref="PlayEntryOneShot" />.</summary>
            private AudioSource PlayEntryAt(SoundEntry entry, Vector3 position, SoundOptions options, bool quiet)
            {
                AudioClip clip = PickClip(entry, options);
                if (clip == null)
                {
                    WarnNoUsableClip(entry, quiet);
                    return null;
                }

                bool randomizePitch = options.RandomizePitchOverride ?? entry.RandomizePitch;
                float pitchMin = options.PitchMinOverride ?? entry.PitchMin;
                float pitchMax = options.PitchMaxOverride ?? entry.PitchMax;
                float spatialBlend = options.SpatialBlendOverride ?? entry.SpatialBlend;

                return PlayPositionalInternal(clip, position, EntryGain(entry, options), randomizePitch, pitchMin,
                    pitchMax, spatialBlend, options.ChannelOverride ?? entry.Channel);
            }

            private static AudioClip PickClip(AudioEntry entry, SoundOptions options)
            {
                return options.ClipIndexOverride.HasValue
                    ? entry.ClipAt(options.ClipIndexOverride.Value)
                    : entry.NextClip();
            }

            /// <summary>
            ///     The entry multiplier of one shot: entry volume (or the per-call override) x the trim of the
            ///     clip that was just picked.
            /// </summary>
            /// <remarks>
            ///     WHY the clip trim multiplies rather than replaces: a per-call override says how loud this
            ///     ENTRY should be, while the trim describes how hot that particular take was recorded. Both are
            ///     true at once, and the channel volume multiplies in on top.
            ///     WHY the ceiling is MaxVolume and not 1: entry volume and clip trim are MULTIPLIERS of the
            ///     channel, and both are authored up to 2 precisely so a quietly mastered sample can be lifted.
            ///     Clamping their product at 1 threw that away - an entry set to 2 played at 1.
            /// </remarks>
            private static float EntryGain(AudioEntry entry, SoundOptions options)
            {
                return Mathf.Clamp((options.VolumeOverride ?? entry.Volume) * entry.LastClipVolume, 0f,
                    AudioEntry.MaxVolume);
            }

            private static void WarnNoUsableClip(AudioEntry entry, bool quiet)
            {
                // WHY: a placeholder is empty on purpose and TryPlay promised silence; everything else that
                // reaches here is an entry that lost its clip, which is worth saying.
                if (quiet || entry.IsPlaceholder)
                {
                    return;
                }

                NeoDiagnostics.LogWarning($"[AM] Sound entry '{entry.Id}' has no usable clip.");
            }

            /// <summary>
            ///     Plays a one-shot on a channel, routing it through a spare AudioSource when the pitch is
            ///     randomised.
            ///     <para>
            ///         WHY the extra sources: <see cref="AudioSource.pitch"/> applies to the whole source, so
            ///         raising it before a PlayOneShot also retunes every one-shot still ringing on that source.
            ///         With effects firing on top of each other - the usual case for a hit sound - that is
            ///         audible. Each pitched shot therefore gets its own voice, round-robin over a small pool.
            ///     </para>
            ///     <para>
            ///         The volume passed here is the entry multiplier; <see cref="AudioSource.PlayOneShot" />
            ///         scales it by the source volume, which is the channel volume. That product is the
            ///         channel x entry contract.
            ///     </para>
            /// </summary>
            /// <returns>True when the clip was started.</returns>
            private bool PlayOneShotInternal(AudioClip clip, float volume, bool randomizePitch, float pitchMin,
                float pitchMax, SoundChannel channel)
            {
                channel = NormalizeChannel(channel);
                AudioSource channelSource = ResolveChannelSource(channel);
                if (channelSource == null)
                {
                    return false;
                }

                if (!randomizePitch)
                {
                    channelSource.PlayOneShot(clip, volume);
                    _lastOneShotSource = channelSource;
                    return true;
                }

                AudioSource voice = channel == SoundChannel.Ui
                    ? NextPitchVoice(ref _uiPitchSources, ref _uiPitchVoiceIndex, channelSource, "UiPitchVoice_")
                    : NextPitchVoice(ref _pitchSources, ref _pitchVoiceIndex, channelSource, "PitchVoice_");
                if (voice == null)
                {
                    channelSource.PlayOneShot(clip, volume);
                    _lastOneShotSource = channelSource;
                    return true;
                }

                // WHY mirror on every shot (10.12.0): SetEfxVolume - the slider every game exposes - writes
                // _efx.volume, and a voice that mirrored it once would keep playing at whatever the volume
                // happened to be when it was created. The voice belongs to this channel's own rotation, so the
                // mirror never moves a tail that is still ringing from another channel.
                MirrorChannel(voice, channelSource);
                voice.pitch = UnityEngine.Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax));
                voice.PlayOneShot(clip, volume);
                _lastOneShotSource = voice;
                return true;
            }

            /// <summary>
            ///     Advances one channel's pitch-voice rotation and returns the voice to use, creating it on first
            ///     use as a DontSave child of the channel's source.
            /// </summary>
            private AudioSource NextPitchVoice(ref AudioSource[] pool, ref int index, AudioSource channelSource,
                string namePrefix)
            {
                if (channelSource == null)
                {
                    return null;
                }

                int count = Mathf.Max(1, _pitchVoices);
                if (pool == null)
                {
                    pool = new AudioSource[count];
                }
                else if (pool.Length != count)
                {
                    // WHY: copy the voices over instead of dropping the array. A pool resized from the
                    // Inspector during Play Mode would otherwise orphan its GameObjects and build a new set
                    // next to them.
                    AudioSource[] resized = new AudioSource[count];
                    for (int slot = 0; slot < pool.Length; slot++)
                    {
                        if (slot < count)
                        {
                            resized[slot] = pool[slot];
                        }
                        else
                        {
                            DestroyVoice(pool[slot]);
                        }
                    }

                    pool = resized;
                }

                index = (Mathf.Max(0, index) + 1) % count;
                if (pool[index] != null)
                {
                    return pool[index];
                }

                GameObject go = new GameObject(namePrefix + index) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(channelSource.transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                MirrorChannel(source, channelSource);

                pool[index] = source;
                return source;
            }

            /// <summary>
            ///     Plays a clip on a pooled positional voice placed at <paramref name="position" />.
            ///     <para>
            ///         WHY Play and not PlayOneShot: a voice is one sound. Moving a voice to a new point while an
            ///         earlier one-shot was still ringing on it would teleport that sound too, so the voice is
            ///         stopped and given the new clip instead - which is also why a positional voice can be shared
            ///         by the channels: nothing of the previous sound survives the reuse. WHY its own pool and not
            ///         the pitch voices: those stay parked at their channel source, and a 3D shot moving one of
            ///         them would drag the 2D shots ringing on it along.
            ///     </para>
            /// </summary>
            private AudioSource PlayPositionalInternal(AudioClip clip, Vector3 position, float gain,
                bool randomizePitch, float pitchMin, float pitchMax, float spatialBlend, SoundChannel channel)
            {
                channel = NormalizeChannel(channel);
                AudioSource channelSource = ResolveChannelSource(channel);
                if (channelSource == null || _efx == null)
                {
                    return null;
                }

                int slot = NextPositionalSlot();
                AudioSource voice = _positionalSources[slot];

                voice.Stop();
                MirrorChannel(voice, channelSource);

                // WHY 3D settings from the Efx source whatever the channel: the channel decides how LOUD a sound
                // is and where it is mixed; how it falls off in the world is one authored setting for every
                // positional sound, and the UI source is a 2D source with no meaningful 3D settings of its own.
                voice.rolloffMode = _efx.rolloffMode;
                voice.minDistance = _efx.minDistance;
                voice.maxDistance = _efx.maxDistance;
                voice.spread = _efx.spread;

                // WHY no doppler: the voice is teleported to the new point and started in the same frame, so the
                // jump reads as a huge velocity and Unity bends the first frames of the shot.
                voice.dopplerLevel = 0f;
                voice.spatialBlend = Mathf.Clamp01(spatialBlend);
                voice.transform.position = position;
                voice.pitch = randomizePitch
                    ? UnityEngine.Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax))
                    : 1f;
                voice.loop = false;
                voice.clip = clip;
                // WHY the product on AudioSource.volume: Play has no per-shot scale the way PlayOneShot does, so
                // channel x entry lands on the voice itself and RefreshChannelVoices recomputes it from the gain
                // kept below when the channel changes mid-sound.
                voice.volume = Mathf.Clamp01(channelSource.volume * gain);
                _positionalVoiceChannels[slot] = channel;
                _positionalVoiceGains[slot] = gain;
                voice.Play();
                return voice;
            }

            /// <summary>
            ///     Picks the positional voice for the next sound: the first free one after the last used, or -
            ///     when every voice is busy - the next one in rotation, which is cut. Creates voices on first use.
            /// </summary>
            private int NextPositionalSlot()
            {
                int count = Mathf.Max(1, _positionalVoices);
                if (_positionalSources == null || _positionalSources.Length != count)
                {
                    ResizePositionalPool(count);
                }

                int start = Mathf.Clamp(_positionalVoiceIndex, -1, count - 1);
                for (int offset = 1; offset <= count; offset++)
                {
                    int index = (start + offset) % count;
                    AudioSource candidate = _positionalSources[index];
                    if (candidate != null && candidate.isPlaying)
                    {
                        continue;
                    }

                    if (candidate == null)
                    {
                        CreatePositionalVoice(index);
                    }

                    _positionalVoiceIndex = index;
                    return index;
                }

                int reuse = (start + 1) % count;
                _positionalVoiceIndex = reuse;
                return reuse;
            }

            private void ResizePositionalPool(int count)
            {
                AudioSource[] sources = new AudioSource[count];
                SoundChannel[] channels = new SoundChannel[count];
                float[] gains = new float[count];

                if (_positionalSources != null)
                {
                    for (int index = 0; index < _positionalSources.Length; index++)
                    {
                        if (_positionalSources[index] == null)
                        {
                            continue;
                        }

                        if (index < count)
                        {
                            sources[index] = _positionalSources[index];
                            channels[index] = _positionalVoiceChannels[index];
                            gains[index] = _positionalVoiceGains[index];
                        }
                        else
                        {
                            DestroyVoice(_positionalSources[index]);
                        }
                    }
                }

                _positionalSources = sources;
                _positionalVoiceChannels = channels;
                _positionalVoiceGains = gains;
            }

            private void CreatePositionalVoice(int index)
            {
                GameObject go = new GameObject("PositionalVoice_" + index) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                AudioSource voice = go.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;

                if (_efx != null)
                {
                    voice.priority = _efx.priority;

                    // WHY once, here: the curve getter allocates, so it cannot run per shot. A custom rolloff is
                    // authored on the Efx source and is the one positional sounds should fall off with.
                    if (_efx.rolloffMode == AudioRolloffMode.Custom)
                    {
                        voice.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                            _efx.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
                    }
                }

                _positionalSources[index] = voice;
            }

            private static void DestroyVoice(AudioSource voice)
            {
                if (voice == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(voice.gameObject);
                }
                else
                {
                    DestroyImmediate(voice.gameObject);
                }
            }

            private static void DestroyVoices(AudioSource[] voices)
            {
                if (voices == null)
                {
                    return;
                }

                for (int index = 0; index < voices.Length; index++)
                {
                    DestroyVoice(voices[index]);
                    voices[index] = null;
                }
            }

            /// <summary>
            ///     Folds any value that is not <see cref="SoundChannel.Ui" /> - a future or corrupted int cast to
            ///     the enum - into <see cref="SoundChannel.Sfx" />, once, before it is stored or compared.
            /// </summary>
            private static SoundChannel NormalizeChannel(SoundChannel channel)
            {
                return channel == SoundChannel.Ui ? SoundChannel.Ui : SoundChannel.Sfx;
            }

            /// <summary>
            ///     Copies a channel source's routing and mix settings onto a voice.
            /// </summary>
            /// <remarks>
            ///     WHY on every shot and not only at creation: <see cref="SetEfxVolume" /> - the volume
            ///     slider every game exposes - writes <c>_efx.volume</c>. A voice that mirrored it once
            ///     would keep playing at whatever the volume was when it happened to be created, so
            ///     turning effects down would silence the plain one-shots and leave the pitched ones loud.
            /// </remarks>
            private static void MirrorChannel(AudioSource voice, AudioSource channelSource)
            {
                voice.outputAudioMixerGroup = channelSource.outputAudioMixerGroup;
                voice.volume = channelSource.volume;
                voice.mute = channelSource.mute;
                voice.spatialBlend = channelSource.spatialBlend;
                voice.bypassEffects = channelSource.bypassEffects;
                voice.bypassListenerEffects = channelSource.bypassListenerEffects;
                voice.bypassReverbZones = channelSource.bypassReverbZones;
                voice.ignoreListenerPause = channelSource.ignoreListenerPause;
            }

            #endregion

            #region Channels

            /// <summary>
            ///     Volume of a channel (0..1). <see cref="SoundChannel.Sfx" /> is the Efx AudioSource volume
            ///     (<see cref="EfxVolume" />); <see cref="SoundChannel.Ui" /> is <see cref="UiVolume" />.
            /// </summary>
            public float GetChannelVolume(SoundChannel channel)
            {
                return channel == SoundChannel.Ui ? Mathf.Clamp01(_uiVolume) : EfxVolume;
            }

            /// <summary>
            ///     Sets a channel volume (clamped to 0..1). What a cue on that channel is heard at is this x the
            ///     entry volume x the clip trim. Voices already ringing on the channel follow at once.
            /// </summary>
            /// <param name="channel">Channel to change.</param>
            /// <param name="volume">Volume (0-1).</param>
            public void SetChannelVolume(SoundChannel channel, float volume)
            {
                if (channel != SoundChannel.Ui)
                {
                    SetVolume(volume, true);
                    return;
                }

                _uiVolume = Mathf.Clamp01(volume);
                RefreshChannelVoices(SoundChannel.Ui);
            }

            /// <summary>Sets the UI channel volume. Single float argument on purpose: a UI slider's UnityEvent can call it.</summary>
            /// <param name="volume">Volume (0-1).</param>
            public void SetUiVolume(float volume)
            {
                SetChannelVolume(SoundChannel.Ui, volume);
            }

            /// <summary>Whether a channel is muted. The Sfx channel reads the Efx AudioSource's mute.</summary>
            public bool IsChannelMuted(SoundChannel channel)
            {
                return channel == SoundChannel.Ui ? _uiMuted : _efx != null && _efx.mute;
            }

            /// <summary>
            ///     Mutes or unmutes a channel without touching its volume. The Sfx channel writes the Efx
            ///     AudioSource's mute - the same flag <c>AMSettings.SetEfx</c> flips.
            /// </summary>
            public void SetChannelMute(SoundChannel channel, bool muted)
            {
                if (channel == SoundChannel.Ui)
                {
                    _uiMuted = muted;
                    RefreshChannelVoices(SoundChannel.Ui);
                    return;
                }

                EnsureRuntimeInitialized();
                if (_efx != null)
                {
                    _efx.mute = muted;
                    RefreshChannelVoices(SoundChannel.Sfx);
                }
            }

            /// <summary>
            ///     The mixer group configured for a channel, or null. The Sfx channel reads the Efx AudioSource's
            ///     output group. A Ui channel without a group of its own (null here) routes through the Sfx group.
            /// </summary>
            public AudioMixerGroup GetChannelMixerGroup(SoundChannel channel)
            {
                if (channel == SoundChannel.Ui)
                {
                    return _uiMixerGroup;
                }

                return _efx != null ? _efx.outputAudioMixerGroup : null;
            }

            /// <summary>
            ///     Routes a channel through a mixer group. Null on the Ui channel means "the same group as Sfx".
            ///     The volume API does not depend on the mixer, so it keeps working on platforms without one
            ///     (WebGL).
            /// </summary>
            public void SetChannelMixerGroup(SoundChannel channel, AudioMixerGroup group)
            {
                if (channel == SoundChannel.Ui)
                {
                    _uiMixerGroup = group;
                    RefreshChannelVoices(SoundChannel.Ui);
                    return;
                }

                EnsureRuntimeInitialized();
                if (_efx == null)
                {
                    return;
                }

                _efx.outputAudioMixerGroup = group;
                RefreshChannelVoices(SoundChannel.Sfx);

                // WHY: a Ui channel without a group of its own follows the Sfx group, so it moves with it.
                if (_uiMixerGroup == null && _uiSource != null)
                {
                    RefreshChannelVoices(SoundChannel.Ui);
                }
            }

            /// <summary>
            ///     The AudioSource that carries a channel's plain (unpitched, 2D) one-shots: <see cref="Efx" /> for
            ///     Sfx, a source AM creates on first use (not saved with the scene) for Ui. Its volume, mute and
            ///     group are owned by the channel API above, which rewrites them on every change.
            /// </summary>
            public AudioSource GetChannelSource(SoundChannel channel)
            {
                EnsureRuntimeInitialized();
                return ResolveChannelSource(channel);
            }

            private AudioSource ResolveChannelSource(SoundChannel channel)
            {
                // WHY anything but Ui falls back to Sfx: a value cast from a future or corrupted int must still
                // play somewhere audible rather than throw on a hot path.
                return channel == SoundChannel.Ui ? EnsureUiSource() : _efx;
            }

            private AudioSource EnsureUiSource()
            {
                if (_uiSource == null)
                {
                    GameObject go = new GameObject("UiChannel") { hideFlags = HideFlags.DontSave };
                    go.transform.SetParent(transform, false);
                    _uiSource = go.AddComponent<AudioSource>();
                    _uiSource.playOnAwake = false;
                    _uiSource.loop = false;

                    // WHY 2D whatever the Efx source says: an interface sound has no place in the world.
                    _uiSource.spatialBlend = 0f;

                    // WHY copied once, not on every sync: these are the settings a game may want to change on the
                    // UI source alone (ignoreListenerPause, so menu clicks are heard while the game is paused).
                    if (_efx != null)
                    {
                        _uiSource.priority = _efx.priority;
                        _uiSource.bypassEffects = _efx.bypassEffects;
                        _uiSource.bypassListenerEffects = _efx.bypassListenerEffects;
                        _uiSource.bypassReverbZones = _efx.bypassReverbZones;
                        _uiSource.ignoreListenerPause = _efx.ignoreListenerPause;
                    }
                }

                SyncUiSource();
                return _uiSource;
            }

            private void SyncUiSource()
            {
                if (_uiSource == null)
                {
                    return;
                }

                _uiSource.volume = Mathf.Clamp01(_uiVolume);
                _uiSource.mute = _uiMuted;
                _uiSource.outputAudioMixerGroup = _uiMixerGroup != null
                    ? _uiMixerGroup
                    : _efx != null ? _efx.outputAudioMixerGroup : null;
            }

            /// <summary>
            ///     Pushes a channel's current volume, mute and group onto its source, onto the channel's own pitch
            ///     voices and onto every positional voice playing on that channel, so a settings slider moves
            ///     sounds that are already ringing.
            /// </summary>
            private void RefreshChannelVoices(SoundChannel channel)
            {
                channel = NormalizeChannel(channel);
                AudioSource channelSource;
                AudioSource[] pitchPool;
                if (channel == SoundChannel.Ui)
                {
                    SyncUiSource();
                    channelSource = _uiSource;
                    pitchPool = _uiPitchSources;
                }
                else
                {
                    channelSource = _efx;
                    pitchPool = _pitchSources;
                }

                if (channelSource == null)
                {
                    return;
                }

                if (pitchPool != null)
                {
                    for (int index = 0; index < pitchPool.Length; index++)
                    {
                        AudioSource voice = pitchPool[index];
                        if (voice == null)
                        {
                            continue;
                        }

                        voice.volume = channelSource.volume;
                        voice.mute = channelSource.mute;
                        voice.outputAudioMixerGroup = channelSource.outputAudioMixerGroup;
                    }
                }

                if (_positionalSources != null)
                {
                    for (int index = 0; index < _positionalSources.Length; index++)
                    {
                        AudioSource voice = _positionalSources[index];
                        if (voice == null || _positionalVoiceChannels[index] != channel)
                        {
                            continue;
                        }

                        voice.volume = Mathf.Clamp01(channelSource.volume * _positionalVoiceGains[index]);
                        voice.mute = channelSource.mute;
                        voice.outputAudioMixerGroup = channelSource.outputAudioMixerGroup;
                    }
                }
            }

            #endregion

            #region Music

            /// <summary>
            ///     Starts the music pool with this id, crossfading. Calling it for the pool that is already
            ///     playing does nothing - no restart, no fade into itself - so a screen is free to re-assert
            ///     its music as often as it likes.
            ///     <para>Single string argument on purpose: this is the call a UnityEvent can make with no code.</para>
            /// </summary>
            /// <param name="id">Music entry id, as typed in the inspector.</param>
            public void PlayMusicPool(string id)
            {
                PlayMusicPool(id, MusicTransition.Default);
            }

            /// <summary>Starts the music pool with this id using an explicit transition.</summary>
            /// <param name="id">Music entry id, as typed in the inspector.</param>
            /// <param name="transition">
            ///     <see cref="MusicTransition.Instant" /> for a hard cut, <see cref="MusicTransition.Fade" />
            ///     for a one-off length.
            /// </param>
            public void PlayMusicPool(string id, MusicTransition transition)
            {
                PlayMusicPool(id, new MusicOptions { Transition = transition });
            }

            /// <summary>Starts the music pool with this id, with per-call overrides.</summary>
            /// <param name="id">Music entry id, as typed in the inspector.</param>
            /// <param name="options">Overrides for this change only; see <see cref="MusicOptions" />.</param>
            public void PlayMusicPool(string id, MusicOptions options)
            {
                EnsureRuntimeInitialized();

                MusicEntry entry = GetMusicPool(id);
                if (entry == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] No music entry with id '{id}'.");
                    return;
                }

                // WHY before PlayMusicEntry: a placeholder pool has nothing to play yet, and switching to it
                // would drop the current pool for silence. Keep what is playing and say nothing.
                if (entry.IsPlaceholder && entry.IsEmpty)
                {
                    return;
                }

                PlayMusicEntry(entry, options, false);
            }

            /// <summary>Starts the music pool at this index, crossfading.</summary>
            /// <param name="index">Entry index in the Music list.</param>
            public void PlayMusicPool(int index)
            {
                PlayMusicPool(index, default(MusicOptions));
            }

            /// <summary>Starts the music pool at this index using an explicit transition.</summary>
            /// <param name="index">Entry index in the Music list.</param>
            /// <param name="transition">How the change should sound.</param>
            public void PlayMusicPool(int index, MusicTransition transition)
            {
                PlayMusicPool(index, new MusicOptions { Transition = transition });
            }

            /// <summary>Starts the music pool at this index, with per-call overrides.</summary>
            /// <param name="index">Entry index in the Music list.</param>
            /// <param name="options">Overrides for this change only; see <see cref="MusicOptions" />.</param>
            public void PlayMusicPool(int index, MusicOptions options)
            {
                EnsureRuntimeInitialized();

                MusicEntry entry = ResolveMusicEntry(index);
                if (entry == null)
                {
                    return;
                }

                PlayMusicEntry(entry, options, false);
            }

            /// <summary>Plays music by index at the given volume.</summary>
            /// <param name="id">Entry index in the Music list.</param>
            /// <param name="volume">Volume multiplier (0-1) replacing the entry's own.</param>
            [Button]
            public void PlayMusic(int id, float volume)
            {
                PlayMusicPool(id, MusicOptions.Volume(volume));
            }

            /// <summary>
            ///     Plays music by index using the entry's own volume multiplier. Identical to
            ///     <see cref="PlayMusicPool(int)" />, which is the name to prefer in new code - a music entry
            ///     is a pool, and the pool vocabulary is what the id and transition overloads use. This one
            ///     stays because existing projects call it.
            /// </summary>
            /// <param name="id">Entry index in the Music list.</param>
            [Button]
            public void PlayMusic(int id)
            {
                PlayMusicPool(id);
            }

            /// <summary>Plays a clip as music, crossfading unless told otherwise.</summary>
            /// <param name="clip">Clip to play.</param>
            /// <param name="volume">Volume multiplier (0-1), still multiplied by the music channel.</param>
            /// <param name="options">Overrides for this change only; see <see cref="MusicOptions" />.</param>
            public void PlayMusicByClip(AudioClip clip, float volume, MusicOptions options = default)
            {
                EnsureRuntimeInitialized();

                if (_music == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Music AudioSource is not initialized.");
                    return;
                }

                if (clip == null)
                {
                    NeoDiagnostics.LogWarning("[AM] AudioClip is null.");
                    return;
                }

                options.VolumeOverride = Mathf.Clamp(volume, 0f, AudioEntry.MaxVolume);
                MusicEntry entry = new MusicEntry(string.Empty, clip) { Mode = MusicPoolMode.Loop };
                PlayMusicEntry(entry, options, false);
            }

            /// <summary>Plays a clip as music at full volume, crossfading unless told otherwise.</summary>
            /// <param name="clip">Clip to play.</param>
            public void PlayMusicByClip(AudioClip clip)
            {
                PlayMusicByClip(clip, 1f);
            }

            /// <summary>Plays a clip as music with an explicit transition.</summary>
            /// <param name="clip">Clip to play.</param>
            /// <param name="transition">How the change should sound.</param>
            public void PlayMusicByClip(AudioClip clip, MusicTransition transition)
            {
                PlayMusicByClip(clip, 1f, new MusicOptions { Transition = transition });
            }

            /// <summary>
            ///     Moves to another random track of the pool that is playing, guaranteed to differ from the
            ///     current one while the pool holds more than one usable clip. This is the call a game makes on
            ///     its own beat - a wave boundary, a boss entrance - rather than waiting for the clip to end.
            ///     <para>Parameterless on purpose: a UnityEvent can call this with no code.</para>
            /// </summary>
            public void NextMusicTrack()
            {
                TryNextMusicTrack(default);
            }

            /// <summary>Moves to another random track of the current pool with an explicit transition.</summary>
            /// <param name="transition">How the change should sound.</param>
            public void NextMusicTrack(MusicTransition transition)
            {
                TryNextMusicTrack(new MusicOptions { Transition = transition });
            }

            /// <summary>
            ///     <see cref="NextMusicTrack()" /> for callers that want to know whether anything happened -
            ///     it returns false when no pool is playing or the pool has nothing else to offer.
            /// </summary>
            /// <param name="options">Overrides for this change only; see <see cref="MusicOptions" />.</param>
            /// <returns>True when a different track was started.</returns>
            public bool TryNextMusicTrack(MusicOptions options = default)
            {
                if (_currentMusicEntry == null)
                {
                    NeoDiagnostics.LogWarning("[AM] NextMusicTrack: no music pool is playing.");
                    return false;
                }

                if (_currentMusicEntry.ClipCount <= 1 && !options.TrackIndexOverride.HasValue)
                {
                    NeoDiagnostics.LogWarning(
                        $"[AM] NextMusicTrack: pool '{_currentMusicEntry.Id}' has nothing else to play.");
                    return false;
                }

                AudioClip previous = _music != null ? _music.clip : null;
                float volume = Mathf.Clamp(options.VolumeOverride ?? _currentMusicEntryVolume, 0f, AudioEntry.MaxVolume);
                if (!StartMusicClip(_currentMusicEntry, options, volume))
                {
                    return false;
                }

                bool changed = _music != null && _music.clip != previous;
                if (changed)
                {
                    OnRandomMusicTrackChanged?.Invoke(_music.clip);
                }

                return changed;
            }

            /// <summary>Finds a music entry by id, or null.</summary>
            public MusicEntry GetMusicPool(string id)
            {
                return FindById(_musicEntries, id);
            }

            /// <summary>Replaces the music entries at runtime. Does not change what is currently playing.</summary>
            public void SetMusicEntries(params MusicEntry[] entries)
            {
                _musicEntries = entries ?? Array.Empty<MusicEntry>();
            }

            /// <summary>Returns the clip currently assigned to the music source, or null.</summary>
            public AudioClip GetCurrentMusicClip()
            {
                return _music != null ? _music.clip : null;
            }

            /// <summary>
            ///     Releases exact caller-owned clips from music (including outgoing fades), ambience,
            ///     positional voices and legacy random-track references, without destroying clips or sources.
            ///     Other clips continue playing. Use PlayAt with spatialBlend = 0 for independently releasable
            ///     2D effects: overlapping PlayOneShot calls on a shared source cannot be stopped selectively.
            /// </summary>
            public void ReleaseClips(params AudioClip[] clips)
            {
                if (clips == null || clips.Length == 0)
                {
                    return;
                }

                bool wasPlaying = _currentMusicEntry != null || (_music != null && _music.isPlaying);
                bool releasedMusic = ReleaseSourceClip(_music, clips);
                if (releasedMusic)
                {
                    StopShuffleWatchdog();
                    _currentMusicEntry = null;
                    _currentMusicEntryVolume = 1f;
                    _currentMusicClipVolume = 1f;
                    _musicSourceGain = 1f;
                    _musicEnvelope = 1f;
                    _useRandomMusic = false;
                }

                // Keep the shared fade routine: unrelated outgoing tracks must finish their own envelopes.
                for (int index = 0; index < _musicFadeSources.Length; index++)
                {
                    if (ReleaseSourceClip(_musicFadeSources[index], clips))
                    {
                        _musicFadeGains[index] = 0f;
                        _musicFadeEnvelopes[index] = 0f;
                    }
                }

                if (ReleaseSourceClip(_ambience, clips))
                {
                    if (_ambienceFadeRoutine != null)
                    {
                        StopCoroutine(_ambienceFadeRoutine);
                        _ambienceFadeRoutine = null;
                    }

                    _ambienceFade = 0f;
                    ApplyAmbienceVolume();
                }

                if (_positionalSources != null)
                {
                    for (int index = 0; index < _positionalSources.Length; index++)
                    {
                        if (ReleaseSourceClip(_positionalSources[index], clips))
                        {
                            _positionalVoiceGains[index] = 0f;
                        }
                    }
                }

                _randomMusicTracks = DetachReleasedClips(_randomMusicTracks, clips);
                if (_legacyRandomPool != null)
                {
                    _legacyRandomPool.Clips = DetachReleasedClips(_legacyRandomPool.Clips, clips);
                }

                if (releasedMusic && wasPlaying)
                {
                    OnMusicStopped?.Invoke();
                }
            }

            private static bool ReleaseSourceClip(AudioSource source, AudioClip[] clips)
            {
                if (source == null || !ContainsClip(clips, source.clip))
                {
                    return false;
                }

                source.Stop();
                source.clip = null;
                return true;
            }

            private static bool ContainsClip(AudioClip[] clips, AudioClip clip)
            {
                if (clip == null)
                {
                    return false;
                }

                for (int index = 0; index < clips.Length; index++)
                {
                    if (ReferenceEquals(clips[index], clip))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static AudioClip[] DetachReleasedClips(AudioClip[] source, AudioClip[] clips)
            {
                if (source == null)
                {
                    return null;
                }

                AudioClip[] detached = null;
                for (int index = 0; index < source.Length; index++)
                {
                    if (ContainsClip(clips, source[index]))
                    {
                        detached ??= (AudioClip[])source.Clone();
                        detached[index] = null;
                    }
                }

                return detached ?? source;
            }

            /// <summary>
            ///     Stops music, fading out unless told otherwise, and raises <see cref="OnMusicStopped" />.
            /// </summary>
            public void StopMusic()
            {
                StopMusic(MusicTransition.Default);
            }

            /// <summary>Stops music with an explicit transition and raises <see cref="OnMusicStopped" />.</summary>
            /// <param name="transition">
            ///     <see cref="MusicTransition.Instant" /> to cut the music dead, otherwise a fade-out.
            /// </param>
            public void StopMusic(MusicTransition transition)
            {
                bool wasPlaying = (_music != null && _music.isPlaying) || _currentMusicEntry != null;

                _useRandomMusic = false;
                StopShuffleWatchdog();
                _currentMusicEntry = null;
                _currentMusicEntryVolume = 1f;
                _currentMusicClipVolume = 1f;

                float duration = ResolveFadeDuration(null, transition);
                if (CanFade(duration))
                {
                    RestartFade(FadeOutAndStopRoutine(duration));
                }
                else
                {
                    StopFadeRoutine();
                    if (_music != null)
                    {
                        _music.Stop();
                    }

                    _musicSourceGain = 1f;
                    _musicEnvelope = 1f;
                    StopFadeSource();
                }

                if (wasPlaying)
                {
                    OnMusicStopped?.Invoke();
                }
            }

            /// <summary>
            ///     Resolves an index against the entry list, falling back to the legacy <c>_musicClips</c>
            ///     array for components whose data was never deserialized.
            /// </summary>
            private MusicEntry ResolveMusicEntry(int index)
            {
                if (_music == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Music AudioSource is not initialized.");
                    return null;
                }

                if (_musicEntries != null && _musicEntries.Length > 0)
                {
                    if (index < 0 || index >= _musicEntries.Length)
                    {
                        NeoDiagnostics.LogWarning($"[AM] Music clip ID {index} is out of range.");
                        return null;
                    }

                    MusicEntry entry = _musicEntries[index];
                    if (entry == null || entry.IsEmpty)
                    {
                        if (entry == null || !entry.IsPlaceholder)
                        {
                            NeoDiagnostics.LogWarning($"[AM] Music clip at ID {index} is null.");
                        }

                        return null;
                    }

                    return entry;
                }

                if (_musicClips == null || index < 0 || index >= _musicClips.Length)
                {
                    NeoDiagnostics.LogWarning($"[AM] Music clip ID {index} is out of range.");
                    return null;
                }

                if (_musicClips[index] == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] Music clip at ID {index} is null.");
                    return null;
                }

                return new MusicEntry(string.Empty, _musicClips[index]) { Mode = MusicPoolMode.Loop };
            }

            /// <summary>
            ///     The one place a music entry becomes sound. Skips the work entirely when the requested pool
            ///     is already the playing one, so a screen that re-asserts its music every frame costs nothing
            ///     and never fades a track into itself.
            /// </summary>
            private void PlayMusicEntry(MusicEntry entry, MusicOptions options, bool silent)
            {
                if (_music == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Music AudioSource is not initialized.");
                    return;
                }

                // WHY MaxVolume and not 1: this is the ENTRY multiplier, not the audible level. The audible
                // level is the product in MusicTargetVolume, which is clamped to 1 because AudioSource.volume
                // genuinely is 0..1. Capping the multiplier here as well made a pool authored at 2 - the way
                // to lift a quiet track against a channel the player has turned down - play at 1.
                float volume = Mathf.Clamp(options.VolumeOverride ?? entry.Volume, 0f, AudioEntry.MaxVolume);

                // WHY: re-asserting the music of a screen is a normal thing for game code to do every frame.
                // Restarting the pool there would stutter the track and fade it into itself; an explicit
                // track override is the one case where the caller really does mean "change now".
                bool sameEntry = ReferenceEquals(_currentMusicEntry, entry) &&
                                 _music.clip != null &&
                                 Mathf.Approximately(_currentMusicEntryVolume, volume) &&
                                 !options.TrackIndexOverride.HasValue;
                if (sameEntry)
                {
                    return;
                }

                if (StartMusicClip(entry, options, volume) && !silent)
                {
                    OnMusicStarted?.Invoke(_music.clip);
                }
            }

            /// <summary>
            ///     Picks the next clip of the entry and hands it to the transition machinery.
            ///     <para>
            ///         The proposed entry volume is committed only after selecting a usable clip, so rejected
            ///         requests leave the playing pool, gain and event state unchanged.
            ///     </para>
            /// </summary>
            private bool StartMusicClip(MusicEntry entry, MusicOptions options, float volume)
            {
                AudioClip clip = options.TrackIndexOverride.HasValue
                    ? entry.ClipAt(options.TrackIndexOverride.Value)
                    : entry.NextClip();
                if (clip == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] Music entry '{entry.Id}' has no usable clip.");
                    return false;
                }

                // Commit pool and gain only after selection succeeds, preserving the playing track on failure.
                if (!ReferenceEquals(_currentMusicEntry, entry))
                {
                    _useRandomMusic = false;
                }

                _currentMusicEntry = entry;
                _currentMusicEntryVolume = volume;

                // Tracks of one pool are rarely mastered to the same level; the trim of whichever one was
                // picked joins the product before the fade starts chasing it.
                _currentMusicClipVolume = entry.LastClipVolume;
                float outgoingGain = _musicSourceGain;
                _musicSourceGain = _currentMusicEntryVolume * _currentMusicClipVolume;

                // WHY: a shuffle pool must not loop its clip, or the watchdog never sees the end. A pool with
                // a single clip has nothing to shuffle to, so it loops regardless of mode.
                bool loop = entry.IsLooping || entry.ClipCount <= 1;
                float duration = ResolveFadeDuration(entry, options.Transition);

                SwitchMusicSource(clip, entry.NextPitch(), loop, duration, outgoingGain);
                RestartShuffleWatchdog(entry);
                return true;
            }

            /// <summary>
            ///     Crossfade core. The primary <c>_music</c> source always carries the <i>incoming</i> track -
            ///     that is what keeps <see cref="Music" />, <see cref="GetCurrentMusicClip" /> and every
            ///     existing volume tweak pointing at the track you can hear. The outgoing track is handed to a
            ///     second, hidden source at the exact playback position it had reached, and faded there.
            /// </summary>
            private void SwitchMusicSource(AudioClip clip, float pitch, bool loop, float duration, float outgoingGain)
            {
                float target = MusicTargetVolume;

                if (!CanFade(duration))
                {
                    StopFadeRoutine();
                    StopFadeSource();
                    _music.Stop();
                    _music.clip = clip;
                    _music.pitch = pitch;
                    _music.loop = loop;
                    _musicEnvelope = 1f;
                    _music.volume = target;
                    _music.Play();
                    return;
                }

                if (_music.isPlaying && _music.clip != null)
                {
                    HandOffToFadeSource(outgoingGain);
                }

                _music.clip = clip;
                _music.pitch = pitch;
                _music.loop = loop;
                _musicEnvelope = 0f;
                _music.volume = 0f;
                _music.Play();

                RestartFade(CrossfadeRoutine(duration));
            }

            /// <summary>
            ///     Moves the currently audible track onto a hidden voice, at the exact playback position it
            ///     had reached, so the fade continues the performance instead of restarting it.
            /// </summary>
            private void HandOffToFadeSource(float outgoingGain)
            {
                AudioSource fade = NextFadeSource();
                if (fade == null)
                {
                    return;
                }

                fade.Stop();
                fade.clip = _music.clip;
                fade.pitch = _music.pitch;
                fade.loop = false;
                fade.volume = _music.volume;
                int slot = Array.IndexOf(_musicFadeSources, fade);
                _musicFadeGains[slot] = outgoingGain;
                _musicFadeEnvelopes[slot] = _musicEnvelope;
                // WHY mirror the mix and not just the clip: a muted or mixer-routed music channel that came
                // back un-muted for the length of every crossfade would be worse than not fading at all.
                fade.outputAudioMixerGroup = _music.outputAudioMixerGroup;
                fade.mute = _music.mute;
                fade.spatialBlend = _music.spatialBlend;
                fade.bypassEffects = _music.bypassEffects;
                fade.bypassListenerEffects = _music.bypassListenerEffects;
                fade.bypassReverbZones = _music.bypassReverbZones;
                fade.ignoreListenerPause = _music.ignoreListenerPause;
                fade.Play();
                fade.time = Mathf.Min(_music.time, Mathf.Max(0f, _music.clip.length - 0.01f));

                _music.Stop();
            }

            /// <summary>
            ///     Picks the outgoing voice to use, preferring one that is not currently carrying a fade, so
            ///     two switches in quick succession do not cut each other off.
            /// </summary>
            private AudioSource NextFadeSource()
            {
                if (_music == null)
                {
                    return null;
                }

                for (int offset = 0; offset < _musicFadeSources.Length; offset++)
                {
                    int index = (_musicFadeSourceIndex + offset) % _musicFadeSources.Length;
                    AudioSource candidate = _musicFadeSources[index];
                    if (candidate != null && candidate.isPlaying)
                    {
                        continue;
                    }

                    _musicFadeSourceIndex = (index + 1) % _musicFadeSources.Length;
                    return candidate != null ? candidate : CreateFadeSource(index);
                }

                // Every voice is busy - a third switch inside one fade length. Reuse the oldest.
                int reuse = _musicFadeSourceIndex;
                _musicFadeSourceIndex = (reuse + 1) % _musicFadeSources.Length;
                return _musicFadeSources[reuse] != null ? _musicFadeSources[reuse] : CreateFadeSource(reuse);
            }

            private AudioSource CreateFadeSource(int index)
            {
                GameObject go = new GameObject("MusicCrossfade_" + index) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(_music.transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.priority = _music.priority;
                _musicFadeSources[index] = source;
                return source;
            }

            /// <summary>
            ///     Both halves of the crossfade in one routine, on <see cref="Time.unscaledDeltaTime" /> so a
            ///     paused game (<c>Time.timeScale = 0</c>) still hears the transition finish. The target level
            ///     is re-read every frame, so changing the music volume mid-fade lands where you expect.
            /// </summary>
            private IEnumerator CrossfadeRoutine(float duration)
            {
                // WHY capture per voice: a switch landing mid-fade leaves an older voice part-way down, and
                // restarting its ramp from full would make that track swell back up before dying.
                float[] outStart = new float[_musicFadeSources.Length];
                for (int index = 0; index < _musicFadeSources.Length; index++)
                {
                    outStart[index] = _musicFadeEnvelopes[index];
                }

                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    _musicEnvelope = t;

                    for (int index = 0; index < _musicFadeSources.Length; index++)
                    {
                        if (_musicFadeSources[index] != null)
                        {
                            _musicFadeEnvelopes[index] = outStart[index] * (1f - t);
                        }
                    }

                    ApplyMusicVolumes();
                    yield return null;
                }

                _musicEnvelope = 1f;
                if (_music != null)
                {
                    _music.volume = MusicTargetVolume;
                }

                StopFadeSource();
                _musicFadeRoutine = null;
            }

            private IEnumerator FadeOutAndStopRoutine(float duration)
            {
                float start = _musicEnvelope;
                float[] outStart = new float[_musicFadeSources.Length];
                for (int index = 0; index < _musicFadeSources.Length; index++)
                {
                    outStart[index] = _musicFadeEnvelopes[index];
                }

                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    _musicEnvelope = start * (1f - t);

                    for (int index = 0; index < _musicFadeSources.Length; index++)
                    {
                        if (_musicFadeSources[index] != null)
                        {
                            _musicFadeEnvelopes[index] = outStart[index] * (1f - t);
                        }
                    }

                    ApplyMusicVolumes();
                    yield return null;
                }

                _musicSourceGain = 1f;
                _musicEnvelope = 1f;
                if (_music != null)
                {
                    _music.Stop();
                    _music.volume = MusicTargetVolume;
                }

                StopFadeSource();
                _musicFadeRoutine = null;
            }

            /// <summary>
            ///     Watches the tail of a shuffle pool's track and crossfades into the next one so the change
            ///     lands exactly at the end instead of after a gap. Loop pools do not need it and do not get it.
            /// </summary>
            private IEnumerator ShuffleWatchdogRoutine(MusicEntry entry, float lead)
            {
                while (true)
                {
                    yield return null;

                    if (!ReferenceEquals(_currentMusicEntry, entry) || _music == null || _music.clip == null)
                    {
                        _musicShuffleRoutine = null;
                        yield break;
                    }

                    float pitch = Mathf.Abs(_music.pitch) < 0.01f ? 1f : Mathf.Abs(_music.pitch);
                    float remaining = (_music.clip.length - _music.time) / pitch;
                    if (remaining > Mathf.Max(0.05f, lead))
                    {
                        continue;
                    }

                    _musicShuffleRoutine = null;
                    AudioClip previous = _music.clip;
                    StartMusicClip(entry, default, _currentMusicEntryVolume);
                    if (_music.clip != previous)
                    {
                        OnRandomMusicTrackChanged?.Invoke(_music.clip);
                    }

                    yield break;
                }
            }

            private void RestartShuffleWatchdog(MusicEntry entry)
            {
                StopShuffleWatchdog();

                if (entry == null || entry.IsLooping || entry.ClipCount <= 1)
                {
                    return;
                }

                if (!Application.isPlaying || !isActiveAndEnabled)
                {
                    return;
                }

                float lead = ResolveFadeDuration(entry, MusicTransition.Default);
                _musicShuffleRoutine = StartCoroutine(ShuffleWatchdogRoutine(entry, lead));
            }

            private void StopShuffleWatchdog()
            {
                if (_musicShuffleRoutine != null)
                {
                    StopCoroutine(_musicShuffleRoutine);
                    _musicShuffleRoutine = null;
                }
            }

            private void RestartFade(IEnumerator routine)
            {
                StopFadeRoutine();
                _musicFadeRoutine = StartCoroutine(routine);
            }

            private void StopFadeRoutine()
            {
                if (_musicFadeRoutine != null)
                {
                    StopCoroutine(_musicFadeRoutine);
                    _musicFadeRoutine = null;
                }
            }

            private void StopFadeSource()
            {
                for (int index = 0; index < _musicFadeSources.Length; index++)
                {
                    AudioSource source = _musicFadeSources[index];
                    if (source == null)
                    {
                        continue;
                    }

                    source.Stop();
                    source.clip = null;
                    source.volume = 0f;
                    _musicFadeGains[index] = 0f;
                    _musicFadeEnvelopes[index] = 0f;
                }
            }

            private void StopMusicRoutines()
            {
                StopFadeRoutine();
                StopShuffleWatchdog();
            }

            /// <summary>
            ///     A fade needs a running player loop and a live component. Outside play mode - EditMode tests,
            ///     inspector buttons - every transition degrades to a clean cut rather than silently doing
            ///     nothing.
            /// </summary>
            private bool CanFade(float duration)
            {
                return duration > 0.001f && Application.isPlaying && this != null && isActiveAndEnabled;
            }

            /// <summary>
            ///     Call argument beats pool override beats manager default; the manager default is zero when
            ///     crossfading is switched off entirely.
            /// </summary>
            private float ResolveFadeDuration(MusicEntry entry, MusicTransition transition)
            {
                if (transition.HasDuration)
                {
                    return transition.Duration;
                }

                if (entry != null && entry.HasFadeOverride)
                {
                    return entry.FadeDuration;
                }

                return _crossfadeMusic ? Mathf.Max(0f, _musicFadeDuration) : 0f;
            }

            /// <summary>
            ///     The audible level of the current music: channel x entry x the playing track's own trim.
            ///     Read live by the fade routines, so a volume change mid-fade still lands correctly.
            /// </summary>
            private float MusicTargetVolume =>
                Mathf.Clamp01(_musicChannelVolume * _musicSourceGain);

            private void ApplyMusicVolumes()
            {
                if (_music != null)
                {
                    _music.volume = MusicTargetVolume * _musicEnvelope;
                }

                for (int index = 0; index < _musicFadeSources.Length; index++)
                {
                    AudioSource fade = _musicFadeSources[index];
                    if (fade != null)
                    {
                        fade.volume = Mathf.Clamp01(_musicChannelVolume * _musicFadeGains[index]) *
                                      _musicFadeEnvelopes[index];
                    }
                }
            }

            #endregion

            #region Ambience

            private float _ambienceFade;
            private Coroutine _ambienceFadeRoutine;

            // WHY a flag and not a hideFlags or parent check: _ambience is serialized and may be an authored
            // source, even one on AM's own GameObject, so only the source EnsureAmbienceSource created is AM's
            // to destroy.
            private bool _ambienceOwned;

            /// <summary>The looping bed under the music; null until first used or assigned.</summary>
            public AudioSource Ambience => _ambience;

            /// <summary>Clip currently looping as ambience, or null.</summary>
            public AudioClip CurrentAmbienceClip => _ambience != null && _ambience.isPlaying ? _ambience.clip : null;

            /// <summary>Ambience level relative to the music channel (0-1).</summary>
            public float AmbienceVolume
            {
                get => _ambienceVolume;
                set
                {
                    _ambienceVolume = Mathf.Clamp01(value);
                    ApplyAmbienceVolume();
                }
            }

            /// <summary>Loops <paramref name="clip"/> under the music with the default fade.</summary>
            public void PlayAmbience(AudioClip clip) => PlayAmbience(clip, -1f);

            /// <summary>
            ///     Loops <paramref name="clip"/> under the music. The ambience runs alongside any music pool
            ///     (it has its own AudioSource), sits on the music bus, follows the music channel volume and
            ///     mute, and is <see cref="AmbienceVolume"/> times quieter. Asking for the clip already playing
            ///     is a no-op; null stops the ambience.
            /// </summary>
            /// <param name="clip">Loop to play; null stops the ambience.</param>
            /// <param name="fadeSeconds">Fade-in length; negative uses the inspector default, 0 cuts in.</param>
            public void PlayAmbience(AudioClip clip, float fadeSeconds)
            {
                if (clip == null)
                {
                    StopAmbience(fadeSeconds);
                    return;
                }

                EnsureRuntimeInitialized();
                EnsureAmbienceSource();
                if (_ambience.clip == clip && _ambience.isPlaying && _ambienceFadeRoutine == null && _ambienceFade >= 1f)
                {
                    return;
                }

                bool alreadyLooping = _ambience.isPlaying && _ambience.clip == clip;
                _ambience.clip = clip;
                _ambience.loop = true;
                if (!alreadyLooping)
                {
                    _ambienceFade = 0f;
                    ApplyAmbienceVolume();
                    _ambience.Play();
                }

                FadeAmbience(1f, fadeSeconds, false);
            }

            /// <summary>Fades the ambience out and stops it.</summary>
            /// <param name="fadeSeconds">Fade-out length; negative uses the inspector default, 0 cuts.</param>
            public void StopAmbience(float fadeSeconds = -1f)
            {
                if (_ambience == null || !_ambience.isPlaying)
                {
                    return;
                }

                FadeAmbience(0f, fadeSeconds, true);
            }

            private void FadeAmbience(float target, float fadeSeconds, bool stopAtEnd)
            {
                if (_ambienceFadeRoutine != null)
                {
                    StopCoroutine(_ambienceFadeRoutine);
                    _ambienceFadeRoutine = null;
                }

                float duration = fadeSeconds < 0f ? _ambienceFadeDuration : fadeSeconds;
                if (duration <= 0f || !Application.isPlaying || !isActiveAndEnabled)
                {
                    _ambienceFade = target;
                    ApplyAmbienceVolume();
                    if (stopAtEnd && _ambience != null)
                    {
                        _ambience.Stop();
                    }

                    return;
                }

                _ambienceFadeRoutine = StartCoroutine(AmbienceFadeRoutine(target, duration, stopAtEnd));
            }

            private IEnumerator AmbienceFadeRoutine(float target, float duration, bool stopAtEnd)
            {
                float start = _ambienceFade;
                float time = 0f;
                while (time < duration)
                {
                    time += Time.unscaledDeltaTime;
                    _ambienceFade = Mathf.Lerp(start, target, Mathf.Clamp01(time / duration));
                    ApplyAmbienceVolume();
                    yield return null;
                }

                _ambienceFade = target;
                ApplyAmbienceVolume();
                if (stopAtEnd && _ambience != null)
                {
                    _ambience.Stop();
                }

                _ambienceFadeRoutine = null;
            }

            private void ApplyAmbienceVolume()
            {
                if (_ambience == null)
                {
                    return;
                }

                _ambience.volume = Mathf.Clamp01(_musicChannelVolume * _ambienceVolume * _ambienceFade);
                if (_music != null)
                {
                    _ambience.mute = _music.mute;
                    _ambience.outputAudioMixerGroup = _music.outputAudioMixerGroup;
                }
            }

            private void EnsureAmbienceSource()
            {
                if (_ambience != null)
                {
                    return;
                }

                // WHY: a runtime helper like PitchVoice_* and MusicCrossfade_*: an edit-time call must not leave
                // a child saved into the scene or prefab.
                GameObject obj = new("Ambience") { hideFlags = HideFlags.DontSave };
                obj.transform.SetParent(transform, false);
                _ambience = obj.AddComponent<AudioSource>();
                _ambienceOwned = true;
                _ambience.loop = true;
                _ambience.playOnAwake = false;
                _ambience.priority = 127;
                ApplyAmbienceVolume();
            }

            // WHY: AMSettings mutes music by flipping the music source's mute directly, without telling AM,
            // so the ambience copies it every frame instead of waiting for a call that never comes. Volume
            // needs no such copy: AMSettings.SetMusicVolume goes through SetMusicVolume.
            private void LateUpdate()
            {
                if (_music != null)
                {
                    for (int index = 0; index < _musicFadeSources.Length; index++)
                    {
                        AudioSource fade = _musicFadeSources[index];
                        if (fade != null)
                        {
                            fade.mute = _music.mute;
                        }
                    }
                }

                if (_ambience != null && _music != null && _ambience.mute != _music.mute)
                {
                    _ambience.mute = _music.mute;
                }
            }

            #endregion

            #region Legacy random music

            /// <summary>
            ///     Replaces the legacy random-track list. Kept for existing projects; the modern shape is a
            ///     music entry with several clips and <see cref="MusicPoolMode.Shuffle" />, addressed by id.
            /// </summary>
            /// <param name="tracks">New track list (null clears the list).</param>
            [Obsolete("Configure a music entry (pool) with MusicPoolMode.Shuffle and call PlayMusicPool(id) instead.")]
            public void SetRandomMusicTracks(params AudioClip[] tracks)
            {
                _randomMusicTracks = tracks ?? Array.Empty<AudioClip>();

                // WHY: hot-swapping the list of a pool that is already playing must not tear the audio, so the
                // clips are replaced in place. The next EnableRandomMusic() / NextMusicTrack() crossfades into
                // the new set.
                // WHY identity, not the id string: a project is free to name one of its own pools
                // "Random", and that pool must not be quietly rewritten by a legacy call it never made.
                if (_legacyRandomPool != null && ReferenceEquals(_currentMusicEntry, _legacyRandomPool))
                {
                    _legacyRandomPool.Clips = _randomMusicTracks;
                    _legacyRandomPool.ResetClipHistory();
                }
            }

            /// <summary>
            ///     Starts the legacy random-track list as a shuffle pool.
            /// </summary>
            [Obsolete("Configure a music entry (pool) with MusicPoolMode.Shuffle and call PlayMusicPool(id) instead.")]
            public void EnableRandomMusic()
            {
                EnsureRuntimeInitialized();

                if (_randomMusicTracks == null || _randomMusicTracks.Length == 0)
                {
                    NeoDiagnostics.LogWarning("[AM] Random music track list is empty.");
                    return;
                }

                if (_music == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Music AudioSource is not initialized.");
                    return;
                }

                MusicEntry pool = new MusicEntry(LegacyRandomPoolId, (AudioClip[])_randomMusicTracks.Clone())
                {
                    Mode = MusicPoolMode.Shuffle
                };
                if (pool.IsEmpty)
                {
                    NeoDiagnostics.LogWarning("[AM] Random music track list has no usable clip.");
                    return;
                }

                // WHY: the pre-10.13 method stopped single-track music and raised OnMusicStopped before
                // starting the pool. Subscribers count on that exact event, so it is preserved - what changed
                // is that the pool now crossfades in instead of following a hard stop.
                if (_music.isPlaying)
                {
                    OnMusicStopped?.Invoke();
                }

                // WHY a fresh instance each call: the pre-10.13 method restarted the rotation every time,
                // and PlayMusicEntry skips an entry it is already playing. A new object keeps that restart.
                _legacyRandomPool = pool;

                PlayMusicEntry(_legacyRandomPool, default, true);
                _useRandomMusic = true;
            }

            /// <summary>Stops the legacy random-music mode.</summary>
            public void DisableRandomMusic()
            {
                _useRandomMusic = false;

                if (_legacyRandomPool != null && ReferenceEquals(_currentMusicEntry, _legacyRandomPool))
                {
                    StopShuffleWatchdog();
                    StopFadeRoutine();
                    StopFadeSource();
                    _currentMusicEntry = null;
                    if (_music != null)
                    {
                        _music.Stop();
                    }
                }
            }

            /// <summary>Whether the legacy random-music mode is active.</summary>
            public bool IsRandomMusicEnabled()
            {
                return _useRandomMusic && _music != null && _music.clip != null;
            }

            #endregion

            #region Volume

            /// <summary>
            ///     Sets a channel volume. The audible level of a cue is this multiplied by the entry's own
            ///     volume.
            /// </summary>
            /// <param name="volume">Volume (0-1).</param>
            /// <param name="efx">True for effects, false for music.</param>
            public void SetVolume(float volume, bool efx)
            {
                EnsureRuntimeInitialized();

                float clamped = Mathf.Clamp01(volume);

                if (efx)
                {
                    if (_efx != null)
                    {
                        _efx.volume = clamped;
                        RefreshChannelVoices(SoundChannel.Sfx);
                    }

                    return;
                }

                _musicChannelVolume = clamped;
                ApplyMusicVolumes();

                ApplyAmbienceVolume();
            }

            /// <summary>Sets the music channel volume.</summary>
            /// <param name="volume">Volume (0-1).</param>
            public void SetMusicVolume(float volume) => SetVolume(volume, false);

            /// <summary>Sets the sound-effects channel volume (<see cref="SoundChannel.Sfx" />).</summary>
            /// <param name="volume">Volume (0-1).</param>
            public void SetEfxVolume(float volume) => SetVolume(volume, true);

            /// <summary>Applies startup volumes to the AudioSources.</summary>
            public void ApplyStartVolumes()
            {
                EnsureRuntimeInitialized();

                if (_efx != null)
                {
                    _efx.volume = StartVolumeEfx;
                    RefreshChannelVoices(SoundChannel.Sfx);
                }

                _musicChannelVolume = Mathf.Clamp01(StartVolumeMusic);
                ApplyMusicVolumes();

                // WHY: an ambience started before AMSettings.Start would otherwise keep the pre-settings level.
                ApplyAmbienceVolume();
            }

            #endregion

            private static T FindById<T>(T[] entries, string id) where T : AudioEntry
            {
                if (entries == null || string.IsNullOrEmpty(id))
                {
                    return null;
                }

                for (int index = 0; index < entries.Length; index++)
                {
                    T entry = entries[index];
                    if (entry != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                    {
                        return entry;
                    }
                }

                return null;
            }
        }
}
