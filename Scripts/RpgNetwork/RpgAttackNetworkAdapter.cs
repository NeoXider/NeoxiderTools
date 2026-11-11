using Neo.Network;
using UnityEngine;

#if MIRROR
using Mirror;
#endif

namespace Neo.Rpg.Network
{
    /// <summary>Optional Mirror authority and projectile-spawn policy for <see cref="RpgAttackController"/>.</summary>
    [NeoDoc("Rpg/Network.md")]
    [AddComponentMenu("Neoxider/RPG/Network/Rpg Attack Network Adapter")]
    public sealed class RpgAttackNetworkAdapter : MonoBehaviour, IRpgAttackNetworkAdapter
    {
        /// <summary>Built-in attack input runs only for the peer that owns the attacker (always offline or without a NetworkIdentity).</summary>
        public bool CanProcessBuiltInInput(GameObject source)
        {
#if MIRROR
            if (!NeoNetworkState.IsNetworkActive)
            {
                return true;
            }

            NetworkIdentity identity = ResolveIdentity(source);
            return identity == null || NeoNetworkState.HasAuthority(identity.gameObject);
#else
            return true;
#endif
        }

        /// <summary>A pure client never spawns networked projectiles itself; the server spawns and replicates them.</summary>
        public bool ShouldBlockProjectileSpawn(GameObject source)
        {
#if MIRROR
            return NeoNetworkState.IsClientOnly && ResolveIdentity(source) != null;
#else
            return false;
#endif
        }

        /// <summary>Server: network-spawns a projectile that carries a NetworkIdentity.</summary>
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

#if MIRROR
        private NetworkIdentity ResolveIdentity(GameObject source)
        {
            NetworkIdentity identity = source != null ? source.GetComponentInParent<NetworkIdentity>() : null;
            return identity != null ? identity : GetComponentInParent<NetworkIdentity>();
        }
#endif
    }
}
