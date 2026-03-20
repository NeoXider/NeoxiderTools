using System.Collections.Generic;
using Neo.Progression;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Progression
{
    [CustomEditor(typeof(UnlockTreeDefinition))]
    [CanEditMultipleObjects]
    public sealed class UnlockTreeDefinitionEditor : CustomEditorBase
    {
        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            var definition = (UnlockTreeDefinition)target;
            SerializedProperty nodesProperty = serializedObject.FindProperty("_nodes");
            IReadOnlyList<string> issues = definition.ValidateDefinition();
            int nodeCount = nodesProperty != null ? nodesProperty.arraySize : 0;

            List<NeoxiderEditorGUI.Badge> badges = new()
            {
                new NeoxiderEditorGUI.Badge($"Nodes {nodeCount}", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge(issues.Count == 0 ? "Validated" : "Needs Review",
                    issues.Count == 0 ? new Color(0.18f, 0.62f, 0.32f, 1f) : new Color(0.78f, 0.46f, 0.18f, 1f))
            };

            NeoxiderEditorGUI.DrawSummaryCard("Unlock Tree Definition",
                "Defines unlock nodes, prerequisites, additional conditions, and one-time rewards.",
                badges.ToArray());
            EditorGUILayout.Space(4f);

            ProgressionEditorHelpers.DrawValidationBlock(issues, "No unlock tree issues detected.");
            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Nodes",
                "Each node can define required level, prerequisite nodes, and rewards.");
            EditorGUILayout.PropertyField(nodesProperty, true);
            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
