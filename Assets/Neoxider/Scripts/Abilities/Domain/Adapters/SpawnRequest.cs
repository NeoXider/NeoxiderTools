using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Domain-side description of an entity spawn (projectile, zone, summon).
    ///     Contains only serializable data — the host maps ArchetypeId to prefab/pool.
    /// </summary>
    public readonly struct SpawnRequest
    {
        public readonly string ArchetypeId;
        public readonly UnitId Owner;
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly UnitId TargetUnit;
        public readonly string AbilityId;
        public readonly float Magnitude;

        /// <summary>Cast id for projectile spawns — the host reports hits back with it.</summary>
        public readonly uint CastId;

        public SpawnRequest(string archetypeId, UnitId owner, Vector3 position, Vector3 direction,
            UnitId targetUnit, string abilityId, float magnitude, uint castId = 0)
        {
            ArchetypeId = archetypeId;
            Owner = owner;
            Position = position;
            Direction = direction;
            TargetUnit = targetUnit;
            AbilityId = abilityId;
            Magnitude = magnitude;
            CastId = castId;
        }
    }
}
