using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.UI.Editor
{
    [InitializeOnLoad]
    internal static class UIMeshRigMotionPreviewDriver
    {
        private static double _lastTime;

        static UIMeshRigMotionPreviewDriver()
        {
            _lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Clamp((float)(now - _lastTime), 0f, 0.1f);
            _lastTime = now;
            if (EditorApplication.isPlayingOrWillChangePlaymode || deltaTime <= 0f)
            {
                return;
            }

            UIMeshRigPointMotion[] motions = Resources.FindObjectsOfTypeAll<UIMeshRigPointMotion>();
            bool previewed = false;
            for (int index = 0; index < motions.Length; index++)
            {
                UIMeshRigPointMotion motion = motions[index];
                if (motion == null || !motion.isActiveAndEnabled || !motion.PreviewInEditMode ||
                    !motion.IsPlaying || motion.IsPaused || EditorUtility.IsPersistent(motion))
                {
                    continue;
                }

                motion.SetTime(motion.CurrentTime + deltaTime);
                previewed = true;
            }

            if (previewed)
            {
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }

    [CustomEditor(typeof(UIMeshRigGraphic))]
    public sealed class UIMeshRigGraphicEditor : UnityEditor.Editor
    {
        private UIMeshRigPoint _selectedPoint;

        public override void OnInspectorGUI()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)target;
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "_authoringMode", "_sceneTool");
            if (serializedObject.ApplyModifiedProperties())
            {
                UIMeshRigEditorUtility.MarkChanged(rig);
            }

            UIMeshRigInspector.DrawDiagnostics(rig);
            UIMeshRigInspector.DrawAuthoringControls(rig);
            DrawPointList(rig);
        }

        private void OnSceneGUI()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)target;
            _selectedPoint = UIMeshRigSceneHandles.Draw(rig, _selectedPoint);
        }

        private void DrawPointList(UIMeshRigGraphic rig)
        {
            IReadOnlyList<UIMeshRigPoint> points = rig.Points;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Points (" + points.Count + ")", EditorStyles.boldLabel);

            for (int index = 0; index < points.Count; index++)
            {
                UIMeshRigPoint point = points[index];
                EditorGUILayout.BeginHorizontal();
                bool isSelected = point == _selectedPoint;
                if (GUILayout.Toggle(isSelected, point.name, "Button"))
                {
                    _selectedPoint = point;
                }

                if (GUILayout.Button("Focus", GUILayout.Width(48f)))
                {
                    _selectedPoint = point;
                    UIMeshRigEditorUtility.Focus(point);
                }

                if (GUILayout.Button("Dup", GUILayout.Width(38f)))
                {
                    _selectedPoint = UIMeshRigEditorUtility.DuplicatePoint(rig, point);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    UIMeshRigEditorUtility.DeletePoint(rig, point);
                    _selectedPoint = null;
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    [CustomEditor(typeof(UIMeshRigPoint))]
    public sealed class UIMeshRigPointEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UIMeshRigPoint point = (UIMeshRigPoint)target;
            UIMeshRigGraphic rig = point.GetComponentInParent<UIMeshRigGraphic>();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            if (serializedObject.ApplyModifiedProperties())
            {
                if (rig != null)
                {
                    rig.NotifyPointChanged();
                    UIMeshRigEditorUtility.MarkChanged(rig, point);
                }
                else
                {
                    UIMeshRigEditorUtility.MarkChanged(point);
                }
            }

            if (rig == null)
            {
                EditorGUILayout.HelpBox("This point must be a child of a UI Mesh Rig Graphic.", MessageType.Error);
                return;
            }

            UIMeshRigInspector.DrawAuthoringControls(rig);
            DrawPointActions(rig, point);
            UIMeshRigMotionInspector.Draw(point);
            if (UIMeshRigMotionInspector.IsPreviewing(point))
            {
                SceneView.RepaintAll();
            }
        }

        public override bool RequiresConstantRepaint()
        {
            UIMeshRigPoint point = target as UIMeshRigPoint;
            return point != null && UIMeshRigMotionInspector.IsPreviewing(point);
        }

        private void OnSceneGUI()
        {
            UIMeshRigPoint point = (UIMeshRigPoint)target;
            UIMeshRigGraphic rig = point.GetComponentInParent<UIMeshRigGraphic>();
            if (rig != null)
            {
                UIMeshRigSceneHandles.Draw(rig, point);
            }
        }

        private static void DrawPointActions(UIMeshRigGraphic rig, UIMeshRigPoint point)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Point Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus"))
            {
                UIMeshRigEditorUtility.Focus(point);
            }

            if (GUILayout.Button("Duplicate"))
            {
                UIMeshRigPoint duplicate = UIMeshRigEditorUtility.DuplicatePoint(rig, point);
                UIMeshRigEditorUtility.Focus(duplicate);
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Delete"))
            {
                Selection.activeGameObject = rig.gameObject;
                UIMeshRigEditorUtility.DeletePoint(rig, point);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    internal static class UIMeshRigInspector
    {
        public static void DrawDiagnostics(UIMeshRigGraphic rig)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);
            int vertexCount = (rig.Columns + 1) * (rig.Rows + 1);
            int triangleCount = rig.Columns * rig.Rows * 2;
            EditorGUILayout.LabelField("Generated geometry", vertexCount + " vertices / " + triangleCount + " triangles");

            if (rig.Sprite == null)
            {
                EditorGUILayout.HelpBox("Assign a Source Sprite. The rig cannot render without one.", MessageType.Warning);
            }

            if (rig.Columns < 4 || rig.Rows < 4)
            {
                EditorGUILayout.HelpBox("A grid below 4 x 4 usually cannot deform smoothly.", MessageType.Warning);
            }
            else if (vertexCount > 1200)
            {
                EditorGUILayout.HelpBox("This is a dense UI mesh. Reduce the grid unless the silhouette needs this detail.", MessageType.Info);
            }

            HashSet<string> bindingKeys = new HashSet<string>();
            IReadOnlyList<UIMeshRigPoint> points = rig.Points;
            for (int index = 0; index < points.Count; index++)
            {
                string bindingKey = points[index].BindingKey;
                if (!bindingKeys.Add(bindingKey))
                {
                    EditorGUILayout.HelpBox(
                        "Duplicate Binding Key: " + bindingKey + ". Give every point a stable unique key.",
                        MessageType.Warning);
                    break;
                }
            }
        }

        public static void DrawAuthoringControls(UIMeshRigGraphic rig)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authoring", EditorStyles.boldLabel);
            UIMeshRigAuthoringMode requestedMode = (UIMeshRigAuthoringMode)GUILayout.Toolbar(
                (int)rig.AuthoringMode,
                new[] { "Setup", "Pose / Animate" });
            if (requestedMode != rig.AuthoringMode)
            {
                UIMeshRigEditorUtility.RecordRig(rig, "Change UI mesh rig mode");
                rig.SetAuthoringMode(requestedMode);
                UIMeshRigEditorUtility.MarkRigAndPointsChanged(rig);
            }

            if (rig.AuthoringMode == UIMeshRigAuthoringMode.Pose)
            {
                UIMeshRigSceneTool requestedTool = (UIMeshRigSceneTool)GUILayout.Toolbar(
                    (int)rig.SceneTool,
                    new[] { "Move", "Rotate", "Scale" });
                if (requestedTool != rig.SceneTool)
                {
                    Undo.RecordObject(rig, "Change UI mesh rig scene tool");
                    rig.SetSceneTool(requestedTool);
                    UIMeshRigEditorUtility.MarkChanged(rig);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Point"))
            {
                UIMeshRigPoint point = UIMeshRigEditorUtility.CreatePoint(rig);
                UIMeshRigEditorUtility.Focus(point);
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Capture Rest Pose"))
            {
                UIMeshRigEditorUtility.RecordRig(rig, "Capture UI mesh rig rest pose");
                rig.CaptureRestPose();
                UIMeshRigEditorUtility.MarkRigAndPointsChanged(rig);
            }

            if (GUILayout.Button("Reset Pose"))
            {
                UIMeshRigEditorUtility.RecordRig(rig, "Reset UI mesh rig pose");
                rig.ResetPose();
                UIMeshRigEditorUtility.MarkRigAndPointsChanged(rig);
            }
            EditorGUILayout.EndHorizontal();

            string help = rig.AuthoringMode == UIMeshRigAuthoringMode.Setup
                ? "SETUP edits bind centers, influence ellipses and falloff. Select either the rig or a point; Scene handles remain available."
                : "POSE edits the point RectTransforms. Position, rotation and scale are Animator-recordable and can also be saved as a static deformation.";
            EditorGUILayout.HelpBox(help, MessageType.Info);
        }
    }

    internal static class UIMeshRigSceneHandles
    {
        private const int EllipseSegments = 64;

        public static UIMeshRigPoint Draw(UIMeshRigGraphic rig, UIMeshRigPoint selectedPoint)
        {
            IReadOnlyList<UIMeshRigPoint> points = rig.Points;
            if (selectedPoint == null && Selection.activeGameObject != null)
            {
                UIMeshRigPoint selectedComponent = Selection.activeGameObject.GetComponent<UIMeshRigPoint>();
                if (ContainsPoint(points, selectedComponent))
                {
                    selectedPoint = selectedComponent;
                }
            }

            for (int index = 0; index < points.Count; index++)
            {
                UIMeshRigPoint point = points[index];
                bool selected = point == selectedPoint;
                Color color = selected ? new Color(1f, 0.45f, 0.1f) : Color.cyan;
                DrawInfluence(rig, point, color, selected);

                Vector3 center = point.transform.position;
                float pickSize = HandleUtility.GetHandleSize(center) * 0.055f;
                Handles.color = color;
                if (Handles.Button(center, point.transform.rotation, pickSize, pickSize, Handles.CircleHandleCap))
                {
                    selectedPoint = point;
                    Selection.activeGameObject = point.gameObject;
                }
            }

            if (!ContainsPoint(points, selectedPoint))
            {
                return null;
            }

            if (rig.AuthoringMode == UIMeshRigAuthoringMode.Setup)
            {
                DrawSetupHandles(rig, selectedPoint);
            }
            else
            {
                DrawPoseHandle(rig, selectedPoint);
            }

            return selectedPoint;
        }

        private static void DrawInfluence(UIMeshRigGraphic rig, UIMeshRigPoint point, Color color, bool selected)
        {
            Vector2 center = point.RestCenterNormalized;
            Vector2 radius = point.RadiusNormalized;
            DrawEllipse(rig, center, radius, color, selected ? 3f : 1.5f);
            Vector2 innerRadius = radius * (1f - point.Falloff);
            Color innerColor = new Color(color.r, color.g, color.b, 0.45f);
            DrawEllipse(rig, center, innerRadius, innerColor, 1f);
            Handles.color = color;
            Handles.Label(rig.NormalizedToWorld(center), point.name);
        }

        private static void DrawEllipse(UIMeshRigGraphic rig, Vector2 center, Vector2 radius, Color color, float width)
        {
            Vector3[] ellipsePoints = new Vector3[EllipseSegments + 1];
            for (int index = 0; index <= EllipseSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / EllipseSegments;
                Vector2 normalized = center + new Vector2(
                    Mathf.Cos(angle) * radius.x,
                    Mathf.Sin(angle) * radius.y);
                ellipsePoints[index] = rig.NormalizedToWorld(normalized);
            }

            Handles.color = color;
            Handles.DrawAAPolyLine(width, ellipsePoints);
        }

        private static void DrawSetupHandles(UIMeshRigGraphic rig, UIMeshRigPoint point)
        {
            Vector3 centerWorld = point.transform.position;
            EditorGUI.BeginChangeCheck();
            Vector3 movedCenter = Handles.PositionHandle(centerWorld, point.transform.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Move UI mesh rig point");
                point.transform.position = movedCenter;
                point.CaptureRestPose(rig);
                UIMeshRigEditorUtility.MarkChanged(rig, point, point.transform);
            }

            Vector2 center = point.RestCenterNormalized;
            Vector2 radius = point.RadiusNormalized;
            Vector3 horizontalWorld = rig.NormalizedToWorld(center + Vector2.right * radius.x);
            Vector3 verticalWorld = rig.NormalizedToWorld(center + Vector2.up * radius.y);
            float handleSize = HandleUtility.GetHandleSize(centerWorld) * 0.04f;

            EditorGUI.BeginChangeCheck();
            Vector3 movedHorizontal = Handles.FreeMoveHandle(horizontalWorld, handleSize, Vector3.zero, Handles.RectangleHandleCap);
            Vector3 movedVertical = Handles.FreeMoveHandle(verticalWorld, handleSize, Vector3.zero, Handles.RectangleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Resize UI mesh rig influence");
                Vector2 horizontalNormalized = rig.WorldToNormalized(movedHorizontal);
                Vector2 verticalNormalized = rig.WorldToNormalized(movedVertical);
                point.RadiusNormalized = new Vector2(
                    Mathf.Abs(horizontalNormalized.x - center.x),
                    Mathf.Abs(verticalNormalized.y - center.y));
                UIMeshRigEditorUtility.MarkChanged(rig, point);
            }

            float solidFraction = 1f - point.Falloff;
            Vector3 falloffWorld = rig.NormalizedToWorld(center + Vector2.right * radius.x * solidFraction);
            EditorGUI.BeginChangeCheck();
            Vector3 movedFalloff = Handles.FreeMoveHandle(falloffWorld, handleSize * 0.85f, Vector3.zero, Handles.CircleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Edit UI mesh rig falloff");
                Vector2 falloffNormalized = rig.WorldToNormalized(movedFalloff);
                float newSolidFraction = Mathf.Abs(falloffNormalized.x - center.x) / Mathf.Max(0.005f, radius.x);
                point.Falloff = 1f - Mathf.Clamp(newSolidFraction, 0f, 0.99f);
                UIMeshRigEditorUtility.MarkChanged(rig, point);
            }
        }

        private static void DrawPoseHandle(UIMeshRigGraphic rig, UIMeshRigPoint point)
        {
            RectTransform pointRect = (RectTransform)point.transform;
            EditorGUI.BeginChangeCheck();
            Vector3 position = pointRect.position;
            Quaternion rotation = pointRect.rotation;
            Vector3 scale = pointRect.localScale;

            switch (rig.SceneTool)
            {
                case UIMeshRigSceneTool.Rotate:
                    rotation = Handles.RotationHandle(pointRect.rotation, pointRect.position);
                    break;
                case UIMeshRigSceneTool.Scale:
                    scale = Handles.ScaleHandle(
                        pointRect.localScale,
                        pointRect.position,
                        pointRect.rotation,
                        HandleUtility.GetHandleSize(pointRect.position));
                    break;
                default:
                    position = Handles.PositionHandle(pointRect.position, pointRect.rotation);
                    break;
            }

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Pose UI mesh rig point");
            pointRect.position = position;
            pointRect.rotation = rotation;
            pointRect.localScale = scale;
            pointRect.hasChanged = true;
            rig.NotifyPoseChanged();
            UIMeshRigEditorUtility.MarkChanged(rig, point, pointRect);
        }

        private static bool ContainsPoint(IReadOnlyList<UIMeshRigPoint> points, UIMeshRigPoint candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            for (int index = 0; index < points.Count; index++)
            {
                if (points[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static class UIMeshRigEditorUtility
    {
        public static UIMeshRigPoint CreatePoint(UIMeshRigGraphic rig)
        {
            string pointName = ObjectNames.GetUniqueName(GetSiblingNames(rig.transform), "Rig Point");
            GameObject pointObject = new GameObject(pointName, typeof(RectTransform), typeof(UIMeshRigPoint));
            Undo.RegisterCreatedObjectUndo(pointObject, "Add UI mesh rig point");
            Undo.SetTransformParent(pointObject.transform, rig.transform, "Parent UI mesh rig point");
            RectTransform pointRect = (RectTransform)pointObject.transform;
            pointRect.anchorMin = new Vector2(0.5f, 0.5f);
            pointRect.anchorMax = new Vector2(0.5f, 0.5f);
            pointRect.sizeDelta = new Vector2(24f, 24f);
            pointRect.anchoredPosition = Vector2.zero;
            pointObject.layer = rig.gameObject.layer;
            UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
            point.SetBindingKey(pointName);
            point.CaptureRestPose(rig);
            rig.NotifyPointChanged();
            MarkChanged(rig, point, pointRect);
            return point;
        }

        public static UIMeshRigPoint DuplicatePoint(UIMeshRigGraphic rig, UIMeshRigPoint source)
        {
            GameObject duplicateObject = UnityEngine.Object.Instantiate(source.gameObject, rig.transform);
            duplicateObject.name = ObjectNames.GetUniqueName(GetSiblingNames(rig.transform), source.gameObject.name);
            Undo.RegisterCreatedObjectUndo(duplicateObject, "Duplicate UI mesh rig point");
            UIMeshRigPoint duplicate = duplicateObject.GetComponent<UIMeshRigPoint>();
            duplicate.SetBindingKey(duplicateObject.name);
            rig.NotifyPointChanged();
            MarkChanged(rig, duplicate, duplicate.transform);
            return duplicate;
        }

        public static void DeletePoint(UIMeshRigGraphic rig, UIMeshRigPoint point)
        {
            Undo.RecordObject(rig, "Delete UI mesh rig point");
            Undo.DestroyObjectImmediate(point.gameObject);
            rig.NotifyPointChanged();
            MarkChanged(rig);
        }

        public static void Focus(UIMeshRigPoint point)
        {
            Selection.activeGameObject = point.gameObject;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.FrameSelected();
                sceneView.Repaint();
            }
        }

        public static void RecordRig(UIMeshRigGraphic rig, string actionName)
        {
            IReadOnlyList<UIMeshRigPoint> points = rig.Points;
            UnityEngine.Object[] objects = new UnityEngine.Object[2 + points.Count * 2];
            objects[0] = rig;
            objects[1] = rig.transform;
            for (int index = 0; index < points.Count; index++)
            {
                objects[2 + index * 2] = points[index];
                objects[3 + index * 2] = points[index].transform;
            }

            Undo.RecordObjects(objects, actionName);
        }

        public static void RecordPointAndRig(UIMeshRigGraphic rig, UIMeshRigPoint point, string actionName)
        {
            Undo.RecordObjects(
                new UnityEngine.Object[] { rig, rig.transform, point, point.transform },
                actionName);
        }

        public static void MarkRigAndPointsChanged(UIMeshRigGraphic rig)
        {
            MarkChanged(rig, rig.transform);
            IReadOnlyList<UIMeshRigPoint> points = rig.Points;
            for (int index = 0; index < points.Count; index++)
            {
                MarkChanged(points[index], points[index].transform);
            }
        }

        public static void MarkChanged(params UnityEngine.Object[] objects)
        {
            for (int index = 0; index < objects.Length; index++)
            {
                UnityEngine.Object changedObject = objects[index];
                if (changedObject == null)
                {
                    continue;
                }

                EditorUtility.SetDirty(changedObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(changedObject);
            }
        }

        private static string[] GetSiblingNames(Transform parent)
        {
            string[] names = new string[parent.childCount];
            for (int index = 0; index < parent.childCount; index++)
            {
                names[index] = parent.GetChild(index).name;
            }

            return names;
        }
    }

    internal static class UIMeshRigMotionInspector
    {
        private static bool _showCurves;

        public static void Draw(UIMeshRigPoint point)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Preset", EditorStyles.boldLabel);
            UIMeshRigPointMotion motion = point.GetComponent<UIMeshRigPointMotion>();
            if (motion == null)
            {
                if (GUILayout.Button("Add Point Motion"))
                {
                    motion = Undo.AddComponent<UIMeshRigPointMotion>(point.gameObject);
                    UIMeshRigEditorUtility.MarkChanged(point, motion);
                }

                return;
            }

            SerializedObject motionObject = new SerializedObject(motion);
            motionObject.Update();
            SerializedProperty preset = motionObject.FindProperty("_preset");
            SerializedProperty profile = motionObject.FindProperty("_profile");
            SerializedProperty playOnEnable = motionObject.FindProperty("_playOnEnable");
            SerializedProperty useUnscaledTime = motionObject.FindProperty("_useUnscaledTime");
            SerializedProperty preview = motionObject.FindProperty("_previewInEditMode");
            SerializedProperty speed = motionObject.FindProperty("_speed");
            SerializedProperty phase = motionObject.FindProperty("_phase");

            EditorGUILayout.PropertyField(preset, new GUIContent("Preset"));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Preset"))
            {
                motionObject.ApplyModifiedProperties();
                Undo.RecordObject(motion, "Apply UI mesh rig motion preset");
                motion.ApplyPreset((UIMeshRigMotionPreset)preset.enumValueIndex);
                UIMeshRigEditorUtility.MarkChanged(motion, point);
                if (!Application.isPlaying && motion.PreviewInEditMode)
                {
                    motion.Restart();
                }
            }

            if (GUILayout.Button("Custom Curves"))
            {
                preset.enumValueIndex = (int)UIMeshRigMotionPreset.Custom;
                _showCurves = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(playOnEnable);
            EditorGUILayout.PropertyField(useUnscaledTime);
            EditorGUILayout.PropertyField(speed);
            EditorGUILayout.PropertyField(phase);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(preview, new GUIContent("Preview In Edit Mode"));
            bool previewChanged = EditorGUI.EndChangeCheck();

            _showCurves = EditorGUILayout.Foldout(_showCurves, "Motion Profile / Curves", true);
            if (_showCurves)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(profile, true);
                EditorGUI.indentLevel--;
            }

            bool propertiesChanged = motionObject.ApplyModifiedProperties();
            if (propertiesChanged)
            {
                UIMeshRigEditorUtility.MarkChanged(motion, point);
            }

            if (previewChanged)
            {
                if (preview.boolValue)
                {
                    motion.Restart();
                }
                else
                {
                    motion.Stop();
                }

                SceneView.RepaintAll();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart Preview"))
            {
                motion.Restart();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Stop Preview"))
            {
                motion.Stop();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        public static bool IsPreviewing(UIMeshRigPoint point)
        {
            UIMeshRigPointMotion motion = point.GetComponent<UIMeshRigPointMotion>();
            return !Application.isPlaying && motion != null && motion.PreviewInEditMode;
        }
    }
}
