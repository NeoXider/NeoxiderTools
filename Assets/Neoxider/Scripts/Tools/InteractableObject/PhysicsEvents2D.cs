/***************************************************************************
 *  PhysicsEvents2D ‒ the same idea, but for 2-D physics.                  *
 ***************************************************************************/

using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/Physics/Physics Events 2D")]
    public sealed class PhysicsEvents2D : MonoBehaviour
    {
        public bool interactable = true;

        [Header("Filtering")] public LayerMask layers = ~0;
        public string requiredTag = "";

        /* ───────── EVENTS ─────────────────────────────────────────── */

        [Header("Trigger events")] public Collider2DEvent onTriggerEnter = new();
        public Collider2DEvent onTriggerStay = new();
        public Collider2DEvent onTriggerExit = new();

        [Header("Collision events")] public Collision2DEvent onCollisionEnter = new();
        public Collision2DEvent onCollisionStay = new();
        public Collision2DEvent onCollisionExit = new();

        [System.Serializable]
        public class Collider2DEvent : UnityEvent<Collider2D>
        {
        }

        [System.Serializable]
        public class Collision2DEvent : UnityEvent<Collision2D>
        {
        }

        /* ───────── INTERNAL ───────────────────────────────────────── */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PassFilter(GameObject go)
        {
            if (((1 << go.layer) & layers) == 0) return false;
            return string.IsNullOrEmpty(requiredTag) || go.CompareTag(requiredTag);
        }

        /* Trigger ---------------------------------------------------- */
        private void OnTriggerEnter2D(Collider2D c)
        {
            if (interactable && PassFilter(c.gameObject)) onTriggerEnter.Invoke(c);
        }

        private void OnTriggerStay2D(Collider2D c)
        {
            if (interactable && PassFilter(c.gameObject)) onTriggerStay.Invoke(c);
        }

        private void OnTriggerExit2D(Collider2D c)
        {
            if (interactable && PassFilter(c.gameObject)) onTriggerExit.Invoke(c);
        }

        /* Collision -------------------------------------------------- */
        private void OnCollisionEnter2D(Collision2D c)
        {
            if (interactable && PassFilter(c.gameObject)) onCollisionEnter.Invoke(c);
        }

        private void OnCollisionStay2D(Collision2D c)
        {
            if (interactable && PassFilter(c.gameObject)) onCollisionStay.Invoke(c);
        }

        private void OnCollisionExit2D(Collision2D c)
        {
            if (interactable && PassFilter(c.gameObject)) onCollisionExit.Invoke(c);
        }
    }
}