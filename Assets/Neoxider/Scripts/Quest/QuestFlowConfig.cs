using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Конфиг потоков квестов: последовательные цепочки и отдельные (standalone) квесты.
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

        public IReadOnlyList<QuestChain> Chains => _chains;
        public IReadOnlyList<QuestConfig> StandaloneQuests => _standaloneQuests;

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

                    if (questIndex > 0)
                    {
                        previousQuest = chain.Quests[questIndex - 1];
                    }

                    return true;
                }
            }

            return false;
        }

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

            if (manager.GetState(quest) != null)
            {
                reason = "Already accepted/completed/failed.";
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

            public string ChainId => _chainId;
            public string DisplayName => _displayName;
            public bool StrictOrder => _strictOrder;
            public IReadOnlyList<QuestConfig> Quests => _quests;
        }
    }
}
