using System;
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
        private int _generation;
        private bool _expired;
        private const int MaxReusableHits = 4096;

        private sealed class HitComparer : IComparer<RaycastHit>
        {
            public static readonly HitComparer Instance = new();
            public int Compare(RaycastHit left, RaycastHit right)
            {
                int distance = left.distance.CompareTo(right.distance);
                return distance != 0 ? distance : string.CompareOrdinal(
                    NeoDiagnostics.StableId(left.collider), NeoDiagnostics.StableId(right.collider));
            }
        }

        private sealed class Hit2DComparer : IComparer<RaycastHit2D>
        {
            public static readonly Hit2DComparer Instance = new();
            public int Compare(RaycastHit2D left, RaycastHit2D right)
            {
                int distance = left.distance.CompareTo(right.distance);
                return distance != 0 ? distance : string.CompareOrdinal(
                    NeoDiagnostics.StableId(left.collider), NeoDiagnostics.StableId(right.collider));
            }
        }

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

        private int Query3D(Vector3 from, Vector3 direction, float distance)
        {
            if (_hitBuffer == null || _hitBuffer.Length == 0)
            {
                _hitBuffer = new RaycastHit[Mathf.Clamp(_maxHitsPerCast, 1, MaxReusableHits)];
            }
            while (true)
            {
                int count = Physics.SphereCastNonAlloc(from, Mathf.Max(0.01f, _definition.Radius), direction,
                    _hitBuffer, distance, _definition.TargetLayers);
                if (count < _hitBuffer.Length)
                {
                    return count;
                }
                if (_hitBuffer.Length >= MaxReusableHits)
                {
                    _hitBuffer = Physics.SphereCastAll(from, Mathf.Max(0.01f, _definition.Radius), direction,
                        distance, _definition.TargetLayers);
                    return _hitBuffer.Length;
                }
                _hitBuffer = new RaycastHit[Mathf.Min(MaxReusableHits, _hitBuffer.Length * 2)];
            }
        }

        private int Query2D(Vector3 from, Vector3 direction, float distance)
        {
            if (_hit2DBuffer == null || _hit2DBuffer.Length == 0)
            {
                _hit2DBuffer = new RaycastHit2D[Mathf.Clamp(_maxHitsPerCast, 1, MaxReusableHits)];
            }
            while (true)
            {
                int count = Physics2D.CircleCastNonAlloc(from, Mathf.Max(0.01f, _definition.Radius), direction,
                    _hit2DBuffer, distance, _definition.TargetLayers);
                if (count < _hit2DBuffer.Length)
                {
                    return count;
                }
                if (_hit2DBuffer.Length >= MaxReusableHits)
                {
                    _hit2DBuffer = Physics2D.CircleCastAll(from, Mathf.Max(0.01f, _definition.Radius), direction,
                        distance, _definition.TargetLayers);
                    return _hit2DBuffer.Length;
                }
                _hit2DBuffer = new RaycastHit2D[Mathf.Min(MaxReusableHits, _hit2DBuffer.Length * 2)];
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
            int generation = _generation;
            if (_definition.Use3D)
            {
                int hitCount = Query3D(from, direction, distance);
                Array.Sort(_hitBuffer, 0, hitCount, HitComparer.Instance);
                for (int i = 0; i < hitCount; i++)
                {
                    TryHitTarget(_hitBuffer[i].collider != null ? _hitBuffer[i].collider.gameObject : null);
                    if (generation != _generation || _expired || !isActiveAndEnabled || _remainingHits <= 0)
                    {
                        return;
                    }
                }
            }

            if (_definition.Use2D)
            {
                int hit2DCount = Query2D(from, direction, distance);
                Array.Sort(_hit2DBuffer, 0, hit2DCount, Hit2DComparer.Instance);
                for (int i = 0; i < hit2DCount; i++)
                {
                    TryHitTarget(_hit2DBuffer[i].collider != null ? _hit2DBuffer[i].collider.gameObject : null);
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
