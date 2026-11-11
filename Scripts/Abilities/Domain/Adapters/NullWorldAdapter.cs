using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Default no-op world adapter: no positions, empty spatial queries, ignored spawns.
    ///     Lets the pure domain run headless (tests, servers) until a host installs a real adapter.
    /// </summary>
    public sealed class NullWorldAdapter : IAbilityWorldAdapter
    {
        public static readonly NullWorldAdapter Instance = new NullWorldAdapter();

        public bool TryGetPosition(UnitId unit, out Vector3 position)
        {
            position = default;
            return false;
        }

        public void QueryUnitsInRadius(Vector3 point, float radius, List<UnitId> results)
        {
        }

        public bool TryMoveUnit(UnitId unit, Vector3 newPosition)
        {
            return false;
        }

        public void RequestSpawn(SpawnRequest request)
        {
        }
    }
}
