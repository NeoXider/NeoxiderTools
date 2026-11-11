using System.Collections.Generic;
using Neo.Tools;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tools.Physics
{
    [CustomEditor(typeof(MagneticField))]
    [CanEditMultipleObjects]
    public sealed class MagneticFieldEditor : CustomEditorBase
    {
        // WHY: OnSceneGUI runs for every Scene view event and, with several fields selected, once per target.
        // The shared serializedObject spans all targets, so each target keeps its own SerializedObject here
        // instead of a new one being built on every event.
        private readonly Dictionary<Object, SerializedObject> _sceneObjects = new();

        protected override void OnDisable()
        {
            foreach (SerializedObject sceneObject in _sceneObjects.Values)
            {
                sceneObject?.Dispose();
            }

            _sceneObjects.Clear();
            base.OnDisable();
        }

        private SerializedObject GetSceneObject(Object sceneTarget)
        {
            if (_sceneObjects.TryGetValue(sceneTarget, out SerializedObject cached) && cached != null &&
                cached.targetObject == sceneTarget)
            {
                return cached;
            }

            cached?.Dispose();
            SerializedObject created = new(sceneTarget);
            _sceneObjects[sceneTarget] = created;
            return created;
        }

        private void OnSceneGUI()
        {
            if (target == null)
            {
                return;
            }

            SerializedObject so = GetSceneObject(target);
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
