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

        private readonly HashSet<GameObject> _hitTargets = new();
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
                RaycastHit[] hits = Physics.SphereCastAll(from, Mathf.Max(0.01f, _definition.Radius), direction,
                    distance, _definition.TargetLayers);
                for (int i = 0; i < hits.Length; i++)
                {
                    TryHitTarget(hits[i].collider != null ? hits[i].collider.gameObject : null);
                    if (_remainingHits <= 0)
                    {
                        return;
                    }
                }
            }

            if (_definition.Use2D)
            {
                RaycastHit2D[] hits2D = Physics2D.CircleCastAll(from, Mathf.Max(0.01f, _definition.Radius), direction,
                    distance, _definition.TargetLayers);
                for (int i = 0; i < hits2D.Length; i++)
                {
                    TryHitTarget(hits2D[i].collider != null ? hits2D[i].collider.gameObject : null);
                    if (_remainingHits <= 0)
                    {
                        return;
                    }
                }
            }
        }

        private void TryHitTarget(GameObject target)
        {
            if (target == null || !_hitTargets.Add(target))
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
