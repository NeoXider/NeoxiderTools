using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>What the ability hub adds to a spawned instance whose prefab has no ability component of its own.</summary>
    public enum SpawnArchetypeRole
    {
        /// <summary>The prefab is spawned unchanged; it may carry its own ability component.</summary>
        AsIs = 0,

        /// <summary>A plain visual prefab flies as a projectile: the hub adds and configures the component.</summary>
        Projectile = 1,

        /// <summary>A plain visual prefab shows a zone: the hub adds and configures the component.</summary>
        ZoneView = 2
    }

    /// <summary>Projectile settings applied by the hub to a prefab that has no <see cref="AbilityProjectileBehaviour" />.</summary>
    [Serializable]
    public sealed class AbilityProjectileSettings
    {
        [Tooltip("Fallback speed when the spawn request carries none.")]
        public float Speed = 20f;

        [Tooltip("Projectile dies after this many seconds.")]
        public float MaxLifetime = 5f;

        [Tooltip("Hit detection radius in world units.")]
        public float HitRadius = 0.5f;

        [Tooltip("How many units the projectile can hit before despawning (pierce).")]
        public int MaxHits = 1;

        [Tooltip("GroundXZ for 3D models, ScreenXY for 2D sprites drawn pointing right.")]
        public AbilityZonePlane Plane = AbilityZonePlane.GroundXZ;

        [Tooltip("Flight: homes or flies straight. Orbit: circles its owner for Max Lifetime seconds.")]
        public AbilityProjectileBehaviour.ProjectileMotion Motion = AbilityProjectileBehaviour.ProjectileMotion.Flight;

        [Tooltip("Orbit: distance from the owner.")]
        public float OrbitRadius = 2f;

        [Tooltip("Orbit: degrees per second.")]
        public float OrbitDegreesPerSecond = 180f;

        [Tooltip("Seconds before the same unit can be hit again. 0 = once per projectile.")]
        public float RehitInterval;
    }

    /// <summary>Zone view settings applied by the hub to a prefab that has no <see cref="AbilityZoneView" />.</summary>
    [Serializable]
    public sealed class AbilityZoneViewSettings
    {
        [Tooltip("Scale the object so the mesh matches the zone radius.")]
        public bool ScaleToRadius = true;

        [Tooltip("Ground XZ: a 3D floor disc. Screen XY: 2D sprites facing the camera.")]
        public AbilityZonePlane Plane = AbilityZonePlane.GroundXZ;

        [Tooltip("Radius of the mesh or sprite at scale 1 (0.5 for a unit quad or a 1-unit sprite).")]
        public float MeshRadius = 0.5f;

        [Tooltip("Scale of the axis that is not scaled to the radius.")]
        public float HeightScale = 1f;

        [Tooltip("Offset from the zone centre.")]
        public Vector3 Offset = new Vector3(0f, 0.02f, 0f);
    }

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

        [Tooltip("As Is: the prefab is spawned unchanged. Projectile / Zone View: a plain visual prefab is enough; " +
                 "the hub adds the component to each spawned instance and applies the settings below. A prefab that " +
                 "already has the component keeps its own settings.")]
        public SpawnArchetypeRole Role;

        [Tooltip("Used when Role is Projectile and the prefab has no AbilityProjectileBehaviour.")]
        public AbilityProjectileSettings Projectile;

        [Tooltip("Used when Role is Zone View and the prefab has no AbilityZoneView.")]
        public AbilityZoneViewSettings Zone;
    }
}
