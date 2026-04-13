using UnityEngine;
using UnityEngine.Events;
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
    public class NetworkEventDispatcher : 
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        [Tooltip("If true, requires the caller to have network authority over this object.")]
        public bool requiresAuthority = false;

        [Space]
        [Header("Global Replicated Event")]
        [Tooltip("This event fires identically on ALL connected clients and the server when dispatched.")]
        public UnityEvent onNetworkEvent;

        /// <summary>
        /// Universal entry point. Call this from any local UnityEvent to broadcast to everyone.
        /// </summary>
        public void DispatchGlobalEvent()
        {
#if MIRROR
            if (NeoNetworkState.IsClient || NeoNetworkState.IsServer)
            {
                if (NeoNetworkState.IsClient)
                {
                    CmdDispatchEvent();
                }
                else if (NeoNetworkState.IsServer)
                {
                    // If invoked directly from dedicated server, broadcast to all clients
                    onNetworkEvent?.Invoke();
                    RpcDispatchEvent();
                }
                return;
            }
#endif
            // Offline fallback
            onNetworkEvent?.Invoke();
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdDispatchEvent()
        {
            if (requiresAuthority && !NeoNetworkState.HasAuthority(gameObject))
            {
                return; // Unauthorized
            }

            // Execute on dedicated server
            if (isServerOnly)
            {
                onNetworkEvent?.Invoke();
            }

            // Sync to all clients
            RpcDispatchEvent();
        }

        [ClientRpc(includeOwner = true)]
        private void RpcDispatchEvent()
        {
            onNetworkEvent?.Invoke();
        }
#endif
    }
}
