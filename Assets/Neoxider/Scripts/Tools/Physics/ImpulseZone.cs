using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Impulse zone that applies impulse to objects on trigger enter.
    ///     Supports multiple impulse directions, filtering, and optional physics setup.
    /// </summary>
    [NeoDoc("Tools/Physics/ImpulseZone.md")]
    [CreateFromMenu("Neoxider/Tools/Physics/ImpulseZone")]
    [AddComponentMenu("Neoxider/" + "Tools/" + "Physics/" + nameof(ImpulseZone))]
    [RequireComponent(typeof(Collider))]
    public class ImpulseZone : MonoBehaviour
    {
        /// <summary>
        ///     Impulse direction.
        /// </summary>
        public enum ImpulseDirection
        {
            /// <summary>Away from zone center</summary>
            AwayFromCenter,

            /// <summary>Toward zone center</summary>
            TowardsCenter,

            /// <summary>Along Transform.forward</summary>
            TransformForward,

            /// <summary>Custom direction</summary>
            Custom
        }

        [Header("Settings")] [Tooltip("Impulse force")] [Min(0f)] [SerializeField]
        private float impulseForce = 50f;

        [Tooltip("Impulse direction")] [SerializeField]
        private ImpulseDirection direction = ImpulseDirection.AwayFromCenter;

        [Tooltip("Custom direction (used in Custom mode)")] [SerializeField]
        private Vector3 customDirection = Vector3.up;

        [Header("Filtering")] [Tooltip("Layers of objects affected by impulse")] [SerializeField]
        private LayerMask affectedLayers = -1;

        [Tooltip("Object tag (empty = ignore tag filter)")] [SerializeField]
        private string requiredTag = "";

        [Header("Options")] [Tooltip("Automatically add Rigidbody to objects without physics")] [SerializeField]
        private bool addRigidbodyIfNeeded;

        [Tooltip("One-shot (each object can receive impulse only once)")] [SerializeField]
        private bool oneTimeOnly;

        [Tooltip("Cooldown between triggers per object")] [Min(0f)] [SerializeField]
        private float cooldown;

        [Tooltip("Invoked when object enters zone")]
        public UnityEvent<GameObject> OnObjectEntered = new();

        [Tooltip("Invoked when impulse is applied")]
        public UnityEvent<GameObject> OnImpulseApplied = new();

        private const float ProcessedPurgeInterval = 1f;

        private static readonly System.Predicate<Rigidbody> IsDestroyedBody = body => body == null;

        private readonly Dictionary<Rigidbody, float> cooldownTimers = new();

        private readonly HashSet<Rigidbody> processedRigidbodies = new();
        private readonly HashSet<Rigidbody> applyingImpulse = new();
        private readonly List<Rigidbody> expiredCooldowns = new();

        private Collider zoneCollider;
        private float _nextCooldownExpiry;
        private float _nextProcessedPurgeTime;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            // WHY: this is bookkeeping only (cooldowns are checked against their deadline on use), so the scans
            // run when something can actually expire instead of on every frame of every zone.
            float now = Time.time;
            if (processedRigidbodies.Count > 0 && now >= _nextProcessedPurgeTime)
            {
                _nextProcessedPurgeTime = now + ProcessedPurgeInterval;
                processedRigidbodies.RemoveWhere(IsDestroyedBody);
            }

            if (cooldownTimers.Count == 0 || now < _nextCooldownExpiry)
            {
                return;
            }

            expiredCooldowns.Clear();
            float nextExpiry = float.PositiveInfinity;
            foreach (KeyValuePair<Rigidbody, float> entry in cooldownTimers)
            {
                if (entry.Key == null || now >= entry.Value)
                {
                    expiredCooldowns.Add(entry.Key);
                }
                else if (entry.Value < nextExpiry)
                {
                    nextExpiry = entry.Value;
                }
            }

            for (int i = 0; i < expiredCooldowns.Count; i++)
            {
                cooldownTimers.Remove(expiredCooldowns[i]);
            }

            expiredCooldowns.Clear();
            _nextCooldownExpiry = nextExpiry;
        }

        private void OnDrawGizmosSelected()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<Collider>();
            }

            if (zoneCollider == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (zoneCollider is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (zoneCollider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
            else if (zoneCollider is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(capsule.center + Vector3.up * (capsule.height * 0.5f - capsule.radius),
                    capsule.radius);
                Gizmos.DrawWireSphere(capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius),
                    capsule.radius);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ApplyImpulse(other, true);
        }

        /// <summary>
        ///     Applies impulse manually, respecting filters, one-shot history, and cooldown per Rigidbody.
        /// </summary>
        public void ApplyImpulseToObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Collider col = target.GetComponent<Collider>();
            if (col == null)
            {
                return;
            }

            if (!PassesFilter(target))
            {
                return;
            }

            ApplyImpulse(col);
        }

        /// <summary>
        ///     Sets impulse force.
        /// </summary>
        public void SetImpulseForce(float newForce)
        {
            impulseForce = Mathf.Max(0f, newForce);
        }

        /// <summary>Current impulse direction mode.</summary>
        public ImpulseDirection CurrentDirection => direction;

        /// <summary>
        ///     Selects how the impulse direction is computed.
        /// </summary>
        public void SetDirection(ImpulseDirection newDirection)
        {
            direction = newDirection;
        }

        /// <summary>
        ///     Sets the direction by its <see cref="ImpulseDirection" /> index, for UnityEvents, which cannot pass an
        ///     enum. An undefined index is ignored.
        /// </summary>
        public void SetDirectionIndex(int directionIndex)
        {
            if (System.Enum.IsDefined(typeof(ImpulseDirection), directionIndex))
            {
                SetDirection((ImpulseDirection)directionIndex);
            }
        }

        /// <summary>
        ///     Sets a world-space impulse direction and selects <see cref="ImpulseDirection.Custom" />.
        /// </summary>
        public void SetCustomDirection(Vector3 newDirection)
        {
            customDirection = newDirection;
            direction = ImpulseDirection.Custom;
        }

        /// <summary>
        ///     Sets the per-Rigidbody cooldown in seconds. 0 disables the cooldown check, including running deadlines;
        ///     a positive value is checked against the deadlines recorded when each impulse was applied.
        /// </summary>
        public void SetCooldown(float seconds)
        {
            cooldown = Mathf.Max(0f, seconds);
        }

        /// <summary>
        ///     Current impulse force.
        /// </summary>
        public float ImpulseForce => impulseForce;

        /// <summary>
        ///     Layers that can receive the impulse. Changes apply to the next contact or manual call.
        /// </summary>
        public LayerMask AffectedLayers
        {
            get => affectedLayers;
            set => affectedLayers = value;
        }

        /// <summary>
        ///     Clears processed objects (allows impulse to apply again).
        /// </summary>
        public void ClearProcessedObjects()
        {
            processedRigidbodies.Clear();
            cooldownTimers.Clear();
        }

        private void ApplyImpulse(Collider col, bool notifyEntered = false)
        {
            if (col == null || !PassesFilter(col.gameObject))
            {
                return;
            }

            Rigidbody rb = col.attachedRigidbody;
            if (rb == null)
            {
                if (addRigidbodyIfNeeded)
                {
                    rb = col.gameObject.AddComponent<Rigidbody>();
                }
                else
                {
                    if (notifyEntered)
                    {
                        OnObjectEntered?.Invoke(col.gameObject);
                    }

                    return;
                }
            }

            if ((oneTimeOnly && processedRigidbodies.Contains(rb)) ||
                (cooldown > 0f && cooldownTimers.TryGetValue(rb, out float deadline) && Time.time < deadline) ||
                !applyingImpulse.Add(rb))
            {
                return;
            }

            try
            {
                if (notifyEntered)
                {
                    OnObjectEntered?.Invoke(col.gameObject);
                }

                if (this == null || col == null || rb == null)
                {
                    return;
                }

                Vector3 impulseDirection = CalculateDirection(rb.worldCenterOfMass);
                rb.AddForce(impulseDirection * impulseForce, ForceMode.Impulse);

                if (oneTimeOnly)
                {
                    processedRigidbodies.Add(rb);
                }

                if (cooldown > 0f)
                {
                    float cooldownDeadline = Time.time + cooldown;
                    cooldownTimers[rb] = cooldownDeadline;
                    _nextCooldownExpiry = Mathf.Min(_nextCooldownExpiry, cooldownDeadline);
                }

                OnImpulseApplied?.Invoke(col.gameObject);
            }
            finally
            {
                applyingImpulse.Remove(rb);
            }
        }

        private Vector3 CalculateDirection(Vector3 targetPosition)
        {
            Vector3 center = transform.position;
            Vector3 result = Vector3.zero;

            switch (direction)
            {
                case ImpulseDirection.AwayFromCenter:
                    result = (targetPosition - center).normalized;
                    if (result.sqrMagnitude < 0.01f)
                    {
                        result = Random.onUnitSphere;
                    }

                    break;

                case ImpulseDirection.TowardsCenter:
                    result = (center - targetPosition).normalized;
                    if (result.sqrMagnitude < 0.01f)
                    {
                        result = -Random.onUnitSphere;
                    }

                    break;

                case ImpulseDirection.TransformForward:
                    result = transform.forward;
                    break;

                case ImpulseDirection.Custom:
                    result = customDirection.normalized;
                    break;
            }

            return result;
        }

        private bool PassesFilter(GameObject obj)
        {
            if (((1 << obj.layer) & affectedLayers) == 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !obj.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }

        [Button("Clear Processed Objects")]
        private void ClearProcessedObjectsButton()
        {
            ClearProcessedObjects();
        }
    }
}
