using System;
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
    /// var sm = gameObject.AddComponent&lt;StateMachineBehaviourBase&gt;();
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

        private StateMachine<IState> stateMachine;

        /// <summary>
        ///     Underlying State Machine instance.
        /// </summary>
        public StateMachine<IState> StateMachine
        {
            get
            {
                if (stateMachine == null)
                {
                    stateMachine = new StateMachine<IState>();
                    SetupEvents();
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

        public string CurrentStateName => currentStateName;

        public string PreviousStateName => previousStateName;

        public int StateChangeCount => stateChangeCount;

        public float CurrentStateElapsedTime => CurrentState == null ? 0f : Mathf.Max(0f, Time.time - stateEnterTime);

        public bool HasCurrentState => CurrentState != null;

        /// <summary>GameObjects mapped to Override1..5 slots in transition conditions (set in the scene).</summary>
        public GameObject[] ContextOverrides => contextOverrides ?? new GameObject[0];

        private void Awake()
        {
            InitializeStateMachine();
        }

        private void Start()
        {
            if (stateMachineData != null)
            {
                LoadFromStateMachineData();
            }
        }

        private void Update()
        {
            StateMachine.Update();

            if (autoEvaluateTransitions)
            {
                EvaluateTransitionsInternal();
            }
        }

        private void FixedUpdate()
        {
            StateMachine.FixedUpdate();
        }

        private void LateUpdate()
        {
            StateMachine.LateUpdate();
        }

        private void OnDestroy()
        {
            StateMachine?.ClearStateCache();
            StateMachine?.ClearTransitionCache();
        }

        /// <summary>
        ///     Changes state by concrete type.
        /// </summary>
        /// <typeparam name="T">Target state type.</typeparam>
        public void ChangeState<T>() where T : class, IState, new()
        {
            StateMachine.ChangeState<T>();

            if (enableDebugLog)
            {
                Debug.Log($"[StateMachineBehaviour] Changed state to {typeof(T).Name}", this);
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
                Debug.LogWarning("[StateMachineBehaviour] Cannot change state by name: StateMachineData is null.",
                    this);
                return;
            }

            StateData stateData = Array.Find(stateMachineData.States, s => s != null && s.StateName == stateName);
            if (stateData != null)
            {
                StateMachine.ChangeState(stateData);

                if (enableDebugLog)
                {
                    Debug.Log($"[StateMachineBehaviour] Changed state to {stateName}", this);
                }
            }
            else
            {
                Debug.LogWarning(
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
                StateMachine.ChangeState(stateMachineData.InitialState);
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
                Debug.LogWarning("[StateMachineBehaviour] Cannot load: StateMachineData is null.", this);
                return;
            }

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
            stateMachine = new StateMachine<IState>();
            SetupEvents();
            currentStateName = "";
            previousStateName = "";
            stateChangeCount = 0;
            stateEnterTime = Time.time;
            onInitialized?.Invoke();
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
                    Debug.Log($"[StateMachineBehaviour] State changed: {fromName} -> {toName}", this);
                }
            });

            StateMachine.OnTransitionEvaluated.AddListener((transition, result) =>
            {
                string transitionName = GetTransitionName(transition);
                onTransitionEvaluated?.Invoke(transitionName, result);
            });
        }

        private void EvaluateTransitionsInternal()
        {
            StateMachineEvaluationContext.Push(gameObject,
                contextOverrides != null && contextOverrides.Length > 0 ? contextOverrides : null);
            try
            {
                StateMachine.EvaluateTransitions();
            }
            finally
            {
                StateMachineEvaluationContext.Pop();
            }
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

        private static string GetTransitionName(StateTransition transition)
        {
            if (transition == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(transition.TransitionName))
            {
                return transition.TransitionName;
            }

            string from = transition.FromStateData != null
                ? transition.FromStateData.StateName
                : transition.FromStateType?.Name;
            string to = transition.ToStateData != null
                ? transition.ToStateData.StateName
                : transition.ToStateType?.Name;

            return $"{from ?? "Any"} -> {to ?? "None"}";
        }

        [Serializable]
        public class StateChangedEvent : UnityEvent<string, string>
        {
        }

        [Serializable]
        public class TransitionEvaluatedEvent : UnityEvent<string, bool>
        {
        }
    }
}
