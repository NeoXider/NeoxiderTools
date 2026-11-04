using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Universal component for following a target with position and rotation.
    ///     Supports multiple smoothing modes, deadzone, distance controls, and position/rotation limits.
    /// </summary>
    [NeoDoc("Tools/Move/Follow.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/Follow")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Follow))]
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

        [Tooltip("If target is null at Start, find by tag.")] [SerializeField]
        private bool findTargetByTag;

        [Tooltip("Tag to search when Find Target By Tag is enabled (e.g. Player).")] [SerializeField]
        private string targetTag = "Player";

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

        public UnityEvent onStartFollowing;

        public UnityEvent onStopFollowing;

        [Tooltip("Invoked once when target becomes null (e.g. destroyed).")]
        public UnityEvent onTargetLost;

        [Header("Debug")] [SerializeField] private bool showDebugGizmos = true;

        private const float MinAimDistanceSqr = 0.000001f;

        private bool _hadTargetLastFrame;

        private bool _isFollowing;

        private Vector3 _positionVelocity;
        private Vector3 _rotationVelocity;

        private bool _hasRotationWrite;
        private Quaternion _lastRotationWritten;
        private Quaternion _lastRotationStored;

        private void Start()
        {
            if (target == null && findTargetByTag && !string.IsNullOrEmpty(targetTag))
            {
                GameObject found = GameObject.FindWithTag(targetTag);
                if (found != null)
                {
                    target = found.transform;
                }
            }

            _hadTargetLastFrame = target != null;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                if (_hadTargetLastFrame)
                {
                    _hadTargetLastFrame = false;
                    _isFollowing = false;
                    onTargetLost?.Invoke();
                }

                return;
            }

            _hadTargetLastFrame = true;

            if (followPosition)
            {
                UpdatePosition();
            }

            // WHY: a follow-state listener may have cleared the target or destroyed this follower.
            if (followRotation && this != null && target != null)
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

            Vector3 current = transform.position;
            float distanceToTarget = Vector3.Distance(current, targetPos);

            if (distanceControl.activationDistance > 0f && distanceToTarget < distanceControl.activationDistance)
            {
                UpdateFollowingState(false);
                return;
            }

            if (UpdateFollowingState(true))
            {
                // WHY: an onStartFollowing listener may clear or replace the target, move the follower (for
                // example TeleportToTarget) or destroy it; continue from the state it left, never a stale one.
                if (this == null || target == null)
                {
                    return;
                }

                current = transform.position;
                targetPos = ApplyPositionLimits(CalculateTargetPosition());
            }

            targetPos = ApplyDeadzone(current, targetPos);
            targetPos = ApplyStoppingDistance(current, targetPos);

            Vector3 next = SmoothPosition(current, targetPos);
            // WHY: exact comparison; skipping the write keeps idle followers from dirtying their transform
            // hierarchy every frame without ever leaving a sub-epsilon step unapplied.
            if (next.x != current.x || next.y != current.y || next.z != current.z)
            {
                transform.position = next;
            }
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

        private Vector3 ApplyDeadzone(Vector3 current, Vector3 targetPos)
        {
            if (!deadzone.enabled)
            {
                return targetPos;
            }

            Vector3 diff = targetPos - current;
            float distance = diff.magnitude;

            if (distance <= deadzone.radius)
            {
                return current;
            }

            return current + diff.normalized * (distance - deadzone.radius);
        }

        private Vector3 ApplyStoppingDistance(Vector3 current, Vector3 targetPos)
        {
            if (distanceControl.stoppingDistance <= 0f)
            {
                return targetPos;
            }

            Vector3 diff = targetPos - current;
            float distance = diff.magnitude;

            if (distance <= distanceControl.stoppingDistance)
            {
                return current;
            }

            return current + diff.normalized * (distance - distanceControl.stoppingDistance);
        }

        /// <summary>Updates the follow state; returns true when a state event was invoked.</summary>
        private bool UpdateFollowingState(bool shouldFollow)
        {
            if (shouldFollow && !_isFollowing)
            {
                _isFollowing = true;
                onStartFollowing?.Invoke();
                return true;
            }

            if (!shouldFollow && _isFollowing)
            {
                _isFollowing = false;
                onStopFollowing?.Invoke();
                return true;
            }

            return false;
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
            if (!TryGetAimRotation(out Quaternion targetRotation))
            {
                return;
            }

            Quaternion current = transform.rotation;
            WriteRotation(current, SmoothRotation(current, targetRotation));
        }

        /// <summary>
        ///     Rotation that aims at the target from the current position, with the offsets and rotation limits of
        ///     the follow mode applied. False when the follower sits on the target and the aim is undefined.
        /// </summary>
        private bool TryGetAimRotation(out Quaternion rotation)
        {
            Vector3 direction = target.position - transform.position;
            if (followMode == FollowMode.ThreeD)
            {
                if (direction.sqrMagnitude < 0.001f)
                {
                    rotation = Quaternion.identity;
                    return false;
                }

                rotation = ApplyRotationLimits3D(Quaternion.LookRotation(direction) *
                                                 Quaternion.Euler(rotationOffset3D));
                return true;
            }

            // WHY: on top of the target the aim is undefined; Atan2(0, 0) would snap the follower to -90 degrees.
            if (direction.x * direction.x + direction.y * direction.y < MinAimDistanceSqr)
            {
                rotation = Quaternion.identity;
                return false;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + rotationOffset2D;

            if (rotationLimitZ.enabled)
            {
                angle = ClampAngleToSector(angle, rotationLimitZ.min, rotationLimitZ.max);
            }

            rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            return true;
        }

        /// <summary>
        ///     Assigns <paramref name="next" /> unless that would repeat what the transform already holds, so an
        ///     aligned follower stops dirtying its transform hierarchy every frame.
        /// </summary>
        private void WriteRotation(Quaternion current, Quaternion next)
        {
            // WHY: exact comparisons only. Unity may store a renormalized copy of the assigned value, so an aligned
            // follower is recognised by computing the same rotation as its last write while the transform still holds
            // what that write stored; a new aim, smoothing progress or an external rotation is always written.
            if (ExactlyEqual(next, current) || (_hasRotationWrite && ExactlyEqual(next, _lastRotationWritten) &&
                                                ExactlyEqual(current, _lastRotationStored)))
            {
                return;
            }

            transform.rotation = next;
            _lastRotationWritten = next;
            _lastRotationStored = transform.rotation;
            _hasRotationWrite = true;
        }

        private static bool ExactlyEqual(Quaternion a, Quaternion b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        private Quaternion ApplyRotationLimits3D(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;

            // WHY: the same sector clamp as 2D, so a yaw sector such as [135, 225] stays reachable and a target
            // crossing behind an asymmetric sector snaps to the nearer edge instead of flipping to the min edge.
            if (rotationLimitX.enabled)
            {
                euler.x = ClampAngleToSector(NormalizeAngle(euler.x), rotationLimitX.min, rotationLimitX.max);
            }

            if (rotationLimitY.enabled)
            {
                euler.y = ClampAngleToSector(NormalizeAngle(euler.y), rotationLimitY.min, rotationLimitY.max);
            }

            return Quaternion.Euler(euler);
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.DeltaAngle(0f, angle);
        }

        /// <summary>
        ///     Clamps <paramref name="angle" /> into the sector from <paramref name="min" /> to <paramref name="max" />
        ///     and returns the nearer edge for an angle outside it, with the sector rules of <see cref="AngleSector" />
        ///     (shared with <see cref="UniversalRotator" />): sectors beyond [-180, 180] such as [-225, -135] stay
        ///     reachable, a range whose max is below its min wraps through 180, and a range of 360 degrees or more leaves
        ///     the angle unchanged.
        /// </summary>
        internal static float ClampAngleToSector(float angle, float min, float max)
        {
            return AngleSector.Clamp(angle, min, max);
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
            [Tooltip("Do not follow when target is closer than this distance (0 = always follow).")]
            public float activationDistance;

            [Tooltip("Stop moving when this close to target (0 = move to target).")]
            public float stoppingDistance;
        }

        /// <summary>
        ///     Sets a new target to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
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
        ///     Returns whether position following is enabled.
        /// </summary>
        public bool GetFollowPosition()
        {
            return followPosition;
        }

        /// <summary>
        ///     Enable or disable rotation following.
        /// </summary>
        public void SetFollowRotation(bool enabled)
        {
            followRotation = enabled;
        }

        /// <summary>
        ///     Returns whether rotation following is enabled.
        /// </summary>
        public bool GetFollowRotation()
        {
            return followRotation;
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
        ///     Snaps whatever this follower follows onto the target immediately (ignoring smoothing, deadzone and
        ///     stopping distance). With position following on, it moves to the target position: position limits still
        ///     apply, and the SmoothDamp velocity is reset so the next frame does not overshoot. With rotation following
        ///     on, it then turns straight to the aim (offsets and rotation limits apply) and resets the rotation
        ///     velocity. A rotation-only follower, such as a turret, therefore keeps its place and only turns.
        /// </summary>
        public void TeleportToTarget()
        {
            if (target == null)
            {
                return;
            }

            if (followPosition)
            {
                transform.position = ApplyPositionLimits(CalculateTargetPosition());
                _positionVelocity = Vector3.zero;
            }

            if (followRotation && TryGetAimRotation(out Quaternion aim))
            {
                WriteRotation(transform.rotation, aim);
                _rotationVelocity = Vector3.zero;
            }
        }
    }
}
