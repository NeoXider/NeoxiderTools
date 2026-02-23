using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Кастомный редактор для StateMachineData.
    ///     Предоставляет визуальное отображение состояний и переходов в инспекторе.
    /// </summary>
    [CustomEditor(typeof(StateMachineData))]
    [CanEditMultipleObjects]
    public class StateMachineDataEditor : CustomEditorBase
    {
        private StateMachineData data;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        private void OnEnable()
        {
            data = target as StateMachineData;
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State Machine Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Валидация (silent режим - не логируем в консоль при каждой отрисовке)
            bool isValid = data.Validate(true);
            if (!isValid)
            {
                EditorGUILayout.HelpBox(
                    "State Machine configuration has errors. Add states to configure the state machine.",
                    MessageType.Warning);
            }

            DrawStatesSection();

            EditorGUILayout.Space();

            DrawInitialStateSection();

            EditorGUILayout.Space();

            DrawTransitionsSection();

            EditorGUILayout.Space();

            // Кнопка валидации
            if (GUILayout.Button("Validate Configuration"))
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

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatesSection()
        {
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
            SerializedProperty statesProp = serializedObject.FindProperty("states");
            EditorGUILayout.PropertyField(statesProp, true);
        }

        private void DrawInitialStateSection()
        {
            EditorGUILayout.LabelField("Initial State", EditorStyles.boldLabel);
            SerializedProperty initialStateProp = serializedObject.FindProperty("initialState");
            EditorGUILayout.PropertyField(initialStateProp, new GUIContent("Initial State (StateData)"));

            SerializedProperty initialStateNameProp = serializedObject.FindProperty("initialStateName");
            if (initialStateProp.objectReferenceValue == null && !string.IsNullOrEmpty(initialStateNameProp.stringValue))
            {
                EditorGUILayout.HelpBox(
                    $"Using legacy initial state name: {initialStateNameProp.stringValue}. Please assign a StateData object instead.",
                    MessageType.Warning);
            }
        }

        private void DrawTransitionsSection()
        {
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
            SerializedProperty transitionsProp = serializedObject.FindProperty("transitions");
            if (transitionsProp == null)
            {
                EditorGUILayout.HelpBox("Transitions property is missing.", MessageType.Error);
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

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Transition {i + 1}", EditorStyles.boldLabel);
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
        }
    }

    /// <summary>
    ///     Кастомный редактор для StateMachineData.
    ///     Регистрация происходит автоматически через атрибут [CustomEditor] и StateMachineEditorRegistrar.
    /// </summary>
}