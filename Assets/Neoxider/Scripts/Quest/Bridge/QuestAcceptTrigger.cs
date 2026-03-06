using Neo;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Вызывает QuestManager.AcceptQuest(quest). Поле: Quest (QuestConfig). Метод AcceptQuest() — без параметров, для вызова из UnityEvent (кнопка и т.д.).
    /// </summary>
    [NeoDoc("Quest/Scenarios.md")]
    [CreateFromMenu("Neoxider/Quest/Quest Accept Trigger")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestAcceptTrigger))]
    public class QuestAcceptTrigger : MonoBehaviour
    {
        [Tooltip("Квест, который будет принят при вызове AcceptQuest().")] [SerializeField]
        private QuestConfig _quest;

        /// <summary>Вызывает QuestManager.Instance.AcceptQuest(_quest).</summary>
        [Button("Accept Quest")]
        public void AcceptQuest()
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

            manager.AcceptQuest(_quest);
        }
    }
}