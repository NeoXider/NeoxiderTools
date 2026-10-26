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
    /// StateMachine&lt;IState&gt; stateMachine = new StateMachine&lt;IState&gt;();
    /// 
    /// // Register transitions
    /// StateTransition transition = new StateTransition
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
        private const int MaxLifecycleRequestsPerDispatch = 128;
        private readonly Queue<TState> lifecycleRequests = new();
        private readonly Func<float> timeProvider;
        private bool _sortedTransitionsDirty;
        private bool isDispatchingLifecycle;
        private ulong stateGeneration;
        private float currentStateEnterTime = float.NaN;

        /// <summary>
        ///     Creates a new State Machine instance.
        /// </summary>
        /// <param name="enableStateCaching">Enable state instance caching (default true).</param>
        /// <param name="enableTransitionCaching">Enable transition caching (default true).</param>
        public StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)
            : this(enableStateCaching, enableTransitionCaching, null)
        {
        }

        /// <summary>
        ///     Creates a machine with an optional clock for built-in duration predicates.
        /// </summary>
        /// <param name="enableStateCaching">Enable state instance caching.</param>
        /// <param name="enableTransitionCaching">Enable transition caching.</param>
        /// <param name="timeProvider">Clock in seconds; null uses <see cref="Time.time" />.</param>
        public StateMachine(bool enableStateCaching, bool enableTransitionCaching, Func<float> timeProvider)
        {
            this.enableStateCaching = enableStateCaching;
            this.enableTransitionCaching = enableTransitionCaching;
            this.timeProvider = timeProvider ?? (() => Time.time);
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
        ///     Changes state to the given instance. Requests made during lifecycle dispatch are queued FIFO.
        /// </summary>
        /// <remarks>
        ///     At most 128 requests are processed per outer dispatch. Excess requests are discarded with a gated warning.
        ///     Exceptions discard pending requests and release the dispatcher; completed lifecycle work is not rolled back.
        /// </remarks>
        /// <param name="newState">New active state.</param>
        public void ChangeState(TState newState)
        {
            if (newState == null)
            {
                StateMachineLog.Warning("[StateMachine] Attempted to change to null state.");
                return;
            }

            if (!isDispatchingLifecycle && ReferenceEquals(CurrentState, newState))
            {
                return;
            }

            DispatchLifecycleRequest(newState);
        }

        /// <summary>
        ///     Changes only this machine's duration anchor for its active state, without lifecycle callbacks.
        ///     Use this instead of editing a shared duration predicate after machine entry.
        /// </summary>
        /// <param name="enterTime">Entry time in the same clock domain as this machine's time provider.</param>
        /// <returns>False when the machine is stopped; otherwise true.</returns>
        public bool SetCurrentStateEnterTime(float enterTime)
        {
            if (CurrentState == null)
            {
                return false;
            }

            currentStateEnterTime = enterTime;
            return true;
        }

        /// <summary>
        ///     Preserves custom entry hooks while built-in duration predicates use the machine-owned anchor.
        /// </summary>
        private void AnchorTimeBasedPredicates(TState state)
        {
            if (state == null)
            {
                return;
            }

            float enterTime = currentStateEnterTime;
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitions(state.GetType());
            StateMachineEvaluationContext.PushDuration(enterTime, enterTime);
            try
            {
                for (int i = 0; i < transitions.Count; i++)
                {
                    transitions[i]?.NotifyFromStateEntered(enterTime);
                }
            }
            finally
            {
                StateMachineEvaluationContext.PopDuration();
            }
        }

        /// <summary>
        ///     Tries to change state by type if transition conditions allow.
        /// </summary>
        /// <typeparam name="T">Target state type.</typeparam>
        /// <returns>True if conditions allow the change and the request is dispatched or queued.</returns>
        public bool TryChangeState<T>() where T : class, TState, new()
        {
            ulong evaluatedGeneration = stateGeneration;
            if (CanTransitionTo<T>() && stateGeneration == evaluatedGeneration)
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

            TState evaluatedState = CurrentState;
            ulong evaluatedGeneration = stateGeneration;
            Type targetType = typeof(T);
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitions(evaluatedState.GetType());

            for (int i = 0; i < transitions.Count; i++)
            {
                StateTransition t = transitions[i];
                if (t.ToStateType != targetType)
                {
                    continue;
                }

                bool canTransition = EvaluateTransition(t, evaluatedState);
                if (stateGeneration != evaluatedGeneration)
                {
                    return false;
                }

                if (canTransition)
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
                StateMachineLog.Warning("[StateMachine] Attempted to register null transition.");
                return;
            }

            _sortedTransitionsDirty = true;

            if (transition.FromStateType != null && enableTransitionCaching)
            {
                Type fromType = transition.FromStateType;
                if (!transitionCache.ContainsKey(fromType))
                {
                    transitionCache[fromType] = new List<StateTransition>();
                }

                transitionCache[fromType].Add(transition);
            }
            else if (transition.FromStateData != null && enableTransitionCaching)
            {
                Type stateDataType = transition.FromStateData.GetType();
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

            // Source selectors are mutable. Remove from the original bucket even if the caller changed them.
            foreach (List<StateTransition> transitions in transitionCache.Values)
            {
                transitions.RemoveAll(candidate => ReferenceEquals(candidate, transition));
            }

            globalTransitions.RemoveAll(candidate => ReferenceEquals(candidate, transition));
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
                // Priority can change through its public setter or the Inspector without registration changing.
                for (int i = 1; i < cachedSorted.Count; i++)
                {
                    if (cachedSorted[i - 1].Priority < cachedSorted[i].Priority)
                    {
                        cachedSorted.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                        break;
                    }
                }

                return cachedSorted;
            }

            List<StateTransition> availableTransitions = new();

            if (enableTransitionCaching && transitionCache.TryGetValue(fromStateType, out List<StateTransition> cached))
            {
                availableTransitions.AddRange(cached);
            }

            availableTransitions.AddRange(globalTransitions);

            // WHY: In-place sort prevents OrderByDescending+ToList allocations
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

            TState evaluatedState = CurrentState;
            ulong evaluatedGeneration = stateGeneration;
            Type currentType = evaluatedState.GetType();
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitions(currentType);

            for (int i = 0; i < transitions.Count; i++)
            {
                StateTransition transition = transitions[i];
                if (!transition.IsEnabled)
                {
                    continue;
                }

                bool canTransition = EvaluateTransition(transition, evaluatedState);
                OnTransitionEvaluated?.Invoke(transition, canTransition);

                // Predicates and evaluation listeners may stop the machine or choose another state.
                if (stateGeneration != evaluatedGeneration)
                {
                    return;
                }

                if (!canTransition)
                {
                    continue;
                }

                if (TryApplyTransitionTarget(transition))
                {
                    break; // WHY: Only the first matching transition runs
                }

                if (stateGeneration != evaluatedGeneration)
                {
                    return; // A target constructor or lifecycle callback may have made an explicit choice before failing.
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
        ///     Exits the current state without entering another one. Useful for MonoBehaviour disable/reload lifecycle.
        ///     During lifecycle dispatch, queues the stop after the current notification sequence.
        /// </summary>
        public void Stop()
        {
            if (!isDispatchingLifecycle && CurrentState == null)
            {
                return;
            }

            DispatchLifecycleRequest(null);
        }

        private void DispatchLifecycleRequest(TState requestedState)
        {
            lifecycleRequests.Enqueue(requestedState); // null represents Stop; ChangeState(null) is rejected above.
            // Invalidate an in-flight condition as soon as an explicit request is accepted, even if queued.
            stateGeneration++;
            if (isDispatchingLifecycle)
            {
                return;
            }

            isDispatchingLifecycle = true;
            try
            {
                int processedRequests = 0;
                while (lifecycleRequests.Count > 0 && processedRequests < MaxLifecycleRequestsPerDispatch)
                {
                    TState nextState = lifecycleRequests.Dequeue();
                    processedRequests++;
                    ApplyLifecycleRequest(nextState);
                }

                if (lifecycleRequests.Count > 0)
                {
                    StateMachineLog.Warning("[StateMachine] Lifecycle request limit (128) reached; pending requests discarded.");
                }
            }
            finally
            {
                lifecycleRequests.Clear();
                isDispatchingLifecycle = false;
            }
        }

        private void ApplyLifecycleRequest(TState newState)
        {
            TState previous = CurrentState;
            if (ReferenceEquals(previous, newState))
            {
                return;
            }

            PreviousState = previous;
            if (newState == null)
            {
                // Preserve Stop's existing contract: OnExit sees a stopped machine.
                CurrentState = null;
                currentStateEnterTime = float.NaN;
                previous.OnExit();
                OnStateExited?.Invoke(previous);
                OnStateChanged?.Invoke(previous, null);
                return;
            }

            // ChangeState's OnExit still sees the old current state. Nested requests cannot re-enter this exit.
            previous?.OnExit();
            OnStateExited?.Invoke(previous);
            CurrentState = newState;
            currentStateEnterTime = timeProvider();
            AnchorTimeBasedPredicates(newState);
            newState.OnEnter();
            OnStateEntered?.Invoke(newState);
            OnStateChanged?.Invoke(previous, newState);
        }

        private bool EvaluateTransition(StateTransition transition, TState evaluatedState)
        {
            StateMachineEvaluationContext.PushDuration(currentStateEnterTime, timeProvider());
            try
            {
                return transition.CanTransition(evaluatedState);
            }
            finally
            {
                StateMachineEvaluationContext.PopDuration();
            }
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
                return ChangeStateByType(transition.ToStateType);
            }

            if (transition.ToStateData is TState noCodeState)
            {
                ChangeState(noCodeState);
                return true;
            }

            return false;
        }

        private bool ChangeStateByType(Type stateType)
        {
            if (stateType == null || !stateType.IsClass || !typeof(TState).IsAssignableFrom(stateType) ||
                stateType.IsAbstract || stateType.ContainsGenericParameters ||
                typeof(UnityEngine.Object).IsAssignableFrom(stateType) ||
                stateType.GetConstructor(Type.EmptyTypes) == null)
            {
                return false;
            }

            if (enableStateCaching && stateCache.TryGetValue(stateType, out TState cachedState))
            {
                ChangeState(cachedState);
                return true;
            }

            try
            {
                TState newState = Activator.CreateInstance(stateType) as TState;
                if (newState != null)
                {
                    if (enableStateCaching)
                    {
                        stateCache[stateType] = newState;
                    }

                    ChangeState(newState);
                    return true;
                }
            }
            catch (Exception ex)
            {
                StateMachineLog.Error($"[StateMachine] Failed to create state of type {stateType.Name}: {ex.Message}");
            }

            return false;
        }
    }

    internal static class StateMachineLog
    {
        public static bool Enabled { get; set; }

        public static void Info(string message, UnityEngine.Object context = null)
        {
            if (Enabled)
            {
                Debug.Log(message, context);
            }
        }

        public static void Warning(string message, UnityEngine.Object context = null)
        {
            if (Enabled)
            {
                Debug.LogWarning(message, context);
            }
        }

        public static void Error(string message, UnityEngine.Object context = null)
        {
            if (Enabled)
            {
                Debug.LogError(message, context);
            }
        }
    }
}
