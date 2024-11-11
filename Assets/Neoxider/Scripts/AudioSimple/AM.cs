using System.Collections.Generic;
using UnityEngine;

namespace Neoxider
{
    namespace Audio
    {
        public class AM : MonoBehaviour
        {
            public static AM Instance;

            [SerializeField] private AudioSource _audioSource;  
            [SerializeField] private AudioClip[] _clips;      

            [Range(0f, 1f)]
            [SerializeField] private float defaultVolume = 1f;  

            private void Awake()
            {
                Instance = this;

                _audioSource.volume = defaultVolume;
            }

            /// <summary>
            /// Play audio by ID from the clips array with a specified volume.
            /// </summary>
            /// <param name="id">The ID of the clip in the array to play.</param>
            /// <param name="volume">The volume level for playback, default is 1.</param>
            public void Play(int id, float volume = 1f)
            {
                if (id >= 0 && id < _clips.Length)
                {
                    _audioSource.PlayOneShot(_clips[id], Mathf.Clamp(volume, 0f, 1f));
                }
                else
                {
                    Debug.LogWarning("Clip ID out of range.");
                }
            }

            public static void Play(int id)
            {
                Instance.Play(id);
            }

            /// <summary>
            /// Set the volume of the audio source.
            /// </summary>
            /// <param name="volume">Volume value between 0 and 1.</param>
            public void SetVolume(float volume)
            {
                _audioSource.volume = Mathf.Clamp(volume, 0f, 1f);
            }

            /// <summary>
            /// Reset the volume to default value.
            /// </summary>
            public void ResetVolume()
            {
                _audioSource.volume = defaultVolume;
            }
        }
    }
}
