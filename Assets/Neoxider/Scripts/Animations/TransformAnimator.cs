using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Animations
{
    /// <summary>
    ///     Composable local-transform animation with rotation, bob, scale pulse and shake channels.
    /// </summary>
    [NeoDoc("Animations/TransformAnimator.md")]
    [CreateFromMenu("Neoxider/Animations/TransformAnimator")]
    [AddComponentMenu("Neoxider/" + "Animations/" + nameof(TransformAnimator))]
    public class TransformAnimator : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform to animate. Defaults to this GameObject's transform.")]
        [SerializeField]
        [FormerlySerializedAs("target")]
        private Transform _target;

        [Header("Control")]
        [Tooltip("Start on the first Start call and after every subsequent re-enable (pool-safe)")]
        [SerializeField]
        [FormerlySerializedAs("playOnStart")]
        private bool _playOnEnable = true;

        [Tooltip("Randomize start time so multiple items do not animate in sync")]
        [SerializeField]
        [FormerlySerializedAs("randomizeStartTime")]
        private bool _randomizeStartTime = true;

        [Tooltip("Use unscaled time (ignores Time.timeScale)")]
        [SerializeField]
        [FormerlySerializedAs("useUnscaledTime")]
        private bool _useUnscaledTime;

        [Header("Channels")]
        [SerializeField]
        [FormerlySerializedAs("settings")]
        private TransformAnimationSettings _settings = new();

        [Header("Events")]
        [Tooltip("Invoked when animation starts")]
        [SerializeField]
        [FormerlySerializedAs("OnAnimationStarted")]
        private UnityEvent _onAnimationStarted = new();

        [Tooltip("Invoked when animation stops")]
        [SerializeField]
        [FormerlySerializedAs("OnAnimationStopped")]
        private UnityEvent _onAnimationStopped = new();

        [Tooltip("Invoked when animation is paused")]
        [SerializeField]
        [FormerlySerializedAs("OnAnimationPaused")]
        private UnityEvent _onAnimationPaused = new();

        private Vector3 _baseEulerAngles;
        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private Transform _capturedTarget;
        private bool _hasBase;
        private bool _hasStarted;
        private float _impulseStrength;
        private float _impulseTime = -1f;
        private float _seed;
        private float _time;

        /// <summary>The effective transform being animated. Assign through this property or <see cref="SetTarget" />.</summary>
        public Transform Target
        {
            get => ResolveTarget();
            set => SetTarget(value);
        }

        /// <summary>Whether the component automatically plays after startup and pool re-enables.</summary>
        public bool PlayOnEnable
        {
            get => _playOnEnable;
            set => _playOnEnable = value;
        }

        /// <summary>Whether each enable randomizes the clock and noise seed.</summary>
        public bool RandomizeStartTime
        {
            get => _randomizeStartTime;
            set => _randomizeStartTime = value;
        }

        /// <summary>Whether the clock ignores <see cref="UnityEngine.Time.timeScale" />.</summary>
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        /// <summary>Animation channel settings. Null is valid and evaluates to the captured base pose.</summary>
        public TransformAnimationSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        /// <summary>Invoked when animation starts or resumes.</summary>
        public UnityEvent OnAnimationStarted => _onAnimationStarted;

        /// <summary>Invoked when animation is explicitly stopped.</summary>
        public UnityEvent OnAnimationStopped => _onAnimationStopped;

        /// <summary>Invoked when animation is paused.</summary>
        public UnityEvent OnAnimationPaused => _onAnimationPaused;

        /// <summary>Whether the animation is currently playing.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Whether the animation is paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Current animation time in seconds.</summary>
        public float Time => _time;

        private void OnEnable()
        {
            CaptureBase();

            if (_randomizeStartTime)
            {
                RandomizeTime();
            }

            if (_hasStarted && _playOnEnable)
            {
                Play();
            }
        }

        private void Start()
        {
            _hasStarted = true;
            if (_playOnEnable)
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

            float deltaTime = _useUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
            Advance(deltaTime);
        }

        private void OnDisable()
        {
            RestoreCapturedBase();
            IsPlaying = false;
            IsPaused = false;
            _impulseTime = -1f;
        }

        private void Advance(float deltaTime)
        {
            if (!_hasBase)
            {
                CaptureBase();
            }

            _time += Mathf.Max(0f, deltaTime);

            if (_impulseTime >= 0f)
            {
                _impulseTime += Mathf.Max(0f, deltaTime);
                float impulseDuration = _settings != null ? _settings.ImpulseDuration : 0f;
                if (impulseDuration <= 0f || _impulseTime > impulseDuration)
                {
                    _impulseTime = -1f;
                }
            }

            ApplyCurrentState();
        }

        /// <summary>
        ///     Changes the animated transform, restores the old captured target and captures the new target's pose.
        ///     Pass null to animate this component's own transform.
        /// </summary>
        public void SetTarget(Transform target)
        {
            Transform nextTarget = target != null ? target : transform;
            if (_capturedTarget == nextTarget && ResolveTarget() == nextTarget)
            {
                _target = target;
                return;
            }

            RestoreCapturedBase();
            _target = target;
            CaptureBase();

            if (IsPlaying && !IsPaused)
            {
                ApplyCurrentState();
            }
        }

        /// <summary>Captures the current local pose of the effective target as the animation base.</summary>
        [Button("Capture Base")]
        public void CaptureBase()
        {
            Transform currentTarget = ResolveTarget();
            if (currentTarget == null)
            {
                _capturedTarget = null;
                _hasBase = false;
                return;
            }

            _capturedTarget = currentTarget;
            _basePosition = currentTarget.localPosition;
            _baseEulerAngles = currentTarget.localEulerAngles;
            _baseScale = currentTarget.localScale;
            _hasBase = true;
        }

        /// <summary>Starts the animation. Calling it while already playing is idempotent.</summary>
        [Button(PlayModeOnly = true)]
        public void Play()
        {
            if (IsPlaying && !IsPaused)
            {
                return;
            }

            IsPlaying = true;
            IsPaused = false;
            _onAnimationStarted?.Invoke();
        }

        /// <summary>Stops the animation and restores the exact target whose base was captured.</summary>
        [Button(PlayModeOnly = true)]
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            _impulseTime = -1f;
            RestoreCapturedBase();
            _onAnimationStopped?.Invoke();
        }

        /// <summary>Pauses the animation.</summary>
        public void Pause()
        {
            if (!IsPlaying || IsPaused)
            {
                return;
            }

            IsPaused = true;
            _onAnimationPaused?.Invoke();
        }

        /// <summary>Resumes the animation from pause.</summary>
        public void Resume()
        {
            if (!IsPlaying || !IsPaused)
            {
                return;
            }

            IsPaused = false;
            _onAnimationStarted?.Invoke();
        }

        /// <summary>Triggers a one-shot impulse shake. Null settings and disabled durations are safe.</summary>
        public void Shake(float strength)
        {
            _impulseStrength = Mathf.Max(0f, strength);
            _impulseTime = _impulseStrength > 0f ? 0f : -1f;
        }

        /// <summary>Triggers a one-shot impulse shake at full strength.</summary>
        [Button(PlayModeOnly = true)]
        public void Shake()
        {
            Shake(1f);
        }

        /// <summary>Resets animation time to zero.</summary>
        public void ResetTime()
        {
            _time = 0f;
        }

        /// <summary>Sets a random initial animation time and seed.</summary>
        public void RandomizeTime()
        {
            _time = Random.Range(0f, 1000f);
            _seed = Random.Range(0f, 1000f);
        }

        /// <summary>Evaluates the current state and applies it only to the target whose base is captured.</summary>
        public void ApplyCurrentState()
        {
            Transform currentTarget = ResolveTarget();
            if (!_hasBase || _capturedTarget != currentTarget)
            {
                CaptureBase();
            }

            if (!_hasBase || _capturedTarget == null)
            {
                return;
            }

            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                _settings, _basePosition, _baseEulerAngles, _baseScale, _time, _seed,
                _impulseTime, _impulseStrength);

            _capturedTarget.localPosition = state.LocalPosition;
            _capturedTarget.localEulerAngles = state.LocalEulerAngles;
            _capturedTarget.localScale = state.LocalScale;
        }

        private Transform ResolveTarget()
        {
            return _target != null ? _target : transform;
        }

        private void RestoreCapturedBase()
        {
            if (!_hasBase || _capturedTarget == null)
            {
                return;
            }

            _capturedTarget.localPosition = _basePosition;
            _capturedTarget.localEulerAngles = _baseEulerAngles;
            _capturedTarget.localScale = _baseScale;
        }
    }
}
