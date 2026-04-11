using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Describes how a single stat grows along with the character level.
    /// </summary>
    [Serializable]
    public struct RpgStatGrowthRule
    {
        public float BaseValue;
        public float AddPerLevel;
        public bool UseCurve;
        public AnimationCurve Curve;

        /// <summary>
        /// Calculates the final stat value for the given level.
        /// </summary>
        public float Evaluate(int level)
        {
            if (UseCurve && Curve != null && Curve.length > 0)
            {
                return Curve.Evaluate(level);
            }
            return BaseValue + (AddPerLevel * Mathf.Max(0, level - 1));
        }
    }

    /// <summary>
    /// ScriptableObject defining growth for multiple stats across levels.
    /// </summary>
    [NeoDoc("Rpg/RpgStatGrowth.md")]
    [CreateAssetMenu(menuName = "Neoxider/RPG/StatGrowthDefinition", fileName = "StatGrowthDefinition")]
    public sealed class RpgStatGrowthDefinition : ScriptableObject
    {
        [Header("Primary Resource Growth")]
        public RpgStatGrowthRule MaxHp = new() { BaseValue = 100f, AddPerLevel = 10f };
        public RpgStatGrowthRule HpRegen = new() { BaseValue = 0f, AddPerLevel = 0f };

        [Header("Combat Modifiers Growth")]
        [Tooltip("A flat percentage added to out-going damage per level.")]
        public RpgStatGrowthRule DamagePercent = new() { BaseValue = 0f, AddPerLevel = 1f };

        [Tooltip("A flat percentage added to overall defense per level.")]
        public RpgStatGrowthRule DefensePercent = new() { BaseValue = 0f, AddPerLevel = 0.5f };
    }
}
