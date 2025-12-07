using UnityEditor;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonCircleLayout)), CanEditMultipleObjects]
    public class FlexalonCircleLayoutEditor : FlexalonComponentEditor
    {
        private SerializedProperty _plane;
        private SerializedProperty _radius;
        private SerializedProperty _initialRadius;
        private SerializedProperty _spiral;
        private SerializedProperty _spiralSpacing;
        private SerializedProperty _spacingType;
        private SerializedProperty _spacingDegrees;
        private SerializedProperty _radiusType;
        private SerializedProperty _radiusStep;
        private SerializedProperty _startAtDegrees;
        private SerializedProperty _rotate;
        private SerializedProperty _planeAlign;

        [MenuItem("GameObject/Flexalon/Circle Layout")]
        public static void Create(MenuCommand command)
        {
            FlexalonComponentEditor.Create<FlexalonCircleLayout>("Circle Layout", command.context);
        }

        void OnEnable()
        {
            _plane = serializedObject.FindProperty("_plane");
            _radius = serializedObject.FindProperty("_radius");
            _initialRadius = serializedObject.FindProperty("_initialRadius");
            _spiral = serializedObject.FindProperty("_spiral");
            _spiralSpacing = serializedObject.FindProperty("_spiralSpacing");
            _spacingType = serializedObject.FindProperty("_spacingType");
            _spacingDegrees = serializedObject.FindProperty("_spacingDegrees");
            _radiusType = serializedObject.FindProperty("_radiusType");
            _radiusStep = serializedObject.FindProperty("_radiusStep");
            _startAtDegrees = serializedObject.FindProperty("_startAtDegrees");
            _rotate = serializedObject.FindProperty("_rotate");
            _planeAlign = serializedObject.FindProperty("_planeAlign");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForceUpdateButton();

            EditorGUILayout.PropertyField(_plane);

            EditorGUILayout.PropertyField(_initialRadius);
            if (_initialRadius.enumValueIndex == (int)FlexalonCircleLayout.InitialRadiusOptions.Fixed)
            {
                EditorGUILayout.PropertyField(_radius);
            }

            EditorGUILayout.PropertyField(_spiral);

            if (_spiral.boolValue)
            {
                EditorGUILayout.PropertyField(_spiralSpacing);
            }

            EditorGUILayout.PropertyField(_spacingType);

            if (_spacingType.enumValueIndex == (int)FlexalonCircleLayout.SpacingOptions.Fixed)
            {
                EditorGUILayout.PropertyField(_spacingDegrees);
            }

            EditorGUILayout.PropertyField(_radiusType);
            if (_radiusType.enumValueIndex != (int)FlexalonCircleLayout.RadiusOptions.Constant)
            {
                EditorGUILayout.PropertyField(_radiusStep);
            }

            EditorGUILayout.PropertyField(_startAtDegrees);
            EditorGUILayout.PropertyField(_rotate);
            EditorGUILayout.PropertyField(_planeAlign);

            ApplyModifiedProperties();
        }
    }
}