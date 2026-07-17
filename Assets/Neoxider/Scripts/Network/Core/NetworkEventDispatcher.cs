using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Neo.Network;

#if MIRROR
using Mirror;
#endif

namespace Neo.Tools
{
    /// <summary>
    /// A universal No-Code network event dispatcher.
    /// Hook any local event (like UI Button or Physics collision) to CmdDispatchEvent().
    /// The Server will receive it, validate it, and send an RPC to all clients to trigger 'onNetworkEvent'.
    /// </summary>
    [NeoDoc("Tools/Network/NetworkEventDispatcher.md")]
    [AddComponentMenu("Neoxider/Tools/Network/Network Event Dispatcher")]
    public class NetworkEventDispatcher : NeoNetworkComponent
    {
        [Tooltip(
            "Who may trigger this event over the network. Default None lets NoCode scene objects work without ownership.")]
        [SerializeField]
        [FormerlySerializedAs("requiresAuthority")]
        private NetworkAuthorityMode _authorityMode = NetworkAuthorityMode.None;

        [Space]
        [Header("Global Replicated Event")]
        [Tooltip("This event fires identically on ALL connected clients and the server when dispatched.")]
        public UnityEvent onNetworkEvent = new();

        /// <summary>Manual NoCode authority policy. Defaults to None.</summary>
        public NetworkAuthorityMode AuthorityMode
        {
            get => _authorityMode;
            set => _authorityMode = value;
        }

        /// <summary>Compatibility wrapper for older code that used a boolean owner-only gate.</summary>
        [System.Obsolete("Use AuthorityMode instead.")]
        public bool requiresAuthority
        {
            get => _authorityMode == NetworkAuthorityMode.OwnerOnly;
            set => _authorityMode = value ? NetworkAuthorityMode.OwnerOnly : NetworkAuthorityMode.None;
        }

        /// <summary>
        /// Universal entry point. Call this from any local UnityEvent to broadcast to everyone.
        /// </summary>
        public void DispatchGlobalEvent()
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsServer)
                {
                    bool skipHostLocalRpc = NeoNetworkState.IsClient;
                    onNetworkEvent?.Invoke();
                    RpcDispatchEvent(skipHostLocalRpc);
                }
                else if (NeoNetworkState.IsClientOnly)
                {
                    CmdDispatchEvent();
                }

                return;
            }
#endif
            // WHY: Offline fallback
            onNetworkEvent?.Invoke();
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdDispatchEvent(NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck(sender))
            {
                return; // WHY: Too frequent — protects against Cmd spam amplified by the RPC broadcast
            }

            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode))
            {
                return;
            }

            if (isServerOnly)
            {
                onNetworkEvent?.Invoke();
            }

            RpcDispatchEvent(false);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcDispatchEvent(bool skipHostLocal)
        {
            if (skipHostLocal && NeoNetworkState.IsHost)
            {
                return;
            }

            onNetworkEvent?.Invoke();
        }
#endif

        [Space]
        [Header("Payload Events")]
        [Tooltip("Fired on all clients by DispatchGlobalInt(...).")]
        public UnityEvent<int> onNetworkIntEvent = new();

        [Tooltip("Fired on all clients by DispatchGlobalFloat(...).")]
        public UnityEvent<float> onNetworkFloatEvent = new();

        [Tooltip("Fired on all clients by DispatchGlobalString(...).")]
        public UnityEvent<string> onNetworkStringEvent = new();

        /// <summary>Broadcasts an int payload to everyone (fires <see cref="onNetworkIntEvent"/>).</summary>
        public void DispatchGlobalInt(int value)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsServer)
                {
                    onNetworkIntEvent?.Invoke(value);
                    RpcDispatchInt(NeoNetworkState.IsClient, value);
                }
                else if (NeoNetworkState.IsClientOnly)
                {
                    CmdDispatchInt(value);
                }

                return;
            }
#endif
            onNetworkIntEvent?.Invoke(value);
        }

        /// <summary>Broadcasts a float payload to everyone (fires <see cref="onNetworkFloatEvent"/>).</summary>
        public void DispatchGlobalFloat(float value)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsServer)
                {
                    onNetworkFloatEvent?.Invoke(value);
                    RpcDispatchFloat(NeoNetworkState.IsClient, value);
                }
                else if (NeoNetworkState.IsClientOnly)
                {
                    CmdDispatchFloat(value);
                }

                return;
            }
#endif
            onNetworkFloatEvent?.Invoke(value);
        }

        /// <summary>Broadcasts a string payload to everyone (fires <see cref="onNetworkStringEvent"/>).</summary>
        public void DispatchGlobalString(string value)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsServer)
                {
                    onNetworkStringEvent?.Invoke(value);
                    RpcDispatchString(NeoNetworkState.IsClient, value);
                }
                else if (NeoNetworkState.IsClientOnly)
                {
                    CmdDispatchString(value);
                }

                return;
            }
#endif
            onNetworkStringEvent?.Invoke(value);
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdDispatchInt(int value, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck(sender) || !NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode))
            {
                return;
            }

            if (isServerOnly)
            {
                onNetworkIntEvent?.Invoke(value);
            }

            RpcDispatchInt(false, value);
        }

        [Command(requiresAuthority = false)]
        private void CmdDispatchFloat(float value, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck(sender) || !NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode))
            {
                return;
            }

            if (isServerOnly)
            {
                onNetworkFloatEvent?.Invoke(value);
            }

            RpcDispatchFloat(false, value);
        }

        [Command(requiresAuthority = false)]
        private void CmdDispatchString(string value, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck(sender) || !NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode))
            {
                return;
            }

            if (isServerOnly)
            {
                onNetworkStringEvent?.Invoke(value);
            }

            RpcDispatchString(false, value);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcDispatchInt(bool skipHostLocal, int value)
        {
            if (skipHostLocal && NeoNetworkState.IsHost)
            {
                return;
            }

            onNetworkIntEvent?.Invoke(value);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcDispatchFloat(bool skipHostLocal, float value)
        {
            if (skipHostLocal && NeoNetworkState.IsHost)
            {
                return;
            }

            onNetworkFloatEvent?.Invoke(value);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcDispatchString(bool skipHostLocal, string value)
        {
            if (skipHostLocal && NeoNetworkState.IsHost)
            {
                return;
            }

            onNetworkStringEvent?.Invoke(value);
        }
#endif
    }
}
