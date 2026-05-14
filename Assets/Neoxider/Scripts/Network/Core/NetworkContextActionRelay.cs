using System;
using UnityEngine;
using UnityEngine.Events;

#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    public enum NetworkContextSourceMode
    {
        Self = 0,
        LocalPlayer = 1,
        Owner = 2,
        EventArgument = 3,
        ExplicitObject = 4
    }

    public enum NetworkContextRootMode
    {
        SourceObject = 0,
        NetworkIdentityInParents = 1,
        NeoNetworkPlayerInParents = 2
    }

    public enum NetworkContextTargetMode
    {
        Root = 0,
        ChildByName = 1,
        ChildByPath = 2,
        ChildByComponent = 3
    }

    public enum NetworkContextActionType
    {
        InvokeEventsOnly = 0,
        SetActive = 1,
        SendMessage = 2,
        InvokeComponentMethod = 3
    }

    public enum NetworkContextMethodArgumentMode
    {
        None = 0,
        Bool = 1,
        Float = 2,
        String = 3,
        TargetGameObject = 4,
        ContextGameObject = 5
    }

    /// <summary>
    /// Bridges ordinary UnityEvents into context-aware network actions.
    /// Use Trigger() from Button, InteractiveObject, NeoCondition or any UnityEvent.
    /// Use Trigger(Collider/GameObject) when the event already provides context.
    /// </summary>
    [NeoDoc("Network/NetworkContextActionRelay.md")]
    [AddComponentMenu("Neoxider/Network/Network Context Action Relay")]
    public class NetworkContextActionRelay : NeoNetworkComponent
    {
        [Header("Context")]
        [SerializeField] private NetworkContextSourceMode _contextSource = NetworkContextSourceMode.Self;
        [SerializeField] private NetworkContextRootMode _rootMode = NetworkContextRootMode.NetworkIdentityInParents;
        [SerializeField] private GameObject _explicitContext;

        [Header("Target")]
        [SerializeField] private NetworkContextTargetMode _targetMode = NetworkContextTargetMode.Root;
        [SerializeField] private string _targetName;
        [SerializeField] private string _targetPath;
        [SerializeField] private string _targetComponentType;
        [SerializeField] private bool _includeInactive = true;

        [Header("Action")]
        [SerializeField] private NetworkContextActionType _action = NetworkContextActionType.InvokeEventsOnly;
        [SerializeField] private bool _boolValue = true;
        [SerializeField] private float _floatValue = 1f;
        [SerializeField] private string _stringValue;
        [SerializeField] private string _messageName;
        [SerializeField] private string _methodComponentType;
        [SerializeField] private string _methodName;
        [SerializeField] private NetworkContextMethodArgumentMode _methodArgumentMode = NetworkContextMethodArgumentMode.None;

        [Header("Networking")]
        [SerializeField] private NetworkActionScope _scope = NetworkActionScope.AllClients;
        [SerializeField] private NetworkAuthorityMode _authorityMode = NetworkAuthorityMode.None;

        [Header("Events")]
        [SerializeField] private UnityEvent _onNetworkTriggered = new();
        [SerializeField] private GameObjectEvent _onContextResolved = new();
        [SerializeField] private GameObjectEvent _onTargetResolved = new();

#if MIRROR
        private const uint NoNetId = 0;

        /// <summary>
        ///     Server-side <see cref="NetworkContextActionMessage"/> handling must not debounce unrelated contexts:
        ///     several players (or tests) can fire <see cref="Trigger"/> within one tick; default Command spacing would drop those.
        /// </summary>
        protected override float NetworkRateLimit => 0f;
#endif

        public NetworkContextSourceMode ContextSource
        {
            get => _contextSource;
            set => _contextSource = value;
        }

        public NetworkContextRootMode RootMode
        {
            get => _rootMode;
            set => _rootMode = value;
        }

        public NetworkContextTargetMode TargetMode
        {
            get => _targetMode;
            set => _targetMode = value;
        }

        public NetworkContextActionType Action
        {
            get => _action;
            set => _action = value;
        }

        public NetworkActionScope Scope
        {
            get => _scope;
            set => _scope = value;
        }

        public NetworkAuthorityMode AuthorityMode
        {
            get => _authorityMode;
            set => _authorityMode = value;
        }

        public string TargetChildName
        {
            get => _targetName;
            set => _targetName = value;
        }

        public string TargetChildPath
        {
            get => _targetPath;
            set => _targetPath = value;
        }

        public string TargetComponentTypeName
        {
            get => _targetComponentType;
            set => _targetComponentType = value;
        }

        public bool IncludeInactive
        {
            get => _includeInactive;
            set => _includeInactive = value;
        }

        public bool ActionBoolValue
        {
            get => _boolValue;
            set => _boolValue = value;
        }

        public float ActionFloatValue
        {
            get => _floatValue;
            set => _floatValue = value;
        }

        public string SendMessageName
        {
            get => _messageName;
            set => _messageName = value;
        }

        public string ActionStringValue
        {
            get => _stringValue;
            set => _stringValue = value;
        }

        public string MethodComponentTypeName
        {
            get => _methodComponentType;
            set => _methodComponentType = value;
        }

        public string MethodName
        {
            get => _methodName;
            set => _methodName = value;
        }

        public NetworkContextMethodArgumentMode MethodArgumentMode
        {
            get => _methodArgumentMode;
            set => _methodArgumentMode = value;
        }

        public GameObject ExplicitContext
        {
            get => _explicitContext;
            set => _explicitContext = value;
        }

        public UnityEvent OnNetworkTriggered => _onNetworkTriggered;
        public GameObjectEvent OnContextResolved => _onContextResolved;
        public GameObjectEvent OnTargetResolved => _onTargetResolved;

#if MIRROR
        private void Awake()
        {
            EnsureMessageHandlers();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            EnsureMessageHandlers();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            EnsureMessageHandlers();
        }

        /// <summary>
        ///     Registers Mirror handlers for <see cref="NetworkContextActionMessage"/>.
        ///     Must run after <see cref="NetworkClient.Initialize"/> (see <see cref="NeoNetworkManager.OnStartServer"/> / <c>OnStartClient</c>);
        ///     relay <see cref="Awake"/> can run earlier and lose the slot when Mirror resets client handlers.
        /// </summary>
        public static void RegisterMirrorHandlers()
        {
            if (NetworkServer.active)
            {
                NetworkServer.ReplaceHandler<NetworkContextActionMessage>(OnServerMessage, false);
            }

            if (NetworkClient.active)
            {
                NetworkClient.ReplaceHandler<NetworkContextActionMessage>(OnClientMessage, false);
            }
        }
#endif

        /// <summary>Universal no-argument entry point for Button, InteractiveObject, NeoCondition and ordinary UnityEvents.</summary>
        public void Trigger()
        {
            TriggerFromSource(null);
        }

        public void TriggerSelf()
        {
            TriggerWithContext(gameObject);
        }

        public void TriggerLocalPlayer()
        {
            TriggerWithContext(GetLocalPlayerObject());
        }

        public void Trigger(Collider context)
        {
            TriggerFromSource(context != null ? context.gameObject : null);
        }

        public void Trigger(GameObject context)
        {
            TriggerFromSource(context);
        }

#if MIRROR
        public void Trigger(NetworkIdentity context)
        {
            TriggerFromSource(context != null ? context.gameObject : null);
        }
#endif

        private void TriggerFromSource(GameObject eventArgument)
        {
            GameObject context = ResolveContextSource(eventArgument);
            TriggerWithContext(context);
        }

        private void TriggerWithContext(GameObject context)
        {
            GameObject root = ResolveRoot(context);
            if (root == null)
            {
                Debug.LogWarning($"[NetworkContextActionRelay] Context root not found on '{name}'.", this);
                return;
            }

#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (!TryGetNetworkIdentity(root, out NetworkIdentity identity))
                {
                    Debug.LogWarning($"[NetworkContextActionRelay] Context root '{root.name}' has no NetworkIdentity.", root);
                    return;
                }

                uint contextNetId = identity.netId;
                NetworkContextActionMessage message = CreateMessage(contextNetId);
                if (message.relayNetId == NoNetId)
                {
                    Debug.LogWarning($"[NetworkContextActionRelay] Relay '{name}' must be on or under a spawned NetworkIdentity.", this);
                    return;
                }

                if (NeoNetworkState.IsClientOnly)
                {
                    EnsureMessageHandlers();
                    NetworkClient.Send(message);
                    return;
                }

                if (NeoNetworkState.IsServer)
                {
                    NetworkConnectionToClient senderConn = NetworkServer.localConnection;
                    if (TryGetNetworkIdentity(root, out NetworkIdentity contextIdentity) &&
                        contextIdentity.connectionToClient != null)
                    {
                        senderConn = contextIdentity.connectionToClient;
                    }

                    DispatchOnServer(message, senderConn);
                    return;
                }
            }
#endif

            ApplyResolved(root);
        }

        private GameObject ResolveContextSource(GameObject eventArgument)
        {
            switch (_contextSource)
            {
                case NetworkContextSourceMode.Self:
                    return gameObject;
                case NetworkContextSourceMode.LocalPlayer:
                    return GetLocalPlayerObject();
                case NetworkContextSourceMode.Owner:
                    return GetOwnerObject();
                case NetworkContextSourceMode.EventArgument:
                    return eventArgument;
                case NetworkContextSourceMode.ExplicitObject:
                    return _explicitContext;
                default:
                    return eventArgument != null ? eventArgument : gameObject;
            }
        }

        private GameObject ResolveRoot(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            switch (_rootMode)
            {
                case NetworkContextRootMode.SourceObject:
                    return source;
                case NetworkContextRootMode.NetworkIdentityInParents:
#if MIRROR
                    if (TryGetNetworkIdentity(source, out var identity))
                    {
                        return identity.gameObject;
                    }

                    return !isNetworked || !NeoNetworkState.IsNetworkActive ? source : null;
#else
                    return source;
#endif
                case NetworkContextRootMode.NeoNetworkPlayerInParents:
                    NeoNetworkPlayer player = source.GetComponentInParent<NeoNetworkPlayer>(_includeInactive);
                    return player != null ? player.gameObject : source;
                default:
                    return source;
            }
        }

        private GameObject ResolveTarget(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            switch (_targetMode)
            {
                case NetworkContextTargetMode.Root:
                    return root;
                case NetworkContextTargetMode.ChildByName:
                    return FindChildByName(root.transform, _targetName);
                case NetworkContextTargetMode.ChildByPath:
                    return FindChildByPath(root.transform, _targetPath);
                case NetworkContextTargetMode.ChildByComponent:
                    return FindChildByComponent(root, _targetComponentType);
                default:
                    return root;
            }
        }

        private void ApplyResolved(GameObject root)
        {
            GameObject target = ResolveTarget(root);
            if (target == null)
            {
                Debug.LogWarning($"[NetworkContextActionRelay] Target not found for root '{root.name}' on '{name}'.", this);
                return;
            }

            _onNetworkTriggered?.Invoke();
            _onContextResolved?.Invoke(root);
            _onTargetResolved?.Invoke(target);
            ApplyAction(root, target);
        }

        private void ApplyAction(GameObject context, GameObject target)
        {
            switch (_action)
            {
                case NetworkContextActionType.InvokeEventsOnly:
                    return;
                case NetworkContextActionType.SetActive:
                    target.SetActive(_boolValue);
                    return;
                case NetworkContextActionType.SendMessage:
                    if (!string.IsNullOrEmpty(_messageName))
                    {
                        target.SendMessage(_messageName, SendMessageOptions.DontRequireReceiver);
                    }
                    return;
                case NetworkContextActionType.InvokeComponentMethod:
                    TryInvokeConfiguredComponentMethod(target, context);
                    return;
            }
        }

        private static GameObject FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (string.Equals(root.name, childName, StringComparison.Ordinal))
            {
                return root.gameObject;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                GameObject found = FindChildByName(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindChildByPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            Transform found = root.Find(path);
            return found != null ? found.gameObject : null;
        }

        private GameObject FindChildByComponent(GameObject root, string componentTypeName)
        {
            Component component = FindComponentByTypeName(root, componentTypeName);
            if (component == null)
            {
                Debug.LogWarning($"[NetworkContextActionRelay] Component type '{componentTypeName}' not found.", this);
                return null;
            }

            return component.gameObject;
        }

        private bool TryInvokeConfiguredComponentMethod(GameObject target, GameObject context)
        {
            Component component = FindComponentByTypeName(target, _methodComponentType);
            if (component == null)
            {
                Debug.LogWarning($"[NetworkContextActionRelay] Component '{_methodComponentType}' not found on '{target.name}'.", target);
                return false;
            }

            object argument = GetConfiguredArgument(target, context);
            Type argumentType = argument?.GetType();
            var method = argumentType != null
                ? component.GetType().GetMethod(_methodName, new[] { argumentType })
                : component.GetType().GetMethod(_methodName, Type.EmptyTypes);

            if (method == null)
            {
                Debug.LogWarning($"[NetworkContextActionRelay] Method '{_methodName}' not found on '{component.GetType().Name}'.", component);
                return false;
            }

            method.Invoke(component, argumentType != null ? new[] { argument } : null);
            return true;
        }

        private object GetConfiguredArgument(GameObject target, GameObject context)
        {
            switch (_methodArgumentMode)
            {
                case NetworkContextMethodArgumentMode.Bool:
                    return _boolValue;
                case NetworkContextMethodArgumentMode.Float:
                    return _floatValue;
                case NetworkContextMethodArgumentMode.String:
                    return _stringValue;
                case NetworkContextMethodArgumentMode.TargetGameObject:
                    return target;
                case NetworkContextMethodArgumentMode.ContextGameObject:
                    return context;
                default:
                    return null;
            }
        }

        private Component FindComponentByTypeName(GameObject root, string componentTypeName)
        {
            Type componentType = ResolveType(componentTypeName);
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
            {
                return null;
            }

            Component direct = root.GetComponent(componentType);
            if (direct != null)
            {
                return direct;
            }

            return root.GetComponentInChildren(componentType, _includeInactive);
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private GameObject GetLocalPlayerObject()
        {
#if MIRROR
            if (NetworkClient.localPlayer != null)
            {
                return NetworkClient.localPlayer.gameObject;
            }

            if (NetworkServer.localConnection != null && NetworkServer.localConnection.identity != null)
            {
                return NetworkServer.localConnection.identity.gameObject;
            }
#endif
            return gameObject;
        }

        private GameObject GetOwnerObject()
        {
#if MIRROR
            if (TryGetComponent(out NetworkIdentity identity))
            {
                if (identity.connectionToClient != null && identity.connectionToClient.identity != null)
                {
                    return identity.connectionToClient.identity.gameObject;
                }

                if ((identity.isLocalPlayer || identity.isOwned) && NetworkClient.localPlayer != null)
                {
                    return NetworkClient.localPlayer.gameObject;
                }
            }
#endif
            return gameObject;
        }

#if MIRROR
        public struct NetworkContextActionMessage : NetworkMessage
        {
            public uint relayNetId;
            public uint contextNetId;
        }

        private static bool TryGetNetworkIdentity(GameObject source, out NetworkIdentity identity)
        {
            identity = null;
            if (source == null)
            {
                return false;
            }

            identity = source.GetComponentInParent<NetworkIdentity>(true);
            return identity != null;
        }

        private bool AuthorizedSender(NetworkConnectionToClient sender) =>
            NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode);

        private NetworkContextActionMessage CreateMessage(uint contextNetId)
        {
            uint relayNetId = NoNetId;
            if (TryGetComponent(out NetworkIdentity identity))
            {
                relayNetId = identity.netId;
            }

            if (relayNetId == NoNetId)
            {
                NetworkIdentity parentIdentity = GetComponentInParent<NetworkIdentity>(true);
                if (parentIdentity != null)
                {
                    relayNetId = parentIdentity.netId;
                }
            }

            return new NetworkContextActionMessage
            {
                relayNetId = relayNetId,
                contextNetId = contextNetId
            };
        }

        private void DispatchOnServer(NetworkContextActionMessage message, NetworkConnectionToClient sender)
        {
            if (!TryResolveNetworkObject(message.contextNetId, out GameObject root))
            {
                return;
            }

            if (_scope == NetworkActionScope.ServerOnly)
            {
                ApplyResolved(root);
                return;
            }

            bool skipHostLocalRpc = sender == NetworkServer.localConnection && NeoNetworkState.IsClient;
            if (_scope == NetworkActionScope.AllClients && (!NeoNetworkState.IsClient || skipHostLocalRpc))
            {
                ApplyResolved(root);
            }

            if (_scope == NetworkActionScope.OthersOnly)
            {
                SendToOthers(sender, message, ShouldSkipHostSender(sender));
                return;
            }

            BroadcastAllClientsApply(message.contextNetId, message, skipHostLocalRpc);
        }

        /// <summary>
        ///     Mirror <see cref="ClientRpc"/> uses <see cref="NetworkIdentity.observers"/> (same path as generated RPCs).
        ///     Raw <see cref="NetworkConnection.Send{T}"/> for custom messages can fail to reach every client in some setups;
        ///     when observers are not built yet, fall back to <see cref="SendToClients"/>.
        /// </summary>
        private void BroadcastAllClientsApply(uint contextNetId, NetworkContextActionMessage message, bool skipHostLocal)
        {
            if (netIdentity != null && netIdentity.observers != null && netIdentity.observers.Count > 0)
            {
                RpcApplyFromServer(contextNetId);
                return;
            }

            SendToClients(message, skipHostLocal);
        }

        [ClientRpc]
        private void RpcApplyFromServer(uint contextNetId)
        {
            if (!TryResolveNetworkObject(contextNetId, out GameObject root))
            {
                return;
            }

            ApplyResolved(root);
        }

        private static void EnsureMessageHandlers()
        {
            RegisterMirrorHandlers();
        }

        private static void OnServerMessage(NetworkConnectionToClient sender, NetworkContextActionMessage message)
        {
            if (!TryResolveRelay(message.relayNetId, out NetworkContextActionRelay relay))
            {
                return;
            }

            if (relay.RateLimitCheck()) return;
            if (!relay.AuthorizedSender(sender)) return;
            if (message.contextNetId == NoNetId) return;

            relay.DispatchOnServer(message, sender);
        }

        private static void OnClientMessage(NetworkContextActionMessage message)
        {
            if (!TryResolveRelay(message.relayNetId, out NetworkContextActionRelay relay))
            {
                return;
            }

            if (!TryResolveNetworkObject(message.contextNetId, out GameObject root))
            {
                return;
            }

            relay.ApplyResolved(root);
        }

        private static bool TryResolveRelay(uint relayNetId, out NetworkContextActionRelay relay)
        {
            relay = null;
            if (!TryResolveNetworkObject(relayNetId, out GameObject relayObject))
            {
                return false;
            }

            relay = relayObject.GetComponent<NetworkContextActionRelay>();
            if (relay == null)
            {
                relay = relayObject.GetComponentInChildren<NetworkContextActionRelay>(true);
            }

            return relay != null;
        }

        private void SendToClients(NetworkContextActionMessage message, bool skipHostLocal)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection == null)
                {
                    continue;
                }

                if (skipHostLocal && IsHostLocalConnection(connection))
                {
                    continue;
                }

                connection.Send(message);
            }
        }

        private void SendToOthers(NetworkConnectionToClient sender, NetworkContextActionMessage message, bool skipHostSender)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (IsSenderConnection(connection, sender, skipHostSender))
                {
                    continue;
                }

                connection.Send(message);
            }
        }

        private static bool TryResolveNetworkObject(uint netId, out GameObject result)
        {
            result = null;
            if (netId == NoNetId)
            {
                return false;
            }

            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity serverIdentity) && serverIdentity != null)
            {
                result = serverIdentity.gameObject;
                return true;
            }

            if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity clientIdentity) && clientIdentity != null)
            {
                result = clientIdentity.gameObject;
                return true;
            }

            NetworkIdentity[] identities = FindObjectsByType<NetworkIdentity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < identities.Length; i++)
            {
                if (identities[i] != null && identities[i].netId == netId)
                {
                    result = identities[i].gameObject;
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldSkipHostSender(NetworkConnectionToClient sender)
        {
            return NeoNetworkState.IsHost && (sender == null || sender == NetworkServer.localConnection);
        }

        private static bool IsSenderConnection(NetworkConnectionToClient connection, NetworkConnectionToClient sender, bool skipHostSender)
        {
            if (connection == null)
            {
                return true;
            }

            if (sender != null && connection.connectionId == sender.connectionId)
            {
                return true;
            }

            return skipHostSender &&
                   (connection == NetworkServer.localConnection ||
                    connection.connectionId == NetworkConnection.LocalConnectionId);
        }

        private static bool IsHostLocalConnection(NetworkConnectionToClient connection)
        {
            return connection == NetworkServer.localConnection ||
                   connection.connectionId == NetworkConnection.LocalConnectionId;
        }

        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
        }
#endif

        [Serializable]
        public class GameObjectEvent : UnityEvent<GameObject>
        {
        }
    }
}
