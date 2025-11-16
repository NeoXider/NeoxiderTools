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
    [CreateAssetMenu(fileName = "New State", menuName = "Neo/State Machine/State Data")]
    public class StateData : ScriptableObject, IState
    {
        [SerializeField]
        [Tooltip("Имя состояния для идентификации")]
        private string stateName = "New State";

        [SerializeField]
        [Tooltip("Действия, выполняемые при входе в состояние")]
        private List<StateAction> onEnterActions = new List<StateAction>();

        [SerializeField]
        [Tooltip("Действия, выполняемые каждый кадр в состоянии")]
        private List<StateAction> onUpdateActions = new List<StateAction>();

        [SerializeField]
        [Tooltip("Действия, выполняемые при выходе из состояния")]
        private List<StateAction> onExitActions = new List<StateAction>();

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
        public List<StateAction> OnEnterActions => onEnterActions;

        /// <summary>
        ///     Действия при обновлении состояния.
        /// </summary>
        public List<StateAction> OnUpdateActions => onUpdateActions;

        /// <summary>
        ///     Действия при выходе из состояния.
        /// </summary>
        public List<StateAction> OnExitActions => onExitActions;

        /// <summary>
        ///     Вызывается при входе в состояние.
        /// </summary>
        public void OnEnter()
        {
            ExecuteActions(onEnterActions);
        }

        /// <summary>
        ///     Вызывается каждый кадр в состоянии.
        /// </summary>
        public void OnUpdate()
        {
            ExecuteActions(onUpdateActions);
        }

        /// <summary>
        ///     Вызывается при выходе из состояния.
        /// </summary>
        public void OnExit()
        {
            ExecuteActions(onExitActions);
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

            foreach (var action in actions)
            {
                if (action != null)
                {
                    try
                    {
                        action.Execute();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[StateData] Error executing action in state '{stateName}': {ex.Message}", this);
                    }
                }
            }
        }
    }
}

