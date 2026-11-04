using System;
using Neo.StateMachine.NoCode;
using UnityEngine;

namespace Neo.StateMachine
{
    /// <summary>
    ///     MonoBehaviour-hosted State Machine: ticks Update / FixedUpdate / LateUpdate for the active state.
    /// </summary>
    /// <typeparam name="TState">State type; must implement IState.</typeparam>
    /// <remarks>
    ///     Add to a GameObject for automatic lifecycle wiring.
    ///     Supports code-defined states and ScriptableObject configuration via StateMachineData.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class PlayerStateMachine : StateMachineBehaviour&lt;IState&gt;
    /// {
    ///     private void Start()
    ///     {
    ///         ChangeState&lt;IdleState&gt;();
    ///     }
    /// }
    /// </code>
    /// </example>
    [NeoDoc("StateMachine/StateMachineBehaviour.md")]
    [CreateFromMenu("Neoxider/Tools/State Machine/StateMachineBehaviour")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(StateMachineBehaviour))]
    public class StateMachineBehaviour<TState> : MonoBehaviour where TState : class, IState
    {
        [Header("Settings")]
        [SerializeField]
        [Tooltip(
            "Initial C# state entered in Start when no StateMachineData is set: full type name (Namespace.State) or assembly-qualified name.")]
        private string initialStateTypeName;

        [SerializeField] [Tooltip("Enable state transition logging")]
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
            "GameObjects for transition conditions. In conditions use Context Slot: Owner / Override1..5. Assign here in scene.")]
        private GameObject[] contextOverrides = new GameObject[0];

        private Type initialStateType;

        private StateMachine<TState> stateMachine;
        private bool initialized;
        private bool started;

        /// <summary>
        ///     Underlying State Machine instance.
        /// </summary>
        public StateMachine<TState> StateMachine
        {
            get
            {
                if (stateMachine == null)
                {
                    stateMachine = new StateMachine<TState> { DebugLogEnabled = enableDebugLog };
                    SetupEvents();
                }

                return stateMachine;
            }
        }

        /// <summary>
        ///     When true (default), transitions are evaluated every Update. Disable to drive evaluation manually
        ///     through <see cref="EvaluateTransitionsNow"/>.
        /// </summary>
        public bool AutoEvaluateTransitions
        {
            get => autoEvaluateTransitions;
            set => autoEvaluateTransitions = value;
        }

        /// <summary>GameObjects mapped to Override1..5 slots in transition conditions (set in the scene).</summary>
        public GameObject[] ContextOverrides => contextOverrides ?? Array.Empty<GameObject>();

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

        /// <summary>Optional NoCode configuration; call <see cref="LoadFromStateMachineData"/> after changing it.</summary>
        public StateMachineData StateMachineData
        {
            get => stateMachineData;
            set => stateMachineData = value;
        }

        /// <summary>
        ///     Assembly-qualified or full ("Namespace.State") name of the C# state entered in Start when no
        ///     StateMachineData is assigned. Resolved in Awake.
        /// </summary>
        public string InitialStateTypeName => initialStateTypeName;

        /// <summary>
        ///     Currently active state.
        /// </summary>
        public TState CurrentState => StateMachine.CurrentState;

        /// <summary>
        ///     State before the last transition.
        /// </summary>
        public TState PreviousState => StateMachine.PreviousState;

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
            else if (initialStateType != null)
            {
                ChangeStateByType(initialStateType);
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
            // WHY: an Inspector toggle during Play Mode reaches the running machine without a re-enable; the field is
            // read directly so validation never creates the machine.
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
        public void ChangeState<T>() where T : class, TState, new()
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
        ///     Changes state by name when using StateMachineData (NoCode).
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
        ///     Changes to the given StateData asset with this component's scene context. The asset must be
        ///     compatible with <typeparamref name="TState"/>; null is ignored.
        /// </summary>
        /// <param name="stateData">Target state asset.</param>
        public void ChangeState(StateData stateData)
        {
            if (stateData == null)
            {
                return;
            }

            // WHY: StateData implements IState; still require compatibility with TState
            if (stateData is not TState state)
            {
                StateMachineLog.Warning(enableDebugLog,
                    $"[StateMachineBehaviour] StateData '{stateData.StateName}' is not compatible with TState type.",
                    this);
                return;
            }

            PushContext();
            try
            {
                StateMachine.ChangeState(state);
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
        ///     Evaluates transitions once immediately (with this component's scene context).
        /// </summary>
        public void EvaluateTransitionsNow()
        {
            EvaluateTransitionsInternal();
        }

        /// <summary>
        ///     Reloads transitions and initial state from StateMachineData.
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
            // WHY: keep a machine configured through the lazy getter before Awake (inactive object, AddComponent
            // followed by RegisterTransition); replacing it here silently dropped that configuration.
            if (stateMachine == null)
            {
                stateMachine = new StateMachine<TState> { DebugLogEnabled = enableDebugLog };
                SetupEvents();
            }

            if (!string.IsNullOrEmpty(initialStateTypeName))
            {
                initialStateType = ResolveStateType(initialStateTypeName);
                // WHY: gated on this component's own flag; the shared StateMachineLog.Enabled is assigned only in
                // OnEnable, after Awake, so routing through it suppressed the warning for a misspelled name.
                if (initialStateType == null && enableDebugLog)
                {
                    NeoDiagnostics.LogWarning(
                        $"[StateMachineBehaviour] Initial state type '{initialStateTypeName}' was not found.", this,
                        true);
                }
            }

            initialized = true;
        }

        /// <summary>
        ///     Resolves a state type from an assembly-qualified or full ("Namespace.State") type name.
        ///     <see cref="Type.GetType(string)"/> alone only searches the calling assembly and mscorlib, so game states
        ///     in Assembly-CSharp or another asmdef named by full name were never found and the initial state was
        ///     silently skipped.
        /// </summary>
        private static Type ResolveStateType(string typeName)
        {
            Type type = Type.GetType(typeName, false);
            if (type != null)
            {
                return type;
            }

            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private void SetupEvents()
        {
            StateMachine.OnStateChanged.AddListener((from, to) =>
            {
                if (enableDebugLog)
                {
                    StateMachineLog.Info(true,
                        $"[StateMachineBehaviour] State changed: {GetStateLogName(from)} -> {GetStateLogName(to)}", this);
                }
            });
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

        private static string GetStateLogName(TState state)
        {
            if (state == null)
            {
                return "None";
            }

            // WHY: every NoCode state shares the StateData type name; the asset's state name tells them apart.
            return state is StateData stateData ? stateData.StateName : state.GetType().Name;
        }

        private void ChangeStateByType(Type stateType)
        {
            if (stateType == null)
            {
                return;
            }

            try
            {
                TState state = Activator.CreateInstance(stateType) as TState;
                if (state != null)
                {
                    // WHY: No ChangeState(Type) — create instance via reflection
                    PushContext();
                    try
                    {
                        StateMachine.ChangeState(state);
                    }
                    finally
                    {
                        StateMachineEvaluationContext.Pop();
                    }
                }
            }
            catch (Exception ex)
            {
                StateMachineLog.Error(enableDebugLog,
                    $"[StateMachineBehaviour] Failed to change state to {stateType.Name}: {ex.Message}",
                    this);
            }
        }
    }
}
