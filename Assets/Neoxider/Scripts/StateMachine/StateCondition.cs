using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Base class for transition conditions in the State Machine (legacy).
    ///     Prefer <see cref="StatePredicate"/> for new work.
    /// </summary>
    /// <remarks>
    ///     Kept for backward compatibility.
    ///     New projects should use StatePredicate for richer composition.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class CustomCondition : StateCondition
    /// {
    ///     public override bool Evaluate()
    ///     {
    ///         // Condition logic
    ///         return true;
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class StateCondition
    {
        /// <summary>
        ///     Evaluates the condition.
        /// </summary>
        /// <returns>True if the condition passes.</returns>
        public abstract bool Evaluate();
    }

    /// <summary>
    ///     Condition that returns a stored bool value.
    /// </summary>
    [Serializable]
    public class BoolStateCondition : StateCondition
    {
        [SerializeField] private bool value;

        /// <summary>
        ///     Value to return from Evaluate.
        /// </summary>
        public bool Value
        {
            get => value;
            set => this.value = value;
        }

        public override bool Evaluate()
        {
            return value;
        }
    }

    /// <summary>
    ///     Condition that compares a float to a threshold.
    /// </summary>
    [Serializable]
    public class FloatStateCondition : StateCondition
    {
        [SerializeField] private float value;

        [SerializeField] private ComparisonType comparison = ComparisonType.GreaterThan;

        [SerializeField] private float threshold;

        /// <summary>
        ///     Left-hand value for comparison.
        /// </summary>
        public float Value
        {
            get => value;
            set => this.value = value;
        }

        /// <summary>
        ///     Comparison operator.
        /// </summary>
        public ComparisonType Comparison
        {
            get => comparison;
            set => comparison = value;
        }

        /// <summary>
        ///     Threshold to compare against.
        /// </summary>
        public float Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        public override bool Evaluate()
        {
            return comparison switch
            {
                ComparisonType.GreaterThan => value > threshold,
                ComparisonType.LessThan => value < threshold,
                ComparisonType.GreaterThanOrEqual => value >= threshold,
                ComparisonType.LessThanOrEqual => value <= threshold,
                ComparisonType.Equal => Mathf.Approximately(value, threshold),
                ComparisonType.NotEqual => !Mathf.Approximately(value, threshold),
                _ => false
            };
        }
    }

    /// <summary>
    ///     Condition driven by a UnityEvent; listeners call <see cref="SetResult"/> to set the outcome.
    /// </summary>
    [Serializable]
    public class EventStateCondition : StateCondition
    {
        [SerializeField] private UnityEvent onEvaluate = new();

        private bool lastResult;

        /// <summary>
        ///     Invoked during evaluation; use SetResult to provide the outcome.
        /// </summary>
        public UnityEvent OnEvaluate => onEvaluate;

        /// <summary>
        ///     Sets the result used when Evaluate returns.
        /// </summary>
        /// <param name="result">Outcome of the condition.</param>
        public void SetResult(bool result)
        {
            lastResult = result;
        }

        public override bool Evaluate()
        {
            onEvaluate?.Invoke();
            return lastResult;
        }
    }
}
