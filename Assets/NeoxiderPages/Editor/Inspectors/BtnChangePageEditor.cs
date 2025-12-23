using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(BtnChangePage))]
    public sealed class BtnChangePageEditor : CustomEditorBase
    {
        private const string DefaultFolder = "Assets/NeoxiderPages/Pages";
        private const float ModeButtonHeight = 22f;

        private SerializedProperty intecactableProp;
        private SerializedProperty imageTargetProp;
        private SerializedProperty actionProp;
        private SerializedProperty targetPageIdProp;
        private SerializedProperty canSwitchProp;
        private SerializedProperty executeStateProp;

        private SerializedProperty useAnimImageProp;
        private SerializedProperty timeAnimImageProp;
        private SerializedProperty scaleAnimProp;

        private SerializedProperty changeTextProp;
        private SerializedProperty textPageProp;

        private SerializedProperty onClickProp;

        private int selectorMode;

        private void OnEnable()
        {
            intecactableProp = serializedObject.FindProperty("intecactable");
            imageTargetProp = serializedObject.FindProperty("_imageTarget");
            actionProp = serializedObject.FindProperty("action");
            targetPageIdProp = serializedObject.FindProperty("targetPageId");
            canSwitchProp = serializedObject.FindProperty("_canSwitchPage");
            executeStateProp = serializedObject.FindProperty("_executeState");

            useAnimImageProp = serializedObject.FindProperty("_useAnimImage");
            timeAnimImageProp = serializedObject.FindProperty("_timeAnimImage");
            scaleAnimProp = serializedObject.FindProperty("_scaleAnim");

            changeTextProp = serializedObject.FindProperty("_changeText");
            textPageProp = serializedObject.FindProperty("_textPage");

            onClickProp = serializedObject.FindProperty("OnClick");
        }

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            NeoxiderModuleInspectorHeader.Draw(typeof(BtnChangePageEditor).Assembly, "Neoxider Pages");

            serializedObject.Update();

            EditorGUILayout.PropertyField(intecactableProp);
            EditorGUILayout.PropertyField(imageTargetProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Page Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(actionProp);

            if ((BtnChangePage.Action)actionProp.enumValueIndex == BtnChangePage.Action.OpenPage)
            {
                DrawPageIdSelector(targetPageIdProp);
            }

            EditorGUILayout.PropertyField(canSwitchProp);
            EditorGUILayout.PropertyField(executeStateProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useAnimImageProp);
            if (useAnimImageProp.boolValue)
            {
                EditorGUILayout.PropertyField(timeAnimImageProp);
                EditorGUILayout.PropertyField(scaleAnimProp);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("SetText", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(changeTextProp);
            if (changeTextProp.boolValue)
            {
                EditorGUILayout.PropertyField(textPageProp);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(onClickProp);

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
                EditorGUILayout.LabelField("Target Page", EditorStyles.miniBoldLabel);
                selectorMode = DrawSegmentedMode(selectorMode,
                    new GUIContent("Dropdown", "Выбор из PageId ассетов по папке"),
                    new GUIContent("Asset", "Ручной выбор конкретного PageId ассета"));
                EditorGUILayout.Space(2);

                if (selectorMode == 0)
                {
                    PageId[] ids = FindAllPageIds(DefaultFolder);
                    if (ids.Length == 0)
                    {
                        EditorGUILayout.HelpBox(
                            $"В папке нет PageId ассетов: {DefaultFolder}\nСоздай PageId вручную или сгенерируй через меню: Tools → Neo → Pages → Generate Default PageIds.",
                            MessageType.Warning);
                        return;
                    }

                    string[] labels = PageIdEditorCache.GetLabels(DefaultFolder);

                    PageId current = pageId.objectReferenceValue as PageId;
                    int currentIdx = current == null ? 0 : System.Array.FindIndex(ids, x => x == current) + 1;
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

                    EditorGUILayout.LabelField($"Источник: {DefaultFolder}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.PropertyField(pageId, new GUIContent("Page Id (asset)"));
                }
            }
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

        private static PageId[] FindAllPageIds(string folder)
        {
            return PageIdEditorCache.GetIds(folder);
        }
    }
}