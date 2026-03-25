using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Universal contract for evaluating a condition. Allows the same condition setup to be used
    ///     in NeoCondition, StateMachine, and other systems.
    /// </summary>
    public interface IConditionEvaluator
    {
        /// <summary>
        ///     Evaluates the condition in the given context.
        /// </summary>
        /// <param name="context">Owner GameObject (fallback when source is empty).</param>
        /// <returns>true if the condition passes.</returns>
        bool Evaluate(GameObject context);
    }
}
