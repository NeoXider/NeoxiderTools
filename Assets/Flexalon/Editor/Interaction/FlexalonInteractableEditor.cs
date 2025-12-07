#if UNITY_PHYSICS

using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonInteractable)), CanEditMultipleObjects]
    public class FlexalonInteractableEditor : UnityEditor.Editor
    {
        private SerializedProperty _clickable;
        private SerializedProperty _maxClickTime;
        private SerializedProperty _maxClickDistance;
        private SerializedProperty _draggable;
        private SerializedProperty _interpolationSpeed;
        private SerializedProperty _insertRadius;
        private SerializedProperty _restriction;
        private SerializedProperty _planeNormal;
        private SerializedProperty _localSpaceRestriction;
        private SerializedProperty _lineDirection;
        private SerializedProperty _holdOffset;
        private SerializedProperty _localSpaceOffset;
        private SerializedProperty _rotateOnDrag;
        private SerializedProperty _holdRotation;
        private SerializedProperty _localSpaceRotation;
        private SerializedProperty _hideCursor;
        private SerializedProperty _setParentWhileDragging;
        private SerializedProperty _handle;
        private SerializedProperty _bounds;
        private SerializedProperty _layerMask;
        private SerializedProperty _clicked;
        private SerializedProperty _hoverStart;
        private SerializedProperty _hoverEnd;
        private SerializedProperty _selectStart;
        private SerializedProperty _selectEnd;
        private SerializedProperty _dragStart;
        private SerializedProperty _dragEnd;

        private static bool _showDragOptions = true;
        private static bool _showEvents = false;

        void OnEnable()
        {
            _clickable = serializedObject.FindProperty("_clickable");
            _maxClickTime = serializedObject.FindProperty("_maxClickTime");
            _maxClickDistance = serializedObject.FindProperty("_maxClickDistance");
            _draggable = serializedObject.FindProperty("_draggable");
            _interpolationSpeed = serializedObject.FindProperty("_interpolationSpeed");
            _insertRadius = serializedObject.FindProperty("_insertRadius");
            _restriction = serializedObject.FindProperty("_restriction");
            _planeNormal = serializedObject.FindProperty("_planeNormal");
            _localSpaceRestriction = serializedObject.FindProperty("_localSpaceRestriction");
            _lineDirection = serializedObject.FindProperty("_lineDirection");
            _holdOffset = serializedObject.FindProperty("_holdOffset");
            _localSpaceOffset = serializedObject.FindProperty("_localSpaceOffset");
            _rotateOnDrag = serializedObject.FindProperty("_rotateOnDrag");
            _holdRotation = serializedObject.FindProperty("_holdRotation");
            _localSpaceRotation = serializedObject.FindProperty("_localSpaceRotation");
            _hideCursor = serializedObject.FindProperty("_hideCursor");
            _setParentWhileDragging = serializedObject.FindProperty("_setParentWhileDragging");
            _handle = serializedObject.FindProperty("_handle");
            _bounds = serializedObject.FindProperty("_bounds");
            _layerMask = serializedObject.FindProperty("_layerMask");
            _clicked = serializedObject.FindProperty("_clicked");
            _hoverStart = serializedObject.FindProperty("_hoverStart");
            _hoverEnd = serializedObject.FindProperty("_hoverEnd");
            _selectStart = serializedObject.FindProperty("_selectStart");
            _selectEnd = serializedObject.FindProperty("_selectEnd");
            _dragStart = serializedObject.FindProperty("_dragStart");
            _dragEnd = serializedObject.FindProperty("_dragEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_clickable);

            if (_clickable.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_maxClickTime);
                EditorGUILayout.PropertyField(_maxClickDistance);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(_draggable);

            if (_draggable.boolValue)
            {
                EditorGUILayout.Space();
                _showDragOptions = EditorGUILayout.Foldout(_showDragOptions, "Drag Options");
                if (_showDragOptions)
                {
                    EditorGUI.indentLevel++;

                    bool showAllOptions = true;
                    foreach (var target in targets)
                    {
                        var interactable = target as FlexalonInteractable;
                        showAllOptions = showAllOptions && interactable.ShowAllDragProperties;
                    }

                    if (showAllOptions)
                    {
                        EditorGUILayout.PropertyField(_interpolationSpeed);
                    }

                    EditorGUILayout.PropertyField(_insertRadius);

                    if (showAllOptions)
                    {
                        var restriction = _restriction;
                        EditorGUILayout.PropertyField(restriction);
                        if (restriction.enumValueIndex == (int)FlexalonInteractable.RestrictionType.Plane)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(_planeNormal, new GUIContent("Normal"));
                            EditorGUILayout.PropertyField(_localSpaceRestriction, new GUIContent("Local Space"));
                            EditorGUI.indentLevel--;
                        }
                        else if (restriction.enumValueIndex == (int)FlexalonInteractable.RestrictionType.Line)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(_lineDirection, new GUIContent("Direction"));
                            EditorGUILayout.PropertyField(_localSpaceRestriction, new GUIContent("Local Space"));
                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.PropertyField(_holdOffset);
                        EditorGUILayout.PropertyField(_localSpaceOffset, new GUIContent("Local Space"));

                        var rotateOnGrab = _rotateOnDrag;
                        EditorGUILayout.PropertyField(rotateOnGrab);
                        if (rotateOnGrab.boolValue)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(_holdRotation, new GUIContent("Rotation"));
                            EditorGUILayout.PropertyField(_localSpaceRotation, new GUIContent("Local Space"));
                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.PropertyField(_handle);
                        EditorGUILayout.PropertyField(_bounds);
                    }
                    else
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.HelpBox("Some drag options are disabled for the selected input provider.", MessageType.Info);
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.PropertyField(_hideCursor);
                    EditorGUILayout.PropertyField(_setParentWhileDragging);
                    EditorGUILayout.PropertyField(_layerMask);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();

            _showEvents = EditorGUILayout.Foldout(_showEvents, "Events");
            if (_showEvents)
            {
                if (_clickable.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_clicked"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_hoverStart"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_hoverEnd"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_selectStart"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_selectEnd"));

                if (_draggable.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_dragStart"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_dragEnd"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif