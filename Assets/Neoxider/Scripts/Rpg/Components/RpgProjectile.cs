using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Lightweight RPG projectile that applies an attack definition on impact.
    /// </summary>
    [NeoDoc("Rpg/RpgProjectile.md")]
    [CreateFromMenu("Neoxider/RPG/RpgProjectile")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgProjectile))]
    public sealed class RpgProjectile : MonoBehaviour
    {
        [Header("Events")] [SerializeField] private UnityEvent _onInitialized = new();

        [SerializeField] private RpgGameObjectEvent _onHit = new();
        [SerializeField] private UnityEvent _onExpired = new();

        [Header("Performance")] [Tooltip("Initial raw hit buffer capacity. Saturated queries grow; ProjectileMaxHits limits delivered hits.")] [Min(1)]
        [SerializeField] private int _maxHitsPerCast = 16;

        private readonly HashSet<GameObject> _hitTargets = new();
        private readonly HashSet<IRpgCombatReceiver> _hitReceivers = new();
        private RpgPhysicsQueryBuffers _queries;
        private RpgAttackDefinition _definition;
        private Vector3 _direction = Vector3.forward;
        private float _elapsed;
        private Vector3 _lastPosition;
        private float _lifetime;
        private RpgAttackController _owner;
        private int _remainingHits;
        private IRpgCombatReceiver _sourceReceiver;
        private float _speed;
        private int _generation;
        private bool _expired;

        private void Update()
        {
            if (_expired)
            {
                return;
            }
            if (_definition == null || _owner == null)
            {
                Expire();
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Expire();
                return;
            }

            Vector3 nextPosition = transform.position + _direction * (_speed * Time.deltaTime);
            int generation = _generation;
            HandleHitsBetween(_lastPosition, nextPosition);
            if (generation != _generation || _expired || !isActiveAndEnabled)
            {
                return;
            }
            transform.position = nextPosition;
            _lastPosition = nextPosition;
        }

        /// <summary>
        ///     Initializes the projectile.
        /// </summary>
        public void Initialize(RpgAttackController owner, RpgAttackDefinition definition,
            IRpgCombatReceiver sourceReceiver, Vector3 direction)
        {
            _generation++;
            _expired = false;
            _owner = owner;
            _definition = definition;
            _sourceReceiver = sourceReceiver;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            _elapsed = 0f;
            _hitTargets.Clear();
            _hitReceivers.Clear();
            _speed = definition.ProjectileSpeed;
            _lifetime = definition.ProjectileLifetime;
            _remainingHits = definition.ProjectileMaxHits;
            _lastPosition = transform.position;
            _onInitialized?.Invoke();
        }

        private void OnDisable()
        {
            _generation++;
            // WHY: a pooled projectile must not keep the colliders of its last cast alive.
            _queries?.Release();
        }

        private void Expire()
        {
            int generation = _generation;
            _expired = true;
            _onExpired?.Invoke();
            if (generation == _generation)
            {
                Destroy(gameObject);
            }
        }

        private void HandleHitsBetween(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            Vector3 direction = delta / distance;
            float radius = Mathf.Max(0.01f, _definition.Radius);
            // WHY: one buffer set per projectile; a hit callback cannot start another query on this projectile.
            _queries ??= new RpgPhysicsQueryBuffers(_maxHitsPerCast);
            int generation = _generation;
            if (_definition.Use3D)
            {
                // WHY: complete and nearest-first; a saturated buffer grows instead of hiding farther receivers.
                int hitCount = _queries.SphereCast(from, radius, direction, distance, _definition.TargetLayers);
                RaycastHit[] hits = _queries.Hits3D;
                for (int i = 0; i < hitCount; i++)
                {
                    TryHitTarget(hits[i].collider != null ? hits[i].collider.gameObject : null);
                    if (generation != _generation || _expired || !isActiveAndEnabled || _remainingHits <= 0)
                    {
                        return;
                    }
                }
            }

            if (_definition.Use2D)
            {
                // WHY: the list overload grows on its own and replaces the deprecated CircleCastNonAlloc.
                int hit2DCount = _queries.CircleCast2D(from, radius, direction, distance, _definition.TargetLayers);
                List<RaycastHit2D> hits2D = _queries.Hits2D;
                RpgHitSort.Sort(hits2D, hits2D.Count, RpgHit2DDistanceComparer.Instance);
                for (int i = 0; i < hit2DCount && i < hits2D.Count; i++)
                {
                    TryHitTarget(hits2D[i].collider != null ? hits2D[i].collider.gameObject : null);
                    if (generation != _generation || _expired || !isActiveAndEnabled || _remainingHits <= 0)
                    {
                        return;
                    }
                }
            }
        }

        private void TryHitTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (RpgAttackController.TryResolveReceiver(target, out IRpgCombatReceiver receiver))
            {
                if (!_hitReceivers.Add(receiver))
                {
                    return;
                }
            }
            else if (!_hitTargets.Add(target))
            {
                return;
            }

            int generation = _generation;
            bool applied = _owner.ApplyHitToGameObject(target, _definition);
            if (generation != _generation || _expired || !isActiveAndEnabled)
            {
                return;
            }
            if (applied)
            {
                _remainingHits--;
                _onHit?.Invoke(target);
                if (generation == _generation && !_expired && isActiveAndEnabled && _remainingHits <= 0)
                {
                    Expire();
                }
            }
        }
    }
}
