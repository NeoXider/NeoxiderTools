using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;

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

            // ---- Legacy serialized data. Kept so old scenes lose nothing; migrated into the entry lists
            // ---- on load (see OnAfterDeserialize) and hidden from the inspector.
            [HideInInspector] [SerializeField] private AudioClip[] _musicClips;
            [HideInInspector] [SerializeField] private Sound[] _sounds;
            [HideInInspector] [SerializeField] private bool _useRandomMusic;
            [HideInInspector] [SerializeField] private AudioClip[] _randomMusicTracks;
            [HideInInspector] [SerializeField] private int _dataVersion;

            private AudioSource[] _pitchSources;
            private int _pitchVoiceIndex;
            private bool _runtimeInitialized;

            private SoundEntry _legacySoundAdapter;
            private readonly AudioClip[] _legacySoundAdapterClips = new AudioClip[1];

            // WHY two: one outgoing voice is enough for a single crossfade, but a pool switch landing while
            // the previous one is still fading would have to cut that track dead to reuse the voice - an
            // audible click exactly when the game is at its busiest. Two voices let back-to-back switches
            // each fade out properly; a third switch inside one fade length is rare enough to reuse.
            private readonly AudioSource[] _musicFadeSources = new AudioSource[2];
            private int _musicFadeSourceIndex;
            private Coroutine _musicFadeRoutine;
            private Coroutine _musicShuffleRoutine;
            private MusicEntry _currentMusicEntry;
            private MusicEntry _legacyRandomPool;
            private float _currentMusicEntryVolume = 1f;
            private float _currentMusicClipVolume = 1f;
            private float _musicChannelVolume = 1f;

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

            /// <summary>Sound-effects channel volume, multiplied by each entry's own volume.</summary>
            public float EfxVolume
            {
                get => _efx != null ? _efx.volume : 1f;
                set => SetVolume(value, true);
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
                StopMusicRoutines();
                base.OnDestroy();
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
                AudioEntry entry = ResolveSoundEntry(id);
                if (entry == null)
                {
                    return;
                }

                PlayEntryOneShot(entry, options);
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

                AudioEntry entry = GetSound(id);
                if (entry == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] No sound entry with id '{id}'.");
                    return;
                }

                PlayEntryOneShot(entry, options);
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
                    _pitchMin, _pitchMax);
            }

            /// <summary>Plays a clip directly at full volume.</summary>
            /// <param name="clip">Clip to play.</param>
            public void Play(AudioClip clip)
            {
                Play(clip, 1f);
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
            private AudioEntry ResolveSoundEntry(int index)
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
                        NeoDiagnostics.LogWarning($"[AM] Sound clip at ID {index} is null.");
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
            private void PlayEntryOneShot(AudioEntry entry, SoundOptions options)
            {
                AudioClip clip = options.ClipIndexOverride.HasValue
                    ? entry.ClipAt(options.ClipIndexOverride.Value)
                    : entry.NextClip();
                if (clip == null)
                {
                    NeoDiagnostics.LogWarning(
                        $"[AM] Sound entry '{entry.Id}' has no usable clip.");
                    return;
                }

                // WHY the clip trim multiplies rather than replaces: a per-call override says how loud this
                // ENTRY should be, while the trim describes how hot that particular take was recorded. Both
                // are true at once, and PlayOneShot multiplies the effects channel in on top.
                // WHY the ceiling is MaxVolume and not 1: entry volume and clip trim are MULTIPLIERS of the
                // effects channel, and both are authored up to 2 precisely so a quietly mastered sample can
                // be lifted. Clamping their product at 1 threw that away - an entry set to 2 played at 1.
                // AudioSource.PlayOneShot scales by the source volume, so the channel slider still bounds
                // the result; only the multiplier is allowed above 1.
                float volume = Mathf.Clamp((options.VolumeOverride ?? entry.Volume) * entry.LastClipVolume,
                    0f, AudioEntry.MaxVolume);
                bool randomizePitch = options.RandomizePitchOverride ?? entry.RandomizePitch;
                float pitchMin = options.PitchMinOverride ?? entry.PitchMin;
                float pitchMax = options.PitchMaxOverride ?? entry.PitchMax;

                PlayOneShotInternal(clip, volume, randomizePitch, pitchMin, pitchMax);
            }

            /// <summary>
            ///     Plays a one-shot, routing it through a spare AudioSource when the pitch is randomised.
            ///     <para>
            ///         WHY the extra sources: <see cref="AudioSource.pitch"/> applies to the whole source, so
            ///         raising it before a PlayOneShot also retunes every one-shot still ringing on that source.
            ///         With effects firing on top of each other - the usual case for a hit sound - that is
            ///         audible. Each pitched shot therefore gets its own voice, round-robin over a small pool.
            ///     </para>
            ///     <para>
            ///         The volume passed here is the entry multiplier; <see cref="AudioSource.PlayOneShot" />
            ///         scales it by the source volume, which is the effects channel. That product is the
            ///         channel x entry contract.
            ///     </para>
            /// </summary>
            private void PlayOneShotInternal(AudioClip clip, float volume, bool randomizePitch, float pitchMin,
                float pitchMax)
            {
                if (!randomizePitch)
                {
                    _efx.PlayOneShot(clip, volume);
                    return;
                }

                AudioSource source = NextPitchVoice();
                if (source == null)
                {
                    _efx.PlayOneShot(clip, volume);
                    return;
                }

                // WHY MirrorEfx on every shot (10.12.0): SetEfxVolume - the slider every game exposes -
                // writes _efx.volume, and a voice that mirrored it once would keep playing at whatever the
                // volume happened to be when it was created. Turning effects down would silence the plain
                // one-shots and leave the pitched ones loud. The pitch range now comes from the entry
                // rather than the manager, so the two settings compose instead of overriding each other.
                MirrorEfx(source);
                source.pitch = UnityEngine.Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax));
                source.PlayOneShot(clip, volume);
            }

            private AudioSource NextPitchVoice()
            {
                if (_efx == null) return null;

                int count = Mathf.Max(1, _pitchVoices);
                if (_pitchSources == null)
                {
                    _pitchSources = new AudioSource[count];
                }
                else if (_pitchSources.Length != count)
                {
                    // WHY: copy the voices over instead of dropping the array. A pool resized from the
                    // Inspector during Play Mode would otherwise orphan its GameObjects under _efx and
                    // build a new set next to them.
                    AudioSource[] resized = new AudioSource[count];
                    for (int index = 0; index < _pitchSources.Length; index++)
                    {
                        if (_pitchSources[index] == null)
                        {
                            continue;
                        }

                        if (index < count)
                        {
                            resized[index] = _pitchSources[index];
                        }
                        else
                        {
                            Destroy(_pitchSources[index].gameObject);
                        }
                    }

                    _pitchSources = resized;
                }

                _pitchVoiceIndex = (_pitchVoiceIndex + 1) % count;
                AudioSource source = _pitchSources[_pitchVoiceIndex];
                if (source != null) return source;

                GameObject go = new GameObject($"PitchVoice_{_pitchVoiceIndex}");
                go.transform.SetParent(_efx.transform, false);
                go.hideFlags = HideFlags.DontSave;
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                MirrorEfx(source);

                _pitchSources[_pitchVoiceIndex] = source;
                return source;
            }

            /// <summary>
            ///     Copies the effects source's routing and mix settings onto a pitch voice.
            /// </summary>
            /// <remarks>
            ///     WHY on every shot and not only at creation: <see cref="SetEfxVolume" /> — the volume
            ///     slider every game exposes — writes <c>_efx.volume</c>. A voice that mirrored it once
            ///     would keep playing at whatever the volume was when it happened to be created, so
            ///     turning effects down would silence the plain one-shots and leave the pitched ones loud.
            /// </remarks>
            private void MirrorEfx(AudioSource source)
            {
                source.outputAudioMixerGroup = _efx.outputAudioMixerGroup;
                source.volume = _efx.volume;
                source.mute = _efx.mute;
                source.spatialBlend = _efx.spatialBlend;
                source.bypassEffects = _efx.bypassEffects;
                source.bypassListenerEffects = _efx.bypassListenerEffects;
                source.bypassReverbZones = _efx.bypassReverbZones;
                source.ignoreListenerPause = _efx.ignoreListenerPause;
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
                if (options.VolumeOverride.HasValue)
                {
                    _currentMusicEntryVolume =
                        Mathf.Clamp(options.VolumeOverride.Value, 0f, AudioEntry.MaxVolume);
                }

                StartMusicClip(_currentMusicEntry, options);
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
                        NeoDiagnostics.LogWarning($"[AM] Music clip at ID {index} is null.");
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

                if (!ReferenceEquals(_currentMusicEntry, entry))
                {
                    _useRandomMusic = false;
                }

                _currentMusicEntry = entry;
                _currentMusicEntryVolume = volume;

                StartMusicClip(entry, options);

                if (!silent && _music.clip != null)
                {
                    OnMusicStarted?.Invoke(_music.clip);
                }
            }

            /// <summary>
            ///     Picks the next clip of the entry and hands it to the transition machinery.
            ///     <para>
            ///         Takes no volume argument on purpose: the entry level is already on
            ///         <c>_currentMusicEntryVolume</c> by the time anything calls this, and passing it again
            ///         was one more place for the played level and the faded-to level to disagree.
            ///     </para>
            /// </summary>
            private void StartMusicClip(MusicEntry entry, MusicOptions options)
            {
                AudioClip clip = options.TrackIndexOverride.HasValue
                    ? entry.ClipAt(options.TrackIndexOverride.Value)
                    : entry.NextClip();
                if (clip == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] Music entry '{entry.Id}' has no usable clip.");
                    return;
                }

                // Tracks of one pool are rarely mastered to the same level; the trim of whichever one was
                // picked joins the product before the fade starts chasing it.
                _currentMusicClipVolume = entry.LastClipVolume;

                // WHY: a shuffle pool must not loop its clip, or the watchdog never sees the end. A pool with
                // a single clip has nothing to shuffle to, so it loops regardless of mode.
                bool loop = entry.IsLooping || entry.ClipCount <= 1;
                float duration = ResolveFadeDuration(entry, options.Transition);

                SwitchMusicSource(clip, entry.NextPitch(), loop, duration);
                RestartShuffleWatchdog(entry);
            }

            /// <summary>
            ///     Crossfade core. The primary <c>_music</c> source always carries the <i>incoming</i> track -
            ///     that is what keeps <see cref="Music" />, <see cref="GetCurrentMusicClip" /> and every
            ///     existing volume tweak pointing at the track you can hear. The outgoing track is handed to a
            ///     second, hidden source at the exact playback position it had reached, and faded there.
            /// </summary>
            private void SwitchMusicSource(AudioClip clip, float pitch, bool loop, float duration)
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
                    _music.volume = target;
                    _music.Play();
                    return;
                }

                if (_music.isPlaying && _music.clip != null)
                {
                    HandOffToFadeSource();
                }

                _music.clip = clip;
                _music.pitch = pitch;
                _music.loop = loop;
                _music.volume = 0f;
                _music.Play();

                RestartFade(CrossfadeRoutine(duration));
            }

            /// <summary>
            ///     Moves the currently audible track onto a hidden voice, at the exact playback position it
            ///     had reached, so the fade continues the performance instead of restarting it.
            /// </summary>
            private void HandOffToFadeSource()
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
                    outStart[index] = _musicFadeSources[index] != null ? _musicFadeSources[index].volume : 0f;
                }

                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    if (_music != null)
                    {
                        _music.volume = MusicTargetVolume * t;
                    }

                    for (int index = 0; index < _musicFadeSources.Length; index++)
                    {
                        if (_musicFadeSources[index] != null)
                        {
                            _musicFadeSources[index].volume = outStart[index] * (1f - t);
                        }
                    }

                    yield return null;
                }

                if (_music != null)
                {
                    _music.volume = MusicTargetVolume;
                }

                StopFadeSource();
                _musicFadeRoutine = null;
            }

            private IEnumerator FadeOutAndStopRoutine(float duration)
            {
                float start = _music != null ? _music.volume : 0f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    if (_music != null)
                    {
                        _music.volume = start * (1f - t);
                    }

                    for (int index = 0; index < _musicFadeSources.Length; index++)
                    {
                        if (_musicFadeSources[index] != null)
                        {
                            _musicFadeSources[index].volume = start * (1f - t);
                        }
                    }

                    yield return null;
                }

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
                    StartMusicClip(entry, default);
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
                Mathf.Clamp01(_musicChannelVolume * _currentMusicEntryVolume * _currentMusicClipVolume);

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

                // WHY: the pre-10.13 method stopped single-track music and raised OnMusicStopped before
                // starting the pool. Subscribers count on that exact event, so it is preserved - what changed
                // is that the pool now crossfades in instead of following a hard stop.
                if (_music.isPlaying)
                {
                    OnMusicStopped?.Invoke();
                }

                // WHY a fresh instance each call: the pre-10.13 method restarted the rotation every time,
                // and PlayMusicEntry skips an entry it is already playing. A new object keeps that restart.
                _legacyRandomPool = new MusicEntry(LegacyRandomPoolId, (AudioClip[])_randomMusicTracks.Clone())
                {
                    Mode = MusicPoolMode.Shuffle
                };

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
                    }

                    return;
                }

                _musicChannelVolume = clamped;
                if (_music != null)
                {
                    _music.volume = MusicTargetVolume;
                }
            }

            /// <summary>Sets the music channel volume.</summary>
            /// <param name="volume">Volume (0-1).</param>
            public void SetMusicVolume(float volume) => SetVolume(volume, false);

            /// <summary>Sets the sound-effects channel volume.</summary>
            /// <param name="volume">Volume (0-1).</param>
            public void SetEfxVolume(float volume) => SetVolume(volume, true);

            /// <summary>Applies startup volumes to the AudioSources.</summary>
            public void ApplyStartVolumes()
            {
                EnsureRuntimeInitialized();

                if (_efx != null)
                {
                    _efx.volume = StartVolumeEfx;
                }

                _musicChannelVolume = Mathf.Clamp01(StartVolumeMusic);
                if (_music != null)
                {
                    _music.volume = MusicTargetVolume;
                }
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
