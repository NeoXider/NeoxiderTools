using System;
using Neo.Editor;
using Neo.Pages;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(UIPage))]
    public sealed class UIPageEditor : CustomEditorBase
    {
        private const float ModeButtonHeight = 22f;
        private const string SourceLabelAll = "Источник: все папки проекта";
        private SerializedProperty animationProp;

        private string generateName = "Menu";
        private SerializedProperty ignoreOnExclusiveChangeProp;
        private SerializedProperty onlyPlayBackwardProp;

        private SerializedProperty pageIdProp;
        private SerializedProperty playBackwardProp;
        private SerializedProperty popupProp;
        private int selectorMode; // 0 dropdown, 1 asset

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        private void OnEnable()
        {
            pageIdProp = serializedObject.FindProperty("pageId");
            popupProp = serializedObject.FindProperty("popup");
            ignoreOnExclusiveChangeProp = serializedObject.FindProperty("ignoreOnExclusiveChange");
            animationProp = serializedObject.FindProperty("_animation");
            playBackwardProp = serializedObject.FindProperty("_playBackward");
            onlyPlayBackwardProp = serializedObject.FindProperty("_onlyPlayBackward");
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            NeoxiderModuleInspectorHeader.Draw(typeof(UIPageEditor).Assembly, "Neoxider Pages");

            serializedObject.Update();

            EditorGUILayout.LabelField("Page", EditorStyles.boldLabel);
            DrawPageIdSelector(pageIdProp);
            EditorGUILayout.PropertyField(popupProp);
            EditorGUILayout.PropertyField(ignoreOnExclusiveChangeProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Anim", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(animationProp);
            EditorGUILayout.PropertyField(playBackwardProp);
            EditorGUILayout.PropertyField(onlyPlayBackwardProp);

            serializedObject.ApplyModifiedProperties();
        }

        protected override void ProcessAttributeAssignments()
        {
            // Pages-инспекторы не используют авто-assign из NeoCustomEditor.
        }

        private void DrawPageIdSelector(SerializedProperty pageId)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Page Id", EditorStyles.miniBoldLabel);
                selectorMode = DrawSegmentedMode(selectorMode,
                    new GUIContent("Dropdown", "Выбор из PageId ассетов по папке"),
                    new GUIContent("Asset", "Ручной выбор конкретного PageId ассета"));
                EditorGUILayout.Space(2);

                if (selectorMode == 0)
                {
                    PageId[] ids = FindAllPageIds(null);
                    if (ids.Length == 0)
                    {
                        EditorGUILayout.HelpBox(
                            "В проекте нет PageId ассетов.\nСоздай PageId вручную или сгенерируй через меню: Tools → Neo → Pages → Generate Default PageIds.",
                            MessageType.Warning);
                        return;
                    }

                    string[] labels = PageIdEditorCache.GetLabels(null);

                    PageId current = pageId.objectReferenceValue as PageId;
                    int currentIdx = current == null ? 0 : Array.FindIndex(ids, x => x == current) + 1;
                    if (currentIdx < 0)
                    {
                        currentIdx = 0;
                    }

                    int newIdx = EditorGUILayout.Popup(new GUIContent("Page"), currentIdx, labels);
                    if (newIdx == 0)
                    {
                        pageId.objectReferenceValue = null;
                    }
                    else if (newIdx > 0 && newIdx <= ids.Length)
                    {
                        pageId.objectReferenceValue = ids[newIdx - 1];
                    }

                    EditorGUILayout.LabelField(SourceLabelAll, EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.PropertyField(pageId, new GUIContent("Page Id (asset)"));
                }
            }

            DrawGenerateAndAssign(pageId);
        }

        private static int DrawSegmentedMode(int value, GUIContent left, GUIContent right)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int selected = value;

                Color prevBg = GUI.backgroundColor;
                Color prevContent = GUI.contentColor;

                DrawModeButton(0, left, ref selected, EditorStyles.miniButtonLeft);
                DrawModeButton(1, right, ref selected, EditorStyles.miniButtonRight);

                GUI.backgroundColor = prevBg;
                GUI.contentColor = prevContent;

                return selected;
            }
        }

        private static void DrawModeButton(int index, GUIContent content, ref int selected, GUIStyle style)
        {
            bool isSelected = selected == index;
            GUI.backgroundColor = isSelected ? GetSelectedBackgroundColor() : Color.white;
            GUI.contentColor = isSelected ? GetSelectedContentColor() : Color.white;

            if (GUILayout.Button(content, style, GUILayout.Height(ModeButtonHeight)))
            {
                selected = index;
            }
        }

        private static Color GetSelectedBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.55f, 0.95f, 1f)
                : new Color(0.20f, 0.45f, 0.90f, 1f);
        }

        private static Color GetSelectedContentColor()
        {
            return Color.white;
        }

        private void DrawGenerateAndAssign(SerializedProperty pageId)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Generate", EditorStyles.miniBoldLabel);
            PageId current = pageId.objectReferenceValue as PageId;
            if (current != null)
            {
                generateName = current.DisplayName;
            }

            using (new EditorGUI.DisabledScope(current != null))
            {
                generateName = EditorGUILayout.TextField("Page Name", generateName);
            }

            using (new EditorGUI.DisabledScope(current != null || string.IsNullOrWhiteSpace(generateName)))
            {
                if (GUILayout.Button("Generate & Assign"))
                {
                    string folder = PageIdGenerator.GetPreferredFolder();
                    string normalizedName = generateName;
                    string assetName = normalizedName.Trim().StartsWith("Page", StringComparison.Ordinal)
                        ? normalizedName.Trim()
                        : "Page" + normalizedName.Trim();
                    string path = $"{folder}/{assetName}.asset";
                    bool alreadyExists = AssetDatabase.LoadAssetAtPath<PageId>(path) != null;

                    PageId id = PageIdGenerator.GetOrCreate(normalizedName, folder);
                    if (id != null)
                    {
                        if (alreadyExists)
                        {
                            Debug.LogWarning($"[UIPage] PageId already exists: {path}. Assigned existing.",
                                (UIPage)target);
                        }

                        pageId.objectReferenceValue = id;
                    }
                }
            }
        }

        private static PageId[] FindAllPageIds(string folder = null)
        {
            return PageIdEditorCache.GetIds(folder);
        }
    }
}