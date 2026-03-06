using System;
using Neo.Condition;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Тип цели квеста: как проверяется выполнение.
    /// </summary>
    public enum QuestObjectiveType
    {
        /// <summary>Выполняется по внешнему триггеру (например QuestNoCodeAction) или по ConditionEntry в менеджере.</summary>
        CustomCondition,

        /// <summary>Счётчик: убить N врагов с заданным ID (NotifyKill).</summary>
        KillCount,

        /// <summary>Счётчик: собрать N предметов с заданным ID (NotifyCollect).</summary>
        CollectCount,

        /// <summary>Дойти до точки (триггер/UI-событие).</summary>
        ReachPoint,

        /// <summary>Поговорить с NPC (триггер/UI-событие).</summary>
        Talk
    }

    /// <summary>
    ///     Данные одной цели квеста: тип, параметры или условие (ConditionEntry).
    /// </summary>
    [Serializable]
    public class QuestObjectiveData
    {
        [Tooltip("Objective type: how completion is evaluated.")] [SerializeField]
        private QuestObjectiveType _type = QuestObjectiveType.CustomCondition;

        [Tooltip("Objective target ID (enemy, item, point) for KillCount/CollectCount/ReachPoint types.")]
        [SerializeField]
        private string _targetId = "";

        [Tooltip("Required count for counter objectives (KillCount, CollectCount).")] [SerializeField]
        private int _requiredCount = 1;

        [Tooltip("Custom objective text for UI. If empty, text can be generated from Type/TargetId.")]
        [SerializeField]
        private string _displayText = "";

        [Tooltip(
            "Completion condition for CustomCondition objectives. Evaluated by QuestManager against context. If empty, complete via external trigger (e.g., QuestNoCodeAction).")]
        [SerializeField]
        private ConditionEntry _condition;

        /// <summary>Тип цели.</summary>
        public QuestObjectiveType Type
        {
            get => _type;
            set => _type = value;
        }

        /// <summary>ID цели (враг, предмет, точка).</summary>
        public string TargetId
        {
            get => _targetId;
            set => _targetId = value ?? "";
        }

        /// <summary>Требуемое количество для счётчиков.</summary>
        public int RequiredCount
        {
            get => _requiredCount;
            set => _requiredCount = value;
        }

        /// <summary>Пользовательский текст цели для UI.</summary>
        public string DisplayText
        {
            get => _displayText;
            set => _displayText = value ?? "";
        }

        /// <summary>Условие для CustomCondition (может быть null — тогда только внешний триггер).</summary>
        public ConditionEntry Condition
        {
            get => _condition;
            set => _condition = value;
        }
    }
}