using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>Optional transport boundary for attack input authority and projectile spawning.</summary>
    public interface IRpgAttackNetworkAdapter
    {
        bool CanProcessBuiltInInput(GameObject source);
        bool ShouldBlockProjectileSpawn(GameObject source);
        void SpawnProjectile(GameObject projectile);
    }
}
