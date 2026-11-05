using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>A projectile, zone visual or summon the fixed core asks the host to realize.</summary>
    public readonly struct FixedSpawnRequest
    {
        public readonly string ArchetypeId;
        public readonly int Owner;
        public readonly FixedVec3 Position;
        public readonly FixedVec3 Direction;
        public readonly int TargetUnit;
        public readonly string AbilityId;

        /// <summary>Raw magnitude: projectile speed, zone radius or the spawn op's amount.</summary>
        public readonly int MagnitudeRaw;

        public readonly uint CastId;
        public readonly int ZoneId;
        public readonly int DurationTicks;

        public FixedSpawnRequest(string archetypeId, int owner, FixedVec3 position, FixedVec3 direction, int targetUnit,
            string abilityId, int magnitudeRaw, uint castId = 0u, int zoneId = 0, int durationTicks = 0)
        {
            ArchetypeId = archetypeId;
            Owner = owner;
            Position = position;
            Direction = direction;
            TargetUnit = targetUnit;
            AbilityId = abilityId;
            MagnitudeRaw = magnitudeRaw;
            CastId = castId;
            ZoneId = zoneId;
            DurationTicks = durationTicks;
        }
    }

    /// <summary>
    ///     World seam of the fixed core. Answers must be deterministic: the same units in the same order on every
    ///     peer for the same state.
    /// </summary>
    public interface IFixedWorldAdapter
    {
        bool TryGetPosition(int unit, out FixedVec3 position);

        /// <summary>Appends the ids of units within <paramref name="radiusRaw" /> of <paramref name="center" /> (inclusive).</summary>
        void QueryUnitsInRadius(FixedVec3 center, int radiusRaw, List<int> results);

        void RequestSpawn(in FixedSpawnRequest request);
    }

    /// <summary>
    ///     Default adapter: positions are <see cref="FixedUnit.Position" /> and radius queries scan the units in
    ///     creation order (linear; a host with thousands of units plugs its spatial hash instead). Spawn requests
    ///     are counted and dropped.
    /// </summary>
    public sealed class FixedUnitWorldAdapter : IFixedWorldAdapter
    {
        private readonly FixedAbilitySystem _system;

        public FixedUnitWorldAdapter(FixedAbilitySystem system)
        {
            _system = system;
        }

        public int SpawnRequestCount { get; private set; }

        public bool TryGetPosition(int unit, out FixedVec3 position)
        {
            FixedUnit found = _system.GetUnit(unit);
            if (found == null)
            {
                position = FixedVec3.Zero;
                return false;
            }

            position = found.Position;
            return true;
        }

        public void QueryUnitsInRadius(FixedVec3 center, int radiusRaw, List<int> results)
        {
            IReadOnlyList<FixedUnit> units = _system.UnitsInCreationOrder;
            for (int i = 0; i < units.Count; i++)
            {
                FixedUnit unit = units[i];
                if (FixedVec3.IsWithinRadius(unit.Position, center, radiusRaw))
                {
                    results.Add(unit.Id);
                }
            }
        }

        public void RequestSpawn(in FixedSpawnRequest request)
        {
            SpawnRequestCount++;
        }
    }
}
