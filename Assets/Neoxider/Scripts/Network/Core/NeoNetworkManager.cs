#if MIRROR
using Mirror;
using System.Reflection;
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
        private static readonly FieldInfo NetworkIdentityHasSpawnedField =
            typeof(NetworkIdentity).GetField("hasSpawned", BindingFlags.NonPublic | BindingFlags.Instance);
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
            ApplyScenePlayerTemplateMode();
            base.OnValidate();
            EnsureScenePlayerTemplateSpawnId();

            if (_scenePlayerTemplate != null && !_scenePlayerTemplate.TryGetComponent(out NetworkIdentity _))
                Debug.LogError("[NeoNetworkManager] Scene Player Template must have a NetworkIdentity.");
        }

        public override void Awake()
        {
            PrepareScenePlayerTemplate();
            base.Awake();
        }

        public override void Start()
        {
            PrepareScenePlayerTemplate();
            base.Start();
        }

        public new void StartHost()
        {
            PrepareScenePlayerTemplate();
            RegisterScenePlayerTemplateSpawnHandler();
            base.StartHost();
            DisableScenePlayerTemplateInstance();
        }

        public new void StartServer()
        {
            PrepareScenePlayerTemplate();
            base.StartServer();
            DisableScenePlayerTemplateInstance();
        }

        public new void StartClient()
        {
            PrepareScenePlayerTemplate();
            RegisterScenePlayerTemplateSpawnHandler();
            base.StartClient();
        }

        public new void StartClient(System.Uri uri)
        {
            PrepareScenePlayerTemplate();
            RegisterScenePlayerTemplateSpawnHandler();
            base.StartClient(uri);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            PrepareScenePlayerTemplate();
            DisableScenePlayerTemplateInstance();
            NetworkContextActionRelay.RegisterMirrorHandlers();
            _onServerStarted?.Invoke();
            Debug.Log("[NeoNetworkManager] Server started.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkContextActionRelay.RegisterMirrorHandlers();
            RegisterScenePlayerTemplateSpawnHandler();
            if (!NetworkServer.active)
                DisableScenePlayerTemplateInstance();
        }

        public override void OnStartHost()
        {
            base.OnStartHost();
            NetworkContextActionRelay.RegisterMirrorHandlers();
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
            ((NeoNetworkManager)this).StartHost();
        }

        /// <summary>
        ///     Convenience method: start as Client only.
        /// </summary>
        public void StartAsClient()
        {
            ((NeoNetworkManager)this).StartClient();
        }

        /// <summary>
        ///     Convenience method: start as Server only (headless/dedicated).
        /// </summary>
        public void StartAsServer()
        {
            ((NeoNetworkManager)this).StartServer();
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
            NormalizeScenePlayerTemplateMode();

            if (!_useScenePlayerTemplate)
                return;

            ApplyScenePlayerTemplateMode();

            if (_scenePlayerTemplate == null || !_disableScenePlayerTemplate)
                return;

            DisableScenePlayerTemplateInstance();
        }

        private void DisableScenePlayerTemplateInstance()
        {
            if (!_useScenePlayerTemplate || !_disableScenePlayerTemplate || _scenePlayerTemplate == null)
                return;

            if (_scenePlayerTemplate.activeSelf)
                _scenePlayerTemplate.SetActive(false);
        }

        private void NormalizeScenePlayerTemplateMode()
        {
            if (_useScenePlayerTemplate)
                return;

            if (playerPrefab == null)
                return;

            if (!playerPrefab.TryGetComponent(out NetworkIdentity identity) || identity.sceneId == 0)
                return;

            _useScenePlayerTemplate = true;
            _scenePlayerTemplate = playerPrefab;
            Debug.LogWarning(
                "[NeoNetworkManager] Player Prefab references a scene object. Switching to Scene Player Template mode automatically.",
                this);
        }

        private void ApplyScenePlayerTemplateMode()
        {
            if (!_useScenePlayerTemplate)
                return;

            autoCreatePlayer = false;
            playerPrefab = null;
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

            player = InstantiateScenePlayerTemplate(position, rotation);
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
            GameObject player = InstantiateScenePlayerTemplate(message.position, message.rotation);
            player.transform.localScale = message.scale;
            player.name = $"{_scenePlayerTemplate.name} (Remote Player)";
            player.SetActive(true);
            return player;
        }

        private GameObject InstantiateScenePlayerTemplate(Vector3 position, Quaternion rotation)
        {
            NetworkIdentity[] templateIdentities = _scenePlayerTemplate.GetComponentsInChildren<NetworkIdentity>(true);
            ulong[] originalSceneIds = new ulong[templateIdentities.Length];
            bool[] originalHasSpawned = new bool[templateIdentities.Length];

            for (int i = 0; i < templateIdentities.Length; i++)
            {
                originalSceneIds[i] = templateIdentities[i].sceneId;
                originalHasSpawned[i] = GetHasSpawned(templateIdentities[i]);
                templateIdentities[i].sceneId = 0;
                SetHasSpawned(templateIdentities[i], false);
            }

            GameObject player;
            try
            {
                player = Instantiate(_scenePlayerTemplate, position, rotation);
            }
            finally
            {
                for (int i = 0; i < templateIdentities.Length; i++)
                {
                    templateIdentities[i].sceneId = originalSceneIds[i];
                    SetHasSpawned(templateIdentities[i], originalHasSpawned[i]);
                }
            }

            ClearSceneIds(player);
            return player;
        }

        private static bool GetHasSpawned(NetworkIdentity identity)
        {
            return NetworkIdentityHasSpawnedField != null &&
                   (bool)NetworkIdentityHasSpawnedField.GetValue(identity);
        }

        private static void SetHasSpawned(NetworkIdentity identity, bool value)
        {
            NetworkIdentityHasSpawnedField?.SetValue(identity, value);
        }

        private static void ClearSceneIds(GameObject target)
        {
            NetworkIdentity[] identities = target.GetComponentsInChildren<NetworkIdentity>(true);
            for (int i = 0; i < identities.Length; i++)
            {
                identities[i].sceneId = 0;
            }
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
