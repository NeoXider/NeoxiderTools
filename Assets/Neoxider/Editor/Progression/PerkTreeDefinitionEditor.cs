using System.Collections.Generic;
using Neo.Progression;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Progression
{
    [CustomEditor(typeof(PerkTreeDefinition))]
    [CanEditMultipleObjects]
    public sealed class PerkTreeDefinitionEditor : CustomEditorBase
    {
        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            var definition = (PerkTreeDefinition)target;
            SerializedProperty perksProperty = serializedObject.FindProperty("_perks");
            IReadOnlyList<string> issues = definition.ValidateDefinition();
            int perkCount = perksProperty != null ? perksProperty.arraySize : 0;

            List<NeoxiderEditorGUI.Badge> badges = new()
            {
                new NeoxiderEditorGUI.Badge($"Perks {perkCount}", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge(issues.Count == 0 ? "Validated" : "Needs Review",
                    issues.Count == 0 ? new Color(0.18f, 0.62f, 0.32f, 1f) : new Color(0.78f, 0.46f, 0.18f, 1f))
            };

            NeoxiderEditorGUI.DrawSummaryCard("Perk Tree Definition",
                "Defines perk costs, level requirements, prerequisite perks, and unlock node dependencies.",
                badges.ToArray());
            EditorGUILayout.Space(4f);

            ProgressionEditorHelpers.DrawValidationBlock(issues, "No perk tree issues detected.");
            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Perks", "Each perk can spend perk points and dispatch one-time rewards.");
            EditorGUILayout.PropertyField(perksProperty, true);
            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
