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
        public int XpToNextLevel { get; private set; } = 100;

        /// <summary>
        ///     Progress through the current level in [0, 1]: the share of the XP between the current level's threshold
        ///     and the next one that is already earned. 1 at the max level or without an XP curve. For XP bars.
        /// </summary>
        public float LevelProgress01
        {
            get
            {
                if (!UseXp || XpToNextLevel <= 0)
                {
                    return 1f;
                }

                int start = GetCurrentLevelStartXp();
                long span = (long)TotalXp + XpToNextLevel - start;
                return span <= 0 ? 1f : Math.Min(1f, Math.Max(0f, (float)((TotalXp - start) / (double)span)));
            }
        }

        private int _cachedStartLevel = -1;
        private int _cachedStartXp;
        private int _cachedStartTotal = -1;

        public event Action<int, int> OnLevelChanged; // WHY: args are (previousLevel, newLevel)
        public event Action<int, int> OnXpGained; // WHY: args are (added, newTotal)

        public void SetUseXp(bool useXp)
        {
            UseXp = useXp;
            if (UseXp)
            {
                RecomputeLevelAndXpToNext();
            }
            else
            {
                XpToNextLevel = 0;
            }
        }

        public void SetMaxLevel(int maxLevel)
        {
            MaxLevel = maxLevel < 0 ? 0 : maxLevel;
            if (!UseXp)
            {
                // WHY: RecomputeLevelAndXpToNext is XP-only; a lowered cap must still clamp a direct level.
                if (MaxLevel > 0 && CurrentLevel > MaxLevel)
                {
                    int previous = CurrentLevel;
                    CurrentLevel = MaxLevel;
                    OnLevelChanged?.Invoke(previous, CurrentLevel);
                }

                return;
            }

            RecomputeLevelAndXpToNext();
        }

        /// <summary>Set curve definition (SO with Formula/Curve/Custom mode). Takes precedence over SetCurve.</summary>
        public void SetCurveDefinition(ILevelCurveDefinition definition)
        {
            _curveDefinition = definition;
            RecomputeLevelAndXpToNext();
        }

        public void SetCurve(LevelCurveType curveType, int xpPerLevel = 100, float quadraticBase = 100f,
            float expBase = 100f, float expFactor = 1.5f, IReadOnlyList<ILevelCurveEntry> customEntries = null)
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

            int added = Math.Min(amount, int.MaxValue - TotalXp);
            if (added == 0)
            {
                return;
            }

            int previousLevel = CurrentLevel;
            TotalXp += added;
            RecomputeLevelAndXpToNext();
            int resultingLevel = CurrentLevel;
            OnXpGained?.Invoke(added, TotalXp);
            if (resultingLevel != previousLevel)
            {
                OnLevelChanged?.Invoke(previousLevel, resultingLevel);
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
                TotalXp = FindMinimumXpForLevel(level);
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

        private int FindMinimumXpForLevel(int level)
        {
            if (level <= 1)
            {
                return 0;
            }

            int high = Math.Max(1, TotalXp);
            while (EvaluateLevelForXp(high) < level && high < int.MaxValue)
            {
                high = high > int.MaxValue / 2 ? int.MaxValue : high * 2;
            }

            if (EvaluateLevelForXp(high) < level)
            {
                // WHY: requested level is unreachable on this curve (e.g. Custom entries top out below it);
                // search for the highest achievable level instead of converging to int.MaxValue XP.
                level = EvaluateLevelForXp(high);
                if (level <= 1)
                {
                    return 0;
                }
            }

            int low = 0;
            while (low < high)
            {
                int mid = low + (high - low) / 2;
                if (EvaluateLevelForXp(mid) >= level)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return low;
        }

        private int EvaluateLevelForXp(int totalXp)
        {
            if (_curveDefinition != null)
            {
                return _curveDefinition.EvaluateLevel(totalXp, MaxLevel);
            }

            return LevelCurveEvaluator.EvaluateLevel(
                totalXp,
                _curveType,
                _xpPerLevel,
                _quadraticBase,
                _expBase,
                _expFactor,
                _customEntries,
                MaxLevel);
        }

        /// <summary>Sets the level directly, ignoring the curve (no XP synchronization).</summary>
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

        // WHY: the curve only maps XP to a level, so the threshold of the current level is found by a binary search
        // over [0, TotalXp] (levels never drop as XP grows). Cached per level and total, so a bar polling it is cheap.
        private int GetCurrentLevelStartXp()
        {
            if (_cachedStartLevel == CurrentLevel && _cachedStartTotal == TotalXp)
            {
                return _cachedStartXp;
            }

            int low = 0;
            int high = TotalXp;
            while (low < high)
            {
                int mid = low + (high - low) / 2;
                if (EvaluateLevelAt(mid) >= CurrentLevel)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            _cachedStartLevel = CurrentLevel;
            _cachedStartTotal = TotalXp;
            _cachedStartXp = low;
            return low;
        }

        private int EvaluateLevelAt(int totalXp)
        {
            if (_curveDefinition != null)
            {
                return _curveDefinition.EvaluateLevel(totalXp, MaxLevel);
            }

            return LevelCurveEvaluator.EvaluateLevel(totalXp, _curveType, _xpPerLevel, _quadraticBase, _expBase,
                _expFactor, _customEntries, MaxLevel);
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
