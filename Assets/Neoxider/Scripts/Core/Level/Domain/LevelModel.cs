using System;
using System.Collections.Generic;

namespace Neo.Core.Level
{
    /// <summary>
    ///     Pure logic: holds level/XP state and applies curve evaluation. No UnityEngine.
    /// </summary>
    public sealed class LevelModel
    {
        private int _totalXp;
        private int _currentLevel = 1;
        private int _maxLevel; // 0 = no cap
        private bool _useXp = true;
        private ILevelCurveDefinition _curveDefinition;
        private LevelCurveType _curveType = LevelCurveType.Linear;
        private int _xpPerLevel = 100;
        private float _quadraticBase = 100f;
        private float _expBase = 100f;
        private float _expFactor = 1.5f;
        private List<ILevelCurveEntry> _customEntries;

        /// <summary>Current total XP.</summary>
        public int TotalXp => _totalXp;

        /// <summary>Resolved level (from curve or set directly).</summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>Maximum level cap (0 = no cap).</summary>
        public int MaxLevel => _maxLevel;

        /// <summary>Whether level is derived from XP (curve).</summary>
        public bool UseXp => _useXp;

        /// <summary>Whether a max level is set.</summary>
        public bool HasMaxLevel => _maxLevel > 0;

        /// <summary>XP required to reach next level (0 if at max or no curve).</summary>
        public int XpToNextLevel { get; private set; }

        public event Action<int, int> OnLevelChanged; // previousLevel, newLevel
        public event Action<int, int> OnXpGained;      // added, newTotal

        public void SetUseXp(bool useXp)
        {
            _useXp = useXp;
        }

        public void SetMaxLevel(int maxLevel)
        {
            _maxLevel = maxLevel < 0 ? 0 : maxLevel;
            RecomputeLevelAndXpToNext();
        }

        /// <summary>Задать определение кривой (SO с режимом Formula/Curve/Custom). Имеет приоритет над SetCurve.</summary>
        public void SetCurveDefinition(ILevelCurveDefinition definition)
        {
            _curveDefinition = definition;
            RecomputeLevelAndXpToNext();
        }

        public void SetCurve(LevelCurveType curveType, int xpPerLevel = 100, float quadraticBase = 100f, float expBase = 100f, float expFactor = 1.5f, IReadOnlyList<LevelCurveEntry> customEntries = null)
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
            _totalXp = totalXp < 0 ? 0 : totalXp;
            if (_useXp)
            {
                RecomputeLevelAndXpToNext();
            }
            else
            {
                _currentLevel = level < 1 ? 1 : level;
                if (_maxLevel > 0 && _currentLevel > _maxLevel)
                {
                    _currentLevel = _maxLevel;
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

            int previousLevel = _currentLevel;
            _totalXp += amount;
            RecomputeLevelAndXpToNext();
            OnXpGained?.Invoke(amount, _totalXp);
            if (_currentLevel != previousLevel)
            {
                OnLevelChanged?.Invoke(previousLevel, _currentLevel);
            }
        }

        public void SetLevel(int level)
        {
            level = level < 1 ? 1 : level;
            if (_maxLevel > 0 && level > _maxLevel)
            {
                level = _maxLevel;
            }

            int previous = _currentLevel;
            _currentLevel = level;
            if (_useXp)
            {
                // Optionally sync totalXp to match level (e.g. set to required for this level)
                RecomputeLevelAndXpToNext();
            }
            else
            {
                XpToNextLevel = 0;
            }

            if (_currentLevel != previous)
            {
                OnLevelChanged?.Invoke(previous, _currentLevel);
            }
        }

        public void SetLevelDirect(int level)
        {
            level = level < 1 ? 1 : level;
            if (_maxLevel > 0 && level > _maxLevel)
            {
                level = _maxLevel;
            }

            int previous = _currentLevel;
            _currentLevel = level;
            XpToNextLevel = 0;
            if (_currentLevel != previous)
            {
                OnLevelChanged?.Invoke(previous, _currentLevel);
            }
        }

        private void RecomputeLevelAndXpToNext()
        {
            if (!_useXp)
            {
                return;
            }

            if (_curveDefinition != null)
            {
                _currentLevel = _curveDefinition.EvaluateLevel(_totalXp, _maxLevel);
                XpToNextLevel = _curveDefinition.GetXpToNextLevel(_totalXp, _maxLevel);
                return;
            }

            int newLevel = LevelCurveEvaluator.EvaluateLevel(
                _totalXp,
                _curveType,
                _xpPerLevel,
                _quadraticBase,
                _expBase,
                _expFactor,
                _customEntries,
                _maxLevel);

            _currentLevel = newLevel;
            XpToNextLevel = LevelCurveEvaluator.GetXpToNextLevel(
                _totalXp,
                _curveType,
                _xpPerLevel,
                _quadraticBase,
                _expBase,
                _expFactor,
                _customEntries,
                _maxLevel);
        }
    }
}
