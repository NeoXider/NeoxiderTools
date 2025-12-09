using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Universal component for following a target with position and rotation.
    ///     Supports multiple smoothing modes, deadzone, distance controls, and position/rotation limits.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(Follow))]
    public class Follow : MonoBehaviour
    {
        public enum FollowMode
        {
            ThreeD,
            TwoD
        }

        public enum SmoothMode
        {
            None,
            MoveTowards,
            Lerp,
            SmoothDamp,
            Exponential
        }

        [Header("Target")] [SerializeField] private Transform target;

        [SerializeField] private FollowMode followMode = FollowMode.ThreeD;

        [Header("Position Settings")] [SerializeField]
        private bool followPosition = true;

        [SerializeField] private SmoothMode positionSmoothMode = SmoothMode.MoveTowards;

        [Tooltip("Speed for position smoothing. Meaning varies by smooth mode.")] [SerializeField]
        private float positionSpeed = 5f;

        [SerializeField] private Vector3 offset;

        [Header("Deadzone")] [SerializeField] private DeadzoneSettings deadzone = new();

        [Header("Distance Control")] [SerializeField]
        private DistanceSettings distanceControl = new();

        [Header("Position Limits")] [SerializeField]
        private AxisLimit limitX = new();

        [SerializeField] private AxisLimit limitY = new();

        [SerializeField] private AxisLimit limitZ = new();

        [Header("Rotation Settings")] [SerializeField]
        private bool followRotation = true;

        [SerializeField] private SmoothMode rotationSmoothMode = SmoothMode.Lerp;

        [Tooltip("Speed for rotation smoothing (degrees per second for MoveTowards).")] [SerializeField]
        private float rotationSpeed = 180f;

        [SerializeField] private Vector3 rotationOffset3D;

        [SerializeField] private float rotationOffset2D;

        [Header("Rotation Limits (3D)")] [SerializeField]
        private AxisLimit rotationLimitX = new();

        [SerializeField] private AxisLimit rotationLimitY = new();

        [Header("Rotation Limits (2D)")] [SerializeField]
        private AxisLimit rotationLimitZ = new();

        [Header("Events")] public UnityEvent onStartFollowing;

        public UnityEvent onStopFollowing;

        [Header("Debug")] [SerializeField] private bool showDebugGizmos = true;

        private bool _isFollowing;
        private Vector3 _lastTargetPosition;

        private Vector3 _positionVelocity;
        private Vector3 _rotationVelocity;

        private void Start()
        {
            if (target != null)
            {
                _lastTargetPosition = target.position;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (followPosition)
            {
                UpdatePosition();
            }

            if (followRotation)
            {
                UpdateRotation();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || target == null)
            {
                return;
            }

            if (deadzone.enabled && followPosition)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, deadzone.radius);
            }

            if (distanceControl.activationDistance > 0f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, distanceControl.activationDistance);
            }

            if (distanceControl.stoppingDistance > 0f)
            {
                Vector3 targetPos = target.position + offset;
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(targetPos, distanceControl.stoppingDistance);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position + offset);
        }

        private void UpdatePosition()
        {
            Vector3 targetPos = CalculateTargetPosition();
            targetPos = ApplyPositionLimits(targetPos);

            float distanceToTarget = Vector3.Distance(transform.position, targetPos);

            if (distanceControl.activationDistance > 0f && distanceToTarget < distanceControl.activationDistance)
            {
                UpdateFollowingState(false);
                return;
            }

            UpdateFollowingState(true);

            targetPos = ApplyDeadzone(targetPos);
            targetPos = ApplyStoppingDistance(targetPos);

            transform.position = SmoothPosition(transform.position, targetPos);
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 desiredPosition = target.position + offset;

            if (followMode == FollowMode.TwoD)
            {
                desiredPosition.z = offset.z;
            }

            return desiredPosition;
        }

        private Vector3 ApplyPositionLimits(Vector3 position)
        {
            if (limitX.enabled)
            {
                position.x = Mathf.Clamp(position.x, limitX.min, limitX.max);
            }

            if (limitY.enabled)
            {
                position.y = Mathf.Clamp(position.y, limitY.min, limitY.max);
            }

            if (limitZ.enabled)
            {
                position.z = Mathf.Clamp(position.z, limitZ.min, limitZ.max);
            }

            return position;
        }

        private Vector3 ApplyDeadzone(Vector3 targetPos)
        {
            if (!deadzone.enabled)
            {
                return targetPos;
            }

            Vector3 diff = targetPos - transform.position;
            float distance = diff.magnitude;

            if (distance <= deadzone.radius)
            {
                return transform.position;
            }

            return transform.position + diff.normalized * (distance - deadzone.radius);
        }

        private Vector3 ApplyStoppingDistance(Vector3 targetPos)
        {
            if (distanceControl.stoppingDistance <= 0f)
            {
                return targetPos;
            }

            Vector3 diff = targetPos - transform.position;
            float distance = diff.magnitude;

            if (distance <= distanceControl.stoppingDistance)
            {
                return transform.position;
            }

            return transform.position + diff.normalized * (distance - distanceControl.stoppingDistance);
        }

        private void UpdateFollowingState(bool shouldFollow)
        {
            if (shouldFollow && !_isFollowing)
            {
                _isFollowing = true;
                onStartFollowing?.Invoke();
            }
            else if (!shouldFollow && _isFollowing)
            {
                _isFollowing = false;
                onStopFollowing?.Invoke();
            }
        }

        private Vector3 SmoothPosition(Vector3 current, Vector3 target)
        {
            return positionSmoothMode switch
            {
                SmoothMode.None => target,
                SmoothMode.MoveTowards => Vector3.MoveTowards(current, target,
                    positionSpeed * Time.deltaTime),
                SmoothMode.Lerp => Vector3.Lerp(current, target,
                    Mathf.Clamp01(positionSpeed * Time.deltaTime)),
                SmoothMode.SmoothDamp => Vector3.SmoothDamp(current, target,
                    ref _positionVelocity, 1f / positionSpeed),
                SmoothMode.Exponential => Vector3.Lerp(current, target,
                    1f - Mathf.Exp(-positionSpeed * Time.deltaTime)),
                _ => target
            };
        }

        private void UpdateRotation()
        {
            if (followMode == FollowMode.ThreeD)
            {
                UpdateRotation3D();
            }
            else
            {
                UpdateRotation2D();
            }
        }

        private void UpdateRotation3D()
        {
            Vector3 direction = target.position - transform.position;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset3D);
            targetRotation = ApplyRotationLimits3D(targetRotation);
            transform.rotation = SmoothRotation(transform.rotation, targetRotation);
        }

        private void UpdateRotation2D()
        {
            Vector3 direction = target.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + rotationOffset2D;

            if (rotationLimitZ.enabled)
            {
                angle = Mathf.Clamp(angle, rotationLimitZ.min, rotationLimitZ.max);
            }

            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = SmoothRotation(transform.rotation, targetRotation);
        }

        private Quaternion ApplyRotationLimits3D(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;

            if (rotationLimitX.enabled)
            {
                float angleX = NormalizeAngle(euler.x);
                euler.x = Mathf.Clamp(angleX, rotationLimitX.min, rotationLimitX.max);
            }

            if (rotationLimitY.enabled)
            {
                float angleY = NormalizeAngle(euler.y);
                euler.y = Mathf.Clamp(angleY, rotationLimitY.min, rotationLimitY.max);
            }

            return Quaternion.Euler(euler);
        }

        private float NormalizeAngle(float angle)
        {
            angle = angle % 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private Quaternion SmoothRotation(Quaternion current, Quaternion target)
        {
            return rotationSmoothMode switch
            {
                SmoothMode.None => target,
                SmoothMode.MoveTowards => Quaternion.RotateTowards(current, target,
                    rotationSpeed * Time.deltaTime),
                SmoothMode.Lerp => Quaternion.Lerp(current, target,
                    Mathf.Clamp01(rotationSpeed * Time.deltaTime)),
                SmoothMode.SmoothDamp => QuaternionSmoothDamp(current, target, ref _rotationVelocity,
                    1f / rotationSpeed),
                SmoothMode.Exponential => Quaternion.Lerp(current, target,
                    1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)),
                _ => target
            };
        }

        private Quaternion QuaternionSmoothDamp(Quaternion current, Quaternion target, ref Vector3 velocity,
            float smoothTime)
        {
            Vector3 currentEuler = current.eulerAngles;
            Vector3 targetEuler = target.eulerAngles;

            float x = Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref velocity.x, smoothTime);
            float y = Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref velocity.y, smoothTime);
            float z = Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref velocity.z, smoothTime);

            return Quaternion.Euler(x, y, z);
        }

        [Serializable]
        public class AxisLimit
        {
            public bool enabled;
            public float min = -10f;
            public float max = 10f;
        }

        [Serializable]
        public class DeadzoneSettings
        {
            public bool enabled;

            [Tooltip("Camera doesn't move while target is within this radius.")]
            public float radius = 1f;
        }

        [Serializable]
        public class DistanceSettings
        {
            [Tooltip("Minimum distance to start following (0 = no limit).")]
            public float activationDistance;

            [Tooltip("Stop moving when this close to target (0 = move to target).")]
            public float stoppingDistance;
        }

        #region === Public API ===

        /// <summary>
        ///     Sets a new target to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _lastTargetPosition = target.position;
            }
        }

        /// <summary>
        ///     Gets the current target.
        /// </summary>
        public Transform GetTarget()
        {
            return target;
        }

        /// <summary>
        ///     Enable or disable position following.
        /// </summary>
        public void SetFollowPosition(bool enabled)
        {
            followPosition = enabled;
        }

        /// <summary>
        ///     Enable or disable rotation following.
        /// </summary>
        public void SetFollowRotation(bool enabled)
        {
            followRotation = enabled;
        }

        /// <summary>
        ///     Set position movement speed.
        /// </summary>
        public void SetPositionSpeed(float speed)
        {
            positionSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        ///     Set rotation speed.
        /// </summary>
        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        ///     Set position offset relative to target.
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        ///     Set activation distance (minimum distance to start following).
        /// </summary>
        public void SetActivationDistance(float distance)
        {
            distanceControl.activationDistance = Mathf.Max(0f, distance);
        }

        /// <summary>
        ///     Set stopping distance (distance from target where movement stops).
        /// </summary>
        public void SetStoppingDistance(float distance)
        {
            distanceControl.stoppingDistance = Mathf.Max(0f, distance);
        }

        /// <summary>
        ///     Enable or disable deadzone.
        /// </summary>
        public void SetDeadzoneEnabled(bool enabled)
        {
            deadzone.enabled = enabled;
        }

        /// <summary>
        ///     Set deadzone radius.
        /// </summary>
        public void SetDeadzoneRadius(float radius)
        {
            deadzone.radius = Mathf.Max(0f, radius);
        }

        /// <summary>
        ///     Returns current distance to target (ignoring offset).
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (target == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(transform.position, target.position);
        }

        /// <summary>
        ///     Returns true if currently following the target.
        /// </summary>
        public bool IsFollowing()
        {
            return _isFollowing;
        }

        /// <summary>
        ///     Set position smooth mode at runtime.
        /// </summary>
        public void SetPositionSmoothMode(SmoothMode mode)
        {
            positionSmoothMode = mode;
        }

        /// <summary>
        ///     Set rotation smooth mode at runtime.
        /// </summary>
        public void SetRotationSmoothMode(SmoothMode mode)
        {
            rotationSmoothMode = mode;
        }

        /// <summary>
        ///     Teleport to target position immediately (ignoring smooth).
        /// </summary>
        public void TeleportToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = CalculateTargetPosition();
        }

        #endregion
    }
}