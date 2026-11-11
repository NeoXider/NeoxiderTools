using System;
using System.Collections.Generic;
using System.Threading;
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
    ///     Changing <see cref="Priority"/>, <see cref="FromStateType"/> or <see cref="FromStateData"/> (through the
    ///     setters, the Inspector or any Unity deserialization) invalidates the sorted transition caches of every
    ///     <see cref="StateMachine{TState}"/>; the next evaluation rebuilds them once.
    /// </remarks>
    /// <example>
    ///     <code>
    /// StateTransition transition = new StateTransition
    /// {
    ///     FromStateType = typeof(IdleState),
    ///     ToStateType = typeof(RunningState),
    ///     Priority = 1
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
    public class StateTransition : ISerializationCallbackReceiver
    {
        // WHY: one process-wide counter instead of one per transition: a machine compares a single int per lookup,
        // so a warm evaluation never re-checks priority order or source selectors, and rare routing edits rebuild.
        private static int s_routingVersion;

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
            set
            {
                if (fromStateType == value)
                {
                    return;
                }

                fromStateType = value;
                MarkRoutingChanged();
            }
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
            set
            {
                if (ReferenceEquals(fromStateData, value))
                {
                    return;
                }

                fromStateData = value;
                MarkRoutingChanged();
            }
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
        ///     Higher priority transitions are evaluated first; equal priorities keep registration order.
        /// </summary>
        public int Priority
        {
            get => priority;
            set
            {
                if (priority == value)
                {
                    return;
                }

                priority = value;
                MarkRoutingChanged();
            }
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

            if (fromStateType != null)
            {
                if (currentState.GetType() != fromStateType)
                {
                    return false;
                }
            }

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

            for (int i = 0; i < predicates.Count; i++)
            {
                StatePredicate p = predicates[i];
                if (p == null || !p.Evaluate())
                {
                    return false;
                }
            }

            return true;
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
        ///     Tells every predicate that the source state just became active, so time-based predicates measure
        ///     from that moment instead of from scene load.
        /// </summary>
        /// <param name="enterTime">Entry time in the machine's clock domain (normally UnityEngine.Time.time).</param>
        public void NotifyFromStateEntered(float enterTime)
        {
            if (predicates == null)
            {
                return;
            }

            for (int i = 0; i < predicates.Count; i++)
            {
                predicates[i]?.OnOwnerStateEntered(enterTime);
            }
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
        ///     Process-wide routing version: changes whenever any transition's priority or source selector changes.
        ///     State machines compare it once per lookup instead of re-checking every transition; custom tooling that
        ///     caches transition order can do the same.
        /// </summary>
        public static int RoutingVersion => Volatile.Read(ref s_routingVersion);

        /// <summary>
        ///     Invalidates the sorted transition caches of every state machine. The setters and Unity deserialization
        ///     call it already; call it after writing the serialized priority or source fields through another path (for
        ///     example reflection or a custom serializer that bypasses Unity's serialization callbacks).
        /// </summary>
        public static void MarkRoutingChanged()
        {
            Interlocked.Increment(ref s_routingVersion);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // WHY: Inspector edits, Undo and JsonUtility write the serialized priority/source fields directly; Unity
            // may run this on a loading thread, hence the interlocked counter.
            MarkRoutingChanged();
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
