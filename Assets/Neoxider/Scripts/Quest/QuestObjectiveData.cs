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
        /// <summary>Выполняется по внешнему триггеру (QuestObjectiveNotifier) или по ConditionEntry в менеджере.</summary>
        CustomCondition,

        /// <summary>Счётчик: убить N врагов с заданным ID (NotifyKill).</summary>
        KillCount,

        /// <summary>Счётчик: собрать N предметов с заданным ID (NotifyCollect).</summary>
        CollectCount,

        /// <summary>Дойти до точки (триггер/Notifier).</summary>
        ReachPoint,

        /// <summary>Поговорить с NPC (триггер/Notifier).</summary>
        Talk
    }

    /// <summary>
    ///     Данные одной цели квеста: тип, параметры или условие (ConditionEntry).
    /// </summary>
    [Serializable]
    public class QuestObjectiveData
    {
        [Tooltip("Тип цели: как проверяется выполнение.")] [SerializeField]
        private QuestObjectiveType _type = QuestObjectiveType.CustomCondition;

        [Tooltip("ID цели (враг, предмет, точка) для типов KillCount/CollectCount/ReachPoint.")] [SerializeField]
        private string _targetId = "";

        [Tooltip("Требуемое количество для счётчиков (KillCount, CollectCount).")] [SerializeField]
        private int _requiredCount = 1;

        [Tooltip(
            "Условие выполнения цели (для CustomCondition). Проверяется в QuestManager по контексту. Если пусто — только внешний триггер (QuestObjectiveNotifier).")]
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

        /// <summary>Условие для CustomCondition (может быть null — тогда только Notifier).</summary>
        public ConditionEntry Condition
        {
            get => _condition;
            set => _condition = value;
        }
    }
}