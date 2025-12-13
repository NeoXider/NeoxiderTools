using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(BtnChangePage))]
    public sealed class BtnChangePageEditor : UnityEditor.Editor
    {
        private const string DefaultFolder = "Assets/NeoxiderPages/Pages";

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

        public override void OnInspectorGUI()
        {
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

        private void DrawPageIdSelector(SerializedProperty pageId)
        {
            selectorMode = GUILayout.Toolbar(selectorMode, new[] { "Dropdown", "Asset" });

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

                string[] labels = new string[ids.Length + 1];
                labels[0] = "<None>";
                for (int i = 0; i < ids.Length; i++)
                {
                    labels[i + 1] = ids[i].DisplayName;
                }

                PageId current = pageId.objectReferenceValue as PageId;
                int currentIdx = current == null ? 0 : System.Array.FindIndex(ids, x => x == current) + 1;
                if (currentIdx < 0)
                {
                    currentIdx = 0;
                }

                int newIdx = EditorGUILayout.Popup("Page", currentIdx, labels);
                if (newIdx == 0)
                {
                    pageId.objectReferenceValue = null;
                }
                else if (newIdx > 0 && newIdx <= ids.Length)
                {
                    pageId.objectReferenceValue = ids[newIdx - 1];
                }

                EditorGUILayout.HelpBox($"Источник: {DefaultFolder}", MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(pageId, new GUIContent("Page Id (asset)"));
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