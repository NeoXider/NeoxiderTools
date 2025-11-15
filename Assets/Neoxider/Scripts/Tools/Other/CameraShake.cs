using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Camera shake component using DOTween with support for position and rotation shake
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(CameraShake))]
    public class CameraShake : MonoBehaviour
    {
        public enum ShakeType
        {
            Position,
            Rotation,
            Both
        }

        [Header("Shake Type")] [Tooltip("What to shake: Position, Rotation, or Both")]
        public ShakeType shakeType = ShakeType.Position;

        [Header("Shake Settings")] [Tooltip("Duration of the shake in seconds")] [Min(0.01f)]
        public float duration = 0.5f;

        [Tooltip("Strength of the shake")] [Min(0f)]
        public float strength = 1f;

        [Tooltip("How many times the shake will vibrate")] [Range(1, 50)]
        public int vibrato = 10;

        [Tooltip("Randomness of the shake (0 = no randomness, 180 = full randomness)")] [Range(0f, 180f)]
        public float randomness = 90f;

        [Tooltip("If true, the shake will fade out smoothly")]
        public bool fadeOut = true;

        [Header("Position Shake Settings")] [Tooltip("Shake on X axis")]
        public bool shakeX = true;

        [Tooltip("Shake on Y axis")] public bool shakeY = true;

        [Tooltip("Shake on Z axis")] public bool shakeZ;

        [Header("Rotation Shake Settings")] [Tooltip("Shake rotation on X axis (pitch)")]
        public bool rotateX;

        [Tooltip("Shake rotation on Y axis (yaw)")]
        public bool rotateY;

        [Tooltip("Shake rotation on Z axis (roll)")]
        public bool rotateZ = true;

        [Header("Advanced Settings")] [Tooltip("Easing type for the shake")]
        public Ease easeType = Ease.Linear;

        [Tooltip("If true, uses unscaled time (ignores Time.timeScale)")]
        public bool useUnscaledTime;

        [Tooltip("If true, the shake will be relative to the current position/rotation")]
        public bool relativeShake = true;

        [Header("Events")] [Tooltip("Called when shake starts")]
        public UnityEvent OnShakeStart;

        [Tooltip("Called when shake completes")]
        public UnityEvent OnShakeComplete;

        [Tooltip("Called when shake is stopped manually")]
        public UnityEvent OnShakeStop;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Tween _positionTween;
        private Tween _rotationTween;

        /// <summary>
        ///     Gets whether the camera is currently shaking
        /// </summary>
        public bool IsShaking { get; private set; }

        private void Awake()
        {
            _originalPosition = transform.localPosition;
            _originalRotation = transform.localRotation;
        }

        private void OnDisable()
        {
            StopShake();
        }

        private void OnDestroy()
        {
            StopShake();
        }

        private void OnValidate()
        {
            if (duration < 0.01f)
            {
                duration = 0.01f;
            }

            if (strength < 0f)
            {
                strength = 0f;
            }

            if (vibrato < 1)
            {
                vibrato = 1;
            }

            if (vibrato > 50)
            {
                vibrato = 50;
            }

            randomness = Mathf.Clamp(randomness, 0f, 180f);
        }

        /// <summary>
        ///     Starts shake with default settings
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void StartShake()
        {
            StartShake(duration, strength);
        }

        /// <summary>
        ///     Starts shake with custom duration and strength
        /// </summary>
        /// <param name="customDuration">Custom duration in seconds</param>
        /// <param name="customStrength">Custom strength</param>
        public void StartShake(float customDuration, float customStrength)
        {
            StopShake();

            IsShaking = true;
            OnShakeStart?.Invoke();

            Sequence sequence = DOTween.Sequence();

            // Position shake
            if (shakeType == ShakeType.Position || shakeType == ShakeType.Both)
            {
                Vector3 shakeStrength = new(
                    shakeX ? customStrength : 0f,
                    shakeY ? customStrength : 0f,
                    shakeZ ? customStrength : 0f
                );

                _positionTween = transform.DOShakePosition(
                    customDuration,
                    shakeStrength,
                    vibrato,
                    randomness,
                    fadeOut,
                    relativeShake
                ).SetEase(easeType);

                if (useUnscaledTime)
                {
                    _positionTween.SetUpdate(true);
                }

                sequence.Join(_positionTween);
            }

            // Rotation shake
            if (shakeType == ShakeType.Rotation || shakeType == ShakeType.Both)
            {
                Vector3 rotationStrength = new(
                    rotateX ? customStrength : 0f,
                    rotateY ? customStrength : 0f,
                    rotateZ ? customStrength : 0f
                );

                _rotationTween = transform.DOShakeRotation(
                    customDuration,
                    rotationStrength,
                    vibrato,
                    randomness,
                    fadeOut
                ).SetEase(easeType);

                if (useUnscaledTime)
                {
                    _rotationTween.SetUpdate(true);
                }

                sequence.Join(_rotationTween);
            }

            sequence.OnComplete(() =>
            {
                IsShaking = false;
                OnShakeComplete?.Invoke();
            });

            sequence.Play();
        }

        /// <summary>
        ///     Stops the current shake immediately
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void StopShake()
        {
            if (!IsShaking)
            {
                return;
            }

            _positionTween?.Kill();
            _rotationTween?.Kill();

            // Reset to original position/rotation
            if (shakeType == ShakeType.Position || shakeType == ShakeType.Both)
            {
                transform.localPosition = _originalPosition;
            }

            if (shakeType == ShakeType.Rotation || shakeType == ShakeType.Both)
            {
                transform.localRotation = _originalRotation;
            }

            IsShaking = false;
            OnShakeStop?.Invoke();
        }

        /// <summary>
        ///     Restores original position and rotation
        /// </summary>
        public void ResetTransform()
        {
            transform.localPosition = _originalPosition;
            transform.localRotation = _originalRotation;
        }
    }
}