using System;
using System.Collections.Generic;
using Neo.Condition;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    ///     Defines a progression unlock node.
    /// </summary>
    [Serializable]
    public sealed class UnlockNodeDefinition
    {
        [SerializeField] private string _id = "node-id";
        [SerializeField] private string _displayName = "Unlock Node";
        [SerializeField] [TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _unlockedByDefault;
        [SerializeField] [Min(1)] private int _requiredLevel = 1;
        [SerializeField] private List<string> _prerequisiteNodeIds = new();
        [SerializeField] private List<ConditionEntry> _conditions = new();
        [SerializeField] private List<ProgressionReward> _rewards = new();

        /// <summary>
        ///     Gets the stable node identifier.
        /// </summary>
        public string Id => _id;

        /// <summary>
        ///     Gets the UI-facing node title.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        ///     Gets the optional node description.
        /// </summary>
        public string Description => _description;

        /// <summary>
        ///     Gets the optional node icon.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        ///     Gets whether the node should be available by default.
        /// </summary>
        public bool UnlockedByDefault => _unlockedByDefault;

        /// <summary>
        ///     Gets the minimum level required to unlock the node.
        /// </summary>
        public int RequiredLevel => _requiredLevel;

        /// <summary>
        ///     Gets the prerequisite node identifiers.
        /// </summary>
        public IReadOnlyList<string> PrerequisiteNodeIds => _prerequisiteNodeIds;

        /// <summary>
        ///     Gets the extra condition evaluators that must pass before unlocking.
        /// </summary>
        public IReadOnlyList<ConditionEntry> Conditions => _conditions;

        /// <summary>
        ///     Gets the rewards granted when the node is unlocked.
        /// </summary>
        public IReadOnlyList<ProgressionReward> Rewards => _rewards;
    }

    /// <summary>
    ///     Stores unlock node definitions for the progression system.
    /// </summary>
    [CreateAssetMenu(fileName = "Unlock Tree Definition", menuName = "Neoxider/Progression/Unlock Tree Definition")]
    public sealed class UnlockTreeDefinition : ScriptableObject
    {
        [SerializeField] private List<UnlockNodeDefinition> _nodes = new();

        /// <summary>
        ///     Gets the configured unlock nodes.
        /// </summary>
        public IReadOnlyList<UnlockNodeDefinition> Nodes => _nodes;

        private void OnValidate()
        {
            _nodes.Sort((left, right) => string.Compare(left?.Id, right?.Id, StringComparison.Ordinal));
        }

        /// <summary>
        ///     Tries to get a node by identifier.
        /// </summary>
        public bool TryGetNode(string nodeId, out UnlockNodeDefinition node)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                UnlockNodeDefinition candidate = _nodes[i];
                if (candidate != null && string.Equals(candidate.Id, nodeId, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            node = null;
            return false;
        }

        /// <summary>
        ///     Validates identifiers, references, and graph cycles.
        /// </summary>
        public IReadOnlyList<string> ValidateDefinition()
        {
            List<string> issues = new();
            Dictionary<string, UnlockNodeDefinition> nodeMap = new(StringComparer.Ordinal);

            for (int i = 0; i < _nodes.Count; i++)
            {
                UnlockNodeDefinition node = _nodes[i];
                if (node == null)
                {
                    issues.Add($"Nodes[{i}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    issues.Add($"Nodes[{i}] has an empty id.");
                    continue;
                }

                if (!nodeMap.TryAdd(node.Id, node))
                {
                    issues.Add($"Duplicate unlock node id '{node.Id}'.");
                }
            }

            foreach (KeyValuePair<string, UnlockNodeDefinition> pair in nodeMap)
            {
                IReadOnlyList<string> prerequisites = pair.Value.PrerequisiteNodeIds;
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    string prerequisiteId = prerequisites[i];
                    if (string.IsNullOrWhiteSpace(prerequisiteId))
                    {
                        issues.Add($"Unlock node '{pair.Key}' contains an empty prerequisite id.");
                        continue;
                    }

                    if (!nodeMap.ContainsKey(prerequisiteId))
                    {
                        issues.Add($"Unlock node '{pair.Key}' references missing prerequisite '{prerequisiteId}'.");
                    }
                }
            }

            HashSet<string> visiting = new(StringComparer.Ordinal);
            HashSet<string> visited = new(StringComparer.Ordinal);
            foreach (string nodeId in nodeMap.Keys)
            {
                ValidateCycles(nodeId, nodeMap, visiting, visited, issues);
            }

            return issues;
        }

        private static void ValidateCycles(string nodeId,
            IReadOnlyDictionary<string, UnlockNodeDefinition> nodeMap,
            ISet<string> visiting,
            ISet<string> visited,
            ICollection<string> issues)
        {
            if (visited.Contains(nodeId))
            {
                return;
            }

            if (!visiting.Add(nodeId))
            {
                issues.Add($"Unlock tree cycle detected at node '{nodeId}'.");
                return;
            }

            if (nodeMap.TryGetValue(nodeId, out UnlockNodeDefinition node))
            {
                IReadOnlyList<string> prerequisites = node.PrerequisiteNodeIds;
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    string prerequisiteId = prerequisites[i];
                    if (!string.IsNullOrWhiteSpace(prerequisiteId) && nodeMap.ContainsKey(prerequisiteId))
                    {
                        ValidateCycles(prerequisiteId, nodeMap, visiting, visited, issues);
                    }
                }
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
        }
    }
}
