using System;
using UnityEngine;
using UnityEditor;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Кастомный редактор для StateMachineData.
    ///     Предоставляет визуальное отображение состояний и переходов в инспекторе.
    /// </summary>
    [CustomEditor(typeof(StateMachineData))]
    [CanEditMultipleObjects]
    public class StateMachineDataEditor : UnityEditor.Editor
    {
        private StateMachineData data;

        private void OnEnable()
        {
            data = target as StateMachineData;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State Machine Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Валидация (silent режим - не логируем в консоль при каждой отрисовке)
            bool isValid = data.Validate(silent: true);
            if (!isValid)
            {
                EditorGUILayout.HelpBox("State Machine configuration has errors. Add states to configure the state machine.", MessageType.Warning);
            }

            // Отображение состояний
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
            SerializedProperty statesProp = serializedObject.FindProperty("states");
            EditorGUILayout.PropertyField(statesProp, true);

            EditorGUILayout.Space();

            // Начальное состояние
            EditorGUILayout.LabelField("Initial State", EditorStyles.boldLabel);
            SerializedProperty initialStateProp = serializedObject.FindProperty("initialState");
            EditorGUILayout.PropertyField(initialStateProp, new GUIContent("Initial State (StateData)"));
            
            // Legacy поле для обратной совместимости (скрыто, но сохраняется)
            SerializedProperty initialStateNameProp = serializedObject.FindProperty("initialStateName");
            if (initialStateProp.objectReferenceValue == null && !string.IsNullOrEmpty(initialStateNameProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Using legacy initial state name: {initialStateNameProp.stringValue}. Please assign a StateData object instead.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Переходы
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
            SerializedProperty transitionsProp = serializedObject.FindProperty("transitions");
            EditorGUILayout.PropertyField(transitionsProp, true);

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
                    EditorUtility.DisplayDialog("Validation", "State Machine configuration has errors. Check the console.", "OK");
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    ///     Кастомный редактор для StateMachineData.
    ///     Регистрация происходит автоматически через атрибут [CustomEditor] и StateMachineEditorRegistrar.
    /// </summary>
}

