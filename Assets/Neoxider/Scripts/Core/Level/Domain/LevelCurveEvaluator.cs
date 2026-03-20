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
            int xpPerLevelClamped = xpPerLevel < 1 ? 1 : xpPerLevel;
            float quadBaseClamped = quadraticBase < 0.01f ? 0.01f : quadraticBase;
            float expB = expBase < 0.01f ? 0.01f : expBase;
            float expF = expFactor < 1.01f ? 1.01f : expFactor;

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
            int currentLevel = EvaluateLevel(totalXp, curveType, xpPerLevel, quadraticBase, expBase, expFactor,
                customEntries);
            if (maxLevel > 0 && currentLevel >= maxLevel)
            {
                return 0;
            }

            switch (curveType)
            {
                case LevelCurveType.Linear:
                    int nextLevelLinear = currentLevel + 1;
                    int xpPer = xpPerLevel < 1 ? 1 : xpPerLevel;
                    int xpForNextLinear = nextLevelLinear * xpPer;
                    int diffLinear = xpForNextLinear - totalXp;
                    return diffLinear < 0 ? 0 : diffLinear;

                case LevelCurveType.Quadratic:
                    int nextLevelQuad = currentLevel + 1;
                    int xpForNextQuad = (int)(quadraticBase * nextLevelQuad * nextLevelQuad);
                    int diffQuad = xpForNextQuad - totalXp;
                    return diffQuad < 0 ? 0 : diffQuad;

                case LevelCurveType.Exponential:
                    int xpForNextExp = (int)(expBase * Math.Pow(expFactor, currentLevel + 1));
                    int diffExp = xpForNextExp - totalXp;
                    return diffExp < 0 ? 0 : diffExp;

                case LevelCurveType.Custom:
                    if (customEntries == null || customEntries.Count == 0)
                    {
                        return 0;
                    }

                    for (int i = 0; i < customEntries.Count; i++)
                    {
                        ILevelCurveEntry e = customEntries[i];
                        if (e == null)
                        {
                            continue;
                        }

                        if (e.RequiredXp > totalXp)
                        {
                            int diff = e.RequiredXp - totalXp;
                            return diff < 0 ? 0 : diff;
                        }
                    }

                    return 0;

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

            return 1 + totalXp / xpPerLevel;
        }

        private static int EvaluateQuadratic(int totalXp, float baseValue)
        {
            if (totalXp <= 0 || baseValue <= 0)
            {
                return 1;
            }

            double level = Math.Sqrt(totalXp / baseValue);
            return 1 + (int)Math.Floor(level);
        }

        private static int EvaluateExponential(int totalXp, float baseValue, float factor)
        {
            if (totalXp <= 0 || baseValue <= 0 || factor <= 1)
            {
                return 1;
            }

            // RequiredXp(level) = base * factor^level; solve for level: level = log(totalXp/base) / log(factor)
            double level = Math.Log(Math.Max(1, totalXp / baseValue)) / Math.Log(factor);
            return 1 + (int)Math.Floor(level);
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

        // --- Formula mode (LevelFormulaType) ---

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
            int xpPer = xpPerLevel < 1 ? 1 : xpPerLevel;
            float qBase = quadraticBase < 0.01f ? 0.01f : quadraticBase;
            float eBase = expBase < 0.01f ? 0.01f : expBase;
            float eFactor = expFactor < 1.01f ? 1.01f : expFactor;
            float pBase = powerBase < 0.01f ? 0.01f : powerBase;
            float pExp = powerExponent < 0.1f ? 0.1f : powerExponent;

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
            int currentLevel = EvaluateLevelByFormula(
                totalXp, formulaType, xpPerLevel, constantOffset,
                quadraticBase, expBase, expFactor, powerBase, powerExponent);

            if (maxLevel > 0 && currentLevel >= maxLevel)
            {
                return 0;
            }

            double requiredNext = GetRequiredXpForLevelFormula(
                currentLevel + 1, formulaType,
                xpPerLevel, constantOffset, quadraticBase, expBase, expFactor, powerBase, powerExponent);
            int diff = (int)Math.Ceiling(requiredNext) - totalXp;
            return diff < 0 ? 0 : diff;
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

            int xpPer = xpPerLevel < 1 ? 1 : xpPerLevel;
            float qBase = quadraticBase < 0.01f ? 0.01f : quadraticBase;
            float eBase = expBase < 0.01f ? 0.01f : expBase;
            float eFactor = expFactor < 1.01f ? 1.01f : expFactor;
            float pBase = powerBase < 0.01f ? 0.01f : powerBase;
            float pExp = powerExponent < 0.1f ? 0.1f : powerExponent;

            return formulaType switch
            {
                LevelFormulaType.Linear => (level - 1) * (double)xpPer,
                LevelFormulaType.LinearWithOffset => constantOffset + (level - 1) * (double)xpPer,
                LevelFormulaType.Quadratic => qBase * (level - 1) * (level - 1),
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

            double n = (totalXp - constantOffset) / (double)xpPerLevel;
            return 1 + (int)Math.Floor(n);
        }

        private static int EvaluatePower(int totalXp, float baseValue, float exponent)
        {
            if (totalXp <= 0 || baseValue <= 0 || exponent <= 0)
            {
                return 1;
            }

            double level = Math.Pow(totalXp / baseValue, 1.0 / exponent);
            return 1 + (int)Math.Floor(level);
        }
    }
}
