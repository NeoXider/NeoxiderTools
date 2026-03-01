using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Мост для NoCode: NeoCondition.OnTrue → QuestManager.CompleteObjective. Подключите OnTrue к NotifyComplete().
    /// </summary>
    [NeoDoc("Quest/NoCode.md")]
    [CreateFromMenu("Neoxider/Quest/Quest Objective Notifier")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestObjectiveNotifier))]
    public class QuestObjectiveNotifier : MonoBehaviour
    {
        [Tooltip("Квест, в котором засчитывается цель.")] [SerializeField]
        private QuestConfig _quest;

        [Tooltip("Индекс цели (0, 1, 2, …) в списке целей квеста.")] [SerializeField]
        private int _objectiveIndex;

        /// <summary>Вызвать из UnityEvent или NeoCondition.OnTrue — засчитывает цель в QuestManager.</summary>
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