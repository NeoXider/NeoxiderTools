/***************************************************************************
 *  PhysicsEvents2D ‒ the same idea, but for 2-D physics.                  *
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
    [NeoDoc("Tools/InteractableObject/PhysicsEvents2D.md")]
    [CreateFromMenu("Neoxider/Tools/Physics/PhysicsEvents2D")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PhysicsEvents2D))]
    public sealed class PhysicsEvents2D : NeoNetworkComponent
    {
        public bool interactable = true;

        [Header("Filtering")]
        [Tooltip("When enabled, other object must match requiredTag (if requiredTag is non-empty).")]
        public bool filterByTag;

        [Tooltip("When enabled, other object’s layer must be included in layers.")]
        public bool filterByLayer = true;

        public LayerMask layers = ~0;

        [Tooltip("Tag to match when filterByTag is enabled and this string is non-empty.")]
        public string requiredTag = "";

        /* ───────── EVENTS ─────────────────────────────────────────── */

        public Collider2DEvent onTriggerEnter = new();
        public Collider2DEvent onTriggerStay = new();
        public Collider2DEvent onTriggerExit = new();

        public Collision2DEvent onCollisionEnter = new();
        public Collision2DEvent onCollisionStay = new();
        public Collision2DEvent onCollisionExit = new();

        /// <inheritdoc cref="PhysicsEvents3D.TriggerEnterOccurred"/>
        public event Action<Collider2D> TriggerEnterOccurred;
        public event Action<Collider2D> TriggerStayOccurred;
        public event Action<Collider2D> TriggerExitOccurred;
        public event Action<Collision2D> CollisionEnterOccurred;
        public event Action<Collision2D> CollisionStayOccurred;
        public event Action<Collision2D> CollisionExitOccurred;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchTriggerEnter(Collider2D c)
        {
            onTriggerEnter.Invoke(c);
            TriggerEnterOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchTriggerStay(Collider2D c)
        {
            onTriggerStay.Invoke(c);
            TriggerStayOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchTriggerExit(Collider2D c)
        {
            onTriggerExit.Invoke(c);
            TriggerExitOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchCollisionEnter(Collision2D c)
        {
            onCollisionEnter.Invoke(c);
            CollisionEnterOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchCollisionStay(Collision2D c)
        {
            onCollisionStay.Invoke(c);
            CollisionStayOccurred?.Invoke(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchCollisionExit(Collision2D c)
        {
            onCollisionExit.Invoke(c);
            CollisionExitOccurred?.Invoke(c);
        }

        /* Collision -------------------------------------------------- */
        private void OnCollisionEnter2D(Collision2D c)
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

        private void OnCollisionExit2D(Collision2D c)
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

        private void OnCollisionStay2D(Collision2D c)
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
        private void OnTriggerEnter2D(Collider2D c)
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

        private void OnTriggerExit2D(Collider2D c)
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

        private void OnTriggerStay2D(Collider2D c)
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
            DispatchCollisionEnter(null); 
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
            var col = target != null ? target.GetComponent<Collider2D>() : null;
            DispatchTriggerEnter(col);
        }

        [ClientRpc]
        private void RpcTriggerExit(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            var col = target != null ? target.GetComponent<Collider2D>() : null;
            DispatchTriggerExit(col);
        }

        [ClientRpc]
        private void RpcTriggerStay(GameObject target)
        {
            if (NeoNetworkState.IsServer) return;
            var col = target != null ? target.GetComponent<Collider2D>() : null;
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
        public class Collider2DEvent : UnityEvent<Collider2D>
        {
        }

        [Serializable]
        public class Collision2DEvent : UnityEvent<Collision2D>
        {
        }
    }
}
