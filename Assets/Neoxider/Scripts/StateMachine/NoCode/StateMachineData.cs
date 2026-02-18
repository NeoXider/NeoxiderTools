using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.StateMachine.NoCode
{
    /// <summary>
    ///     ScriptableObject для конфигурации State Machine без кода.
    ///     Позволяет создавать полную конфигурацию State Machine визуально в инспекторе.
    /// </summary>
    /// <remarks>
    ///     StateMachineData содержит все состояния и переходы, которые можно настроить в инспекторе.
    ///     Может быть загружен в StateMachineBehaviour для использования.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Создать StateMachineData через меню: Create > Neo > State Machine > State Machine Data
    /// // Настроить состояния и переходы в инспекторе
    /// // Присвоить в StateMachineBehaviour.stateMachineData
    /// // StateMachineBehaviour автоматически загрузит конфигурацию в Start()
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "New State Machine", menuName = "Neoxider/State Machine/State Machine Data")]
    public class StateMachineData : ScriptableObject
    {
        [SerializeField] [Tooltip("All State Machine states")]
        private StateData[] states = new StateData[0];

        [SerializeField] [Tooltip("Initial state (ScriptableObject)")]
        private StateData initialState;

        [SerializeField] [Tooltip("Initial state name (legacy, used when initialState not set)")]
        private string initialStateName = "";

        [SerializeField] [Tooltip("Global transitions between states")]
        private List<StateTransition> transitions = new();

        [SerializeField] [Tooltip("Node positions (legacy, unused)")]
        private List<StatePosition> statePositions = new();

        /// <summary>
        ///     Все состояния State Machine.
        /// </summary>
        public StateData[] States
        {
            get => states;
            set => states = value;
        }

        /// <summary>
        ///     Начальное состояние (ScriptableObject).
        /// </summary>
        public StateData InitialState
        {
            get => initialState;
            set => initialState = value;
        }

        /// <summary>
        ///     Имя начального состояния (для обратной совместимости).
        /// </summary>
        public string InitialStateName
        {
            get => initialState != null ? initialState.StateName : initialStateName;
            set
            {
                initialStateName = value;
                if (initialState == null && !string.IsNullOrEmpty(value))
                {
                    initialState = GetStateByName(value);
                }
            }
        }

        /// <summary>
        ///     Глобальные переходы между состояниями.
        /// </summary>
        public List<StateTransition> Transitions => transitions;

        private void OnValidate()
        {
            // Автоматическая валидация в редакторе
            if (Application.isPlaying)
            {
                Validate();
            }
        }

        /// <summary>
        ///     Получить позицию состояния (legacy метод, не используется).
        /// </summary>
        /// <param name="stateName">Имя состояния.</param>
        /// <returns>Позиция состояния или Vector2.zero, если не найдено.</returns>
        public Vector2 GetStatePosition(string stateName)
        {
            if (string.IsNullOrEmpty(stateName) || statePositions == null)
            {
                return Vector2.zero;
            }

            StatePosition pos = statePositions.FirstOrDefault(p => p != null && p.stateName == stateName);
            return pos != null ? pos.position : Vector2.zero;
        }

        /// <summary>
        ///     Установить позицию состояния (legacy метод, не используется).
        /// </summary>
        /// <param name="stateName">Имя состояния.</param>
        /// <param name="position">Позиция.</param>
        public void SetStatePosition(string stateName, Vector2 position)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return;
            }

            if (statePositions == null)
            {
                statePositions = new List<StatePosition>();
            }

            StatePosition existing = statePositions.FirstOrDefault(p => p != null && p.stateName == stateName);
            if (existing != null)
            {
                existing.position = position;
            }
            else
            {
                statePositions.Add(new StatePosition(stateName, position));
            }
        }

        /// <summary>
        ///     Очистить все сохраненные позиции.
        /// </summary>
        public void ClearStatePositions()
        {
            if (statePositions != null)
            {
                statePositions.Clear();
            }
        }

        /// <summary>
        ///     Загрузить конфигурацию в State Machine.
        /// </summary>
        /// <typeparam name="TState">Тип состояний.</typeparam>
        /// <param name="stateMachine">State Machine для загрузки конфигурации.</param>
        public void LoadIntoStateMachine<TState>(StateMachine<TState> stateMachine) where TState : class, IState
        {
            if (stateMachine == null)
            {
                Debug.LogError("[StateMachineData] Cannot load into null StateMachine.", this);
                return;
            }

            // Регистрация переходов
            foreach (StateTransition transition in transitions)
            {
                if (transition != null)
                {
                    // Для NoCode переходов нужно установить типы на основе имен состояний
                    SetupNoCodeTransition(transition);
                    stateMachine.RegisterTransition(transition);
                }
            }

            // Регистрация переходов из состояний
            foreach (StateData state in states)
            {
                if (state != null && state is StateData stateData)
                {
                    // Если у состояния есть свои переходы, они должны быть в transitions
                    // Здесь можно добавить дополнительную логику для переходов из конкретных состояний
                }
            }
        }

        /// <summary>
        ///     Получить состояние по имени.
        /// </summary>
        /// <param name="stateName">Имя состояния.</param>
        /// <returns>StateData или null, если не найдено.</returns>
        public StateData GetStateByName(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return null;
            }

            return states.FirstOrDefault(s => s != null && s.StateName == stateName);
        }

        /// <summary>
        ///     Проверить валидность конфигурации.
        /// </summary>
        /// <param name="silent">Если true, не выводит предупреждения в консоль (только возвращает результат).</param>
        /// <returns>True, если конфигурация валидна.</returns>
        public bool Validate(bool silent = false)
        {
            if (states == null || states.Length == 0)
            {
                if (!silent)
                {
                    Debug.LogWarning("[StateMachineData] No states defined.", this);
                }

                return false;
            }

            if (initialState == null && string.IsNullOrEmpty(initialStateName))
            {
                if (!silent)
                {
                    Debug.LogWarning("[StateMachineData] Initial state is not set.", this);
                }
            }
            else if (initialState != null && !states.Contains(initialState))
            {
                if (!silent)
                {
                    Debug.LogWarning(
                        $"[StateMachineData] Initial state '{initialState.StateName}' not found in states array.",
                        this);
                }

                return false;
            }
            else if (initialState == null && GetStateByName(initialStateName) == null)
            {
                if (!silent)
                {
                    Debug.LogWarning($"[StateMachineData] Initial state '{initialStateName}' not found in states.",
                        this);
                }

                return false;
            }

            // Проверка переходов
            foreach (StateTransition transition in transitions)
            {
                if (transition == null)
                {
                    continue;
                }

                if (transition.FromStateData != null)
                {
                    if (!States.Contains(transition.FromStateData))
                    {
                        if (!silent)
                        {
                            Debug.LogWarning(
                                $"[StateMachineData] Transition from state '{transition.FromStateData.StateName}' not found in States array.",
                                this);
                        }
                    }
                }
                else
                {
                    if (!silent)
                    {
                        Debug.LogWarning("[StateMachineData] Transition has null FromStateData.", this);
                    }
                }

                if (transition.ToStateData != null)
                {
                    if (!States.Contains(transition.ToStateData))
                    {
                        if (!silent)
                        {
                            Debug.LogWarning(
                                $"[StateMachineData] Transition to state '{transition.ToStateData.StateName}' not found in States array.",
                                this);
                        }
                    }
                }
                else
                {
                    if (!silent)
                    {
                        Debug.LogWarning("[StateMachineData] Transition has null ToStateData.", this);
                    }
                }
            }

            return true;
        }

        private void SetupNoCodeTransition(StateTransition transition)
        {
            // Для NoCode переходов можно установить типы на основе StateData
            // Но так как StateData - это ScriptableObject, а не класс состояния,
            // мы используем имена состояний для идентификации
            // StateMachineBehaviour будет обрабатывать переходы по именам
        }

        [Serializable]
        private class StatePosition
        {
            public string stateName;
            public Vector2 position;

            public StatePosition(string name, Vector2 pos)
            {
                stateName = name;
                position = pos;
            }
        }
    }
}