using UnityEngine;

namespace Neo.Rpg.Components.Weapons
{
    /// <summary>
    /// Base class for all melee weapons, providing common layer and tag filtering.
    /// </summary>
    public abstract class MeleeWeapon : MonoBehaviour
    {
        [Header("Targeting Filtering")]
        public LayerMask hitLayers;
        public string targetTag = "Enemy";

        [Header("Damage Settings")]
        public int damage = 10;

        /// <summary>
        /// Checks if a collider is a valid target based on layer, tag, and excludes the weapon's own hierarchy.
        /// </summary>
        protected bool IsValidTarget(Collider other)
        {
            if (other.transform.root == this.transform.root)
                return false;

            if (hitLayers != (hitLayers | (1 << other.gameObject.layer)))
                return false;

            if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
                return false;

            return true;
        }

        /// <summary>
        /// Apply damage to a target combatant.
        /// </summary>
        protected virtual void DealDamage(Collider target)
        {
            var combatant = target.GetComponentInParent<RpgCombatant>();
            if (combatant != null)
            {
                var source = GetComponentInParent<IRpgCombatReceiver>();
                float amount = damage;
                
                if (source != null)
                {
                    amount *= source.GetOutgoingDamageMultiplier();
                }

                combatant.TakeDamage(new RpgDamageInfo(amount, source: source));
            }
        }
    }
}
