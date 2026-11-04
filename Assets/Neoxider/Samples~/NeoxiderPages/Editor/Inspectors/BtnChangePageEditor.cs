using Neo.Editor;
using Neo.Pages;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(BtnChangePage))]
    [CanEditMultipleObjects]
    public sealed class BtnChangePageEditor : CustomEditorBase
    {
        private static readonly GUIContent InteractableContent = new("Interactable",
            "When off, clicks and the press animation are ignored.");
        private static readonly GUIContent PageContent = new("Page");
        private static readonly GUIContent PageAssetContent = new("Page Id (asset)");
        private static readonly GUIContent UseOpenPageContent = new("Use Open Page",
            "Replace the legacy value with Open Page; the button keeps opening its Target Page.");

        private SerializedProperty actionProp;
        private string legacyActionMessage;
        private int legacyActionMessageValue = int.MinValue;
        private SerializedProperty canSwitchProp;

        private SerializedProperty changeTextProp;
        private SerializedProperty executeStateProp;
        private SerializedProperty imageTargetProp;

        private SerializedProperty intecactableProp;

        private SerializedProperty onClickProp;
        private SerializedProperty scaleAnimProp;

        private int selectorMode;
        private SerializedProperty targetPageIdProp;
        private SerializedProperty textPageProp;
        private SerializedProperty timeAnimImageProp;

        private SerializedProperty useAnimImageProp;

        protected override bool UseCustomNeoxiderInspectorGUI => true;
        protected override string NeoxiderModuleName => "Neoxider Pages";

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

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(intecactableProp, InteractableContent);
            EditorGUILayout.PropertyField(imageTargetProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Page Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(actionProp);

            // WHY: intValue, not enumValueIndex: an out-of-range value from an old scene must not throw or hide
            // the field; it shows as a warning so the author picks a valid action.
            int actionValue = actionProp.intValue;
            bool singleAction = !actionProp.hasMultipleDifferentValues;
            bool legacyAction = singleAction && BtnChangePage.IsLegacyAction((BtnChangePage.Action)actionValue);
            if (legacyAction)
            {
                EditorGUILayout.HelpBox(GetLegacyActionMessage(actionValue), MessageType.Warning);
                if (GUILayout.Button(UseOpenPageContent))
                {
                    actionProp.intValue = (int)BtnChangePage.Action.OpenPage;
                }
            }

            if (singleAction && (legacyAction || actionValue == (int)BtnChangePage.Action.OpenPage))
            {
                DrawPageIdSelector(targetPageIdProp);
            }

            EditorGUILayout.PropertyField(canSwitchProp);
            EditorGUILayout.PropertyField(executeStateProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useAnimImageProp);
            if (useAnimImageProp.boolValue || useAnimImageProp.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(timeAnimImageProp);
                EditorGUILayout.PropertyField(scaleAnimProp);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("SetText", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(changeTextProp);
            if (changeTextProp.boolValue || changeTextProp.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(textPageProp);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(onClickProp);

            serializedObject.ApplyModifiedProperties();
        }

        protected override void ProcessAttributeAssignments()
        {
            // WHY: Pages inspectors do not use NeoCustomEditor auto-assign.
        }

        private string GetLegacyActionMessage(int actionValue)
        {
            // WHY: cached per value so a misconfigured button does not allocate a message on every repaint.
            if (legacyActionMessage == null || legacyActionMessageValue != actionValue)
            {
                legacyActionMessageValue = actionValue;
                legacyActionMessage = "Legacy action value " + actionValue.ToString() +
                                      " (old UIKit.Page enum): the button opens its Target Page. Pick an Action.";
            }

            return legacyActionMessage;
        }

        private void DrawPageIdSelector(SerializedProperty pageId)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Target Page", EditorStyles.miniBoldLabel);
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
        }
    }
}
