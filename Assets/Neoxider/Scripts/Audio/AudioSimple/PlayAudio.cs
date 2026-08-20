using Neo.Extensions;
using UnityEngine;

namespace Neo.Audio
{
        /// <summary>Component to play sound effects from AM. Supports playing a specific clip by ID or a random clip from a list.</summary>
        [NeoDoc("Audio/PlayAudio.md")]
        [CreateFromMenu("Neoxider/Audio/PlayAudio")]
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudio))]
        public class PlayAudio : MonoBehaviour
        {
            [Header("By Sound Id (recommended)")]
            [Tooltip("Sound entry on AM, chosen by id. Survives reordering the list, unlike the index below.")]
            [AudioId(AudioIdKind.Sound)]
            [SerializeField]
            private string _soundId = string.Empty;

            [Header("Legacy Mode (by index)")] [SerializeField]
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
            ///     Plays the sound. Explicit clips win, then the sound id, then the legacy index - so an
            ///     existing component that only has an index set behaves exactly as it always did.
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
                        NeoDiagnostics.LogWarning("[PlayAudio] Selected clip is null.");
                    }
                }
                else if (!string.IsNullOrEmpty(_soundId))
                {
                    AM.I?.Play(_soundId, _volume);
                }
                else
                {
                    AM.I?.Play(_clipType, _volume);
                }
            }
        }
}
