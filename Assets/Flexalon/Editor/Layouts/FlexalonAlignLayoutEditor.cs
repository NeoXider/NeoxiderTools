using UnityEditor;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonAlignLayout)), CanEditMultipleObjects]
    public class FlexalonAlignLayoutEditor : FlexalonComponentEditor
    {
        private SerializedProperty _horizontalAlign;
        private SerializedProperty _verticalAlign;
        private SerializedProperty _depthAlign;
        private SerializedProperty _horizontalPivot;
        private SerializedProperty _verticalPivot;
        private SerializedProperty _depthPivot;

        [MenuItem("GameObject/Flexalon/Align Layout")]
        public static void Create(MenuCommand command)
        {
            FlexalonComponentEditor.Create<FlexalonAlignLayout>("Align Layout", command.context);
        }

        void OnEnable()
        {
            _horizontalAlign = serializedObject.FindProperty("_horizontalAlign");
            _verticalAlign = serializedObject.FindProperty("_verticalAlign");
            _depthAlign = serializedObject.FindProperty("_depthAlign");
            _horizontalPivot = serializedObject.FindProperty("_horizontalPivot");
            _verticalPivot = serializedObject.FindProperty("_verticalPivot");
            _depthPivot = serializedObject.FindProperty("_depthPivot");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForceUpdateButton();
            EditorGUILayout.PropertyField(_horizontalAlign);
            EditorGUILayout.PropertyField(_verticalAlign);
            EditorGUILayout.PropertyField(_depthAlign);
            EditorGUILayout.PropertyField(_horizontalPivot);
            EditorGUILayout.PropertyField(_verticalPivot);
            EditorGUILayout.PropertyField(_depthPivot);
            ApplyModifiedProperties();
        }
    }
}