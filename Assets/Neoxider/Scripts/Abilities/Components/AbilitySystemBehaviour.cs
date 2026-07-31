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
    public sealed class AbilitySystemBehaviour : MonoBehaviour, IAbilityWorldAdapter
    {
        private static AbilitySystemBehaviour _instance;

        [Tooltip("Ability/modifier catalogs registered on Awake.")]
        [SerializeField] private List<AbilityLibrary> _libraries = new List<AbilityLibrary>();

        [Tooltip("Archetype id → prefab bindings for projectiles, zones and summons.")]
        [SerializeField] private List<SpawnArchetypeEntry> _archetypes = new List<SpawnArchetypeEntry>();

        [Tooltip("Advance the system every frame with Time.deltaTime. Disable for manual/server ticking.")]
        [SerializeField] private bool _autoTick = true;

        private readonly Dictionary<UnitId, AbilityUnitBehaviour> _behaviours =
            new Dictionary<UnitId, AbilityUnitBehaviour>();

        private AbilitySystem _system;

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
                        var go = new GameObject("AbilitySystem");
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
                _system.Tick(Time.deltaTime);
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
                _behaviours[behaviour.UnitId] = behaviour;
            }
        }

        internal void UnregisterBehaviour(AbilityUnitBehaviour behaviour)
        {
            if (behaviour != null && behaviour.UnitId.IsValid)
            {
                _behaviours.Remove(behaviour.UnitId);
            }
        }

        /// <summary>Scene component of a unit id, when the unit has a scene presence.</summary>
        public AbilityUnitBehaviour GetBehaviour(UnitId id)
        {
            return _behaviours.TryGetValue(id, out AbilityUnitBehaviour behaviour) ? behaviour : null;
        }

        public bool TryGetPosition(UnitId unit, out Vector3 position)
        {
            if (_behaviours.TryGetValue(unit, out AbilityUnitBehaviour behaviour) && behaviour != null)
            {
                position = behaviour.transform.position;
                return true;
            }

            position = default;
            return false;
        }

        public void QueryUnitsInRadius(Vector3 point, float radius, List<UnitId> results)
        {
            float sqrRadius = radius * radius;
            foreach (KeyValuePair<UnitId, AbilityUnitBehaviour> pair in _behaviours)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if ((pair.Value.transform.position - point).sqrMagnitude <= sqrRadius)
                {
                    results.Add(pair.Key);
                }
            }
        }

        public bool TryMoveUnit(UnitId unit, Vector3 newPosition)
        {
            if (_behaviours.TryGetValue(unit, out AbilityUnitBehaviour behaviour) && behaviour != null)
            {
                behaviour.transform.position = newPosition;
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

            var spawned = instance.GetComponent<ISpawnedAbilityEntity>();
            spawned?.OnSpawned(request, this);
        }

        private GameObject ResolveArchetype(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < _archetypes.Count; i++)
            {
                if (string.Equals(_archetypes[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return _archetypes[i].Prefab;
                }
            }

            return null;
        }
    }
}
