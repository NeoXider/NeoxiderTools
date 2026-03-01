using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Маркер объекта, который используется как контекст для проверки условий старта квестов (ConditionEntry.Evaluate).
    ///     Добавьте на игрока или менеджер мира и назначьте этот GameObject в QuestManager → Condition Context.
    /// </summary>
    [NeoDoc("Quest/README.md")]
    [CreateFromMenu("Neoxider/Quest/Quest Context")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestContext))]
    public class QuestContext : MonoBehaviour
    {
    }
}