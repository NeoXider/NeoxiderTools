using Neo.Tools;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    [CustomEditor(typeof(InventoryItemData))]
    public class InventoryItemDataEditor : CustomEditorBase
    {
        private SerializedProperty _iconProp;
        private SerializedProperty _itemIdProp;
        private SerializedProperty _displayNameProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _maxStackProp;
        private SerializedProperty _categoryProp;
        private SerializedProperty _worldDropPrefabProp;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
        }

        private void OnEnable()
        {
            _itemIdProp = serializedObject.FindProperty("_itemId");
            _displayNameProp = serializedObject.FindProperty("_displayName");
            _descriptionProp = serializedObject.FindProperty("_description");
            _iconProp = serializedObject.FindProperty("_icon");
            _maxStackProp = serializedObject.FindProperty("_maxStack");
            _categoryProp = serializedObject.FindProperty("_category");
            _worldDropPrefabProp = serializedObject.FindProperty("_worldDropPrefab");
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            if (_iconProp == null || _worldDropPrefabProp == null || _itemIdProp == null || _displayNameProp == null)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawSummary();
            DrawValidation();

            NeoxiderEditorGUI.BeginSection("Identity", "Ключевые данные предмета, которые используются в UI и runtime storage.");
            EditorGUILayout.PropertyField(_itemIdProp);
            EditorGUILayout.PropertyField(_displayNameProp);
            EditorGUILayout.PropertyField(_descriptionProp);
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Presentation", "Иконка и prefab для world drop / preview icon generation.");
            EditorGUILayout.PropertyField(_iconProp);
            EditorGUILayout.PropertyField(_worldDropPrefabProp);
            DrawIconTools();
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Stacking", "Параметры стакования и простая категоризация.");
            EditorGUILayout.PropertyField(_maxStackProp);
            EditorGUILayout.PropertyField(_categoryProp);
            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary()
        {
            string title = string.IsNullOrWhiteSpace(_displayNameProp.stringValue) ? target.name : _displayNameProp.stringValue;
            string subtitle = $"Item ID: <b>{_itemIdProp.intValue}</b>";

            NeoxiderEditorGUI.DrawSummaryCard(title, subtitle,
                new NeoxiderEditorGUI.Badge(_iconProp.objectReferenceValue != null ? "Icon Ready" : "No Icon",
                    _iconProp.objectReferenceValue != null
                        ? new Color(0.18f, 0.62f, 0.32f, 1f)
                        : new Color(0.40f, 0.40f, 0.44f, 1f)),
                new NeoxiderEditorGUI.Badge(_worldDropPrefabProp.objectReferenceValue != null ? "Prefab Linked" : "No Prefab",
                    _worldDropPrefabProp.objectReferenceValue != null
                        ? new Color(0.20f, 0.50f, 0.78f, 1f)
                        : new Color(0.78f, 0.46f, 0.18f, 1f)),
                new NeoxiderEditorGUI.Badge(
                    _maxStackProp.intValue < 0 ? "Infinite Stack" : $"Max Stack {_maxStackProp.intValue}",
                    new Color(0.42f, 0.34f, 0.82f, 1f)));

            EditorGUILayout.Space(4f);
        }

        private void DrawValidation()
        {
            if (string.IsNullOrWhiteSpace(_displayNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Display Name пустой. Предмету лучше дать читаемое имя для UI и debug.", MessageType.Warning);
            }

            if (_maxStackProp.intValue == 1)
            {
                EditorGUILayout.HelpBox("Max Stack = 1. Предмет будет вести себя как non-stackable.", MessageType.Info);
            }

            if (_iconProp.objectReferenceValue == null && _worldDropPrefabProp.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox("Иконка не задана, но есть World Drop Prefab. Её можно быстро сгенерировать кнопкой ниже.", MessageType.Info);
            }
        }

        private void DrawIconTools()
        {
            EditorGUILayout.HelpBox(
                "Create a Sprite from the World Drop Prefab preview and assign it as this item's icon.",
                MessageType.None);

            EditorGUI.BeginDisabledGroup(_worldDropPrefabProp.objectReferenceValue == null);
            if (GUILayout.Button("Create Icon from World Drop Prefab", GUILayout.Height(24)))
            {
                CreateIconFromPrefab();
            }

            EditorGUI.EndDisabledGroup();

            if (_worldDropPrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign World Drop Prefab above to use this button.", MessageType.Info);
            }
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