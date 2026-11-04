using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Quest flow configuration: sequential chains and standalone quests.
    /// </summary>
    [NeoDoc("Quest/QuestFlowConfig.md")]
    [CreateAssetMenu(fileName = "QuestFlowConfig", menuName = "Neoxider/Quest/Quest Flow Config")]
    public class QuestFlowConfig : ScriptableObject
    {
        [Header("Sequential Chains")]
        [Tooltip("Quest chains. Each chain can enforce strict order if needed.")]
        [SerializeField]
        private List<QuestChain> _chains = new();

        [Header("Standalone Quests")] [Tooltip("Independent quests not tied to chain progression.")] [SerializeField]
        private List<QuestConfig> _standaloneQuests = new();

        /// <summary>Configured quest chains in authoring order.</summary>
        public IReadOnlyList<QuestChain> Chains => _chains;

        /// <summary>Quests that are not gated by any chain.</summary>
        public IReadOnlyList<QuestConfig> StandaloneQuests => _standaloneQuests;

        /// <summary>
        ///     Finds the first strict-order chain containing <paramref name="quest" /> and returns its nearest
        ///     non-null predecessor. Returns true when the quest belongs to a strict chain; previousQuest is null for
        ///     the first quest of that chain.
        /// </summary>
        public bool TryGetPreviousInStrictChain(QuestConfig quest, out QuestConfig previousQuest)
        {
            previousQuest = null;
            if (quest == null)
            {
                return false;
            }

            for (int chainIndex = 0; chainIndex < _chains.Count; chainIndex++)
            {
                QuestChain chain = _chains[chainIndex];
                if (chain == null || !chain.StrictOrder || chain.Quests == null || chain.Quests.Count == 0)
                {
                    continue;
                }

                for (int questIndex = 0; questIndex < chain.Quests.Count; questIndex++)
                {
                    QuestConfig inChain = chain.Quests[questIndex];
                    if (inChain != quest)
                    {
                        continue;
                    }

                    for (int previousIndex = questIndex - 1; previousIndex >= 0; previousIndex--)
                    {
                        if (chain.Quests[previousIndex] != null)
                        {
                            previousQuest = chain.Quests[previousIndex];
                            break;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Combines <see cref="QuestManager.CanAcceptQuest" /> (id, existing state, start conditions) with the
        ///     strict-chain rule: the predecessor must be Completed. Does not accept the quest.
        /// </summary>
        public bool CanAcceptQuest(QuestManager manager, QuestConfig quest, out string reason)
        {
            reason = null;
            if (quest == null)
            {
                reason = "Quest is null.";
                return false;
            }

            if (manager == null)
            {
                reason = "QuestManager is null.";
                return false;
            }

            if (!manager.CanAcceptQuest(quest, out reason))
            {
                return false;
            }

            return IsChainSatisfied(manager, quest, out reason);
        }

        /// <summary>
        ///     Checks only the strict-chain rule (the nearest non-null predecessor must be Completed), without
        ///     evaluating start conditions. Pair it with <see cref="QuestManager.TryAcceptQuest(QuestConfig,out string)" />
        ///     so user predicates run once per acceptance attempt.
        /// </summary>
        public bool IsChainSatisfied(QuestManager manager, QuestConfig quest, out string reason)
        {
            reason = null;
            if (quest == null)
            {
                reason = "Quest is null.";
                return false;
            }

            if (manager == null)
            {
                reason = "QuestManager is null.";
                return false;
            }

            bool hasChainRule = TryGetPreviousInStrictChain(quest, out QuestConfig previousQuest);
            if (!hasChainRule || previousQuest == null)
            {
                return true;
            }

            if (!manager.IsCompleted(previousQuest))
            {
                reason = $"Locked by previous quest: {previousQuest.Id}";
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Returns every distinct non-null quest: chain quests in chain order first, then standalone quests.
        ///     Allocates a new list; cache the result for UI that rebuilds often.
        /// </summary>
        public List<QuestConfig> BuildOrderedQuestList()
        {
            List<QuestConfig> ordered = new();
            HashSet<QuestConfig> added = new();

            for (int i = 0; i < _chains.Count; i++)
            {
                QuestChain chain = _chains[i];
                if (chain?.Quests == null)
                {
                    continue;
                }

                for (int j = 0; j < chain.Quests.Count; j++)
                {
                    QuestConfig quest = chain.Quests[j];
                    if (quest == null || added.Contains(quest))
                    {
                        continue;
                    }

                    ordered.Add(quest);
                    added.Add(quest);
                }
            }

            for (int i = 0; i < _standaloneQuests.Count; i++)
            {
                QuestConfig quest = _standaloneQuests[i];
                if (quest == null || added.Contains(quest))
                {
                    continue;
                }

                ordered.Add(quest);
                added.Add(quest);
            }

            return ordered;
        }

        /// <summary>Ordered group of quests; with StrictOrder each quest requires its predecessor to be Completed.</summary>
        [Serializable]
        public class QuestChain
        {
            [Header("Chain Settings")] [Tooltip("Unique chain identifier used for tooling/debugging.")] [SerializeField]
            private string _chainId = "main";

            [Tooltip("Display name used in UI and editor.")] [SerializeField]
            private string _displayName = "Main Chain";

            [Tooltip("If enabled, quests in this chain must be completed strictly in listed order.")] [SerializeField]
            private bool _strictOrder = true;

            [Tooltip("Ordered list of quests in this chain.")] [SerializeField]
            private List<QuestConfig> _quests = new();

            /// <summary>Chain identifier used for tooling/debugging.</summary>
            public string ChainId => _chainId;

            /// <summary>Display name for UI and editor.</summary>
            public string DisplayName => _displayName;

            /// <summary>Whether quests must be completed in listed order.</summary>
            public bool StrictOrder => _strictOrder;

            /// <summary>Ordered quests of this chain (may contain null slots, which are skipped).</summary>
            public IReadOnlyList<QuestConfig> Quests => _quests;
        }
    }
}
