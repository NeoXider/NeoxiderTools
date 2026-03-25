using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Comparison mode for numeric predicates.
    /// </summary>
    public enum ComparisonType
    {
        /// <summary>Greater than</summary>
        GreaterThan,

        /// <summary>Less than</summary>
        LessThan,

        /// <summary>Greater than or equal</summary>
        GreaterThanOrEqual,

        /// <summary>Less than or equal</summary>
        LessThanOrEqual,

        /// <summary>Equal</summary>
        Equal,

        /// <summary>Not equal</summary>
        NotEqual
    }

    /// <summary>
    ///     Base class for transition predicates in the State Machine.
    ///     Predicates gate whether a transition may run.
    /// </summary>
    /// <remarks>
    ///     Flip results with <see cref="IsInverted"/>.
    ///     Serializable for Inspector-driven NoCode setups.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class CustomPredicate : StatePredicate
    /// {
    ///     public override bool Evaluate(IState currentState)
    ///     {
    ///         // Predicate logic
    ///         return true;
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class StatePredicate
    {
        /// <summary>
        ///     Display name for debugging and custom editors.
        /// </summary>
        [SerializeField] protected string predicateName = "Unnamed Predicate";

        /// <summary>
        ///     When true, negates the predicate result after evaluation.
        /// </summary>
        [SerializeField] protected bool isInverted;

        /// <summary>
        ///     Predicate display name.
        /// </summary>
        public string PredicateName
        {
            get => predicateName;
            set => predicateName = value;
        }

        /// <summary>
        ///     Whether to invert the evaluated result.
        /// </summary>
        public bool IsInverted
        {
            get => isInverted;
            set => isInverted = value;
        }

        /// <summary>
        ///     Evaluates the predicate with the active state as optional context.
        /// </summary>
        /// <param name="currentState">Active state, if any.</param>
        /// <returns>Result after applying <see cref="IsInverted"/>.</returns>
        public virtual bool Evaluate(IState currentState)
        {
            bool result = EvaluateInternal(currentState);
            return isInverted ? !result : result;
        }

        /// <summary>
        ///     Evaluates without an IState context (EvaluateInternal(null)).
        /// </summary>
        /// <returns>Result after applying <see cref="IsInverted"/>.</returns>
        public virtual bool Evaluate()
        {
            bool result = EvaluateInternal(null);
            return isInverted ? !result : result;
        }

        /// <summary>
        ///     Core evaluation; implement in derived types.
        /// </summary>
        /// <param name="currentState">Active state or null.</param>
        /// <returns>Raw result before <see cref="IsInverted"/> is applied.</returns>
        protected abstract bool EvaluateInternal(IState currentState);
    }

    /// <summary>
    ///     Predicate that returns a constant bool.
    /// </summary>
    [Serializable]
    public class BoolPredicate : StatePredicate
    {
        [SerializeField] private bool value;

        /// <summary>
        ///     Value returned by EvaluateInternal.
        /// </summary>
        public bool Value
        {
            get => value;
            set => this.value = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            return value;
        }
    }

    /// <summary>
    ///     Compares a float to a threshold using <see cref="ComparisonType"/>.
    /// </summary>
    [Serializable]
    public class FloatComparisonPredicate : StatePredicate
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

        protected override bool EvaluateInternal(IState currentState)
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
    ///     Compares an int to a threshold using <see cref="ComparisonType"/>.
    /// </summary>
    [Serializable]
    public class IntComparisonPredicate : StatePredicate
    {
        [SerializeField] private int value;

        [SerializeField] private ComparisonType comparison = ComparisonType.GreaterThan;

        [SerializeField] private int threshold;

        /// <summary>
        ///     Left-hand value for comparison.
        /// </summary>
        public int Value
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
        public int Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            return comparison switch
            {
                ComparisonType.GreaterThan => value > threshold,
                ComparisonType.LessThan => value < threshold,
                ComparisonType.GreaterThanOrEqual => value >= threshold,
                ComparisonType.LessThanOrEqual => value <= threshold,
                ComparisonType.Equal => value == threshold,
                ComparisonType.NotEqual => value != threshold,
                _ => false
            };
        }
    }

    /// <summary>
    ///     String equality predicate with optional case sensitivity.
    /// </summary>
    [Serializable]
    public class StringComparisonPredicate : StatePredicate
    {
        [SerializeField] private string value = "";

        [SerializeField] private string target = "";

        [SerializeField] private bool caseSensitive;

        /// <summary>
        ///     First string operand.
        /// </summary>
        public string Value
        {
            get => value;
            set => this.value = value;
        }

        /// <summary>
        ///     Second string operand.
        /// </summary>
        public string Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        ///     When true, comparison is case-sensitive.
        /// </summary>
        public bool CaseSensitive
        {
            get => caseSensitive;
            set => caseSensitive = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (caseSensitive)
            {
                return value == target;
            }

            return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    ///     Predicate driven by UnityEvent; listeners call <see cref="SetResult"/>.
    /// </summary>
    [Serializable]
    public class EventPredicate : StatePredicate
    {
        [SerializeField] private UnityEvent onEvaluate = new();

        private bool lastResult;

        /// <summary>
        ///     Invoked during evaluation; use SetResult to provide the outcome.
        /// </summary>
        public UnityEvent OnEvaluate => onEvaluate;

        /// <summary>
        ///     Stores the boolean returned by EvaluateInternal after the event runs.
        /// </summary>
        /// <param name="result">Predicate outcome.</param>
        public void SetResult(bool result)
        {
            lastResult = result;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            onEvaluate?.Invoke();
            return lastResult;
        }
    }

    /// <summary>
    ///     Predicate backed by a runtime Func&lt;bool&gt; (not serialized).
    /// </summary>
    [Serializable]
    public class CustomPredicate : StatePredicate
    {
        private Func<bool> customEvaluator;

        /// <summary>
        ///     Assigns the delegate used by EvaluateInternal.
        /// </summary>
        /// <param name="evaluator">Func returning the raw predicate result.</param>
        public void SetEvaluator(Func<bool> evaluator)
        {
            customEvaluator = evaluator;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            return customEvaluator?.Invoke() ?? false;
        }
    }

    /// <summary>
    ///     Compares elapsed time since state entry to a required duration.
    /// </summary>
    [Serializable]
    public class StateDurationPredicate : StatePredicate
    {
        [SerializeField] private float requiredDuration = 1f;

        [SerializeField] private ComparisonType comparison = ComparisonType.GreaterThanOrEqual;

        private float stateEnterTime;

        /// <summary>
        ///     Duration threshold in seconds.
        /// </summary>
        public float RequiredDuration
        {
            get => requiredDuration;
            set => requiredDuration = value;
        }

        /// <summary>
        ///     How elapsed time is compared to <see cref="RequiredDuration"/>.
        /// </summary>
        public ComparisonType Comparison
        {
            get => comparison;
            set => comparison = value;
        }

        /// <summary>
        ///     Sets the reference time (typically state enter Time.time).
        /// </summary>
        /// <param name="enterTime">Time.time when the state was entered.</param>
        public void SetEnterTime(float enterTime)
        {
            stateEnterTime = enterTime;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            float elapsedTime = Time.time - stateEnterTime;
            return comparison switch
            {
                ComparisonType.GreaterThan => elapsedTime > requiredDuration,
                ComparisonType.LessThan => elapsedTime < requiredDuration,
                ComparisonType.GreaterThanOrEqual => elapsedTime >= requiredDuration,
                ComparisonType.LessThanOrEqual => elapsedTime <= requiredDuration,
                ComparisonType.Equal => Mathf.Approximately(elapsedTime, requiredDuration),
                ComparisonType.NotEqual => !Mathf.Approximately(elapsedTime, requiredDuration),
                _ => false
            };
        }
    }

    /// <summary>
    ///     Combines child predicates with AND semantics.
    /// </summary>
    [Serializable]
    public class AndPredicate : StatePredicate
    {
        [SerializeReference] [SerializeField] private List<StatePredicate> predicates = new();

        /// <summary>
        ///     Child predicates (all must pass).
        /// </summary>
        public List<StatePredicate> Predicates => predicates;

        /// <summary>
        ///     Adds a child predicate if not already present.
        /// </summary>
        /// <param name="predicate">Child predicate.</param>
        public void AddPredicate(StatePredicate predicate)
        {
            if (predicate != null && !predicates.Contains(predicate))
            {
                predicates.Add(predicate);
            }
        }

        /// <summary>
        ///     Removes a child predicate.
        /// </summary>
        /// <param name="predicate">Child predicate.</param>
        public void RemovePredicate(StatePredicate predicate)
        {
            predicates.Remove(predicate);
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (predicates.Count == 0)
            {
                return true;
            }

            foreach (StatePredicate predicate in predicates)
            {
                if (predicate == null)
                {
                    continue;
                }

                if (!predicate.Evaluate(currentState))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    ///     Combines child predicates with OR semantics.
    /// </summary>
    [Serializable]
    public class OrPredicate : StatePredicate
    {
        [SerializeReference] [SerializeField] private List<StatePredicate> predicates = new();

        /// <summary>
        ///     Child predicates (any may pass).
        /// </summary>
        public List<StatePredicate> Predicates => predicates;

        /// <summary>
        ///     Adds a child predicate if not already present.
        /// </summary>
        /// <param name="predicate">Child predicate.</param>
        public void AddPredicate(StatePredicate predicate)
        {
            if (predicate != null && !predicates.Contains(predicate))
            {
                predicates.Add(predicate);
            }
        }

        /// <summary>
        ///     Removes a child predicate.
        /// </summary>
        /// <param name="predicate">Child predicate.</param>
        public void RemovePredicate(StatePredicate predicate)
        {
            predicates.Remove(predicate);
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (predicates.Count == 0)
            {
                return false;
            }

            foreach (StatePredicate predicate in predicates)
            {
                if (predicate == null)
                {
                    continue;
                }

                if (predicate.Evaluate(currentState))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Negates another predicate's result (distinct from <see cref="StatePredicate.IsInverted"/> on a single node).
    /// </summary>
    [Serializable]
    public class NotPredicate : StatePredicate
    {
        [SerializeReference] [SerializeField] private StatePredicate predicate;

        /// <summary>
        ///     Predicate whose result is negated.
        /// </summary>
        public StatePredicate Predicate
        {
            get => predicate;
            set => predicate = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (predicate == null)
            {
                return true;
            }

            return !predicate.Evaluate(currentState);
        }
    }
}
