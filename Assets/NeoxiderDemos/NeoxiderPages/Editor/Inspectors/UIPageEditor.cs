using Neo.Editor;
using Neo.Pages;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(UIPage))]
    [CanEditMultipleObjects]
    public sealed class UIPageEditor : CustomEditorBase
    {
        private const string AnimationHelp =
            "Compatibility setting for older scenes and project extensions. The base page switches instantly; a " +
            "subclass reports its animation timing through HasShowAnimation / ShowAnimationDuration.";

        private static readonly GUIContent PageContent = new("Page");
        private static readonly GUIContent PageAssetContent = new("Page Id (asset)");
        private static readonly GUIContent PageNameContent = new("Page Name");
        private static readonly GUIContent GenerateContent = new("Generate & Assign",
            "Create (or reuse) the PageId asset 'Page<Name>' and assign it.");
        private static readonly GUIContent PopupContent = new("Popup");
        private static readonly GUIContent IgnoreOnExclusiveContent = new("Ignore On Exclusive Change");
        private static readonly GUIContent AnimationModeContent = new("Animation Mode");

        private SerializedProperty animationModeProp;

        private string generateName = "Menu";
        private PageId shownNameSource;
        private string shownName;
        private SerializedProperty ignoreOnExclusiveChangeProp;
        private SerializedProperty pageIdProp;
        private SerializedProperty popupProp;
        private int selectorMode; // WHY: 0 = dropdown, 1 = asset

        protected override bool UseCustomNeoxiderInspectorGUI => true;
        protected override string NeoxiderModuleName => "Neoxider Pages";

        private void OnEnable()
        {
            pageIdProp = serializedObject.FindProperty("pageId");
            popupProp = serializedObject.FindProperty("popup");
            ignoreOnExclusiveChangeProp = serializedObject.FindProperty("ignoreOnExclusiveChange");
            animationModeProp = serializedObject.FindProperty("_animationMode");
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Page", EditorStyles.boldLabel);
            DrawPageIdSelector(pageIdProp);
            DrawPropertyIfAvailable(popupProp, PopupContent);
            DrawPropertyIfAvailable(ignoreOnExclusiveChangeProp, IgnoreOnExclusiveContent);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Anim", EditorStyles.boldLabel);
            DrawPropertyIfAvailable(animationModeProp, AnimationModeContent);
            EditorGUILayout.HelpBox(AnimationHelp, MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        protected override void ProcessAttributeAssignments()
        {
            // WHY: Pages inspectors do not use auto-assign from NeoCustomEditor.
        }

        private void DrawPageIdSelector(SerializedProperty pageId)
        {
            if (pageId == null)
            {
                EditorGUILayout.HelpBox("Serialized field 'pageId' was not found on UIPage.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Page Id", EditorStyles.miniBoldLabel);
            selectorMode = PagesEditorGUI.SegmentedMode(selectorMode, PagesEditorGUI.DropdownOrAsset);
            EditorGUILayout.Space(2);

            if (selectorMode == 0)
            {
                if (PagesEditorGUI.PageIdPopup(pageId, PageContent))
                {
                    EditorGUILayout.LabelField(PagesEditorGUI.SourceLabelAll, EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(pageId, PageAssetContent);
            }

            EditorGUILayout.EndVertical();
            DrawGenerateAndAssign(pageId);
        }

        private static void DrawPropertyIfAvailable(SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                EditorGUILayout.HelpBox("Serialized field for '" + label.text + "' was not found on UIPage.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(property, label);
        }

        private string DisplayNameOf(PageId id)
        {
            // WHY: DisplayName reads Object.name, a new string per call; cache it per assigned PageId.
            if (shownNameSource != id)
            {
                shownNameSource = id;
                shownName = id.DisplayName;
            }

            return shownName;
        }

        private void DrawGenerateAndAssign(SerializedProperty pageId)
        {
            if (pageId.hasMultipleDifferentValues)
            {
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Generate", EditorStyles.miniBoldLabel);
            PageId current = pageId.objectReferenceValue as PageId;
            bool hasPage = current != null;

            using (new EditorGUI.DisabledScope(hasPage))
            {
                // WHY: with a PageId assigned the field shows its name without writing it back every repaint.
                string shown = EditorGUILayout.TextField(PageNameContent, hasPage ? DisplayNameOf(current) : generateName);
                if (!hasPage)
                {
                    generateName = shown;
                }
            }

            using (new EditorGUI.DisabledScope(hasPage || string.IsNullOrWhiteSpace(generateName)))
            {
                if (GUILayout.Button(GenerateContent))
                {
                    string folder = PageIdGenerator.GetPreferredFolder();
                    string path = PageIdGenerator.GetAssetPath(generateName, folder);
                    bool alreadyExists = AssetDatabase.LoadAssetAtPath<PageId>(path) != null;

                    PageId id = PageIdGenerator.GetOrCreate(generateName, folder);
                    if (id != null)
                    {
                        if (alreadyExists)
                        {
                            Debug.LogWarning($"[UIPage] PageId already exists: {path}. Assigned existing.", target);
                        }

                        pageId.objectReferenceValue = id;
                    }
                }
            }
        }
    }
}
