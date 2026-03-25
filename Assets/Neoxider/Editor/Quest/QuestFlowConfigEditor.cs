using System.Collections.Generic;
using Neo.Quest;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Quest
{
    [CustomEditor(typeof(QuestFlowConfig))]
    [CanEditMultipleObjects]
    public sealed class QuestFlowConfigEditor : CustomEditorBase
    {
        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty chainsProp = serializedObject.FindProperty("_chains");
            SerializedProperty standaloneProp = serializedObject.FindProperty("_standaloneQuests");

            DrawSummary(chainsProp, standaloneProp);
            DrawValidation(chainsProp, standaloneProp);

            NeoxiderEditorGUI.BeginSection("Chains", "Sequential quest chains with optional strict order.");
            EditorGUILayout.PropertyField(chainsProp, true);
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Standalone Quests", "Quests not tied to any chain.");
            EditorGUILayout.PropertyField(standaloneProp, true);
            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary(SerializedProperty chainsProp, SerializedProperty standaloneProp)
        {
            int strictChains = 0;
            int totalQuestRefs = 0;
            int duplicateRefs = 0;
            HashSet<Object> uniqueQuests = new();

            for (int i = 0; i < chainsProp.arraySize; i++)
            {
                SerializedProperty chainProp = chainsProp.GetArrayElementAtIndex(i);
                if (chainProp.FindPropertyRelative("_strictOrder").boolValue)
                {
                    strictChains++;
                }

                SerializedProperty questsProp = chainProp.FindPropertyRelative("_quests");
                for (int j = 0; j < questsProp.arraySize; j++)
                {
                    totalQuestRefs++;
                    Object quest = questsProp.GetArrayElementAtIndex(j).objectReferenceValue;
                    if (quest != null && !uniqueQuests.Add(quest))
                    {
                        duplicateRefs++;
                    }
                }
            }

            for (int i = 0; i < standaloneProp.arraySize; i++)
            {
                totalQuestRefs++;
                Object quest = standaloneProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (quest != null && !uniqueQuests.Add(quest))
                {
                    duplicateRefs++;
                }
            }

            List<NeoxiderEditorGUI.Badge> badges = new()
            {
                new NeoxiderEditorGUI.Badge($"Chains {chainsProp.arraySize}", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge($"Strict {strictChains}", new Color(0.42f, 0.34f, 0.82f, 1f)),
                new NeoxiderEditorGUI.Badge($"Standalone {standaloneProp.arraySize}",
                    new Color(0.24f, 0.60f, 0.60f, 1f)),
                new NeoxiderEditorGUI.Badge($"Quest Refs {totalQuestRefs}", new Color(0.18f, 0.62f, 0.32f, 1f))
            };

            if (duplicateRefs > 0)
            {
                badges.Add(new NeoxiderEditorGUI.Badge($"Duplicates {duplicateRefs}",
                    new Color(0.78f, 0.46f, 0.18f, 1f)));
            }

            NeoxiderEditorGUI.DrawSummaryCard("Quest Flow Config",
                "Config drives chains and standalone quests. Summary above shows structure size and potential duplicates.",
                badges.ToArray());
            EditorGUILayout.Space(4f);
        }

        private void DrawValidation(SerializedProperty chainsProp, SerializedProperty standaloneProp)
        {
            if (chainsProp.arraySize == 0 && standaloneProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Config is empty. Add at least one chain or one standalone quest.",
                    MessageType.Info);
            }

            if (HasEmptyChains(chainsProp))
            {
                EditorGUILayout.HelpBox(
                    "Some chains have no quests. Fill them or remove them to keep the config clean.",
                    MessageType.Warning);
            }

            if (HasDuplicateQuestReferences(chainsProp, standaloneProp))
            {
                EditorGUILayout.HelpBox(
                    "The same QuestConfig appears more than once. That is not always valid and can confuse progression flow.",
                    MessageType.Warning);
            }
        }

        private static bool HasEmptyChains(SerializedProperty chainsProp)
        {
            for (int i = 0; i < chainsProp.arraySize; i++)
            {
                SerializedProperty questsProp = chainsProp.GetArrayElementAtIndex(i).FindPropertyRelative("_quests");
                if (questsProp.arraySize == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDuplicateQuestReferences(SerializedProperty chainsProp,
            SerializedProperty standaloneProp)
        {
            HashSet<Object> unique = new();

            for (int i = 0; i < chainsProp.arraySize; i++)
            {
                SerializedProperty questsProp = chainsProp.GetArrayElementAtIndex(i).FindPropertyRelative("_quests");
                for (int j = 0; j < questsProp.arraySize; j++)
                {
                    Object quest = questsProp.GetArrayElementAtIndex(j).objectReferenceValue;
                    if (quest != null && !unique.Add(quest))
                    {
                        return true;
                    }
                }
            }

            for (int i = 0; i < standaloneProp.arraySize; i++)
            {
                Object quest = standaloneProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (quest != null && !unique.Add(quest))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
