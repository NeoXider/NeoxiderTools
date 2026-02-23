using System;
using Neo.StateMachine.NoCode;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Базовый MonoBehaviour класс State Machine для использования на GameObject.
    ///     Не-generic версия для возможности добавления на GameObject в Unity.
    /// </summary>
    /// <remarks>
    ///     Этот компонент можно добавить на GameObject через меню Unity.
    ///     Использует IState как базовый тип состояний.
    ///     Для более специфичных типов состояний используйте StateMachineBehaviour&lt;TState&gt;.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Добавить на GameObject через: Component > Neo > Tools > State Machine Behaviour
    /// // Или использовать в коде:
    /// var sm = gameObject.AddComponent&lt;StateMachineBehaviourBase&gt;();
    /// sm.ChangeState("Idle");
    /// </code>
    /// </example>
    [NeoDoc("StateMachine/StateMachineBehaviourBase.md")]
    [CreateFromMenu("Neoxider/Tools/State Machine/State Machine Behaviour")]
    [AddComponentMenu("Neoxider/Tools/State Machine Behaviour")]
    public class StateMachineBehaviourBase : MonoBehaviour
    {
        [Serializable]
        public class StateChangedEvent : UnityEvent<string, string>
        {
        }

        [Serializable]
        public class TransitionEvaluatedEvent : UnityEvent<string, bool>
        {
        }

        [Header("Settings")] [SerializeField] [Tooltip("Enable state transition logging")]
        private bool enableDebugLog;

        [SerializeField] [Tooltip("Show current state in inspector")]
        private bool showStateInInspector = true;

        [SerializeField] [Tooltip("Evaluate transitions every frame")]
        private bool autoEvaluateTransitions = true;

        [Header("References")] [SerializeField] [Tooltip("NoCode State Machine config (optional)")]
        private StateMachineData stateMachineData;

        [Header("Context for conditions")]
        [SerializeField]
        [Tooltip("GameObjects for transition conditions. In StateMachineData set Condition Context Slot: Owner = this object, Override1 = element 0, Override2 = element 1, etc. SO cannot reference scene objects — assign here in scene.")]
        private GameObject[] contextOverrides = new GameObject[0];

        [Header("Events")] [SerializeField]
        private UnityEvent onInitialized = new();

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
        ///     Получить экземпляр State Machine.
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
        ///     Текущее активное состояние.
        /// </summary>
        public IState CurrentState => StateMachine.CurrentState;

        /// <summary>
        ///     Предыдущее состояние.
        /// </summary>
        public IState PreviousState => StateMachine.PreviousState;

        public string CurrentStateName => currentStateName;

        public string PreviousStateName => previousStateName;

        public int StateChangeCount => stateChangeCount;

        public float CurrentStateElapsedTime => CurrentState == null ? 0f : Mathf.Max(0f, Time.time - stateEnterTime);

        public bool HasCurrentState => CurrentState != null;

        /// <summary>Объекты для слотов Override1..5 в условиях переходов (задаются в сцене).</summary>
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
        ///     Сменить состояние по типу.
        /// </summary>
        /// <typeparam name="T">Тип нового состояния.</typeparam>
        public void ChangeState<T>() where T : class, IState, new()
        {
            StateMachine.ChangeState<T>();

            if (enableDebugLog)
            {
                Debug.Log($"[StateMachineBehaviour] Changed state to {typeof(T).Name}", this);
            }
        }

        /// <summary>
        ///     Сменить состояние по имени (для NoCode).
        /// </summary>
        /// <param name="stateName">Имя состояния.</param>
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
        ///     Зарегистрировать переход.
        /// </summary>
        /// <param name="transition">Переход для регистрации.</param>
        public void RegisterTransition(StateTransition transition)
        {
            StateMachine.RegisterTransition(transition);
        }

        /// <summary>
        ///     Оценить переходы один раз вручную.
        /// </summary>
        public void EvaluateTransitionsNow()
        {
            EvaluateTransitionsInternal();
        }

        /// <summary>
        ///     Перезагрузить конфигурацию из StateMachineData.
        /// </summary>
        public void ReloadFromStateMachineData()
        {
            LoadFromStateMachineData();
        }

        /// <summary>
        ///     Попробовать перейти в начальное состояние из StateMachineData.
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
        ///     Загрузить конфигурацию из StateMachineData.
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

            StateMachine.OnStateExited.AddListener(_ =>
            {
                onStateExited?.Invoke();
            });

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
            StateMachineEvaluationContext.Push(gameObject, contextOverrides != null && contextOverrides.Length > 0 ? contextOverrides : null);
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

            string from = transition.FromStateData != null ? transition.FromStateData.StateName : transition.FromStateType?.Name;
            string to = transition.ToStateData != null ? transition.ToStateData.StateName : transition.ToStateType?.Name;

            return $"{from ?? "Any"} -> {to ?? "None"}";
        }
    }
}