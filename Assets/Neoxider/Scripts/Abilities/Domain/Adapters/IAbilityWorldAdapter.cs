using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Host seam between the pure ability domain and the world (scene, physics, spawning).
    ///     The Unity layer implements it over transforms/physics; tests use an in-memory fake.
    /// </summary>
    public interface IAbilityWorldAdapter
    {
        /// <summary>World position of a unit. Returns false when the unit has no world presence.</summary>
        bool TryGetPosition(UnitId unit, out Vector3 position);

        /// <summary>Collects unit ids within a radius of a world point into <paramref name="results" />.</summary>
        void QueryUnitsInRadius(Vector3 point, float radius, List<UnitId> results);

        /// <summary>
        ///     Requests a unit be moved to <paramref name="newPosition" /> (the motion seam for
        ///     knockback / pull / teleport). The host clamps to navmesh/bounds and applies it; returns
        ///     true when the unit was moved. Hosts without a world presence for the unit return false.
        /// </summary>
        bool TryMoveUnit(UnitId unit, Vector3 newPosition);

        /// <summary>
        ///     Asks the host to spawn a gameplay entity (projectile, zone, summon) described by an open
        ///     archetype id. The host resolves the archetype; the domain only tracks the receipt.
        /// </summary>
        void RequestSpawn(SpawnRequest request);
    }
}
