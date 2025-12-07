using UnityEditor;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonAspectRatioAdapter)), CanEditMultipleObjects]
    public class FlexalonAspectRatioAdapterEditor : FlexalonComponentEditor
    {
        private SerializedProperty _width;
        private SerializedProperty _height;

        void OnEnable()
        {
            _width = serializedObject.FindProperty("_width");
            _height = serializedObject.FindProperty("_height");
        }

        public override void OnInspectorGUI()
        {
            ForceUpdateButton();
            SerializedObject so = serializedObject;
            EditorGUILayout.PropertyField(_width);
            EditorGUILayout.PropertyField(_height);
            ApplyModifiedProperties();
        }
    }
}