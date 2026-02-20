using UnityEditor;
using UnityEngine;
using Neo.Tools;

namespace Neo.Editor
{
    [CustomEditor(typeof(InventoryItemData))]
    public class InventoryItemDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _iconProp;
        private SerializedProperty _worldDropPrefabProp;

        private void OnEnable()
        {
            _iconProp = serializedObject.FindProperty("_icon");
            _worldDropPrefabProp = serializedObject.FindProperty("_worldDropPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (_iconProp == null || _worldDropPrefabProp == null)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Icon from Prefab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create a Sprite from the World Drop Prefab preview and assign it as this item's icon.",
                MessageType.None);

            GUI.enabled = _worldDropPrefabProp.objectReferenceValue != null;
            if (GUILayout.Button("Create Icon from World Drop Prefab", GUILayout.Height(24)))
            {
                CreateIconFromPrefab();
            }
            GUI.enabled = true;

            if (_worldDropPrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign World Drop Prefab above to use this button.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        private void CreateIconFromPrefab()
        {
            GameObject prefab = _worldDropPrefabProp.objectReferenceValue as GameObject;
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Create Icon", "Assign a World Drop Prefab first.", "OK");
                return;
            }

            string defaultName = prefab.name + "_Icon";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Icon Sprite",
                defaultName,
                "png",
                "Choose where to save the icon");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Sprite sprite = PrefabToSpriteWindow.CreateSpriteFromPreviewAndAssign(prefab, path);
            if (sprite != null)
            {
                _iconProp.objectReferenceValue = sprite;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(sprite);
            }
        }
    }
}
