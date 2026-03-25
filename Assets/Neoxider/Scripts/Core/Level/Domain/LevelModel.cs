using System;
using System.Collections.Generic;

namespace Neo.Core.Level
{
    /// <summary>
    ///     Pure logic: holds level/XP state and applies curve evaluation. No UnityEngine.
    /// </summary>
    public sealed class LevelModel
    {
        private ILevelCurveDefinition _curveDefinition;
        private LevelCurveType _curveType = LevelCurveType.Linear;
        private List<ILevelCurveEntry> _customEntries;
        private float _expBase = 100f;
        private float _expFactor = 1.5f;
        private float _quadraticBase = 100f;
        private int _xpPerLevel = 100;

        /// <summary>Current total XP.</summary>
        public int TotalXp { get; private set; }

        /// <summary>Resolved level (from curve or set directly).</summary>
        public int CurrentLevel { get; private set; } = 1;

        /// <summary>Maximum level cap (0 = no cap).</summary>
        public int MaxLevel { get; private set; }

        /// <summary>Whether level is derived from XP (curve).</summary>
        public bool UseXp { get; private set; } = true;

        /// <summary>Whether a max level is set.</summary>
        public bool HasMaxLevel => MaxLevel > 0;

        /// <summary>XP required to reach next level (0 if at max or no curve).</summary>
        public int XpToNextLevel { get; private set; }

        public event Action<int, int> OnLevelChanged; // previousLevel, newLevel
        public event Action<int, int> OnXpGained; // added, newTotal

        public void SetUseXp(bool useXp)
        {
            UseXp = useXp;
        }

        public void SetMaxLevel(int maxLevel)
        {
            MaxLevel = maxLevel < 0 ? 0 : maxLevel;
            RecomputeLevelAndXpToNext();
        }

        /// <summary>Set curve definition (SO with Formula/Curve/Custom mode). Takes precedence over SetCurve.</summary>
        public void SetCurveDefinition(ILevelCurveDefinition definition)
        {
            _curveDefinition = definition;
            RecomputeLevelAndXpToNext();
        }

        public void SetCurve(LevelCurveType curveType, int xpPerLevel = 100, float quadraticBase = 100f,
            float expBase = 100f, float expFactor = 1.5f, IReadOnlyList<LevelCurveEntry> customEntries = null)
        {
            _curveDefinition = null;
            _curveType = curveType;
            _xpPerLevel = xpPerLevel < 1 ? 1 : xpPerLevel;
            _quadraticBase = quadraticBase < 0.01f ? 0.01f : quadraticBase;
            _expBase = expBase < 0.01f ? 0.01f : expBase;
            _expFactor = expFactor < 1.01f ? 1.01f : expFactor;
            _customEntries = customEntries == null ? null : new List<ILevelCurveEntry>(customEntries);
            RecomputeLevelAndXpToNext();
        }

        public void SetState(int totalXp, int level)
        {
            TotalXp = totalXp < 0 ? 0 : totalXp;
            if (UseXp)
            {
                RecomputeLevelAndXpToNext();
            }
            else
            {
                CurrentLevel = level < 1 ? 1 : level;
                if (MaxLevel > 0 && CurrentLevel > MaxLevel)
                {
                    CurrentLevel = MaxLevel;
                }

                XpToNextLevel = 0;
            }
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int previousLevel = CurrentLevel;
            TotalXp += amount;
            RecomputeLevelAndXpToNext();
            OnXpGained?.Invoke(amount, TotalXp);
            if (CurrentLevel != previousLevel)
            {
                OnLevelChanged?.Invoke(previousLevel, CurrentLevel);
            }
        }

        public void SetLevel(int level)
        {
            level = level < 1 ? 1 : level;
            if (MaxLevel > 0 && level > MaxLevel)
            {
                level = MaxLevel;
            }

            int previous = CurrentLevel;
            CurrentLevel = level;
            if (UseXp)
            {
                // Optionally sync totalXp to match level (e.g. set to required for this level)
                RecomputeLevelAndXpToNext();
            }
            else
            {
                XpToNextLevel = 0;
            }

            if (CurrentLevel != previous)
            {
                OnLevelChanged?.Invoke(previous, CurrentLevel);
            }
        }

        public void SetLevelDirect(int level)
        {
            level = level < 1 ? 1 : level;
            if (MaxLevel > 0 && level > MaxLevel)
            {
                level = MaxLevel;
            }

            int previous = CurrentLevel;
            CurrentLevel = level;
            XpToNextLevel = 0;
            if (CurrentLevel != previous)
            {
                OnLevelChanged?.Invoke(previous, CurrentLevel);
            }
        }

        private void RecomputeLevelAndXpToNext()
        {
            if (!UseXp)
            {
                return;
            }

            if (_curveDefinition != null)
            {
                CurrentLevel = _curveDefinition.EvaluateLevel(TotalXp, MaxLevel);
                XpToNextLevel = _curveDefinition.GetXpToNextLevel(TotalXp, MaxLevel);
                return;
            }

            int newLevel = LevelCurveEvaluator.EvaluateLevel(
                TotalXp,
                _curveType,
                _xpPerLevel,
                _quadraticBase,
                _expBase,
                _expFactor,
                _customEntries,
                MaxLevel);

            CurrentLevel = newLevel;
            XpToNextLevel = LevelCurveEvaluator.GetXpToNextLevel(
                TotalXp,
                _curveType,
                _xpPerLevel,
                _quadraticBase,
                _expBase,
                _expFactor,
                _customEntries,
                MaxLevel);
        }
    }
}
