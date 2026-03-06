using UnityEngine;
using UnityEngine.Events;

namespace Neo.Animations
{
    /// <summary>
    ///     Universal animator for Vector3 values. Provides an easy way to animate position, scale, rotation and other Vector3 parameters.
    /// </summary>
    [NeoDoc("Animations/Vector3Animator.md")]
    [CreateFromMenu("Neoxider/Animations/Vector3Animator")]
    [AddComponentMenu("Neoxider/" + "Animations/" + nameof(Vector3Animator))]
    public class Vector3Animator : MonoBehaviour
    {
        [Header("Animation Settings")] [Tooltip("Animation type")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Vector Settings")] [Tooltip("Start vector")]
        public Vector3 startVector = Vector3.zero;

        [Tooltip("End vector")] public Vector3 endVector = Vector3.one;

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

        [Tooltip("Invoked when vector changes")]
        public UnityEvent<Vector3> OnVectorChanged;

        [Tooltip("Invoked when animation starts")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Invoked when animation stops")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Invoked when animation is paused")]
        public UnityEvent OnAnimationPaused;

        private float animationTime;
        private Vector3 lastVector;
        private Vector2 randomOffset;

        /// <summary>Current animated vector (read-only).</summary>
        public Vector3 CurrentVector { get; private set; }

        /// <summary>Whether the animation is currently playing.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Whether the animation is paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Start vector (writable from outside).</summary>
        public Vector3 StartVector
        {
            get => startVector;
            set => startVector = value;
        }

        /// <summary>End vector (writable from outside).</summary>
        public Vector3 EndVector
        {
            get => endVector;
            set => endVector = value;
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

            CurrentVector = startVector;
            lastVector = startVector;

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

            // Получаем новый вектор
            Vector3 newVector = AnimationUtils.GetAnimatedVector3(
                animationType,
                startVector, endVector,
                animationTime, animationSpeed,
                customCurve);

            CurrentVector = newVector;

            // Вызываем событие если вектор изменился
            if (Vector3.Distance(newVector, lastVector) > 0.001f)
            {
                OnVectorChanged?.Invoke(newVector);
                lastVector = newVector;
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