using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     One resource cost of a cast (e.g. 50 mana). Mana costs honor the mana_cost_mul property.
    /// </summary>
    [Serializable]
    public struct AbilityCost
    {
        [Tooltip("Resource pool id (health, mana, or custom).")]
        public string ResourceId;

        [Tooltip("Amount spent on cast.")]
        public float Amount;

        public AbilityCost(string resourceId, float amount)
        {
            ResourceId = resourceId;
            Amount = amount;
        }
    }
}
