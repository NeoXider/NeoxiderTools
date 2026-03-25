using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Neo.StateMachine.NoCode
{
    /// <summary>
    ///     ScriptableObject state: name, actions, and hooks for use inside StateMachineData.
    ///     Author states in the Inspector without writing code.
    /// </summary>
    /// <remarks>
    ///     StateData implements IState and can be used directly with StateMachine.
    ///     Supports enter, update, and exit action lists.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Create StateData via menu: Create > Neo > State Machine > State Data
    /// // Configure actions in the Inspector
    /// // Reference from StateMachineData
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "New State", menuName = "Neoxider/State Machine/State Data")]
    public class StateData : ScriptableObject, IState
    {
        [SerializeField] [Tooltip("State name for identification")]
        private string stateName = "New State";

        [FormerlySerializedAs("<OnEnterActions>k__BackingField")]
        [SerializeReference]
        [SerializeField]
        [Tooltip("Actions on state enter")]
        private List<StateAction> onEnterActions = new();

        [FormerlySerializedAs("<OnUpdateActions>k__BackingField")]
        [SerializeReference]
        [SerializeField]
        [Tooltip("Actions every frame while in state")]
        private List<StateAction> onUpdateActions = new();

        [FormerlySerializedAs("<OnExitActions>k__BackingField")]
        [SerializeReference]
        [SerializeField]
        [Tooltip("Actions on state exit")]
        private List<StateAction> onExitActions = new();

        /// <summary>
        ///     Display / lookup name of this state.
        /// </summary>
        public string StateName
        {
            get => stateName;
            set => stateName = value;
        }

        /// <summary>
        ///     Actions run once when entering the state.
        /// </summary>
        public List<StateAction> OnEnterActions => onEnterActions;

        /// <summary>
        ///     Actions run every frame while the state is active.
        /// </summary>
        public List<StateAction> OnUpdateActions => onUpdateActions;

        /// <summary>
        ///     Actions run when leaving the state.
        /// </summary>
        public List<StateAction> OnExitActions => onExitActions;

        /// <summary>
        ///     Called when entering the state.
        /// </summary>
        public void OnEnter()
        {
            ExecuteActions(OnEnterActions);
        }

        /// <summary>
        ///     Called every frame while in the state.
        /// </summary>
        public void OnUpdate()
        {
            ExecuteActions(OnUpdateActions);
        }

        /// <summary>
        ///     Called when exiting the state.
        /// </summary>
        public void OnExit()
        {
            ExecuteActions(OnExitActions);
        }

        /// <summary>
        ///     Called every physics tick (optional; not used by default for NoCode).
        /// </summary>
        public void OnFixedUpdate()
        {
            // NoCode states do not run FixedUpdate actions by default
        }

        /// <summary>
        ///     Called every frame after Update (optional; not used by default for NoCode).
        /// </summary>
        public void OnLateUpdate()
        {
            // NoCode states do not run LateUpdate actions by default
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
