using System.Collections.Generic;
using Neo.Progression;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Progression
{
    [CustomEditor(typeof(LevelCurveDefinition))]
    [CanEditMultipleObjects]
    public sealed class LevelCurveDefinitionEditor : CustomEditorBase
    {
        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            LevelCurveDefinition definition = (LevelCurveDefinition)target;
            SerializedProperty levelsProperty = serializedObject.FindProperty("_levels");
            IReadOnlyList<string> issues = definition.ValidateDefinition();
            int levelCount = levelsProperty != null ? levelsProperty.arraySize : 0;

            List<NeoxiderEditorGUI.Badge> badges = new()
            {
                new($"Levels {levelCount}", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new(issues.Count == 0 ? "Validated" : "Needs Review",
                    issues.Count == 0 ? new Color(0.18f, 0.62f, 0.32f, 1f) : new Color(0.78f, 0.46f, 0.18f, 1f))
            };

            NeoxiderEditorGUI.DrawSummaryCard("Level Curve Definition",
                "Defines XP thresholds, level transitions, perk point grants, and optional level rewards.",
                badges.ToArray());
            EditorGUILayout.Space(4f);

            ProgressionEditorHelpers.DrawValidationBlock(issues, "No level curve issues detected.");
            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Levels", "Configure cumulative XP thresholds in ascending order.");
            EditorGUILayout.PropertyField(levelsProperty, true);
            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
