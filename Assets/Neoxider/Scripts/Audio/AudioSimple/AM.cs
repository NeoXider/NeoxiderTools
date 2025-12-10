using System;
using Neo.Tools;
using UnityEngine;
#if ODIN_INSPECTOR
#endif

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

        /// <summary>
        ///     Центральный аудио менеджер для воспроизведения звуковых эффектов и музыки.
        ///     Поддерживает как конкретную музыку из списка, так и случайное воспроизведение.
        /// </summary>
        [AddComponentMenu("Neo/" + "Audio/" + nameof(AM))]
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

            /// <summary>
            ///     AudioSource для воспроизведения звуковых эффектов.
            /// </summary>
            public AudioSource Efx => _efx;

            /// <summary>
            ///     AudioSource для воспроизведения музыки.
            /// </summary>
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

            /// <summary>
            ///     Событие вызывается при начале воспроизведения музыки.
            /// </summary>
            public event Action<AudioClip> OnMusicStarted;

            /// <summary>
            ///     Событие вызывается при остановке музыки.
            /// </summary>
            public event Action OnMusicStopped;

            /// <summary>
            ///     Событие вызывается при смене трека в режиме случайной музыки.
            /// </summary>
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
            ///     Воспроизводит звуковой эффект по ID из массива звуков с указанной громкостью.
            /// </summary>
            /// <param name="id">ID звука в массиве _sounds.</param>
            /// <param name="volume">Громкость воспроизведения (0-1).</param>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
            public void Play(int id, float volume)
            {
                if (_efx == null)
                {
                    Debug.LogWarning("[AM] AudioSource для эффектов не инициализирован.");
                    return;
                }

                if (_sounds == null || id < 0 || id >= _sounds.Length)
                {
                    Debug.LogWarning($"[AM] Sound ID {id} is out of range.");
                    return;
                }

                if (_sounds[id].clip == null)
                {
                    Debug.LogWarning($"[AM] Sound clip по ID {id} равен null.");
                    return;
                }

                _efx.PlayOneShot(_sounds[id].clip, Mathf.Clamp(volume, 0f, 1f));
            }

            /// <summary>
            ///     Воспроизводит звуковой эффект по ID из массива звуков с громкостью по умолчанию из настроек.
            /// </summary>
            /// <param name="id">ID звука в массиве _sounds.</param>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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
            ///     Воспроизводит звуковой эффект напрямую по AudioClip с указанной громкостью.
            /// </summary>
            /// <param name="clip">AudioClip для воспроизведения.</param>
            /// <param name="volume">Громкость воспроизведения (0-1).</param>
            public void Play(AudioClip clip, float volume)
            {
                if (_efx == null)
                {
                    Debug.LogWarning("[AM] AudioSource для эффектов не инициализирован.");
                    return;
                }

                if (clip == null)
                {
                    Debug.LogWarning("[AM] AudioClip равен null.");
                    return;
                }

                _efx.PlayOneShot(clip, Mathf.Clamp(volume, 0f, 1f));
            }

            /// <summary>
            ///     Воспроизводит музыку по ID из массива музыки с указанной громкостью.
            ///     Останавливает случайную музыку, если она была включена.
            /// </summary>
            /// <param name="id">ID музыки в массиве _musicClips.</param>
            /// <param name="volume">Громкость воспроизведения (0-1).</param>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
            public void PlayMusic(int id, float volume)
            {
                if (_music == null)
                {
                    Debug.LogWarning("[AM] AudioSource для музыки не инициализирован.");
                    return;
                }

                if (_musicClips == null || id < 0 || id >= _musicClips.Length)
                {
                    Debug.LogWarning($"[AM] Music clip ID {id} is out of range.");
                    return;
                }

                if (_musicClips[id] == null)
                {
                    Debug.LogWarning($"[AM] Music clip по ID {id} равен null.");
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
            ///     Воспроизводит музыку по ID из массива музыки с громкостью по умолчанию.
            ///     Останавливает случайную музыку, если она была включена.
            /// </summary>
            /// <param name="id">ID музыки в массиве _musicClips.</param>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
            public void PlayMusic(int id)
            {
                PlayMusic(id, 1f);
            }

            /// <summary>
            ///     Воспроизводит музыку напрямую по AudioClip с указанной громкостью.
            ///     Останавливает случайную музыку, если она была включена.
            /// </summary>
            /// <param name="clip">AudioClip для воспроизведения.</param>
            /// <param name="volume">Громкость воспроизведения (0-1).</param>
            public void PlayMusicByClip(AudioClip clip, float volume)
            {
                if (_music == null)
                {
                    Debug.LogWarning("[AM] AudioSource для музыки не инициализирован.");
                    return;
                }

                if (clip == null)
                {
                    Debug.LogWarning("[AM] AudioClip равен null.");
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
            ///     Возвращает текущий воспроизводимый музыкальный клип.
            /// </summary>
            /// <returns>Текущий AudioClip или null, если ничего не воспроизводится.</returns>
            public AudioClip GetCurrentMusicClip()
            {
                if (_useRandomMusic && _randomMusicController != null)
                {
                    return _randomMusicController.CurrentTrack;
                }

                return _music != null ? _music.clip : null;
            }

            /// <summary>
            ///     Включает режим случайной музыки из списка треков.
            ///     Останавливает текущую конкретную музыку, если она играет.
            /// </summary>
            public void EnableRandomMusic()
            {
                if (_randomMusicTracks == null || _randomMusicTracks.Length == 0)
                {
                    Debug.LogWarning("[AM] Список треков для случайной музыки пуст.");
                    return;
                }

                if (_music == null)
                {
                    Debug.LogWarning("[AM] AudioSource для музыки не инициализирован.");
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
            ///     Выключает режим случайной музыки.
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
            ///     Возвращает true, если включен режим случайной музыки.
            /// </summary>
            public bool IsRandomMusicEnabled()
            {
                return _useRandomMusic && _randomMusicController != null && _randomMusicController.IsPlaying;
            }

            /// <summary>
            ///     Устанавливает громкость для звуковых эффектов или музыки.
            /// </summary>
            /// <param name="volume">Громкость (0-1).</param>
            /// <param name="efx">true для эффектов, false для музыки.</param>
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
            ///     Применяет стартовые громкости к AudioSource'ам.
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