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

#if MIRROR
        [Header("Scene Player Template")]
        [Tooltip("Use a player object configured in the scene as the NoCode template instead of a prefab asset.")]
        [SerializeField] private bool _useScenePlayerTemplate;
        [Tooltip("Disabled scene object that contains NetworkIdentity and all NoCode references for the player.")]
        [SerializeField] private GameObject _scenePlayerTemplate;
        [Tooltip("Disable the scene template at runtime so only spawned network copies are active.")]
        [SerializeField] private bool _disableScenePlayerTemplate = true;
        [SerializeField, HideInInspector] private string _scenePlayerTemplateSpawnId;

        private uint _scenePlayerTemplateAssetId;
        private uint _registeredScenePlayerTemplateAssetId;
        private bool _scenePlayerTemplateSpawnHandlerRegistered;
#endif

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

        /// <summary>
        ///     Use a scene-authored player object as a template for spawned player copies.
        /// </summary>
        public bool UseScenePlayerTemplate
        {
            get => _useScenePlayerTemplate;
            set => _useScenePlayerTemplate = value;
        }

        /// <summary>
        ///     Scene object used as the player template when <see cref="UseScenePlayerTemplate"/> is enabled.
        /// </summary>
        public GameObject ScenePlayerTemplate
        {
            get => _scenePlayerTemplate;
            set => _scenePlayerTemplate = value;
        }

        /// <summary>
        ///     Stable id used by Mirror spawn handlers for scene-authored player templates.
        /// </summary>
        public string ScenePlayerTemplateSpawnId
        {
            get => _scenePlayerTemplateSpawnId;
            set
            {
                _scenePlayerTemplateSpawnId = value;
                _scenePlayerTemplateAssetId = 0;
            }
        }

        /// <summary>
        ///     Disable the original scene template at runtime.
        /// </summary>
        public bool DisableScenePlayerTemplate
        {
            get => _disableScenePlayerTemplate;
            set => _disableScenePlayerTemplate = value;
        }

        public override void Reset()
        {
            base.Reset();
            EnsureScenePlayerTemplateSpawnId();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            EnsureScenePlayerTemplateSpawnId();

            if (_scenePlayerTemplate != null && !_scenePlayerTemplate.TryGetComponent(out NetworkIdentity _))
                Debug.LogError("[NeoNetworkManager] Scene Player Template must have a NetworkIdentity.");
        }

        public override void Awake()
        {
            base.Awake();
            PrepareScenePlayerTemplate();
        }

        public override void Start()
        {
            base.Start();
            PrepareScenePlayerTemplate();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            PrepareScenePlayerTemplate();
            _onServerStarted?.Invoke();
            Debug.Log("[NeoNetworkManager] Server started.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            RegisterScenePlayerTemplateSpawnHandler();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _onServerStopped?.Invoke();
            Debug.Log("[NeoNetworkManager] Server stopped.");
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            UnregisterScenePlayerTemplateSpawnHandler();
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (TryCreateScenePlayer(conn, out GameObject player))
            {
                NetworkServer.AddPlayerForConnection(conn, player, ScenePlayerTemplateAssetId);
                return;
            }

            base.OnServerAddPlayer(conn);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            TryAddSceneTemplatePlayer();
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

        private uint ScenePlayerTemplateAssetId
        {
            get
            {
                if (_scenePlayerTemplateAssetId == 0)
                    _scenePlayerTemplateAssetId = CalculateStableAssetId(GetEffectiveScenePlayerTemplateSpawnId());

                return _scenePlayerTemplateAssetId;
            }
        }

        private void PrepareScenePlayerTemplate()
        {
            if (!_useScenePlayerTemplate)
                return;

            autoCreatePlayer = false;

            if (_scenePlayerTemplate == null || !_disableScenePlayerTemplate)
                return;

            _scenePlayerTemplate.SetActive(false);
        }

        private void TryAddSceneTemplatePlayer()
        {
            if (!_useScenePlayerTemplate || NetworkClient.localPlayer != null)
                return;

            if (!NetworkClient.ready)
                NetworkClient.Ready();

            NetworkClient.AddPlayer();
        }

        private bool TryCreateScenePlayer(NetworkConnectionToClient conn, out GameObject player)
        {
            player = null;

            if (!_useScenePlayerTemplate)
                return false;

            if (!IsScenePlayerTemplateValid())
                return false;

            Transform startPosition = GetStartPosition();
            Vector3 position = startPosition != null ? startPosition.position : _scenePlayerTemplate.transform.position;
            Quaternion rotation = startPosition != null ? startPosition.rotation : _scenePlayerTemplate.transform.rotation;

            player = Instantiate(_scenePlayerTemplate, position, rotation);
            if (player.TryGetComponent(out NetworkIdentity identity))
                identity.sceneId = 0;

            player.name = conn != null
                ? $"{_scenePlayerTemplate.name} (Player {conn.connectionId})"
                : $"{_scenePlayerTemplate.name} (Player)";
            player.SetActive(true);
            return true;
        }

        private void RegisterScenePlayerTemplateSpawnHandler()
        {
            if (!_useScenePlayerTemplate || !IsScenePlayerTemplateValid())
                return;

            uint assetId = ScenePlayerTemplateAssetId;
            if (_scenePlayerTemplateSpawnHandlerRegistered && _registeredScenePlayerTemplateAssetId == assetId)
                return;

            UnregisterScenePlayerTemplateSpawnHandler();
            NetworkClient.RegisterSpawnHandler(assetId, SpawnScenePlayerTemplate, UnspawnScenePlayerTemplate);
            _registeredScenePlayerTemplateAssetId = assetId;
            _scenePlayerTemplateSpawnHandlerRegistered = true;
        }

        private void UnregisterScenePlayerTemplateSpawnHandler()
        {
            if (!_scenePlayerTemplateSpawnHandlerRegistered)
                return;

            NetworkClient.UnregisterSpawnHandler(_registeredScenePlayerTemplateAssetId);
            _registeredScenePlayerTemplateAssetId = 0;
            _scenePlayerTemplateSpawnHandlerRegistered = false;
        }

        private GameObject SpawnScenePlayerTemplate(SpawnMessage message)
        {
            GameObject player = Instantiate(_scenePlayerTemplate, message.position, message.rotation);
            if (player.TryGetComponent(out NetworkIdentity identity))
                identity.sceneId = 0;

            player.transform.localScale = message.scale;
            player.name = $"{_scenePlayerTemplate.name} (Remote Player)";
            player.SetActive(true);
            return player;
        }

        private static void UnspawnScenePlayerTemplate(GameObject spawned)
        {
            Destroy(spawned);
        }

        private bool IsScenePlayerTemplateValid()
        {
            if (_scenePlayerTemplate == null)
            {
                Debug.LogError("[NeoNetworkManager] Scene Player Template is enabled, but no template object is assigned.");
                return false;
            }

            if (!_scenePlayerTemplate.TryGetComponent(out NetworkIdentity _))
            {
                Debug.LogError("[NeoNetworkManager] Scene Player Template must have a NetworkIdentity.");
                return false;
            }

            return true;
        }

        private string GetEffectiveScenePlayerTemplateSpawnId()
        {
            if (!string.IsNullOrWhiteSpace(_scenePlayerTemplateSpawnId))
                return _scenePlayerTemplateSpawnId;

            string scenePath = gameObject.scene.IsValid() ? gameObject.scene.path : string.Empty;
            string templateName = _scenePlayerTemplate != null ? _scenePlayerTemplate.name : "ScenePlayerTemplate";
            return $"{scenePath}/{name}/{templateName}";
        }

        private void EnsureScenePlayerTemplateSpawnId()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(_scenePlayerTemplateSpawnId))
                _scenePlayerTemplateSpawnId = System.Guid.NewGuid().ToString("N");
#endif
            _scenePlayerTemplateAssetId = 0;
        }

        private static uint CalculateStableAssetId(string value)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;

            uint hash = offset;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= prime;
            }

            return hash == 0 ? 1 : hash;
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
