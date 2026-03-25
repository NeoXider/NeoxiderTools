using System;
using System.Collections.Generic;
using Neo.Condition;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Quest
{
    /// <summary>
    ///     Single entry point for quests: state registry, accept/complete objectives, start-condition checks via
    ///     NeoCondition.
    ///     Singleton: quest registry, AcceptQuest/CompleteObjective, StartConditions evaluation. UnityEvent hooks and
    ///     QuestConfig overloads for code. See Quest/QuestManager.md.
    /// </summary>
    [NeoDoc("Quest/QuestManager.md")]
    [CreateFromMenu("Neoxider/Quest/QuestManager")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestManager))]
    public class QuestManager : Singleton<QuestManager>
    {
        [Header("Context")]
        [Tooltip(
            "Object passed to ConditionEntry.Evaluate(context) when checking StartConditions. Usually the player or a world manager.")]
        [SerializeField]
        private GameObject _conditionContext;

        [Header("Known Quests")]
        [Tooltip("Quest configs used to resolve questId -> QuestConfig in AcceptQuest(string).")]
        [SerializeField]
        private List<QuestConfig> _knownQuests = new();

        [Header("Events")] [Tooltip("Invoked when a quest is accepted (passes questId).")] [SerializeField]
        private UnityEvent<string> _onQuestAccepted = new();

        [Tooltip("Invoked when objective progress changes (questId, objectiveIndex, currentCount).")] [SerializeField]
        private UnityEvent<string, int, int> _onObjectiveProgress = new();

        [Tooltip("Invoked when an objective becomes completed (questId, objectiveIndex).")] [SerializeField]
        private UnityEvent<string, int> _onObjectiveCompleted = new();

        [Tooltip("Invoked when a quest is completed (all objectives done). Passes questId.")] [SerializeField]
        private UnityEvent<string> _onQuestCompleted = new();

        [Tooltip("Invoked when a quest fails (questId).")] [SerializeField]
        private UnityEvent<string> _onQuestFailed = new();

        [Tooltip("Invoked for any quest acceptance (no args, useful for simple bindings).")] [SerializeField]
        private UnityEvent _onAnyQuestAccepted = new();

        [Tooltip("Invoked for any quest completion (no args).")] [SerializeField]
        private UnityEvent _onAnyQuestCompleted = new();

        [Header("Editor (Inspector Buttons)")]
        [Tooltip("QuestId used by the editor buttons for AcceptQuest / CompleteObjective.")]
        [SerializeField]
        private string _editorQuestId = "";

        [Tooltip("Objective index used by the editor CompleteObjective button.")] [SerializeField]
        private int _editorObjectiveIndex;

        private readonly List<QuestState> _states = new();

        /// <summary>Context for start-condition checks (ConditionEntry.Evaluate).</summary>
        public GameObject ConditionContext
        {
            get => _conditionContext;
            set => _conditionContext = value;
        }

        /// <summary>Singleton access alias for backwards compatibility.</summary>
        public static QuestManager Instance => I;

        /// <summary>Event: quest accepted (questId).</summary>
        public UnityEvent<string> OnQuestAccepted => _onQuestAccepted;

        /// <summary>Event: objective progress (questId, objectiveIndex, currentCount).</summary>
        public UnityEvent<string, int, int> OnObjectiveProgress => _onObjectiveProgress;

        /// <summary>Event: quest completed (questId).</summary>
        public UnityEvent<string> OnQuestCompleted => _onQuestCompleted;

        /// <summary>Event: single objective completed (questId, objectiveIndex).</summary>
        public UnityEvent<string, int> OnObjectiveCompleted => _onObjectiveCompleted;

        /// <summary>Event: quest failed (questId).</summary>
        public UnityEvent<string> OnQuestFailed => _onQuestFailed;

        /// <summary>Event: any quest accepted (no arguments).</summary>
        public UnityEvent OnAnyQuestAccepted => _onAnyQuestAccepted;

        /// <summary>Event: any quest completed (no arguments).</summary>
        public UnityEvent OnAnyQuestCompleted => _onAnyQuestCompleted;

        /// <summary>All quest states.</summary>
        public IReadOnlyList<QuestState> AllQuests => _states;

        /// <summary>Active quests only.</summary>
        public IReadOnlyList<QuestState> ActiveQuests
        {
            get
            {
                List<QuestState> list = new();
                foreach (QuestState s in _states)
                {
                    if (s.Status == QuestStatus.Active)
                    {
                        list.Add(s);
                    }
                }

                return list;
            }
        }

        /// <summary>C# event: quest accepted.</summary>
        public event Action<QuestConfig> QuestAccepted;

        /// <summary>C# event: objective progress.</summary>
        public event Action<QuestConfig, int, int> ObjectiveProgress;

        /// <summary>C# event: quest completed.</summary>
        public event Action<QuestConfig> QuestCompleted;

        /// <summary>Accept quest by ID. Evaluates StartConditions using ConditionContext. For UnityEvent binding.</summary>
        /// <returns>true if the quest was accepted.</returns>
        public bool AcceptQuest(string questId)
        {
            QuestConfig config = GetConfigById(questId);
            return config != null && AcceptQuest(config);
        }

        /// <summary>Accept quest by config (type-safe from code).</summary>
        public bool AcceptQuest(QuestConfig quest)
        {
            if (quest == null)
            {
                return false;
            }

            string id = quest.Id;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (GetState(id) != null)
            {
                return false;
            }

            if (!EvaluateStartConditions(quest))
            {
                return false;
            }

            QuestState state = new(id, quest.Objectives.Count);
            state.Status = QuestStatus.Active;
            _states.Add(state);

            _onQuestAccepted?.Invoke(id);
            _onAnyQuestAccepted?.Invoke();
            QuestAccepted?.Invoke(quest);
            return true;
        }

        /// <summary>Try to accept a quest and report a failure reason.</summary>
        public bool TryAcceptQuest(string questId, out string failReason)
        {
            failReason = null;
            QuestConfig config = GetConfigById(questId);
            if (config == null)
            {
                failReason = "Quest not found.";
                return false;
            }

            if (GetState(config.Id) != null)
            {
                failReason = "Already accepted or completed.";
                return false;
            }

            if (!EvaluateStartConditions(config))
            {
                failReason = "Start conditions not met.";
                return false;
            }

            return AcceptQuest(config);
        }

        /// <summary>Credit objective completion. Call from QuestNoCodeAction(CompleteObjective) or from code.</summary>
        public void CompleteObjective(string questId, int objectiveIndex)
        {
            QuestConfig config = GetConfigById(questId);
            if (config != null)
            {
                CompleteObjective(config, objectiveIndex);
            }
        }

        /// <summary>Credit objective completion using a quest config.</summary>
        public void CompleteObjective(QuestConfig quest, int objectiveIndex)
        {
            if (quest == null)
            {
                return;
            }

            QuestState state = GetState(quest.Id);
            if (state == null || state.Status != QuestStatus.Active)
            {
                return;
            }

            if (objectiveIndex < 0 || objectiveIndex >= quest.Objectives.Count)
            {
                return;
            }

            if (state.IsObjectiveCompleted(objectiveIndex))
            {
                return;
            }

            QuestObjectiveData obj = quest.Objectives[objectiveIndex];
            if (obj.Type == QuestObjectiveType.KillCount || obj.Type == QuestObjectiveType.CollectCount)
            {
                int newProgress = state.AddObjectiveProgress(objectiveIndex, 1);
                int required = obj.RequiredCount;
                _onObjectiveProgress?.Invoke(quest.Id, objectiveIndex, newProgress);
                ObjectiveProgress?.Invoke(quest, objectiveIndex, newProgress);
                if (newProgress >= required)
                {
                    state.MarkObjectiveCompleted(objectiveIndex);
                    _onObjectiveCompleted?.Invoke(quest.Id, objectiveIndex);
                }
            }
            else
            {
                state.MarkObjectiveCompleted(objectiveIndex);
                _onObjectiveProgress?.Invoke(quest.Id, objectiveIndex, 1);
                ObjectiveProgress?.Invoke(quest, objectiveIndex, 1);
                _onObjectiveCompleted?.Invoke(quest.Id, objectiveIndex);
            }

            if (AllObjectivesCompleted(state, quest))
            {
                state.Status = QuestStatus.Completed;
                _onQuestCompleted?.Invoke(quest.Id);
                _onAnyQuestCompleted?.Invoke();
                QuestCompleted?.Invoke(quest);
            }
        }

        /// <summary>Notify enemy kill (for KillCount objectives).</summary>
        public void NotifyKill(string enemyId)
        {
            foreach (QuestState state in _states)
            {
                if (state.Status != QuestStatus.Active)
                {
                    continue;
                }

                QuestConfig config = GetConfigById(state.QuestId);
                if (config == null)
                {
                    continue;
                }

                for (int i = 0; i < config.Objectives.Count; i++)
                {
                    QuestObjectiveData obj = config.Objectives[i];
                    if (obj.Type == QuestObjectiveType.KillCount && obj.TargetId == enemyId &&
                        !state.IsObjectiveCompleted(i))
                    {
                        CompleteObjective(config, i);
                    }
                }
            }
        }

        /// <summary>Notify item collected (for CollectCount objectives).</summary>
        public void NotifyCollect(string itemId)
        {
            foreach (QuestState state in _states)
            {
                if (state.Status != QuestStatus.Active)
                {
                    continue;
                }

                QuestConfig config = GetConfigById(state.QuestId);
                if (config == null)
                {
                    continue;
                }

                for (int i = 0; i < config.Objectives.Count; i++)
                {
                    QuestObjectiveData obj = config.Objectives[i];
                    if (obj.Type == QuestObjectiveType.CollectCount && obj.TargetId == itemId &&
                        !state.IsObjectiveCompleted(i))
                    {
                        CompleteObjective(config, i);
                    }
                }
            }
        }

        /// <summary>Get quest state by ID.</summary>
        public QuestState GetState(string questId)
        {
            foreach (QuestState s in _states)
            {
                if (s.QuestId == questId)
                {
                    return s;
                }
            }

            return null;
        }

        /// <summary>Get quest state by config.</summary>
        public QuestState GetState(QuestConfig quest)
        {
            return quest == null ? null : GetState(quest.Id);
        }

        /// <summary>Fail quest by ID (status → Failed). Invokes OnQuestFailed.</summary>
        public void FailQuest(string questId)
        {
            QuestState state = GetState(questId);
            if (state == null || state.Status != QuestStatus.Active)
            {
                return;
            }

            state.Status = QuestStatus.Failed;
            _onQuestFailed?.Invoke(questId);
        }

        /// <summary>Fail quest by config.</summary>
        public void FailQuest(QuestConfig quest)
        {
            if (quest != null)
            {
                FailQuest(quest.Id);
            }
        }

        /// <summary>
        ///     Clear quest state (removes the entry from the registry). Allows replaying the quest.
        /// </summary>
        public bool ResetQuest(string questId)
        {
            QuestState state = GetState(questId);
            if (state == null)
            {
                return false;
            }

            _states.Remove(state);
            return true;
        }

        /// <summary>Clear quest state by config.</summary>
        public bool ResetQuest(QuestConfig quest)
        {
            return quest != null && ResetQuest(quest.Id);
        }

        /// <summary>
        ///     Restart quest: remove old state and try accepting again.
        /// </summary>
        public bool RestartQuest(string questId)
        {
            QuestConfig config = GetConfigById(questId);
            if (config == null)
            {
                return false;
            }

            ResetQuest(questId);
            return AcceptQuest(config);
        }

        /// <summary>Restart quest by config.</summary>
        public bool RestartQuest(QuestConfig quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.Id))
            {
                return false;
            }

            ResetQuest(quest.Id);
            return AcceptQuest(quest);
        }

        /// <summary>Clear all quest states (active, completed, and failed).</summary>
        public void ResetAllQuests()
        {
            _states.Clear();
        }

        /// <summary>Accept quest using Editor Quest Id field (Inspector button).</summary>
        [Button("Accept Quest (Editor Id)")]
        public void AcceptQuestFromEditor()
        {
            if (!string.IsNullOrEmpty(_editorQuestId))
            {
                AcceptQuest(_editorQuestId);
            }
        }

        /// <summary>Complete objective using Editor Quest Id and Objective Index (Inspector button).</summary>
        [Button("Complete Objective (Editor)")]
        public void CompleteObjectiveFromEditor()
        {
            if (!string.IsNullOrEmpty(_editorQuestId))
            {
                CompleteObjective(_editorQuestId, _editorObjectiveIndex);
            }
        }

        /// <summary>Whether the quest is active.</summary>
        public bool IsActive(QuestConfig quest)
        {
            QuestState s = GetState(quest);
            return s != null && s.Status == QuestStatus.Active;
        }

        /// <summary>Whether the quest is completed.</summary>
        public bool IsCompleted(QuestConfig quest)
        {
            QuestState s = GetState(quest);
            return s != null && s.Status == QuestStatus.Completed;
        }

        /// <summary>Objective progress for display.</summary>
        public int GetObjectiveProgress(QuestConfig quest, int index)
        {
            QuestState s = GetState(quest);
            return s?.GetObjectiveProgress(index) ?? 0;
        }

        private bool EvaluateStartConditions(QuestConfig quest)
        {
            if (quest.StartConditions == null || quest.StartConditions.Count == 0)
            {
                return true;
            }

            GameObject context = _conditionContext != null ? _conditionContext : gameObject;
            foreach (ConditionEntry entry in quest.StartConditions)
            {
                if (entry == null)
                {
                    continue;
                }

                if (!entry.Evaluate(context))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllObjectivesCompleted(QuestState state, QuestConfig config)
        {
            for (int i = 0; i < config.Objectives.Count; i++)
            {
                if (!state.IsObjectiveCompleted(i))
                {
                    return false;
                }
            }

            return true;
        }

        private QuestConfig GetConfigById(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            foreach (QuestConfig c in _knownQuests)
            {
                if (c != null && c.Id == questId)
                {
                    return c;
                }
            }

            return null;
        }
    }
}
