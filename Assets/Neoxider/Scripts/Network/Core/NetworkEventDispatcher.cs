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
        [Tooltip("Who may trigger this event over the network. Default None lets NoCode scene objects work without ownership.")]
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
            // Offline fallback
            onNetworkEvent?.Invoke();
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdDispatchEvent(NetworkConnectionToClient sender = null)
        {
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode))
            {
                return; // Unauthorized
            }

            // Execute on dedicated server
            if (isServerOnly)
            {
                onNetworkEvent?.Invoke();
            }

            // Sync to all clients
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
    }
}
