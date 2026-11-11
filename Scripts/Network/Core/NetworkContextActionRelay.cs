using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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

    /// <summary>Where <see cref="NetworkContextActionRelay"/> takes the context object from.</summary>
    public enum NetworkContextSourceMode
    {
        /// <summary>The relay's own GameObject.</summary>
        Self = 0,

        /// <summary>The local player object (host player on a server).</summary>
        LocalPlayer = 1,

        /// <summary>The player object of the connection that owns the relay.</summary>
        Owner = 2,

        /// <summary>The object passed to <c>Trigger(Collider)</c> / <c>Trigger(GameObject)</c>.</summary>
        EventArgument = 3,

        /// <summary>The assigned <see cref="NetworkContextActionRelay.ExplicitContext"/>.</summary>
        ExplicitObject = 4
    }

    /// <summary>How the context object is climbed to the networked root that identifies it on every peer.</summary>
    public enum NetworkContextRootMode
    {
        /// <summary>The context object itself.</summary>
        SourceObject = 0,

        /// <summary>The nearest <c>NetworkIdentity</c> in the parents.</summary>
        NetworkIdentityInParents = 1,

        /// <summary>The nearest <see cref="NeoNetworkPlayer"/> in the parents.</summary>
        NeoNetworkPlayerInParents = 2
    }

    /// <summary>Where the action target is found below the resolved root.</summary>
    public enum NetworkContextTargetMode
    {
        /// <summary>The root itself.</summary>
        Root = 0,

        /// <summary>The first descendant with the configured name (depth-first).</summary>
        ChildByName = 1,

        /// <summary>A <c>Transform.Find</c> path below the root.</summary>
        ChildByPath = 2,

        /// <summary>The first object below the root carrying the configured component type.</summary>
        ChildByComponent = 3
    }

    /// <summary>What <see cref="NetworkContextActionRelay"/> does with the resolved target.</summary>
    public enum NetworkContextActionType
    {
        /// <summary>Only raise the relay's UnityEvents.</summary>
        InvokeEventsOnly = 0,

        /// <summary><c>SetActive</c> with the configured bool.</summary>
        SetActive = 1,

        /// <summary><c>SendMessage</c> with the configured method name.</summary>
        SendMessage = 2,

        /// <summary>Call a public method of a component on the target via reflection.</summary>
        InvokeComponentMethod = 3
    }

    /// <summary>Argument passed to the method of <see cref="NetworkContextActionType.InvokeComponentMethod"/>.</summary>
    public enum NetworkContextMethodArgumentMode
    {
        /// <summary>Parameterless method.</summary>
        None = 0,

        /// <summary>The configured bool.</summary>
        Bool = 1,

        /// <summary>The configured float.</summary>
        Float = 2,

        /// <summary>The configured string (null allowed).</summary>
        String = 3,

        /// <summary>The resolved target GameObject.</summary>
        TargetGameObject = 4,

        /// <summary>The resolved context root GameObject.</summary>
        ContextGameObject = 5
    }

    /// <summary>
    /// Bridges ordinary UnityEvents into context-aware network actions.
    /// Use Trigger() from Button, InteractiveObject, NeoCondition or any UnityEvent.
    /// Use Trigger(Collider/GameObject) when the event already provides context.
    /// </summary>
    [NeoDoc("Network/NetworkContextActionRelay.md")]
    [CreateFromMenu("Neoxider/Network/Network Context Action Relay")]
    [AddComponentMenu("Neoxider/Network/Network Context Action Relay")]
    public class NetworkContextActionRelay : NeoNetworkComponent
    {
        [Header("Context")] [SerializeField]
        private NetworkContextSourceMode _contextSource = NetworkContextSourceMode.Self;

        [SerializeField] private NetworkContextRootMode _rootMode = NetworkContextRootMode.NetworkIdentityInParents;
        [SerializeField] private GameObject _explicitContext;

        [Header("Target")] [SerializeField]
        private NetworkContextTargetMode _targetMode = NetworkContextTargetMode.Root;

        [SerializeField] private string _targetName;
        [SerializeField] private string _targetPath;
        [SerializeField] private string _targetComponentType;
        [SerializeField] private bool _includeInactive = true;

        [Header("Action")] [SerializeField]
        private NetworkContextActionType _action = NetworkContextActionType.InvokeEventsOnly;

        [SerializeField] private bool _boolValue = true;
        [SerializeField] private float _floatValue = 1f;
        [SerializeField] private string _stringValue;
        [SerializeField] private string _messageName;
        [SerializeField] private string _methodComponentType;
        [SerializeField] private string _methodName;

        [SerializeField]
        private NetworkContextMethodArgumentMode _methodArgumentMode = NetworkContextMethodArgumentMode.None;

        [Header("Networking")] [SerializeField]
        private NetworkActionScope _scope = NetworkActionScope.AllClients;

        [SerializeField] private NetworkAuthorityMode _authorityMode = NetworkAuthorityMode.None;

        [Tooltip("When ON, the relay fires only when the resolved context belongs to the local player. " +
                 "Recommended for physics triggers: every client runs physics on every replicated collider, " +
                 "so without this filter the same trigger fires on every client and the server gets duplicate messages. " +
                 "With this ON, only the player who really walked into the trigger sends the action.")]
        [SerializeField]
        private bool _triggerOnlyForLocalContext = true;

        [Header("Diagnostics")]
        [Tooltip(
            "Print trace logs at every step (Trigger → Send → Server → Client). Use to debug why a relay does not replicate to other clients.")]
        [SerializeField]
        private bool _verboseLogging;

        [Tooltip(
            "Print static Mirror handler registration logs. Usually needed only while diagnosing transport setup.")]
        [SerializeField]
        private bool _verboseRegistrationLogging;

        private static bool s_verboseRegistrationLogging;

        /// <summary>
        ///     Resolved component types by configured name. Without it every apply of a
        ///     ChildByComponent / InvokeComponentMethod relay rescanned every loaded assembly on every peer.
        /// </summary>
        private static readonly Dictionary<string, Type> s_resolvedTypes = new(StringComparer.Ordinal);

        /// <summary>
        ///     Unresolved type names with the <see cref="s_assemblyLoads"/> value of their last scan. A miss is
        ///     scanned again only after another assembly has loaded (a mod or hot-update DLL loaded at runtime).
        /// </summary>
        private static readonly Dictionary<string, int> s_unresolvedTypes = new(StringComparer.Ordinal);

        private static int s_assemblyLoads;
        private static bool s_assemblyLoadHooked;

        private Type _cachedMethodOwner;
        private Type _cachedMethodArgumentType;
        private string _cachedMethodName;
        private MethodInfo _cachedMethod;
        private object[] _singleArgument;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticDiagnosticsState()
        {
            s_verboseRegistrationLogging = false;
            s_resolvedTypes.Clear();
            s_unresolvedTypes.Clear();
#if MIRROR
            s_enabledRelays.Clear();
            ResetServerIngressState();
            UnregisterMirrorHandlers();
#endif
        }

#if MIRROR
        /// <summary>
        ///     Small per-connection token bucket for server-side <see cref="NetworkContextActionMessage"/> ingress.
        ///     Keyed by live connection objects (never by id or address); refilled with unscaled double time.
        /// </summary>
        private struct ServerIngressBucket
        {
            public double LastRefillTime;
            public double Tokens;
        }

        private const double ServerIngressBurstTokens = 5.0;
        private const double ServerIngressRefillPerSecond = 5.0;

        /// <summary>
        ///     Relays currently enabled. Mirror handlers stay registered only while at least one entry is live:
        ///     idempotent <see cref="Awake"/>/<see cref="OnEnable"/> tracking, <see cref="OnDisable"/>/<see cref="OnDestroy"/>
        ///     removal. Stale (destroyed) entries are pruned on every mutation.
        /// </summary>
        private static readonly HashSet<NetworkContextActionRelay> s_enabledRelays =
            new HashSet<NetworkContextActionRelay>();

        private static readonly Dictionary<NetworkConnectionToClient, ServerIngressBucket> s_serverIngressBuckets =
            new Dictionary<NetworkConnectionToClient, ServerIngressBucket>();
#endif

        [Header("Editor Helpers")]
        [Tooltip("Optional reference GameObject used by the custom inspector to build component/method dropdowns. " +
                 "It is NOT used at runtime — runtime always resolves the target via Context + Target Mode. " +
                 "Drag a representative object (e.g., the player prefab/template) to enable the dropdown pickers.")]
        [SerializeField]
        private GameObject _editorPreviewTarget;

        [Header("Events")] [SerializeField] private UnityEvent _onNetworkTriggered = new();
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

        /// <summary>Where the context object comes from.</summary>
        public NetworkContextSourceMode ContextSource
        {
            get => _contextSource;
            set => _contextSource = value;
        }

        /// <summary>How the context is climbed to its networked root.</summary>
        public NetworkContextRootMode RootMode
        {
            get => _rootMode;
            set => _rootMode = value;
        }

        /// <summary>Where the action target is found below the root.</summary>
        public NetworkContextTargetMode TargetMode
        {
            get => _targetMode;
            set => _targetMode = value;
        }

        /// <summary>What is done with the resolved target.</summary>
        public NetworkContextActionType Action
        {
            get => _action;
            set => _action = value;
        }

        /// <summary>Who applies the action: all clients, the server only, or everyone except the sender.</summary>
        public NetworkActionScope Scope
        {
            get => _scope;
            set => _scope = value;
        }

        /// <summary>Which remote senders may trigger the relay.</summary>
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

        /// <summary>Descendant name for <see cref="NetworkContextTargetMode.ChildByName"/>.</summary>
        public string TargetChildName
        {
            get => _targetName;
            set => _targetName = value;
        }

        /// <summary><c>Transform.Find</c> path for <see cref="NetworkContextTargetMode.ChildByPath"/>.</summary>
        public string TargetChildPath
        {
            get => _targetPath;
            set => _targetPath = value;
        }

        /// <summary>Full or assembly-qualified component type for <see cref="NetworkContextTargetMode.ChildByComponent"/>.</summary>
        public string TargetComponentTypeName
        {
            get => _targetComponentType;
            set => _targetComponentType = value;
        }

        /// <summary>Include inactive children when searching for the target or component.</summary>
        public bool IncludeInactive
        {
            get => _includeInactive;
            set => _includeInactive = value;
        }

        /// <summary>Value for <see cref="NetworkContextActionType.SetActive"/> and the Bool method argument.</summary>
        public bool ActionBoolValue
        {
            get => _boolValue;
            set => _boolValue = value;
        }

        /// <summary>Value of the Float method argument.</summary>
        public float ActionFloatValue
        {
            get => _floatValue;
            set => _floatValue = value;
        }

        /// <summary>Method name for <see cref="NetworkContextActionType.SendMessage"/>.</summary>
        public string SendMessageName
        {
            get => _messageName;
            set => _messageName = value;
        }

        /// <summary>Value of the String method argument.</summary>
        public string ActionStringValue
        {
            get => _stringValue;
            set => _stringValue = value;
        }

        /// <summary>Full or assembly-qualified component type whose method is invoked.</summary>
        public string MethodComponentTypeName
        {
            get => _methodComponentType;
            set => _methodComponentType = value;
        }

        /// <summary>Public method invoked by <see cref="NetworkContextActionType.InvokeComponentMethod"/>.</summary>
        public string MethodName
        {
            get => _methodName;
            set => _methodName = value;
        }

        /// <summary>Selects the method signature and its argument.</summary>
        public NetworkContextMethodArgumentMode MethodArgumentMode
        {
            get => _methodArgumentMode;
            set => _methodArgumentMode = value;
        }

        /// <summary>Context object for <see cref="NetworkContextSourceMode.ExplicitObject"/>.</summary>
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

        /// <summary>Raised on every peer that applies the action, after server validation.</summary>
        public UnityEvent OnNetworkTriggered => _onNetworkTriggered;

        /// <summary>Raised with the resolved context root (for example the entering player).</summary>
        public GameObjectEvent OnContextResolved => _onContextResolved;

        /// <summary>Raised with the resolved target.</summary>
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

        /// <summary>Enable to log Mirror handler registration (transport setup diagnostics).</summary>
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
            if (!_verboseLogging)
            {
                return;
            }

            NetworkDiagnostics.Log($"[NetworkContextActionRelay] {message}", this, true);
        }

