using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>
    /// Static helper to check network state globally safely.
    /// Falls back to safe solo-mode defaults when Mirror is not installed.
    /// </summary>
    /// <remarks>
    /// Supersedes the former <c>NeoNetworkHelpers</c> class — all network state
    /// queries are now consolidated here.
    /// </remarks>
    public static class NeoNetworkState
    {
        /// <summary>
        /// True if running as a Server or Host. In Solo mode (no Mirror), always true.
        /// </summary>
        public static bool IsServer
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

        /// <summary>
        /// True if running as a Client. In Solo mode (no Mirror), always true.
        /// </summary>
        public static bool IsClient
        {
            get
            {
#if MIRROR
                return NetworkClient.active;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// True if the runtime is a pure client (connected to a remote server, NOT hosting).
        /// In Solo mode (no Mirror), always false.
        /// </summary>
        public static bool IsClientOnly
        {
            get
            {
#if MIRROR
                return NetworkClient.active && !NetworkServer.active;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// True if the runtime is a host (server + client simultaneously).
        /// In Solo mode (no Mirror), always true.
        /// </summary>
        public static bool IsHost
        {
            get
            {
#if MIRROR
                return NetworkServer.active && NetworkClient.active;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Whether any network session is currently running (server, client, or host).
        /// In Solo mode (no Mirror), always false.
        /// </summary>
        public static bool IsNetworkActive
        {
            get
            {
#if MIRROR
                return NetworkServer.active || NetworkClient.active;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Whether it is safe to perform server-authoritative operations
        /// (spawn, mutate game state, save world data, etc.).
        /// Returns <c>true</c> in solo mode.
        /// </summary>
        public static bool CanMutateState
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

        /// <summary>
        /// Checks if the local player has authority over the given GameObject.
        /// Useful for LocalPlayerOnly execution filtering.
        /// In Solo mode (no Mirror), always true.
        /// </summary>
        public static bool HasAuthority(GameObject obj)
        {
#if MIRROR
            if (obj != null && obj.TryGetComponent(out NetworkIdentity identity))
            {
                return identity.isLocalPlayer || identity.isOwned;
            }
            return false; // In multiplayer, if it lacks NetworkIdentity, NO ONE has authority to trigger LocalPlayer events from it.
#else
            return true;
#endif
        }

#if MIRROR
        /// <summary>
        /// Checks a manual NoCode authority policy for commands declared with requiresAuthority = false.
        /// </summary>
        public static bool IsAuthorized(GameObject obj, NetworkConnectionToClient sender, NetworkAuthorityMode mode)
        {
            switch (mode)
            {
                case NetworkAuthorityMode.None:
                    return true;
                case NetworkAuthorityMode.ServerOnly:
                    return sender == null || sender == NetworkServer.localConnection;
                case NetworkAuthorityMode.OwnerOnly:
                    if (sender == null || sender == NetworkServer.localConnection)
                    {
                        return true;
                    }

                    return obj != null
                           && obj.TryGetComponent(out NetworkIdentity identity)
                           && identity.connectionToClient != null
                           && sender == identity.connectionToClient;
                default:
                    return true;
            }
        }

#endif
    }
}
