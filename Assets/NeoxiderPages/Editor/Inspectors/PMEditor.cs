using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(PM))]
    public sealed class PMEditor : UnityEditor.Editor
    {
        private const string DefaultFolder = "Assets/NeoxiderPages/Pages";

        private SerializedProperty currentUiPageProp;
        private SerializedProperty previousUiPageProp;
        private SerializedProperty allPagesProp;
        private SerializedProperty startupPageProp;
        private SerializedProperty ignoredPageIdsProp;
        private SerializedProperty onPageChangedProp;
        private SerializedProperty refreshPagesInEditorProp;
        private SerializedProperty editorActivePageIdProp;
        private SerializedProperty autoSelectEditorPageProp;

        private int startupSelectMode; // 0 dropdown, 1 asset
        private int ignoredSelectMode; // 0 dropdown, 1 asset
        private int editorSelectMode; // 0 buttons, 1 dropdown, 2 asset

        private ReorderableList ignoredList;

        private void OnEnable()
        {
            currentUiPageProp = serializedObject.FindProperty("currentUiPage");
            previousUiPageProp = serializedObject.FindProperty("previousUiPage");
            allPagesProp = serializedObject.FindProperty("allPages");
            startupPageProp = serializedObject.FindProperty("startupPage");
            ignoredPageIdsProp = serializedObject.FindProperty("ignoredPageIds");
            onPageChangedProp = serializedObject.FindProperty("OnPageChanged");
            refreshPagesInEditorProp = serializedObject.FindProperty("refreshPagesInEditor");
            editorActivePageIdProp = serializedObject.FindProperty("editorActivePageId");
            autoSelectEditorPageProp = serializedObject.FindProperty("autoSelectEditorPage");

            ignoredList = new ReorderableList(serializedObject, ignoredPageIdsProp, true, true, true, true);
            ignoredList.drawHeaderCallback =
                rect => EditorGUI.LabelField(rect, "Ignored Pages (do not change active state)");
            ignoredList.drawElementCallback = DrawIgnoredElement;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            DrawStartupSelector();

            EditorGUILayout.Space(4);
            DrawIgnoredSelectorMode();
            ignoredList.DoLayoutList();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentUiPageProp);
            EditorGUILayout.PropertyField(previousUiPageProp);
            EditorGUILayout.PropertyField(allPagesProp, true);
            EditorGUILayout.PropertyField(onPageChangedProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(refreshPagesInEditorProp);
            EditorGUILayout.PropertyField(autoSelectEditorPageProp);
            DrawEditorActiveSelector();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStartupSelector()
        {
            EditorGUILayout.LabelField("Startup Page", EditorStyles.boldLabel);
            startupSelectMode = GUILayout.Toolbar(startupSelectMode, new[] { "Dropdown", "Asset" });

            if (startupSelectMode == 0)
            {
                DrawPageIdDropdown(startupPageProp, "Startup Page");
            }
            else
            {
                EditorGUILayout.PropertyField(startupPageProp, new GUIContent("Startup Page (asset)"));
            }
        }

        private void DrawIgnoredSelectorMode()
        {
            EditorGUILayout.LabelField("Ignored Pages", EditorStyles.boldLabel);
            ignoredSelectMode = GUILayout.Toolbar(ignoredSelectMode, new[] { "Dropdown", "Asset" });
        }

        private void DrawIgnoredElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = ignoredPageIdsProp.GetArrayElementAtIndex(index);
            rect.y += 1;

            if (ignoredSelectMode == 0)
            {
                DrawPageIdDropdown(element, GUIContent.none, rect);
            }
            else
            {
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            }
        }

        private void DrawEditorActiveSelector()
        {
            EditorGUILayout.LabelField("Editor Active Page", EditorStyles.boldLabel);
            editorSelectMode = GUILayout.Toolbar(editorSelectMode, new[] { "Buttons", "Dropdown", "Asset" });

            if (editorSelectMode == 0)
            {
                DrawEditorButtons();
                EditorGUILayout.PropertyField(editorActivePageIdProp, new GUIContent("Editor Active (asset)"));
            }
            else if (editorSelectMode == 1)
            {
                DrawPageIdDropdown(editorActivePageIdProp, "Editor Active Page");
            }
            else
            {
                EditorGUILayout.PropertyField(editorActivePageIdProp, new GUIContent("Editor Active Page"));
            }
        }

        private void DrawEditorButtons()
        {
            PageId[] ids = FindAllPageIds(DefaultFolder);
            if (ids.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"В папке нет PageId ассетов: {DefaultFolder}\nСоздай PageId вручную или сгенерируй через меню: Tools → Neo → Pages → Generate Default PageIds.",
                    MessageType.Warning);
                return;
            }

            const float buttonWidth = 110f;
            const float buttonHeight = 22f;

            int columns = Mathf.Clamp(Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 40) / buttonWidth), 2, 6);
            int i = 0;

            while (i < ids.Length)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns && i < ids.Length; c++, i++)
                {
                    PageId id = ids[i];
                    using (new EditorGUI.DisabledScope(id == null))
                    {
                        if (GUILayout.Button(id.DisplayName, GUILayout.Width(buttonWidth),
                                GUILayout.Height(buttonHeight), GUILayout.ExpandWidth(false)))
                        {
                            editorActivePageIdProp.objectReferenceValue = id;
                            ApplyAndPreview(id);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void ApplyAndPreview(PageId id)
        {
            serializedObject.ApplyModifiedProperties();

            if (EditorApplication.isPlaying)
            {
                return;
            }

            PM pm = (PM)target;
            if (pm == null || id == null)
            {
                return;
            }

            // Превью в редакторе: деактивируем и активируем выбранную страницу (как делает OnValidate, но без ожидания).
            pm.ActivateAll(false);
            pm.ActivatePages(id, true, GetIgnoredArray(pm), false);
        }

        private static PageId[] GetIgnoredArray(PM pm)
        {
            // Берём текущие сериализованные значения с объекта (если null — пусто)
            SerializedObject so = new(pm);
            SerializedProperty prop = so.FindProperty("ignoredPageIds");
            if (prop == null || !prop.isArray)
            {
                return new PageId[] { };
            }

            PageId[] result = new PageId[prop.arraySize];
            for (int i = 0; i < prop.arraySize; i++)
            {
                result[i] = prop.GetArrayElementAtIndex(i).objectReferenceValue as PageId;
            }

            return result.Where(x => x != null).ToArray();
        }

        private static void DrawPageIdDropdown(SerializedProperty prop, string label)
        {
            DrawPageIdDropdown(prop, new GUIContent(label));
        }

        private static void DrawPageIdDropdown(SerializedProperty prop, GUIContent label, Rect? rectOverride = null)
        {
            PageId[] ids = FindAllPageIds(DefaultFolder);
            if (ids.Length == 0)
            {
                if (rectOverride.HasValue)
                {
                    return;
                }

                EditorGUILayout.HelpBox(
                    $"В папке нет PageId ассетов: {DefaultFolder}\nСоздай PageId вручную или сгенерируй через меню: Tools → Neo → Pages → Generate Default PageIds.",
                    MessageType.Warning);
                return;
            }

            string[] labels = new string[ids.Length + 1];
            labels[0] = "<None>";
            for (int i = 0; i < ids.Length; i++)
            {
                labels[i + 1] = ids[i].DisplayName;
            }

            PageId current = prop.objectReferenceValue as PageId;
            int currentIdx = current == null ? 0 : System.Array.FindIndex(ids, x => x == current) + 1;
            if (currentIdx < 0)
            {
                currentIdx = 0;
            }

            int newIdx;
            if (rectOverride.HasValue)
            {
                newIdx = EditorGUI.Popup(rectOverride.Value, currentIdx, labels);
            }
            else
            {
                newIdx = EditorGUILayout.Popup(label, currentIdx, labels);
            }

            if (newIdx == 0)
            {
                prop.objectReferenceValue = null;
            }
            else if (newIdx > 0 && newIdx <= ids.Length)
            {
                prop.objectReferenceValue = ids[newIdx - 1];
            }
        }

        private static PageId[] FindAllPageIds(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:PageId", new[] { folder });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<PageId>)
                .Where(x => x != null)
                .OrderBy(x => x.DisplayName)
                .ToArray();
        }
    }
}