using System;
using System.Collections.Generic;

namespace Neo.Core.Level
{
    /// <summary>
    ///     Pure logic: evaluates level from total XP using formulas or custom entries.
    /// </summary>
    public static class LevelCurveEvaluator
    {
        /// <summary>
        ///     Evaluates the level for the given total XP using the specified curve type and parameters.
        /// </summary>
        /// <param name="totalXp">Accumulated XP</param>
        /// <param name="curveType">Linear, Quadratic, Exponential, or Custom</param>
        /// <param name="xpPerLevel">Used for Linear (XP required per level step)</param>
        /// <param name="quadraticBase">Used for Quadratic (base for level^2)</param>
        /// <param name="expBase">Used for Exponential</param>
        /// <param name="expFactor">Used for Exponential</param>
        /// <param name="customEntries">Used for Custom: ordered list of (Level, RequiredXp). Can be null.</param>
        /// <param name="maxLevel">Optional cap (0 = no cap)</param>
        /// <returns>Resolved level (at least 1)</returns>
        public static int EvaluateLevel(
            int totalXp,
            LevelCurveType curveType,
            int xpPerLevel = 100,
            float quadraticBase = 100f,
            float expBase = 100f,
            float expFactor = 1.5f,
            IReadOnlyList<ILevelCurveEntry> customEntries = null,
            int maxLevel = 0)
        {
            totalXp = Math.Max(0, totalXp);
            int xpPerLevelClamped = xpPerLevel < 1 ? 1 : xpPerLevel;
            float quadBaseClamped = NormalizeParameter(quadraticBase, 0.01f);
            float expB = NormalizeParameter(expBase, 0.01f);
            float expF = NormalizeParameter(expFactor, 1.01f);

            int raw = curveType switch
            {
                LevelCurveType.Linear => EvaluateLinear(totalXp, xpPerLevelClamped),
                LevelCurveType.Quadratic => EvaluateQuadratic(totalXp, quadBaseClamped),
                LevelCurveType.Exponential => EvaluateExponential(totalXp, expB, expF),
                LevelCurveType.Custom => EvaluateCustom(totalXp, customEntries),
                _ => 1
            };

            int level = raw < 1 ? 1 : raw;
            if (maxLevel > 0 && level > maxLevel)
            {
                level = maxLevel;
            }

            return level;
        }

        /// <summary>
        ///     Returns XP needed to reach the next level (0 if at max or no curve).
        /// </summary>
        public static int GetXpToNextLevel(
            int totalXp,
            LevelCurveType curveType,
            int xpPerLevel = 100,
            float quadraticBase = 100f,
            float expBase = 100f,
            float expFactor = 1.5f,
            IReadOnlyList<ILevelCurveEntry> customEntries = null,
            int maxLevel = 0)
        {
            totalXp = Math.Max(0, totalXp);
            int currentLevel = EvaluateLevel(totalXp, curveType, xpPerLevel, quadraticBase, expBase, expFactor,
                customEntries);
            if (currentLevel == int.MaxValue || (maxLevel > 0 && currentLevel >= maxLevel))
            {
                return 0;
            }

            // WHY: Evaluate* use the (L-1) convention (XP to reach level L is f(L-1)),
            // so XP for level currentLevel+1 is f(currentLevel), not f(currentLevel+1).
            switch (curveType)
            {
                case LevelCurveType.Linear:
                    return GetXpDifference(GetRequiredXpForLevelFormula(
                        currentLevel + 1, LevelFormulaType.Linear, xpPerLevel: xpPerLevel), totalXp);

                case LevelCurveType.Quadratic:
                    return GetXpDifference(GetRequiredXpForLevelFormula(
                        currentLevel + 1, LevelFormulaType.Quadratic, quadraticBase: quadraticBase), totalXp);

                case LevelCurveType.Exponential:
                    return GetXpDifference(GetRequiredXpForLevelFormula(
                        currentLevel + 1, LevelFormulaType.Exponential, expBase: expBase, expFactor: expFactor), totalXp);

                case LevelCurveType.Custom:
                    if (customEntries == null || customEntries.Count == 0)
                    {
                        return 0;
                    }

                    int nextRequiredXp = int.MaxValue;
                    bool hasNextLevel = false;
                    for (int i = 0; i < customEntries.Count; i++)
                    {
                        ILevelCurveEntry e = customEntries[i];
                        if (e == null)
                        {
                            continue;
                        }

                        if (e.Level > currentLevel && e.RequiredXp > totalXp &&
                            (!hasNextLevel || e.RequiredXp < nextRequiredXp))
                        {
                            nextRequiredXp = e.RequiredXp;
                            hasNextLevel = true;
                        }
                    }

                    return hasNextLevel ? GetXpDifference(nextRequiredXp, totalXp) : 0;

                default:
                    return 0;
            }
        }

