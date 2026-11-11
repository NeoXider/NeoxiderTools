using System;
using Neo.Condition;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Quest objective type: how completion is evaluated.
    /// </summary>
    public enum QuestObjectiveType
    {
        /// <summary>Fulfilled by external trigger (e.g. QuestNoCodeAction) or ConditionEntry in the manager.</summary>
        CustomCondition,

        /// <summary>Counter: kill N enemies with the given ID (NotifyKill).</summary>
        KillCount,

        /// <summary>Counter: collect N items with the given ID (NotifyCollect).</summary>
        CollectCount,

        /// <summary>Reach a point (trigger/UI event).</summary>
        ReachPoint,

        /// <summary>Talk to an NPC (trigger/UI event).</summary>
        Talk
    }

    /// <summary>
    ///     Data for a single quest objective: type, parameters, or condition (ConditionEntry).
    /// </summary>
    [Serializable]
    public class QuestObjectiveData
    {
        [Tooltip("Objective type: how completion is evaluated.")] [SerializeField]
        private QuestObjectiveType _type = QuestObjectiveType.CustomCondition;

        [Tooltip("Objective target ID (enemy, item, point) for KillCount/CollectCount/ReachPoint types.")]
        [SerializeField]
        private string _targetId = "";

        [Tooltip("Required count for counter objectives (KillCount, CollectCount). Values below 1 behave as 1.")]
        [SerializeField]
        [Min(1)]
        private int _requiredCount = 1;

        [Tooltip("Custom objective text for UI. If empty, text can be generated from Type/TargetId.")] [SerializeField]
        private string _displayText = "";

        [Tooltip(
            "Completion condition for CustomCondition objectives. Evaluated against ConditionContext when QuestManager.EvaluateObjectiveConditions() is called. If empty, complete via external trigger (e.g., QuestNoCodeAction).")]
        [SerializeField]
        private ConditionEntry _condition;

        /// <summary>Objective type.</summary>
        public QuestObjectiveType Type
        {
            get => _type;
            set => _type = value;
        }

        /// <summary>Target ID (enemy, item, point).</summary>
        public string TargetId
        {
            get => _targetId;
            set => _targetId = value ?? "";
        }

        /// <summary>Required count for counter objectives.</summary>
        public int RequiredCount
        {
            get => _requiredCount;
            set => _requiredCount = value;
        }

        /// <summary>Custom objective text for UI.</summary>
        public string DisplayText
        {
            get => _displayText;
            set => _displayText = value ?? "";
        }

        /// <summary>
        ///     True for counter objectives (KillCount, CollectCount) whose progress accumulates to RequiredCount;
        ///     other types are binary (progress 0 or 1).
        /// </summary>
        public bool IsCounter => _type == QuestObjectiveType.KillCount || _type == QuestObjectiveType.CollectCount;

        /// <summary>Progress value that completes this objective: RequiredCount (at least 1) for counters, otherwise 1.</summary>
        public int TargetProgress => IsCounter ? Mathf.Max(1, _requiredCount) : 1;

        /// <summary>Condition for CustomCondition (may be null — then only external trigger applies).</summary>
        public ConditionEntry Condition
        {
            get => _condition;
            set => _condition = value;
        }
    }
}
