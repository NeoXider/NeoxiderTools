using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Marker component for the object used as context when evaluating quest start conditions (ConditionEntry.Evaluate).
    ///     Add it to a player/world object and assign that GameObject to QuestManager -> Condition Context.
    /// </summary>
    [NeoDoc("Quest/QuestContext.md")]
    [CreateFromMenu("Neoxider/Quest/Quest Context")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestContext))]
    public class QuestContext : MonoBehaviour
    {
    }
}
