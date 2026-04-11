using UnityEngine;
using Neo.Tools;

namespace Neo.Rpg.Components.Weapons
{
    /// <summary>
    /// An aura weapon that damages nearby enemies on a timer.
    /// Can be linked to a TimerObject's UnityEvent or driven interally.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class AuraWeapon : MeleeWeapon
    {
        private SphereCollider _auraCollider;

        private void Awake()
        {
            _auraCollider = GetComponent<SphereCollider>();
            _auraCollider.isTrigger = true;
            
            var timer = GetComponent<TimerObject>();
            if (timer != null)
                timer.OnTimerCompleted.AddListener(ApplyAuraDamage);
        }

        /// <summary>
        /// Applies damage to all valid targets currently within the aura's trigger bounds.
        /// Call this from a TimerObject's OnTimerCompleted event (set Timer to loop).
        /// </summary>
        public void ApplyAuraDamage()
        {
            // Use physics overlap to find all colliders in the aura radius
            Collider[] hits = Physics.OverlapSphere(transform.position, _auraCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
            
            foreach (var hit in hits)
            {
                if (hit == null || hit.gameObject == this.gameObject) continue;

                if (IsValidTarget(hit))
                {
                    DealDamage(hit);
                }
            }
        }
    }
}
