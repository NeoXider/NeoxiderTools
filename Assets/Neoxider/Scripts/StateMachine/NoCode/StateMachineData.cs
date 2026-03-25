using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.StateMachine.NoCode
{
    /// <summary>
    ///     ScriptableObject State Machine configuration: states and transitions by name.
    ///     Lets you author a full State Machine visually in the Inspector.
    /// </summary>
    /// <remarks>
    ///     StateMachineData holds all states and transitions editable in the Inspector.
    ///     It can be loaded into StateMachineBehaviour at runtime.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Create StateMachineData via menu: Create > Neo > State Machine > State Machine Data
    /// // Configure states and transitions in the Inspector
    /// // Assign to StateMachineBehaviour.stateMachineData
    /// // StateMachineBehaviour loads the configuration in Start()
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
        ///     All states in this State Machine.
        /// </summary>
        public StateData[] States
        {
            get => states;
            set => states = value;
        }

        /// <summary>
        ///     Initial state (ScriptableObject reference).
        /// </summary>
        public StateData InitialState
        {
            get => initialState;
            set => initialState = value;
        }

        /// <summary>
        ///     Initial state name (legacy compatibility when only a string was used).
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
        ///     Global transitions between states.
        /// </summary>
        public List<StateTransition> Transitions => transitions;

        private void OnValidate()
        {
            // Automatic validation in the editor
            if (Application.isPlaying)
            {
                Validate();
            }
        }

        /// <summary>
        ///     Gets a state's saved node position (legacy; unused).
        /// </summary>
        /// <param name="stateName">State name.</param>
        /// <returns>Stored position or Vector2.zero if missing.</returns>
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
        ///     Sets a state's node position (legacy; unused).
        /// </summary>
        /// <param name="stateName">State name.</param>
        /// <param name="position">Position.</param>
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
        ///     Clears all stored node positions.
        /// </summary>
        public void ClearStatePositions()
        {
            if (statePositions != null)
            {
                statePositions.Clear();
            }
        }

        /// <summary>
        ///     Loads this configuration into a State Machine (registers transitions).
        /// </summary>
        /// <typeparam name="TState">State type.</typeparam>
        /// <param name="stateMachine">Target State Machine.</param>
        public void LoadIntoStateMachine<TState>(StateMachine<TState> stateMachine) where TState : class, IState
        {
            if (stateMachine == null)
            {
                Debug.LogError("[StateMachineData] Cannot load into null StateMachine.", this);
                return;
            }

            // Register transitions
            foreach (StateTransition transition in transitions)
            {
                if (transition != null)
                {
                    // NoCode transitions: resolve types from state names where needed
                    SetupNoCodeTransition(transition);
                    stateMachine.RegisterTransition(transition);
                }
            }

            // Per-state transitions (if added later)
            foreach (StateData state in states)
            {
                if (state != null && state is StateData stateData)
                {
                    // If a state owns its own transitions, they should live in transitions
                    // Hook for future per-source-state registration
                }
            }
        }

        /// <summary>
        ///     Finds a state asset by name.
        /// </summary>
        /// <param name="stateName">State name.</param>
        /// <returns>StateData or null.</returns>
        public StateData GetStateByName(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return null;
            }

            return states.FirstOrDefault(s => s != null && s.StateName == stateName);
        }

        /// <summary>
        ///     Validates the asset configuration.
        /// </summary>
        /// <param name="silent">If true, skips console warnings and only returns the result.</param>
        /// <returns>True if the configuration is valid.</returns>
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

            // Transition checks
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
            // NoCode: types could be derived from StateData assets, but StateData is a ScriptableObject,
            // not a runtime state class — identification is by state name.
            // StateMachineBehaviour resolves transitions by name.
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
