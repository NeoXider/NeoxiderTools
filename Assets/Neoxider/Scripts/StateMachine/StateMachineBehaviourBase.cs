using System;
using Neo.StateMachine.NoCode;
using UnityEngine;

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
    [CreateFromMenu("Neoxider/Tools/State Machine Behaviour")]
    [AddComponentMenu("Neoxider/Tools/State Machine Behaviour")]
    public class StateMachineBehaviourBase : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] [Tooltip("Enable state transition logging")]
        private bool enableDebugLog;

        [SerializeField] [Tooltip("Show current state in inspector")]
        private bool showStateInInspector = true;

        [SerializeField] [Tooltip("Evaluate transitions every frame")]
        private bool autoEvaluateTransitions = true;

        [Header("References")] [SerializeField] [Tooltip("NoCode State Machine config (optional)")]
        private StateMachineData stateMachineData;

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
                StateMachine.EvaluateTransitions();
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
        ///     Загрузить конфигурацию из StateMachineData.
        /// </summary>
        public void LoadFromStateMachineData()
        {
            if (stateMachineData == null)
            {
                Debug.LogWarning("[StateMachineBehaviour] Cannot load: StateMachineData is null.", this);
                return;
            }

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
        }

        private void SetupEvents()
        {
            StateMachine.OnStateChanged.AddListener((from, to) =>
            {
                if (enableDebugLog)
                {
                    string fromName = from?.GetType().Name ?? "None";
                    string toName = to?.GetType().Name ?? "None";
                    Debug.Log($"[StateMachineBehaviour] State changed: {fromName} -> {toName}", this);
                }
            });
        }
    }
}
