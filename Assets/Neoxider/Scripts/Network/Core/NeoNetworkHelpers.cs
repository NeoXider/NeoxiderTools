#if MIRROR
using Mirror;
#endif
using UnityEngine;

namespace Neo.Network
{
    /// <summary>
    ///     Static helper utilities for common network checks.
    ///     All methods return safe solo-mode defaults when Mirror is not installed.
    /// </summary>
    public static class NeoNetworkHelpers
    {
        /// <summary>
        ///     Whether any network session is currently running (server, client, or host).
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
        ///     Whether the current runtime context is the server (or host acting as server).
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
        ///     Whether the current runtime is a client (connected to a remote server).
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
        ///     Whether the runtime is a host (server + client simultaneously).
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
        ///     Whether it is safe to perform server-authoritative operations
        ///     (spawn, mutate game state, save world data, etc.).
        ///     Returns <c>true</c> in solo mode.
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
    }
}
