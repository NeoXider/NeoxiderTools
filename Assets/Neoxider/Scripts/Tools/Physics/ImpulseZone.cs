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

        private readonly Dictionary<Rigidbody, float> cooldownTimers = new();

        private readonly HashSet<Rigidbody> processedRigidbodies = new();
        private readonly HashSet<Rigidbody> applyingImpulse = new();
        private readonly List<Rigidbody> expiredCooldowns = new();

        private Collider zoneCollider;

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
            processedRigidbodies.RemoveWhere(body => body == null);
            expiredCooldowns.Clear();
            foreach (KeyValuePair<Rigidbody, float> entry in cooldownTimers)
            {
                if (entry.Key == null || Time.time >= entry.Value)
                {
                    expiredCooldowns.Add(entry.Key);
                }
            }

            foreach (Rigidbody body in expiredCooldowns)
            {
                cooldownTimers.Remove(body);
            }
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
                    cooldownTimers[rb] = Time.time + cooldown;
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
