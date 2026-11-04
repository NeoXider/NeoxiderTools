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

            return
                false; // WHY: In multiplayer, if it lacks NetworkIdentity, NO ONE has authority to trigger LocalPlayer events from it.
#else
            return true;
#endif
        }

#if MIRROR
        /// <summary>
        /// True when a message sent to <paramref name="connection"/> right now will be dispatched by the peer:
        /// the connection finished the Mirror <c>Ready</c> handshake <b>and</b> owns a player object.
        /// </summary>
        /// <remarks>
        /// <c>NetworkServer.SendToReady</c> only checks <c>isReady</c>, but the spawn burst (and with it every
        /// handler a scene object registers in <c>Awake</c>) is only delivered once the connection owns a player.
        /// A broadcast that lands before that makes Mirror disconnect the client for an unknown message id.
        /// </remarks>
        /// <param name="connection">Server-side connection; <see langword="null"/> returns <see langword="false"/>.</param>
        public static bool IsConnectionSpawned(NetworkConnectionToClient connection)
        {
            return connection != null && connection.isReady && connection.identity != null;
        }

        /// <summary>
        /// True for the host's own loopback connection (the "client" that lives inside the server process).
        /// </summary>
        public static bool IsLocalHostConnection(NetworkConnectionToClient connection)
        {
            return connection is LocalConnectionToClient;
        }

        /// <summary>
        /// Checks a manual NoCode authority policy for commands declared with requiresAuthority = false.
        /// Gathers the Mirror facts about <paramref name="sender"/> and lets <see cref="NetworkAuthorityPolicy"/>
        /// decide, so the decision table lives in one transport-free place.
        /// </summary>
        /// <param name="obj">Target object; its <see cref="NetworkIdentity"/> owner is compared for OwnerOnly.</param>
        /// <param name="sender">Command sender; <see langword="null"/> means direct server code.</param>
        /// <param name="mode">Configured policy; an unnamed value is rejected for every caller.</param>
        public static bool IsAuthorized(GameObject obj, NetworkConnectionToClient sender, NetworkAuthorityMode mode)
        {
            bool isDirectServerCall = sender == null;
            bool isLocalHostSender = !isDirectServerCall && sender == NetworkServer.localConnection;
            // WHY: the ownership lookup is the only fact that costs a component query, and only OwnerOnly reads it.
            bool senderOwnsObject = mode == NetworkAuthorityMode.OwnerOnly && !isDirectServerCall &&
                                    !isLocalHostSender && SenderOwns(obj, sender);
            return NetworkAuthorityPolicy.IsAuthorized(mode, isDirectServerCall, isLocalHostSender, senderOwnsObject);
        }

        private static bool SenderOwns(GameObject obj, NetworkConnectionToClient sender)
        {
            return obj != null
                   && obj.TryGetComponent(out NetworkIdentity identity)
                   && identity.connectionToClient != null
                   && sender == identity.connectionToClient;
        }

#endif
    }

    /// <summary>
    /// Central logging gate for runtime network diagnostics.
    /// Warnings and info logs are disabled by default to keep package code quiet in player builds.
    /// </summary>
    public static class NetworkDiagnostics
    {
        /// <summary>Enables gated info logs of package network code (off by default, reset on play start).</summary>
        public static bool RuntimeLogsEnabled { get; set; }
        /// <summary>Enables gated warnings of package network code (off by default, reset on play start).</summary>
        public static bool RuntimeWarningsEnabled { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            RuntimeLogsEnabled = false;
            RuntimeWarningsEnabled = false;
        }

        /// <summary>Logs when <see cref="RuntimeLogsEnabled"/> is on or <paramref name="force"/> is set.</summary>
        public static void Log(string message, Object context = null, bool force = false)
        {
            if (force || RuntimeLogsEnabled)
            {
                Debug.Log(message, context);
            }
        }

        /// <summary>Logs a warning when <see cref="RuntimeWarningsEnabled"/> is on or <paramref name="force"/> is set.</summary>
        public static void LogWarning(string message, Object context = null, bool force = false)
        {
            if (force || RuntimeWarningsEnabled)
            {
                Debug.LogWarning(message, context);
            }
        }

        /// <summary>Always logs an error.</summary>
        public static void LogError(string message, Object context = null)
        {
            Debug.LogError(message, context);
        }

        /// <summary>
        /// Logs an exception caught at a boundary the package must not let it cross (a listener invoked from
        /// Mirror's message loop). Always on: a swallowed exception is a bug somebody must be able to see.
        /// </summary>
        public static void LogException(System.Exception exception, Object context = null)
        {
            Debug.LogException(exception, context);
        }
    }
}
