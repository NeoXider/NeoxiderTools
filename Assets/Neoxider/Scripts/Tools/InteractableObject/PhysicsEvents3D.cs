/***************************************************************************
 *  PhysicsEvents3D ‒ one compact component that forwards *both*
 *  Trigger **and** Collision callbacks to UnityEvents.
 *  – Interactable switch (no need to disable GameObject)                 *
 *  – Optional layer / tag filters (each toggled separately)               *
 *  – Easy to extend: just add your own UnityEvent fields or extra logic  *
 ***************************************************************************/

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using Neo.Network;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tools
{
    [NeoDoc("Tools/InteractableObject/PhysicsEvents3D.md")]
    [CreateFromMenu("Neoxider/Tools/Physics/PhysicsEvents3D")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PhysicsEvents3D))]
#if MIRROR
    public sealed class PhysicsEvents3D : NetworkBehaviour
#else
    public sealed class PhysicsEvents3D : MonoBehaviour
#endif
    {
        [Tooltip("If OFF, callbacks are suppressed without disabling this GO")]
        public bool interactable = true;

        [Header("Networking")]
        [Tooltip("If enabled, collisions and triggers are caught on the Server and replicated to all Clients via RPC.")]
        public bool isNetworked = false;

        [Header("Filtering")]
        [Tooltip("When enabled, other object must match requiredTag (if requiredTag is non-empty).")]
        public bool filterByTag;

        [Tooltip("When enabled, other object’s layer must be included in layers.")]
        public bool filterByLayer = true;

        public LayerMask layers = ~0;

        [Tooltip("Tag to match when filterByTag is enabled and this string is non-empty.")]
        public string requiredTag = "";

        /* ───────── EVENTS ─────────────────────────────────────────── */

        public ColliderEvent onTriggerEnter = new();
        public ColliderEvent onTriggerStay = new();
        public ColliderEvent onTriggerExit = new();

        public CollisionEvent onCollisionEnter = new();
        public CollisionEvent onCollisionStay = new();
        public CollisionEvent onCollisionExit = new();

        /* Collision -------------------------------------------------- */
        private void OnCollisionEnter(Collision c)
        {
            if (!interactable || !PassFilter(c.gameObject)) return;
            
            if (!isNetworked)
            {
                onCollisionEnter.Invoke(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                onCollisionEnter.Invoke(c);
                var netId = c.gameObject.GetComponent<NetworkIdentity>();
                RpcCollisionEnter(netId != null ? netId.gameObject : null);
            }
#endif
        }

        private void OnCollisionExit(Collision c)
        {
            if (!interactable || !PassFilter(c.gameObject)) return;
            
            if (!isNetworked)
            {
                onCollisionExit.Invoke(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                onCollisionExit.Invoke(c);
                var netId = c.gameObject.GetComponent<NetworkIdentity>();
                RpcCollisionExit(netId != null ? netId.gameObject : null);
            }
#endif
        }

        private void OnCollisionStay(Collision c)
        {
            if (!interactable || !PassFilter(c.gameObject)) return;
            
            if (!isNetworked)
            {
                onCollisionStay.Invoke(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                onCollisionStay.Invoke(c);
                var netId = c.gameObject.GetComponent<NetworkIdentity>();
                RpcCollisionStay(netId != null ? netId.gameObject : null);
            }
#endif
        }

        /* Trigger ---------------------------------------------------- */
        private void OnTriggerEnter(Collider c)
        {
            if (!interactable || !PassFilter(c.gameObject)) return;
            
            if (!isNetworked)
            {
                onTriggerEnter.Invoke(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                onTriggerEnter.Invoke(c);
                var netId = c.GetComponent<NetworkIdentity>();
                RpcTriggerEnter(netId != null ? netId.gameObject : null);
            }
#endif
        }

        private void OnTriggerExit(Collider c)
        {
            if (!interactable || !PassFilter(c.gameObject)) return;
            
            if (!isNetworked)
            {
                onTriggerExit.Invoke(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                onTriggerExit.Invoke(c);
                var netId = c.GetComponent<NetworkIdentity>();
                RpcTriggerExit(netId != null ? netId.gameObject : null);
            }
#endif
        }

        private void OnTriggerStay(Collider c)
        {
            if (!interactable || !PassFilter(c.gameObject)) return;
            
            if (!isNetworked)
            {
                onTriggerStay.Invoke(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                onTriggerStay.Invoke(c);
                var netId = c.GetComponent<NetworkIdentity>();
                RpcTriggerStay(netId != null ? netId.gameObject : null);
            }
#endif
        }

#if MIRROR
        /* ───────── RPCs ───────────────────────────────────────────── */

        [ClientRpc]
        private void RpcCollisionEnter(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            onCollisionEnter.Invoke(null); // Native Collision object cannot be sent across network
        }
        
        [ClientRpc]
        private void RpcCollisionExit(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            onCollisionExit.Invoke(null);
        }

        [ClientRpc]
        private void RpcCollisionStay(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            onCollisionStay.Invoke(null);
        }

        [ClientRpc]
        private void RpcTriggerEnter(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            onTriggerEnter.Invoke(target != null ? target.GetComponent<Collider>() : null);
        }

        [ClientRpc]
        private void RpcTriggerExit(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            onTriggerExit.Invoke(target != null ? target.GetComponent<Collider>() : null);
        }

        [ClientRpc]
        private void RpcTriggerStay(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            onTriggerStay.Invoke(target != null ? target.GetComponent<Collider>() : null);
        }
        
        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
        }
#endif

        /* ───────── INTERNAL ───────────────────────────────────────── */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PassFilter(GameObject go)
        {
            if (filterByTag && !string.IsNullOrEmpty(requiredTag) && !go.CompareTag(requiredTag))
            {
                return false;
            }

            if (filterByLayer && ((1 << go.layer) & layers.value) == 0)
            {
                return false;
            }

            return true;
        }

        [Serializable]
        public class ColliderEvent : UnityEvent<Collider>
        {
        }

        [Serializable]
        public class CollisionEvent : UnityEvent<Collision>
        {
        }
    }
}
