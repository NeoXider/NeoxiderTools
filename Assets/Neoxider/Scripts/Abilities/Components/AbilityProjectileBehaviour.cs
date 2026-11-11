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
        /// <summary>How a spawned projectile moves.</summary>
        public enum ProjectileMotion
        {
            /// <summary>Homes on the target unit, or flies straight to the cast point.</summary>
            Flight = 0,

            /// <summary>Circles its owner until Max Lifetime runs out (orbiting blades, a rotating shield).</summary>
            Orbit = 1
        }

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

        [Header("Motion")]
        [Tooltip("Flight: homes on the target unit or flies straight. Orbit: circles its owner for Max Lifetime " +
                 "seconds and ignores Max Hits. Give an orbiting ability NoTarget targeting and Projectile delivery.")]
        [SerializeField] private ProjectileMotion _motion = ProjectileMotion.Flight;

        [Tooltip("Orbit: distance from the owner. Not used when Hit Points are assigned: the root then stays on the " +
                 "owner and only turns, and the points keep their own offsets.")]
        [SerializeField] [Min(0f)] private float _orbitRadius = 2f;

        [Tooltip("Orbit: degrees per second. Negative turns the other way.")]
        [SerializeField] private float _orbitDegreesPerSecond = 180f;

        [Tooltip("Points that hit units, each with Hit Radius: the blades of an orbiting ring. Empty = this object.")]
        [SerializeField] private Transform[] _hitPoints = System.Array.Empty<Transform>();

        [Tooltip("Seconds before the same unit can be hit again. 0 = each unit once per projectile.")]
        [SerializeField] [Min(0f)] private float _rehitInterval;

        /// <summary>How the projectile faces its flight direction (3D forward or 2D sprite right).</summary>
        public AbilityZonePlane Plane
        {
            get => _plane;
            set => _plane = value;
        }

        /// <summary>Flight (homing or straight) or Orbit around the owner.</summary>
        public ProjectileMotion Motion
        {
            get => _motion;
            set => _motion = value;
        }

        /// <summary>Orbit distance from the owner when no Hit Points are assigned.</summary>
        public float OrbitRadius
        {
            get => _orbitRadius;
            set => _orbitRadius = Mathf.Max(0f, value);
        }

        /// <summary>Orbit speed in degrees per second.</summary>
        public float OrbitDegreesPerSecond
        {
            get => _orbitDegreesPerSecond;
            set => _orbitDegreesPerSecond = value;
        }

        /// <summary>Seconds before the same unit can be hit again. 0 = once per projectile.</summary>
        public float RehitInterval
        {
            get => _rehitInterval;
            set => _rehitInterval = Mathf.Max(0f, value);
        }

        /// <summary>Hit detection radius in world units.</summary>
        public float HitRadius
        {
            get => _hitRadius;
            set => _hitRadius = Mathf.Max(0f, value);
        }

        /// <summary>
        ///     Applies a settings block, as the ability hub does for a plain prefab used as a projectile.
        /// </summary>
        public void Configure(AbilityProjectileSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            _speed = settings.Speed;
            _maxLifetime = settings.MaxLifetime;
            _hitRadius = Mathf.Max(0f, settings.HitRadius);
            _maxHits = settings.MaxHits;
            _plane = settings.Plane;
            _motion = settings.Motion;
            _orbitRadius = Mathf.Max(0f, settings.OrbitRadius);
            _orbitDegreesPerSecond = settings.OrbitDegreesPerSecond;
            _rehitInterval = Mathf.Max(0f, settings.RehitInterval);
        }

        private static readonly System.Collections.Generic.List<UnitId> HitScratch =
            new System.Collections.Generic.List<UnitId>(8);

        // WHY: pierce means N distinct targets — a unit stays inside the hit radius for several
        // frames and must not absorb the remaining hits.
        private readonly System.Collections.Generic.HashSet<UnitId> _hitUnits =
            new System.Collections.Generic.HashSet<UnitId>();

        private readonly System.Collections.Generic.Dictionary<UnitId, float> _hitTimes =
            new System.Collections.Generic.Dictionary<UnitId, float>();

        private float _orbitAngle;
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
            _hitTimes.Clear();
            _orbitAngle = 0f;
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
            if (_motion == ProjectileMotion.Orbit)
            {
                UpdateOrbit(deltaTime);
                return;
            }

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

        private void UpdateOrbit(float deltaTime)
        {
            if (!_hub.TryGetPosition(_owner, out Vector3 center))
            {
                Despawn();
                return;
            }

            _orbitAngle += _orbitDegreesPerSecond * deltaTime;
            bool screen = _plane == AbilityZonePlane.ScreenXY;
            Quaternion turn = Quaternion.AngleAxis(_orbitAngle, screen ? Vector3.forward : Vector3.up);
            if (_hitPoints != null && _hitPoints.Length > 0)
            {
                _transform.SetPositionAndRotation(center, turn);
            }
            else
            {
                Vector3 arm = screen ? Vector3.right : Vector3.forward;
                _transform.SetPositionAndRotation(center + turn * arm * _orbitRadius, turn);
            }

            TryHitUnits();
            _age += deltaTime;
            if (_active && _age >= _maxLifetime)
            {
                Despawn();
            }
        }

        private void TryHitUnits()
        {
            if (_hitPoints != null && _hitPoints.Length > 0)
            {
                for (int i = 0; i < _hitPoints.Length && _active; i++)
                {
                    if (_hitPoints[i] != null)
                    {
                        HitAt(_hitPoints[i].position);
                    }
                }

                return;
            }

            HitAt(_transform.position);
        }

        private void HitAt(Vector3 position)
        {
            HitScratch.Clear();
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
                if (id == _owner || !CanHit(id))
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

                MarkHit(id);
                system.NotifyProjectileHit(_castId, id, position);
                // WHY: an orbiting projectile lives for its lifetime, not for a number of hits.
                if (_motion == ProjectileMotion.Orbit)
                {
                    continue;
                }

                _hits++;
                if (_hits >= Mathf.Max(1, _maxHits))
                {
                    Despawn();
                }
            }
        }

        private bool CanHit(UnitId id)
        {
            if (_rehitInterval > 0f)
            {
                return !_hitTimes.TryGetValue(id, out float hitAt) || _age - hitAt >= _rehitInterval;
            }

            // WHY: pierce means N distinct targets; a unit stays inside the hit radius for several frames.
            return !_hitUnits.Contains(id);
        }

        private void MarkHit(UnitId id)
        {
            if (_rehitInterval > 0f)
            {
                _hitTimes[id] = _age;
            }
            else
            {
                _hitUnits.Add(id);
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
