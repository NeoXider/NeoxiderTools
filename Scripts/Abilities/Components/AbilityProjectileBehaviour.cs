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
    [CreateFromMenu("Neoxider/Abilities/Ability Projectile")]
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

        [Tooltip("GroundXZ: the projectile's forward (Z) follows its flight, for 3D models. ScreenXY: it turns around " +
                 "Z so its right (+X) follows the flight, for 2D sprites drawn pointing right.")]
        [SerializeField] private AbilityZonePlane _plane = AbilityZonePlane.GroundXZ;

        /// <summary>How the projectile faces its flight direction (3D forward or 2D sprite right).</summary>
        public AbilityZonePlane Plane
        {
            get => _plane;
            set => _plane = value;
        }

        private static readonly System.Collections.Generic.List<UnitId> HitScratch =
            new System.Collections.Generic.List<UnitId>(8);

        // WHY: pierce means N distinct targets — a unit stays inside the hit radius for several
        // frames and must not absorb the remaining hits.
        private readonly System.Collections.Generic.HashSet<UnitId> _hitUnits =
            new System.Collections.Generic.HashSet<UnitId>();

        private AbilitySystemBehaviour _hub;
        private Transform _transform;
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

        /// <summary>True between <see cref="OnSpawned" /> and the moment the projectile finishes or is disabled.</summary>
        public bool IsFlying => _active;

        /// <summary>Cast this projectile reports hits for (0 when it was never spawned by the hub).</summary>
        public uint CastId => _castId;

        public void OnSpawned(SpawnRequest request, AbilitySystemBehaviour hub)
        {
            _hub = hub;
            _transform = transform;
            _castId = request.CastId;
            _owner = request.Owner;
            _homingTarget = request.TargetUnit;
            _effectiveSpeed = request.Magnitude > 0f ? request.Magnitude : _speed;
            _age = 0f;
            _hits = 0;
            _hitUnits.Clear();
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
                _direction = _transform.forward;
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_hub == null)
            {
                // WHY: the hub (and its pending cast table) is gone, e.g. after a scene switch; a pooled
                // projectile would otherwise stay active and invisible to the pool forever.
                _active = false;
                PoolManager.Release(gameObject);
                return;
            }

            float deltaTime = Time.deltaTime;
            float step = _effectiveSpeed * deltaTime;
            Transform self = _transform;
            Vector3 position = self.position;

            if (_homingTarget.IsValid && _hub.TryGetPosition(_homingTarget, out Vector3 targetPos))
            {
                Vector3 delta = targetPos - position;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    _direction = delta.normalized;
                }
            }

            position += _direction * step;
            if (_direction.sqrMagnitude > 0.0001f)
            {
                self.SetPositionAndRotation(position, AbilityZonePlaneFacing.Facing(_direction, _plane));
            }
            else
            {
                self.position = position;
            }

            TryHitUnits();

            if (_active && _hasDetonationPoint && (position - _detonationPoint).sqrMagnitude <= step * step)
            {
                Detonate(_detonationPoint);
                return;
            }

            _age += deltaTime;
            if (_active && _age >= _maxLifetime)
            {
                Despawn();
            }
        }

        // WHY: an external disable or destroy (scene unload, pool clear, SetActive(false)) bypasses
        // Despawn; release the pending cast now instead of leaving it routable until the domain timeout.
        // Despawn clears _active before PoolManager.Release disables the object, so nothing runs twice.
        private void OnDisable()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            AbilitySystemBehaviour hub = _hub;
            if (hub != null)
            {
                hub.System.ReleaseProjectileCast(_castId);
            }
        }

        private void TryHitUnits()
        {
            HitScratch.Clear();
            Vector3 position = _transform.position;
            _hub.QueryUnitsInRadius(position, _hitRadius, HitScratch);
            if (HitScratch.Count == 0)
            {
                return;
            }

            AbilitySystem system = _hub.System;
            AbilityUnit owner = system.GetUnit(_owner);

            for (int i = 0; i < HitScratch.Count && _active; i++)
            {
                UnitId id = HitScratch[i];
                if (id == _owner || _hitUnits.Contains(id))
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

                _hitUnits.Add(id);
                system.NotifyProjectileHit(_castId, id, position);
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
