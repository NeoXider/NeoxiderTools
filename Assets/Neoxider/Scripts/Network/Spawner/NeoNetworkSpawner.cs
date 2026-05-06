#if MIRROR
using Mirror;
#endif
using UnityEngine;

namespace Neo.Network
{
    /// <summary>
    ///     Utility for spawning and despawning networked objects.
    ///     In multiplayer, uses <c>NetworkServer.Spawn/Destroy</c>.
    ///     In solo mode, falls back to <c>Instantiate/Destroy</c>.
    /// </summary>
    [NeoDoc("Network/NeoNetworkSpawner.md")]
    public static class NeoNetworkSpawner
    {
        /// <summary>
        ///     Spawns a prefab. In multiplayer, only the server should call this.
        ///     The spawned object is automatically replicated to all clients.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate.</param>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation.</param>
        /// <param name="parent">Optional parent transform (null by default).</param>
        /// <returns>The spawned GameObject.</returns>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[NeoNetworkSpawner] Prefab is null.");
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation, parent);

#if MIRROR
            if (NetworkServer.active)
            {
                NetworkServer.Spawn(instance);
            }
            else if (instance.TryGetComponent<NetworkIdentity>(out _))
            {
                Debug.LogWarning(
                    "[NeoNetworkSpawner] Spawn of a networked prefab was called without an active server. Destroying the instance to avoid client-only ghosts.",
                    prefab);
                Object.Destroy(instance);
                return null;
            }
#endif

            return instance;
        }

        /// <summary>
        ///     Spawns a prefab at the default position/rotation.
        /// </summary>
        public static GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        ///     Despawns (destroys) a networked object.
        ///     In multiplayer, only the server should call this.
        /// </summary>
        public static void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

#if MIRROR
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(instance);
                return;
            }
#endif

            Object.Destroy(instance);
        }

        /// <summary>
        ///     Checks whether the current runtime context has spawn authority.
        ///     Returns true in solo mode, or on the server in multiplayer.
        /// </summary>
        public static bool CanSpawn
        {
            get
            {
#if MIRROR
                return NetworkServer.active;
#else
                return true;
#endif
            }
        }
    }
}
