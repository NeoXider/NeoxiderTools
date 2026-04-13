#if MIRROR
using Mirror;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>
    ///     Neoxider wrapper around Mirror's <c>NetworkManager</c>.
    ///     Provides Unity events for connection lifecycle and integrates with
    ///     Neo systems (scene flow, spawner, etc.).
    ///     <para>Without Mirror installed this component does nothing — it compiles
    ///     as an empty <see cref="MonoBehaviour"/> stub.</para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkManager.md")]
    [AddComponentMenu("Neoxider/Network/" + nameof(NeoNetworkManager))]
    public class NeoNetworkManager :
#if MIRROR
        NetworkManager
#else
        MonoBehaviour
#endif
    {
        [Header("Neo Events")]
        [Tooltip("Triggered when the server is started locally (Host or Dedicated server).")]
        [SerializeField] private UnityEvent _onServerStarted = new();
        [Tooltip("Triggered when the server is stopped.")]
        [SerializeField] private UnityEvent _onServerStopped = new();
        [Tooltip("Triggered when the client successfully connects to the server.")]
        [SerializeField] private UnityEvent _onClientConnected = new();
        [Tooltip("Triggered when the client disconnects from the server.")]
        [SerializeField] private UnityEvent _onClientDisconnected = new();

        /// <summary>Raised on the server when it starts listening.</summary>
        public UnityEvent OnServerStartedEvent => _onServerStarted;

        /// <summary>Raised on the server when it shuts down.</summary>
        public UnityEvent OnServerStoppedEvent => _onServerStopped;

        /// <summary>Raised on the client when it connects to a server.</summary>
        public UnityEvent OnClientConnectedEvent => _onClientConnected;

        /// <summary>Raised on the client when it disconnects from the server.</summary>
        public UnityEvent OnClientDisconnectedEvent => _onClientDisconnected;

#if MIRROR
        /// <summary>
        ///     Whether this instance is running as a server (host or dedicated).
        /// </summary>
        public bool IsServer => NetworkServer.active;

        /// <summary>
        ///     Whether this instance is running as a client.
        /// </summary>
        public bool IsClient => NetworkClient.active;

        /// <summary>
        ///     Whether this instance is a host (server + client).
        /// </summary>
        public bool IsHost => NetworkServer.active && NetworkClient.active;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _onServerStarted?.Invoke();
            Debug.Log("[NeoNetworkManager] Server started.");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _onServerStopped?.Invoke();
            Debug.Log("[NeoNetworkManager] Server stopped.");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            _onClientConnected?.Invoke();
            Debug.Log("[NeoNetworkManager] Client connected.");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            _onClientDisconnected?.Invoke();
            Debug.Log("[NeoNetworkManager] Client disconnected.");
        }

        /// <summary>
        ///     Convenience method: start as Host (server + client).
        /// </summary>
        public void StartAsHost()
        {
            StartHost();
        }

        /// <summary>
        ///     Convenience method: start as Client only.
        /// </summary>
        public void StartAsClient()
        {
            StartClient();
        }

        /// <summary>
        ///     Convenience method: start as Server only (headless/dedicated).
        /// </summary>
        public void StartAsServer()
        {
            StartServer();
        }

        /// <summary>
        ///     Stop whatever role is currently running.
        /// </summary>
        public void StopNetwork()
        {
            if (IsHost)
                StopHost();
            else if (IsServer)
                StopServer();
            else if (IsClient)
                StopClient();
        }
#else
        // Solo-mode stubs so user code compiles without Mirror.
        public bool IsServer => true;
        public bool IsClient => true;
        public bool IsHost => true;

        public void StartAsHost() => Debug.LogWarning("[NeoNetworkManager] Mirror is not installed. Running in solo mode.");
        public void StartAsClient() => Debug.LogWarning("[NeoNetworkManager] Mirror is not installed. Running in solo mode.");
        public void StartAsServer() => Debug.LogWarning("[NeoNetworkManager] Mirror is not installed. Running in solo mode.");
        public void StopNetwork() { }
#endif
    }
}
