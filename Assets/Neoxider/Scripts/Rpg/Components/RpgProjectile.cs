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

        [Header("Performance")] [Tooltip("Maximum hits resolved per cast (reusable buffer size).")] [Min(1)]
        [SerializeField] private int _maxHitsPerCast = 16;

        private readonly HashSet<GameObject> _hitTargets = new();
        private readonly HashSet<IRpgCombatReceiver> _hitReceivers = new();
        private RaycastHit[] _hitBuffer;
        private RaycastHit2D[] _hit2DBuffer;
        private RpgAttackDefinition _definition;
        private Vector3 _direction = Vector3.forward;
        private float _elapsed;
        private Vector3 _lastPosition;
        private float _lifetime;
        private RpgAttackController _owner;
        private int _remainingHits;
        private IRpgCombatReceiver _sourceReceiver;
        private float _speed;

        private void Update()
        {
            if (_definition == null || _owner == null)
            {
                _onExpired?.Invoke();
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                _onExpired?.Invoke();
                Destroy(gameObject);
                return;
            }

            Vector3 nextPosition = transform.position + _direction * (_speed * Time.deltaTime);
            HandleHitsBetween(_lastPosition, nextPosition);
            transform.position = nextPosition;
            _lastPosition = nextPosition;
        }

        /// <summary>
        ///     Initializes the projectile.
        /// </summary>
        public void Initialize(RpgAttackController owner, RpgAttackDefinition definition,
            IRpgCombatReceiver sourceReceiver, Vector3 direction)
        {
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

        private void HandleHitsBetween(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            Vector3 direction = delta / distance;
            if (_definition.Use3D)
            {
                if (_hitBuffer == null || _hitBuffer.Length != _maxHitsPerCast)
                {
                    _hitBuffer = new RaycastHit[Mathf.Max(1, _maxHitsPerCast)];
                }

                int hitCount = Physics.SphereCastNonAlloc(from, Mathf.Max(0.01f, _definition.Radius), direction,
                    _hitBuffer, distance, _definition.TargetLayers);
                for (int i = 0; i < hitCount; i++)
                {
                    TryHitTarget(_hitBuffer[i].collider != null ? _hitBuffer[i].collider.gameObject : null);
                    if (_remainingHits <= 0)
                    {
                        return;
                    }
                }
            }

            if (_definition.Use2D)
            {
                if (_hit2DBuffer == null || _hit2DBuffer.Length != _maxHitsPerCast)
                {
                    _hit2DBuffer = new RaycastHit2D[Mathf.Max(1, _maxHitsPerCast)];
                }

                int hit2DCount = Physics2D.CircleCastNonAlloc(from, Mathf.Max(0.01f, _definition.Radius), direction,
                    _hit2DBuffer, distance, _definition.TargetLayers);
                for (int i = 0; i < hit2DCount; i++)
                {
                    TryHitTarget(_hit2DBuffer[i].collider != null ? _hit2DBuffer[i].collider.gameObject : null);
                    if (_remainingHits <= 0)
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

            if (_owner.ApplyHitToGameObject(target, _definition))
            {
                _onHit?.Invoke(target);
                _remainingHits--;
                if (_remainingHits <= 0)
                {
                    _onExpired?.Invoke();
                    Destroy(gameObject);
                }
            }
        }
    }
}