        private static int EvaluateLinear(int totalXp, int xpPerLevel)
        {
            if (totalXp <= 0)
            {
                return 1;
            }

            int steps = totalXp / xpPerLevel;
            return steps == int.MaxValue ? int.MaxValue : 1 + steps;
        }

        private static int EvaluateQuadratic(int totalXp, float baseValue)
        {
            if (totalXp <= 0 || baseValue <= 0)
            {
                return 1;
            }

            int level = LevelFromSteps(Math.Sqrt(totalXp / (double)baseValue));
            if (level > 1 && baseValue * (level - 1d) * (level - 1d) > totalXp)
            {
                level--;
            }

            if (level < int.MaxValue && baseValue * (double)level * level <= totalXp)
            {
                level++;
            }

            return level;
        }

        private static int EvaluateExponential(int totalXp, float baseValue, float factor)
        {
            if (totalXp <= 0 || baseValue <= 0 || factor <= 1)
            {
                return 1;
            }

            // RequiredXp(L) = base * factor^(L-1); estimate completed steps, then verify adjacent thresholds.
            int level = LevelFromSteps(Math.Log(Math.Max(1d, totalXp / (double)baseValue)) / Math.Log(factor));
            if (level > 1 && baseValue * Math.Pow(factor, level - 1d) > totalXp)
            {
                level--;
            }

            if (level < int.MaxValue && baseValue * Math.Pow(factor, level) <= totalXp)
            {
                level++;
            }

            return level;
        }

        private static int EvaluateCustom(int totalXp, IReadOnlyList<ILevelCurveEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 1;
            }

            int resolved = 1;
            for (int i = 0; i < entries.Count; i++)
            {
                ILevelCurveEntry e = entries[i];
                if (e == null)
                {
                    continue;
                }

                if (totalXp >= e.RequiredXp)
                {
                    resolved = Math.Max(resolved, e.Level);
                }
            }

            return resolved;
        }

        /// <summary>
        ///     Evaluates level from total XP using a formula type and parameters.
        ///     RequiredXp(level) is defined per formula type (cumulative XP to reach that level).
        /// </summary>
        public static int EvaluateLevelByFormula(
            int totalXp,
            LevelFormulaType formulaType,
            int xpPerLevel = 100,
            float constantOffset = 0f,
            float quadraticBase = 100f,
            float expBase = 100f,
            float expFactor = 1.5f,
            float powerBase = 100f,
            float powerExponent = 2f,
            int maxLevel = 0)
        {
            totalXp = Math.Max(0, totalXp);
            constantOffset = float.IsNaN(constantOffset) || float.IsInfinity(constantOffset) ? 0f : constantOffset;
            int xpPer = xpPerLevel < 1 ? 1 : xpPerLevel;
            float qBase = NormalizeParameter(quadraticBase, 0.01f);
            float eBase = NormalizeParameter(expBase, 0.01f);
            float eFactor = NormalizeParameter(expFactor, 1.01f);
            float pBase = NormalizeParameter(powerBase, 0.01f);
            float pExp = NormalizeParameter(powerExponent, 0.1f);

            int raw = formulaType switch
            {
                LevelFormulaType.Linear => EvaluateLinear(totalXp, xpPer),
                LevelFormulaType.LinearWithOffset => EvaluateLinearWithOffset(totalXp, constantOffset, xpPer),
                LevelFormulaType.Quadratic => EvaluateQuadratic(totalXp, qBase),
                LevelFormulaType.Exponential => EvaluateExponential(totalXp, eBase, eFactor),
                LevelFormulaType.Power => EvaluatePower(totalXp, pBase, pExp),
                LevelFormulaType.PolynomialSingle => EvaluatePower(totalXp, pBase, pExp),
                _ => 1
            };

            int level = raw < 1 ? 1 : raw;
            if (maxLevel > 0 && level > maxLevel)
            {
                level = maxLevel;
            }

            return level;
        }

