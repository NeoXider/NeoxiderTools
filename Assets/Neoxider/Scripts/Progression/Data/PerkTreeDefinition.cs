using System;
using System.Collections.Generic;
using Neo.Condition;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    /// Defines a perk entry that can be purchased with perk points.
    /// </summary>
    [Serializable]
    public sealed class PerkDefinition
    {
        [SerializeField] private string _id = "perk-id";
        [SerializeField] private string _displayName = "Perk";
        [SerializeField] [TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _purchasedByDefault;
        [SerializeField] [Min(0)] private int _cost = 1;
        [SerializeField] [Min(1)] private int _requiredLevel = 1;
        [SerializeField] private List<string> _prerequisitePerkIds = new();
        [SerializeField] private List<string> _requiredUnlockNodeIds = new();
        [SerializeField] private List<ConditionEntry> _conditions = new();
        [SerializeField] private List<ProgressionReward> _rewards = new();

        /// <summary>
        /// Gets the stable perk identifier.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// Gets the UI-facing perk name.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Gets the optional perk description.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Gets the optional perk icon.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Gets whether the perk is granted by default.
        /// </summary>
        public bool PurchasedByDefault => _purchasedByDefault;

        /// <summary>
        /// Gets the perk point cost.
        /// </summary>
        public int Cost => _cost;

        /// <summary>
        /// Gets the minimum required player level.
        /// </summary>
        public int RequiredLevel => _requiredLevel;

        /// <summary>
        /// Gets the prerequisite perk identifiers.
        /// </summary>
        public IReadOnlyList<string> PrerequisitePerkIds => _prerequisitePerkIds;

        /// <summary>
        /// Gets the unlock node identifiers that must already be unlocked.
        /// </summary>
        public IReadOnlyList<string> RequiredUnlockNodeIds => _requiredUnlockNodeIds;

        /// <summary>
        /// Gets the extra condition evaluators that must pass before buying the perk.
        /// </summary>
        public IReadOnlyList<ConditionEntry> Conditions => _conditions;

        /// <summary>
        /// Gets the rewards granted once when the perk is purchased.
        /// </summary>
        public IReadOnlyList<ProgressionReward> Rewards => _rewards;
    }

    /// <summary>
    /// Stores perk definitions for the progression system.
    /// </summary>
    [CreateAssetMenu(fileName = "Perk Tree Definition", menuName = "Neoxider/Progression/Perk Tree Definition")]
    public sealed class PerkTreeDefinition : ScriptableObject
    {
        [SerializeField] private List<PerkDefinition> _perks = new();

        /// <summary>
        /// Gets the configured perk entries.
        /// </summary>
        public IReadOnlyList<PerkDefinition> Perks => _perks;

        /// <summary>
        /// Tries to get a perk by identifier.
        /// </summary>
        public bool TryGetPerk(string perkId, out PerkDefinition perk)
        {
            for (int i = 0; i < _perks.Count; i++)
            {
                PerkDefinition candidate = _perks[i];
                if (candidate != null && string.Equals(candidate.Id, perkId, StringComparison.Ordinal))
                {
                    perk = candidate;
                    return true;
                }
            }

            perk = null;
            return false;
        }

        /// <summary>
        /// Validates identifiers, references, and graph cycles.
        /// </summary>
        public IReadOnlyList<string> ValidateDefinition()
        {
            List<string> issues = new();
            Dictionary<string, PerkDefinition> perkMap = new(StringComparer.Ordinal);

            for (int i = 0; i < _perks.Count; i++)
            {
                PerkDefinition perk = _perks[i];
                if (perk == null)
                {
                    issues.Add($"Perks[{i}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(perk.Id))
                {
                    issues.Add($"Perks[{i}] has an empty id.");
                    continue;
                }

                if (!perkMap.TryAdd(perk.Id, perk))
                {
                    issues.Add($"Duplicate perk id '{perk.Id}'.");
                }
            }

            foreach (KeyValuePair<string, PerkDefinition> pair in perkMap)
            {
                IReadOnlyList<string> prerequisites = pair.Value.PrerequisitePerkIds;
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    string prerequisiteId = prerequisites[i];
                    if (string.IsNullOrWhiteSpace(prerequisiteId))
                    {
                        issues.Add($"Perk '{pair.Key}' contains an empty prerequisite id.");
                        continue;
                    }

                    if (!perkMap.ContainsKey(prerequisiteId))
                    {
                        issues.Add($"Perk '{pair.Key}' references missing prerequisite perk '{prerequisiteId}'.");
                    }
                }
            }

            HashSet<string> visiting = new(StringComparer.Ordinal);
            HashSet<string> visited = new(StringComparer.Ordinal);
            foreach (string perkId in perkMap.Keys)
            {
                ValidateCycles(perkId, perkMap, visiting, visited, issues);
            }

            return issues;
        }

        private static void ValidateCycles(string perkId,
            IReadOnlyDictionary<string, PerkDefinition> perkMap,
            ISet<string> visiting,
            ISet<string> visited,
            ICollection<string> issues)
        {
            if (visited.Contains(perkId))
            {
                return;
            }

            if (!visiting.Add(perkId))
            {
                issues.Add($"Perk tree cycle detected at perk '{perkId}'.");
                return;
            }

            if (perkMap.TryGetValue(perkId, out PerkDefinition perk))
            {
                IReadOnlyList<string> prerequisites = perk.PrerequisitePerkIds;
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    string prerequisiteId = prerequisites[i];
                    if (!string.IsNullOrWhiteSpace(prerequisiteId) && perkMap.ContainsKey(prerequisiteId))
                    {
                        ValidateCycles(prerequisiteId, perkMap, visiting, visited, issues);
                    }
                }
            }

            visiting.Remove(perkId);
            visited.Add(perkId);
        }

        private void OnValidate()
        {
            _perks.Sort((left, right) => string.Compare(left?.Id, right?.Id, StringComparison.Ordinal));
        }
    }
}
