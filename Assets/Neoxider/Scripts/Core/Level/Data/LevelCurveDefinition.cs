using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Core.Level
{
    /// <summary>
    ///     ScriptableObject: three modes — Formula, Curve (graph), Custom (manual table).
    ///     Formula: pick type (Linear, Quadratic, Exponential, Power, etc.) and parameters.
    ///     Curve: AnimationCurve (X axis = level, Y = cumulative XP to that level).
    ///     Custom: list of (level, required XP) pairs.
    /// </summary>
    [CreateAssetMenu(fileName = "Level Curve Definition", menuName = "Neoxider/Core/Level Curve Definition")]
    [NeoDoc("Core/Level/Data/CurveEvaluation.md")]
    public sealed class LevelCurveDefinition : ScriptableObject, ILevelCurveDefinition
    {
        private const int CurveLevelLimit = 10000;

        [Header("Mode")] [SerializeField] private LevelCurveMode _mode = LevelCurveMode.Formula;

        [Tooltip("Formula type (used when mode is Formula)")] [SerializeField]
        private LevelFormulaType _formulaType = LevelFormulaType.Linear;

        [Header("Formula parameters")] [SerializeField] [Min(1)]
        private int _xpPerLevel = 100;

        [Tooltip("Offset for LinearWithOffset")] [SerializeField] [Min(0)]
        private float _constantOffset;

        [SerializeField] [Min(0.01f)] private float _quadraticBase = 100f;
        [SerializeField] [Min(0.01f)] private float _expBase = 100f;
        [SerializeField] [Min(1.01f)] private float _expFactor = 1.5f;

        [Tooltip("Base for Power/PolynomialSingle: RequiredXp(level) = base * level^exponent")]
        [SerializeField]
        [Min(0.01f)]
        private float _powerBase = 100f;

        [SerializeField] [Min(0.1f)] private float _powerExponent = 2f;

        [Header("Curve")] [Tooltip("X axis = level (1, 2, 3...), Y = cumulative XP to that level")] [SerializeField]
        private AnimationCurve _animationCurve = new(
            new Keyframe(1f, 0f),
            new Keyframe(2f, 100f),
            new Keyframe(3f, 250f));

        [Header("Custom table")] [SerializeField]
        private List<LevelCurveEntry> _customEntries = new();

        public LevelCurveMode Mode => _mode;
        public LevelFormulaType FormulaType => _formulaType;
        public int XpPerLevel { get => _xpPerLevel; set => _xpPerLevel = value < 1 ? 1 : value; }
        public float ConstantOffset => _constantOffset;
        public float QuadraticBase => _quadraticBase;
        public float ExpBase => _expBase;
        public float ExpFactor => _expFactor;
        public float PowerBase => _powerBase;
        public float PowerExponent => _powerExponent;
        public AnimationCurve AnimationCurve => _animationCurve;
        public IReadOnlyList<LevelCurveEntry> CustomEntries => _customEntries;

        /// <summary>Backward compatibility: curve type (when Mode=Formula matches FormulaType).</summary>
        public LevelCurveType CurveType => _mode == LevelCurveMode.Formula
            ? MapFormulaTypeToCurveType(_formulaType)
            : _mode == LevelCurveMode.Custom
                ? LevelCurveType.Custom
                : LevelCurveType.Linear;

        public int EvaluateLevel(int totalXp, int maxLevel = 0)
        {
            switch (_mode)
            {
                case LevelCurveMode.Formula:
                    return LevelCurveEvaluator.EvaluateLevelByFormula(
                        totalXp, _formulaType,
                        _xpPerLevel, _constantOffset,
                        _quadraticBase, _expBase, _expFactor,
                        _powerBase, _powerExponent,
                        maxLevel);
                case LevelCurveMode.Curve:
                    return EvaluateLevelFromCurve(totalXp, maxLevel);
                case LevelCurveMode.Custom:
                    return LevelCurveEvaluator.EvaluateLevel(
                        totalXp, LevelCurveType.Custom,
                        _xpPerLevel, _quadraticBase, _expBase, _expFactor,
                        _customEntries, maxLevel);
                default:
                    return LevelCurveEvaluator.EvaluateLevelByFormula(
                        totalXp, LevelFormulaType.Linear, _xpPerLevel, 0,
                        _quadraticBase, _expBase, _expFactor, _powerBase, _powerExponent, maxLevel);
            }
        }

        public int GetXpToNextLevel(int totalXp, int maxLevel = 0)
        {
            switch (_mode)
            {
                case LevelCurveMode.Formula:
                    return LevelCurveEvaluator.GetXpToNextLevelByFormula(
                        totalXp, _formulaType,
                        _xpPerLevel, _constantOffset,
                        _quadraticBase, _expBase, _expFactor,
                        _powerBase, _powerExponent,
                        maxLevel);
                case LevelCurveMode.Curve:
                    return GetXpToNextLevelFromCurve(totalXp, maxLevel);
                case LevelCurveMode.Custom:
                    return LevelCurveEvaluator.GetXpToNextLevel(
                        totalXp, LevelCurveType.Custom,
                        _xpPerLevel, _quadraticBase, _expBase, _expFactor,
                        _customEntries, maxLevel);
                default:
                    return LevelCurveEvaluator.GetXpToNextLevelByFormula(
                        totalXp, LevelFormulaType.Linear, _xpPerLevel, 0,
                        _quadraticBase, _expBase, _expFactor, _powerBase, _powerExponent, maxLevel);
            }
        }

        /// <summary>Set linear formula with fixed XP per level (tests/runtime).</summary>
        public void SetLinear(int xpPerLevel)
        {
            _mode = LevelCurveMode.Formula;
            _formulaType = LevelFormulaType.Linear;
            _xpPerLevel = xpPerLevel < 1 ? 1 : xpPerLevel;
        }

        private int EvaluateLevelFromCurve(int totalXp, int maxLevel)
        {
            if (_animationCurve == null || _animationCurve.length == 0)
            {
                return 1;
            }

            int levelLimit = GetCurveLevelLimit();
            int level = 1;
            for (int L = 1; L <= levelLimit; L++)
            {
                float required = _animationCurve.Evaluate(L);
                if ((double)required > totalXp)
                {
                    break;
                }

                level = L;
                if (maxLevel > 0 && level >= maxLevel)
                {
                    return maxLevel;
                }
            }

            if (maxLevel > 0 && level > maxLevel)
            {
                level = maxLevel;
            }

            return level;
        }

        private int GetXpToNextLevelFromCurve(int totalXp, int maxLevel)
        {
            if (_animationCurve == null || _animationCurve.length == 0)
            {
                return 0;
            }

            int currentLevel = EvaluateLevelFromCurve(totalXp, 0);
            if (currentLevel >= GetCurveLevelLimit() || (maxLevel > 0 && currentLevel >= maxLevel))
            {
                return 0;
            }

            double requiredNext = _animationCurve.Evaluate(currentLevel + 1);
            return CeilingToXp(requiredNext - totalXp);
        }

        private int GetCurveLevelLimit()
        {
            // Only integer levels inside the authored domain are reachable. Keep the existing scan cap.
            float lastKeyTime = _animationCurve[_animationCurve.length - 1].time;
            return lastKeyTime >= CurveLevelLimit ? CurveLevelLimit : lastKeyTime >= 1f ? (int)lastKeyTime : 1;
        }

        private static int CeilingToXp(double value)
        {
            if (double.IsNaN(value) || value <= 0d)
            {
                return 0;
            }

            return value >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(value);
        }

        private static LevelCurveType MapFormulaTypeToCurveType(LevelFormulaType formulaType)
        {
            return formulaType switch
            {
                LevelFormulaType.Linear => LevelCurveType.Linear,
                LevelFormulaType.LinearWithOffset => LevelCurveType.Linear,
                LevelFormulaType.Quadratic => LevelCurveType.Quadratic,
                LevelFormulaType.Exponential => LevelCurveType.Exponential,
                _ => LevelCurveType.Linear
            };
        }

        /// <summary>Cumulative XP to reach a level, rounded up and saturated at int.MaxValue (preview/debug).</summary>
        public int GetRequiredXpForLevel(int level)
        {
            if (level < 1)
            {
                return 0;
            }

            switch (_mode)
            {
                case LevelCurveMode.Formula:
                    return CeilingToXp(LevelCurveEvaluator.GetRequiredXpForLevelFormula(
                        level, _formulaType,
                        _xpPerLevel, _constantOffset,
                        _quadraticBase, _expBase, _expFactor,
                        _powerBase, _powerExponent));
                case LevelCurveMode.Curve:
                    if (_animationCurve == null || _animationCurve.length == 0)
                    {
                        return 0;
                    }

                    return CeilingToXp(_animationCurve.Evaluate(level));
                case LevelCurveMode.Custom:
                    if (TryGetDefinition(level, out LevelCurveEntry entry))
                    {
                        return entry.RequiredXp;
                    }

                    return 0;
                default:
                    return CeilingToXp(LevelCurveEvaluator.GetRequiredXpForLevelFormula(
                        level, LevelFormulaType.Linear, _xpPerLevel, 0,
                        _quadraticBase, _expBase, _expFactor, _powerBase, _powerExponent));
            }
        }

        /// <summary>Get entry by level (meaningful only for Custom).</summary>
        public bool TryGetDefinition(int level, out LevelCurveEntry entry)
        {
            if (_customEntries == null)
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < _customEntries.Count; i++)
            {
                LevelCurveEntry e = _customEntries[i];
                if (e != null && e.Level == level)
                {
                    entry = e;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }
}
