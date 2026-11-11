using System.Collections.Generic;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    [CustomEditor(typeof(StateMachineBehaviourBase))]
    public class StateMachineBehaviourBaseEditor : CustomEditorBase
    {
        private static readonly GUIContent ContextOverridesContent = new GUIContent("Context Overrides",
            "Scene objects for Override1..5. Element 0 is Override1; Owner uses this GameObject.");

        private static readonly GUIContent TargetStateContent = new GUIContent("Target State",
            "State the Change State button switches to.");

        private int selectedStateIndex;

        // WHY: the target-state popup used to run a LINQ chain and allocate a fresh name array on every IMGUI
        // event.
        private readonly List<string> _stateNames = new List<string>();
        private string[] _stateNameOptions = System.Array.Empty<string>();

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            DrawSettingsSection();
            EditorGUILayout.Space(6);
            DrawReferencesSection();
            EditorGUILayout.Space(6);
            DrawEventsSection();
            EditorGUILayout.Space(6);
            DrawRuntimeSection();
            EditorGUILayout.Space(6);
            DrawControlButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettingsSection()
        {
            NeoxiderEditorGUI.BeginSection("Settings", "Logging, inspector readout and lifecycle behaviour.");
            DrawField("enableDebugLog");
            DrawField("showStateInInspector");
            DrawField("autoEvaluateTransitions");
            DrawField("exitCurrentStateOnDisable");
            DrawField("reloadDataOnEnable");
            NeoxiderEditorGUI.EndSection();
        }

        private void DrawReferencesSection()
        {
            NeoxiderEditorGUI.BeginSection("References", "The StateMachineData asset and scene context objects.");
            DrawField("stateMachineData");
            SerializedProperty overrides = serializedObject.FindProperty("contextOverrides");
            if (overrides != null)
            {
                EditorGUILayout.PropertyField(overrides, ContextOverridesContent, true);
            }

            NeoxiderEditorGUI.EndSection();
        }

        private void DrawEventsSection()
        {
            NeoxiderEditorGUI.BeginSection("Events", "Raised on initialization, state changes and evaluations.");
            DrawField("onInitialized");
            DrawField("onStateEntered");
            DrawField("onStateExited");
            DrawField("onStateChanged");
            DrawField("onTransitionEvaluated");
            NeoxiderEditorGUI.EndSection();
        }

        private void DrawRuntimeSection()
        {
            NeoxiderEditorGUI.BeginSection("Runtime", Application.isPlaying
                ? "Live values, refreshed while the inspector is visible."
                : "Filled in while the game runs.");
            using (new EditorGUI.DisabledScope(true))
            {
                DrawField("currentStateName");
                DrawField("previousStateName");
                DrawField("stateChangeCount");
                DrawField("stateEnterTime");
            }

            NeoxiderEditorGUI.EndSection();

            if (Application.isPlaying)
            {
                // WHY: the runtime fields change while nothing in the editor does; a capped live lease keeps
                // them current and lapses on its own once the inspector is hidden.
                EnsureLiveRepaint();
            }
        }

        private void DrawField(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        private void DrawControlButtons()
        {
            StateMachineBehaviourBase behaviour = (StateMachineBehaviourBase)target;
            SerializedProperty dataProp = serializedObject.FindProperty("stateMachineData");
            StateMachineData data = dataProp?.objectReferenceValue as StateMachineData;

            NeoxiderEditorGUI.BeginSection("Controls", "Drive the machine by hand while the game runs.");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime controls are available in Play Mode.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload Data"))
                {
                    behaviour.ReloadFromStateMachineData();
                }

                if (GUILayout.Button("Evaluate Now"))
                {
                    behaviour.EvaluateTransitionsNow();
                }
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Go To Initial State"))
                {
                    behaviour.GoToInitialState();
                }
            }

            string[] stateNames = GetStateNames(data);
            if (stateNames.Length == 0)
            {
                if (data == null)
                {
                    NeoxiderEditorGUI.DrawCaption("Assign State Machine Data to pick a target state here.");
                }

                NeoxiderEditorGUI.EndSection();
                return;
            }

            selectedStateIndex = Mathf.Clamp(selectedStateIndex, 0, stateNames.Length - 1);
            selectedStateIndex = EditorGUILayout.Popup(TargetStateContent, selectedStateIndex, stateNames);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Change State"))
                {
                    behaviour.ChangeState(stateNames[selectedStateIndex]);
                }
            }

            NeoxiderEditorGUI.EndSection();
        }

        private string[] GetStateNames(StateMachineData data)
        {
            if (data == null)
            {
                _stateNameOptions = System.Array.Empty<string>();
                return _stateNameOptions;
            }

            // WHY: rebuilt into a reused list every draw (a state asset can be renamed without touching the
            // machine asset), but a new popup array is only allocated when the names actually differ.
            _stateNames.Clear();
            StateData[] states = data.States;
            if (states != null)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    StateData state = states[i];
                    if (state != null && !string.IsNullOrEmpty(state.StateName))
                    {
                        _stateNames.Add(state.StateName);
                    }
                }
            }

            if (!SameNames(_stateNames, _stateNameOptions))
            {
                _stateNameOptions = _stateNames.ToArray();
            }

            return _stateNameOptions;
        }

        private static bool SameNames(List<string> names, string[] options)
        {
            if (names.Count != options.Length)
            {
                return false;
            }

            for (int i = 0; i < options.Length; i++)
            {
                if (!string.Equals(names[i], options[i], System.StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
