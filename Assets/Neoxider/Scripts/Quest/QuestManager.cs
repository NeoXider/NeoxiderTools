using System;
using System.Collections.Generic;
using Neo.Condition;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Quest
{
    /// <summary>
    ///     Единая точка входа для квестов: реестр состояний, принятие/завершение целей, проверка условий старта через
    ///     NeoCondition.
    ///     Удобен и для NoCode (UnityEvent), и для кода (перегрузки с QuestConfig). Подробнее: Quest/QuestManager.md.
    /// </summary>
    [NeoDoc("Quest/QuestManager.md")]
    [CreateFromMenu("Neoxider/Quest/QuestManager")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestManager))]
    public class QuestManager : MonoBehaviour
    {
        [Header("Context")]
        [Tooltip(
            "Объект, передаваемый в ConditionEntry.Evaluate(context) при проверке StartConditions. Обычно — игрок или менеджер мира.")]
        [SerializeField]
        private GameObject _conditionContext;

        [Header("Known Quests")]
        [Tooltip("Список конфигов квестов для разрешения questId → QuestConfig при AcceptQuest(string).")]
        [SerializeField]
        private List<QuestConfig> _knownQuests = new();

        [Header("Events (NoCode)")] [SerializeField]
        private UnityEvent<string> _onQuestAccepted = new();

        [SerializeField] private UnityEvent<string, int, int> _onObjectiveProgress = new();

        [SerializeField] private UnityEvent<string> _onQuestCompleted = new();

        private readonly List<QuestState> _states = new();

        /// <summary>Контекст для проверки условий старта (ConditionEntry.Evaluate).</summary>
        public GameObject ConditionContext
        {
            get => _conditionContext;
            set => _conditionContext = value;
        }

        /// <summary>Синглтон (первый QuestManager в сцене).</summary>
        public static QuestManager Instance { get; private set; }

        /// <summary>Событие: квест принят (questId).</summary>
        public UnityEvent<string> OnQuestAccepted => _onQuestAccepted;

        /// <summary>Событие: прогресс по цели (questId, objectiveIndex, currentCount).</summary>
        public UnityEvent<string, int, int> OnObjectiveProgress => _onObjectiveProgress;

        /// <summary>Событие: квест завершён (questId).</summary>
        public UnityEvent<string> OnQuestCompleted => _onQuestCompleted;

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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
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

        /// <summary>Зачесть выполнение цели (NoCode: вызывать из QuestObjectiveNotifier или из кода).</summary>
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
                }
            }
            else
            {
                state.MarkObjectiveCompleted(objectiveIndex);
                _onObjectiveProgress?.Invoke(quest.Id, objectiveIndex, 1);
                ObjectiveProgress?.Invoke(quest, objectiveIndex, 1);
            }

            if (AllObjectivesCompleted(state, quest))
            {
                state.Status = QuestStatus.Completed;
                _onQuestCompleted?.Invoke(quest.Id);
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