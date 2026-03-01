using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Мост для NoCode: кнопка/UnityEvent → QuestManager.AcceptQuest. Подключите OnClick к AcceptQuest().
    /// </summary>
    [NeoDoc("Quest/NoCode.md")]
    [CreateFromMenu("Neoxider/Quest/Quest Accept Trigger")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestAcceptTrigger))]
    public class QuestAcceptTrigger : MonoBehaviour
    {
        [Tooltip("Квест, который будет принят при вызове AcceptQuest().")] [SerializeField]
        private QuestConfig _quest;

        /// <summary>Вызвать из UnityEvent (например кнопки) — принимает квест в QuestManager.</summary>
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