using Neo.Extensions;
using UnityEngine;

namespace Neo
{
    namespace Audio
    {
        /// <summary>Component to play sound effects from AM. Supports playing a specific clip by ID or a random clip from a list.</summary>
        [NeoDoc("Audio/PlayAudio.md")]
        [CreateFromMenu("Neoxider/Audio/PlayAudio")]
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudio))]
        public class PlayAudio : MonoBehaviour
        {
            [Header("Legacy Mode (by ID)")] [SerializeField]
            private int _clipType;

            [Header("New Mode (by Clip)")] [SerializeField]
            private AudioClip[] _clips;

            [SerializeField] private bool _useRandomClip;

            [SerializeField] private bool _playOnAwake;
            [SerializeField] private float _volume = 1;

            private void Start()
            {
                if (_playOnAwake)
                {
                    AudioPlay();
                }
            }

            /// <summary>
            ///     Plays the sound. If clips are set and useRandomClip is true, picks a random clip; otherwise uses legacy ID
            ///     mode.
            /// </summary>
            public void AudioPlay()
            {
                if (_clips != null && _clips.Length > 0)
                {
                    AudioClip clipToPlay;
                    if (_useRandomClip && _clips.Length > 1)
                    {
                        clipToPlay = _clips.GetRandomElement();
                    }
                    else
                    {
                        clipToPlay = _clips[0];
                    }

                    if (clipToPlay != null)
                    {
                        AM.I?.Play(clipToPlay, _volume);
                    }
                    else
                    {
                        Debug.LogWarning("[PlayAudio] Selected clip is null.");
                    }
                }
                else
                {
                    AM.I?.Play(_clipType, _volume);
                }
            }
        }
    }
}
