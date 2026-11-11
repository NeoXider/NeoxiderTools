using System;
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Scene hub of the ability system: owns the single <see cref="AbilitySystem" /> instance,
    ///     ticks it, implements the world adapter over registered <see cref="AbilityUnitBehaviour" />s
    ///     and spawns archetype prefabs (projectiles/zones/summons) through <see cref="PoolManager" />.
    ///     Auto-creates itself on first access — drop units into a scene and press Play.
    /// </summary>
    [NeoDoc("Abilities/AbilitySystemBehaviour.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability System")]
    [AddComponentMenu("Neoxider/Abilities/Ability System")]
    [DefaultExecutionOrder(-500)]
    public sealed partial class AbilitySystemBehaviour : MonoBehaviour, IAbilityWorldAdapter
    {
        private static AbilitySystemBehaviour _instance;

        [Tooltip("Ability/modifier catalogs registered on Awake.")]
        [SerializeField] private List<AbilityLibrary> _libraries = new List<AbilityLibrary>();

        [Tooltip("Archetype id → prefab bindings for projectiles, zones and summons.")]
        [SerializeField] private List<SpawnArchetypeEntry> _archetypes = new List<SpawnArchetypeEntry>();

        [Tooltip("Advance the system every frame with Time.deltaTime. Disable for manual/server ticking.")]
        [SerializeField] private bool _autoTick = true;

        [Tooltip("Variable: one domain step per frame (Time.deltaTime). Fixed Ticks: whole ticks of 1 / Ticks Per " +
                 "Second, the same step every time (replays, lockstep, deterministic tests).")]
        [SerializeField] private AbilityTimeMode _timeMode = AbilityTimeMode.Variable;

        [Tooltip("Ticks per second of the Fixed Ticks time mode.")]
        [SerializeField] [Min(1)] private int _ticksPerSecond = 30;

        [Header("Diagnostics")]
        [Tooltip("Record the runtime trace (AbilitySystem.Trace) from the start: why reactions and effects fired.")]
        [SerializeField] private bool _traceEnabled;

        [Tooltip("Entries kept by the trace ring buffer.")]
        [SerializeField] [Min(8)] private int _traceCapacity = 256;

        [Header("Performance")]
        [Tooltip("Cell size (world units) of a per-frame spatial grid for unit radius queries (projectiles, " +
                 "area effects, auto-casters). 0 = scan every registered unit on each query, always exact. " +
                 "> 0 = snapshot unit positions once per frame: much faster with hundreds of units; a unit moved " +
                 "by another script later in the same frame is found at its new position from the next frame. " +
                 "About the typical hit/area radius (2-5) works well.")]
        [SerializeField] [Min(0f)] private float _spatialCellSize;

        private readonly Dictionary<UnitId, UnitPresence> _behaviours = new Dictionary<UnitId, UnitPresence>();

        private AbilitySystem _system;
        private AbilitySpatialGrid _grid;
        private int _gridFrame = -1;
        private bool _gridDirty = true;

        /// <summary>Scene singleton; auto-created when first accessed.</summary>
        public static AbilitySystemBehaviour I
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AbilitySystemBehaviour>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AbilitySystem");
                        _instance = go.AddComponent<AbilitySystemBehaviour>();
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        ///     The existing hub or null — never auto-creates. Use from OnDisable/OnDestroy paths:
        ///     during scene teardown the hub may already be gone, and resurrecting it there would
        ///     leak a fresh hub and unsubscribe from the wrong event bus.
        /// </summary>
        public static AbilitySystemBehaviour InstanceOrNull => _instance;

        public AbilitySystem System
        {
            get
            {
                EnsureInitialized();
                return _system;
            }
        }

        /// <summary>
        ///     When true the auto-tick is suspended (modifiers, cooldowns and regeneration freeze).
        ///     Use it for menus, level-up screens and pauses. Manual <see cref="AbilitySystem.Tick" />
        ///     callers are unaffected.
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        ///     Cell size of the per-frame spatial grid used by <see cref="QueryUnitsInRadius" />; 0 disables
        ///     the grid (exact linear scan). Non-finite or negative values are treated as 0.
        /// </summary>
        public float SpatialCellSize
        {
            get => _spatialCellSize;
            set
            {
                _spatialCellSize = float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value;
                _gridDirty = true;
            }
        }

        /// <summary>
        ///     Makes the next radius query re-read every unit position. Call it after teleporting units by
        ///     script when same-frame queries must see the new positions (spatial grid enabled only).
        /// </summary>
        public void InvalidateSpatialCache()
        {
            _gridDirty = true;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureInitialized();
        }

        private void Update()
        {
            if (_autoTick && !Paused && _system != null)
            {
                _system.Advance(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void EnsureInitialized()
        {
            if (_system != null)
            {
                return;
            }

            _system = new AbilitySystem { World = this };
            if (_timeMode == AbilityTimeMode.FixedTicks)
            {
                _system.Rules.Time = new FixedStepTimeSource(_ticksPerSecond);
            }

            if (_traceCapacity != _system.Trace.Capacity)
            {
                _system.Trace.SetCapacity(_traceCapacity);
            }

            _system.Trace.Enabled = _traceEnabled;
            for (int i = 0; i < _libraries.Count; i++)
            {
                if (_libraries[i] != null)
                {
                    _libraries[i].RegisterInto(_system);
                }
            }
        }

        /// <summary>Registers an additional library at runtime (DLC, mods, generated content).</summary>
        public void AddLibrary(AbilityLibrary library)
        {
            if (library == null)
            {
                return;
            }

            EnsureInitialized();
            library.RegisterInto(_system);
            if (!_libraries.Contains(library))
            {
                _libraries.Add(library);
            }
        }

        /// <summary>Adds an archetype binding at runtime.</summary>
        public void AddArchetype(string id, GameObject prefab)
        {
            if (!string.IsNullOrEmpty(id) && prefab != null)
            {
                _archetypes.Add(new SpawnArchetypeEntry { Id = id, Prefab = prefab });
            }
        }

        internal void RegisterBehaviour(AbilityUnitBehaviour behaviour)
        {
            if (behaviour != null && behaviour.UnitId.IsValid)
            {
                _behaviours[behaviour.UnitId] = new UnitPresence(behaviour);
                _gridDirty = true;
            }
        }

        internal void UnregisterBehaviour(AbilityUnitBehaviour behaviour)
        {
            if (behaviour == null || !behaviour.UnitId.IsValid || !_behaviours.Remove(behaviour.UnitId))
            {
                return;
            }

            // WHY: removing a registry entry keeps the enumeration order of the others, so the current
            // snapshot can drop the unit in place; kills no longer force a rebuild on the next query.
            if (!IsSpatialGridCurrent() || !_grid.Remove(behaviour.UnitId))
            {
                _gridDirty = true;
            }
        }

        /// <summary>Number of units with a registered scene presence.</summary>
        public int RegisteredUnitCount => _behaviours.Count;

        /// <summary>Scene component of a unit id, when the unit has a scene presence.</summary>
        public AbilityUnitBehaviour GetBehaviour(UnitId id)
        {
            return _behaviours.TryGetValue(id, out UnitPresence presence) ? presence.Behaviour : null;
        }

        public bool TryGetPosition(UnitId unit, out Vector3 position)
        {
            if (_behaviours.TryGetValue(unit, out UnitPresence presence) && presence.Behaviour != null)
            {
                position = presence.Transform.position;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>
        ///     Collects registered units within <paramref name="radius" /> of <paramref name="point" />
        ///     (inclusive, <see cref="AbilitySpatialGrid.IsWithinRadius" />). Uses the per-frame spatial grid when
        ///     <see cref="SpatialCellSize" /> is positive; both paths return the same ids in the same order (the
        ///     hub's registry enumeration order) for the same positions. That order matches registration order
        ///     only until a unit unregisters.
        /// </summary>
        public void QueryUnitsInRadius(Vector3 point, float radius, List<UnitId> results)
        {
            if (results == null)
            {
                return;
            }

            if (_spatialCellSize > 0f)
            {
                EnsureSpatialGrid().Query(point, radius, results);
                return;
            }

            foreach (KeyValuePair<UnitId, UnitPresence> pair in _behaviours)
            {
                if (pair.Value.Behaviour == null)
                {
                    continue;
                }

                if (AbilitySpatialGrid.IsWithinRadius(pair.Value.Transform.position, point, radius))
                {
                    results.Add(pair.Key);
                }
            }
        }

        public bool TryMoveUnit(UnitId unit, Vector3 newPosition)
        {
            if (_behaviours.TryGetValue(unit, out UnitPresence presence) && presence.Behaviour != null)
            {
                presence.Transform.position = newPosition;
                // WHY: knockback/pull/teleport interleave with hit queries; moving the one entry keeps the
                // snapshot current instead of rebuilding it on the next query. The transform is read back
                // so the entry holds exactly what a rebuild would store.
                if (!IsSpatialGridCurrent() || !_grid.Move(unit, presence.Transform.position))
                {
                    _gridDirty = true;
                }

                return true;
            }

            return false;
        }

        public void RequestSpawn(SpawnRequest request)
        {
            GameObject prefab = ResolveArchetype(request.ArchetypeId);
            if (prefab == null)
            {
                return;
            }

            Quaternion rotation = request.Direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(request.Direction.normalized)
                : Quaternion.identity;
            GameObject instance = PoolManager.Get(prefab, request.Position, rotation);
            if (instance == null)
            {
                return;
            }

            // WHY: TryGetComponent avoids the editor-only allocation GetComponent makes for a missing component.
            if (instance.TryGetComponent(out ISpawnedAbilityEntity spawned))
            {
                spawned.OnSpawned(request, this);
            }
        }

        private AbilitySpatialGrid EnsureSpatialGrid()
        {
            if (_grid == null)
            {
                _grid = new AbilitySpatialGrid(_spatialCellSize);
                _gridDirty = true;
            }
            else if (!_grid.CellSize.Equals(_spatialCellSize))
            {
                _grid.CellSize = _spatialCellSize;
                _gridDirty = true;
            }

            int frame = Time.frameCount;
            if (_gridDirty || frame != _gridFrame)
            {
                _grid.Clear();
                foreach (KeyValuePair<UnitId, UnitPresence> pair in _behaviours)
                {
                    if (pair.Value.Behaviour != null)
                    {
                        _grid.Add(pair.Key, pair.Value.Transform.position);
                    }
                }

                _gridFrame = frame;
                _gridDirty = false;
            }

            return _grid;
        }

        // WHY: only a snapshot taken this frame, not invalidated and built with the configured cell size
        // may be patched in place; anything else is rebuilt by the next query anyway.
        private bool IsSpatialGridCurrent()
        {
            return _spatialCellSize > 0f && _grid != null && !_gridDirty && _gridFrame == Time.frameCount &&
                   _grid.CellSize.Equals(_spatialCellSize);
        }

        private GameObject ResolveArchetype(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < _archetypes.Count; i++)
            {
                if (AbilityIdComparer.Same(_archetypes[i].Id, id))
                {
                    return _archetypes[i].Prefab;
                }
            }

            return null;
        }

        // WHY: the Transform is cached with the registration; radius queries read every unit's position
        // and the transform property is a native call per access.
        private readonly struct UnitPresence
        {
            public readonly AbilityUnitBehaviour Behaviour;
            public readonly Transform Transform;

            public UnitPresence(AbilityUnitBehaviour behaviour)
            {
                Behaviour = behaviour;
                Transform = behaviour.transform;
            }
        }

        // WHY: with Enter Play Mode Options disabling Domain Reload the static hub survives between
        // sessions — the destroyed component keeps its AbilitySystem and every registered unit alive,
        // and InstanceOrNull hands a dead object to teardown paths. OnExitingPlayMode releases it the
        // moment play stops (Unity 6.5+); SubsystemRegistration covers older editors and player builds.
        // The lifecycle attribute is source-generated, so the containing class must stay partial.
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }
    }
}
