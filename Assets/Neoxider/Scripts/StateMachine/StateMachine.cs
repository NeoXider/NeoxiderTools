using System;
using System.Collections.Generic;
using System.Linq;
using Neo.StateMachine.NoCode;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Core State Machine class with optional caching of states and transitions.
    ///     Manages the state lifecycle and transitions between states.
    /// </summary>
    /// <typeparam name="TState">State type; must implement IState.</typeparam>
    /// <remarks>
    ///     The State Machine caches state instances and transitions by default for performance.
    ///     Caching can be disabled via the constructor.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var stateMachine = new StateMachine&lt;IState&gt;();
    /// 
    /// // Register transitions
    /// var transition = new StateTransition
    /// {
    ///     FromStateType = typeof(IdleState),
    ///     ToStateType = typeof(RunningState)
    /// };
    /// stateMachine.RegisterTransition(transition);
    /// 
    /// // Change state
    /// stateMachine.ChangeState&lt;IdleState&gt;();
    /// 
    /// // Tick
    /// stateMachine.Update();
    /// stateMachine.EvaluateTransitions(); // Automatic transition evaluation
    /// </code>
    /// </example>
    public class StateMachine<TState> where TState : class, IState
    {
        private readonly bool enableStateCaching;
        private readonly bool enableTransitionCaching;
        private readonly List<StateTransition> globalTransitions = new();
        private readonly Dictionary<Type, TState> stateCache = new();
        private readonly Dictionary<Type, List<StateTransition>> transitionCache = new();
        private readonly Dictionary<Type, List<StateTransition>> _sortedTransitionsCache = new();
        private bool _sortedTransitionsDirty;

        /// <summary>
        ///     Creates a new State Machine instance.
        /// </summary>
        /// <param name="enableStateCaching">Enable state instance caching (default true).</param>
        /// <param name="enableTransitionCaching">Enable transition caching (default true).</param>
        public StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)
        {
            this.enableStateCaching = enableStateCaching;
            this.enableTransitionCaching = enableTransitionCaching;
        }

        /// <summary>
        ///     Raised when the active state changes (after exit/enter).
        /// </summary>
        public UnityEvent<TState, TState> OnStateChanged { get; } = new();

        /// <summary>
        ///     Raised when entering a new state.
        /// </summary>
        public UnityEvent<TState> OnStateEntered { get; } = new();

        /// <summary>
        ///     Raised when leaving a state.
        /// </summary>
        public UnityEvent<TState> OnStateExited { get; } = new();

        /// <summary>
        ///     Raised for each transition evaluation.
        /// </summary>
        public UnityEvent<StateTransition, bool> OnTransitionEvaluated { get; } = new();

        /// <summary>
        ///     Currently active state.
        /// </summary>
        public TState CurrentState { get; private set; }

        /// <summary>
        ///     Previous state before the last change.
        /// </summary>
        public TState PreviousState { get; private set; }

        /// <summary>
        ///     Gets or creates a state instance, using the cache when enabled.
        /// </summary>
        /// <typeparam name="T">Concrete state type.</typeparam>
        /// <returns>State instance.</returns>
        public T GetOrCreateState<T>() where T : class, TState, new()
        {
            Type stateType = typeof(T);

            if (enableStateCaching && stateCache.TryGetValue(stateType, out TState cachedState))
            {
                return cachedState as T;
            }

            T newState = new();

            if (enableStateCaching)
            {
                stateCache[stateType] = newState;
            }

            return newState;
        }

        /// <summary>
        ///     Changes state by type (creates instance via GetOrCreateState).
        /// </summary>
        /// <typeparam name="T">Target state type.</typeparam>
        public void ChangeState<T>() where T : class, TState, new()
        {
            T newState = GetOrCreateState<T>();
            ChangeState(newState);
        }

        /// <summary>
        ///     Changes state to the given instance.
        /// </summary>
        /// <param name="newState">New active state.</param>
        public void ChangeState(TState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[StateMachine] Attempted to change to null state.");
                return;
            }

            if (CurrentState == newState)
            {
                return;
            }

            PreviousState = CurrentState;
            CurrentState = newState;

            PreviousState?.OnExit();
            OnStateExited?.Invoke(PreviousState);

            CurrentState?.OnEnter();
            OnStateEntered?.Invoke(CurrentState);

            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }

        /// <summary>
        ///     Tries to change state by type if transition conditions allow.
        /// </summary>
        /// <typeparam name="T">Target state type.</typeparam>
        /// <returns>True if the state was changed.</returns>
        public bool TryChangeState<T>() where T : class, TState, new()
        {
            if (CanTransitionTo<T>())
            {
                ChangeState<T>();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns whether a transition to the given state type is allowed from the current state.
        /// </summary>
        /// <typeparam name="T">State type to check.</typeparam>
        /// <returns>True if transition is possible.</returns>
        public bool CanTransitionTo<T>() where T : class, TState
        {
            if (CurrentState == null)
            {
                return true;
            }

            Type targetType = typeof(T);
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitions(CurrentState.GetType());

            for (int i = 0; i < transitions.Count; i++)
            {
                StateTransition t = transitions[i];
                if (t.ToStateType == targetType && t.EvaluatePredicates(CurrentState))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Registers a transition with this State Machine.
        /// </summary>
        /// <param name="transition">Transition to register.</param>
        public void RegisterTransition(StateTransition transition)
        {
            if (transition == null)
            {
                Debug.LogWarning("[StateMachine] Attempted to register null transition.");
                return;
            }

            _sortedTransitionsDirty = true;

            if (transition.FromStateType != null)
            {
                if (enableTransitionCaching)
                {
                    Type fromType = transition.FromStateType;
                    if (!transitionCache.ContainsKey(fromType))
                    {
                        transitionCache[fromType] = new List<StateTransition>();
                    }

                    transitionCache[fromType].Add(transition);
                }
            }
            else if (transition.FromStateData != null && enableTransitionCaching)
            {
                Type stateDataType = typeof(StateData);
                if (!transitionCache.ContainsKey(stateDataType))
                {
                    transitionCache[stateDataType] = new List<StateTransition>();
                }

                transitionCache[stateDataType].Add(transition);
            }
            else
            {
                globalTransitions.Add(transition);
            }
        }

        /// <summary>
        ///     Unregisters a transition from this State Machine.
        /// </summary>
        /// <param name="transition">Transition to remove.</param>
        public void UnregisterTransition(StateTransition transition)
        {
            if (transition == null)
            {
                return;
            }

            _sortedTransitionsDirty = true;

            if (transition.FromStateType != null && enableTransitionCaching)
            {
                Type fromType = transition.FromStateType;
                if (transitionCache.TryGetValue(fromType, out List<StateTransition> transitions))
                {
                    transitions.Remove(transition);
                    if (transitions.Count == 0)
                    {
                        transitionCache.Remove(fromType);
                    }
                }
            }
            else if (transition.FromStateData != null && enableTransitionCaching)
            {
                Type stateDataType = typeof(StateData);
                if (transitionCache.TryGetValue(stateDataType, out List<StateTransition> transitions))
                {
                    transitions.Remove(transition);
                    if (transitions.Count == 0)
                    {
                        transitionCache.Remove(stateDataType);
                    }
                }
            }
            else
            {
                globalTransitions.Remove(transition);
            }
        }

        /// <summary>
        ///     Returns transitions available from the given state type.
        /// </summary>
        /// <param name="fromStateType">Source state type.</param>
        /// <returns>List of applicable transitions.</returns>
        public IReadOnlyList<StateTransition> GetAvailableTransitions(Type fromStateType)
        {
            if (_sortedTransitionsDirty)
            {
                _sortedTransitionsCache.Clear();
                _sortedTransitionsDirty = false;
            }

            if (_sortedTransitionsCache.TryGetValue(fromStateType, out List<StateTransition> cachedSorted))
            {
                return cachedSorted;
            }

            List<StateTransition> availableTransitions = new();

            if (enableTransitionCaching && transitionCache.TryGetValue(fromStateType, out List<StateTransition> cached))
            {
                availableTransitions.AddRange(cached);
            }

            availableTransitions.AddRange(globalTransitions);

            // In-place sort prevents OrderByDescending+ToList allocations
            availableTransitions.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            _sortedTransitionsCache[fromStateType] = availableTransitions;
            return availableTransitions;
        }

        /// <summary>
        ///     Evaluates transitions and performs the first one whose conditions pass.
        /// </summary>
        public void EvaluateTransitions()
        {
            if (CurrentState == null)
            {
                return;
            }

            Type currentType = CurrentState.GetType();
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitions(currentType);

            for (int i = 0; i < transitions.Count; i++)
            {
                StateTransition transition = transitions[i];
                if (!transition.IsEnabled)
                {
                    continue;
                }

                bool canTransition = transition.CanTransition(CurrentState);
                OnTransitionEvaluated?.Invoke(transition, canTransition);

                if (!canTransition)
                {
                    continue;
                }

                if (TryApplyTransitionTarget(transition))
                {
                    break; // Only the first matching transition runs
                }
            }
        }

        /// <summary>
        ///     Calls OnUpdate on the current state.
        /// </summary>
        public void Update()
        {
            CurrentState?.OnUpdate();
        }

        /// <summary>
        ///     Calls OnFixedUpdate on the current state.
        /// </summary>
        public void FixedUpdate()
        {
            CurrentState?.OnFixedUpdate();
        }

        /// <summary>
        ///     Calls OnLateUpdate on the current state.
        /// </summary>
        public void LateUpdate()
        {
            CurrentState?.OnLateUpdate();
        }

        /// <summary>
        ///     Clears the state instance cache.
        /// </summary>
        public void ClearStateCache()
        {
            stateCache.Clear();
        }

        /// <summary>
        ///     Clears registered transitions and the transition cache.
        /// </summary>
        public void ClearTransitionCache()
        {
            transitionCache.Clear();
            globalTransitions.Clear();
            _sortedTransitionsCache.Clear();
            _sortedTransitionsDirty = true;
        }

        private bool TryApplyTransitionTarget(StateTransition transition)
        {
            if (transition.ToStateType != null)
            {
                ChangeStateByType(transition.ToStateType);
                return true;
            }

            if (transition.ToStateData is TState noCodeState)
            {
                ChangeState(noCodeState);
                return true;
            }

            return false;
        }

        private void ChangeStateByType(Type stateType)
        {
            if (stateType == null)
            {
                return;
            }

            if (enableStateCaching && stateCache.TryGetValue(stateType, out TState cachedState))
            {
                ChangeState(cachedState);
                return;
            }

            try
            {
                var newState = Activator.CreateInstance(stateType) as TState;
                if (newState != null)
                {
                    if (enableStateCaching)
                    {
                        stateCache[stateType] = newState;
                    }

                    ChangeState(newState);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateMachine] Failed to create state of type {stateType.Name}: {ex.Message}");
            }
        }
    }
}
