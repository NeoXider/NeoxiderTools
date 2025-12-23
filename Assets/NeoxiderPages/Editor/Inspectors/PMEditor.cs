using System.Linq;
using Neo.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(PM))]
    public sealed class PMEditor : CustomEditorBase
    {
        private const string DefaultFolder = "Assets/NeoxiderPages/Pages";
        private const float ModeButtonHeight = 22f;

        private SerializedProperty dontDestroyOnLoadProp;
        private SerializedProperty setInstanceOnAwakeProp;

        private SerializedProperty currentUiPageProp;
        private SerializedProperty previousUiPageProp;
        private SerializedProperty allPagesProp;
        private SerializedProperty startupPageProp;
        private SerializedProperty ignoredPageIdsProp;
        private SerializedProperty onPageChangedProp;
        private SerializedProperty refreshPagesInEditorProp;
        private SerializedProperty editorActivePageIdProp;
        private SerializedProperty autoSelectEditorPageProp;
        private SerializedProperty integrateWithGMProp;

        private int startupSelectMode;
        private int ignoredSelectMode; 
        private int editorSelectMode; 

        private ReorderableList ignoredList;

        private void OnEnable()
        {
            dontDestroyOnLoadProp = serializedObject.FindProperty("_dontDestroyOnLoad");
            setInstanceOnAwakeProp = serializedObject.FindProperty("_setInstanceOnAwake");

            currentUiPageProp = serializedObject.FindProperty("currentUiPage");
            previousUiPageProp = serializedObject.FindProperty("previousUiPage");
            allPagesProp = serializedObject.FindProperty("allPages");
            startupPageProp = serializedObject.FindProperty("startupPage");
            ignoredPageIdsProp = serializedObject.FindProperty("ignoredPageIds");
            onPageChangedProp = serializedObject.FindProperty("OnPageChanged");
            refreshPagesInEditorProp = serializedObject.FindProperty("refreshPagesInEditor");
            editorActivePageIdProp = serializedObject.FindProperty("editorActivePageId");
            autoSelectEditorPageProp = serializedObject.FindProperty("autoSelectEditorPage");
            integrateWithGMProp = serializedObject.FindProperty("integrateWithGM");

            ignoredList = new ReorderableList(serializedObject, ignoredPageIdsProp, true, true, true, true);
            ignoredList.drawHeaderCallback =
                rect => EditorGUI.LabelField(rect, "Ignored Pages (do not change active state)");
            ignoredList.drawElementCallback = DrawIgnoredElement;
        }

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            NeoxiderModuleInspectorHeader.Draw(typeof(PMEditor).Assembly, "Neoxider Pages");

            serializedObject.Update();

            DrawSingletonSection();
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (integrateWithGMProp != null)
                {
                    EditorGUILayout.PropertyField(integrateWithGMProp, new GUIContent("Integrate With GM"));
                    EditorGUILayout.Space(4);
                }

                EditorGUILayout.LabelField("Startup Page", EditorStyles.miniBoldLabel);
                DrawStartupSelector();
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Ignored Pages", EditorStyles.miniBoldLabel);
                DrawIgnoredSelectorMode();
                ignoredList.DoLayoutList();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentUiPageProp);
            EditorGUILayout.PropertyField(previousUiPageProp);
            EditorGUILayout.PropertyField(allPagesProp, true);
            EditorGUILayout.PropertyField(onPageChangedProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(refreshPagesInEditorProp);
                EditorGUILayout.PropertyField(autoSelectEditorPageProp);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Editor Active Page", EditorStyles.miniBoldLabel);
                DrawEditorActiveSelector();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void ProcessAttributeAssignments()
        {
            // Pages-инспекторы не используют авто-assign из NeoCustomEditor.
        }

        private void DrawSingletonSection()
        {
            EditorGUILayout.LabelField("Singleton", EditorStyles.boldLabel);

            if (dontDestroyOnLoadProp != null)
            {
                EditorGUILayout.PropertyField(dontDestroyOnLoadProp, new GUIContent("Dont Destroy On Load"));
            }

            if (setInstanceOnAwakeProp != null)
            {
                EditorGUILayout.PropertyField(setInstanceOnAwakeProp, new GUIContent("Set Instance On Awake"));
            }

            PM instance = FindInstanceInScene();
            using (new EditorGUI.DisabledScope(instance == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Select Instance", GUILayout.Height(20)))
                    {
                        Selection.activeObject = instance;
                        EditorGUIUtility.PingObject(instance);
                    }

                    using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                    {
                        if (GUILayout.Button("Destroy (Play Mode)", GUILayout.Height(20)))
                        {
                            PM.DestroyInstance();
                        }
                    }
                }
            }
        }

        private static PM FindInstanceInScene()
        {
            return Object.FindFirstObjectByType<PM>(FindObjectsInactive.Include);
        }

        private void DrawStartupSelector()
        {
            startupSelectMode = DrawSegmentedMode(startupSelectMode,
                new GUIContent("Dropdown", "Выбор из PageId ассетов по папке"),
                new GUIContent("Asset", "Ручной выбор конкретного PageId ассета"));
            EditorGUILayout.Space(2);

            if (startupSelectMode == 0)
            {
                DrawPageIdDropdown(startupPageProp, "Startup Page");
                EditorGUILayout.LabelField($"Источник: {DefaultFolder}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.PropertyField(startupPageProp, new GUIContent("Startup Page (asset)"));
            }
        }

        private void DrawIgnoredSelectorMode()
        {
            ignoredSelectMode = DrawSegmentedMode(ignoredSelectMode,
                new GUIContent("Dropdown", "Выбор из PageId ассетов по папке"),
                new GUIContent("Asset", "Ручной выбор конкретного PageId ассета"));
            EditorGUILayout.Space(2);
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
            editorSelectMode = DrawSegmentedMode(editorSelectMode,
                new GUIContent("Buttons", "Быстрые кнопки по всем PageId ассетам в папке"),
                new GUIContent("Dropdown", "Выбор из списка PageId ассетов в папке"),
                new GUIContent("Asset", "Ручной выбор конкретного PageId ассета"));
            EditorGUILayout.Space(2);

            if (editorSelectMode == 0)
            {
                DrawEditorButtons();
                EditorGUILayout.PropertyField(editorActivePageIdProp, new GUIContent("Editor Active (asset)"));
            }
            else if (editorSelectMode == 1)
            {
                DrawPageIdDropdown(editorActivePageIdProp, "Editor Active Page");
                EditorGUILayout.LabelField($"Источник: {DefaultFolder}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.PropertyField(editorActivePageIdProp, new GUIContent("Editor Active Page"));
            }
        }

        private static int DrawSegmentedMode(int value, GUIContent a, GUIContent b)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int selected = value;

                Color prevBg = GUI.backgroundColor;
                Color prevContent = GUI.contentColor;

                DrawModeButton(0, a, ref selected, EditorStyles.miniButtonLeft);
                DrawModeButton(1, b, ref selected, EditorStyles.miniButtonRight);

                GUI.backgroundColor = prevBg;
                GUI.contentColor = prevContent;

                return selected;
            }
        }

        private static int DrawSegmentedMode(int value, GUIContent a, GUIContent b, GUIContent c)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int selected = value;

                Color prevBg = GUI.backgroundColor;
                Color prevContent = GUI.contentColor;

                DrawModeButton(0, a, ref selected, EditorStyles.miniButtonLeft);
                DrawModeButton(1, b, ref selected, EditorStyles.miniButtonMid);
                DrawModeButton(2, c, ref selected, EditorStyles.miniButtonRight);

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

            string[] labels = PageIdEditorCache.GetLabels(DefaultFolder);

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
            return PageIdEditorCache.GetIds(folder);
        }
    }
}