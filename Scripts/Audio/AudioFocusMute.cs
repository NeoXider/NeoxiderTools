using UnityEngine;

namespace Neo.Audio
{
    /// <summary>
    ///     Silences all game audio while the application has no focus: a minimized window, another browser tab or
    ///     another application in front. When focus returns, the listener volume it found comes back.
    ///     <para>
    ///         It works through <see cref="AudioListener.volume" />, so every source (music, effects, footsteps,
    ///         UI) goes quiet at once and keeps its playback position. Nothing is paused, so sounds triggered while
    ///         unfocused do not burst out together on return, and a game's own pause logic on
    ///         <see cref="AudioListener.pause" /> is left alone.
    ///     </para>
    ///     <para>
    ///         In WebGL, <c>OnApplicationFocus</c> follows the page window: switching tabs, minimizing the browser
    ///         and focusing another window all count as losing focus. Audio played outside Unity (an HTML video, an
    ///         iframe) is not affected.
    ///     </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Neoxider/Audio/Audio Focus Mute")]
    public sealed class AudioFocusMute : MonoBehaviour
    {
        [Tooltip("Silence all audio while the application is unfocused (minimized, another tab or application).")]
        [SerializeField]
        private bool _muteWhenUnfocused = true;

        [Tooltip("Also silence in the Unity Editor when it loses focus during Play Mode.")]
        [SerializeField]
        private bool _muteInEditor = true;

        private bool _muted;
        private float _restoreVolume = 1f;

        /// <summary>Whether this component currently holds the listener volume at zero.</summary>
        public bool IsMuted => _muted;

        /// <summary>Silence audio while unfocused. Turning it off while muted restores the volume at once.</summary>
        public bool MuteWhenUnfocused
        {
            get => _muteWhenUnfocused;
            set
            {
                _muteWhenUnfocused = value;
                if (!value)
                {
                    Restore();
                }
            }
        }

        /// <summary>Silence also in the Unity Editor when it loses focus during Play Mode.</summary>
        public bool MuteInEditor
        {
            get => _muteInEditor;
            set
            {
                _muteInEditor = value;
                if (!value && Application.isEditor)
                {
                    Restore();
                }
            }
        }

        /// <summary>
        ///     Returns the scene's instance, creating a persistent one (<c>DontDestroyOnLoad</c>) when there is
        ///     none. One call at startup is enough for the whole game.
        /// </summary>
        public static AudioFocusMute Install() => Install(true);

        /// <inheritdoc cref="Install()" />
        /// <param name="muteInEditor">Whether an unfocused Unity Editor in Play Mode is silenced too.</param>
        public static AudioFocusMute Install(bool muteInEditor)
        {
            AudioFocusMute mute = FindFirstObjectByType<AudioFocusMute>(FindObjectsInactive.Include);
            if (mute == null)
            {
                GameObject host = new(nameof(AudioFocusMute));
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(host);
                }

                mute = host.AddComponent<AudioFocusMute>();
            }

            mute.MuteInEditor = muteInEditor;
            return mute;
        }

        /// <summary>Applies a focus change; the Unity callbacks route here, hosts and tests may call it too.</summary>
        public void SetFocused(bool focused)
        {
            if (focused)
            {
                Restore();
                return;
            }

            if (_muted || !_muteWhenUnfocused || !enabled || !gameObject.activeInHierarchy ||
                (Application.isEditor && !_muteInEditor))
            {
                return;
            }

            _restoreVolume = AudioListener.volume;
            AudioListener.volume = 0f;
            _muted = true;
        }

        // WHY: a game opened in a background tab or behind another window gets no focus callback until the
        // learner comes back, so the current state is applied on enable, not only on the next change.
        private void OnEnable() => SetFocused(Application.isFocused);

        private void OnApplicationFocus(bool hasFocus) => SetFocused(hasFocus);

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SetFocused(false);
            }
            else if (Application.isFocused)
            {
                SetFocused(true);
            }
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            if (!_muted)
            {
                return;
            }

            _muted = false;
            AudioListener.volume = _restoreVolume;
        }
    }
}
