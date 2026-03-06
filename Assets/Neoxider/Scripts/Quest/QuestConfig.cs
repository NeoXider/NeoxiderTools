using System.Collections.Generic;
using Neo.Condition;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Конфигурация квеста: описание, цели, условия старта. Создаётся как ScriptableObject.
    /// </summary>
    [NeoDoc("Quest/QuestConfig.md")]
    [CreateAssetMenu(fileName = "QuestConfig", menuName = "Neoxider/Quest/Quest Config")]
    public class QuestConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique quest identifier. Used in AcceptQuest/GetState.")]
        [SerializeField]
        private string _id = "";

        [Header("Display")] [Tooltip("Quest title for UI.")] [SerializeField]
        private string _title = "";

        [Tooltip("Quest description for UI.")] [TextArea(2, 6)] [SerializeField]
        private string _description = "";

        [Tooltip("Optional quest icon for UI.")]
        [SerializeField]
        private Sprite _icon;

        [Header("Objectives")]
        [Tooltip("Quest objectives list. Order defines objective index (0, 1, ...).")]
        [SerializeField]
        private List<QuestObjectiveData> _objectives = new();

        [Header("Start Conditions")]
        [Tooltip(
            "Quest availability conditions. Evaluated via QuestManager.ConditionContext during AcceptQuest. All must be true (AND).")]
        [SerializeField]
        private List<ConditionEntry> _startConditions = new();

        [Header("Optional")] [Tooltip("Quest IDs that become available after this quest is completed.")] [SerializeField]
        private List<string> _nextQuestIds = new();

        /// <summary>Уникальный идентификатор квеста.</summary>
        public string Id => _id;

        /// <summary>Название для UI.</summary>
        public string Title => _title;

        /// <summary>Описание для UI.</summary>
        public string Description => _description;

        /// <summary>Иконка квеста для UI (опционально).</summary>
        public Sprite Icon => _icon;

        /// <summary>Цели квеста (readonly).</summary>
        public IReadOnlyList<QuestObjectiveData> Objectives => _objectives;

        /// <summary>Условия старта (readonly).</summary>
        public IReadOnlyList<ConditionEntry> StartConditions => _startConditions;

        /// <summary>ID следующих квестов после завершения.</summary>
        public IReadOnlyList<string> NextQuestIds => _nextQuestIds;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(_title))
            {
                _id = _title.Replace(" ", "_");
            }
        }
    }
}