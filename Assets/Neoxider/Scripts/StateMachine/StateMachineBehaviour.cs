using System;
using System.Reflection;
using Neo.StateMachine.NoCode;
using UnityEngine;
#if ODIN_INSPECTOR
#endif

namespace Neo.StateMachine
{
    /// <summary>
    ///     MonoBehaviour версия State Machine для использования на GameObject.
    ///     Автоматически вызывает Update, FixedUpdate и LateUpdate для текущего состояния.
    /// </summary>
    /// <typeparam name="TState">Тип состояний, должен реализовывать IState.</typeparam>
    /// <remarks>
    ///     Этот компонент можно добавить на GameObject для автоматического управления состояниями.
    ///     Поддерживает как код-состояния, так и NoCode конфигурацию через StateMachineData.
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
    [AddComponentMenu("Neo/Tools/" + nameof(StateMachineBehaviour))]
    public class StateMachineBehaviour<TState> : MonoBehaviour where TState : class, IState
    {
        [SerializeField] [Tooltip("Начальное состояние (тип состояния для кода)")]
        private string initialStateTypeName;

        [SerializeField] [Tooltip("Включить логирование переходов состояний")]
        private bool enableDebugLog;

        [SerializeField] [Tooltip("Показывать текущее состояние в инспекторе")]
        private bool showStateInInspector = true;

        [SerializeField] [Tooltip("NoCode конфигурация State Machine (опционально)")]
        private StateMachineData stateMachineData;

        [SerializeField] [Tooltip("Автоматически оценивать переходы каждый кадр")]
        private bool autoEvaluateTransitions = true;

        private Type initialStateType;

        private StateMachine<TState> stateMachine;

        /// <summary>
        ///     Получить экземпляр State Machine.
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
        ///     Текущее активное состояние.
        /// </summary>
        public TState CurrentState => StateMachine.CurrentState;

        /// <summary>
        ///     Предыдущее состояние.
        /// </summary>
        public TState PreviousState => StateMachine.PreviousState;

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
            else if (initialStateType != null)
            {
                ChangeStateByType(initialStateType);
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
            StateMachine.ClearStateCache();
            StateMachine.ClearTransitionCache();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (showStateInInspector && Application.isPlaying && StateMachine != null)
            {
                // Информация о текущем состоянии будет отображаться в кастомном редакторе
            }
        }
#endif

        /// <summary>
        ///     Сменить состояние по типу.
        /// </summary>
        /// <typeparam name="T">Тип нового состояния.</typeparam>
        public void ChangeState<T>() where T : class, TState, new()
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
                // StateData реализует IState, но нужно проверить совместимость типов
                if (stateData is TState state)
                {
                    StateMachine.ChangeState(state);

                    if (enableDebugLog)
                    {
                        Debug.Log($"[StateMachineBehaviour] Changed state to {stateName}", this);
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[StateMachineBehaviour] StateData '{stateName}' is not compatible with TState type.", this);
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
            stateMachine = new StateMachine<TState>();
            SetupEvents();

            if (!string.IsNullOrEmpty(initialStateTypeName))
            {
                initialStateType = Type.GetType(initialStateTypeName);
            }
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

        private void ChangeStateByType(Type stateType)
        {
            if (stateType == null)
            {
                return;
            }

            try
            {
                MethodInfo method = typeof(StateMachine<TState>).GetMethod("ChangeState", new[] { typeof(Type) });
                if (method == null)
                {
                    // Используем рефлексию для создания экземпляра
                    TState state = Activator.CreateInstance(stateType) as TState;
                    if (state != null)
                    {
                        StateMachine.ChangeState(state);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateMachineBehaviour] Failed to change state to {stateType.Name}: {ex.Message}",
                    this);
            }
        }
    }
}