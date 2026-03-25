using System;
using Neo.Tools;
using UnityEngine;

namespace Neo
{
    namespace Audio
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

            [Header("Random Music")] [SerializeField]
            private bool _useRandomMusic;

            [SerializeField] private AudioClip[] _randomMusicTracks;

            private RandomMusicController _randomMusicController;

            public float startVolumeEfx { get; set; } = 1f;
            public float startVolumeMusic { get; set; } = 1f;

            /// <summary>AudioSource for sound effects.</summary>
            public AudioSource Efx => _efx;

            /// <summary>AudioSource for music.</summary>
            public AudioSource Music => _music;

            private void OnValidate()
            {
                if (_music == null)
                {
                    CreateMusic();
                }

                if (_efx == null)
                {
                    CreateEfx();
                }
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

            /// <summary>
            ///     Plays a sound effect by index in the <c>_sounds</c> array at the given volume.
            /// </summary>
            /// <param name="id">Sound index in <c>_sounds</c>.</param>
            /// <param name="volume">Playback volume (0–1).</param>
            [Button]
            public void Play(int id, float volume)
            {
                if (_efx == null)
                {
                    Debug.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return;
                }

                if (_sounds == null || id < 0 || id >= _sounds.Length)
                {
                    Debug.LogWarning($"[AM] Sound ID {id} is out of range.");
                    return;
                }

                if (_sounds[id].clip == null)
                {
                    Debug.LogWarning($"[AM] Sound clip at ID {id} is null.");
                    return;
                }

                _efx.PlayOneShot(_sounds[id].clip, Mathf.Clamp(volume, 0f, 1f));
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
                    Debug.LogWarning($"[AM] Sound ID {id} is out of range.");
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
            /// <param name="volume">Playback volume (0–1).</param>
            public void Play(AudioClip clip, float volume)
            {
                if (_efx == null)
                {
                    Debug.LogWarning("[AM] Effects AudioSource is not initialized.");
                    return;
                }

                if (clip == null)
                {
                    Debug.LogWarning("[AM] AudioClip is null.");
                    return;
                }

                _efx.PlayOneShot(clip, Mathf.Clamp(volume, 0f, 1f));
            }

            /// <summary>
            ///     Plays music by index in <c>_musicClips</c> at the given volume.
            ///     Stops random music if it was active.
            /// </summary>
            /// <param name="id">Music index in <c>_musicClips</c>.</param>
            /// <param name="volume">Playback volume (0–1).</param>
            [Button]
            public void PlayMusic(int id, float volume)
            {
                if (_music == null)
                {
                    Debug.LogWarning("[AM] Music AudioSource is not initialized.");
                    return;
                }

                if (_musicClips == null || id < 0 || id >= _musicClips.Length)
                {
                    Debug.LogWarning($"[AM] Music clip ID {id} is out of range.");
                    return;
                }

                if (_musicClips[id] == null)
                {
                    Debug.LogWarning($"[AM] Music clip at ID {id} is null.");
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
            /// <param name="volume">Playback volume (0–1).</param>
            public void PlayMusicByClip(AudioClip clip, float volume)
            {
                if (_music == null)
                {
                    Debug.LogWarning("[AM] Music AudioSource is not initialized.");
                    return;
                }

                if (clip == null)
                {
                    Debug.LogWarning("[AM] AudioClip is null.");
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
                if (_randomMusicTracks == null || _randomMusicTracks.Length == 0)
                {
                    Debug.LogWarning("[AM] Random music track list is empty.");
                    return;
                }

                if (_music == null)
                {
                    Debug.LogWarning("[AM] Music AudioSource is not initialized.");
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
            /// <param name="volume">Volume (0–1).</param>
            /// <param name="efx">True for effects, false for music.</param>
            public void SetVolume(float volume, bool efx)
            {
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
            ///     Applies startup volumes to the AudioSources.
            /// </summary>
            public void ApplyStartVolumes()
            {
                if (_efx != null)
                {
                    _efx.volume = startVolumeEfx;
                }

                if (_music != null)
                {
                    _music.volume = startVolumeMusic;
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
}
