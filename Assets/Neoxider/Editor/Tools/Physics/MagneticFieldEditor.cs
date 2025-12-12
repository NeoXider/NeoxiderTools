using Neo.Tools;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tools.Physics
{
    [CustomEditor(typeof(MagneticField))]
    [CanEditMultipleObjects]
    public sealed class MagneticFieldEditor : UnityEditor.Editor
    {
        private SerializedProperty modeProp;
        private SerializedProperty targetPointProp;
        private SerializedProperty directionProp;
        private SerializedProperty directionIsLocalProp;
        private SerializedProperty directionGizmoDistanceProp;

        private void OnEnable()
        {
            modeProp = serializedObject.FindProperty("mode");
            targetPointProp = serializedObject.FindProperty("targetPoint");
            directionProp = serializedObject.FindProperty("direction");
            directionIsLocalProp = serializedObject.FindProperty("directionIsLocal");
            directionGizmoDistanceProp = serializedObject.FindProperty("directionGizmoDistance");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();

            MagneticField field = (MagneticField)target;
            var mode = (MagneticField.FieldMode)modeProp.enumValueIndex;

            switch (mode)
            {
                case MagneticField.FieldMode.ToPoint:
                    DrawTargetPointHandle(field);
                    break;

                case MagneticField.FieldMode.Direction:
                    DrawDirectionHandle(field);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTargetPointHandle(MagneticField field)
        {
            Transform t = field.transform;
            Vector3 current = targetPointProp.vector3Value;

            EditorGUI.BeginChangeCheck();
            Vector3 next = Handles.PositionHandle(current, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                Handles.color = Color.yellow;
                Handles.DrawLine(t.position, current);
                return;
            }

            Undo.RecordObject(field, "Move MagneticField Target Point");
            targetPointProp.vector3Value = next;
        }

        private void DrawDirectionHandle(MagneticField field)
        {
            Transform t = field.transform;

            Vector3 dir = directionProp.vector3Value;
            bool isLocal = directionIsLocalProp.boolValue;
            float dist = Mathf.Max(0.01f, directionGizmoDistanceProp.floatValue);

            Vector3 dirWorld = isLocal ? t.TransformDirection(dir) : dir;
            if (dirWorld.sqrMagnitude < 0.0001f)
            {
                dirWorld = t.forward;
            }

            dirWorld.Normalize();

            Vector3 origin = t.position;
            Vector3 currentEnd = origin + dirWorld * dist;

            EditorGUI.BeginChangeCheck();
            Vector3 nextEnd = Handles.PositionHandle(currentEnd, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                Handles.color = Color.yellow;
                Handles.DrawLine(origin, currentEnd);
                return;
            }

            Vector3 delta = nextEnd - origin;
            float nextDist = delta.magnitude;
            if (nextDist < 0.01f)
            {
                return;
            }

            Vector3 nextDirWorld = delta / nextDist;
            Vector3 nextDir = isLocal ? t.InverseTransformDirection(nextDirWorld) : nextDirWorld;

            Undo.RecordObject(field, "Change MagneticField Direction");
            directionProp.vector3Value = nextDir;
            directionGizmoDistanceProp.floatValue = nextDist;
        }
    }
}

