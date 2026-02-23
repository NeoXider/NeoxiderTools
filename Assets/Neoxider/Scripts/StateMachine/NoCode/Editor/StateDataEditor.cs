using System;
using System.Linq;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    [CustomEditor(typeof(StateData))]
    public class StateDataEditor : CustomEditorBase
    {
        private Type[] actionTypes;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        private void OnEnable()
        {
            actionTypes = TypeCache.GetTypesDerivedFrom<StateAction>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .ToArray();
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty stateNameProp = serializedObject.FindProperty("stateName");
            EditorGUILayout.PropertyField(stateNameProp, new GUIContent("State Name"));

            EditorGUILayout.Space(8);
            DrawActionList("On Enter Actions", serializedObject.FindProperty("onEnterActions"));
            EditorGUILayout.Space(6);
            DrawActionList("On Update Actions", serializedObject.FindProperty("onUpdateActions"));
            EditorGUILayout.Space(6);
            DrawActionList("On Exit Actions", serializedObject.FindProperty("onExitActions"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActionList(string label, SerializedProperty listProperty)
        {
            if (listProperty == null)
            {
                EditorGUILayout.HelpBox($"Property for '{label}' not found.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            int removeIndex = -1;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetManagedReferenceTypeName(element), EditorStyles.miniBoldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                listProperty.DeleteArrayElementAtIndex(removeIndex);
            }

            if (GUILayout.Button($"Add {label}", GUILayout.Height(22)))
            {
                ShowAddActionMenu(listProperty);
            }
        }

        private void ShowAddActionMenu(SerializedProperty listProperty)
        {
            GenericMenu menu = new();
            if (actionTypes == null || actionTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No action types available"));
                menu.ShowAsContext();
                return;
            }

            foreach (Type actionType in actionTypes)
            {
                menu.AddItem(new GUIContent(actionType.Name), false, () => AddAction(listProperty, actionType));
            }

            menu.ShowAsContext();
        }

        private void AddAction(SerializedProperty listProperty, Type actionType)
        {
            serializedObject.Update();
            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = Activator.CreateInstance(actionType);
            serializedObject.ApplyModifiedProperties();
        }

        private static string GetManagedReferenceTypeName(SerializedProperty property)
        {
            if (property == null || string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                return "Action";
            }

            string[] split = property.managedReferenceFullTypename.Split(' ');
            if (split.Length != 2)
            {
                return "Action";
            }

            string fullTypeName = split[1];
            int lastDot = fullTypeName.LastIndexOf('.');
            return lastDot >= 0 ? fullTypeName[(lastDot + 1)..] : fullTypeName;
        }
    }
}
