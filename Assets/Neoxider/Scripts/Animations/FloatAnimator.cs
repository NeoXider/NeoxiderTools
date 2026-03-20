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

        [Tooltip("Reactive value; subscribe via Value.OnChanged")]
        public ReactivePropertyFloat Value = new();

        [Tooltip("Invoked when animation starts")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Invoked when animation stops")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Invoked when animation is paused")]
        public UnityEvent OnAnimationPaused;

        private float animationTime;
        private float lastValue;
        private Vector2 randomOffset;

        /// <summary>Current animated value (read-only).</summary>
        public float CurrentValue => Value.CurrentValue;

        /// <summary>Current value (for NeoCondition and reflection).</summary>
        public float ValueFloat => Value.CurrentValue;

        /// <summary>Whether the animation is currently playing.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Whether the animation is paused.</summary>
        public bool IsPaused { get; private set; }

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
            animationTime = Random.Range(0f, 1000f);
            randomOffset = new Vector2(Random.Range(-1000f, 1000f),
                Random.Range(-1000f, 1000f));

            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!IsPlaying || IsPaused)
            {
                return;
            }

            animationTime += Time.deltaTime;

            // Получаем новое значение
            float newValue = AnimationUtils.GetTargetValue(
                animationType,
                minValue, maxValue,
                animationTime, animationSpeed,
                use2DNoise, randomOffset, noiseOffset, noiseScale,
                customCurve);

            if (Mathf.Abs(newValue - lastValue) > 0.001f)
            {
                Value.Value = newValue;
                lastValue = newValue;
            }
        }

        private void OnValidate()
        {
            if (maxValue < minValue)
            {
                maxValue = minValue;
            }
        }

        /// <summary>Starts the animation.</summary>
        public void Play()
        {
            IsPlaying = true;
            IsPaused = false;
            OnAnimationStarted?.Invoke();
        }

        /// <summary>Stops the animation.</summary>
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            OnAnimationStopped?.Invoke();
        }

        /// <summary>Pauses the animation.</summary>
        public void Pause()
        {
            if (IsPlaying)
            {
                IsPaused = true;
                OnAnimationPaused?.Invoke();
            }
        }

        /// <summary>Resumes the animation from pause.</summary>
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
