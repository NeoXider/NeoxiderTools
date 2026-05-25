using System;
using UnityEngine;
using UnityEngine.Events;

#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
#if MIRROR
    /// <summary>
    ///     Mirror <see cref="NetworkMessage"/> carrying a relay action from one peer to another.
    ///     Defined at namespace scope (not nested inside <see cref="NetworkContextActionRelay"/>)
    ///     so Mirror's weaver reliably generates Read/Write extensions for it.
    ///     <para><see cref="relayComponentIndex"/> disambiguates multiple <see cref="NetworkContextActionRelay"/>s
    ///     attached to the same <see cref="NetworkIdentity"/> (e.g., "pickup self" + "bonus on player" on one trigger cube).</para>
    /// </summary>
    public struct NetworkContextActionMessage : NetworkMessage
    {
        public uint relayNetId;
        public byte relayComponentIndex;
        public uint contextNetId;
    }
#endif

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

        [Tooltip("When ON, the relay fires only when the resolved context belongs to the local player. " +
                 "Recommended for physics triggers: every client runs physics on every replicated collider, " +
                 "so without this filter the same trigger fires on every client and the server gets duplicate messages. " +
                 "With this ON, only the player who really walked into the trigger sends the action.")]
        [SerializeField] private bool _triggerOnlyForLocalContext = true;

        [Header("Diagnostics")]
        [Tooltip("Print trace logs at every step (Trigger → Send → Server → Client). Use to debug why a relay does not replicate to other clients.")]
        [SerializeField] private bool _verboseLogging;
        [Tooltip("Print static Mirror handler registration logs. Usually needed only while diagnosing transport setup.")]
        [SerializeField] private bool _verboseRegistrationLogging;

        private static bool s_verboseRegistrationLogging;

        [Header("Editor Helpers")]
        [Tooltip("Optional reference GameObject used by the custom inspector to build component/method dropdowns. " +
                 "It is NOT used at runtime — runtime always resolves the target via Context + Target Mode. " +
                 "Drag a representative object (e.g., the player prefab/template) to enable the dropdown pickers.")]
        [SerializeField] private GameObject _editorPreviewTarget;

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

        /// <summary>
        ///     When true (default), <see cref="Trigger(Collider)"/>/<see cref="Trigger(GameObject)"/> ignore
        ///     contexts whose <see cref="NetworkIdentity"/> is not owned by the local player. Prevents
        ///     duplicate dispatches when every client's local physics fires <see cref="OnTriggerEnter"/>
        ///     for replicated colliders.
        /// </summary>
        public bool TriggerOnlyForLocalContext
        {
            get => _triggerOnlyForLocalContext;
            set => _triggerOnlyForLocalContext = value;
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

        /// <summary>
        ///     Editor-only reference object used to build dropdowns for component / method pickers
        ///     in the custom inspector. Never read at runtime.
        /// </summary>
        public GameObject EditorPreviewTarget
        {
            get => _editorPreviewTarget;
            set => _editorPreviewTarget = value;
        }

        public UnityEvent OnNetworkTriggered => _onNetworkTriggered;
        public GameObjectEvent OnContextResolved => _onContextResolved;
        public GameObjectEvent OnTargetResolved => _onTargetResolved;

        /// <summary>Enable to print step-by-step trace logs (Trigger → Send → Receive → Apply).</summary>
        public bool VerboseLogging
        {
            get => _verboseLogging;
            set
            {
                _verboseLogging = value;
                s_verboseRegistrationLogging = value || _verboseRegistrationLogging;
            }
        }

        public bool VerboseRegistrationLogging
        {
            get => _verboseRegistrationLogging;
            set
            {
                _verboseRegistrationLogging = value;
                s_verboseRegistrationLogging = value || _verboseLogging;
            }
        }

        private void LogVerbose(string message)
        {
            if (!_verboseLogging) return;
            NetworkDiagnostics.Log($"[NetworkContextActionRelay] {message}", this, true);
        }

#if MIRROR
        private void Awake()
        {
            s_verboseRegistrationLogging |= _verboseRegistrationLogging || _verboseLogging;
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
            ushort msgId = Mirror.NetworkMessageId<NetworkContextActionMessage>.Id;
            bool didServer = false;
            bool didClient = false;

            if (NetworkServer.active)
            {
                NetworkServer.ReplaceHandler<NetworkContextActionMessage>(OnServerMessage, false);
                didServer = true;
            }

            if (NetworkClient.active)
            {
                NetworkClient.ReplaceHandler<NetworkContextActionMessage>(OnClientMessage, false);
                didClient = true;
            }

            if (s_verboseRegistrationLogging)
            {
                NetworkDiagnostics.Log(
                    $"[NetworkContextActionRelay] RegisterMirrorHandlers: msgId={msgId}, server={didServer}, client={didClient}",
                    force: true);
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
#if MIRROR
            // De-duplication on the event-argument side: every client's local physics fires OnTriggerEnter
            // for every replicated collider, so a single player walking into a trigger produces N messages
            // from N clients. With this filter on, only the client that actually owns the entering collider
            // dispatches; remote clients ignore the duplicate event and wait for the server's broadcast.
            // We check the EVENT ARGUMENT (the entering collider) rather than the resolved context, so the
            // filter still works when ContextSource = Self (e.g. shared-object pickup pattern).
            if (_triggerOnlyForLocalContext && eventArgument != null &&
                isNetworked && NeoNetworkState.IsNetworkActive && NeoNetworkState.IsClient)
            {
                NetworkIdentity eventIdentity = eventArgument.GetComponentInParent<NetworkIdentity>(true);
                if (eventIdentity != null && !eventIdentity.isLocalPlayer && !eventIdentity.isOwned)
                {
                    LogVerbose(
                        $"Skipping trigger on '{name}': event argument '{eventArgument.name}' (netId={eventIdentity.netId}) is not the local player and TriggerOnlyForLocalContext is ON.");
                    return;
                }
            }
#endif

            GameObject context = ResolveContextSource(eventArgument);
            TriggerWithContext(context);
        }

        private void TriggerWithContext(GameObject context)
        {
            GameObject root = ResolveRoot(context);
            if (root == null)
            {
                NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Context root not found on '{name}'.", this);
                return;
            }

#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (!TryGetNetworkIdentity(root, out NetworkIdentity identity))
                {
                    NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Context root '{root.name}' has no NetworkIdentity.", root);
                    return;
                }

                uint contextNetId = identity.netId;
                NetworkContextActionMessage message = CreateMessage(contextNetId);
                if (message.relayNetId == NoNetId)
                {
                    NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Relay '{name}' must be on or under a spawned NetworkIdentity.", this);
                    return;
                }

                LogVerbose(
                    $"Trigger on '{name}': contextNetId={contextNetId} ('{root.name}'), relayNetId={message.relayNetId}, IsClientOnly={NeoNetworkState.IsClientOnly}, IsServer={NeoNetworkState.IsServer}");

                if (NeoNetworkState.IsClientOnly)
                {
                    EnsureMessageHandlers();
                    NetworkClient.Send(message);
                    LogVerbose($"Client → Server: NetworkClient.Send dispatched (relayNetId={message.relayNetId}, contextNetId={contextNetId})");
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
                NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Target not found for root '{root.name}' on '{name}'.", this);
                return;
            }

            LogVerbose(
                $"ApplyResolved on '{name}': root='{root.name}' (id={root.GetInstanceID()}) → target='{target.name}' (id={target.GetInstanceID()}, was active={target.activeSelf}) → action={_action}");

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
                NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Component type '{componentTypeName}' not found.", this);
                return null;
            }

            return component.gameObject;
        }

        private bool TryInvokeConfiguredComponentMethod(GameObject target, GameObject context)
        {
            Component component = FindComponentByTypeName(target, _methodComponentType);
            if (component == null)
            {
                NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Component '{_methodComponentType}' not found on '{target.name}'.", target);
                return false;
            }

            object argument = GetConfiguredArgument(target, context);
            Type argumentType = argument?.GetType();
            var method = argumentType != null
                ? component.GetType().GetMethod(_methodName, new[] { argumentType })
                : component.GetType().GetMethod(_methodName, Type.EmptyTypes);

            if (method == null)
            {
                NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Method '{_methodName}' not found on '{component.GetType().Name}'.", component);
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
                relayComponentIndex = (byte)ComponentIndex,
                contextNetId = contextNetId
            };
        }

        /// <summary>
        ///     Server-side dispatch: applies the action authoritatively (when meaningful for the local instance)
        ///     and broadcasts a <see cref="NetworkContextActionMessage"/> to the remaining clients.
        ///
        ///     The original implementation tried to deliver the per-client effect via Mirror's <c>[ClientRpc]</c>,
        ///     which is observer-driven: the RPC reaches every connection that observes the trigger's
        ///     <see cref="NetworkIdentity"/>. In practice this is fragile for triggers that live on scene
        ///     <see cref="NetworkIdentity"/> objects — the observer list can be empty mid-spawn or be filtered by
        ///     AOI/interest management. The result was a "host sees it, remote doesn't" bug.
        ///
        ///     This version always uses the direct <see cref="NetworkConnection.Send{T}"/> path so every
        ///     connected client receives the action regardless of the relay's observer state. The host's
        ///     local connection is skipped because the server already applied locally, avoiding a double-apply.
        /// </summary>
        private void DispatchOnServer(NetworkContextActionMessage message, NetworkConnectionToClient sender)
        {
            if (!TryResolveNetworkObject(message.contextNetId, out GameObject root))
            {
                NetworkDiagnostics.LogWarning(
                    $"[NetworkContextActionRelay] '{name}': could not resolve contextNetId={message.contextNetId} on server.",
                    this);
                return;
            }

            int connectionCount = NetworkServer.connections != null ? NetworkServer.connections.Count : 0;
            LogVerbose(
                $"DispatchOnServer on '{name}': scope={_scope}, sender={(sender != null ? sender.connectionId.ToString() : "null")}, host={NeoNetworkState.IsHost}, connections={connectionCount}");

            if (_scope == NetworkActionScope.ServerOnly)
            {
                ApplyResolved(root);
                return;
            }

            bool isHost = NeoNetworkState.IsHost;
            bool senderIsHostLocal = IsHostLocalConnection(sender);

            if (_scope == NetworkActionScope.OthersOnly)
            {
                // Sender is excluded by definition. On host, the host's local connection is the sender
                // (or stand-in for it) when the trigger fired on the host — so skip it as well.
                int sent = SendToOthers(sender, message, skipHostSender: senderIsHostLocal);
                LogVerbose($"OthersOnly broadcast complete on '{name}': sent to {sent} connection(s)");
                return;
            }

            // AllClients: apply once on the host's client view (via the server-side apply) and broadcast to remotes.
            // On a dedicated server we still apply server-side for parity, but it does not display anywhere.
            if (isHost)
            {
                ApplyResolved(root);
                int sent = SendToClients(message, skipHostLocal: true);
                LogVerbose($"AllClients broadcast complete on '{name}' (host): applied locally + sent to {sent} remote connection(s)");
            }
            else
            {
                // Dedicated server: do not call ApplyResolved (no client view here) — just relay to all clients.
                int sent = SendToClients(message, skipHostLocal: false);
                LogVerbose($"AllClients broadcast complete on '{name}' (dedicated): sent to {sent} connection(s)");
            }
        }

        private static void EnsureMessageHandlers()
        {
            RegisterMirrorHandlers();
        }

        private static void OnServerMessage(NetworkConnectionToClient sender, NetworkContextActionMessage message)
        {
            if (!TryResolveRelay(message.relayNetId, message.relayComponentIndex, out NetworkContextActionRelay relay))
            {
                NetworkDiagnostics.LogWarning(
                    $"[NetworkContextActionRelay] Server: relay for netId={message.relayNetId} component={message.relayComponentIndex} not found. (Did the trigger spawn yet?)");
                return;
            }

            if (relay.RateLimitCheck()) return;
            if (!relay.AuthorizedSender(sender))
            {
                NetworkDiagnostics.LogWarning(
                    $"[NetworkContextActionRelay] Server: sender {sender?.connectionId} not authorized for relay '{relay.name}'.");
                return;
            }

            if (message.contextNetId == NoNetId) return;

            relay.LogVerbose(
                $"OnServerMessage on '{relay.name}#{message.relayComponentIndex}': from connId={(sender != null ? sender.connectionId.ToString() : "null")}, relayNetId={message.relayNetId}, contextNetId={message.contextNetId}");
            relay.DispatchOnServer(message, sender);
        }

        private static void OnClientMessage(NetworkContextActionMessage message)
        {
            if (!TryResolveRelay(message.relayNetId, message.relayComponentIndex, out NetworkContextActionRelay relay))
            {
                NetworkDiagnostics.LogWarning(
                    $"[NetworkContextActionRelay] Client: relay for netId={message.relayNetId} component={message.relayComponentIndex} not found locally.");
                return;
            }

            relay.LogVerbose(
                $"Client RECEIVED: relayNetId={message.relayNetId}, componentIndex={message.relayComponentIndex}, contextNetId={message.contextNetId}");

            if (!TryResolveNetworkObject(message.contextNetId, out GameObject root))
            {
                NetworkDiagnostics.LogWarning(
                    $"[NetworkContextActionRelay] Client: contextNetId={message.contextNetId} not spawned locally on '{relay.name}'.");
                return;
            }

            relay.LogVerbose(
                $"OnClientMessage on '{relay.name}#{message.relayComponentIndex}': applying with relayNetId={message.relayNetId}, contextNetId={message.contextNetId} ('{root.name}')");
            relay.ApplyResolved(root);
        }

        /// <summary>
        ///     Resolves the EXACT relay on the spawned NetworkIdentity by <paramref name="componentIndex"/>.
        ///     Multiple relays can be attached to one NetworkIdentity (e.g. "pickup self" + "bonus on player"
        ///     on the same trigger cube) — without the component index we'd always pick the first one and
        ///     the wrong action would fire on the wrong context.
        /// </summary>
        private static bool TryResolveRelay(uint relayNetId, byte componentIndex, out NetworkContextActionRelay relay)
        {
            relay = null;
            if (!TryResolveNetworkObject(relayNetId, out GameObject relayObject))
            {
                return false;
            }

            // Preferred path: index into NetworkIdentity.NetworkBehaviours — same ordering on every peer
            // because Mirror sorts NetworkBehaviours by Component order at spawn.
            if (relayObject.TryGetComponent(out NetworkIdentity identity) &&
                identity.NetworkBehaviours != null &&
                componentIndex < identity.NetworkBehaviours.Length)
            {
                relay = identity.NetworkBehaviours[componentIndex] as NetworkContextActionRelay;
                if (relay != null)
                {
                    return true;
                }
            }

            // Fallback for legacy messages without a valid index or relays sitting on a child NetworkIdentity.
            relay = relayObject.GetComponent<NetworkContextActionRelay>();
            if (relay == null)
            {
                relay = relayObject.GetComponentInChildren<NetworkContextActionRelay>(true);
            }

            return relay != null;
        }

        private int SendToClients(NetworkContextActionMessage message, bool skipHostLocal)
        {
            int sent = 0;
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
                sent++;
            }

            return sent;
        }

        private int SendToOthers(NetworkConnectionToClient sender, NetworkContextActionMessage message, bool skipHostSender)
        {
            int sent = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (IsSenderConnection(connection, sender, skipHostSender))
                {
                    continue;
                }

                connection.Send(message);
                sent++;
            }

            return sent;
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
            s_verboseRegistrationLogging = _verboseRegistrationLogging || _verboseLogging;
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
