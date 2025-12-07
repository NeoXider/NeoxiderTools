using UnityEditor;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonGridCell)), CanEditMultipleObjects]
    public class FlexalonGridCellEditor : FlexalonComponentEditor
    {
        private SerializedProperty _column;
        private SerializedProperty _row;
        private SerializedProperty _layer;

        void OnEnable()
        {
            _column = serializedObject.FindProperty("_column");
            _row = serializedObject.FindProperty("_row");
            _layer = serializedObject.FindProperty("_layer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_column);
            EditorGUILayout.PropertyField(_row);
            EditorGUILayout.PropertyField(_layer);
            ApplyModifiedProperties();
        }
    }
}