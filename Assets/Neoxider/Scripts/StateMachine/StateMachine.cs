using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    ///     Each lookup returns a priority-sorted list that is rebuilt only after a registration change or a routing
    ///     edit (<see cref="StateTransition.Priority"/>, <see cref="StateTransition.FromStateType"/>,
    ///     <see cref="StateTransition.FromStateData"/>); the rebuild reads the current source selectors, so a source
    ///     edited after registration routes from its new state. NoCode (<see cref="StateData"/>) states get their own
    ///     list per asset, so a frame evaluates only the transitions that leave the active state.
    ///     Diagnostics are per machine (<see cref="DebugLogEnabled"/>).
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
    /// stateMachine.EvaluateTransitions();
    /// </code>
    /// </example>
    public class StateMachine<TState> where TState : class, IState
    {
        private readonly bool enableStateCaching;
        private readonly bool enableTransitionCaching;

        // WHY: one registration-ordered list classified when a lookup is rebuilt. Buckets chosen at registration
        // kept a transition under the source type it had then, so a FromStateType edited later never routed from
        // its new state (and a cleared one never became source-less).
        private readonly List<StateTransition> registeredTransitions = new();
        private readonly Dictionary<Type, TState> stateCache = new();
        private readonly Dictionary<Type, TransitionLookup> _sortedTransitionsCache = new();

        // WHY: every StateData shares one CLR type, so a type-keyed list held the transitions of all NoCode states and
        // each frame evaluated (and reported) all of them; a per-asset list keeps the walk O(outgoing).
        private readonly Dictionary<StateData, TransitionLookup> _sortedStateDataTransitions =
            new(StateDataReferenceComparer.Instance);

        private readonly List<StateTransition> _lookupScratch = new();

        // WHY: type shape never changes at runtime; re-running GetConstructor on every automatic transition
        // allocated reflection arrays even when the target instance was already cached.
        private readonly Dictionary<Type, bool> stateTypeAdmissionCache = new();
        private const int MaxLifecycleRequestsPerDispatch = 128;
        private readonly Queue<TState> lifecycleRequests = new();
        private readonly Func<float> timeProvider;
        private bool _sortedTransitionsDirty;
        private bool isDispatchingLifecycle;
        private ulong stateGeneration;
        private float currentStateEnterTime = float.NaN;
        private bool? debugLogEnabled;

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
        ///     Per-machine diagnostics switch. Gates this machine's warnings and errors and, while it runs state
        ///     callbacks (enter, exit, update) or lifecycle listeners, the logs of the NoCode actions those callbacks
        ///     execute (<see cref="LogStateAction"/>, action errors). Until it is assigned it follows the shared
        ///     switch that StateMachineBehaviour components write in OnEnable (the legacy behaviour); both wrappers
        ///     assign their own Enable Debug Log, so one component's setting does not change another machine's logs.
        /// </summary>
        public bool DebugLogEnabled
        {
            get => debugLogEnabled ?? StateMachineLog.Enabled;
            set => debugLogEnabled = value;
        }

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
        /// <remarks>
        ///     A new instance whose constructor changes or stops this machine is returned but not cached:
        ///     the constructor's newer state choice supersedes the activation that requested the instance.
        /// </remarks>
        /// <typeparam name="T">Concrete state type.</typeparam>
        /// <returns>State instance.</returns>
        public T GetOrCreateState<T>() where T : class, TState, new()
        {
            Type stateType = typeof(T);

            if (enableStateCaching && stateCache.TryGetValue(stateType, out TState cachedState))
            {
                return cachedState as T;
            }

            ulong creationGeneration = stateGeneration;
            T newState = new();

            if (enableStateCaching && stateGeneration == creationGeneration)
            {
                stateCache[stateType] = newState;
            }

            return newState;
        }

        /// <summary>
        ///     Changes state by type (creates instance via GetOrCreateState).
        /// </summary>
        /// <remarks>
        ///     If the target constructor changes or stops this machine, that newer choice wins and the
        ///     constructed target is neither entered nor cached.
        /// </remarks>
        /// <typeparam name="T">Target state type.</typeparam>
        public void ChangeState<T>() where T : class, TState, new()
        {
            ulong activationGeneration = stateGeneration;
            T newState = GetOrCreateState<T>();
            if (stateGeneration == activationGeneration)
            {
                ChangeState(newState);
            }
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
                StateMachineLog.Warning(DebugLogEnabled, "[StateMachine] Attempted to change to null state.");
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
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitionsFrom(state);
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
        /// <returns>
        ///     True if conditions allow the change and the request is dispatched or queued; false when a
        ///     predicate, listener or the target constructor made a newer state choice first.
        /// </returns>
        public bool TryChangeState<T>() where T : class, TState, new()
        {
            ulong evaluatedGeneration = stateGeneration;
            if (CanTransitionTo<T>() && stateGeneration == evaluatedGeneration)
            {
                T newState = GetOrCreateState<T>();
                if (stateGeneration != evaluatedGeneration)
                {
                    return false;
                }

                ChangeState(newState);
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
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitionsFrom(evaluatedState);

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
                StateMachineLog.Warning(DebugLogEnabled, "[StateMachine] Attempted to register null transition.");
                return;
            }

            _sortedTransitionsDirty = true;
            registeredTransitions.Add(transition);
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
            RemoveReference(registeredTransitions, transition);
        }

        /// <summary>
        ///     Returns the transitions registered for the given state type plus the global (source-less) ones, sorted
        ///     by descending <see cref="StateTransition.Priority"/> (equal priorities keep registration order).
        /// </summary>
        /// <remarks>
        ///     For <c>typeof(StateData)</c> this is every NoCode transition of the machine; the evaluation itself uses
        ///     <see cref="GetAvailableTransitionsFrom"/>, which narrows NoCode states to the transitions leaving that asset.
        ///     The returned list is cached and must not be modified; it is replaced (not mutated) when registrations or
        ///     routing change, so a caller may keep iterating it.
        /// </remarks>
        /// <param name="fromStateType">Source state type.</param>
        /// <returns>List of applicable transitions.</returns>
        public IReadOnlyList<StateTransition> GetAvailableTransitions(Type fromStateType)
        {
            DropListsAfterRegistrationChange();
            int routingVersion = StateTransition.RoutingVersion;
            if (_sortedTransitionsCache.TryGetValue(fromStateType, out TransitionLookup lookup))
            {
                if (lookup.RoutingVersion != routingVersion)
                {
                    FillTypeTransitions(fromStateType, _lookupScratch);
                    lookup.Revalidate(_lookupScratch, routingVersion);
                }

                return lookup.Transitions;
            }

            List<StateTransition> availableTransitions = new();
            FillTypeTransitions(fromStateType, availableTransitions);
            _sortedTransitionsCache[fromStateType] = new TransitionLookup(availableTransitions, routingVersion);
            return availableTransitions;
        }

        /// <summary>
        ///     Returns the transitions that <see cref="EvaluateTransitions"/>, <see cref="CanTransitionTo{T}"/> and
        ///     state-entry notifications consider for <paramref name="state"/>, sorted like
        ///     <see cref="GetAvailableTransitions"/>. A <see cref="StateData"/> state gets only the transitions whose
        ///     current <see cref="StateTransition.FromStateData"/> is that asset (or that have no source); a C# state
        ///     gets <see cref="GetAvailableTransitions"/> for its type.
        /// </summary>
        /// <param name="state">Source state; null returns an empty list.</param>
        /// <returns>Cached, read-only list; do not modify it.</returns>
        public IReadOnlyList<StateTransition> GetAvailableTransitionsFrom(TState state)
        {
            if (state == null)
            {
                return Array.Empty<StateTransition>();
            }

            if (state is StateData stateData)
            {
                return GetStateDataTransitions(stateData);
            }

            return GetAvailableTransitions(state.GetType());
        }

        private IReadOnlyList<StateTransition> GetStateDataTransitions(StateData state)
        {
            DropListsAfterRegistrationChange();
            int routingVersion = StateTransition.RoutingVersion;
            if (_sortedStateDataTransitions.TryGetValue(state, out TransitionLookup lookup))
            {
                if (lookup.RoutingVersion != routingVersion)
                {
                    FillStateDataTransitions(state, _lookupScratch);
                    lookup.Revalidate(_lookupScratch, routingVersion);
                }

                return lookup.Transitions;
            }

            List<StateTransition> leaving = new();
            FillStateDataTransitions(state, leaving);
            _sortedStateDataTransitions[state] = new TransitionLookup(leaving, routingVersion);
            return leaving;
        }

        private void DropListsAfterRegistrationChange()
        {
            if (!_sortedTransitionsDirty)
            {
                return;
            }

            // WHY: dropped lists are replaced, never refilled, so an evaluation still iterating one keeps its snapshot.
            _sortedTransitionsCache.Clear();
            _sortedStateDataTransitions.Clear();
            _sortedTransitionsDirty = false;
        }

        /// <summary>
        ///     Source type a transition is listed under: its <see cref="StateTransition.FromStateType"/>, else the type
        ///     of its <see cref="StateTransition.FromStateData"/>; null for a source-less (global) transition and for
        ///     every transition when transition caching is off.
        /// </summary>
        private Type GetRoutingKey(StateTransition transition)
        {
            if (!enableTransitionCaching)
            {
                return null;
            }

            Type fromType = transition.FromStateType;
            if (fromType != null)
            {
                return fromType;
            }

            // WHY: a destroyed source asset keeps the NoCode key it had when it was alive; an unassigned one (also the
            // Editor's empty placeholder for an unassigned serialized field) leaves the transition source-less.
            StateData fromData = transition.FromStateData;
            return fromData != null || Neo.Condition.BindingSourceGameObjectResolver.IsDestroyedReference(fromData)
                ? fromData.GetType()
                : null;
        }

        private void FillTypeTransitions(Type fromStateType, List<StateTransition> destination)
        {
            // WHY: transitions of the type first, then source-less ones, each in registration order; the stable
            // sort below keeps that order for equal priorities.
            if (fromStateType != null)
            {
                for (int i = 0; i < registeredTransitions.Count; i++)
                {
                    StateTransition transition = registeredTransitions[i];
                    if (GetRoutingKey(transition) == fromStateType)
                    {
                        destination.Add(transition);
                    }
                }
            }

            for (int i = 0; i < registeredTransitions.Count; i++)
            {
                StateTransition transition = registeredTransitions[i];
                if (GetRoutingKey(transition) == null)
                {
                    destination.Add(transition);
                }
            }

            SortByPriorityDescending(destination);
        }

        private void FillStateDataTransitions(StateData state, List<StateTransition> destination)
        {
            Type stateType = state.GetType();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < registeredTransitions.Count; i++)
                {
                    StateTransition transition = registeredTransitions[i];
                    Type key = GetRoutingKey(transition);
                    bool inPass = pass == 0 ? key == stateType : key == null;
                    if (inPass && LeavesState(transition, state, stateType))
                    {
                        destination.Add(transition);
                    }
                }
            }

            SortByPriorityDescending(destination);
        }

        private static bool LeavesState(StateTransition transition, StateData state, Type stateType)
        {
            Type fromType = transition.FromStateType;
            if (fromType != null && fromType != stateType)
            {
                return false;
            }

            // WHY: mirrors StateTransition.CanTransition - a missing (or destroyed) source asset does not restrict
            // the source, and any other asset never matches this one.
            StateData fromData = transition.FromStateData;
            return ReferenceEquals(fromData, state) || fromData == null;
        }

        /// <summary>Stable insertion sort by descending priority; transition lists per state are short.</summary>
        private static void SortByPriorityDescending(List<StateTransition> transitions)
        {
            for (int i = 1; i < transitions.Count; i++)
            {
                StateTransition item = transitions[i];
                int priority = item.Priority;
                int j = i - 1;
                while (j >= 0 && transitions[j].Priority < priority)
                {
                    transitions[j + 1] = transitions[j];
                    j--;
                }

                transitions[j + 1] = item;
            }
        }

        private static void RemoveReference(List<StateTransition> transitions, StateTransition transition)
        {
            for (int i = transitions.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(transitions[i], transition))
                {
                    transitions.RemoveAt(i);
                }
            }
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
            IReadOnlyList<StateTransition> transitions = GetAvailableTransitionsFrom(evaluatedState);

            for (int i = 0; i < transitions.Count; i++)
            {
                StateTransition transition = transitions[i];
                if (!transition.IsEnabled)
                {
                    continue;
                }

                bool canTransition = EvaluateTransition(transition, evaluatedState);
                OnTransitionEvaluated?.Invoke(transition, canTransition);

                // WHY: predicates and evaluation listeners may stop the machine or choose another state.
                if (stateGeneration != evaluatedGeneration)
                {
                    return;
                }

                if (!canTransition)
                {
                    continue;
                }

                // WHY: only the first matching transition runs.
                if (TryApplyTransitionTarget(transition))
                {
                    break;
                }

                // WHY: a target constructor or lifecycle callback may have made an explicit choice before failing.
                if (stateGeneration != evaluatedGeneration)
                {
                    return;
                }
            }
        }

        /// <summary>
        ///     Calls OnUpdate on the current state.
        /// </summary>
        public void Update()
        {
            TState state = CurrentState;
            if (state == null)
            {
                return;
            }

            byte logScope = StateMachineLog.EnterScope(DebugLogEnabled);
            try
            {
                state.OnUpdate();
            }
            finally
            {
                StateMachineLog.ExitScope(logScope);
            }
        }

        /// <summary>
        ///     Calls OnFixedUpdate on the current state.
        /// </summary>
        public void FixedUpdate()
        {
            TState state = CurrentState;
            if (state == null)
            {
                return;
            }

            byte logScope = StateMachineLog.EnterScope(DebugLogEnabled);
            try
            {
                state.OnFixedUpdate();
            }
            finally
            {
                StateMachineLog.ExitScope(logScope);
            }
        }

        /// <summary>
        ///     Calls OnLateUpdate on the current state.
        /// </summary>
        public void LateUpdate()
        {
            TState state = CurrentState;
            if (state == null)
            {
                return;
            }

            byte logScope = StateMachineLog.EnterScope(DebugLogEnabled);
            try
            {
                state.OnLateUpdate();
            }
            finally
            {
                StateMachineLog.ExitScope(logScope);
            }
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
            // WHY: null represents Stop (ChangeState(null) is rejected earlier); the generation invalidates an
            // in-flight condition as soon as an explicit request is accepted, even if it is only queued.
            lifecycleRequests.Enqueue(requestedState);
            stateGeneration++;
            if (isDispatchingLifecycle)
            {
                return;
            }

            isDispatchingLifecycle = true;

            // WHY: exits, entries and their listeners run inside this machine's log scope, so NoCode actions log by
            // this machine's DebugLogEnabled instead of whichever behaviour enabled last.
            byte logScope = StateMachineLog.EnterScope(DebugLogEnabled);
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
                    StateMachineLog.Warning(DebugLogEnabled,
                        "[StateMachine] Lifecycle request limit (128) reached; pending requests discarded.");
                }
            }
            finally
            {
                lifecycleRequests.Clear();
                isDispatchingLifecycle = false;
                StateMachineLog.ExitScope(logScope);
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
                // WHY: Stop's existing contract - OnExit sees a stopped machine.
                CurrentState = null;
                currentStateEnterTime = float.NaN;
                previous.OnExit();
                OnStateExited?.Invoke(previous);
                OnStateChanged?.Invoke(previous, null);
                return;
            }

            // WHY: ChangeState's OnExit still sees the old current state; nested requests cannot re-enter this exit.
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
            registeredTransitions.Clear();
            _sortedTransitionsCache.Clear();
            _sortedStateDataTransitions.Clear();
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

        private bool IsActivatableStateType(Type stateType)
        {
            if (stateType == null)
            {
                return false;
            }

            if (stateTypeAdmissionCache.TryGetValue(stateType, out bool admitted))
            {
                return admitted;
            }

            admitted = stateType.IsClass && typeof(TState).IsAssignableFrom(stateType) &&
                       !stateType.IsAbstract && !stateType.ContainsGenericParameters &&
                       !typeof(UnityEngine.Object).IsAssignableFrom(stateType) &&
                       stateType.GetConstructor(Type.EmptyTypes) != null;
            stateTypeAdmissionCache[stateType] = admitted;
            return admitted;
        }

        private bool ChangeStateByType(Type stateType)
        {
            if (!IsActivatableStateType(stateType))
            {
                return false;
            }

            if (enableStateCaching && stateCache.TryGetValue(stateType, out TState cachedState))
            {
                ChangeState(cachedState);
                return true;
            }

            ulong activationGeneration = stateGeneration;
            try
            {
                TState newState = Activator.CreateInstance(stateType) as TState;
                if (stateGeneration != activationGeneration)
                {
                    return false;
                }

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
                if (DebugLogEnabled)
                {
                    // WHY: Activator wraps constructor faults; the inner exception names the actual cause.
                    Exception cause = ex is TargetInvocationException { InnerException: not null } invocation
                        ? invocation.InnerException
                        : ex;
                    StateMachineLog.Error(true,
                        $"[StateMachine] Failed to create state of type {stateType.Name}: {cause.Message}");
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     One cached, priority-sorted transition list plus the <see cref="StateTransition.RoutingVersion"/> it was
    ///     last checked against.
    /// </summary>
    internal sealed class TransitionLookup
    {
        public TransitionLookup(List<StateTransition> transitions, int routingVersion)
        {
            Transitions = transitions;
            RoutingVersion = routingVersion;
        }

        public List<StateTransition> Transitions { get; private set; }

        public int RoutingVersion { get; private set; }

        /// <summary>
        ///     Keeps the current list when <paramref name="rebuilt"/> has the same order, otherwise replaces it with a
        ///     copy. Clears <paramref name="rebuilt"/>.
        /// </summary>
        /// <remarks>
        ///     WHY: a routing edit anywhere bumps the shared version; comparing instead of replacing keeps unrelated
        ///     machines allocation-free, and replacing instead of refilling keeps a list that is being iterated intact.
        /// </remarks>
        public void Revalidate(List<StateTransition> rebuilt, int routingVersion)
        {
            List<StateTransition> current = Transitions;
            bool same = current.Count == rebuilt.Count;
            for (int i = 0; same && i < rebuilt.Count; i++)
            {
                same = ReferenceEquals(current[i], rebuilt[i]);
            }

            if (!same)
            {
                Transitions = new List<StateTransition>(rebuilt);
            }

            rebuilt.Clear();
            RoutingVersion = routingVersion;
        }
    }

    /// <summary>Identity comparer: a destroyed StateData must not collide with another destroyed asset as a key.</summary>
    internal sealed class StateDataReferenceComparer : IEqualityComparer<StateData>
    {
        public static readonly StateDataReferenceComparer Instance = new();

        public bool Equals(StateData x, StateData y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(StateData obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    ///     Debug-only log gateway of the State Machine module. Machine and wrapper logs pass their own switch; logs
    ///     from code that has no machine reference (NoCode actions, StateData) follow the innermost machine scope and,
    ///     outside any scope, the shared <see cref="Enabled"/> switch.
    /// </summary>
    internal static class StateMachineLog
    {
        private const byte NoScope = 0;
        private const byte ScopeOff = 1;
        private const byte ScopeOn = 2;

        // WHY: thread-static like StateMachineEvaluationContext; callers save and restore the previous value instead
        // of pushing onto a stack, so a scope per tick costs no allocation.
        [ThreadStatic] private static byte s_scope;

        /// <summary>
        ///     Shared switch for logs that run outside any machine scope; StateMachineBehaviour components write it in
        ///     OnEnable, and machines whose <see cref="StateMachine{TState}.DebugLogEnabled"/> is unassigned follow it.
        /// </summary>
        public static bool Enabled { get; set; }

        /// <summary>True when the innermost machine scope enables logs; outside any scope, <see cref="Enabled"/>.</summary>
        public static bool ScopeEnabled => s_scope == NoScope ? Enabled : s_scope == ScopeOn;

        /// <summary>Opens a machine log scope; pass the returned value to <see cref="ExitScope"/>.</summary>
        public static byte EnterScope(bool enabled)
        {
            byte previous = s_scope;
            s_scope = enabled ? ScopeOn : ScopeOff;
            return previous;
        }

        /// <summary>Restores the scope that was active before the matching <see cref="EnterScope"/>.</summary>
        public static void ExitScope(byte previous)
        {
            s_scope = previous;
        }

        public static void Info(string message, UnityEngine.Object context = null)
        {
            Info(ScopeEnabled, message, context);
        }

        public static void Info(bool enabled, string message, UnityEngine.Object context = null)
        {
            if (enabled)
            {
                Debug.Log(message, context);
            }
        }

        public static void Warning(string message, UnityEngine.Object context = null)
        {
            Warning(ScopeEnabled, message, context);
        }

        public static void Warning(bool enabled, string message, UnityEngine.Object context = null)
        {
            if (enabled)
            {
                Debug.LogWarning(message, context);
            }
        }

        public static void Error(string message, UnityEngine.Object context = null)
        {
            Error(ScopeEnabled, message, context);
        }

        public static void Error(bool enabled, string message, UnityEngine.Object context = null)
        {
            if (enabled)
            {
                Debug.LogError(message, context);
            }
        }
    }
}
