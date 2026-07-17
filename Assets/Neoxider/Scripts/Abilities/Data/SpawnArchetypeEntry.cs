using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Maps a domain spawn archetype id to a scene prefab (projectile, zone, summon).
    /// </summary>
    [Serializable]
    public struct SpawnArchetypeEntry
    {
        [Tooltip("Archetype id referenced by abilities (ProjectileArchetypeId, spawn op ArchetypeId).")]
        public string Id;

        [Tooltip("Prefab spawned through PoolManager when the domain requests this archetype.")]
        public GameObject Prefab;
    }
}
