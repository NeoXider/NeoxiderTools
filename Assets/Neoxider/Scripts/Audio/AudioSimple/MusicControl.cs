using UnityEngine;

namespace Neo.Audio
{
    /// <summary>
    ///     Drives <see cref="AM" />'s music from the inspector, with no code: start a pool by id on enable
    ///     or from a UnityEvent, move to another track of the pool, stop.
    ///     <para>
    ///         Every public method here takes zero or one argument, so all of them show up in a UnityEvent
    ///         dropdown - a button, a trigger volume or a state machine can drive the soundtrack directly.
    ///     </para>
    /// </summary>
    [NeoDoc("Audio/MusicControl.md")]
    [CreateFromMenu("Neoxider/Audio/MusicControl")]
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(MusicControl))]
    public class MusicControl : MonoBehaviour
    {
        /// <summary>How this component changes music.</summary>
        public enum TransitionMode
        {
            /// <summary>Use the crossfade configured on AM and on the pool.</summary>
            Default = 0,

            /// <summary>Crossfade over <see cref="_fadeSeconds" />.</summary>
            Fade = 1,

            /// <summary>Cut straight in.</summary>
            Instant = 2
        }

        [Tooltip("Music pool to control, chosen from the ids configured on AM.")]
        [AudioId(AudioIdKind.Music)]
        [SerializeField]
        private string _poolId = string.Empty;

        [Tooltip("Start this pool automatically when the object is enabled.")]
        [SerializeField]
        private bool _playOnEnable;

        [Header("Transition")]
        [Tooltip("Default uses AM's crossfade. Fade uses the length below. Instant cuts.")]
        [SerializeField]
        private TransitionMode _transition = TransitionMode.Default;

        [Range(0f, 10f)]
        [SerializeField]
        private float _fadeSeconds = 0.8f;

        [Header("Volume")]
        [Tooltip("Play the pool at this volume instead of its own. Negative = keep the pool's setting. " +
                 "This is a multiplier of the music channel, so the player's volume slider still applies.")]
        [Range(-1f, 1f)]
        [SerializeField]
        private float _volumeOverride = -1f;

        /// <summary>Pool this component starts.</summary>
        public string PoolId
        {
            get => _poolId;
            set => _poolId = value ?? string.Empty;
        }

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                PlayPool();
            }
        }

        /// <summary>Starts the configured pool. Safe to call repeatedly - it will not restart the track.</summary>
        public void PlayPool()
        {
            PlayPool(_poolId);
        }

        /// <summary>Starts a pool by id, using this component's transition and volume settings.</summary>
        /// <param name="id">Music entry id configured on AM.</param>
        public void PlayPool(string id)
        {
            if (AM.I == null)
            {
                NeoDiagnostics.LogWarning("[MusicControl] No AM in the scene.");
                return;
            }

            if (string.IsNullOrEmpty(id))
            {
                NeoDiagnostics.LogWarning($"[MusicControl] '{name}' has no music pool id set.");
                return;
            }

            AM.I.PlayMusicPool(id, BuildOptions());
        }

        /// <summary>Moves to another random track of the pool that is currently playing.</summary>
        public void NextTrack()
        {
            if (AM.I == null)
            {
                NeoDiagnostics.LogWarning("[MusicControl] No AM in the scene.");
                return;
            }

            AM.I.TryNextMusicTrack(BuildOptions());
        }

        /// <summary>Stops the music, using this component's transition setting.</summary>
        public void StopMusic()
        {
            if (AM.I == null)
            {
                return;
            }

            AM.I.StopMusic(BuildOptions().Transition);
        }

        private MusicOptions BuildOptions()
        {
            MusicOptions options = default;

            switch (_transition)
            {
                case TransitionMode.Fade:
                    options.Transition = MusicTransition.Fade(_fadeSeconds);
                    break;
                case TransitionMode.Instant:
                    options.Transition = MusicTransition.Instant;
                    break;
            }

            if (_volumeOverride >= 0f)
            {
                options.VolumeOverride = Mathf.Clamp01(_volumeOverride);
            }

            return options;
        }
    }
}
