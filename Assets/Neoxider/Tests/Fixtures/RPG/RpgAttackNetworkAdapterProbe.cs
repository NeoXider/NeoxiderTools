using Neo.Rpg;
using UnityEngine;

namespace Neo.Tests.Edit.RPG
{
    /// <summary>
    ///     Attack network adapter probe that rejects built-in input and projectile spawns and counts the queries.
    /// </summary>
    /// <remarks>
    ///     WHY: test MonoBehaviours live in the runtime fixture assembly; Unity 6.3 rejects AddComponent for
    ///     MonoBehaviours declared inside the Editor-only test assembly.
    /// </remarks>
    public sealed class RpgAttackNetworkAdapterProbe : MonoBehaviour, IRpgAttackNetworkAdapter
    {
        /// <summary>Number of authority queries answered by this adapter.</summary>
        public int Queries { get; private set; }

        /// <inheritdoc />
        public bool CanProcessBuiltInInput(GameObject source)
        {
            Queries++;
            return false;
        }

        /// <inheritdoc />
        public bool ShouldBlockProjectileSpawn(GameObject source)
        {
            Queries++;
            return true;
        }

        /// <inheritdoc />
        public void SpawnProjectile(GameObject projectile)
        {
        }
    }
}
