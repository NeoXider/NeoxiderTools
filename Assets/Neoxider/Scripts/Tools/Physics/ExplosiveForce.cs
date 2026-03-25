using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Applies explosion force to objects within a radius.
    ///     Supports layer filtering, optional Rigidbody setup, and multiple activation modes.
    /// </summary>
    [NeoDoc("Tools/Physics/ExplosiveForce.md")]
    [CreateFromMenu("Neoxider/Tools/Physics/ExplosiveForce")]
    [AddComponentMenu("Neoxider/" + "Tools/" + "Physics/" + nameof(ExplosiveForce))]
    public class ExplosiveForce : MonoBehaviour
    {
        /// <summary>
        ///     When the explosion runs.
        /// </summary>
        public enum ActivationMode
        {
            /// <summary>Run on Start</summary>
            OnStart,

            /// <summary>Run on Awake</summary>
            OnAwake,

            /// <summary>Run after delay</summary>
            Delayed,

            /// <summary>Manual call only</summary>
            Manual
        }

        /// <summary>
        ///     Force falloff over distance.
        /// </summary>
        public enum FalloffType
        {
            /// <summary>Linear falloff</summary>
            Linear,

            /// <summary>Quadratic falloff</summary>
            Quadratic
        }

        /// <summary>
        ///     How force is applied.
        /// </summary>
        public enum ForceMode
        {
            /// <summary>AddForce — directional impulse</summary>
            AddForce,

            /// <summary>AddExplosionForce — radial with falloff</summary>
            AddExplosionForce
        }

        [Header("Activation")] [Tooltip("Explosion activation mode")] [SerializeField]
        private ActivationMode activationMode = ActivationMode.OnStart;

        [Tooltip("Delay before explosion (used in Delayed mode)")] [SerializeField]
        private float delay;

        [Header("Explosion Force")] [Tooltip("Base explosion force")] [Min(0f)] [SerializeField]
        private float force = 100f;

        [Tooltip("Random force variation (added to base force)")] [SerializeField]
        private float forceRandomness;

        [Tooltip("Force application mode")] [SerializeField]
        private ForceMode forceMode = ForceMode.AddExplosionForce;

        [Tooltip("Force falloff type over distance")] [SerializeField]
        private FalloffType falloffType = FalloffType.Quadratic;

        [Header("Radius & Filtering")] [Tooltip("Explosion radius")] [Min(0f)] [SerializeField]
        private float radius = 5f;

        [Tooltip("Layers of objects affected by explosion")] [SerializeField]
        private LayerMask affectedLayers = -1;

        [Header("Options")] [Tooltip("Automatically add Rigidbody to objects without physics")] [SerializeField]
        private bool addRigidbodyIfNeeded;

        [Tooltip("Destroy this object after explosion")] [SerializeField]
        private bool destroyAfterExplosion;

        [Tooltip("Delay before destroy (if destroyAfterExplosion = true)")] [SerializeField]
        private float destroyDelay;

        [Tooltip("Invoked on explosion")] public UnityEvent OnExplode = new();

        [Tooltip("Invoked for each affected object")]
        public UnityEvent<GameObject> OnObjectAffected = new();

        /// <summary>
        ///     Current explosion force.
        /// </summary>
        public float CurrentForce => force;

        /// <summary>
        ///     Current explosion radius.
        /// </summary>
        public float CurrentRadius => radius;

        /// <summary>
        ///     Whether the explosion has already fired (non-manual modes).
        /// </summary>
        public bool HasExploded { get; private set; }

        private void Awake()
        {
            if (activationMode == ActivationMode.OnAwake)
            {
                if (delay > 0f)
                {
                    StartCoroutine(ExplodeDelayed(delay));
                }
                else
                {
                    Explode();
                }
            }
        }

        private void Start()
        {
            if (activationMode == ActivationMode.OnStart)
            {
                if (delay > 0f)
                {
                    StartCoroutine(ExplodeDelayed(delay));
                }
                else
                {
                    Explode();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, radius);
        }

        /// <summary>
        ///     Triggers explosion using the component's base force.
        /// </summary>
        public void Explode()
        {
            Explode(0f);
        }

        /// <summary>
        ///     Triggers explosion with a custom force.
        /// </summary>
        /// <param name="customForce">Override force; if 0, uses base force.</param>
        public void Explode(float customForce)
        {
            if (HasExploded && activationMode != ActivationMode.Manual)
            {
                return;
            }

            HasExploded = true;

            float finalForce = customForce > 0f ? customForce : force;
            if (forceRandomness > 0f)
            {
                finalForce += Random.Range(-forceRandomness, forceRandomness);
            }

            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, affectedLayers);

            foreach (Collider col in colliders)
            {
                if (col == null || col.gameObject == gameObject)
                {
                    continue;
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
                        continue;
                    }
                }

                Vector3 direction = col.transform.position - transform.position;
                float distance = direction.magnitude;

                if (distance < 0.01f)
                {
                    direction = Random.onUnitSphere;
                    distance = 0.01f;
                }

                direction.Normalize();

                float forceAtDistance = CalculateForceAtDistance(distance, finalForce);

                if (forceMode == ForceMode.AddExplosionForce)
                {
                    rb.AddExplosionForce(forceAtDistance, transform.position, radius, 0f,
                        UnityEngine.ForceMode.Impulse);
                }
                else
                {
                    rb.AddForce(direction * forceAtDistance, UnityEngine.ForceMode.Impulse);
                }

                OnObjectAffected?.Invoke(col.gameObject);
            }

            OnExplode?.Invoke();

            if (destroyAfterExplosion)
            {
                if (destroyDelay > 0f)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        ///     Sets explosion force.
        /// </summary>
        public void SetForce(float newForce)
        {
            force = Mathf.Max(0f, newForce);
        }

        /// <summary>
        ///     Sets explosion radius.
        /// </summary>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0f, newRadius);
        }

        /// <summary>
        ///     Clears HasExploded so the explosion can run again.
        ///     Useful after OnStart, OnAwake, or Delayed auto explosions.
        ///     Manual mode can explode repeatedly without this.
        /// </summary>
        public void ResetExplosion()
        {
            HasExploded = false;
        }

        private float CalculateForceAtDistance(float distance, float baseForce)
        {
            if (distance >= radius)
            {
                return 0f;
            }

            float normalizedDistance = distance / radius;
            float falloff = falloffType == FalloffType.Linear
                ? 1f - normalizedDistance
                : 1f - normalizedDistance * normalizedDistance;

            return baseForce * falloff;
        }

        private IEnumerator ExplodeDelayed(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            Explode();
        }

        [Button("Explode Now")]
        private void ExplodeButton()
        {
            Explode();
        }

        [Button("Reset Explosion")]
        private void ResetExplosionButton()
        {
            ResetExplosion();
        }
    }
}
