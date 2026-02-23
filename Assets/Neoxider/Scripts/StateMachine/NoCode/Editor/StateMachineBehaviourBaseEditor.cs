using System.Linq;
using Neo.Editor;
using Neo.StateMachine;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    [CustomEditor(typeof(StateMachineBehaviourBase))]
    public class StateMachineBehaviourBaseEditor : CustomEditorBase
    {
        private int selectedStateIndex;

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
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDebugLog"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showStateInInspector"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoEvaluateTransitions"));
        }

        private void DrawReferencesSection()
        {
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            SerializedProperty dataProp = serializedObject.FindProperty("stateMachineData");
            EditorGUILayout.PropertyField(dataProp);
        }

        private void DrawEventsSection()
        {
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onInitialized"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onStateEntered"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onStateExited"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onStateChanged"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onTransitionEvaluated"));
        }

        private void DrawRuntimeSection()
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentStateName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("previousStateName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stateChangeCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stateEnterTime"));
            }
        }

        private void DrawControlButtons()
        {
            StateMachineBehaviourBase behaviour = (StateMachineBehaviourBase)target;
            SerializedProperty dataProp = serializedObject.FindProperty("stateMachineData");
            StateMachineData data = dataProp?.objectReferenceValue as StateMachineData;

            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

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
            if (GUILayout.Button("Go To Initial State"))
            {
                behaviour.GoToInitialState();
            }

            if (data == null || data.States == null || data.States.Length == 0)
            {
                return;
            }

            string[] stateNames = data.States
                .Where(s => s != null)
                .Select(s => s.StateName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray();

            if (stateNames.Length == 0)
            {
                return;
            }

            selectedStateIndex = Mathf.Clamp(selectedStateIndex, 0, stateNames.Length - 1);
            selectedStateIndex = EditorGUILayout.Popup("Target State", selectedStateIndex, stateNames);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            if (GUILayout.Button("Change State"))
            {
                behaviour.ChangeState(stateNames[selectedStateIndex]);
            }
        }
    }
}
