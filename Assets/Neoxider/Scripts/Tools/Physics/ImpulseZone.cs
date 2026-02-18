using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Зона импульса, которая применяет импульс к объектам при входе в триггер.
    ///     Поддерживает различные направления импульса, фильтрацию и опциональное добавление физики.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + "Physics/" + nameof(ImpulseZone))]
    [RequireComponent(typeof(Collider))]
    public class ImpulseZone : MonoBehaviour
    {
        /// <summary>
        ///     Направление импульса.
        /// </summary>
        public enum ImpulseDirection
        {
            /// <summary>От центра зоны</summary>
            AwayFromCenter,

            /// <summary>К центру зоны</summary>
            TowardsCenter,

            /// <summary>По направлению Transform.forward</summary>
            TransformForward,

            /// <summary>Кастомное направление</summary>
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

        private readonly Dictionary<Collider, float> cooldownTimers = new();

        private readonly HashSet<Collider> processedColliders = new();

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
            if (cooldown > 0f && cooldownTimers.Count > 0)
            {
                List<Collider> toRemove = new();
                foreach (KeyValuePair<Collider, float> kvp in cooldownTimers)
                {
                    cooldownTimers[kvp.Key] = kvp.Value - Time.deltaTime;
                    if (cooldownTimers[kvp.Key] <= 0f)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (Collider col in toRemove)
                {
                    cooldownTimers.Remove(col);
                    if (oneTimeOnly)
                    {
                        processedColliders.Remove(col);
                    }
                }
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
            if (!PassesFilter(other.gameObject))
            {
                return;
            }

            if (oneTimeOnly && processedColliders.Contains(other))
            {
                return;
            }

            if (cooldown > 0f && cooldownTimers.ContainsKey(other))
            {
                return;
            }

            OnObjectEntered?.Invoke(other.gameObject);
            ApplyImpulse(other);
        }

        /// <summary>
        ///     Применить импульс к объекту вручную.
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
        ///     Установить силу импульса.
        /// </summary>
        public void SetImpulseForce(float newForce)
        {
            impulseForce = Mathf.Max(0f, newForce);
        }

        /// <summary>
        ///     Очистить список обработанных объектов (позволяет снова применять импульс).
        /// </summary>
        public void ClearProcessedObjects()
        {
            processedColliders.Clear();
            cooldownTimers.Clear();
        }

        private void ApplyImpulse(Collider col)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null)
            {
                if (addRigidbodyIfNeeded)
                {
                    rb = col.gameObject.AddComponent<Rigidbody>();
                }
                else
                {
                    return;
                }
            }

            Vector3 impulseDirection = CalculateDirection(col.transform.position);
            rb.AddForce(impulseDirection * impulseForce, ForceMode.Impulse);

            processedColliders.Add(col);
            if (cooldown > 0f)
            {
                cooldownTimers[col] = cooldown;
            }

            OnImpulseApplied?.Invoke(col.gameObject);
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