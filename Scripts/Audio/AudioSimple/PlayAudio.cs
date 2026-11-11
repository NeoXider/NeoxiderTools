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

            // WHY the default is negative and not 1: this value REPLACES the entry's own volume multiplier.
            // Defaulting it to 1 meant that pointing a fresh component at a sound entry authored at 0.5
            // silently played it at full - the entry's slider had no effect and nothing said so. Negative
            // means "whatever the entry says", which is what a component that only names an id should do.
            // Components serialized before this carry an explicit 1 and go on overriding, as they always did.
            [Tooltip("Volume for this play, replacing the AM entry's own volume multiplier. " +
                     "Negative = use the entry's volume. Still multiplied by the effects channel.")]
            [Range(-1f, AudioEntry.MaxVolume)]
            [SerializeField]
            private float _volume = -1f;

            /// <summary>True when this component overrides the entry's own volume rather than deferring to it.</summary>
            private bool HasVolumeOverride => _volume >= 0f;

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
                        // A loose clip has no entry to defer to, so "no override" is simply full volume.
                        AM.I?.Play(clipToPlay, HasVolumeOverride ? _volume : 1f);
                    }
                    else
                    {
                        NeoDiagnostics.LogWarning("[PlayAudio] Selected clip is null.");
                    }
                }
                else if (!string.IsNullOrEmpty(_soundId))
                {
                    if (HasVolumeOverride)
                    {
                        AM.I?.Play(_soundId, _volume);
                    }
                    else
                    {
                        AM.I?.Play(_soundId);
                    }
                }
                else if (HasVolumeOverride)
                {
                    AM.I?.Play(_clipType, _volume);
                }
                else
                {
                    AM.I?.Play(_clipType);
                }
            }
        }
}
