using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonGridLayout)), CanEditMultipleObjects]
    public class FlexalonGridLayoutEditor : FlexalonComponentEditor
    {
        private SerializedProperty _cellType;
        private SerializedProperty _columns;
        private SerializedProperty _rows;
        private SerializedProperty _layers;
        private SerializedProperty _columnDirection;
        private SerializedProperty _layerDirection;
        private SerializedProperty _rowDirection;
        private SerializedProperty _rowSizeType;
        private SerializedProperty _rowSize;
        private SerializedProperty _columnSizeType;
        private SerializedProperty _columnSize;
        private SerializedProperty _layerSizeType;
        private SerializedProperty _layerSize;
        private SerializedProperty _columnSpacing;
        private SerializedProperty _rowSpacing;
        private SerializedProperty _layerSpacing;
        private SerializedProperty _horizontalAlign;
        private SerializedProperty _verticalAlign;
        private SerializedProperty _depthAlign;

        private GUIContent _rowSizeLabel;
        private GUIContent _columnSizeLabel;
        private GUIContent _layerSizeLabel;

        [MenuItem("GameObject/Flexalon/Grid Layout")]
        public static void Create(MenuCommand command)
        {
            FlexalonComponentEditor.Create<FlexalonGridLayout>("Grid Layout", command.context);
        }

        void OnEnable()
        {
            _cellType = serializedObject.FindProperty("_cellType");
            _columns = serializedObject.FindProperty("_columns");
            _rows = serializedObject.FindProperty("_rows");
            _layers = serializedObject.FindProperty("_layers");
            _columnDirection = serializedObject.FindProperty("_columnDirection");
            _rowDirection = serializedObject.FindProperty("_rowDirection");
            _layerDirection = serializedObject.FindProperty("_layerDirection");
            _rowSizeType = serializedObject.FindProperty("_rowSizeType");
            _rowSize = serializedObject.FindProperty("_rowSize");
            _columnSizeType = serializedObject.FindProperty("_columnSizeType");
            _columnSize = serializedObject.FindProperty("_columnSize");
            _layerSizeType = serializedObject.FindProperty("_layerSizeType");
            _layerSize = serializedObject.FindProperty("_layerSize");
            _columnSpacing = serializedObject.FindProperty("_columnSpacing");
            _rowSpacing = serializedObject.FindProperty("_rowSpacing");
            _layerSpacing = serializedObject.FindProperty("_layerSpacing");
            _horizontalAlign = serializedObject.FindProperty("_horizontalAlign");
            _verticalAlign = serializedObject.FindProperty("_verticalAlign");
            _depthAlign = serializedObject.FindProperty("_depthAlign");
            _rowSizeLabel = new GUIContent("Row Size");
            _columnSizeLabel = new GUIContent("Column Size");
            _layerSizeLabel = new GUIContent("Layer Size");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForceUpdateButton();

            SerializedObject so = serializedObject;
            EditorGUILayout.PropertyField(_cellType);
            EditorGUILayout.PropertyField(_columns);
            EditorGUILayout.PropertyField(_rows);
            EditorGUILayout.PropertyField(_layers);
            EditorGUILayout.PropertyField(_columnDirection);
            EditorGUILayout.PropertyField(_rowDirection);
            EditorGUILayout.PropertyField(_layerDirection);
            CreateSizeProperty(_columnSizeType, _columnSize, _columnSizeLabel);
            CreateSizeProperty(_rowSizeType, _rowSize, _rowSizeLabel);
            CreateSizeProperty(_layerSizeType, _layerSize, _layerSizeLabel);
            EditorGUILayout.PropertyField(_columnSpacing);
            EditorGUILayout.PropertyField(_rowSpacing);
            EditorGUILayout.PropertyField(_layerSpacing);
            EditorGUILayout.PropertyField(_horizontalAlign);
            EditorGUILayout.PropertyField(_verticalAlign);
            EditorGUILayout.PropertyField(_depthAlign);
            ApplyModifiedProperties();
        }

        private void CreateSizeProperty(SerializedProperty typeProperty, SerializedProperty sizeProperty, GUIContent label)
        {
            EditorGUILayout.BeginHorizontal();
            bool showLabel = true;
            if (typeProperty.enumValueIndex == (int)FlexalonGridLayout.CellSizeTypes.Fixed)
            {
                showLabel = false;
                EditorGUILayout.PropertyField(sizeProperty, label, true);
            }

            EditorGUILayout.PropertyField(typeProperty, showLabel ? label : GUIContent.none, true);
            EditorGUILayout.EndHorizontal();
        }
    }
}