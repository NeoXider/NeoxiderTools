using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.StateMachine.NoCode
{
    /// <summary>
    ///     ScriptableObject для создания NoCode состояний.
    ///     Позволяет создавать состояния визуально в инспекторе без написания кода.
    /// </summary>
    /// <remarks>
    ///     StateData реализует IState и может использоваться напрямую в StateMachine.
    ///     Поддерживает действия при входе, обновлении и выходе из состояния.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Создать StateData через меню: Create > Neo > State Machine > State Data
    /// // Настроить действия в инспекторе
    /// // Использовать в StateMachineData
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "New State", menuName = "Neoxider/State Machine/State Data")]
    public class StateData : ScriptableObject, IState
    {
        [SerializeField] [Tooltip("State name for identification")]
        private string stateName = "New State";

        /// <summary>
        ///     Имя состояния.
        /// </summary>
        public string StateName
        {
            get => stateName;
            set => stateName = value;
        }

        /// <summary>
        ///     Действия при входе в состояние.
        /// </summary>
        [field: Tooltip("Actions on state enter")]
        public List<StateAction> OnEnterActions { get; } = new();

        /// <summary>
        ///     Действия при обновлении состояния.
        /// </summary>
        [field: Tooltip("Actions every frame while in state")]
        public List<StateAction> OnUpdateActions { get; } = new();

        /// <summary>
        ///     Действия при выходе из состояния.
        /// </summary>
        [field: Tooltip("Actions on state exit")]
        public List<StateAction> OnExitActions { get; } = new();

        /// <summary>
        ///     Вызывается при входе в состояние.
        /// </summary>
        public void OnEnter()
        {
            ExecuteActions(OnEnterActions);
        }

        /// <summary>
        ///     Вызывается каждый кадр в состоянии.
        /// </summary>
        public void OnUpdate()
        {
            ExecuteActions(OnUpdateActions);
        }

        /// <summary>
        ///     Вызывается при выходе из состояния.
        /// </summary>
        public void OnExit()
        {
            ExecuteActions(OnExitActions);
        }

        /// <summary>
        ///     Вызывается каждый фиксированный кадр (опционально).
        /// </summary>
        public void OnFixedUpdate()
        {
            // NoCode состояния не поддерживают FixedUpdate по умолчанию
        }

        /// <summary>
        ///     Вызывается каждый кадр после всех обновлений (опционально).
        /// </summary>
        public void OnLateUpdate()
        {
            // NoCode состояния не поддерживают LateUpdate по умолчанию
        }

        private void ExecuteActions(List<StateAction> actions)
        {
            if (actions == null)
            {
                return;
            }

            foreach (StateAction action in actions)
            {
                if (action != null)
                {
                    try
                    {
                        action.Execute();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[StateData] Error executing action in state '{stateName}': {ex.Message}",
                            this);
                    }
                }
            }
        }
    }
}