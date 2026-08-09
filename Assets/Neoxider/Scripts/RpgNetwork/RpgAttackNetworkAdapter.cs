using Neo.Network;
using UnityEngine;

#if MIRROR
using Mirror;
#endif

namespace Neo.Rpg.Network
{
    /// <summary>Optional Mirror authority and projectile-spawn policy for <see cref="RpgAttackController"/>.</summary>
    [AddComponentMenu("Neoxider/RPG/Network/Rpg Attack Network Adapter")]
    public sealed class RpgAttackNetworkAdapter : MonoBehaviour, IRpgAttackNetworkAdapter
    {
        public bool CanProcessBuiltInInput(GameObject source)
        {
#if MIRROR
            if (!NeoNetworkState.IsNetworkActive)
            {
                return true;
            }

            NetworkIdentity identity = source != null ? source.GetComponentInParent<NetworkIdentity>() : null;
            if (identity == null)
            {
                identity = GetComponentInParent<NetworkIdentity>();
            }

            return identity == null || NeoNetworkState.HasAuthority(identity.gameObject);
#else
            return true;
#endif
        }

        public bool ShouldBlockProjectileSpawn(GameObject source)
        {
#if MIRROR
            return NeoNetworkState.IsClientOnly && source != null &&
                   source.GetComponentInParent<NetworkIdentity>() != null;
#else
            return false;
#endif
        }

        public void SpawnProjectile(GameObject projectile)
        {
#if MIRROR
            if (projectile != null && NeoNetworkState.IsServer &&
                projectile.TryGetComponent(out NetworkIdentity _))
            {
                NetworkServer.Spawn(projectile);
            }
#endif
        }
    }
}
