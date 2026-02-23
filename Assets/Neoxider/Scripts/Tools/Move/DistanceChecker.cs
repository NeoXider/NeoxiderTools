using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Monitors distance between two objects and triggers events on approach/departure.
    ///     Optimized using squared distance calculations.
    /// </summary>
    [NeoDoc("Tools/Move/DistanceChecker.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/DistanceChecker")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(DistanceChecker))]
    public class DistanceChecker : MonoBehaviour
    {
        public enum UpdateMode
        {
            EveryFrame,
            FixedInterval
        }

        [Header("Objects")] [Tooltip("Source object. Uses this transform if not set.")] [SerializeField]
        private Transform currentObject;

        [Tooltip("Target object to measure distance to.")] [SerializeField]
        private Transform targetObject;

        [Header("Distance Settings")]
        [Tooltip("Distance threshold for triggering approach/depart events.")]
        [SerializeField] private float distanceThreshold = 5f;
        [Tooltip("Optional hysteresis: depart event when distance > threshold + this value. Reduces flicker at boundary.")]
        [SerializeField] [Min(0f)] private float hysteresisOffset = 0f;

        [Header("Update Settings")] [SerializeField]
        private UpdateMode updateMode = UpdateMode.EveryFrame;

        [Tooltip("Update interval in seconds (only for FixedInterval mode).")] [SerializeField]
        private float updateInterval = 0.1f;

        [Header("Continuous Tracking")]
        [Tooltip("Enable continuous distance tracking with events.")]
        [SerializeField] private bool enableContinuousTracking;
        [Tooltip("Minimum distance change to invoke On Distance Changed (avoids spam).")]
        [SerializeField] private float continuousEventThreshold = 0.01f;

        public UnityEvent onApproach;

        public UnityEvent onDepart;

        [Header("Continuous Distance Event")]
        [Tooltip("Reactive distance to target; subscribe via Distance.OnChanged")]
        public ReactivePropertyFloat Distance = new();

        /// <summary>Текущая дистанция до цели (для NeoCondition и рефлексии).</summary>
        public float DistanceValue => Distance.CurrentValue;

        [Header("Debug")] [SerializeField] private bool showDebugGizmos = true;

        private float _distanceThresholdSqr;
        private float _departThresholdSqr;

        private bool _isWithinDistance;
        private float _lastDistance;
        private float _lastUpdateTime;

        private void Awake()
        {
            if (currentObject == null)
            {
                currentObject = transform;
            }

            RecalculateThresholdSqr();
        }

        private void RecalculateThresholdSqr()
        {
            _distanceThresholdSqr = distanceThreshold * distanceThreshold;
            float departThreshold = distanceThreshold + hysteresisOffset;
            _departThresholdSqr = departThreshold * departThreshold;
        }

        private void Update()
        {
            if (currentObject == null || targetObject == null)
            {
                return;
            }

            if (updateMode == UpdateMode.FixedInterval)
            {
                if (Time.time - _lastUpdateTime < updateInterval)
                {
                    return;
                }

                _lastUpdateTime = Time.time;
            }

            CheckDistance();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || currentObject == null)
            {
                return;
            }

            Transform source = currentObject != null ? currentObject : transform;

            Gizmos.color = _isWithinDistance ? Color.green : Color.red;
            Gizmos.DrawWireSphere(source.position, distanceThreshold);

            if (targetObject != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(source.position, targetObject.position);
            }
        }

        private void OnValidate()
        {
            RecalculateThresholdSqr();
        }

        private void CheckDistance()
        {
            float distanceSqr = (targetObject.position - currentObject.position).sqrMagnitude;
            bool approachNow = distanceSqr < _distanceThresholdSqr;
            bool departNow = distanceSqr > _departThresholdSqr;

            if (approachNow && !_isWithinDistance)
            {
                _isWithinDistance = true;
                onApproach?.Invoke();
            }
            else if (departNow && _isWithinDistance)
            {
                _isWithinDistance = false;
                onDepart?.Invoke();
            }

            if (enableContinuousTracking)
            {
                float distance = Mathf.Sqrt(distanceSqr);
                if (Mathf.Abs(distance - _lastDistance) > continuousEventThreshold)
                {
                    _lastDistance = distance;
                    Distance.Value = distance;
                }
            }
        }

        #region === Public API ===

        /// <summary>
        ///     Returns current distance to target.
        /// </summary>
        public float GetCurrentDistance()
        {
            if (currentObject == null || targetObject == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(currentObject.position, targetObject.position);
        }

        /// <summary>
        ///     Returns true if target is within threshold distance.
        /// </summary>
        public bool IsWithinDistance()
        {
            return _isWithinDistance;
        }

        /// <summary>
        ///     Set new target object at runtime.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            targetObject = newTarget;
        }

        /// <summary>
        ///     Set distance threshold at runtime.
        /// </summary>
        public void SetDistanceThreshold(float threshold)
        {
            distanceThreshold = Mathf.Max(0f, threshold);
            RecalculateThresholdSqr();
        }

        /// <summary>
        ///     Set current (source) object at runtime.
        /// </summary>
        public void SetCurrentObject(Transform newCurrent)
        {
            currentObject = newCurrent != null ? newCurrent : transform;
        }

        /// <summary>
        ///     Set update mode at runtime.
        /// </summary>
        public void SetUpdateMode(UpdateMode mode)
        {
            updateMode = mode;
        }

        /// <summary>
        ///     Set update interval for FixedInterval mode.
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.01f, interval);
        }

        /// <summary>
        ///     Enable or disable continuous tracking.
        /// </summary>
        public void SetContinuousTracking(bool enabled)
        {
            enableContinuousTracking = enabled;
        }

        /// <summary>
        ///     Force immediate distance check.
        /// </summary>
        public void ForceCheck()
        {
            if (currentObject != null && targetObject != null)
            {
                CheckDistance();
            }
        }

        #endregion
    }
}