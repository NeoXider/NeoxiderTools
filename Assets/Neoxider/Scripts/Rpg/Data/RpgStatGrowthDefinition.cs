using System;
using UnityEngine;

namespace Neo.Rpg
{
    public enum RpgStatGrowthMode
    {
        Formula = 0,
        Curve = 1
    }

    public enum RpgStatFormulaType
    {
        Linear = 0,
        Exponential = 1,
        Quadratic = 2,
        Power = 3,
        Flat = 4
    }

    /// <summary>
    /// Describes how a single stat grows along with the character level.
    /// </summary>
    [Serializable]
    public struct RpgStatGrowthRule
    {
        public RpgStatGrowthMode Mode;
        public RpgStatFormulaType FormulaType;

        public float BaseValue;
        public float AddPerLevel;
        public float MultiplierPerLevel;

        // Legacy compatibility
        [HideInInspector] public bool UseCurve;
        
        public AnimationCurve Curve;

        /// <summary>
        /// Calculates the final stat value for the given level.
        /// </summary>
        public float Evaluate(int level)
        {
            if ((Mode == RpgStatGrowthMode.Curve || UseCurve) && Curve != null && Curve.length > 0)
            {
                return Curve.Evaluate(level);
            }

            int levelMinusOne = Mathf.Max(0, level - 1);

            switch (FormulaType)
            {
                case RpgStatFormulaType.Flat:
                    return BaseValue;

                case RpgStatFormulaType.Linear:
                    return BaseValue + (AddPerLevel * levelMinusOne);

                case RpgStatFormulaType.Exponential:
                    float multiplier = MultiplierPerLevel <= 0f ? 1.1f : MultiplierPerLevel; 
                    return BaseValue * Mathf.Pow(multiplier, levelMinusOne);

                case RpgStatFormulaType.Quadratic:
                    return BaseValue + (AddPerLevel * levelMinusOne * levelMinusOne);
                
                case RpgStatFormulaType.Power:
                    float exponent = MultiplierPerLevel <= 0f ? 2f : MultiplierPerLevel;
                    return BaseValue + (AddPerLevel * Mathf.Pow(Mathf.Max(1, level), exponent));

                default:
                    return BaseValue + (AddPerLevel * levelMinusOne);
            }
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

        [Header("Progression Growth")]
        [Tooltip("The amount of XP rewarded when this combatant is defeated.")]
        public RpgStatGrowthRule XpReward = new() { BaseValue = 10f, AddPerLevel = 5f };
    }
}
