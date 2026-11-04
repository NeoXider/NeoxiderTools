using Neo.Editor;
using Neo.Pages;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(PM))]
    public sealed class PMEditor : CustomEditorBase
    {
        private const float PageButtonWidth = 110f;
        private const float PageButtonHeight = 22f;

        private static readonly GUIContent IntegrateWithGMContent = new("Integrate With GM",
            "Open PageWin/PageLose/PageEnd when GM reports Win, Lose or End.");
        private static readonly GUIContent ClosePopupsContent = new("Close Popups On Exclusive Change",
            "Opening a non-popup page closes active popup pages.");
        private static readonly GUIContent DontDestroyOnLoadContent = new("Dont Destroy On Load");
        private static readonly GUIContent SetInstanceOnAwakeContent = new("Set Instance On Awake");
        private static readonly GUIContent SelectInstanceContent = new("Select Instance", "Select and ping the PM singleton.");
        private static readonly GUIContent DestroyInstanceContent = new("Destroy (Play Mode)");
        private static readonly GUIContent StartupPageContent = new("Startup Page");
        private static readonly GUIContent StartupPageAssetContent = new("Startup Page (asset)");
        private static readonly GUIContent EditorActivePageContent = new("Editor Active Page");
        private static readonly GUIContent EditorActiveAssetContent = new("Editor Active (asset)");
        private static readonly GUIContent DebugLogsContent = new("Debug Logs",
            "Log page switches and setup warnings of this PM. Errors log unless NeoDiagnostics errors are off.");
        private static readonly GUILayoutOption[] SmallButtonOptions = { GUILayout.Height(20f) };
        private static readonly GUILayoutOption[] PageButtonOptions =
        {
            GUILayout.Width(PageButtonWidth), GUILayout.Height(PageButtonHeight), GUILayout.ExpandWidth(false)
        };

        private SerializedProperty allPagesProp;
        private SerializedProperty autoSelectEditorPageProp;
        private SerializedProperty closePopupsProp;

        private SerializedProperty currentUiPageProp;
        private SerializedProperty debugLogsProp;

        private SerializedProperty dontDestroyOnLoadProp;
        private SerializedProperty editorActivePageIdProp;
        private int editorSelectMode;

        private ReorderableList ignoredList;
        private SerializedProperty ignoredPageIdsProp;
        private int ignoredSelectMode;
        private SerializedProperty integrateWithGMProp;
        private SerializedProperty onPageChangedProp;
        private SerializedProperty previousUiPageProp;
        private SerializedProperty refreshPagesInEditorProp;
        private SerializedProperty setInstanceOnAwakeProp;
        private SerializedProperty startupPageProp;

        private int startupSelectMode;

        protected override bool UseCustomNeoxiderInspectorGUI => true;
        protected override string NeoxiderModuleName => "Neoxider Pages";

        private void OnEnable()
        {
            dontDestroyOnLoadProp = serializedObject.FindProperty("_dontDestroyOnLoad");
            setInstanceOnAwakeProp = serializedObject.FindProperty("_setInstanceOnAwake");

            currentUiPageProp = serializedObject.FindProperty("currentUiPage");
            previousUiPageProp = serializedObject.FindProperty("previousUiPage");
            allPagesProp = serializedObject.FindProperty("allPages");
            startupPageProp = serializedObject.FindProperty("startupPage");
            ignoredPageIdsProp = serializedObject.FindProperty("ignoredPageIds");
            closePopupsProp = serializedObject.FindProperty("closePopupsOnExclusivePageChange");
            onPageChangedProp = serializedObject.FindProperty("OnPageChanged");
            refreshPagesInEditorProp = serializedObject.FindProperty("refreshPagesInEditor");
            editorActivePageIdProp = serializedObject.FindProperty("editorActivePageId");
            autoSelectEditorPageProp = serializedObject.FindProperty("autoSelectEditorPage");
            integrateWithGMProp = serializedObject.FindProperty("integrateWithGM");
            debugLogsProp = serializedObject.FindProperty("debugLogs");

            ignoredList = new ReorderableList(serializedObject, ignoredPageIdsProp, true, true, true, true);
            ignoredList.drawHeaderCallback = DrawIgnoredHeader;
            ignoredList.drawElementCallback = DrawIgnoredElement;
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            DrawSingletonSection();
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawOptionalProperty(integrateWithGMProp, IntegrateWithGMContent);
            DrawOptionalProperty(closePopupsProp, ClosePopupsContent);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Startup Page", EditorStyles.miniBoldLabel);
            DrawStartupSelector();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Ignored Pages", EditorStyles.miniBoldLabel);
            ignoredSelectMode = PagesEditorGUI.SegmentedMode(ignoredSelectMode, PagesEditorGUI.DropdownOrAsset);
            EditorGUILayout.Space(2);
            ignoredList.DoLayoutList();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            DrawOptionalProperty(debugLogsProp, DebugLogsContent);
            EditorGUILayout.PropertyField(currentUiPageProp);
            EditorGUILayout.PropertyField(previousUiPageProp);
            EditorGUILayout.PropertyField(allPagesProp, true);
            EditorGUILayout.PropertyField(onPageChangedProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(refreshPagesInEditorProp);
            EditorGUILayout.PropertyField(autoSelectEditorPageProp);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Editor Active Page", EditorStyles.miniBoldLabel);
            DrawEditorActiveSelector();
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void ProcessAttributeAssignments()
        {
            // WHY: Pages inspectors do not use auto-assign from NeoCustomEditor.
        }

        private static void DrawOptionalProperty(SerializedProperty property, GUIContent label)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, label);
            }
        }

        private void DrawSingletonSection()
        {
            EditorGUILayout.LabelField("Singleton", EditorStyles.boldLabel);
            DrawOptionalProperty(dontDestroyOnLoadProp, DontDestroyOnLoadContent);
            DrawOptionalProperty(setInstanceOnAwakeProp, SetInstanceOnAwakeContent);

            EditorGUILayout.BeginHorizontal();
            // WHY: the instance is resolved on click only; a scene search per repaint made the inspector slow.
            bool selectInstance = GUILayout.Button(SelectInstanceContent, SmallButtonOptions);
            bool destroyInstance;
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                destroyInstance = GUILayout.Button(DestroyInstanceContent, SmallButtonOptions);
            }

            EditorGUILayout.EndHorizontal();

            if (selectInstance)
            {
                PM instance = FindInstanceInScene();
                if (instance != null)
                {
                    Selection.activeObject = instance;
                    EditorGUIUtility.PingObject(instance);
                }
            }

            if (destroyInstance)
            {
                PM.DestroyInstance();
            }
        }

        private static PM FindInstanceInScene()
        {
            if (PM.TryGetInstance(out PM instance))
            {
                return instance;
            }

            return FindAnyObjectByType<PM>(FindObjectsInactive.Include);
        }

        private void DrawStartupSelector()
        {
            startupSelectMode = PagesEditorGUI.SegmentedMode(startupSelectMode, PagesEditorGUI.DropdownOrAsset);
            EditorGUILayout.Space(2);

            if (startupSelectMode == 0)
            {
                if (PagesEditorGUI.PageIdPopup(startupPageProp, StartupPageContent))
                {
                    EditorGUILayout.LabelField(PagesEditorGUI.SourceLabelAll, EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(startupPageProp, StartupPageAssetContent);
            }
        }

        private static void DrawIgnoredHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Ignored Pages (do not change active state)");
        }

        private void DrawIgnoredElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = ignoredPageIdsProp.GetArrayElementAtIndex(index);
            rect.y += 1;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (ignoredSelectMode == 0)
            {
                PagesEditorGUI.PageIdPopup(rect, element);
            }
            else
            {
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            }
        }

        private void DrawEditorActiveSelector()
        {
            editorSelectMode = PagesEditorGUI.SegmentedMode(editorSelectMode, PagesEditorGUI.ButtonsDropdownOrAsset);
            EditorGUILayout.Space(2);

            if (editorSelectMode == 0)
            {
                DrawEditorButtons();
                EditorGUILayout.PropertyField(editorActivePageIdProp, EditorActiveAssetContent);
            }
            else if (editorSelectMode == 1)
            {
                if (PagesEditorGUI.PageIdPopup(editorActivePageIdProp, EditorActivePageContent))
                {
                    EditorGUILayout.LabelField(PagesEditorGUI.SourceLabelAll, EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(editorActivePageIdProp, EditorActivePageContent);
            }
        }

        private void DrawEditorButtons()
        {
            PageId[] ids = PageIdEditorCache.GetIds();
            if (ids.Length == 0)
            {
                EditorGUILayout.HelpBox(PagesEditorGUI.NoPageIdsMessage, MessageType.Warning);
                return;
            }

            GUIContent[] labels = PageIdEditorCache.GetLabelContents();
            int columns = Mathf.Clamp(Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 40) / PageButtonWidth), 2, 6);
            int i = 0;

            while (i < ids.Length)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns && i < ids.Length; c++, i++)
                {
                    PageId id = ids[i];
                    using (new EditorGUI.DisabledScope(id == null))
                    {
                        if (GUILayout.Button(labels[i + 1], PageButtonOptions))
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

            // WHY: shows the page now instead of waiting for OnValidate's deferred refresh, through the same
            // event-free path: an edit-time preview must not raise OnPageChanged.
            pm.PreviewPageInEditor(id);
        }
    }
}
