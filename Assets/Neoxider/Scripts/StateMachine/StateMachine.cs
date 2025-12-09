using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Основной класс State Machine с поддержкой кэширования состояний и переходов.
    ///     Управляет жизненным циклом состояний и переходами между ними.
    /// </summary>
    /// <typeparam name="TState">Тип состояний, должен реализовывать IState.</typeparam>
    /// <remarks>
    ///     State Machine автоматически кэширует экземпляры состояний и переходы для оптимизации производительности.
    ///     Кэширование включено по умолчанию, но может быть отключено через конструктор.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var stateMachine = new StateMachine&lt;IState&gt;();
    /// 
    /// // Регистрация переходов
    /// var transition = new StateTransition
    /// {
    ///     FromStateType = typeof(IdleState),
    ///     ToStateType = typeof(RunningState)
    /// };
    /// stateMachine.RegisterTransition(transition);
    /// 
    /// // Смена состояния
    /// stateMachine.ChangeState&lt;IdleState&gt;();
    /// 
    /// // Обновление
    /// stateMachine.Update();
    /// stateMachine.EvaluateTransitions(); // Автоматическая оценка переходов
    /// </code>
    /// </example>
    public class StateMachine<TState> where TState : class, IState
    {
        private readonly bool enableStateCaching;
        private readonly bool enableTransitionCaching;
        private readonly List<StateTransition> globalTransitions = new();
        private readonly Dictionary<Type, TState> stateCache = new();
        private readonly Dictionary<Type, List<StateTransition>> transitionCache = new();

        /// <summary>
        ///     Создать новый экземпляр State Machine.
        /// </summary>
        /// <param name="enableStateCaching">Включить кэширование состояний (по умолчанию true).</param>
        /// <param name="enableTransitionCaching">Включить кэширование переходов (по умолчанию true).</param>
        public StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)
        {
            this.enableStateCaching = enableStateCaching;
            this.enableTransitionCaching = enableTransitionCaching;
        }

        /// <summary>
        ///     Событие смены состояния. Вызывается при переходе из одного состояния в другое.
        /// </summary>
        public UnityEvent<TState, TState> OnStateChanged { get; } = new();

        /// <summary>
        ///     Событие входа в состояние. Вызывается при входе в новое состояние.
        /// </summary>
        public UnityEvent<TState> OnStateEntered { get; } = new();

        /// <summary>
        ///     Событие выхода из состояния. Вызывается при выходе из состояния.
        /// </summary>
        public UnityEvent<TState> OnStateExited { get; } = new();

        /// <summary>
        ///     Событие оценки перехода. Вызывается при оценке каждого перехода.
        /// </summary>
        public UnityEvent<StateTransition, bool> OnTransitionEvaluated { get; } = new();

        /// <summary>
        ///     Текущее активное состояние.
        /// </summary>
        public TState CurrentState { get; private set; }

        /// <summary>
        ///     Предыдущее состояние.
        /// </summary>
        public TState PreviousState { get; private set; }

        /// <summary>
        ///     Получить или создать экземпляр состояния с кэшированием.
        /// </summary>
        /// <typeparam name="T">Тип состояния.</typeparam>
        /// <returns>Экземпляр состояния.</returns>
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
        ///     Сменить состояние по типу.
        /// </summary>
        /// <typeparam name="T">Тип нового состояния.</typeparam>
        public void ChangeState<T>() where T : class, TState, new()
        {
            T newState = GetOrCreateState<T>();
            ChangeState(newState);
        }

        /// <summary>
        ///     Сменить состояние по экземпляру.
        /// </summary>
        /// <param name="newState">Новое состояние.</param>
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
        ///     Попытаться сменить состояние по типу с проверкой условий.
        /// </summary>
        /// <typeparam name="T">Тип нового состояния.</typeparam>
        /// <returns>True, если состояние было изменено.</returns>
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
        ///     Проверить возможность перехода к указанному типу состояния.
        /// </summary>
        /// <typeparam name="T">Тип состояния для проверки.</typeparam>
        /// <returns>True, если переход возможен.</returns>
        public bool CanTransitionTo<T>() where T : class, TState
        {
            if (CurrentState == null)
            {
                return true;
            }

            Type targetType = typeof(T);
            List<StateTransition> transitions = GetAvailableTransitions(CurrentState.GetType());

            return transitions.Any(t => t.ToStateType == targetType && t.EvaluatePredicates(CurrentState));
        }

        /// <summary>
        ///     Зарегистрировать переход в State Machine.
        /// </summary>
        /// <param name="transition">Переход для регистрации.</param>
        public void RegisterTransition(StateTransition transition)
        {
            if (transition == null)
            {
                Debug.LogWarning("[StateMachine] Attempted to register null transition.");
                return;
            }

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
            else
            {
                globalTransitions.Add(transition);
            }
        }

        /// <summary>
        ///     Удалить переход из State Machine.
        /// </summary>
        /// <param name="transition">Переход для удаления.</param>
        public void UnregisterTransition(StateTransition transition)
        {
            if (transition == null)
            {
                return;
            }

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
            else
            {
                globalTransitions.Remove(transition);
            }
        }

        /// <summary>
        ///     Получить доступные переходы из указанного типа состояния.
        /// </summary>
        /// <param name="fromStateType">Тип исходного состояния.</param>
        /// <returns>Список доступных переходов.</returns>
        public List<StateTransition> GetAvailableTransitions(Type fromStateType)
        {
            List<StateTransition> availableTransitions = new();

            if (enableTransitionCaching && transitionCache.TryGetValue(fromStateType, out List<StateTransition> cached))
            {
                availableTransitions.AddRange(cached);
            }

            availableTransitions.AddRange(globalTransitions);

            return availableTransitions.OrderByDescending(t => t.Priority).ToList();
        }

        /// <summary>
        ///     Оценить все доступные переходы и выполнить переход, если условия выполнены.
        /// </summary>
        public void EvaluateTransitions()
        {
            if (CurrentState == null)
            {
                return;
            }

            Type currentType = CurrentState.GetType();
            List<StateTransition> transitions = GetAvailableTransitions(currentType);

            foreach (StateTransition transition in transitions)
            {
                if (!transition.IsEnabled)
                {
                    continue;
                }

                bool canTransition = transition.CanTransition(CurrentState);
                OnTransitionEvaluated?.Invoke(transition, canTransition);

                if (canTransition && transition.ToStateType != null)
                {
                    ChangeStateByType(transition.ToStateType);
                    break; // Выполняем только первый подходящий переход
                }
            }
        }

        /// <summary>
        ///     Обновить текущее состояние.
        /// </summary>
        public void Update()
        {
            CurrentState?.OnUpdate();
        }

        /// <summary>
        ///     Обновить текущее состояние (физика).
        /// </summary>
        public void FixedUpdate()
        {
            CurrentState?.OnFixedUpdate();
        }

        /// <summary>
        ///     Обновить текущее состояние (поздние обновления).
        /// </summary>
        public void LateUpdate()
        {
            CurrentState?.OnLateUpdate();
        }

        /// <summary>
        ///     Очистить кэш состояний.
        /// </summary>
        public void ClearStateCache()
        {
            stateCache.Clear();
        }

        /// <summary>
        ///     Очистить кэш переходов.
        /// </summary>
        public void ClearTransitionCache()
        {
            transitionCache.Clear();
            globalTransitions.Clear();
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
                TState newState = Activator.CreateInstance(stateType) as TState;
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