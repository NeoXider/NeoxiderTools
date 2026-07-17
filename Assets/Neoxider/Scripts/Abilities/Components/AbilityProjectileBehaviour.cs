using Neo.Tools;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Pooled data-driven projectile: homes to the target unit or flies toward the cast point,
    ///     reports hits back into the domain (<see cref="AbilitySystem.NotifyProjectileHit" />) and
    ///     releases itself through <see cref="PoolManager" />. Uses distance checks against registered
    ///     units — no colliders or physics setup required.
    /// </summary>
    [NeoDoc("Abilities/AbilityProjectileBehaviour.md")]
    [AddComponentMenu("Neoxider/Abilities/Ability Projectile")]
    public sealed class AbilityProjectileBehaviour : MonoBehaviour, ISpawnedAbilityEntity
    {
        [Tooltip("Fallback speed when the spawn request carries none.")]
        [SerializeField] private float _speed = 20f;

        [Tooltip("Projectile dies after this many seconds.")]
        [SerializeField] private float _maxLifetime = 5f;

        [Tooltip("Hit detection radius in world units.")]
        [SerializeField] private float _hitRadius = 0.5f;

        [Tooltip("How many units the projectile can hit before despawning (pierce).")]
        [SerializeField] private int _maxHits = 1;

        private static readonly System.Collections.Generic.List<UnitId> HitScratch =
            new System.Collections.Generic.List<UnitId>(8);

        private AbilitySystemBehaviour _hub;
        private uint _castId;
        private UnitId _owner;
        private UnitId _homingTarget;
        private Vector3 _direction;
        private Vector3 _detonationPoint;
        private bool _hasDetonationPoint;
        private float _age;
        private float _effectiveSpeed;
        private int _hits;
        private bool _active;

        public void OnSpawned(SpawnRequest request, AbilitySystemBehaviour hub)
        {
            _hub = hub;
            _castId = request.CastId;
            _owner = request.Owner;
            _homingTarget = request.TargetUnit;
            _effectiveSpeed = request.Magnitude > 0f ? request.Magnitude : _speed;
            _age = 0f;
            _hits = 0;
            _active = true;

            if (!_homingTarget.IsValid && request.Direction.sqrMagnitude > 0.0001f)
            {
                _detonationPoint = request.Position + request.Direction;
                _hasDetonationPoint = true;
                _direction = request.Direction.normalized;
            }
            else
            {
                _hasDetonationPoint = false;
                _direction = transform.forward;
            }
        }

        private void Update()
        {
            if (!_active || _hub == null)
            {
                return;
            }

            float step = _effectiveSpeed * Time.deltaTime;

            if (_homingTarget.IsValid && _hub.TryGetPosition(_homingTarget, out Vector3 targetPos))
            {
                Vector3 delta = targetPos - transform.position;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    _direction = delta.normalized;
                }
            }

            transform.position += _direction * step;
            if (_direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }

            TryHitUnits();

            if (_active && _hasDetonationPoint &&
                (transform.position - _detonationPoint).sqrMagnitude <= step * step)
            {
                Detonate(_detonationPoint);
                return;
            }

            _age += Time.deltaTime;
            if (_active && _age >= _maxLifetime)
            {
                Despawn();
            }
        }

        private void TryHitUnits()
        {
            HitScratch.Clear();
            _hub.QueryUnitsInRadius(transform.position, _hitRadius, HitScratch);
            if (HitScratch.Count == 0)
            {
                return;
            }

            AbilitySystem system = _hub.System;
            AbilityUnit owner = system.GetUnit(_owner);

            for (int i = 0; i < HitScratch.Count && _active; i++)
            {
                UnitId id = HitScratch[i];
                if (id == _owner)
                {
                    continue;
                }

                AbilityUnit unit = system.GetUnit(id);
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                // WHY: projectiles hit enemies of the owner by default, so allies are skipped here.
                if (owner != null && !owner.Team.IsEnemyOf(unit.Team))
                {
                    continue;
                }

                system.NotifyProjectileHit(_castId, id, transform.position);
                _hits++;
                if (_hits >= Mathf.Max(1, _maxHits))
                {
                    Despawn();
                }
            }
        }

        private void Detonate(Vector3 point)
        {
            _hub.System.NotifyProjectileHit(_castId, UnitId.None, point);
            Despawn();
        }

        private void Despawn()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _hub.System.ReleaseProjectileCast(_castId);
            PoolManager.Release(gameObject);
        }
    }
}
