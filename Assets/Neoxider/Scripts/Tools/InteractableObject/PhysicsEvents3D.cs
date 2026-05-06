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

        /// <summary>
        /// Подписка в коде (<c>+=</c>) — срабатывает вместе с <see cref="onTriggerEnter"/> и т.д.
        /// При <see cref="isNetworked"/>: на сервере с полным <see cref="Collider"/> / <see cref="Collision"/>;
        /// на клиентах после <c>ClientRpc</c> (для коллизий на клиенте <see cref="Collision"/> недоступен — передаётся <c>null</c>, как у UnityEvent).
        /// </summary>
        public event Action<Collider> TriggerEnterOccurred;
        public event Action<Collider> TriggerStayOccurred;
        public event Action<Collider> TriggerExitOccurred;
        public event Action<Collision> CollisionEnterOccurred;
        public event Action<Collision> CollisionStayOccurred;
        public event Action<Collision> CollisionExitOccurred;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchTriggerEnter(Collider c)
        {
            onTriggerEnter.Invoke(c);
            TriggerEnterOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchTriggerStay(Collider c)
        {
            onTriggerStay.Invoke(c);
            TriggerStayOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchTriggerExit(Collider c)
        {
            onTriggerExit.Invoke(c);
            TriggerExitOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchCollisionEnter(Collision c)
        {
            onCollisionEnter.Invoke(c);
            CollisionEnterOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchCollisionStay(Collision c)
        {
            onCollisionStay.Invoke(c);
            CollisionStayOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchCollisionExit(Collision c)
        {
            onCollisionExit.Invoke(c);
            CollisionExitOccurred?.Invoke(c);
        }

        /* Collision -------------------------------------------------- */
        private void OnCollisionEnter(Collision c)
        {
            if (!interactable || !PassFilter(c.gameObject)) return;
            
            if (!isNetworked)
            {
                DispatchCollisionEnter(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                DispatchCollisionEnter(c);
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
                DispatchCollisionExit(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                DispatchCollisionExit(c);
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
                DispatchCollisionStay(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                DispatchCollisionStay(c);
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
                DispatchTriggerEnter(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                DispatchTriggerEnter(c);
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
                DispatchTriggerExit(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                DispatchTriggerExit(c);
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
                DispatchTriggerStay(c);
            }
#if MIRROR
            else if (NeoNetworkState.IsServer)
            {
                DispatchTriggerStay(c);
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
            DispatchCollisionEnter(null); // Native Collision object cannot be sent across network
        }
        
        [ClientRpc]
        private void RpcCollisionExit(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            DispatchCollisionExit(null);
        }

        [ClientRpc]
        private void RpcCollisionStay(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            DispatchCollisionStay(null);
        }

        [ClientRpc]
        private void RpcTriggerEnter(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            var col = target != null ? target.GetComponent<Collider>() : null;
            DispatchTriggerEnter(col);
        }

        [ClientRpc]
        private void RpcTriggerExit(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            var col = target != null ? target.GetComponent<Collider>() : null;
            DispatchTriggerExit(col);
        }

        [ClientRpc]
        private void RpcTriggerStay(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            var col = target != null ? target.GetComponent<Collider>() : null;
            DispatchTriggerStay(col);
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
