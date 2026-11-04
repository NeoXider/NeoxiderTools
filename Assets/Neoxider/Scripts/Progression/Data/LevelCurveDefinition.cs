using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    ///     Defines a level entry with the XP threshold, awarded perk points, and optional rewards.
    /// </summary>
    [Serializable]
    public sealed class ProgressionLevelDefinition
    {
        [Tooltip("Core level this entry rewards. Keep levels unique and ascending.")] [SerializeField] [Min(1)]
        private int _level = 1;

        [Tooltip("Legacy cumulative XP threshold used only by EvaluateLevel/GetXpToNextLevel helpers.")]
        [SerializeField]
        [Min(0)]
        private int _requiredXp;

        [Tooltip("Perk points granted once when this level is reached.")] [SerializeField] [Min(0)]
        private int _grantedPerkPoints;

        [Tooltip("Rewards dispatched once when this level is reached; premium entries need ActivatePremium().")]
        [SerializeField]
        private List<ProgressionReward> _rewards = new();

        /// <summary>
        ///     Gets the numeric level represented by this entry.
        /// </summary>
        public int Level => _level;

        /// <summary>
        ///     Gets the cumulative XP required to reach this level.
        /// </summary>
        public int RequiredXp => _requiredXp;

        /// <summary>
        ///     Gets the number of perk points granted when this level is reached.
        /// </summary>
        public int GrantedPerkPoints => _grantedPerkPoints;

        /// <summary>
        ///     Gets the rewards that are dispatched once when this level is reached.
        /// </summary>
        public IReadOnlyList<ProgressionReward> Rewards => _rewards;
    }

    /// <summary>
    ///     Stores level rewards for the progression system. The actual XP-to-level curve is owned by
    ///     <see cref="Neo.Core.Level.LevelCurveDefinition" />.
    /// </summary>
    [CreateAssetMenu(fileName = "Level Reward Track", menuName = "Neoxider/Progression/Level Reward Track")]
    [NeoDoc("Progression/Data/LevelCurveDefinition.md")]
    public sealed class LevelCurveDefinition : ScriptableObject
    {
        [SerializeField] private List<ProgressionLevelDefinition> _levels = new();

        /// <summary>
        ///     Gets the ordered level entries.
        /// </summary>
        public IReadOnlyList<ProgressionLevelDefinition> Levels => _levels;

        private void OnValidate()
        {
            _levels.Sort((left, right) =>
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                int levelCompare = left.Level.CompareTo(right.Level);
                return levelCompare != 0 ? levelCompare : left.RequiredXp.CompareTo(right.RequiredXp);
            });
        }

        /// <summary>
        ///     Evaluates the highest reachable level for the supplied XP amount.
        ///     Prefer the Core LevelComponent for runtime XP/level calculations.
        /// </summary>
        public int EvaluateLevel(int totalXp)
        {
            int resolvedLevel = 1;
            for (int i = 0; i < _levels.Count; i++)
            {
                ProgressionLevelDefinition levelDefinition = _levels[i];
                if (levelDefinition == null)
                {
                    continue;
                }

                if (totalXp < levelDefinition.RequiredXp)
                {
                    break;
                }

                resolvedLevel = Mathf.Max(resolvedLevel, levelDefinition.Level);
            }

            return resolvedLevel;
        }

        /// <summary>
        ///     Tries to get the definition for the specified level.
        /// </summary>
        public bool TryGetDefinition(int level, out ProgressionLevelDefinition definition)
        {
            for (int i = 0; i < _levels.Count; i++)
            {
                ProgressionLevelDefinition candidate = _levels[i];
                if (candidate != null && candidate.Level == level)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        /// <summary>
        ///     Finds the entry with the smallest level in the range (<paramref name="afterLevel" />,
        ///     <paramref name="maxLevel" />]. For duplicate levels the first entry wins, matching
        ///     <see cref="TryGetDefinition" />. Each call scans all entries (O(entries)). Lets callers walk only defined
        ///     levels instead of every numeric level, so the cost of a level jump does not depend on the size of the
        ///     gap. A walk built from repeated calls is O(entries^2); <see cref="ProgressionManager" /> walks an
        ///     ascending track (see <see cref="IsAscending" />) in one pass instead.
        /// </summary>
        public bool TryGetNextDefinition(int afterLevel, int maxLevel, out ProgressionLevelDefinition definition)
        {
            definition = null;
            for (int i = 0; i < _levels.Count; i++)
            {
                ProgressionLevelDefinition candidate = _levels[i];
                if (candidate == null || candidate.Level <= afterLevel || candidate.Level > maxLevel)
                {
                    continue;
                }

                if (definition == null || candidate.Level < definition.Level)
                {
                    definition = candidate;
                }
            }

            return definition != null;
        }

        /// <summary>
        ///     Whether the non-null entries are ordered by non-decreasing level, which is the order the Inspector keeps
        ///     (<c>OnValidate</c> sorts the list). On such a track the first entry above a level is also the smallest
        ///     one, so a walk can resume from its previous position. O(entries).
        /// </summary>
        public bool IsAscending()
        {
            int previousLevel = int.MinValue;
            for (int i = 0; i < _levels.Count; i++)
            {
                ProgressionLevelDefinition candidate = _levels[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.Level < previousLevel)
                {
                    return false;
                }

                previousLevel = candidate.Level;
            }

            return true;
        }

        /// <summary>
        ///     Returns the remaining XP needed to reach the next defined level.
        ///     Prefer the Core LevelComponent for runtime XP/level calculations.
        /// </summary>
        public int GetXpToNextLevel(int totalXp)
        {
            totalXp = Mathf.Max(0, totalXp);
            for (int i = 0; i < _levels.Count; i++)
            {
                ProgressionLevelDefinition candidate = _levels[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.RequiredXp > totalXp)
                {
                    return candidate.RequiredXp - totalXp;
                }
            }

            return 0;
        }

        /// <summary>
        ///     Validates the reward track definition and returns human-readable issues.
        /// </summary>
        public IReadOnlyList<string> ValidateDefinition()
        {
            List<string> issues = new();
            HashSet<int> seenLevels = new();
            int previousLevel = 0;
            int previousXp = -1;

            for (int i = 0; i < _levels.Count; i++)
            {
                ProgressionLevelDefinition levelDefinition = _levels[i];
                if (levelDefinition == null)
                {
                    issues.Add($"Levels[{i}] is null.");
                    continue;
                }

                if (!seenLevels.Add(levelDefinition.Level))
                {
                    issues.Add($"Duplicate level value '{levelDefinition.Level}'.");
                }

                if (levelDefinition.Level <= previousLevel)
                {
                    issues.Add($"Level '{levelDefinition.Level}' must be greater than the previous level.");
                }

                if (levelDefinition.RequiredXp < previousXp)
                {
                    issues.Add($"Required XP for level '{levelDefinition.Level}' must be ascending.");
                }

                previousLevel = levelDefinition.Level;
                previousXp = levelDefinition.RequiredXp;
            }

            if (_levels.Count > 0 && _levels[0] != null && _levels[0].RequiredXp != 0)
            {
                issues.Add("The first defined level should usually start at 0 XP.");
            }

            return issues;
        }
    }
}
