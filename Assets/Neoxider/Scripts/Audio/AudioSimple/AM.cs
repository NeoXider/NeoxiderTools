using Neo.Tools;
using UnityEngine;

namespace Neo
{
    namespace Audio
    {
        [RequireComponent(typeof(AudioSource))]
        public class AM : Singleton<AM>
        {
            [SerializeField] private AudioSource _efx;
            [SerializeField] private AudioClip[] _clips;
            [Space]
            [SerializeField] private AudioSource _music;
            [SerializeField] private AudioClip[] _musicClips;
            [Space]
            [Header("Settings")]
            [SerializeField] private float _startVolumeEfx = 1;
            [SerializeField] private float _startVolumeMusic = 0.5f;

            public AudioSource Efx => _efx;
            public AudioSource Music => _music;

            protected override void Init()
            {
                base.Init();

                PlayMusic(0);
            }

            /// <summary>
            /// Play audio by ID from the clips array with a specified volume.
            /// </summary>
            /// <param name="id">The ID of the clip in the array to play.</param>
            /// <param name="volume">The volume level for playback, default is 1.</param>
            public void Play(int id, float volume = -1f)
            {
                if (volume < 0)
                    volume = _startVolumeEfx;

                if (id >= 0 && id < _clips.Length)
                {
                    _efx.PlayOneShot(_clips[id], Mathf.Clamp(volume, 0f, 1f));
                }
                else
                {
                    Debug.LogWarning("Clip ID out of range.");
                }
            }

            public void PlayMusic(int id, float volume = -1f)
            {
                if (volume < 0)
                    volume = _startVolumeMusic;

                if (id >= 0 && id < _musicClips.Length)
                {
                    _music.clip = _musicClips[id];
                    _music.volume = Mathf.Clamp(volume, 0f, 1f);
                    _music.Play();
                }
                else
                {
                    Debug.LogWarning("Music clip ID out of range.");
                }
            }

            public static void Play(int id)
            {
                Instance.Play(id);
            }

            public static void PlayMusic(int id)
            {
                Instance.PlayMusic(id);
            }

            /// <summary>
            /// Set the volume of the audio source.
            /// </summary>
            /// <param name="volume">Volume value between 0 and 1.</param>
            public void SetVolume(float volume, bool efx)
            {
                if (efx)
                    _efx.volume = Mathf.Clamp(volume, 0f, 1f);
                else
                    _music.volume = Mathf.Clamp(volume, 0f, 1f);
            }


            private void OnValidate()
            {
                _efx ??= GetComponent<AudioSource>();
                _efx.playOnAwake = false;

                if (_music == null)
                {
                    CreateMusic();
                }
            }

            private void CreateMusic()
            {
                GameObject obj = new GameObject("Music");
                obj.transform.SetParent(transform, false);

                _music = obj.AddComponent<AudioSource>();
                _music.loop = true;
                _music.volume = .7f;
                _music.priority = 126;
            }
        }
    }
}
