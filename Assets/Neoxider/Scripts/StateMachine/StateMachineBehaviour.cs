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
        [Header("Settings")] [SerializeField] [Tooltip("Initial state (state type for code)")]
        private string initialStateTypeName;

        [SerializeField] [Tooltip("Enable state transition logging")]
        private bool enableDebugLog;

        [SerializeField] [Tooltip("Exit the current state when this component is disabled.")]
        private bool exitCurrentStateOnDisable = true;

        [SerializeField] [Tooltip("Reload StateMachineData when the component is enabled again after disable.")]
        private bool reloadDataOnEnable = true;

        [SerializeField] [Tooltip("Show current state in inspector")]
        private bool showStateInInspector = true;

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
                    stateMachine = new StateMachine<TState>();
                    SetupEvents();
                }

                return stateMachine;
            }
        }

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
            StateMachineLog.Enabled = enableDebugLog;
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

        private void Update()
        {
            RunWithContext(() => StateMachine.Update());

            if (autoEvaluateTransitions)
            {
                EvaluateTransitionsInternal();
            }
        }

        private void FixedUpdate()
        {
            RunWithContext(() => StateMachine.FixedUpdate());
        }

        private void LateUpdate()
        {
            RunWithContext(() => StateMachine.LateUpdate());
        }

        private void OnDestroy()
        {
            StateMachine.Stop();
            StateMachine.ClearStateCache();
            StateMachine.ClearTransitionCache();
        }

        private void OnDisable()
        {
            if (exitCurrentStateOnDisable)
            {
                StateMachine.Stop();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (showStateInInspector && Application.isPlaying && StateMachine != null)
            {
                // WHY: Current state is shown by the custom editor
            }
        }
#endif

        /// <summary>
        ///     Changes state by concrete type.
        /// </summary>
        /// <typeparam name="T">Target state type.</typeparam>
        public void ChangeState<T>() where T : class, TState, new()
        {
            RunWithContext(() => StateMachine.ChangeState<T>());

            if (enableDebugLog)
            {
                StateMachineLog.Info($"[StateMachineBehaviour] Changed state to {typeof(T).Name}", this);
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
                StateMachineLog.Warning(
                    "[StateMachineBehaviour] Cannot change state by name: StateMachineData is null.",
                    this);
                return;
            }

            StateData stateData = Array.Find(stateMachineData.States, s => s != null && s.StateName == stateName);
            if (stateData != null)
            {
                // WHY: StateData implements IState; still require compatibility with TState
                if (stateData is TState state)
                {
                    RunWithContext(() => StateMachine.ChangeState(state));

                    if (enableDebugLog)
                    {
                        StateMachineLog.Info($"[StateMachineBehaviour] Changed state to {stateName}", this);
                    }
                }
                else
                {
                    StateMachineLog.Warning(
                        $"[StateMachineBehaviour] StateData '{stateName}' is not compatible with TState type.", this);
                }
            }
            else
            {
                StateMachineLog.Warning(
                    $"[StateMachineBehaviour] State with name '{stateName}' not found in StateMachineData.", this);
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
        ///     Reloads transitions and initial state from StateMachineData.
        /// </summary>
        public void LoadFromStateMachineData()
        {
            if (stateMachineData == null)
            {
                StateMachineLog.Warning("[StateMachineBehaviour] Cannot load: StateMachineData is null.", this);
                return;
            }

            StateMachine.Stop();
            StateMachine.ClearTransitionCache();
            stateMachineData.LoadIntoStateMachine(StateMachine);

            if (stateMachineData.InitialState != null)
            {
                ChangeState(stateMachineData.InitialState.StateName);
            }
            else if (!string.IsNullOrEmpty(stateMachineData.InitialStateName))
            {
                ChangeState(stateMachineData.InitialStateName);
            }
        }

        private void InitializeStateMachine()
        {
            stateMachine = new StateMachine<TState>();
            SetupEvents();

            if (!string.IsNullOrEmpty(initialStateTypeName))
            {
                initialStateType = Type.GetType(initialStateTypeName);
            }

            initialized = true;
        }

        private void SetupEvents()
        {
            StateMachine.OnStateChanged.AddListener((from, to) =>
            {
                if (enableDebugLog)
                {
                    string fromName = from?.GetType().Name ?? "None";
                    string toName = to?.GetType().Name ?? "None";
                    StateMachineLog.Info($"[StateMachineBehaviour] State changed: {fromName} -> {toName}", this);
                }
            });
        }

        private void EvaluateTransitionsInternal()
        {
            RunWithContext(() => StateMachine.EvaluateTransitions());
        }

        private void RunWithContext(Action action)
        {
            StateMachineEvaluationContext.Push(gameObject,
                contextOverrides != null && contextOverrides.Length > 0 ? contextOverrides : null);
            try
            {
                action?.Invoke();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }
        }

        private void ChangeStateByType(Type stateType)
        {
            if (stateType == null)
            {
                return;
            }

            try
            {
                var state = Activator.CreateInstance(stateType) as TState;
                if (state != null)
                {
                    // WHY: No ChangeState(Type) — create instance via reflection
                    RunWithContext(() => StateMachine.ChangeState(state));
                }
            }
            catch (Exception ex)
            {
                StateMachineLog.Error(
                    $"[StateMachineBehaviour] Failed to change state to {stateType.Name}: {ex.Message}",
                    this);
            }
        }
    }
}
