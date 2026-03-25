using System;
using System.Collections.Generic;
using System.Linq;
using Neo.StateMachine.NoCode;
using UnityEngine;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Describes a transition between states in the State Machine.
    ///     Supports gated transitions via predicates and explicit priority ordering.
    /// </summary>
    /// <remarks>
    ///     Works with code states (CLR types) or NoCode StateMachineData (StateData assets).
    ///     Multiple predicates are combined with AND semantics.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var transition = new StateTransition
    /// {
    ///     fromStateType = typeof(IdleState),
    ///     toStateType = typeof(RunningState),
    ///     priority = 1
    /// };
    /// 
    /// transition.AddPredicate(new FloatComparisonPredicate
    /// {
    ///     Value = player.Health,
    ///     Comparison = ComparisonType.GreaterThan,
    ///     Threshold = 50f
    /// });
    /// 
    /// stateMachine.RegisterTransition(transition);
    /// </code>
    /// </example>
    [Serializable]
    public class StateTransition
    {
        [SerializeField] private StateData fromStateData;

        [SerializeField] private StateData toStateData;

        [SerializeField] private int priority;

        [SerializeField] private bool isEnabled = true;

        [SerializeField] private string transitionName = "Unnamed Transition";

        [SerializeReference] [SerializeField] private List<StatePredicate> predicates = new();

        private Type fromStateType;

        private Type toStateType;

        /// <summary>
        ///     Source state type (code-driven machine).
        /// </summary>
        public Type FromStateType
        {
            get => fromStateType;
            set => fromStateType = value;
        }

        /// <summary>
        ///     Target state type (code-driven machine).
        /// </summary>
        public Type ToStateType
        {
            get => toStateType;
            set => toStateType = value;
        }

        /// <summary>
        ///     Source StateData asset (NoCode / StateMachineData).
        /// </summary>
        public StateData FromStateData
        {
            get => fromStateData;
            set => fromStateData = value;
        }

        /// <summary>
        ///     Target StateData asset (NoCode / StateMachineData).
        /// </summary>
        public StateData ToStateData
        {
            get => toStateData;
            set => toStateData = value;
        }

        /// <summary>
        ///     Source state name from the ScriptableObject, if any.
        /// </summary>
        public string FromStateName => fromStateData != null ? fromStateData.StateName : "";

        /// <summary>
        ///     Target state name from the ScriptableObject, if any.
        /// </summary>
        public string ToStateName => toStateData != null ? toStateData.StateName : "";

        /// <summary>
        ///     Predicates that must all pass for the transition to fire.
        /// </summary>
        public List<StatePredicate> Predicates => predicates;

        /// <summary>
        ///     Higher priority transitions are evaluated first.
        /// </summary>
        public int Priority
        {
            get => priority;
            set => priority = value;
        }

        /// <summary>
        ///     When false, the transition is skipped during evaluation.
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        /// <summary>
        ///     Debug / Inspector label for this transition.
        /// </summary>
        public string TransitionName
        {
            get => transitionName;
            set => transitionName = value;
        }

        /// <summary>
        ///     Whether this transition may fire from the given current state (type / asset match + predicates).
        /// </summary>
        /// <param name="currentState">Active state.</param>
        /// <returns>True if the transition is allowed.</returns>
        public bool CanTransition(IState currentState)
        {
            if (!isEnabled)
            {
                return false;
            }

            if (currentState == null)
            {
                return false;
            }

            // Code path: match CLR type
            if (fromStateType != null)
            {
                if (currentState.GetType() != fromStateType)
                {
                    return false;
                }
            }

            // NoCode path: match StateData reference
            if (fromStateData != null)
            {
                if (currentState is StateData currentStateData)
                {
                    if (currentStateData != fromStateData)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return EvaluatePredicates(currentState);
        }

        /// <summary>
        ///     Evaluates predicates without checking source state (enabled flag still applies).
        /// </summary>
        /// <returns>True if every predicate passes.</returns>
        public bool Evaluate()
        {
            if (!isEnabled)
            {
                return false;
            }

            if (predicates.Count == 0)
            {
                return true;
            }

            // All predicates must pass (AND)
            return predicates.All(p => p != null && p.Evaluate());
        }

        /// <summary>
        ///     Evaluates predicates with the current state passed through to each predicate.
        /// </summary>
        /// <param name="currentState">Active state.</param>
        /// <returns>True if every predicate passes.</returns>
        public bool EvaluatePredicates(IState currentState)
        {
            if (!isEnabled)
            {
                return false;
            }

            if (predicates == null || predicates.Count == 0)
            {
                return true;
            }

            foreach (StatePredicate p in predicates)
            {
                if (p == null)
                {
                    return false;
                }

                if (!p.Evaluate(currentState))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Adds a predicate if not already present.
        /// </summary>
        /// <param name="predicate">Predicate instance.</param>
        public void AddPredicate(StatePredicate predicate)
        {
            if (predicate != null && !predicates.Contains(predicate))
            {
                predicates.Add(predicate);
            }
        }

        /// <summary>
        ///     Removes a predicate from the list.
        /// </summary>
        /// <param name="predicate">Predicate instance.</param>
        public void RemovePredicate(StatePredicate predicate)
        {
            predicates.Remove(predicate);
        }

        /// <summary>
        ///     True if this transition originates from the given CLR state type.
        /// </summary>
        /// <param name="stateType">State type to test.</param>
        /// <returns>True when FromStateType matches.</returns>
        public bool MatchesFromState(Type stateType)
        {
            if (fromStateType != null)
            {
                return fromStateType == stateType;
            }

            return false;
        }

        /// <summary>
        ///     True if this NoCode transition originates from the named state.
        /// </summary>
        /// <param name="stateName">State name to test.</param>
        /// <returns>True when FromStateData name matches.</returns>
        public bool MatchesFromState(string stateName)
        {
            if (fromStateData != null)
            {
                return fromStateData.StateName == stateName;
            }

            return false;
        }
    }
}