        /// <summary>
        ///     Returns XP needed to reach the next level for formula-based curves.
        /// </summary>
        public static int GetXpToNextLevelByFormula(
            int totalXp,
            LevelFormulaType formulaType,
            int xpPerLevel = 100,
            float constantOffset = 0f,
            float quadraticBase = 100f,
            float expBase = 100f,
            float expFactor = 1.5f,
            float powerBase = 100f,
            float powerExponent = 2f,
            int maxLevel = 0)
        {
            totalXp = Math.Max(0, totalXp);
            int currentLevel = EvaluateLevelByFormula(
                totalXp, formulaType, xpPerLevel, constantOffset,
                quadraticBase, expBase, expFactor, powerBase, powerExponent);

            if (currentLevel == int.MaxValue || (maxLevel > 0 && currentLevel >= maxLevel))
            {
                return 0;
            }

            double requiredNext = GetRequiredXpForLevelFormula(
                currentLevel + 1, formulaType,
                xpPerLevel, constantOffset, quadraticBase, expBase, expFactor, powerBase, powerExponent);
            return GetXpDifference(requiredNext, totalXp);
        }

        /// <summary>
        ///     Returns cumulative XP required to reach the given level (for formula types).
        /// </summary>
        public static double GetRequiredXpForLevelFormula(
            int level,
            LevelFormulaType formulaType,
            int xpPerLevel = 100,
            float constantOffset = 0f,
            float quadraticBase = 100f,
            float expBase = 100f,
            float expFactor = 1.5f,
            float powerBase = 100f,
            float powerExponent = 2f)
        {
            if (level < 1)
            {
                return 0;
            }

            constantOffset = float.IsNaN(constantOffset) || float.IsInfinity(constantOffset) ? 0f : constantOffset;
            int xpPer = xpPerLevel < 1 ? 1 : xpPerLevel;
            float qBase = NormalizeParameter(quadraticBase, 0.01f);
            float eBase = NormalizeParameter(expBase, 0.01f);
            float eFactor = NormalizeParameter(expFactor, 1.01f);
            float pBase = NormalizeParameter(powerBase, 0.01f);
            float pExp = NormalizeParameter(powerExponent, 0.1f);

            return formulaType switch
            {
                LevelFormulaType.Linear => (level - 1) * (double)xpPer,
                LevelFormulaType.LinearWithOffset => constantOffset + (level - 1) * (double)xpPer,
                LevelFormulaType.Quadratic => qBase * (level - 1d) * (level - 1d),
                LevelFormulaType.Exponential => eBase * Math.Pow(eFactor, level - 1),
                LevelFormulaType.Power => pBase * Math.Pow(level - 1, pExp),
                LevelFormulaType.PolynomialSingle => pBase * Math.Pow(level - 1, pExp),
                _ => (level - 1) * (double)xpPer
            };
        }

        private static int EvaluateLinearWithOffset(int totalXp, float constantOffset, int xpPerLevel)
        {
            if (totalXp <= constantOffset || xpPerLevel < 1)
            {
                return 1;
            }

            double n = (totalXp - (double)constantOffset) / xpPerLevel;
            return LevelFromSteps(n);
        }

        private static int EvaluatePower(int totalXp, float baseValue, float exponent)
        {
            if (totalXp <= 0 || baseValue <= 0 || exponent <= 0)
            {
                return 1;
            }

            int level = LevelFromSteps(Math.Pow(totalXp / (double)baseValue, 1.0 / exponent));
            if (level > 1 && baseValue * Math.Pow(level - 1d, exponent) > totalXp)
            {
                level--;
            }

            if (level < int.MaxValue && baseValue * Math.Pow(level, exponent) <= totalXp)
            {
                level++;
            }

            return level;
        }

        private static float NormalizeParameter(float value, float minimum)
        {
            return float.IsNaN(value) || float.IsInfinity(value) || value < minimum ? minimum : value;
        }

        private static int LevelFromSteps(double steps)
        {
            if (double.IsNaN(steps) || steps <= 0d)
            {
                return 1;
            }

            return steps >= int.MaxValue - 1d ? int.MaxValue : 1 + (int)Math.Floor(steps);
        }

        private static int GetXpDifference(double requiredXp, int totalXp)
        {
            // Subtract before converting: a threshold beyond Int32 can still have a small remaining gap.
            double difference = Math.Ceiling(requiredXp) - totalXp;
            if (double.IsNaN(difference) || difference <= 0d)
            {
                return 0;
            }

            return difference >= int.MaxValue ? int.MaxValue : (int)difference;
        }
    }
}
