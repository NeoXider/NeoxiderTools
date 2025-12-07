using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonRandomModifier)), CanEditMultipleObjects]
    public class FlexalonRandomModifierEditor : FlexalonComponentEditor
    {
        private SerializedProperty _randomSeed;
        private SerializedProperty _randomizePositionX;
        private SerializedProperty _positionMinX;
        private SerializedProperty _positionMaxX;
        private SerializedProperty _horizontalAlign;
        private SerializedProperty _randomizePositionY;
        private SerializedProperty _positionMinY;
        private SerializedProperty _positionMaxY;
        private SerializedProperty _verticalAlign;
        private SerializedProperty _randomizePositionZ;
        private SerializedProperty _positionMinZ;
        private SerializedProperty _positionMaxZ;
        private SerializedProperty _depthAlign;
        private SerializedProperty _randomizeRotationX;
        private SerializedProperty _rotationMinX;
        private SerializedProperty _rotationMaxX;
        private SerializedProperty _randomizeRotationY;
        private SerializedProperty _rotationMinY;
        private SerializedProperty _rotationMaxY;
        private SerializedProperty _randomizeRotationZ;
        private SerializedProperty _rotationMinZ;
        private SerializedProperty _rotationMaxZ;

        private static bool _showPosition = true;
        private static bool _showRotation = true;

        private static GUIContent _xLabel = new GUIContent("X");
        private static GUIContent _yLabel = new GUIContent("Y");
        private static GUIContent _zLabel = new GUIContent("Z");
        private static GUIContent _minLabel = new GUIContent("Min");
        private static GUIContent _maxLabel = new GUIContent("Max");

        private void CreateItem(GUIContent label, SerializedProperty enableProperty, SerializedProperty minProperty, SerializedProperty maxProperty)
        {
            EditorGUILayout.BeginHorizontal();
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 30.0f;
            EditorGUILayout.PropertyField(enableProperty, label, new GUILayoutOption[] { GUILayout.Width(40.0f) });
            if (enableProperty.boolValue)
            {
                EditorGUIUtility.labelWidth = 50.0f;
                EditorGUILayout.PropertyField(minProperty, _minLabel);
                EditorGUILayout.PropertyField(maxProperty, _maxLabel);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = labelWidth;
        }

        void OnEnable()
        {
            _randomSeed = serializedObject.FindProperty("_randomSeed");
            _randomizePositionX = serializedObject.FindProperty("_randomizePositionX");
            _positionMinX = serializedObject.FindProperty("_positionMinX");
            _positionMaxX = serializedObject.FindProperty("_positionMaxX");
            _horizontalAlign = serializedObject.FindProperty("_horizontalAlign");
            _randomizePositionY = serializedObject.FindProperty("_randomizePositionY");
            _positionMinY = serializedObject.FindProperty("_positionMinY");
            _positionMaxY = serializedObject.FindProperty("_positionMaxY");
            _verticalAlign = serializedObject.FindProperty("_verticalAlign");
            _randomizePositionZ = serializedObject.FindProperty("_randomizePositionZ");
            _positionMinZ = serializedObject.FindProperty("_positionMinZ");
            _positionMaxZ = serializedObject.FindProperty("_positionMaxZ");
            _depthAlign = serializedObject.FindProperty("_depthAlign");
            _randomizeRotationX = serializedObject.FindProperty("_randomizeRotationX");
            _rotationMinX = serializedObject.FindProperty("_rotationMinX");
            _rotationMaxX = serializedObject.FindProperty("_rotationMaxX");
            _randomizeRotationY = serializedObject.FindProperty("_randomizeRotationY");
            _rotationMinY = serializedObject.FindProperty("_rotationMinY");
            _rotationMaxY = serializedObject.FindProperty("_rotationMaxY");
            _randomizeRotationZ = serializedObject.FindProperty("_randomizeRotationZ");
            _rotationMinZ = serializedObject.FindProperty("_rotationMinZ");
            _rotationMaxZ = serializedObject.FindProperty("_rotationMaxZ");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForceUpdateButton();

            EditorGUILayout.PropertyField(_randomSeed);
            _showPosition = EditorGUILayout.Foldout(_showPosition, "Position");
            if (_showPosition)
            {
                EditorGUI.indentLevel++;
                CreateItem(_xLabel, _randomizePositionX, _positionMinX, _positionMaxX);
                CreateItem(_yLabel, _randomizePositionY, _positionMinY, _positionMaxY);
                CreateItem(_zLabel, _randomizePositionZ, _positionMinZ, _positionMaxZ);
                EditorGUI.indentLevel--;
            }

            _showRotation = EditorGUILayout.Foldout(_showRotation, "Rotation");
            if (_showRotation)
            {
                EditorGUI.indentLevel++;
                CreateItem(_xLabel, _randomizeRotationX, _rotationMinX, _rotationMaxX);
                CreateItem(_yLabel, _randomizeRotationY, _rotationMinY, _rotationMaxY);
                CreateItem(_zLabel, _randomizeRotationZ, _rotationMinZ, _rotationMaxZ);
                EditorGUI.indentLevel--;
            }

            ApplyModifiedProperties();
        }
    }
}