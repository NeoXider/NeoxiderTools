using System.Collections.Generic;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.UI.Editor
{
    [InitializeOnLoad]
    internal static class UIMeshRigMotionPreviewDriver
    {
        private static readonly List<UIMeshRigPointMotion> ActiveMotions = new List<UIMeshRigPointMotion>();

        // WHY: stopping a preview raises EditModePreviewStateChanged, and its handler edits ActiveMotions.
        // Walking the live list by index therefore skipped entries or indexed past its end as soon as a
        // callback removed something. Every loop below runs over a copy so the callbacks cannot corrupt it.
        private static readonly List<UIMeshRigPointMotion> TickBuffer = new List<UIMeshRigPointMotion>();
        private static readonly List<UIMeshRigPointMotion> SelectionBuffer = new List<UIMeshRigPointMotion>();
        private static double _lastTime;
        private static bool _updateSubscribed;

        static UIMeshRigMotionPreviewDriver()
        {
            _lastTime = EditorApplication.timeSinceStartup;
            UIMeshRigPointMotion.EditModePreviewStateChanged -= HandlePreviewStateChanged;
            UIMeshRigPointMotion.EditModePreviewStateChanged += HandlePreviewStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.quitting -= Shutdown;
            EditorApplication.quitting += Shutdown;
            Selection.selectionChanged -= HandleSelectionChanged;
            Selection.selectionChanged += HandleSelectionChanged;
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Clamp((float)(now - _lastTime), 0f, 0.1f);
            _lastTime = now;
            Tick(deltaTime);
        }

        private static void Tick(float deltaTime)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StopAllPreviews();
                return;
            }

            bool previewed = false;
            try
            {
                TickBuffer.Clear();
                TickBuffer.AddRange(ActiveMotions);
                for (int index = 0; index < TickBuffer.Count; index++)
                {
                    UIMeshRigPointMotion motion = TickBuffer[index];
                    if (motion == null)
                    {
                        // Destroyed entries are swept by RemoveDestroyedMotions in the finally block —
                        // List.Remove cannot match a destroyed UnityEngine.Object by reference equality.
                        continue;
                    }

                    if (!motion.isActiveAndEnabled || !motion.PreviewInEditMode ||
                        EditorUtility.IsPersistent(motion))
                    {
                        Unregister(motion);
                        continue;
                    }

                    if (!motion.IsPlaying || motion.IsPaused || deltaTime <= 0f)
                    {
                        continue;
                    }

                    try
                    {
                        motion.SetTime(motion.CurrentTime + deltaTime);
                        previewed = true;
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogException(exception, motion);
                        Unregister(motion);
                        motion.PreviewInEditMode = false;
                    }
                }
            }
            finally
            {
                TickBuffer.Clear();
                RefreshUpdateSubscription();
                if (previewed)
                {
                    SceneView.RepaintAll();
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }

        internal static bool IsUpdateSubscribed => _updateSubscribed;
        internal static int ActivePreviewCount => ActiveMotions.Count;

        internal static void StartPreview(UIMeshRigPointMotion motion)
        {
            if (motion == null || Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorUtility.IsPersistent(motion))
            {
                return;
            }

            motion.PreviewInEditMode = true;
            Register(motion);
            RefreshUpdateSubscription();
        }

        internal static void PausePreview(UIMeshRigPointMotion motion)
        {
            if (motion == null)
            {
                return;
            }

            motion.Pause();
            RefreshUpdateSubscription();
        }

        internal static void ResumePreview(UIMeshRigPointMotion motion)
        {
            if (motion == null)
            {
                return;
            }

            motion.Resume();
            Register(motion);
            RefreshUpdateSubscription();
        }

        internal static void StopPreview(UIMeshRigPointMotion motion)
        {
            if (motion == null)
            {
                RemoveDestroyedMotions();
                RefreshUpdateSubscription();
                return;
            }

            Unregister(motion);
            motion.PreviewInEditMode = false;
            motion.Stop();
            RefreshUpdateSubscription();
        }

        /// <summary>
        /// Re-checks every running preview against the current selection. Rig inspectors call this when
        /// they are disabled — an Inspector that closes or re-targets is exactly the moment a preview can
        /// be orphaned, and <see cref="Selection.selectionChanged" /> does not fire for it.
        /// <para>
        /// Deliberately selection-based and not target-based: an Editor built with
        /// <c>ScriptableObject.CreateInstance</c> has no target array, and Unity's own <c>Editor.target</c>
        /// getter throws on it — including while Unity runs <c>OnDisable</c> inside <c>DestroyImmediate</c>.
        /// </para>
        /// </summary>
        internal static void StopPreviewsOutsideSelection()
        {
            HandleSelectionChanged();
        }

        internal static void StopAllPreviews()
        {
            while (ActiveMotions.Count > 0)
            {
                int lastIndex = ActiveMotions.Count - 1;
                UIMeshRigPointMotion motion = ActiveMotions[lastIndex];
                ActiveMotions.RemoveAt(lastIndex);
                if (motion != null)
                {
                    motion.PreviewInEditMode = false;
                    motion.Stop();
                }
            }

            RefreshUpdateSubscription();
        }

        internal static void TickForTests(float deltaTime)
        {
            Tick(Mathf.Max(0f, deltaTime));
        }

        private static void HandlePreviewStateChanged(UIMeshRigPointMotion motion)
        {
            if (motion != null && motion.PreviewInEditMode)
            {
                Register(motion);
            }
            else
            {
                Unregister(motion);
            }

            RefreshUpdateSubscription();
        }

        private static void HandleSelectionChanged()
        {
            GameObject selectedObject = Selection.activeGameObject;
            Object selectedAsset = Selection.activeObject;
            SelectionBuffer.Clear();
            SelectionBuffer.AddRange(ActiveMotions);
            for (int index = 0; index < SelectionBuffer.Count; index++)
            {
                UIMeshRigPointMotion motion = SelectionBuffer[index];
                if (motion == null)
                {
                    continue;
                }

                UIMeshRigPoint point = motion.GetComponent<UIMeshRigPoint>();
                IUIMeshRigOwner owner = point != null ? UIMeshRigOwnerResolver.Find(point.transform) : null;
                bool ownsSelection = selectedObject == motion.gameObject ||
                                     (owner != null && (selectedObject == owner.RigTransform.gameObject ||
                                                        selectedAsset == (owner as Object)));
                if (!ownsSelection)
                {
                    StopPreview(motion);
                }
            }

            SelectionBuffer.Clear();
            RefreshUpdateSubscription();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                StopAllPreviews();
            }
        }

        private static void Register(UIMeshRigPointMotion motion)
        {
            if (motion != null && !ActiveMotions.Contains(motion))
            {
                ActiveMotions.Add(motion);
            }
        }

        private static void Unregister(UIMeshRigPointMotion motion)
        {
            if (motion != null)
            {
                ActiveMotions.Remove(motion);
            }
        }

        private static void RemoveDestroyedMotions()
        {
            for (int index = ActiveMotions.Count - 1; index >= 0; index--)
            {
                if (ActiveMotions[index] == null)
                {
                    ActiveMotions.RemoveAt(index);
                }
            }
        }

        private static void RefreshUpdateSubscription()
        {
            RemoveDestroyedMotions();
            bool needsUpdate = false;
            for (int index = 0; index < ActiveMotions.Count; index++)
            {
                UIMeshRigPointMotion motion = ActiveMotions[index];
                if (motion != null && motion.isActiveAndEnabled && motion.PreviewInEditMode &&
                    motion.IsPlaying && !motion.IsPaused)
                {
                    needsUpdate = true;
                    break;
                }
            }

            if (needsUpdate == _updateSubscribed)
            {
                return;
            }

            EditorApplication.update -= Update;
            if (needsUpdate)
            {
                _lastTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += Update;
            }

            _updateSubscribed = needsUpdate;
        }

        private static void Shutdown()
        {
            StopAllPreviews();
            EditorApplication.update -= Update;
            _updateSubscribed = false;
            UIMeshRigPointMotion.EditModePreviewStateChanged -= HandlePreviewStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.quitting -= Shutdown;
            Selection.selectionChanged -= HandleSelectionChanged;
        }
    }

    /// <summary>
    /// uGUI rig inspector. Fields are declared with plain <c>[Header]</c> / <c>[Tooltip]</c> on the
    /// component and drawn by <see cref="CustomEditorBase"/>, which is what produces the collapsible
    /// sections, ON/OFF switches and coloured rails everywhere else in the package. Only the parts that no
    /// attribute can express — the layout button, diagnostics, authoring toolbar, point list and the Scene
    /// handles — live here. The previous shape drew every field by hand and therefore lost the whole system.
    /// </summary>
    [CustomEditor(typeof(UIMeshRigGraphic))]
    public sealed class UIMeshRigGraphicEditor : CustomEditorBase
    {
        private UIMeshRigPoint _selectedPoint;
        private UIMeshRigLayoutPreset _layoutPreset = UIMeshRigLayoutPreset.SimpleBounce;

        protected override string NeoxiderModuleName => "UI Mesh Rig";

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void OnDisable()
        {
            UIMeshRigMotionPreviewDriver.StopPreviewsOutsideSelection();
            UnityEditor.Tools.hidden = false;
            base.OnDisable();
        }

        protected override void OnAfterDrawNeoProperties()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)target;
            UIMeshRigOwnerInspector.DrawQuickStart(rig, ref _layoutPreset);
            UIMeshRigOwnerInspector.DrawDiagnostics(rig, rig.Sprite, rig.Columns, rig.Rows, "Canvas mesh (uGUI)");
            UIMeshRigOwnerInspector.DrawAuthoringControls(rig);
            UnityEditor.Tools.hidden = rig.AuthoringMode == UIMeshRigAuthoringMode.Setup;
            _selectedPoint = UIMeshRigOwnerInspector.DrawPointList(rig, _selectedPoint);
        }

        private void OnSceneGUI()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)target;
            _selectedPoint = UIMeshRigSceneHandles.Draw(rig, _selectedPoint);
        }
    }

    [CustomEditor(typeof(UIMeshRigPoint))]
    public sealed class UIMeshRigPointEditor : CustomEditorBase
    {
        protected override string NeoxiderModuleName => "UI Mesh Rig";

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void OnDisable()
        {
            UIMeshRigMotionPreviewDriver.StopPreviewsOutsideSelection();
            UnityEditor.Tools.hidden = false;
            base.OnDisable();
        }

        protected override void OnAfterDrawNeoProperties()
        {
            UIMeshRigPoint point = (UIMeshRigPoint)target;
            IUIMeshRigOwner owner = UIMeshRigOwnerResolver.Find(point.transform);

            EditorGUILayout.HelpBox(
                "Inside INNER the sprite follows fully. Influence fades to zero at OUTER; vertices outside " +
                "OUTER do not move. No visible change? Move the point in Pose / Animate, or enlarge Outer " +
                "so it contains mesh vertices.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Apply Falloff Preset",
                    "Rebuilds the Falloff Curve from the preset chosen above.")))
            {
                Undo.RecordObject(point, "Apply UI mesh rig falloff preset");
                point.ApplyFalloffPreset(point.FalloffPreset);
                UIMeshRigEditorUtility.MarkChanged(point);
            }

            if (GUILayout.Button(new GUIContent("Full Smooth From Center", "Inner = 0, smooth falloff.")))
            {
                Undo.RecordObject(point, "Use full smooth UI mesh rig falloff");
                point.UseFullSmoothFalloff();
                UIMeshRigEditorUtility.MarkChanged(point);
            }

            if (GUILayout.Button(new GUIContent("Inner = 50% of Outer", "Balanced solid core plus soft edge.")))
            {
                Undo.RecordObject(point, "Set UI mesh rig inner radius");
                point.SetInfluenceRadii(point.OuterRadiusNormalized * 0.5f, point.OuterRadiusNormalized);
                UIMeshRigEditorUtility.MarkChanged(point);
            }
            EditorGUILayout.EndHorizontal();

            if (owner == null)
            {
                EditorGUILayout.HelpBox(
                    "This point must be a child of a UI Mesh Rig Graphic, World Renderer or Sprite Renderer.",
                    MessageType.Error);
                return;
            }

            // WHY: no SceneView.RepaintAll() from OnInspectorGUI. Combined with RequiresConstantRepaint it
            // made the Inspector and the Scene view repaint each other for as long as a motion component
            // existed — including while the preview was paused or stopped. The preview driver repaints
            // exactly on the frames it actually advanced.
            UIMeshRigMotionInspector.Draw(point);

            UIMeshRigOwnerInspector.DrawAuthoringControls(owner);
            UnityEditor.Tools.hidden = owner.AuthoringMode == UIMeshRigAuthoringMode.Setup;
            UIMeshRigOwnerInspector.DrawPointActions(owner, point);
        }

        public override bool RequiresConstantRepaint()
        {
            UIMeshRigPoint point = target as UIMeshRigPoint;
            return point != null && UIMeshRigMotionInspector.IsPreviewPlaying(point);
        }

        private void OnSceneGUI()
        {
            UIMeshRigPoint point = (UIMeshRigPoint)target;
            IUIMeshRigOwner owner = UIMeshRigOwnerResolver.Find(point.transform);
            if (owner != null)
            {
                UIMeshRigSceneHandles.Draw(owner, point);
            }
        }
    }

    [CustomEditor(typeof(UIMeshRigPointMotion))]
    public sealed class UIMeshRigPointMotionEditor : CustomEditorBase
    {
        protected override string NeoxiderModuleName => "UI Mesh Rig";

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void OnDisable()
        {
            UIMeshRigMotionPreviewDriver.StopPreviewsOutsideSelection();
            base.OnDisable();
        }

        protected override void OnAfterDrawNeoProperties()
        {
            UIMeshRigPointMotion motion = (UIMeshRigPointMotion)target;
            UIMeshRigPoint point = motion.GetComponent<UIMeshRigPoint>();
            EditorGUILayout.LabelField(
                UIMeshRigMotionInspector.GetPresetHint(motion.Preset),
                EditorStyles.wordWrappedMiniLabel);
            UIMeshRigMotionInspector.DrawTransport(motion, point);
            if (point != null && GUILayout.Button("Select Rig Point Controls"))
            {
                Selection.activeGameObject = point.gameObject;
                EditorGUIUtility.PingObject(point);
            }
        }

        public override bool RequiresConstantRepaint()
        {
            UIMeshRigPointMotion motion = target as UIMeshRigPointMotion;
            return motion != null && !Application.isPlaying && motion.PreviewInEditMode &&
                   motion.IsPlaying && !motion.IsPaused;
        }
    }

    /// <summary>
    /// Inspector blocks shared by every rig adapter. One implementation, so the uGUI, world, SpriteRenderer
    /// and UI Toolkit inspectors cannot drift apart the way three hand-written copies did.
    /// </summary>
    internal static class UIMeshRigOwnerInspector
    {
        public static void DrawQuickStart(IUIMeshRigOwner rig, ref UIMeshRigLayoutPreset preset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ready-Made Rig", EditorStyles.boldLabel);
            preset = (UIMeshRigLayoutPreset)EditorGUILayout.EnumPopup(
                new GUIContent("Layout",
                    "Simple Bounce uses one point; Character adds Root/Torso/Chest/Head; Flag / Cloth adds a travelling wave."),
                preset);
            if (GUILayout.Button("Apply Layout & Preview", GUILayout.Height(28f)))
            {
                UIMeshRigEditorUtility.ApplyLayout(rig, preset, true);
                Selection.activeGameObject = rig.RigTransform.gameObject;
                GUIUtility.ExitGUI();
            }

            if (rig.RigPoints.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This rig has no points yet. Apply a layout above to make it deform and animate.",
                    MessageType.Warning);
            }
        }

        public static void DrawDiagnostics(IUIMeshRigOwner rig, Sprite sprite, int columns, int rows, string output)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);
            int vertexCount = (columns + 1) * (rows + 1);
            int triangleCount = columns * rows * 2;
            EditorGUILayout.LabelField("Generated geometry", vertexCount + " vertices / " + triangleCount + " triangles");
            EditorGUILayout.LabelField("Control points", rig.RigPoints.Count.ToString());
            EditorGUILayout.LabelField("Output", output);

            if (sprite == null)
            {
                EditorGUILayout.HelpBox("Assign a Source Sprite. The rig cannot render without one.", MessageType.Warning);
            }

            if (columns < 4 || rows < 4)
            {
                EditorGUILayout.HelpBox("A grid below 4 x 4 usually cannot deform smoothly.", MessageType.Warning);
            }
            else if (vertexCount > 1200)
            {
                EditorGUILayout.HelpBox("This is a dense UI mesh. Reduce the grid unless the silhouette needs this detail.", MessageType.Info);
            }

            HashSet<string> bindingKeys = new HashSet<string>();
            IReadOnlyList<UIMeshRigPoint> points = rig.RigPoints;
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

        public static void DrawAuthoringControls(IUIMeshRigOwner rig)
        {
            Object rigObject = rig as Object;
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
                UIMeshRigSceneTool currentTool = UIMeshRigEditorUtility.GetSceneTool(rig);
                UIMeshRigSceneTool requestedTool = (UIMeshRigSceneTool)GUILayout.Toolbar(
                    (int)currentTool,
                    new[] { "Move", "Rotate", "Scale" });
                if (requestedTool != currentTool)
                {
                    Undo.RecordObject(rigObject, "Change UI mesh rig scene tool");
                    UIMeshRigEditorUtility.SetSceneTool(rig, requestedTool);
                    UIMeshRigEditorUtility.MarkChanged(rigObject);
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
                ? "SETUP edits bind centers, influence ellipses and falloff. The same mode switch is available in the Scene view overlay."
                : "POSE edits the point Transforms. Position, rotation and scale are Animator-recordable and can also be saved as a static deformation.";
            EditorGUILayout.HelpBox(help, MessageType.Info);
        }

        public static UIMeshRigPoint DrawPointList(IUIMeshRigOwner rig, UIMeshRigPoint selectedPoint)
        {
            IReadOnlyList<UIMeshRigPoint> points = rig.RigPoints;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Points (" + points.Count + ")", EditorStyles.boldLabel);

            for (int index = 0; index < points.Count; index++)
            {
                UIMeshRigPoint point = points[index];
                EditorGUILayout.BeginHorizontal();
                bool isSelected = point == selectedPoint;
                if (GUILayout.Toggle(isSelected, point.name, "Button"))
                {
                    selectedPoint = point;
                }

                if (GUILayout.Button("Focus", GUILayout.Width(48f)))
                {
                    selectedPoint = point;
                    UIMeshRigEditorUtility.Focus(point);
                }

                if (GUILayout.Button("Dup", GUILayout.Width(38f)))
                {
                    selectedPoint = UIMeshRigEditorUtility.DuplicatePoint(rig, point);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    UIMeshRigEditorUtility.DeletePoint(rig, point);
                    selectedPoint = null;
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            return selectedPoint;
        }

        public static void DrawPointActions(IUIMeshRigOwner rig, UIMeshRigPoint point)
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
                Selection.activeGameObject = rig.RigTransform.gameObject;
                UIMeshRigEditorUtility.DeletePoint(rig, point);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Scene-view authoring for every adapter.
    /// <para>
    /// Three complaints shaped this: the centre and radius handles only existed in Setup (so Pose looked
    /// like the anchor and rings could not be moved at all), seven points drew fourteen opaque ellipses and
    /// seven overlapping labels, and the mode that decided all of it lived far away in the Inspector. Now the
    /// mode switch sits in a Scene overlay, unselected points draw one faint ring without a label, and the
    /// bind handles stay reachable in both modes.
    /// </para>
    /// <para>
    /// <c>Handles.Label</c> ignores <c>Handles.color</c> — it renders through a GUI style — so every label
    /// here is dimmed through its own <see cref="GUIStyle"/> instead.
    /// </para>
    /// </summary>
    internal static class UIMeshRigSceneHandles
    {
        private const int EllipseSegments = 64;
        private const string ShowAllLabelsKey = "Neoxider.UIMeshRig.ShowAllLabels";
        private const string ShowAllRingsKey = "Neoxider.UIMeshRig.ShowAllRings";

        private static readonly Color SelectedOuter = new Color(1f, 0.42f, 0.09f);
        private static readonly Color SelectedInner = new Color(0.15f, 0.85f, 1f);
        private static readonly Color IdleRing = new Color(0.35f, 0.85f, 1f, 0.22f);
        private static readonly Color AnchorColor = new Color(1f, 0.92f, 0.2f);

        private static GUIStyle _selectedLabelStyle;
        private static GUIStyle _idleLabelStyle;

        public static UIMeshRigPoint Draw(IUIMeshRigOwner rig, UIMeshRigPoint selectedPoint)
        {
            if (rig == null || rig.RigTransform == null)
            {
                return null;
            }

            IReadOnlyList<UIMeshRigPoint> points = rig.RigPoints;
            if (selectedPoint == null && Selection.activeGameObject != null)
            {
                UIMeshRigPoint selectedComponent = Selection.activeGameObject.GetComponent<UIMeshRigPoint>();
                if (ContainsPoint(points, selectedComponent))
                {
                    selectedPoint = selectedComponent;
                }
            }

            DrawOverlay(rig, selectedPoint);

            for (int index = 0; index < points.Count; index++)
            {
                UIMeshRigPoint point = points[index];
                if (point == selectedPoint)
                {
                    continue;
                }

                DrawIdlePoint(rig, point);
                Vector3 center = point.transform.position;
                float pickSize = HandleUtility.GetHandleSize(center) * 0.055f;
                Handles.color = new Color(0.35f, 0.85f, 1f, 0.75f);
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

            DrawSelectedPoint(rig, selectedPoint);

            // WHY: bind handles used to disappear the moment the rig switched to Pose, which read as
            // "the anchor and the circles cannot be moved". Both authoring aids are always available now;
            // the pose gizmo is simply added on top while posing.
            DrawSetupHandles(rig, selectedPoint);
            if (rig.AuthoringMode == UIMeshRigAuthoringMode.Pose)
            {
                DrawPoseHandle(rig, selectedPoint);
            }

            return selectedPoint;
        }

        private static bool ShowAllLabels
        {
            get => EditorPrefs.GetBool(ShowAllLabelsKey, false);
            set => EditorPrefs.SetBool(ShowAllLabelsKey, value);
        }

        private static bool ShowAllRings
        {
            get => EditorPrefs.GetBool(ShowAllRingsKey, false);
            set => EditorPrefs.SetBool(ShowAllRingsKey, value);
        }

        /// <summary>Mode switch and clutter toggles, on the Scene view where the handles actually are.</summary>
        private static void DrawOverlay(IUIMeshRigOwner rig, UIMeshRigPoint selectedPoint)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(8f, 8f, 250f, 96f), GUIContent.none, EditorStyles.helpBox);

            GUILayout.Label(
                "UI Mesh Rig — " + rig.RigTransform.name +
                (selectedPoint != null ? " / " + selectedPoint.name : string.Empty),
                EditorStyles.miniBoldLabel);

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
                UIMeshRigSceneTool currentTool = UIMeshRigEditorUtility.GetSceneTool(rig);
                UIMeshRigSceneTool requestedTool = (UIMeshRigSceneTool)GUILayout.Toolbar(
                    (int)currentTool,
                    new[] { "Move", "Rotate", "Scale" });
                if (requestedTool != currentTool)
                {
                    Undo.RecordObject(rig as Object, "Change UI mesh rig scene tool");
                    UIMeshRigEditorUtility.SetSceneTool(rig, requestedTool);
                    UIMeshRigEditorUtility.MarkChanged(rig as Object);
                }
            }

            GUILayout.BeginHorizontal();
            ShowAllLabels = GUILayout.Toggle(ShowAllLabels, new GUIContent("Labels",
                "Off: only the selected point is named, so a dense rig stays readable."), EditorStyles.miniButton);
            ShowAllRings = GUILayout.Toggle(ShowAllRings, new GUIContent("All rings",
                "Off: unselected points draw one faint outer ring instead of two solid ellipses."),
                EditorStyles.miniButton);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void DrawIdlePoint(IUIMeshRigOwner rig, UIMeshRigPoint point)
        {
            DrawEllipse(rig, point.RestCenterNormalized, point.OuterRadiusNormalized, IdleRing, 1.5f);
            if (ShowAllRings)
            {
                DrawEllipse(rig, point.RestCenterNormalized, point.InnerRadiusNormalized,
                    new Color(IdleRing.r, IdleRing.g, IdleRing.b, 0.14f), 1f);
            }

            if (ShowAllLabels)
            {
                Handles.Label(rig.NormalizedToWorld(point.RestCenterNormalized), point.name, GetIdleLabelStyle());
            }
        }

        private static void DrawSelectedPoint(IUIMeshRigOwner rig, UIMeshRigPoint point)
        {
            Vector2 center = point.RestCenterNormalized;
            DrawEllipse(rig, center, point.OuterRadiusNormalized, SelectedOuter, 4f);
            DrawEllipse(rig, center, point.InnerRadiusNormalized, SelectedInner, 3f);

            GUIStyle style = GetSelectedLabelStyle();
            Handles.Label(rig.NormalizedToWorld(center), point.name, style);
            Handles.Label(rig.NormalizedToWorld(center + Vector2.up * point.InnerRadiusNormalized.y), "FULL", style);
            Handles.Label(rig.NormalizedToWorld(center + Vector2.up * point.OuterRadiusNormalized.y), "ZERO", style);
        }

        private static void DrawEllipse(IUIMeshRigOwner rig, Vector2 center, Vector2 radius, Color color, float width)
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

        private static void DrawSetupHandles(IUIMeshRigOwner rig, UIMeshRigPoint point)
        {
            Vector2 center = point.RestCenterNormalized;
            Vector3 centerWorld = rig.NormalizedToWorld(center);
            float baseSize = HandleUtility.GetHandleSize(centerWorld);
            Vector3 rightWorld = (rig.NormalizedToWorld(center + Vector2.right) - centerWorld).normalized;
            Vector3 upWorld = (rig.NormalizedToWorld(center + Vector2.up) - centerWorld).normalized;
            Vector3 normal = rig.RigTransform.forward;

            // WHY: the anchor used to be a 0.09 dot sitting on top of the point's own circle cap and read as
            // one blob. A dark contrast ring plus a bright crosshair makes it findable on any artwork.
            Handles.color = new Color(0f, 0f, 0f, 0.55f);
            Handles.DrawWireDisc(centerWorld, normal, baseSize * 0.155f, 4f);
            Handles.color = AnchorColor;
            Handles.DrawWireDisc(centerWorld, normal, baseSize * 0.145f, 2f);
            Handles.DrawLine(centerWorld - rightWorld * baseSize * 0.22f, centerWorld + rightWorld * baseSize * 0.22f, 2f);
            Handles.DrawLine(centerWorld - upWorld * baseSize * 0.22f, centerWorld + upWorld * baseSize * 0.22f, 2f);

            EditorGUI.BeginChangeCheck();
            Vector3 movedCenter = Handles.Slider2D(
                centerWorld,
                normal,
                rightWorld,
                upWorld,
                baseSize * 0.13f,
                Handles.DotHandleCap,
                Vector2.zero);
            if (EditorGUI.EndChangeCheck())
            {
                UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Move UI mesh rig point");
                UIMeshRigEditorUtility.SetRestCenterPreservingPose(
                    rig,
                    point,
                    rig.WorldToNormalized(movedCenter));
                if (UIMeshRigMotionInspector.IsPreviewing(point))
                {
                    UIMeshRigEditorUtility.MarkChanged(rig as Object, point);
                }
                else
                {
                    UIMeshRigEditorUtility.MarkChanged(rig as Object, point, point.transform);
                }
            }

            Vector2 outer = point.OuterRadiusNormalized;
            Vector2 inner = point.InnerRadiusNormalized;
            float handleSize = baseSize * 0.055f;

            Vector2 movedOuter = DrawRadiusHandles(
                rig, center, outer, rightWorld, upWorld, handleSize, SelectedOuter, Handles.RectangleHandleCap,
                out bool outerChanged);
            if (outerChanged)
            {
                UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Resize UI mesh rig influence");
                point.SetInfluenceRadii(inner, movedOuter);
                UIMeshRigEditorUtility.MarkChanged(rig as Object, point);
                return;
            }

            Vector2 movedInner = DrawRadiusHandles(
                rig, center, inner, rightWorld, upWorld, handleSize, SelectedInner, Handles.CircleHandleCap,
                out bool innerChanged);
            if (innerChanged)
            {
                UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Resize UI mesh rig full influence area");
                point.SetInfluenceRadii(movedInner, outer);
                UIMeshRigEditorUtility.MarkChanged(rig as Object, point);
            }
        }

        /// <summary>
        /// Four handles per ellipse (+X, -X, +Y, -Y). Dragging any of them edits the same radius, so a ring
        /// can be resized from whichever side is not hidden behind the artwork.
        /// </summary>
        private static Vector2 DrawRadiusHandles(
            IUIMeshRigOwner rig,
            Vector2 center,
            Vector2 radius,
            Vector3 rightWorld,
            Vector3 upWorld,
            float handleSize,
            Color color,
            Handles.CapFunction cap,
            out bool changed)
        {
            Handles.color = color;
            Vector2 result = radius;
            changed = false;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 horizontalWorld = rig.NormalizedToWorld(center + Vector2.right * (radius.x * side));
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.Slider(horizontalWorld, rightWorld, handleSize, cap, 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    result.x = Mathf.Abs(rig.WorldToNormalized(moved).x - center.x);
                    changed = true;
                }

                Vector3 verticalWorld = rig.NormalizedToWorld(center + Vector2.up * (radius.y * side));
                EditorGUI.BeginChangeCheck();
                moved = Handles.Slider(verticalWorld, upWorld, handleSize, cap, 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    result.y = Mathf.Abs(rig.WorldToNormalized(moved).y - center.y);
                    changed = true;
                }
            }

            return result;
        }

        private static void DrawPoseHandle(IUIMeshRigOwner rig, UIMeshRigPoint point)
        {
            Transform pointTransform = point.transform;
            EditorGUI.BeginChangeCheck();
            Vector3 position = pointTransform.position;
            Quaternion rotation = pointTransform.rotation;
            Vector3 scale = pointTransform.localScale;

            switch (UIMeshRigEditorUtility.GetSceneTool(rig))
            {
                case UIMeshRigSceneTool.Rotate:
                    rotation = Handles.RotationHandle(pointTransform.rotation, pointTransform.position);
                    break;
                case UIMeshRigSceneTool.Scale:
                    scale = Handles.ScaleHandle(
                        pointTransform.localScale,
                        pointTransform.position,
                        pointTransform.rotation,
                        HandleUtility.GetHandleSize(pointTransform.position));
                    break;
                default:
                    position = Handles.PositionHandle(pointTransform.position, pointTransform.rotation);
                    break;
            }

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Pose UI mesh rig point");
            pointTransform.position = position;
            pointTransform.rotation = rotation;
            pointTransform.localScale = scale;
            pointTransform.hasChanged = true;
            rig.NotifyPoseChanged();
            UIMeshRigEditorUtility.MarkChanged(rig as Object, point, pointTransform);
        }

        // WHY: Handles.Label draws through a GUIStyle and ignores Handles.color entirely, so alpha has to be
        // baked into the style's text colour or every label comes out fully opaque.
        private static GUIStyle GetSelectedLabelStyle()
        {
            if (_selectedLabelStyle == null)
            {
                _selectedLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel);
            }

            _selectedLabelStyle.normal.textColor = new Color(1f, 0.85f, 0.55f, 1f);
            return _selectedLabelStyle;
        }

        private static GUIStyle GetIdleLabelStyle()
        {
            if (_idleLabelStyle == null)
            {
                _idleLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            }

            _idleLabelStyle.normal.textColor = new Color(0.65f, 0.85f, 0.95f, 0.45f);
            return _idleLabelStyle;
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
        public static UIMeshRigPoint ApplyLayout(
            UIMeshRigGraphic rig,
            UIMeshRigLayoutPreset preset,
            bool previewInEditMode)
        {
            return ApplyLayout((IUIMeshRigOwner)rig, preset, previewInEditMode);
        }

        public static UIMeshRigPoint ApplyLayout(
            IUIMeshRigOwner rig,
            UIMeshRigLayoutPreset preset,
            bool previewInEditMode)
        {
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply UI mesh rig layout");
            RecordRig(rig, "Apply UI mesh rig layout");

            UIMeshRigPoint[] existingPoints = rig.RigTransform.GetComponentsInChildren<UIMeshRigPoint>(true);
            for (int index = existingPoints.Length - 1; index >= 0; index--)
            {
                if (UIMeshRigOwnerResolver.OwnsPoint(rig, existingPoints[index]))
                {
                    Undo.DestroyObjectImmediate(existingPoints[index].gameObject);
                }
            }

            UIMeshRigPoint firstPoint = null;
            int pointCount = UIMeshRigLayoutPresets.GetPointCount(preset);
            for (int index = 0; index < pointCount; index++)
            {
                UIMeshRigPointLayout layout = UIMeshRigLayoutPresets.GetPoint(preset, index);
                UIMeshRigPoint point = CreatePoint(rig, layout, previewInEditMode);
                if (firstPoint == null)
                {
                    firstPoint = point;
                }
            }

            // WHY: applying a layout is an explicit authoring action, so the rig is put into Pose mode here,
            // inside the same Undo group that created the points. The transient preview must never do this:
            // the authoring mode is serialized, and leaving Pose again runs ResetPose(), which rewrites every
            // point Transform back onto its bind anchor. That is why the runtime UIMeshRigLayoutBuilder no
            // longer touches the mode at all — starting a preview used to flip it as a side effect.
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            rig.NotifyPointChanged();
            MarkRigAndPointsChanged(rig);
            Undo.CollapseUndoOperations(undoGroup);
            return firstPoint;
        }

        public static UIMeshRigPoint CreatePoint(IUIMeshRigOwner rig)
        {
            string pointName = ObjectNames.GetUniqueName(GetSiblingNames(rig.RigTransform), "Rig Point");
            GameObject pointObject = new GameObject(pointName, typeof(RectTransform), typeof(UIMeshRigPoint));
            Undo.RegisterCreatedObjectUndo(pointObject, "Add UI mesh rig point");
            Undo.SetTransformParent(pointObject.transform, rig.RigTransform, "Parent UI mesh rig point");
            RectTransform pointRect = (RectTransform)pointObject.transform;
            UIMeshRigLayoutBuilder.ApplyPointTransform(
                rig,
                pointRect,
                new UIMeshRigPointLayout(
                    pointName,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.1f, 0.1f),
                    new Vector2(0.2f, 0.2f),
                    UIMeshRigMotionPreset.Custom,
                    0f,
                    0));
            pointObject.layer = rig.RigTransform.gameObject.layer;
            UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
            point.SetBindingKey(pointName);
            point.CaptureRestPose(rig);
            rig.NotifyPointChanged();
            MarkChanged(rig as Object, point, pointRect);
            return point;
        }

        public static UIMeshRigPoint CreatePoint(
            IUIMeshRigOwner rig,
            UIMeshRigPointLayout layout,
            bool previewInEditMode)
        {
            GameObject pointObject = new GameObject(layout.Name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(pointObject, "Add UI mesh rig preset point");
            Undo.SetTransformParent(pointObject.transform, rig.RigTransform, "Parent UI mesh rig preset point");
            RectTransform pointRect = (RectTransform)pointObject.transform;
            UIMeshRigLayoutBuilder.ApplyPointTransform(rig, pointRect, layout);
            pointObject.layer = rig.RigTransform.gameObject.layer;

            UIMeshRigPoint point = Undo.AddComponent<UIMeshRigPoint>(pointObject);
            UIMeshRigLayoutBuilder.ConfigurePointOnOwner(rig, point, layout);
            if (layout.MotionPreset != UIMeshRigMotionPreset.Custom)
            {
                UIMeshRigPointMotion motion = Undo.AddComponent<UIMeshRigPointMotion>(pointObject);
                motion.ApplyPreset(layout.MotionPreset);
                motion.Phase = layout.Phase;
                motion.Seed = layout.Seed;
                motion.PlayOnEnable = true;
                motion.PreviewInEditMode = previewInEditMode;
                MarkChanged(motion);
            }

            rig.NotifyPointChanged();
            MarkChanged(rig as Object, point, pointRect);
            return point;
        }

        public static UIMeshRigPoint DuplicatePoint(IUIMeshRigOwner rig, UIMeshRigPoint source)
        {
            GameObject duplicateObject = UnityEngine.Object.Instantiate(source.gameObject, rig.RigTransform);
            duplicateObject.name = ObjectNames.GetUniqueName(GetSiblingNames(rig.RigTransform), source.gameObject.name);
            Undo.RegisterCreatedObjectUndo(duplicateObject, "Duplicate UI mesh rig point");
            UIMeshRigPoint duplicate = duplicateObject.GetComponent<UIMeshRigPoint>();
            duplicate.SetBindingKey(duplicateObject.name);
            rig.NotifyPointChanged();
            MarkChanged(rig as Object, duplicate, duplicate.transform);
            return duplicate;
        }

        public static void DeletePoint(IUIMeshRigOwner rig, UIMeshRigPoint point)
        {
            Undo.RecordObject(rig as Object, "Delete UI mesh rig point");
            Undo.DestroyObjectImmediate(point.gameObject);
            rig.NotifyPointChanged();
            MarkChanged(rig as Object);
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

        public static UIMeshRigSceneTool GetSceneTool(IUIMeshRigOwner rig)
        {
            switch (rig)
            {
                case UIMeshRigGraphic graphic:
                    return graphic.SceneTool;
                case UIMeshRigWorldRenderer world:
                    return world.SceneTool;
                case UIMeshRigSpriteRenderer sprite:
                    return sprite.SceneTool;
                default:
                    return UIMeshRigSceneTool.Move;
            }
        }

        public static void SetSceneTool(IUIMeshRigOwner rig, UIMeshRigSceneTool tool)
        {
            switch (rig)
            {
                case UIMeshRigGraphic graphic:
                    graphic.SetSceneTool(tool);
                    break;
                case UIMeshRigWorldRenderer world:
                    world.SetSceneTool(tool);
                    break;
                case UIMeshRigSpriteRenderer sprite:
                    sprite.SetSceneTool(tool);
                    break;
            }
        }

        public static void RecordRig(IUIMeshRigOwner rig, string actionName)
        {
            IReadOnlyList<UIMeshRigPoint> points = rig.RigPoints;
            UnityEngine.Object[] objects = new UnityEngine.Object[2 + points.Count * 2];
            objects[0] = rig as UnityEngine.Object;
            objects[1] = rig.RigTransform;
            for (int index = 0; index < points.Count; index++)
            {
                objects[2 + index * 2] = points[index];
                objects[3 + index * 2] = points[index].transform;
            }

            Undo.RecordObjects(objects, actionName);
        }

        public static void RecordPointAndRig(IUIMeshRigOwner rig, UIMeshRigPoint point, string actionName)
        {
            Undo.RecordObjects(
                new UnityEngine.Object[] { rig as UnityEngine.Object, rig.RigTransform, point, point.transform },
                actionName);
        }

        public static void MarkRigAndPointsChanged(IUIMeshRigOwner rig)
        {
            MarkChanged(rig as UnityEngine.Object, rig.RigTransform);
            IReadOnlyList<UIMeshRigPoint> points = rig.RigPoints;
            for (int index = 0; index < points.Count; index++)
            {
                MarkChanged(points[index], points[index].transform);
            }
        }

        internal static void SetRestCenterPreservingPose(
            IUIMeshRigOwner rig,
            UIMeshRigPoint point,
            Vector2 normalizedCenter)
        {
            if (rig == null || point == null)
            {
                return;
            }

            point.SetRestCenterNormalized(normalizedCenter);
            if (rig.AuthoringMode == UIMeshRigAuthoringMode.Setup &&
                !UIMeshRigMotionInspector.IsPreviewing(point))
            {
                point.transform.position = rig.NormalizedToWorld(point.RestCenterNormalized);
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
        public static void Draw(UIMeshRigPoint point)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Preset", EditorStyles.boldLabel);
            UIMeshRigPointMotion motion = point.GetComponent<UIMeshRigPointMotion>();
            if (motion == null)
            {
                EditorGUILayout.HelpBox(
                    "Add a ready-to-preview motion, or leave this point for Animator/manual posing.",
                    MessageType.Info);
                if (GUILayout.Button("Add Breathe Motion & Preview", GUILayout.Height(26f)))
                {
                    motion = Undo.AddComponent<UIMeshRigPointMotion>(point.gameObject);
                    Undo.RecordObject(motion, "Configure UI mesh rig point motion");
                    motion.ApplyPreset(UIMeshRigMotionPreset.Breathe);
                    UIMeshRigEditorUtility.MarkChanged(motion);
                    UIMeshRigMotionPreviewDriver.StartPreview(motion);
                    GUIUtility.ExitGUI();
                }

                return;
            }

            EditorGUILayout.LabelField(
                "Motion preset, curves and timing are edited on the Mesh Rig Point Motion component " +
                "(same GameObject) with the standard inspector.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(GetPresetHint(motion.Preset), EditorStyles.wordWrappedMiniLabel);
            DrawTransport(motion, point);
        }

        public static void DrawTransport(UIMeshRigPointMotion motion, UIMeshRigPoint point)
        {
            if (motion == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Preview", GetPreviewStatus(motion), EditorStyles.miniLabel);

            // WHY: the guarantee used to hide inside one preset hint, so it was invisible for every other
            // preset — and it is the single thing a user needs to trust before pressing Start.
            EditorGUILayout.LabelField(
                "Preview is transient: it never writes the point Transform and never changes the saved " +
                "authoring mode. Stop Preview restores the pose exactly as it was.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!Application.isPlaying && motion.PreviewInEditMode))
            {
                if (GUILayout.Button("Start Preview"))
                {
                    UIMeshRigMotionPreviewDriver.StartPreview(motion);
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button(motion.IsPaused ? "Resume" : "Pause"))
            {
                if (motion.IsPaused)
                {
                    UIMeshRigMotionPreviewDriver.ResumePreview(motion);
                }
                else
                {
                    UIMeshRigMotionPreviewDriver.PausePreview(motion);
                }

                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Restart Preview"))
            {
                UIMeshRigMotionPreviewDriver.StartPreview(motion);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Stop Preview"))
            {
                UIMeshRigMotionPreviewDriver.StopPreview(motion);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string GetPreviewStatus(UIMeshRigPointMotion motion)
        {
            if (Application.isPlaying)
            {
                return "Play Mode drives the motion; Edit Mode preview is off.";
            }

            if (!motion.PreviewInEditMode)
            {
                return "Stopped.";
            }

            return motion.IsPaused
                ? "Paused at " + motion.CurrentTime.ToString("F2") + " s."
                : "Playing — " + motion.CurrentTime.ToString("F2") + " s.";
        }

        public static string GetPresetHint(UIMeshRigMotionPreset preset)
        {
            switch (preset)
            {
                case UIMeshRigMotionPreset.Wave:
                    return "Travelling wave: each point's normalized position offsets the cycle, so motion runs across the rig.";
                case UIMeshRigMotionPreset.Noise:
                    return "Smooth seeded noise: continuous and deterministic, with a different path for every point.";
                case UIMeshRigMotionPreset.SoftJiggle:
                    return "A damped one-cycle settle for impacts and UI reactions.";
                case UIMeshRigMotionPreset.SquashStretch:
                    return "Opposing X/Y scale with a small vertical bounce.";
                case UIMeshRigMotionPreset.Custom:
                    return "Editable position, rotation and scale curves in the Motion Profile.";
                default:
                    return "A looping idle built from the shared position, rotation and scale curves.";
            }
        }

        /// <summary>
        /// True while a transient Edit Mode preview owns the point, paused included. Anything that would
        /// write the point's authored Transform must ask this first — a paused preview still holds a
        /// procedural pose, so the visible position is not the authored one.
        /// </summary>
        public static bool IsPreviewing(UIMeshRigPoint point)
        {
            UIMeshRigPointMotion motion = point != null ? point.GetComponent<UIMeshRigPointMotion>() : null;
            return !Application.isPlaying && motion != null && motion.PreviewInEditMode;
        }

        /// <summary>
        /// True only while the preview is actually advancing. Repaint loops key off this instead of
        /// <see cref="IsPreviewing" />, so a paused or stopped preview costs nothing.
        /// </summary>
        public static bool IsPreviewPlaying(UIMeshRigPoint point)
        {
            UIMeshRigPointMotion motion = point != null ? point.GetComponent<UIMeshRigPointMotion>() : null;
            return !Application.isPlaying && motion != null && motion.PreviewInEditMode &&
                   motion.IsPlaying && !motion.IsPaused;
        }
    }
}
