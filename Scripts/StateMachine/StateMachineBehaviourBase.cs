using System;
using System.Runtime.CompilerServices;
using Neo.StateMachine.NoCode;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Non-generic State Machine behaviour for attaching to GameObjects in the Editor.
    /// </summary>
    /// <remarks>
    ///     Add via the Unity Component menu.
    ///     Uses <see cref="IState"/> as the state type; use <see cref="StateMachineBehaviour{TState}"/> for typed states.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Add via Component &gt; Neo &gt; Tools &gt; State Machine Behaviour
    /// // Or from code:
    /// StateMachineBehaviourBase sm = gameObject.AddComponent&lt;StateMachineBehaviourBase&gt;();
    /// sm.ChangeState("Idle");
    /// </code>
    /// </example>
    [NeoDoc("StateMachine/StateMachineBehaviourBase.md")]
    [CreateFromMenu("Neoxider/Tools/State Machine/State Machine Behaviour")]
    [AddComponentMenu("Neoxider/Tools/State Machine Behaviour")]
    public class StateMachineBehaviourBase : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] [Tooltip("Enable state transition logging")]
        private bool enableDebugLog;

        [SerializeField] [Tooltip("Exit the current state when this component is disabled.")]
        private bool exitCurrentStateOnDisable = true;

        [SerializeField] [Tooltip("Reload StateMachineData when the component is enabled again after disable.")]
        private bool reloadDataOnEnable = true;

#pragma warning disable 0414
        [SerializeField] [Tooltip("Show current state in inspector")]
        private bool showStateInInspector = true;
