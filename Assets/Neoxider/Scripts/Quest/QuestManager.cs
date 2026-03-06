using System;
using System.Collections.Generic;
using Neo;
using Neo.Condition;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Quest
{
    /// <summary>
    ///     Единая точка входа для квестов: реестр состояний, принятие/завершение целей, проверка условий старта через
    ///     NeoCondition.
    ///     Синглтон: реестр квестов, AcceptQuest/CompleteObjective, проверка StartConditions. События для UnityEvent, перегрузки с QuestConfig для кода. Подробнее: Quest/QuestManager.md.
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

        [Tooltip("Invoked when objective progress changes (questId, objectiveIndex, currentCount).")]
        [SerializeField]
        private UnityEvent<string, int, int> _onObjectiveProgress = new();

        [Tooltip("Invoked when an objective becomes completed (questId, objectiveIndex).")]
        [SerializeField]
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

        /// <summary>Контекст для проверки условий старта (ConditionEntry.Evaluate).</summary>
        public GameObject ConditionContext
        {
            get => _conditionContext;
            set => _conditionContext = value;
        }

        /// <summary>Singleton access alias for backwards compatibility.</summary>
        public static QuestManager Instance => I;

        /// <summary>Событие: квест принят (questId).</summary>
        public UnityEvent<string> OnQuestAccepted => _onQuestAccepted;

        /// <summary>Событие: прогресс по цели (questId, objectiveIndex, currentCount).</summary>
        public UnityEvent<string, int, int> OnObjectiveProgress => _onObjectiveProgress;

        /// <summary>Событие: квест завершён (questId).</summary>
        public UnityEvent<string> OnQuestCompleted => _onQuestCompleted;

        /// <summary>Событие: одна цель выполнена (questId, objectiveIndex).</summary>
        public UnityEvent<string, int> OnObjectiveCompleted => _onObjectiveCompleted;

        /// <summary>Событие: квест провален (questId).</summary>
        public UnityEvent<string> OnQuestFailed => _onQuestFailed;

        /// <summary>Событие: любой квест принят (без аргументов).</summary>
        public UnityEvent OnAnyQuestAccepted => _onAnyQuestAccepted;

        /// <summary>Событие: любой квест завершён (без аргументов).</summary>
        public UnityEvent OnAnyQuestCompleted => _onAnyQuestCompleted;

        /// <summary>Все состояния квестов.</summary>
        public IReadOnlyList<QuestState> AllQuests => _states;

        /// <summary>Только активные квесты.</summary>
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

        /// <summary>C#: квест принят.</summary>
        public event Action<QuestConfig> QuestAccepted;

        /// <summary>C#: прогресс по цели.</summary>
        public event Action<QuestConfig, int, int> ObjectiveProgress;

        /// <summary>C#: квест завершён.</summary>
        public event Action<QuestConfig> QuestCompleted;

        /// <summary>Принять квест по ID. Проверяет StartConditions по ConditionContext. Для вызова из UnityEvent.</summary>
        /// <returns>true, если квест принят.</returns>
        public bool AcceptQuest(string questId)
        {
            QuestConfig config = GetConfigById(questId);
            return config != null && AcceptQuest(config);
        }

        /// <summary>Принять квест по конфигу (типобезопасно из кода).</summary>
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

        /// <summary>Попытка принять квест с причиной отказа.</summary>
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

        /// <summary>Зачесть выполнение цели. Вызывать из QuestNoCodeAction(CompleteObjective) или из кода.</summary>
        public void CompleteObjective(string questId, int objectiveIndex)
        {
            QuestConfig config = GetConfigById(questId);
            if (config != null)
            {
                CompleteObjective(config, objectiveIndex);
            }
        }

        /// <summary>Зачесть выполнение цели по конфигу.</summary>
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

        /// <summary>Уведомить об убийстве врага (для целей KillCount).</summary>
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

        /// <summary>Уведомить о сборе предмета (для целей CollectCount).</summary>
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

        /// <summary>Получить состояние квеста по ID.</summary>
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

        /// <summary>Получить состояние квеста по конфигу.</summary>
        public QuestState GetState(QuestConfig quest)
        {
            return quest == null ? null : GetState(quest.Id);
        }

        /// <summary>Провалить квест по ID (статус → Failed). Вызывает OnQuestFailed.</summary>
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

        /// <summary>Провалить квест по конфигу.</summary>
        public void FailQuest(QuestConfig quest)
        {
            if (quest != null)
            {
                FailQuest(quest.Id);
            }
        }

        /// <summary>
        ///     Сбросить состояние квеста (убирает запись из реестра состояний). Позволяет пройти квест заново.
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

        /// <summary>Сбросить состояние квеста по конфигу.</summary>
        public bool ResetQuest(QuestConfig quest)
        {
            return quest != null && ResetQuest(quest.Id);
        }

        /// <summary>
        ///     Перезапустить квест: удалить старое состояние и снова попытаться принять квест.
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

        /// <summary>Перезапустить квест по конфигу.</summary>
        public bool RestartQuest(QuestConfig quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.Id))
            {
                return false;
            }

            ResetQuest(quest.Id);
            return AcceptQuest(quest);
        }

        /// <summary>Сбросить все состояния квестов (активные, завершённые и проваленные).</summary>
        public void ResetAllQuests()
        {
            _states.Clear();
        }

        /// <summary>Принять квест по полю Editor Quest Id (кнопка в инспекторе).</summary>
        [Button("Accept Quest (Editor Id)")]
        public void AcceptQuestFromEditor()
        {
            if (!string.IsNullOrEmpty(_editorQuestId))
            {
                AcceptQuest(_editorQuestId);
            }
        }

        /// <summary>Зачесть цель по полям Editor Quest Id и Objective Index (кнопка в инспекторе).</summary>
        [Button("Complete Objective (Editor)")]
        public void CompleteObjectiveFromEditor()
        {
            if (!string.IsNullOrEmpty(_editorQuestId))
            {
                CompleteObjective(_editorQuestId, _editorObjectiveIndex);
            }
        }

        /// <summary>Квест активен.</summary>
        public bool IsActive(QuestConfig quest)
        {
            QuestState s = GetState(quest);
            return s != null && s.Status == QuestStatus.Active;
        }

        /// <summary>Квест завершён.</summary>
        public bool IsCompleted(QuestConfig quest)
        {
            QuestState s = GetState(quest);
            return s != null && s.Status == QuestStatus.Completed;
        }

        /// <summary>Прогресс по цели (для отображения).</summary>
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