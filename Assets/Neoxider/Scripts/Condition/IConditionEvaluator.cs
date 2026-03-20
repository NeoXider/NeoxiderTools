using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Универсальный контракт оценки условия. Позволяет использовать одну и ту же настройку условия
    ///     в NeoCondition, StateMachine и других системах.
    /// </summary>
    public interface IConditionEvaluator
    {
        /// <summary>
        ///     Оценить условие в заданном контексте.
        /// </summary>
        /// <param name="context">GameObject-владелец (fallback при пустом источнике).</param>
        /// <returns>true, если условие выполнено.</returns>
        bool Evaluate(GameObject context);
    }
}
