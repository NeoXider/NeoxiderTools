/***************************************************************************
 *  PhysicsEvents3D ‒ one compact component that forwards *both*
 *  Trigger **and** Collision callbacks to UnityEvents.
 *  – Interactable switch (no need to disable GameObject)                 *
 *  – Layer-mask & optional Tag filter                                    *
 *  – Easy to extend: just add your own UnityEvent fields or extra logic  *
 ***************************************************************************/

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/Physics/Physics Events 3D")]
    public sealed class PhysicsEvents3D : MonoBehaviour
    {
        [Tooltip("If OFF, callbacks are suppressed without disabling this GO")]
        public bool interactable = true;

        [Header("Filtering")] public LayerMask layers = ~0; // “everything” by default
        public string requiredTag = ""; // empty - ignore tag filter

        /* ───────── EVENTS ─────────────────────────────────────────── */

        [Header("Trigger events")] public ColliderEvent onTriggerEnter = new();
        public ColliderEvent onTriggerStay = new();
        public ColliderEvent onTriggerExit = new();

        [Header("Collision events")] public CollisionEvent onCollisionEnter = new();
        public CollisionEvent onCollisionStay = new();
        public CollisionEvent onCollisionExit = new();

        /* Collision -------------------------------------------------- */
        private void OnCollisionEnter(Collision c)
        {
            if (interactable && PassFilter(c.gameObject)) onCollisionEnter.Invoke(c);
        }

        private void OnCollisionExit(Collision c)
        {
            if (interactable && PassFilter(c.gameObject)) onCollisionExit.Invoke(c);
        }

        private void OnCollisionStay(Collision c)
        {
            if (interactable && PassFilter(c.gameObject)) onCollisionStay.Invoke(c);
        }

        /* Trigger ---------------------------------------------------- */
        private void OnTriggerEnter(Collider c)
        {
            if (interactable && PassFilter(c.gameObject)) onTriggerEnter.Invoke(c);
        }

        private void OnTriggerExit(Collider c)
        {
            if (interactable && PassFilter(c.gameObject)) onTriggerExit.Invoke(c);
        }

        private void OnTriggerStay(Collider c)
        {
            if (interactable && PassFilter(c.gameObject)) onTriggerStay.Invoke(c);
        }

        /* ───────── INTERNAL ───────────────────────────────────────── */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PassFilter(GameObject go)
        {
            if (((1 << go.layer) & layers) == 0) return false;
            return string.IsNullOrEmpty(requiredTag) || go.CompareTag(requiredTag);
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