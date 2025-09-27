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

        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AM))]
        public class AM : Singleton<AM>
        {
            [SerializeField] private AudioSource _efx;
            [Space] [SerializeField] private AudioSource _music;
            [SerializeField] private AudioClip[] _musicClips;
            [SerializeField] private Sound[] _sounds;

            public float startVolumeEfx { get; set; } = 1f;
            public float startVolumeMusic { get; set; } = 1f;

            public AudioSource Efx => _efx;
            public AudioSource Music => _music;

            protected override void Init()
            {
                base.Init();

                if (_musicClips.Length > 0)
                    PlayMusic(0);
            }

            [Button]
            public void Play(int id, float volume)
            {
                if (id >= 0 && id < _sounds.Length)
                    _efx.PlayOneShot(_sounds[id].clip, Mathf.Clamp(volume, 0f, 1f));
                else
                    Debug.LogWarning($"Sound ID {id} is out of range.");
            }

            [Button]
            public void Play(int id)
            {
                if (id >= 0 && id < _sounds.Length)
                {
                    var soundMultiplier = _sounds[id].volume;
                    soundMultiplier = soundMultiplier == 0 ? 1 : soundMultiplier;
                    Play(id, soundMultiplier);
                }
                else
                {
                    Debug.LogWarning($"Sound ID {id} is out of range.");
                }
            }

            [Button]
            public void PlayMusic(int id, float volume)
            {
                if (id >= 0 && id < _musicClips.Length)
                {
                    _music.clip = _musicClips[id];
                    _music.volume = Mathf.Clamp(volume, 0f, 1f);
                    _music.Play();
                }
                else
                {
                    Debug.LogWarning($"Music clip ID {id} is out of range.");
                }
            }

            [Button]
            public void PlayMusic(int id)
            {
                PlayMusic(id, 1f); // Используем базовую громкость, стартовая применяется через AMSettings
            }

            public void SetVolume(float volume, bool efx)
            {
                if (efx)
                    _efx.volume = Mathf.Clamp(volume, 0f, 1f);
                else
                    _music.volume = Mathf.Clamp(volume, 0f, 1f);
            }

            /// <summary>
            ///     Применяет стартовые громкости к AudioSource'ам
            /// </summary>
            public void ApplyStartVolumes()
            {
                if (_efx != null)
                    _efx.volume = startVolumeEfx;
                if (_music != null)
                    _music.volume = startVolumeMusic;
            }


            private void OnValidate()
            {
                if (_music == null) CreateMusic();

                if (_efx == null) CreateEfx();
            }

            private void CreateMusic()
            {
                var obj = new GameObject("Music");
                obj.transform.SetParent(transform, false);

                _music = obj.AddComponent<AudioSource>();
                _music.loop = true;
                _music.volume = .7f;
                _music.priority = 126;
            }

            private void CreateEfx()
            {
                var obj = new GameObject("Efx");
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