#pragma warning restore 0414

        [SerializeField] [Tooltip("Evaluate transitions every frame")]
        private bool autoEvaluateTransitions = true;

        [Header("References")] [SerializeField] [Tooltip("State Machine config — ScriptableObject (optional).")]
        private StateMachineData stateMachineData;

        [Header("Context for conditions")]
        [SerializeField]
        [Tooltip(
            "GameObjects for transition conditions. In StateMachineData set Condition Context Slot: Owner = this object, Override1 = element 0, Override2 = element 1, etc. SO cannot reference scene objects — assign here in scene.")]
        private GameObject[] contextOverrides = new GameObject[0];

        [Header("Events")] [SerializeField] private UnityEvent onInitialized = new();

        [SerializeField] private UnityEvent onStateEntered = new();

        [SerializeField] private UnityEvent onStateExited = new();

        [SerializeField] private StateChangedEvent onStateChanged = new();

        [SerializeField] private TransitionEvaluatedEvent onTransitionEvaluated = new();

        [Header("Runtime State")] [SerializeField]
        private string currentStateName = "";

        [SerializeField] private string previousStateName = "";
        [SerializeField] private int stateChangeCount;
        [SerializeField] private float stateEnterTime;

        // WHY: weak keys - an unnamed transition that is unregistered and dropped by its owner must not be kept alive
        // (with its StateData) for the lifetime of this component just because its label was once reported.
        private readonly ConditionalWeakTable<StateTransition, TransitionNameEntry> transitionNameCache = new();
        private StateMachine<IState> stateMachine;
        private bool initialized;
        private bool started;

        /// <summary>
        ///     Underlying State Machine instance. Created on first access, so it can be configured before Awake
        ///     (for example on an inactive object); Awake keeps that instance.
        /// </summary>
        public StateMachine<IState> StateMachine
        {
            get
            {
                if (stateMachine == null)
                {
                    CreateStateMachine();
                }

                return stateMachine;
            }
        }

        /// <summary>
        ///     Currently active state.
        /// </summary>
        public IState CurrentState => StateMachine.CurrentState;

        /// <summary>
        ///     State before the last transition.
        /// </summary>
        public IState PreviousState => StateMachine.PreviousState;

        /// <summary>Name of the active state (StateData name or C# type name); empty when stopped.</summary>
        public string CurrentStateName => currentStateName;

        /// <summary>Name of the state before the last change; empty when there was none.</summary>
        public string PreviousStateName => previousStateName;

        /// <summary>Number of state changes since initialization (stops included).</summary>
        public int StateChangeCount => stateChangeCount;

        /// <summary>Seconds (<see cref="Time.time"/>) since the active state was entered; 0 when stopped.</summary>
        public float CurrentStateElapsedTime => CurrentState == null ? 0f : Mathf.Max(0f, Time.time - stateEnterTime);

        /// <summary>True while a state is active.</summary>
        public bool HasCurrentState => CurrentState != null;

        /// <summary>GameObjects mapped to Override1..5 slots in transition conditions (set in the scene).</summary>
        public GameObject[] ContextOverrides => contextOverrides ?? Array.Empty<GameObject>();

        /// <summary>
        ///     When true (default), transitions are evaluated every Update. Disable to drive evaluation manually
        ///     through <see cref="EvaluateTransitionsNow"/>.
        /// </summary>
        public bool AutoEvaluateTransitions
        {
            get => autoEvaluateTransitions;
            set => autoEvaluateTransitions = value;
        }

        /// <summary>
        ///     This component's Enable Debug Log: gates its own logs and its machine's
        ///     (<see cref="Neo.StateMachine.StateMachine{TState}.DebugLogEnabled"/>), including the NoCode actions the
        ///     machine runs. Another behaviour's setting does not change it.
        /// </summary>
        public bool EnableDebugLog
        {
            get => enableDebugLog;
            set
            {
                enableDebugLog = value;
                if (stateMachine != null)
                {
                    stateMachine.DebugLogEnabled = value;
                }
            }
        }

        /// <summary>Optional NoCode configuration; call <see cref="ReloadFromStateMachineData"/> after changing it.</summary>
        public StateMachineData StateMachineData
        {
            get => stateMachineData;
            set => stateMachineData = value;
        }

        /// <summary>Invoked once when the component initializes its machine (Awake).</summary>
        public UnityEvent OnInitialized => onInitialized;

        /// <summary>Invoked after a state is entered.</summary>
        public UnityEvent OnStateEntered => onStateEntered;

        /// <summary>Invoked after a state is exited (including Stop).</summary>
        public UnityEvent OnStateExited => onStateExited;

        /// <summary>Invoked on every change with (previous name, current name); names are empty for "no state".</summary>
        public StateChangedEvent OnStateChanged => onStateChanged;

        /// <summary>
        ///     Invoked for every evaluated transition with (transition name, result). Unnamed transitions report a
        ///     cached "From -> To" label.
        /// </summary>
        public TransitionEvaluatedEvent OnTransitionEvaluated => onTransitionEvaluated;

        private void Awake()
        {
            InitializeStateMachine();
        }

        private void OnEnable()
        {
            // WHY: the shared switch only serves logs that run outside any machine (asset validation); this
            // machine and the actions it runs use its own DebugLogEnabled.
            StateMachineLog.Enabled = enableDebugLog;
            StateMachine.DebugLogEnabled = enableDebugLog;
            if (!initialized)
            {
                InitializeStateMachine();
            }

            if (started && reloadDataOnEnable && stateMachineData != null)
            {
                LoadFromStateMachineData();
            }
        }

        private void Start()
        {
            started = true;
            if (stateMachineData != null)
            {
                LoadFromStateMachineData();
            }
        }

        // WHY: the per-frame callbacks push the scene context inline; the former RunWithContext(() => ...) lambdas
        // captured 'this' and allocated a delegate on every Update/FixedUpdate/LateUpdate/evaluation.
        private void Update()
        {
            PushContext();
            try
            {
                StateMachine.Update();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }

            if (autoEvaluateTransitions)
            {
                EvaluateTransitionsInternal();
            }
        }

        private void FixedUpdate()
        {
            PushContext();
            try
            {
                StateMachine.FixedUpdate();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }
        }

        private void LateUpdate()
        {
            PushContext();
            try
            {
                StateMachine.LateUpdate();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }
        }

        private void OnDestroy()
        {
            StopWithContext();
            StateMachine.ClearStateCache();
            StateMachine.ClearTransitionCache();
        }

        private void OnDisable()
        {
            if (exitCurrentStateOnDisable)
            {
                StopWithContext();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // WHY: an Inspector toggle during Play Mode reaches the running machine without a re-enable.
            if (stateMachine != null)
            {
                stateMachine.DebugLogEnabled = enableDebugLog;
            }
        }
#endif

        /// <summary>
        ///     Changes state by concrete type.
        /// </summary>
        /// <typeparam name="T">Target state type.</typeparam>
        public void ChangeState<T>() where T : class, IState, new()
        {
            PushContext();
            try
            {
                StateMachine.ChangeState<T>();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }

            if (enableDebugLog)
            {
                StateMachineLog.Info(true, $"[StateMachineBehaviour] Changed state to {typeof(T).Name}", this);
            }
        }

        /// <summary>
        ///     Changes state by name when using StateMachineData.
        /// </summary>
        /// <param name="stateName">State name on StateData.</param>
        public void ChangeState(string stateName)
        {
            if (stateMachineData == null)
            {
                StateMachineLog.Warning(enableDebugLog,
                    "[StateMachineBehaviour] Cannot change state by name: StateMachineData is null.",
                    this);
                return;
            }

            StateData stateData = stateMachineData.GetStateByName(stateName);
            if (stateData != null)
            {
                ChangeState(stateData);
            }
            else
            {
                StateMachineLog.Warning(enableDebugLog,
                    $"[StateMachineBehaviour] State with name '{stateName}' not found in StateMachineData.", this);
            }
        }

        /// <summary>
        ///     Changes to the given StateData asset with this component's scene context (typed equivalent of
        ///     <see cref="ChangeState(string)"/>; also works for states not listed in StateMachineData).
        /// </summary>
        /// <param name="stateData">Target state asset; null is ignored.</param>
        public void ChangeState(StateData stateData)
        {
            if (stateData == null)
            {
                return;
            }

            PushContext();
            try
            {
                StateMachine.ChangeState(stateData);
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }

            if (enableDebugLog)
            {
                StateMachineLog.Info(true, $"[StateMachineBehaviour] Changed state to {stateData.StateName}", this);
            }
        }

        /// <summary>
        ///     Registers a transition on the internal State Machine.
        /// </summary>
        /// <param name="transition">Transition to add.</param>
        public void RegisterTransition(StateTransition transition)
        {
            StateMachine.RegisterTransition(transition);
        }

        /// <summary>
        ///     Evaluates transitions once immediately.
        /// </summary>
        public void EvaluateTransitionsNow()
        {
            EvaluateTransitionsInternal();
        }

        /// <summary>
        ///     Reloads configuration from StateMachineData.
        /// </summary>
        public void ReloadFromStateMachineData()
        {
            LoadFromStateMachineData();
        }

        /// <summary>
        ///     Transitions to the initial state defined on StateMachineData.
        /// </summary>
        public void GoToInitialState()
        {
            if (stateMachineData == null)
            {
                return;
            }

            if (stateMachineData.InitialState != null)
            {
                ChangeState(stateMachineData.InitialState);
                return;
            }

            if (!string.IsNullOrEmpty(stateMachineData.InitialStateName))
            {
                ChangeState(stateMachineData.InitialStateName);
            }
        }

        /// <summary>
        ///     Loads transitions and initial state from StateMachineData.
        /// </summary>
        public void LoadFromStateMachineData()
        {
            if (stateMachineData == null)
            {
                StateMachineLog.Warning(enableDebugLog, "[StateMachineBehaviour] Cannot load: StateMachineData is null.",
                    this);
                return;
            }

            StopWithContext();
            StateMachine.ClearTransitionCache();
            stateMachineData.LoadIntoStateMachine(StateMachine);

            // WHY: enter the assigned asset itself. Resolving it by name picked the first state with that name, so
            // two states left with the default "New State" name could start the machine in the wrong state.
            if (stateMachineData.InitialState != null)
            {
                ChangeState(stateMachineData.InitialState);
            }
            else if (!string.IsNullOrEmpty(stateMachineData.InitialStateName))
            {
                ChangeState(stateMachineData.InitialStateName);
            }
        }

        private void InitializeStateMachine()
        {
            // WHY: code may configure the machine (RegisterTransition, ChangeState, listeners) through the lazy
            // StateMachine getter before Awake, e.g. on a component added to an inactive object. Replacing that
            // instance in Awake silently dropped the configuration.
            if (stateMachine == null)
            {
                CreateStateMachine();
            }

            initialized = true;
            onInitialized?.Invoke();
        }

        private void CreateStateMachine()
        {
            stateMachine = new StateMachine<IState> { DebugLogEnabled = enableDebugLog };
            currentStateName = "";
            previousStateName = "";
            stateChangeCount = 0;
            stateEnterTime = Time.time;
            SetupEvents();
        }

        private void SetupEvents()
        {
            StateMachine.OnStateEntered.AddListener(state =>
            {
                currentStateName = GetStateName(state);
                stateEnterTime = Time.time;
                onStateEntered?.Invoke();
            });

            StateMachine.OnStateExited.AddListener(_ => { onStateExited?.Invoke(); });

            StateMachine.OnStateChanged.AddListener((from, to) =>
            {
                previousStateName = GetStateName(from);
                currentStateName = GetStateName(to);
                stateChangeCount++;
                onStateChanged?.Invoke(previousStateName, currentStateName);

                if (enableDebugLog)
                {
                    string fromName = string.IsNullOrEmpty(previousStateName) ? "None" : previousStateName;
                    string toName = string.IsNullOrEmpty(currentStateName) ? "None" : currentStateName;
                    StateMachineLog.Info(true, $"[StateMachineBehaviour] State changed: {fromName} -> {toName}", this);
                }
            });

            StateMachine.OnTransitionEvaluated.AddListener((transition, result) =>
            {
                string transitionName = GetTransitionName(transition);
                onTransitionEvaluated?.Invoke(transitionName, result);
            });
        }

        private string GetTransitionName(StateTransition transition)
        {
            if (transition == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(transition.TransitionName))
            {
                return transition.TransitionName;
            }

            // WHY: unnamed transitions are reported for every evaluation (every frame); reuse the fallback label
            // while its inputs are the same instances instead of concatenating (and Type.Name-allocating) each time.
            if (transitionNameCache.TryGetValue(transition, out TransitionNameEntry cached))
            {
                if (!cached.Matches(transition))
                {
                    cached.Rebuild(transition);
                }

                return cached.Name;
            }

            TransitionNameEntry entry = new();
            entry.Rebuild(transition);
            transitionNameCache.Add(transition, entry);
            return entry.Name;
        }

        private void EvaluateTransitionsInternal()
        {
            PushContext();
            try
            {
                StateMachine.EvaluateTransitions();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }
        }

        private void StopWithContext()
        {
            PushContext();
            try
            {
                StateMachine.Stop();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }
        }

        private void PushContext()
        {
            StateMachineEvaluationContext.Push(gameObject,
                contextOverrides != null && contextOverrides.Length > 0 ? contextOverrides : null);
        }

        private static string GetStateName(IState state)
        {
            if (state == null)
            {
                return "";
            }

            if (state is StateData stateData)
            {
                return stateData.StateName;
            }

            return state.GetType().Name;
        }

        /// <summary>Fallback "From -> To" label of an unnamed transition plus the inputs it was built from.</summary>
        private sealed class TransitionNameEntry
        {
            private StateData fromData;
            private StateData toData;
            private string fromDataName;
            private string toDataName;
            private Type fromType;
            private Type toType;

            public string Name { get; private set; }

            public void Rebuild(StateTransition transition)
            {
                fromData = transition.FromStateData;
                toData = transition.ToStateData;
                fromDataName = fromData != null ? fromData.StateName : null;
                toDataName = toData != null ? toData.StateName : null;
                fromType = transition.FromStateType;
                toType = transition.ToStateType;
                string from = fromData != null ? fromDataName : fromType?.Name;
                string to = toData != null ? toDataName : toType?.Name;
                Name = $"{from ?? "Any"} -> {to ?? "None"}";
            }

            public bool Matches(StateTransition transition)
            {
                StateData currentFrom = transition.FromStateData;
                StateData currentTo = transition.ToStateData;
                return ReferenceEquals(fromData, currentFrom) && ReferenceEquals(toData, currentTo) &&
                       ReferenceEquals(fromType, transition.FromStateType) &&
                       ReferenceEquals(toType, transition.ToStateType) &&
                       ReferenceEquals(fromDataName, currentFrom != null ? currentFrom.StateName : null) &&
                       ReferenceEquals(toDataName, currentTo != null ? currentTo.StateName : null);
            }
        }

        /// <summary>UnityEvent with (previous state name, current state name).</summary>
        [Serializable]
        public class StateChangedEvent : UnityEvent<string, string>
        {
        }

        /// <summary>UnityEvent with (transition name, evaluation result).</summary>
        [Serializable]
        public class TransitionEvaluatedEvent : UnityEvent<string, bool>
        {
        }
    }
}
