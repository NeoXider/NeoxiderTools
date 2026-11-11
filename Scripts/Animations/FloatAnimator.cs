using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Animations
{
    /// <summary>
    ///     Universal animator for float values. Provides an easy way to animate any numeric value with various animation
    ///     types.
    /// </summary>
    [NeoDoc("Animations/FloatAnimator.md")]
    [CreateFromMenu("Neoxider/Animations/FloatAnimator")]
    [AddComponentMenu("Neoxider/" + "Animations/" + nameof(FloatAnimator))]
    public class FloatAnimator : MonoBehaviour
    {
        [Header("Animation Settings")] [Tooltip("Animation type")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Value Settings")] [Tooltip("Min value")]
        public float minValue;

        [Tooltip("Max value")] public float maxValue = 1f;

        [Tooltip("Animation speed (0 = disabled)")] [Range(0f, 30f)]
        public float animationSpeed = 1.0f;

        [Header("Noise Settings")] [Tooltip("Noise scale for PerlinNoise")] [Range(0.1f, 20f)]
        public float noiseScale = 1f;

        [Tooltip("Use 2D noise instead of 1D")]
        public bool use2DNoise = true;

        [Tooltip("Additional noise offset")] public Vector2 noiseOffset;

        [Header("Custom Curve")] [Tooltip("Custom curve for CustomCurve type")]
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Control")] [Tooltip("Auto-start animation on Start")]
        public bool playOnStart = true;

        [Tooltip("Randomize the clock and noise offset in Start so several animators do not move in sync. " +
                 "Off = deterministic start at time 0, or at the time set by ResetTime before Start.")]
        [SerializeField] private bool _randomizeStartTime = true;

        [Tooltip("Advance on unscaled time (ignores Time.timeScale), e.g. for UI shown during pause.")]
        [SerializeField] private bool _useUnscaledTime;

        [Tooltip("Reactive value; subscribe via Value.OnChanged")]
        public ReactivePropertyFloat Value = new();

        [Tooltip("Invoked when animation starts")]
        public UnityEvent OnAnimationStarted = new();

        [Tooltip("Invoked when animation stops")]
        public UnityEvent OnAnimationStopped = new();

        [Tooltip("Invoked when animation is paused")]
        public UnityEvent OnAnimationPaused = new();

        private float animationTime;
        private Vector2 randomOffset;
        private bool _disabled;

        /// <summary>Current animated value (read-only).</summary>
        public float CurrentValue => Value.CurrentValue;

        /// <summary>Current value (for NeoCondition and reflection).</summary>
        public float ValueFloat => Value.CurrentValue;

        /// <summary>Whether the animation is currently playing.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Whether the animation is paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     When true, <c>Update</c> does nothing and the owner advances the animation through <see cref="Tick" />
        ///     (custom update loops, deterministic tests, frame-stepped replays). Not serialized.
        /// </summary>
        public bool ManualUpdate { get; set; }

        /// <summary>Whether <c>Start</c> randomizes the clock and noise offset (the legacy default).</summary>
        public bool RandomizeStartTime
        {
            get => _randomizeStartTime;
            set => _randomizeStartTime = value;
        }

        /// <summary>Whether <c>Update</c> advances on <see cref="Time.unscaledDeltaTime" />.</summary>
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        /// <summary>Current animation clock in seconds.</summary>
        public float AnimationTime => animationTime;

        /// <summary>Minimum value (writable from outside).</summary>
        public float MinValue
        {
            get => minValue;
            set => minValue = value;
        }

        /// <summary>Maximum value (writable from outside).</summary>
        public float MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }

        /// <summary>Animation speed (writable from outside).</summary>
        public float AnimationSpeed
        {
            get => animationSpeed;
            set => animationSpeed = value;
        }

        /// <summary>Animation type (writable from outside).</summary>
        public AnimationType AnimationType
        {
            get => animationType;
            set => animationType = value;
        }

        private void Start()
        {
            if (_randomizeStartTime)
            {
                animationTime = Random.Range(0f, 1000f);
                randomOffset = new Vector2(Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f));
            }

            if (playOnStart)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            _disabled = false;
        }

        private void OnDisable()
        {
            _disabled = true;
        }

        private void Update()
        {
            if (ManualUpdate)
            {
                return;
            }

            Tick(_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>
        ///     Advances the clock by <paramref name="deltaTime" /> seconds and publishes the sampled value. Does
        ///     nothing while stopped, paused or disabled (like <c>Update</c>); a negative or non-finite delta samples
        ///     without advancing.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_disabled || !IsPlaying || IsPaused)
            {
                return;
            }

            animationTime += AnimationUtils.SanitizeDeltaTime(deltaTime);

            float newValue = AnimationUtils.GetTargetValue(
                animationType,
                minValue, maxValue,
                animationTime, animationSpeed,
                use2DNoise, randomOffset, noiseOffset, noiseScale,
                customCurve);

            // WHY no change check here: the reactive property compares against its actual value and suppresses
            // unchanged notifications.
            Value.Value = newValue;
        }

        private void OnValidate()
        {
            if (maxValue < minValue)
            {
                maxValue = minValue;
            }
        }

        /// <summary>Starts the animation.</summary>
        [Button(PlayModeOnly = true)]
        public void Play()
        {
            IsPlaying = true;
            IsPaused = false;
            OnAnimationStarted?.Invoke();
        }

        /// <summary>Stops the animation.</summary>
        [Button(PlayModeOnly = true)]
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            OnAnimationStopped?.Invoke();
        }

        /// <summary>Pauses the animation.</summary>
        [Button(PlayModeOnly = true)]
        public void Pause()
        {
            if (IsPlaying)
            {
                IsPaused = true;
                OnAnimationPaused?.Invoke();
            }
        }

        /// <summary>Resumes the animation from pause.</summary>
        [Button(PlayModeOnly = true)]
        public void Resume()
        {
            if (IsPaused)
            {
                IsPaused = false;
                OnAnimationStarted?.Invoke();
            }
        }

        /// <summary>Resets animation time to zero.</summary>
        public void ResetTime()
        {
            animationTime = 0f;
        }

        /// <summary>Sets a random initial animation time.</summary>
        public void RandomizeTime()
        {
            animationTime = Random.Range(0f, 1000f);
        }
    }
}
