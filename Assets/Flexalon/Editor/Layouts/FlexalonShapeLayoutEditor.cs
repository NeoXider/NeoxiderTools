using UnityEditor;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonShapeLayout)), CanEditMultipleObjects]
    public class FlexalonShapeLayoutEditor : FlexalonComponentEditor
    {
        private SerializedProperty _sides;
        private SerializedProperty _shapeRotationDegrees;
        private SerializedProperty _spacing;
        private SerializedProperty _plane;
        private SerializedProperty _planeAlign;

        [MenuItem("GameObject/Flexalon/Shape Layout")]
        public static void Create(MenuCommand command)
        {
            FlexalonComponentEditor.Create<FlexalonShapeLayout>("Shape Layout", command.context);
        }

        void OnEnable()
        {
            _sides = serializedObject.FindProperty("_sides");
            _shapeRotationDegrees = serializedObject.FindProperty("_shapeRotationDegrees");
            _spacing = serializedObject.FindProperty("_spacing");
            _plane = serializedObject.FindProperty("_plane");
            _planeAlign = serializedObject.FindProperty("_planeAlign");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForceUpdateButton();
            EditorGUILayout.PropertyField(_sides);
            EditorGUILayout.PropertyField(_shapeRotationDegrees);
            EditorGUILayout.PropertyField(_spacing);
            EditorGUILayout.PropertyField(_plane);
            EditorGUILayout.PropertyField(_planeAlign);
            ApplyModifiedProperties();
        }
    }
}