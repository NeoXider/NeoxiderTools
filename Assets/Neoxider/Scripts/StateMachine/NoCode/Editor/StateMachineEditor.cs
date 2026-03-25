using System.Collections.Generic;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Custom inspector for StateMachineData.
    ///     Visual layout for states and transitions.
    /// </summary>
    [CustomEditor(typeof(StateMachineData))]
    [CanEditMultipleObjects]
    public class StateMachineDataEditor : CustomEditorBase
    {
        private StateMachineData data;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        private void OnEnable()
        {
            data = target as StateMachineData;
        }

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            bool isValid = data.Validate(true);
            DrawSummary(isValid);

            DrawStatesSection();

            EditorGUILayout.Space(6f);

            DrawInitialStateSection();

            EditorGUILayout.Space(6f);

            DrawTransitionsSection();

            EditorGUILayout.Space(6f);

            NeoxiderEditorGUI.BeginSection("Validation", "Run a manual validation pass and show the result.");
            if (GUILayout.Button("Validate Configuration", GUILayout.Height(24f)))
            {
                bool validationResult = data.Validate();
                if (validationResult)
                {
                    EditorUtility.DisplayDialog("Validation", "State Machine configuration is valid!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation",
                        "State Machine configuration has errors. Check the console.", "OK");
                }
            }

            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary(bool isValid)
        {
            SerializedProperty statesProp = serializedObject.FindProperty("states");
            SerializedProperty transitionsProp = serializedObject.FindProperty("transitions");
            SerializedProperty initialStateProp = serializedObject.FindProperty("initialState");
            SerializedProperty legacyInitialStateNameProp = serializedObject.FindProperty("initialStateName");

            string title = data != null ? data.name : "State Machine";
            string subtitle = isValid
                ? "Configuration looks valid. Edit states, initial state, and transitions below."
                : "There are issues in this configuration. The inspector surfaces them before play mode.";

            List<NeoxiderEditorGUI.Badge> badges = new()
            {
                new NeoxiderEditorGUI.Badge($"States {statesProp.arraySize}", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge($"Transitions {transitionsProp.arraySize}",
                    new Color(0.42f, 0.34f, 0.82f, 1f)),
                new NeoxiderEditorGUI.Badge(
                    initialStateProp.objectReferenceValue != null ? "Initial Set" : "Initial Missing",
                    initialStateProp.objectReferenceValue != null
                        ? new Color(0.18f, 0.62f, 0.32f, 1f)
                        : new Color(0.78f, 0.46f, 0.18f, 1f))
            };

            if (!string.IsNullOrEmpty(legacyInitialStateNameProp.stringValue) &&
                initialStateProp.objectReferenceValue == null)
            {
                badges.Add(new NeoxiderEditorGUI.Badge("Legacy Initial Name", new Color(0.65f, 0.56f, 0.18f, 1f)));
            }

            NeoxiderEditorGUI.DrawSummaryCard(title, subtitle, badges.ToArray());

            if (!isValid)
            {
                EditorGUILayout.HelpBox(
                    "State Machine configuration has errors or missing links. Check states, initial state, and transitions.",
                    MessageType.Warning);
            }

            if (statesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("States array is empty. The state machine cannot start without states.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawStatesSection()
        {
            SerializedProperty statesProp = serializedObject.FindProperty("states");
            NeoxiderEditorGUI.BeginSection("States", "All states available to this machine.");
            EditorGUILayout.PropertyField(statesProp, true);
            NeoxiderEditorGUI.EndSection();
        }

        private void DrawInitialStateSection()
        {
            SerializedProperty initialStateProp = serializedObject.FindProperty("initialState");
            SerializedProperty initialStateNameProp = serializedObject.FindProperty("initialStateName");

            NeoxiderEditorGUI.BeginSection("Initial State",
                "Prefer assigning a StateData reference; keep the legacy name field only for backward compatibility.");
            EditorGUILayout.PropertyField(initialStateProp, new GUIContent("Initial State (StateData)"));

            if (initialStateProp.objectReferenceValue == null &&
                !string.IsNullOrEmpty(initialStateNameProp.stringValue))
            {
                EditorGUILayout.HelpBox(
                    $"Using legacy initial state name: {initialStateNameProp.stringValue}. Please assign a StateData object instead.",
                    MessageType.Warning);
            }

            NeoxiderEditorGUI.EndSection();
        }

        private void DrawTransitionsSection()
        {
            SerializedProperty transitionsProp = serializedObject.FindProperty("transitions");
            NeoxiderEditorGUI.BeginSection("Transitions",
                "Transitions between states. Use clear names and valid references to state assets.");
            if (transitionsProp == null)
            {
                EditorGUILayout.HelpBox("Transitions property is missing.", MessageType.Error);
                NeoxiderEditorGUI.EndSection();
                return;
            }

            int removeIndex = -1;
            int moveUpIndex = -1;
            int moveDownIndex = -1;

            for (int i = 0; i < transitionsProp.arraySize; i++)
            {
                SerializedProperty transitionProp = transitionsProp.GetArrayElementAtIndex(i);
                SerializedProperty fromProp = transitionProp.FindPropertyRelative("fromStateData");
                SerializedProperty toProp = transitionProp.FindPropertyRelative("toStateData");
                SerializedProperty priorityProp = transitionProp.FindPropertyRelative("priority");
                SerializedProperty enabledProp = transitionProp.FindPropertyRelative("isEnabled");
                SerializedProperty nameProp = transitionProp.FindPropertyRelative("transitionName");
                SerializedProperty predicatesProp = transitionProp.FindPropertyRelative("predicates");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                string transitionTitle = nameProp != null && !string.IsNullOrWhiteSpace(nameProp.stringValue)
                    ? nameProp.stringValue
                    : $"Transition {i + 1}";
                EditorGUILayout.LabelField(transitionTitle, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Predicates: {predicatesProp?.arraySize ?? 0}", EditorStyles.miniBoldLabel,
                    GUILayout.Width(90f));
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();

                if (nameProp != null)
                {
                    EditorGUILayout.PropertyField(nameProp, new GUIContent("Name"));
                }

                if (fromProp != null)
                {
                    EditorGUILayout.PropertyField(fromProp, new GUIContent("From State"));
                }

                if (toProp != null)
                {
                    EditorGUILayout.PropertyField(toProp, new GUIContent("To State"));
                }

                if (priorityProp != null)
                {
                    EditorGUILayout.PropertyField(priorityProp, new GUIContent("Priority"));
                }

                if (enabledProp != null)
                {
                    EditorGUILayout.PropertyField(enabledProp, new GUIContent("Is Enabled"));
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Edit Conditions"))
                {
                    serializedObject.ApplyModifiedProperties();
                    if (i < data.Transitions.Count && data.Transitions[i] != null)
                    {
                        TransitionEditorWindow.ShowWindow(data.Transitions[i], data);
                    }

                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Up", GUILayout.Width(48)))
                {
                    moveUpIndex = i;
                }

                if (GUILayout.Button("Down", GUILayout.Width(48)))
                {
                    moveDownIndex = i;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (removeIndex >= 0)
            {
                transitionsProp.DeleteArrayElementAtIndex(removeIndex);
            }
            else if (moveUpIndex > 0)
            {
                transitionsProp.MoveArrayElement(moveUpIndex, moveUpIndex - 1);
            }
            else if (moveDownIndex >= 0 && moveDownIndex < transitionsProp.arraySize - 1)
            {
                transitionsProp.MoveArrayElement(moveDownIndex, moveDownIndex + 1);
            }

            if (GUILayout.Button("Add Transition", GUILayout.Height(24)))
            {
                int index = transitionsProp.arraySize;
                transitionsProp.InsertArrayElementAtIndex(index);
                SerializedProperty newTransition = transitionsProp.GetArrayElementAtIndex(index);
                SerializedProperty fromProp = newTransition.FindPropertyRelative("fromStateData");
                SerializedProperty toProp = newTransition.FindPropertyRelative("toStateData");
                SerializedProperty enabledProp = newTransition.FindPropertyRelative("isEnabled");
                SerializedProperty nameProp = newTransition.FindPropertyRelative("transitionName");
                SerializedProperty priorityProp = newTransition.FindPropertyRelative("priority");
                SerializedProperty predicatesProp = newTransition.FindPropertyRelative("predicates");
                if (fromProp != null)
                {
                    fromProp.objectReferenceValue = null;
                }

                if (toProp != null)
                {
                    toProp.objectReferenceValue = null;
                }

                if (enabledProp != null)
                {
                    enabledProp.boolValue = true;
                }

                if (priorityProp != null)
                {
                    priorityProp.intValue = 0;
                }

                if (nameProp != null)
                {
                    nameProp.stringValue = "New Transition";
                }

                if (predicatesProp != null)
                {
                    predicatesProp.ClearArray();
                }
            }

            NeoxiderEditorGUI.EndSection();
        }
    }
}
