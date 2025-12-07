using UnityEditor;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonFlexibleLayout)), CanEditMultipleObjects]
    public class FlexalonFlexibleLayoutEditor : FlexalonComponentEditor
    {
        private SerializedProperty _direction;
        private SerializedProperty _wrap;
        private SerializedProperty _wrapDirection;
        private SerializedProperty _horizontalAlign;
        private SerializedProperty _verticalAlign;
        private SerializedProperty _depthAlign;
        private SerializedProperty _horizontalInnerAlign;
        private SerializedProperty _verticalInnerAlign;
        private SerializedProperty _depthInnerAlign;
        private SerializedProperty _gapType;
        private SerializedProperty _gap;
        private SerializedProperty _wrapGapType;
        private SerializedProperty _wrapGap;

        [MenuItem("GameObject/Flexalon/Flexible Layout")]
        public static void Create(MenuCommand command)
        {
            FlexalonComponentEditor.Create<FlexalonFlexibleLayout>("Flexible Layout", command.context);
        }

        void OnEnable()
        {
            _direction = serializedObject.FindProperty("_direction");
            _wrap = serializedObject.FindProperty("_wrap");
            _wrapDirection = serializedObject.FindProperty("_wrapDirection");
            _horizontalAlign = serializedObject.FindProperty("_horizontalAlign");
            _verticalAlign = serializedObject.FindProperty("_verticalAlign");
            _depthAlign = serializedObject.FindProperty("_depthAlign");
            _horizontalInnerAlign = serializedObject.FindProperty("_horizontalInnerAlign");
            _verticalInnerAlign = serializedObject.FindProperty("_verticalInnerAlign");
            _depthInnerAlign = serializedObject.FindProperty("_depthInnerAlign");
            _gapType = serializedObject.FindProperty("_gapType");
            _gap = serializedObject.FindProperty("_gap");
            _wrapGapType = serializedObject.FindProperty("_wrapGapType");
            _wrapGap = serializedObject.FindProperty("_wrapGap");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForceUpdateButton();
            SerializedObject so = serializedObject;
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_wrap);

            if ((target as FlexalonFlexibleLayout).Wrap)
            {
                EditorGUILayout.PropertyField(_wrapDirection);
            }

            EditorGUILayout.PropertyField(_horizontalAlign);
            EditorGUILayout.PropertyField(_verticalAlign);
            EditorGUILayout.PropertyField(_depthAlign);
            EditorGUILayout.PropertyField(_horizontalInnerAlign);
            EditorGUILayout.PropertyField(_verticalInnerAlign);
            EditorGUILayout.PropertyField(_depthInnerAlign);
            EditorGUILayout.PropertyField(_gapType);

            if (_gapType.intValue == (int)FlexalonFlexibleLayout.GapOptions.Fixed)
            {
                EditorGUILayout.PropertyField(_gap);
            }

            if ((target as FlexalonFlexibleLayout).Wrap)
            {
                EditorGUILayout.PropertyField(_wrapGapType);
                if (_wrapGapType.intValue == (int)FlexalonFlexibleLayout.GapOptions.Fixed)
                {
                    EditorGUILayout.PropertyField(_wrapGap);
                }
            }

            ApplyModifiedProperties();
        }
    }
}