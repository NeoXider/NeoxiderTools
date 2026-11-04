using UnityEngine;

namespace Neo.Rpg.Components.Weapons
{
    /// <summary>
    /// Base class for all melee weapons, providing common layer and tag filtering.
    /// </summary>
    [NeoDoc("Rpg/Components/Weapons/MeleeWeapon.md")]
    public abstract class MeleeWeapon : MonoBehaviour
    {
        [Header("Targeting Filtering")] public LayerMask hitLayers;
        public string targetTag = "Enemy";

        [Header("Damage Settings")] public int damage = 10;

        [Tooltip("Damage type of every hit (Fire, Cold, ...), matched by the target's DamageTypeResistPercent " +
                 "buffs. Empty = untyped damage.")]
        [SerializeField]
        private string _damageType = string.Empty;

        /// <summary>Damage type of every hit; empty (the default) deals untyped damage. Null is stored as empty.</summary>
        public string DamageType
        {
            get => _damageType ?? string.Empty;
            set => _damageType = value ?? string.Empty;
        }

        /// <summary>
        /// Checks if a collider is a valid target based on layer, tag, and excludes the weapon's own hierarchy.
        /// </summary>
        protected bool IsValidTarget(Collider other)
        {
            if (other.transform.root == transform.root)
            {
                return false;
            }

            if (hitLayers != (hitLayers | (1 << other.gameObject.layer)))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Apply damage to a target combatant. The wielder's <see cref="IRpgCombatReceiver"/> scales the damage
        /// and is passed as <see cref="RpgDamageInfo.Source"/>, so damage modifiers and
        /// <see cref="RpgCharacter.DamageApplied"/> see who hit; <see cref="DamageType"/> selects the resistance.
        /// </summary>
        protected virtual void DealDamage(Collider target)
        {
            RpgCharacter character = target.GetComponentInParent<RpgCharacter>();
            if (character != null)
            {
                IRpgCombatReceiver source = GetComponentInParent<IRpgCombatReceiver>();
                float amount = damage;

                if (source != null)
                {
                    amount *= source.GetOutgoingDamageMultiplier();
                }

                character.ApplyDamage(new RpgDamageInfo(amount, DamageType, source));
            }
        }
    }
}
