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

namespace Neo.Tools
{
    [NeoDoc("Tools/InteractableObject/PhysicsEvents3D.md")]
    [CreateFromMenu("Neoxider/Tools/Physics/PhysicsEvents3D")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PhysicsEvents3D))]
    public sealed class PhysicsEvents3D : MonoBehaviour
    {
        [Tooltip("If OFF, callbacks are suppressed without disabling this GO")]
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

        public ColliderEvent onTriggerEnter = new();
        public ColliderEvent onTriggerStay = new();
        public ColliderEvent onTriggerExit = new();

        public CollisionEvent onCollisionEnter = new();
        public CollisionEvent onCollisionStay = new();
        public CollisionEvent onCollisionExit = new();

        /* Collision -------------------------------------------------- */
        private void OnCollisionEnter(Collision c)
        {
            if (interactable && PassFilter(c.gameObject))
            {
                onCollisionEnter.Invoke(c);
            }
        }

        private void OnCollisionExit(Collision c)
        {
            if (interactable && PassFilter(c.gameObject))
            {
                onCollisionExit.Invoke(c);
            }
        }

        private void OnCollisionStay(Collision c)
        {
            if (interactable && PassFilter(c.gameObject))
            {
                onCollisionStay.Invoke(c);
            }
        }

        /* Trigger ---------------------------------------------------- */
        private void OnTriggerEnter(Collider c)
        {
            if (interactable && PassFilter(c.gameObject))
            {
                onTriggerEnter.Invoke(c);
            }
        }

        private void OnTriggerExit(Collider c)
        {
            if (interactable && PassFilter(c.gameObject))
            {
                onTriggerExit.Invoke(c);
            }
        }

        private void OnTriggerStay(Collider c)
        {
            if (interactable && PassFilter(c.gameObject))
            {
                onTriggerStay.Invoke(c);
            }
        }

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
