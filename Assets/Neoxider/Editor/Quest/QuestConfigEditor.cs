using System.Collections.Generic;
using Neo.Quest;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Quest
{
    [CustomEditor(typeof(QuestConfig))]
    [CanEditMultipleObjects]
    public sealed class QuestConfigEditor : CustomEditorBase
    {
        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty idProp = serializedObject.FindProperty("_id");
            SerializedProperty titleProp = serializedObject.FindProperty("_title");
            SerializedProperty descriptionProp = serializedObject.FindProperty("_description");
            SerializedProperty iconProp = serializedObject.FindProperty("_icon");
            SerializedProperty objectivesProp = serializedObject.FindProperty("_objectives");
            SerializedProperty startConditionsProp = serializedObject.FindProperty("_startConditions");
            SerializedProperty nextQuestIdsProp = serializedObject.FindProperty("_nextQuestIds");

            DrawSummary(idProp, titleProp, iconProp, objectivesProp, startConditionsProp, nextQuestIdsProp);
            DrawValidation(idProp, titleProp, objectivesProp, nextQuestIdsProp);

            NeoxiderEditorGUI.BeginSection("Identity", "Базовые идентификаторы и отображение квеста.");
            EditorGUILayout.PropertyField(idProp);
            EditorGUILayout.PropertyField(titleProp);
            DrawGenerateIdButton(idProp, titleProp);
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Display", "Текст и визуальное представление для UI.");
            EditorGUILayout.PropertyField(descriptionProp);
            EditorGUILayout.PropertyField(iconProp);
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Objectives", "Список целей. Порядок элементов определяет их индекс.");
            EditorGUILayout.PropertyField(objectivesProp, true);
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Flow", "Условия старта и цепочка следующих квестов.");
            EditorGUILayout.PropertyField(startConditionsProp, true);
            EditorGUILayout.PropertyField(nextQuestIdsProp, true);
            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary(SerializedProperty idProp, SerializedProperty titleProp, SerializedProperty iconProp,
            SerializedProperty objectivesProp, SerializedProperty startConditionsProp,
            SerializedProperty nextQuestIdsProp)
        {
            string title = string.IsNullOrWhiteSpace(titleProp.stringValue)
                ? target.name
                : titleProp.stringValue;
            string id = string.IsNullOrWhiteSpace(idProp.stringValue)
                ? "ID will be generated from title"
                : idProp.stringValue;

            List<NeoxiderEditorGUI.Badge> badges = new()
            {
                new NeoxiderEditorGUI.Badge(string.IsNullOrWhiteSpace(idProp.stringValue) ? "Draft" : "Configured",
                    string.IsNullOrWhiteSpace(idProp.stringValue)
                        ? new Color(0.76f, 0.48f, 0.16f, 1f)
                        : new Color(0.18f, 0.62f, 0.32f, 1f)),
                new NeoxiderEditorGUI.Badge($"Objectives {objectivesProp.arraySize}",
                    new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge($"Start Conditions {startConditionsProp.arraySize}",
                    new Color(0.42f, 0.34f, 0.82f, 1f)),
                new NeoxiderEditorGUI.Badge($"Next Quests {nextQuestIdsProp.arraySize}",
                    new Color(0.24f, 0.60f, 0.60f, 1f)),
                new NeoxiderEditorGUI.Badge(iconProp.objectReferenceValue != null ? "Icon Ready" : "No Icon",
                    iconProp.objectReferenceValue != null
                        ? new Color(0.28f, 0.62f, 0.56f, 1f)
                        : new Color(0.38f, 0.38f, 0.42f, 1f))
            };

            NeoxiderEditorGUI.DrawSummaryCard(title, $"Quest ID: <b>{id}</b>", badges.ToArray());
            EditorGUILayout.Space(4f);
        }

        private void DrawValidation(SerializedProperty idProp, SerializedProperty titleProp,
            SerializedProperty objectivesProp,
            SerializedProperty nextQuestIdsProp)
        {
            if (string.IsNullOrWhiteSpace(titleProp.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Заполни Title. Без него квест плохо читается в UI и ID не сможет автосгенерироваться.",
                    MessageType.Warning);
            }

            if (objectivesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Квест пока не содержит ни одной цели.", MessageType.Info);
            }

            if (HasDuplicateValues(nextQuestIdsProp))
            {
                EditorGUILayout.HelpBox(
                    "В списке Next Quest Ids есть дубликаты. Лучше держать цепочку переходов уникальной.",
                    MessageType.Warning);
            }

            if (string.IsNullOrWhiteSpace(idProp.stringValue) && !string.IsNullOrWhiteSpace(titleProp.stringValue))
            {
                EditorGUILayout.HelpBox("ID будет заполнен из Title при валидации asset или по кнопке Generate ID.",
                    MessageType.Info);
            }
        }

        private void DrawGenerateIdButton(SerializedProperty idProp, SerializedProperty titleProp)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(titleProp.stringValue));
                if (GUILayout.Button("Generate ID", GUILayout.Width(110f)))
                {
                    idProp.stringValue = titleProp.stringValue.Replace(" ", "_");
                }

                EditorGUI.EndDisabledGroup();
            }
        }

        private static bool HasDuplicateValues(SerializedProperty arrayProp)
        {
            if (arrayProp == null || !arrayProp.isArray)
            {
                return false;
            }

            HashSet<string> values = new();
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                string value = arrayProp.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (!values.Add(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
