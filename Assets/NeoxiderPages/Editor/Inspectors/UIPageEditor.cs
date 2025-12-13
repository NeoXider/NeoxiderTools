using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    [CustomEditor(typeof(UIPage))]
    public sealed class UIPageEditor : UnityEditor.Editor
    {
        private const string DefaultFolder = "Assets/NeoxiderPages/Pages";

        private SerializedProperty pageIdProp;
        private SerializedProperty popupProp;
        private SerializedProperty ignoreOnExclusiveChangeProp;
        private SerializedProperty animationProp;
        private SerializedProperty playBackwardProp;
        private SerializedProperty onlyPlayBackwardProp;

        private string generateName = "Menu";
        private int selectorMode; // 0 dropdown, 1 asset

        private void OnEnable()
        {
            pageIdProp = serializedObject.FindProperty("pageId");
            popupProp = serializedObject.FindProperty("popup");
            ignoreOnExclusiveChangeProp = serializedObject.FindProperty("ignoreOnExclusiveChange");
            animationProp = serializedObject.FindProperty("_animation");
            playBackwardProp = serializedObject.FindProperty("_playBackward");
            onlyPlayBackwardProp = serializedObject.FindProperty("_onlyPlayBackward");
        }

        public override void OnInspectorGUI()
        {
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

            DrawGenerateAndAssign(pageId);
        }

        private void DrawGenerateAndAssign(SerializedProperty pageId)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Generate", EditorStyles.miniBoldLabel);
            generateName = EditorGUILayout.TextField("Page Name", generateName);

            if (GUILayout.Button("Generate & Assign"))
            {
                string normalizedName = generateName;
                string assetName = normalizedName.Trim().StartsWith("Page")
                    ? normalizedName.Trim()
                    : "Page" + normalizedName.Trim();
                string path = $"{DefaultFolder}/{assetName}.asset";
                bool alreadyExists = AssetDatabase.LoadAssetAtPath<PageId>(path) != null;

                PageId id = PageIdGenerator.GetOrCreate(normalizedName, DefaultFolder);
                if (id != null)
                {
                    if (alreadyExists)
                    {
                        Debug.LogWarning($"[UIPage] PageId already exists: {path}. Assigned existing.", (UIPage)target);
                    }

                    pageId.objectReferenceValue = id;
                }
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