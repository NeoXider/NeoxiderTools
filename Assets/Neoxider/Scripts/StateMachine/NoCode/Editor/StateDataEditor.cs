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

        private void OnEnable()
        {
            actionTypes = TypeCache.GetTypesDerivedFrom<StateAction>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .ToArray();
        }

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty stateNameProp = serializedObject.FindProperty("stateName");
            SerializedProperty onEnterProp = serializedObject.FindProperty("onEnterActions");
            SerializedProperty onUpdateProp = serializedObject.FindProperty("onUpdateActions");
            SerializedProperty onExitProp = serializedObject.FindProperty("onExitActions");

            DrawSummary(stateNameProp, onEnterProp, onUpdateProp, onExitProp);

            NeoxiderEditorGUI.BeginSection("Identity", "State name and base asset configuration.");
            EditorGUILayout.PropertyField(stateNameProp, new GUIContent("State Name"));
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(6);
            DrawActionList("On Enter Actions", onEnterProp, "Run once when entering the state.");
            EditorGUILayout.Space(6);
            DrawActionList("On Update Actions", onUpdateProp, "Run every frame while the state is active.");
            EditorGUILayout.Space(6);
            DrawActionList("On Exit Actions", onExitProp, "Run when leaving the state.");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary(SerializedProperty stateNameProp, SerializedProperty onEnterProp,
            SerializedProperty onUpdateProp, SerializedProperty onExitProp)
        {
            string title = string.IsNullOrWhiteSpace(stateNameProp.stringValue)
                ? target.name
                : stateNameProp.stringValue;

            NeoxiderEditorGUI.DrawSummaryCard(title,
                "State asset with enter, update, and exit action groups. Action types are cached and not recomputed every repaint.",
                new NeoxiderEditorGUI.Badge($"Enter {onEnterProp.arraySize}", new Color(0.18f, 0.62f, 0.32f, 1f)),
                new NeoxiderEditorGUI.Badge($"Update {onUpdateProp.arraySize}", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge($"Exit {onExitProp.arraySize}", new Color(0.78f, 0.46f, 0.18f, 1f)),
                new NeoxiderEditorGUI.Badge($"Action Types {actionTypes?.Length ?? 0}",
                    new Color(0.42f, 0.34f, 0.82f, 1f)));

            if (string.IsNullOrWhiteSpace(stateNameProp.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "State name is empty. Set a meaningful name so transitions and debugging stay readable.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawActionList(string label, SerializedProperty listProperty, string subtitle)
        {
            if (listProperty == null)
            {
                EditorGUILayout.HelpBox($"Property for '{label}' not found.", MessageType.Error);
                return;
            }

            NeoxiderEditorGUI.BeginSection(label, subtitle);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Count: {listProperty.arraySize}", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button($"Add {label}", GUILayout.Height(22), GUILayout.Width(140)))
                {
                    ShowAddActionMenu(listProperty);
                }
            }

            if (listProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("List is empty. Leave it that way if this lifecycle stage is unused.",
                    MessageType.Info);
            }

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

            NeoxiderEditorGUI.EndSection();
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
