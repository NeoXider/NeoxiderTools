using Neo;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Вызывает QuestManager.CompleteObjective(quest.Id, objectiveIndex). Поля: Quest (QuestConfig), Objective Index (int). Метод NotifyComplete() — без параметров, для вызова из UnityEvent.
    /// </summary>
    [NeoDoc("Quest/Scenarios.md")]
    [CreateFromMenu("Neoxider/Quest/Quest Objective Notifier")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestObjectiveNotifier))]
    public class QuestObjectiveNotifier : MonoBehaviour
    {
        [Tooltip("Квест, в котором засчитывается цель.")]
        [SerializeField]
        private QuestConfig _quest;

        [Tooltip("Индекс цели (0, 1, 2, …) в списке целей квеста.")]
        [SerializeField]
        private int _objectiveIndex;

        /// <summary>Вызывает QuestManager.Instance.CompleteObjective(_quest.Id, _objectiveIndex).</summary>
        [Button("Notify Complete")]
        public void NotifyComplete()
        {
            if (_quest == null)
            {
                return;
            }

            QuestManager manager = QuestManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.CompleteObjective(_quest.Id, _objectiveIndex);
        }
    }
}