#if MIRROR
        private void Awake()
        {
            s_verboseRegistrationLogging |= _verboseRegistrationLogging || _verboseLogging;
            // WHY: Awake also runs for a disabled component; OnEnable tracks it once it is enabled.
            if (isActiveAndEnabled)
            {
                TrackEnabledRelay();
            }
        }

        private void OnEnable()
        {
            TrackEnabledRelay();
        }

        private void OnDisable()
        {
            UntrackEnabledRelay();
        }

        private void OnDestroy()
        {
            UntrackEnabledRelay();
        }

        private void TrackEnabledRelay()
        {
            PruneNullRelays();
            s_enabledRelays.Add(this);
            EnsureMessageHandlers();
        }

        private void UntrackEnabledRelay()
        {
            s_enabledRelays.Remove(this);
            PruneNullRelays();
            if (s_enabledRelays.Count == 0)
            {
                ResetServerIngressState();
                UnregisterMirrorHandlers();
            }
        }

        private static void PruneNullRelays()
        {
            s_enabledRelays.RemoveWhere(relay => relay == null);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (isActiveAndEnabled)
            {
                TrackEnabledRelay();
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isActiveAndEnabled)
            {
                TrackEnabledRelay();
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ResetServerIngressState();
        }

        /// <summary>
        ///     Clears per-connection ingress budgets on session reset or last-relay removal.
        /// </summary>
        public static void ResetServerIngressState()
        {
            s_serverIngressBuckets.Clear();
        }

        internal static void ForgetServerIngressPeer(NetworkConnectionToClient peer)
        {
            if (peer == null)
            {
                return;
            }

            s_serverIngressBuckets.Remove(peer);
        }

        /// <summary>
        ///     Registers Mirror handlers for <see cref="NetworkContextActionMessage"/>.
        ///     Must run after <see cref="NetworkClient.Initialize"/> (see <see cref="NeoNetworkManager.OnStartServer"/> / <c>OnStartClient</c>);
        ///     relay <see cref="Awake"/> can run earlier and lose the slot when Mirror resets client handlers.
        ///     <para>Handlers exist only while at least one relay is enabled.</para>
        /// </summary>
        public static void RegisterMirrorHandlers()
        {
            PruneNullRelays();
            if (s_enabledRelays.Count == 0)
            {
                ResetServerIngressState();
                UnregisterMirrorHandlers();
                return;
            }

            ushort msgId = NetworkMessageId<NetworkContextActionMessage>.Id;
            bool didServer = false;
            bool didClient = false;

            if (NetworkServer.active)
            {
                NetworkServer.ReplaceHandler<NetworkContextActionMessage>(OnServerMessage, true);
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

        private static void UnregisterMirrorHandlers()
        {
            NetworkServer.UnregisterHandler<NetworkContextActionMessage>();
            NetworkClient.UnregisterHandler<NetworkContextActionMessage>();
        }
#endif

        /// <summary>Universal no-argument entry point for Button, InteractiveObject, NeoCondition and ordinary UnityEvents.</summary>
        public void Trigger()
        {
            TriggerFromSource(null);
        }

        /// <summary>Applies the action with this object as the context.</summary>
        public void TriggerSelf()
        {
            TriggerWithContext(gameObject);
        }

        /// <summary>Applies the action with the local player as the context.</summary>
        public void TriggerLocalPlayer()
        {
            TriggerWithContext(GetLocalPlayerObject());
        }

        /// <summary>Entry point for physics events; the collider is the event argument.</summary>
        public void Trigger(Collider context)
        {
            TriggerFromSource(context != null ? context.gameObject : null);
        }

        /// <summary>Entry point for events that pass a GameObject; it is the event argument.</summary>
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
            // WHY: De-duplication on the event-argument side - every client's local physics fires OnTriggerEnter
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
                    if (_verboseLogging)
                    {
                        LogVerbose(
                            $"Skipping trigger on '{name}': event argument '{eventArgument.name}' (netId={eventIdentity.netId}) is not the local player and TriggerOnlyForLocalContext is ON.");
                    }

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
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning($"[NetworkContextActionRelay] Context root not found on '{name}'.",
                        this);
                }

                return;
            }

#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (!TryGetNetworkIdentity(root, out NetworkIdentity identity))
                {
                    if (NetworkDiagnostics.RuntimeWarningsEnabled)
                    {
                        NetworkDiagnostics.LogWarning(
                            $"[NetworkContextActionRelay] Context root '{root.name}' has no NetworkIdentity.", root);
                    }

                    return;
                }

                uint contextNetId = identity.netId;
                NetworkContextActionMessage message = CreateMessage(contextNetId);
                if (message.relayNetId == NoNetId)
                {
                    if (NetworkDiagnostics.RuntimeWarningsEnabled)
                    {
                        NetworkDiagnostics.LogWarning(
                            $"[NetworkContextActionRelay] Relay '{name}' must be on or under a spawned NetworkIdentity.",
                            this);
                    }

                    return;
                }

                if (_verboseLogging)
                {
                    LogVerbose(
                        $"Trigger on '{name}': contextNetId={contextNetId} ('{root.name}'), relayNetId={message.relayNetId}, IsClientOnly={NeoNetworkState.IsClientOnly}, IsServer={NeoNetworkState.IsServer}");
                }

                if (NeoNetworkState.IsClientOnly)
                {
                    EnsureMessageHandlers();
                    NetworkClient.Send(message);
                    if (_verboseLogging)
                    {
                        LogVerbose(
                            $"Client → Server: NetworkClient.Send dispatched (relayNetId={message.relayNetId}, contextNetId={contextNetId})");
                    }

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
                    if (TryGetNetworkIdentity(source, out NetworkIdentity identity))
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
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Target not found for root '{root.name}' on '{name}'.", this);
                }

                return;
            }

            if (_verboseLogging)
            {
                string rootId = NeoDiagnostics.StableId(root);
                string targetId = NeoDiagnostics.StableId(target);
                LogVerbose(
                    $"ApplyResolved on '{name}': root='{root.name}' (id={rootId}) → target='{target.name}' (id={targetId}, was active={target.activeSelf}) → action={_action}");
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
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Component type '{componentTypeName}' not found.", this);
                }

                return null;
            }

            return component.gameObject;
        }

        private bool TryInvokeConfiguredComponentMethod(GameObject target, GameObject context)
        {
            if (string.IsNullOrEmpty(_methodName))
            {
                return false;
            }

            Component component = FindComponentByTypeName(target, _methodComponentType);
            if (component == null)
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Component '{_methodComponentType}' not found on '{target.name}'.",
                        target);
                }

                return false;
            }

            object argument = GetConfiguredArgument(target, context);
            // WHY: resolve the configured signature even when a reference argument is null.
            Type argumentType = _methodArgumentMode switch
            {
                NetworkContextMethodArgumentMode.Bool => typeof(bool),
                NetworkContextMethodArgumentMode.Float => typeof(float),
                NetworkContextMethodArgumentMode.String => typeof(string),
                NetworkContextMethodArgumentMode.TargetGameObject => typeof(GameObject),
                NetworkContextMethodArgumentMode.ContextGameObject => typeof(GameObject),
                _ => null
            };
            MethodInfo method = ResolveConfiguredMethod(component.GetType(), argumentType);

            if (method == null)
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Method '{_methodName}' not found on '{component.GetType().Name}'.",
                        component);
                }

                return false;
            }

            if (argumentType == null)
            {
                method.Invoke(component, null);
                return true;
            }

            object[] arguments = _singleArgument ??= new object[1];
            arguments[0] = argument;
            try
            {
                method.Invoke(component, arguments);
            }
            finally
            {
                arguments[0] = null;
            }

            return true;
        }

        /// <summary>
        ///     Returns the configured method of <paramref name="componentType"/>, reusing the last lookup while the
        ///     component type, method name and argument signature stay the same.
        /// </summary>
        private MethodInfo ResolveConfiguredMethod(Type componentType, Type argumentType)
        {
            if (_cachedMethod != null
                && ReferenceEquals(_cachedMethodOwner, componentType)
                && ReferenceEquals(_cachedMethodArgumentType, argumentType)
                && string.Equals(_cachedMethodName, _methodName, StringComparison.Ordinal))
            {
                return _cachedMethod;
            }

            MethodInfo method = argumentType != null
                ? componentType.GetMethod(_methodName, new[] { argumentType })
                : componentType.GetMethod(_methodName, Type.EmptyTypes);
            _cachedMethodOwner = componentType;
            _cachedMethodArgumentType = argumentType;
            _cachedMethodName = _methodName;
            _cachedMethod = method;
            return method;
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

        /// <summary>
        ///     Resolves a configured type name (full or assembly-qualified) once per name; later calls are a
        ///     dictionary lookup without allocation. An unresolved name is scanned again only after a new
        ///     assembly has loaded, so a type from a DLL loaded at runtime is still found.
        /// </summary>
        internal static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            if (s_resolvedTypes.TryGetValue(typeName, out Type cached))
            {
                return cached;
            }

            HookAssemblyLoads();
            // WHY: read before the scan, so an assembly that loads while it runs triggers another scan later.
            int assemblyLoads = Volatile.Read(ref s_assemblyLoads);
            if (s_unresolvedTypes.TryGetValue(typeName, out int scannedAt) && scannedAt == assemblyLoads)
            {
                return null;
            }

            Type type = Type.GetType(typeName);
            if (type == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length && type == null; i++)
                {
                    type = assemblies[i].GetType(typeName);
                }
            }

            if (type == null)
            {
                s_unresolvedTypes[typeName] = assemblyLoads;
                return null;
            }

            s_unresolvedTypes.Remove(typeName);
            s_resolvedTypes[typeName] = type;
            return type;
        }

        private static void HookAssemblyLoads()
        {
            if (s_assemblyLoadHooked)
            {
                return;
            }

            // WHY: the flag is not reset at play start; the AppDomain (and this subscription) survives a play
            // session when domain reload is disabled, and a second subscription would only count loads twice.
            s_assemblyLoadHooked = true;
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
        }

        // WHY: may run on a loader thread, so it only bumps a counter; the caches stay main-thread only.
        private static void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            Interlocked.Increment(ref s_assemblyLoads);
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

        private bool AuthorizedSender(NetworkConnectionToClient sender)
        {
            return NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode);
        }

        /// <summary>
        ///     True only for a live, authenticated, Ready connection that owns its player object:
        ///     the sender must be the current <see cref="NetworkServer.connections"/> reference,
        ///     Ready, with <c>identity.connectionToClient == sender</c>. Checked before any target lookup.
        /// </summary>
        private static bool IsLivePlayerConnection(NetworkConnectionToClient sender)
        {
            if (sender == null || !NetworkServer.active)
            {
                return false;
            }

            if (!sender.isAuthenticated || !sender.isReady)
            {
                return false;
            }

            NetworkIdentity playerIdentity = sender.identity;
            if (playerIdentity == null || playerIdentity.connectionToClient != sender)
            {
                return false;
            }

            if (NetworkServer.connections == null ||
                !NetworkServer.connections.TryGetValue(sender.connectionId,
                    out NetworkConnectionToClient live) ||
                !ReferenceEquals(live, sender))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Small named per-connection token bucket (unscaled double time). Returns false when throttled.
        ///     Runs before even the relay lookup so invalid netIds cannot be used to probe or spam.
        /// </summary>
        private static bool CheckServerIngressBudget(NetworkConnectionToClient sender)
        {
            double now = Time.unscaledTimeAsDouble;
            if (!s_serverIngressBuckets.TryGetValue(sender, out ServerIngressBucket bucket))
            {
                PruneDepartedIngressPeers();
                bucket = new ServerIngressBucket { LastRefillTime = now, Tokens = ServerIngressBurstTokens };
            }
            else if (now > bucket.LastRefillTime)
            {
                double refilled = bucket.Tokens + (now - bucket.LastRefillTime) * ServerIngressRefillPerSecond;
                bucket.Tokens = Math.Min(ServerIngressBurstTokens, refilled);
                bucket.LastRefillTime = now;
            }

            if (bucket.Tokens < 1.0)
            {
                s_serverIngressBuckets[sender] = bucket;
                return false;
            }

            bucket.Tokens -= 1.0;
            s_serverIngressBuckets[sender] = bucket;
            return true;
        }

        /// <summary>
        ///     Drops budgets for departed peers. Runs only when a new bucket is added, never per message,
        ///     so state cannot grow with historical disconnected peers.
        /// </summary>
        private static void PruneDepartedIngressPeers()
        {
            if (s_serverIngressBuckets.Count == 0)
            {
                return;
            }

            List<NetworkConnectionToClient> stale = null;
            foreach (NetworkConnectionToClient peer in s_serverIngressBuckets.Keys)
            {
                if (IsPeerDeparted(peer))
                {
                    if (stale == null)
                    {
                        stale = new List<NetworkConnectionToClient>();
                    }

                    stale.Add(peer);
                }
            }

            if (stale == null)
            {
                return;
            }

            for (int i = 0; i < stale.Count; i++)
            {
                s_serverIngressBuckets.Remove(stale[i]);
            }
        }

        private static bool IsPeerDeparted(NetworkConnectionToClient peer)
        {
            if (peer == null || NetworkServer.connections == null)
            {
                return true;
            }

            return !NetworkServer.connections.TryGetValue(peer.connectionId, out NetworkConnectionToClient live)
                   || !ReferenceEquals(live, peer);
        }

        /// <summary>
        ///     Prevents a client from firing a relay with someone else's context: unowned (shared scene) contexts
        ///     stay usable, the sender's own context is usable, the host-local sender keeps full Trigger behavior,
        ///     anything owned by another connection is rejected.
        /// </summary>
        private static bool IsSenderContextOwner(GameObject contextRoot, NetworkConnectionToClient sender)
        {
            if (sender == null || contextRoot == null)
            {
                return false;
            }

            if (sender == NetworkServer.localConnection)
            {
                return true;
            }

            if (!contextRoot.TryGetComponent(out NetworkIdentity contextIdentity) || contextIdentity == null)
            {
                return false;
            }

            NetworkConnectionToClient owner = contextIdentity.connectionToClient;
            if (owner == null)
            {
                return true;
            }

            return ReferenceEquals(owner, sender);
        }

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
        ///     and sends a <see cref="NetworkContextActionMessage"/> to the relay's observers, the connections
        ///     that have the relay's object spawned (see <see cref="SendToClients"/>).
        ///     <para>The host's local connection is skipped because the server applies locally, avoiding a
        ///     double-apply. Remote observers are sent to before the local apply, so a throwing host-side listener
        ///     cannot leave them out of sync.</para>
        /// </summary>
        private void DispatchOnServer(NetworkContextActionMessage message, NetworkConnectionToClient sender)
        {
            if (!TryResolveServerObject(message.contextNetId, out GameObject root))
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] '{name}': could not resolve contextNetId={message.contextNetId} on server.",
                        this);
                }

                return;
            }

            if (_verboseLogging)
            {
                int connectionCount = NetworkServer.connections != null ? NetworkServer.connections.Count : 0;
                LogVerbose(
                    $"DispatchOnServer on '{name}': scope={_scope}, sender={(sender != null ? sender.connectionId.ToString() : "null")}, host={NeoNetworkState.IsHost}, connections={connectionCount}");
            }

            if (_scope == NetworkActionScope.ServerOnly)
            {
                ApplyResolved(root);
                return;
            }

            bool isHost = NeoNetworkState.IsHost;
            bool senderIsHostLocal = IsHostLocalConnection(sender);

            if (_scope == NetworkActionScope.OthersOnly)
            {
                // WHY: Sender is excluded by definition. On host, the host's local connection is the sender
                // (or stand-in for it) when the trigger fired on the host — so skip it as well.
                int sent = SendToOthers(sender, message, senderIsHostLocal);
                if (_verboseLogging)
                {
                    LogVerbose($"OthersOnly broadcast complete on '{name}': sent to {sent} connection(s)");
                }

                return;
            }

            // WHY: AllClients - apply once on the host's client view (via the server-side apply) and broadcast to remotes.
            // A dedicated server only relays to clients; it has no local client view to apply. Remotes are sent to
            // first: an exception from a host listener must not skip them.
            if (isHost)
            {
                int sent = SendToClients(message, true);
                if (_verboseLogging)
                {
                    LogVerbose(
                        $"AllClients broadcast on '{name}' (host): sent to {sent} remote connection(s), applying locally");
                }

                ApplyResolved(root);
            }
            else
            {
                // WHY: Dedicated server - do not call ApplyResolved (no client view here) — just relay to all clients.
                int sent = SendToClients(message, false);
                if (_verboseLogging)
                {
                    LogVerbose(
                        $"AllClients broadcast complete on '{name}' (dedicated): sent to {sent} connection(s)");
                }
            }
        }

        private static void EnsureMessageHandlers()
        {
            RegisterMirrorHandlers();
        }

        private static void OnServerMessage(NetworkConnectionToClient sender, NetworkContextActionMessage message)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            if (message.contextNetId == NoNetId)
            {
                return;
            }

            // WHY: Sender liveness and ingress budget run before ANY target lookup — even an invalid
            // relayNetId must not cause spawned-dict or scene work on behalf of a stranger.
            if (!IsLivePlayerConnection(sender))
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        "[NetworkContextActionRelay] Server: rejected message from a non-ready or unknown connection.");
                }

                return;
            }

            if (!CheckServerIngressBudget(sender))
            {
                return;
            }

            if (!TryResolveRelay(message.relayNetId, message.relayComponentIndex, true,
                    out NetworkContextActionRelay relay) ||
                relay == null || !relay.isActiveAndEnabled || !relay.isNetworked)
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Server: relay for netId={message.relayNetId} component={message.relayComponentIndex} not found or disabled. (Did the trigger spawn yet?)");
                }

                return;
            }

            if (!TryResolveServerObject(message.contextNetId, out GameObject contextRoot))
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Server: contextNetId={message.contextNetId} not spawned for relay '{relay.name}'.",
                        relay);
                }

                return;
            }

            if (!relay.AuthorizedSender(sender))
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Server: sender {sender.connectionId} not authorized for relay '{relay.name}'.",
                        relay);
                }

                return;
            }

            if (!IsSenderContextOwner(contextRoot, sender))
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Server: sender {sender.connectionId} does not own context '{contextRoot.name}' for relay '{relay.name}'.",
                        relay);
                }

                return;
            }

            if (relay.RateLimitCheck())
            {
                return;
            }

            if (relay._verboseLogging)
            {
                relay.LogVerbose(
                    $"OnServerMessage on '{relay.name}#{message.relayComponentIndex}': from connId={sender.connectionId}, relayNetId={message.relayNetId}, contextNetId={message.contextNetId}");
            }

            try
            {
                relay.DispatchOnServer(message, sender);
            }
            catch (Exception exception)
            {
                // WHY: Mirror disconnects the sending peer when a message handler throws. A bug in a server-side
                // gameplay listener must not kick the player whose valid message triggered it.
                LogListenerException(exception, relay);
            }
        }

        private static void LogListenerException(Exception exception, NetworkContextActionRelay relay)
        {
            Exception reported = exception is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : exception;
            NetworkDiagnostics.LogException(reported, relay);
        }

        private static void OnClientMessage(NetworkContextActionMessage message)
        {
            if (!TryResolveRelay(message.relayNetId, message.relayComponentIndex, false,
                    out NetworkContextActionRelay relay) ||
                relay == null || !relay.isActiveAndEnabled || !relay.isNetworked)
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Client: relay for netId={message.relayNetId} component={message.relayComponentIndex} not found locally or disabled.");
                }

                return;
            }

            if (relay._verboseLogging)
            {
                relay.LogVerbose(
                    $"Client RECEIVED: relayNetId={message.relayNetId}, componentIndex={message.relayComponentIndex}, contextNetId={message.contextNetId}");
            }

            if (!TryResolveClientObject(message.contextNetId, out GameObject root))
            {
                if (NetworkDiagnostics.RuntimeWarningsEnabled)
                {
                    NetworkDiagnostics.LogWarning(
                        $"[NetworkContextActionRelay] Client: contextNetId={message.contextNetId} not spawned locally on '{relay.name}'.",
                        relay);
                }

                return;
            }

            if (relay._verboseLogging)
            {
                relay.LogVerbose(
                    $"OnClientMessage on '{relay.name}#{message.relayComponentIndex}': applying with relayNetId={message.relayNetId}, contextNetId={message.contextNetId} ('{root.name}')");
            }

            try
            {
                relay.ApplyResolved(root);
            }
            catch (Exception exception)
            {
                // WHY: an exception escaping a client message handler makes Mirror drop the whole connection.
                LogListenerException(exception, relay);
            }
        }

        /// <summary>
        ///     Resolves the EXACT relay on the spawned NetworkIdentity by <paramref name="componentIndex"/>.
        ///     Multiple relays can be attached to one NetworkIdentity (e.g. "pickup self" + "bonus on player"
        ///     on the same trigger cube) — without the component index we'd always pick the first one and
        ///     the wrong action would fire on the wrong context.
        ///     <para>There is intentionally no fallback: an out-of-range or mismatched index resolves to nothing
        ///     instead of silently mapping to a different action.</para>
        /// </summary>
        private static bool TryResolveRelay(uint relayNetId, byte componentIndex, bool serverSide,
            out NetworkContextActionRelay relay)
        {
            relay = null;
            if (!TryResolveRelayObject(relayNetId, serverSide, out GameObject relayObject))
            {
                return false;
            }

            // WHY: Preferred path - index into NetworkIdentity.NetworkBehaviours — same ordering on every peer
            // because Mirror sorts NetworkBehaviours by Component order at spawn.
            if (!relayObject.TryGetComponent(out NetworkIdentity identity) ||
                identity == null ||
                identity.NetworkBehaviours == null ||
                componentIndex >= identity.NetworkBehaviours.Length)
            {
                return false;
            }

            relay = identity.NetworkBehaviours[componentIndex] as NetworkContextActionRelay;
            return relay != null;
        }

        private static bool TryResolveServerObject(uint netId, out GameObject result)
        {
            result = null;
            if (netId == NoNetId || !NetworkServer.active)
            {
                return false;
            }

            if (!NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity serverIdentity) ||
                serverIdentity == null)
            {
                return false;
            }

            result = serverIdentity.gameObject;
            return true;
        }

        private static bool TryResolveClientObject(uint netId, out GameObject result)
        {
            result = null;
            if (netId == NoNetId || !NetworkClient.active)
            {
                return false;
            }

            if (!NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity clientIdentity) ||
                clientIdentity == null)
            {
                return false;
            }

            result = clientIdentity.gameObject;
            return true;
        }

        private static bool TryResolveRelayObject(uint netId, bool serverSide, out GameObject result)
        {
            if (serverSide)
            {
                return TryResolveServerObject(netId, out result);
            }

            return TryResolveClientObject(netId, out result);
        }

        /// <summary>
        ///     Sends to the observers of the relay's <see cref="NetworkIdentity"/>: exactly the connections Mirror
        ///     sent that object's spawn message to, spectators without a player included.
        /// </summary>
        /// <remarks>
        ///     Any other connection has no relay to apply the action, and often no handler for the message: a client
        ///     that is still loading, or one whose relay object is hidden by interest management (hiding a scene
        ///     object disables it, and the last disabled relay unregisters the client handler). Mirror disconnects a
        ///     client that receives a message id it has no handler for.
        /// </remarks>
        private int SendToClients(NetworkContextActionMessage message, bool skipHostLocal)
        {
            Dictionary<int, NetworkConnectionToClient> observers = GetRelayObservers(message.relayNetId);
            if (observers == null)
            {
                return 0;
            }

            int sent = 0;
            foreach (NetworkConnectionToClient connection in observers.Values)
            {
                if (connection == null || (skipHostLocal && IsHostLocalConnection(connection)))
                {
                    continue;
                }

                connection.Send(message);
                sent++;
            }

            return sent;
        }

        /// <summary>Same audience as <see cref="SendToClients"/>, minus the sender.</summary>
        private int SendToOthers(NetworkConnectionToClient sender, NetworkContextActionMessage message,
            bool skipHostSender)
        {
            Dictionary<int, NetworkConnectionToClient> observers = GetRelayObservers(message.relayNetId);
            if (observers == null)
            {
                return 0;
            }

            int sent = 0;
            foreach (NetworkConnectionToClient connection in observers.Values)
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

        private static Dictionary<int, NetworkConnectionToClient> GetRelayObservers(uint relayNetId)
        {
            if (relayNetId == NoNetId ||
                !NetworkServer.spawned.TryGetValue(relayNetId, out NetworkIdentity relayIdentity) ||
                relayIdentity == null)
            {
                return null;
            }

            return relayIdentity.observers;
        }

        private static bool IsSenderConnection(NetworkConnectionToClient connection, NetworkConnectionToClient sender,
            bool skipHostSender)
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
            if (connection == null)
            {
                return false;
            }

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

        /// <summary>UnityEvent carrying a GameObject (serializable for the inspector).</summary>
        [Serializable]
        public class GameObjectEvent : UnityEvent<GameObject>
        {
        }
    }
}
