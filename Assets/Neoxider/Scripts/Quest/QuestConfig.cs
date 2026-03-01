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
        [Tooltip("Уникальный идентификатор квеста. Используется в AcceptQuest/GetState.")]
        [SerializeField]
        private string _id = "";

        [Header("Display")] [Tooltip("Название для UI.")] [SerializeField]
        private string _title = "";

        [Tooltip("Описание для UI.")] [TextArea(2, 6)] [SerializeField]
        private string _description = "";

        [Header("Objectives")]
        [Tooltip("Список целей квеста. Порядок определяет индекс цели (0, 1, …).")]
        [SerializeField]
        private List<QuestObjectiveData> _objectives = new();

        [Header("Start Conditions")]
        [Tooltip(
            "Условия доступности квеста. Проверяются через ConditionContext в QuestManager при AcceptQuest. Все должны быть true (AND).")]
        [SerializeField]
        private List<ConditionEntry> _startConditions = new();

        [Header("Optional")] [Tooltip("ID квестов, которые станут доступны после завершения этого.")] [SerializeField]
        private List<string> _nextQuestIds = new();

        /// <summary>Уникальный идентификатор квеста.</summary>
        public string Id => _id;

        /// <summary>Название для UI.</summary>
        public string Title => _title;

        /// <summary>Описание для UI.</summary>
        public string Description => _description;

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