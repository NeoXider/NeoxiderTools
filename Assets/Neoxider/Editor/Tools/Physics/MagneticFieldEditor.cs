using Neo.Tools;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tools.Physics
{
    [CustomEditor(typeof(MagneticField))]
    [CanEditMultipleObjects]
    public sealed class MagneticFieldEditor : CustomEditorBase
    {
        private void OnSceneGUI()
        {
            // Do not use serializedObject in OnSceneGUI — use a SerializedObject created from target.
            SerializedObject so = new(target);
            so.Update();

            MagneticField field = (MagneticField)target;
            SerializedProperty modePropLocal = so.FindProperty("mode");
            MagneticField.FieldMode mode = (MagneticField.FieldMode)modePropLocal.enumValueIndex;

            switch (mode)
            {
                case MagneticField.FieldMode.ToPoint:
                    DrawTargetPointHandle(field, so);
                    break;

                case MagneticField.FieldMode.Direction:
                    DrawDirectionHandle(field, so);
                    break;
            }

            so.ApplyModifiedProperties();
        }

        protected override void ProcessAttributeAssignments()
        {
            if (target is MonoBehaviour mb)
            {
                ComponentDrawer.ProcessComponentAttributes(mb);
                ResourceDrawer.ProcessResourceAttributes(mb);
            }
        }

        private void DrawTargetPointHandle(MagneticField field, SerializedObject so)
        {
            Transform t = field.transform;
            SerializedProperty targetPointPropLocal = so.FindProperty("targetPoint");
            Vector3 current = targetPointPropLocal.vector3Value;

            EditorGUI.BeginChangeCheck();
            Vector3 next = Handles.PositionHandle(current, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                Handles.color = Color.yellow;
                Handles.DrawLine(t.position, current);
                return;
            }

            Undo.RecordObject(field, "Move MagneticField Target Point");
            targetPointPropLocal.vector3Value = next;
        }

        private void DrawDirectionHandle(MagneticField field, SerializedObject so)
        {
            Transform t = field.transform;

            SerializedProperty directionPropLocal = so.FindProperty("direction");
            SerializedProperty directionIsLocalPropLocal = so.FindProperty("directionIsLocal");
            SerializedProperty directionGizmoDistancePropLocal = so.FindProperty("directionGizmoDistance");

            Vector3 dir = directionPropLocal.vector3Value;
            bool isLocal = directionIsLocalPropLocal.boolValue;
            float dist = Mathf.Max(0.01f, directionGizmoDistancePropLocal.floatValue);

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
            directionPropLocal.vector3Value = nextDir;
            directionGizmoDistancePropLocal.floatValue = nextDist;
        }
    }
}