using System;
using Neo.Tools;
using UnityEngine;

namespace Neo.Audio
{
        [Serializable]
        public class Sound
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1;
        }

        /// <summary>Central audio manager for sound effects and music. Supports both specific tracks and random playback.</summary>
        [NeoDoc("Audio/AM.md")]
        [CreateFromMenu("Neoxider/Audio/AM")]
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AM))]
        public class AM : Singleton<AM>
        {
            [SerializeField] private AudioSource _efx;
            [Space] [SerializeField] private AudioSource _music;
            [SerializeField] private AudioClip[] _musicClips;
            [SerializeField] private Sound[] _sounds;

            [Header("Random Pitch")]
            [Tooltip("Vary the pitch of every one-shot sound effect. Keeps repeated cues - a blade "
                     + "hit, a button, a coin - from sounding like the same sample on a loop.")]
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

            [Header("Random Music")] [SerializeField]
            private bool _useRandomMusic;

            [SerializeField] private AudioClip[] _randomMusicTracks;

            private RandomMusicController _randomMusicController;
            private AudioSource[] _pitchSources;
            private int _pitchVoiceIndex;
            private bool _runtimeInitialized;

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

            /// <summary>AudioSource for music.</summary>
            public AudioSource Music => _music;

            /// <summary>Whether one-shot effects are played at a randomised pitch.</summary>
            public bool RandomizePitch { get => _randomizePitch; set => _randomizePitch = value; }

            /// <summary>Pitch range used when <see cref="RandomizePitch"/> is on.</summary>
            public void SetPitchRange(float min, float max)
            {
                _pitchMin = Mathf.Min(min, max);
                _pitchMax = Mathf.Max(min, max);
            }

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

            /// <summary>Raised when music starts playing.</summary>
            public event Action<AudioClip> OnMusicStarted;

            /// <summary>Raised when music stops.</summary>
            public event Action OnMusicStopped;

            /// <summary>Raised when random music track changes.</summary>
            public event Action<AudioClip> OnRandomMusicTrackChanged;

            protected override void Init()
            {
                base.Init();
            }

            private void Start()
            {
                EnsureRuntimeInitialized();
            }

            protected override void OnDestroy()
            {
                _randomMusicController?.Stop();
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

                _randomMusicController = new RandomMusicController();
                if (_music != null)
                {
                    _randomMusicController.Initialize(_music, _randomMusicTracks);
                    _randomMusicController.OnTrackChanged += clip => OnRandomMusicTrackChanged?.Invoke(clip);
                }

                if (_useRandomMusic && _randomMusicTracks != null && _randomMusicTracks.Length > 0)
                {
                    _randomMusicController.Start();
                }
                else if (_musicClips != null && _musicClips.Length > 0)
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

            /// <summary>
            ///     Plays a sound effect by index in the <c>_sounds</c> array at the given volume.
            /// </summary>
            /// <param name="id">Sound index in <c>_sounds</c>.</param>
            /// <param name="volume">Playback volume (0-1).</param>
            [Button]
            public void Play(int id, float volume)
            {
                EnsureRuntimeInitialized();

                if (_efx == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return;
                }

                if (_sounds == null || id < 0 || id >= _sounds.Length)
                {
                    NeoDiagnostics.LogWarning($"[AM] Sound ID {id} is out of range.");
                    return;
                }

                if (_sounds[id].clip == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] Sound clip at ID {id} is null.");
                    return;
                }

                PlayOneShotInternal(_sounds[id].clip, Mathf.Clamp(volume, 0f, 1f));
            }

            /// <summary>
            ///     Plays a sound effect by index using each entry's default volume from the <c>_sounds</c> array.
            /// </summary>
            /// <param name="id">Sound index in <c>_sounds</c>.</param>
            [Button]
            public void Play(int id)
            {
                if (_sounds == null || id < 0 || id >= _sounds.Length)
                {
                    NeoDiagnostics.LogWarning($"[AM] Sound ID {id} is out of range.");
                    return;
                }

                float soundMultiplier = _sounds[id].volume;
                soundMultiplier = soundMultiplier == 0 ? 1 : soundMultiplier;
                Play(id, soundMultiplier);
            }

            /// <summary>
            ///     Plays a sound effect from an <see cref="AudioClip"/> at the given volume.
            /// </summary>
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

                PlayOneShotInternal(clip, Mathf.Clamp(volume, 0f, 1f));
            }

            /// <summary>
            ///     Plays a sound effect from an <see cref="AudioClip"/> at full volume (1).
            /// </summary>
            /// <param name="clip">Clip to play.</param>
            public void Play(AudioClip clip)
            {
                Play(clip, 1f);
            }

            /// <summary>
            ///     Plays a one-shot, routing it through a spare AudioSource when the pitch is
            ///     randomised.
            ///
            ///     <para>
            ///     WHY the extra sources: <see cref="AudioSource.pitch"/> applies to the whole
            ///     source, so raising it before a PlayOneShot also retunes every one-shot still
            ///     ringing on that source. With effects firing on top of each other - the usual
            ///     case for a hit sound - that is audible. Each pitched shot therefore gets its own
            ///     voice, round-robin over a small pool.
            ///     </para>
            /// </summary>
            private void PlayOneShotInternal(AudioClip clip, float volume)
            {
                if (!_randomizePitch)
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

                source.pitch = UnityEngine.Random.Range(_pitchMin, _pitchMax);
                source.PlayOneShot(clip, volume);
            }

            private AudioSource NextPitchVoice()
            {
                if (_efx == null) return null;

                int count = Mathf.Max(1, _pitchVoices);
                if (_pitchSources == null || _pitchSources.Length != count)
                {
                    _pitchSources = new AudioSource[count];
                }

                _pitchVoiceIndex = (_pitchVoiceIndex + 1) % count;
                AudioSource source = _pitchSources[_pitchVoiceIndex];
                if (source != null) return source;

                var go = new GameObject($"PitchVoice_{_pitchVoiceIndex}");
                go.transform.SetParent(_efx.transform, false);
                go.hideFlags = HideFlags.DontSave;
                source = go.AddComponent<AudioSource>();

                // Mirror the effects source so mixer routing, spatial blend and volume all match.
                source.outputAudioMixerGroup = _efx.outputAudioMixerGroup;
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = _efx.spatialBlend;
                source.volume = _efx.volume;
                source.bypassEffects = _efx.bypassEffects;
                source.bypassListenerEffects = _efx.bypassListenerEffects;
                source.bypassReverbZones = _efx.bypassReverbZones;
                source.ignoreListenerPause = _efx.ignoreListenerPause;

                _pitchSources[_pitchVoiceIndex] = source;
                return source;
            }

            /// <summary>
            ///     Plays music by index in <c>_musicClips</c> at the given volume.
            ///     Stops random music if it was active.
            /// </summary>
            /// <param name="id">Music index in <c>_musicClips</c>.</param>
            /// <param name="volume">Playback volume (0-1).</param>
            [Button]
            public void PlayMusic(int id, float volume)
            {
                EnsureRuntimeInitialized();

                if (_music == null)
                {
                    NeoDiagnostics.LogWarning("[AM] Music AudioSource is not initialized.");
                    return;
                }

                if (_musicClips == null || id < 0 || id >= _musicClips.Length)
                {
                    NeoDiagnostics.LogWarning($"[AM] Music clip ID {id} is out of range.");
                    return;
                }

                if (_musicClips[id] == null)
                {
                    NeoDiagnostics.LogWarning($"[AM] Music clip at ID {id} is null.");
                    return;
                }

                if (_useRandomMusic && _randomMusicController != null)
                {
                    _randomMusicController.Stop();
                    _useRandomMusic = false;
                }

                _music.clip = _musicClips[id];
                _music.volume = Mathf.Clamp(volume, 0f, 1f);
                _music.Play();

                OnMusicStarted?.Invoke(_musicClips[id]);
            }

            /// <summary>
            ///     Plays music by index in <c>_musicClips</c> at full volume (1).
            ///     Stops random music if it was active.
            /// </summary>
            /// <param name="id">Music index in <c>_musicClips</c>.</param>
            [Button]
            public void PlayMusic(int id)
            {
                PlayMusic(id, 1f);
            }

            /// <summary>
            ///     Plays music from an <see cref="AudioClip"/> at the given volume.
            ///     Stops random music if it was active.
            /// </summary>
            /// <param name="clip">Clip to play.</param>
            /// <param name="volume">Playback volume (0-1).</param>
            public void PlayMusicByClip(AudioClip clip, float volume)
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

                if (_useRandomMusic && _randomMusicController != null)
                {
                    _randomMusicController.Stop();
                    _useRandomMusic = false;
                }

                _music.clip = clip;
                _music.volume = Mathf.Clamp(volume, 0f, 1f);
                _music.Play();

                OnMusicStarted?.Invoke(clip);
            }

            /// <summary>
            ///     Plays music from an <see cref="AudioClip"/> at full volume (1).
            ///     Stops random music if it was active.
            /// </summary>
            /// <param name="clip">Clip to play.</param>
            public void PlayMusicByClip(AudioClip clip)
            {
                PlayMusicByClip(clip, 1f);
            }

            /// <summary>
            ///     Stops any music playback (single track or random mode) and raises <see cref="OnMusicStopped"/>.
            /// </summary>
            [Button]
            public void StopMusic()
            {
                bool wasPlaying = false;

                if (_randomMusicController != null && (_randomMusicController.IsPlaying || _randomMusicController.IsPaused))
                {
                    _randomMusicController.Stop();
                    wasPlaying = true;
                }

                _useRandomMusic = false;

                if (_music != null && _music.isPlaying)
                {
                    _music.Stop();
                    wasPlaying = true;
                }

                if (wasPlaying)
                {
                    OnMusicStopped?.Invoke();
                }
            }

            /// <summary>
            ///     Replaces the random-music track list at runtime. Does not start playback by itself;
            ///     call <see cref="EnableRandomMusic"/> afterwards.
            /// </summary>
            /// <param name="tracks">New track list (null clears the list).</param>
            public void SetRandomMusicTracks(params AudioClip[] tracks)
            {
                _randomMusicTracks = tracks ?? Array.Empty<AudioClip>();

                if (_randomMusicController != null && _music != null)
                {
                    _randomMusicController.Initialize(_music, _randomMusicTracks);
                }
            }

            /// <summary>
            ///     Returns the currently playing music clip.
            /// </summary>
            /// <returns>Current <see cref="AudioClip"/>, or null if nothing is playing.</returns>
            public AudioClip GetCurrentMusicClip()
            {
                if (_useRandomMusic && _randomMusicController != null)
                {
                    return _randomMusicController.CurrentTrack;
                }

                return _music != null ? _music.clip : null;
            }

            /// <summary>
            ///     Enables random music playback from the track list.
            ///     Stops any single-track music currently playing.
            /// </summary>
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

                if (_randomMusicController == null)
                {
                    _randomMusicController = new RandomMusicController();
                    _randomMusicController.Initialize(_music, _randomMusicTracks);
                    _randomMusicController.OnTrackChanged += clip => OnRandomMusicTrackChanged?.Invoke(clip);
                }

                if (_music.isPlaying)
                {
                    _music.Stop();
                    OnMusicStopped?.Invoke();
                }

                _useRandomMusic = true;
                _randomMusicController.Start();
            }

            /// <summary>
            ///     Disables random music mode.
            /// </summary>
            public void DisableRandomMusic()
            {
                if (_randomMusicController != null)
                {
                    _randomMusicController.Stop();
                }

                _useRandomMusic = false;
            }

            /// <summary>
            ///     Returns whether random music mode is enabled and playing.
            /// </summary>
            public bool IsRandomMusicEnabled()
            {
                return _useRandomMusic && _randomMusicController != null && _randomMusicController.IsPlaying;
            }

            /// <summary>
            ///     Sets volume for sound effects or music.
            /// </summary>
            /// <param name="volume">Volume (0-1).</param>
            /// <param name="efx">True for effects, false for music.</param>
            public void SetVolume(float volume, bool efx)
            {
                EnsureRuntimeInitialized();

                if (efx)
                {
                    if (_efx != null)
                    {
                        _efx.volume = Mathf.Clamp(volume, 0f, 1f);
                    }
                }
                else
                {
                    if (_music != null)
                    {
                        _music.volume = Mathf.Clamp(volume, 0f, 1f);
                    }
                }
            }

            /// <summary>
            ///     Sets the music AudioSource volume. Convenience wrapper for <see cref="SetVolume(float, bool)"/>.
            /// </summary>
            /// <param name="volume">Volume (0-1).</param>
            public void SetMusicVolume(float volume) => SetVolume(volume, false);

            /// <summary>
            ///     Sets the sound-effects AudioSource volume. Convenience wrapper for <see cref="SetVolume(float, bool)"/>.
            /// </summary>
            /// <param name="volume">Volume (0-1).</param>
            public void SetEfxVolume(float volume) => SetVolume(volume, true);

            /// <summary>
            ///     Applies startup volumes to the AudioSources.
            /// </summary>
            public void ApplyStartVolumes()
            {
                EnsureRuntimeInitialized();

                if (_efx != null)
                {
                    _efx.volume = StartVolumeEfx;
                }

                if (_music != null)
                {
                    _music.volume = StartVolumeMusic;
                }
            }

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
        }
}
