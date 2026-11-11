using System.Collections.Generic;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.UI.Editor
{
    [InitializeOnLoad]
    internal static class UIMeshRigMotionPreviewDriver
    {
        /// <summary>
        /// Smallest gap between two preview steps. The motion presets are continuous — nothing ever
        /// sets IsPlaying back to false — so an active preview drives this callback for as long as
        /// it is on. Without a cap it ran at whatever rate the editor could manage and pegged a
        /// core; sixty steps a second is already smoother than the Scene view repaints.
        /// </summary>
        private const double MinStepSeconds = 1.0 / 60.0;

        private static readonly List<UIMeshRigPointMotion> ActiveMotions = new List<UIMeshRigPointMotion>();

        // WHY: stopping a preview raises EditModePreviewStateChanged, and its handler edits ActiveMotions.
        // Walking the live list by index therefore skipped entries or indexed past its end as soon as a
        // callback removed something. Every loop below runs over a copy so the callbacks cannot corrupt it.
        private static readonly List<UIMeshRigPointMotion> TickBuffer = new List<UIMeshRigPointMotion>();
        private static readonly List<UIMeshRigPointMotion> SelectionBuffer = new List<UIMeshRigPointMotion>();
        private static readonly List<IUIMeshRigOwner> AdvancedOwners = new List<IUIMeshRigOwner>();
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
            double elapsed = now - _lastTime;
            if (elapsed < MinStepSeconds)
            {
                return;
            }

            float deltaTime = Mathf.Clamp((float)elapsed, 0f, 0.1f);
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
                        AddAdvancedOwner(motion);
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
                RebuildAdvancedOwners();
                RefreshUpdateSubscription();
                if (previewed)
                {
                    // WHY: repaint is enough to animate the Scene view. QueuePlayerLoopUpdate used to be
                    // called here as well, which schedules another editor tick, which re-enters this
                    // callback, which queues another — a self-sustaining loop with no exit while a preview
                    // is on.
                    SceneView.RepaintAll();
                }
            }
        }

        // WHY: the world and SpriteRenderer rigs rebuild only in LateUpdate, which the editor does not run for
        // a preview tick, so a running preview showed a frozen mesh until the mouse moved. A Canvas rebuilds on
        // the repaint below; the other owners are rebuilt here, once per tick however many points advanced.
        private static void AddAdvancedOwner(UIMeshRigPointMotion motion)
        {
            UIMeshRigPoint point = motion.GetComponent<UIMeshRigPoint>();
            IUIMeshRigOwner owner = point != null ? point.BoundOwner : null;
            if (owner != null && !AdvancedOwners.Contains(owner))
            {
                AdvancedOwners.Add(owner);
            }
        }

        private static void RebuildAdvancedOwners()
        {
            for (int index = 0; index < AdvancedOwners.Count; index++)
            {
                UIMeshRigEditorUtility.RebuildNow(AdvancedOwners[index]);
            }

            AdvancedOwners.Clear();
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
    /// Edit Mode preview of a clip of the Animator above a rig, sampled through <see cref="AnimationMode"/> so
    /// every sampled property is restored exactly when it stops. Like <see cref="UIMeshRigMotionPreviewDriver"/>
    /// it ticks on <see cref="EditorApplication.update"/> only while a clip plays and stops on selection change,
    /// Play Mode, assembly reload and quit. Unity does not evaluate Animator clips outside Play Mode, so this
    /// is the way to see an Animator-driven rig move without pressing Play.
    /// </summary>
    [InitializeOnLoad]
    internal static class UIMeshRigClipPreviewDriver
    {
        private const double MinStepSeconds = 1.0 / 60.0;
        private const double ClipCacheSeconds = 1.0;

        private static readonly List<AnimationClip> ClipBuffer = new List<AnimationClip>();
        private static readonly AnimationClip[] NoClips = new AnimationClip[0];
        private static readonly string[] NoClipNames = new string[0];

        private static IUIMeshRigOwner _rig;
        private static Animator _animator;
        private static AnimationClip _clip;
        private static float _time;
        private static double _lastTime;
        private static bool _updateSubscribed;

        // WHY: the clip popup is drawn on every inspector event, and AnimationUtility.GetAnimationClips builds a
        // new array each call. The list is re-read when the controller changes or once a second while shown.
        private static RuntimeAnimatorController _cachedController;
        private static double _cachedAt;
        private static AnimationClip[] _cachedClips = NoClips;
        private static string[] _cachedClipNames = NoClipNames;

        static UIMeshRigClipPreviewDriver()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.quitting -= Shutdown;
            EditorApplication.quitting += Shutdown;
            Selection.selectionChanged -= HandleSelectionChanged;
            Selection.selectionChanged += HandleSelectionChanged;
        }

        internal static bool IsUpdateSubscribed => _updateSubscribed;
        internal static bool IsAnyPreviewRunning => _clip != null;
        internal static AnimationClip PreviewClip => _clip;
        internal static float PreviewTime => _time;

        internal static bool IsPreviewing(IUIMeshRigOwner rig)
        {
            return _clip != null && rig != null && ReferenceEquals(_rig, rig);
        }

        /// <summary>The Animator on the rig or its nearest ancestor, the one a clip of this rig is keyed from.</summary>
        internal static Animator FindAnimator(IUIMeshRigOwner rig)
        {
            Transform rigTransform = rig != null ? rig.RigTransform : null;
            return rigTransform != null ? rigTransform.GetComponentInParent<Animator>(true) : null;
        }

        /// <summary>Distinct clips of <paramref name="animator"/>'s controller, cached for the inspector.</summary>
        internal static AnimationClip[] GetClips(Animator animator)
        {
            RefreshClipCache(animator);
            return _cachedClips;
        }

        internal static string[] GetClipNames(Animator animator)
        {
            RefreshClipCache(animator);
            return _cachedClipNames;
        }

        /// <summary>
        /// Starts sampling <paramref name="clip"/> on the rig's Animator. Returns false outside Edit Mode, without
        /// an Animator, or while another Animation preview already owns AnimationMode.
        /// </summary>
        internal static bool StartPreview(IUIMeshRigOwner rig, AnimationClip clip)
        {
            StopPreview();
            Animator animator = FindAnimator(rig);
            if (animator == null || clip == null || Application.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.IsPersistent(animator) ||
                AnimationMode.InAnimationMode())
            {
                return false;
            }

            AnimationMode.StartAnimationMode();
            _rig = rig;
            _animator = animator;
            _clip = clip;
            _time = 0f;
            Sample();
            SetUpdateSubscribed(true);
            return true;
        }

        /// <summary>Stops the clip preview; AnimationMode restores every property the clip sampled.</summary>
        internal static void StopPreview()
        {
            IUIMeshRigOwner rig = _rig;
            bool wasPreviewing = _clip != null;
            _rig = null;
            _animator = null;
            _clip = null;
            _time = 0f;
            SetUpdateSubscribed(false);
            if (!wasPreviewing)
            {
                return;
            }

            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            if (rig is Object rigObject && rigObject != null)
            {
                UIMeshRigEditorUtility.RebuildNow(rig);
            }

            SceneView.RepaintAll();
        }

        /// <summary>Stops the preview unless the selection is still the previewed rig, its Animator or a point.</summary>
        internal static void StopIfOutsideSelection()
        {
            HandleSelectionChanged();
        }

        internal static void TickForTests(float deltaTime)
        {
            Tick(Mathf.Max(0f, deltaTime));
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            double elapsed = now - _lastTime;
            if (elapsed < MinStepSeconds)
            {
                return;
            }

            _lastTime = now;
            Tick(Mathf.Clamp((float)elapsed, 0f, 0.1f));
        }

        private static void Tick(float deltaTime)
        {
            if (_clip == null)
            {
                SetUpdateSubscribed(false);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || _animator == null ||
                !(_rig is Object rigObject) || rigObject == null || !AnimationMode.InAnimationMode())
            {
                StopPreview();
                return;
            }

            _time += deltaTime;
            Sample();
        }

        private static void Sample()
        {
            float length = _clip.length;
            float sampleTime = length > 0f ? Mathf.Repeat(_time, length) : 0f;
            AnimationMode.BeginSampling();
            try
            {
                AnimationMode.SampleAnimationClip(_animator.gameObject, _clip, sampleTime);
            }
            finally
            {
                AnimationMode.EndSampling();
            }

            UIMeshRigEditorUtility.RebuildNow(_rig);
            SceneView.RepaintAll();
        }

        private static void SetUpdateSubscribed(bool subscribed)
        {
            if (subscribed == _updateSubscribed)
            {
                return;
            }

            EditorApplication.update -= Update;
            if (subscribed)
            {
                _lastTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += Update;
            }

            _updateSubscribed = subscribed;
        }

        private static void RefreshClipCache(Animator animator)
        {
            RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
            double now = EditorApplication.timeSinceStartup;
            if (controller == _cachedController && now - _cachedAt < ClipCacheSeconds)
            {
                return;
            }

            _cachedController = controller;
            _cachedAt = now;
            ClipBuffer.Clear();
            AnimationClip[] clips = animator != null ? AnimationUtility.GetAnimationClips(animator.gameObject) : NoClips;
            for (int index = 0; index < clips.Length; index++)
            {
                AnimationClip clip = clips[index];
                if (clip != null && !ClipBuffer.Contains(clip))
                {
                    ClipBuffer.Add(clip);
                }
            }

            if (ClipBuffer.Count == 0)
            {
                _cachedClips = NoClips;
                _cachedClipNames = NoClipNames;
                return;
            }

            _cachedClips = ClipBuffer.ToArray();
            _cachedClipNames = new string[_cachedClips.Length];
            for (int index = 0; index < _cachedClips.Length; index++)
            {
                _cachedClipNames[index] = _cachedClips[index].name;
            }

            ClipBuffer.Clear();
        }

        private static void HandleSelectionChanged()
        {
            if (_clip == null)
            {
                return;
            }

            Transform rigTransform = _rig is Object rigObject && rigObject != null ? _rig.RigTransform : null;
            Transform selected = Selection.activeTransform;
            bool ownsSelection = rigTransform != null && selected != null &&
                                 (selected.IsChildOf(rigTransform) ||
                                  (_animator != null && selected == _animator.transform));
            if (!ownsSelection)
            {
                StopPreview();
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                StopPreview();
            }
        }

        private static void Shutdown()
        {
            StopPreview();
            EditorApplication.update -= Update;
            _updateSubscribed = false;
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
        private int _clipPreviewIndex;

        protected override string NeoxiderModuleName => "UI Mesh Rig";

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void OnDisable()
        {
            UIMeshRigMotionPreviewDriver.StopPreviewsOutsideSelection();
            UIMeshRigClipPreviewDriver.StopIfOutsideSelection();
            UnityEditor.Tools.hidden = false;
            base.OnDisable();
        }

        protected override void OnAfterDrawNeoProperties()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)target;
            UIMeshRigOwnerInspector.DrawQuickStart(rig, ref _layoutPreset);
            UIMeshRigOwnerInspector.DrawDiagnostics(rig, rig.Sprite, rig.Columns, rig.Rows, "Canvas mesh (uGUI)");
            UIMeshRigOwnerInspector.DrawSpriteWarnings(rig.Sprite, null);
            UIMeshRigOwnerInspector.DrawAuthoringControls(rig);
            UIMeshRigOwnerInspector.DrawClipPreview(rig, ref _clipPreviewIndex);
            UnityEditor.Tools.hidden = rig.AuthoringMode == UIMeshRigAuthoringMode.Setup;
            _selectedPoint = UIMeshRigOwnerInspector.DrawPointList(rig, _selectedPoint);
            if (UIMeshRigClipPreviewDriver.IsPreviewing(rig))
            {
                EnsureLiveRepaint();
            }
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
        private static readonly GUIContent ApplyFalloffPresetContent = new GUIContent("Apply Falloff Preset",
            "Rebuilds the Falloff Curve from the preset chosen above.");

        private static readonly GUIContent FullSmoothContent = new GUIContent("Full Smooth From Center",
            "Inner = 0, smooth falloff.");

        private static readonly GUIContent HalfInnerContent = new GUIContent("Inner = 50% of Outer",
            "Balanced solid core plus soft edge.");

        protected override string NeoxiderModuleName => "UI Mesh Rig";

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void OnDisable()
        {
            UIMeshRigMotionPreviewDriver.StopPreviewsOutsideSelection();
            UIMeshRigClipPreviewDriver.StopIfOutsideSelection();
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
            if (GUILayout.Button(ApplyFalloffPresetContent))
            {
                Undo.RecordObject(point, "Apply UI mesh rig falloff preset");
                point.ApplyFalloffPreset(point.FalloffPreset);
                UIMeshRigEditorUtility.MarkChanged(point);
            }

            if (GUILayout.Button(FullSmoothContent))
            {
                Undo.RecordObject(point, "Use full smooth UI mesh rig falloff");
                point.UseFullSmoothFalloff();
                UIMeshRigEditorUtility.MarkChanged(point);
            }

            if (GUILayout.Button(HalfInnerContent))
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

            // WHY: no SceneView.RepaintAll() from OnInspectorGUI. While this inspector kept redrawing itself it
            // made the Inspector and the Scene view repaint each other for as long as a motion component
            // existed — including while the preview was paused or stopped. The preview driver repaints the
            // Scene view exactly on the frames it actually advanced; the inspector only takes the capped
            // EnsureLiveRepaint lease below while a preview plays.
            UIMeshRigMotionInspector.Draw(point);

            UIMeshRigOwnerInspector.DrawAuthoringControls(owner);
            UnityEditor.Tools.hidden = owner.AuthoringMode == UIMeshRigAuthoringMode.Setup;
            UIMeshRigOwnerInspector.DrawPointActions(owner, point);

            if (UIMeshRigMotionInspector.IsPreviewPlaying(point))
            {
                // WHY: the Inspector only shows the preview clock; the Scene view is animated by the driver.
                // RequiresConstantRepaint redrew this whole inspector on every editor tick for that one
                // label, while the shared live lease refreshes it at a capped rate and stops when hidden.
                EnsureLiveRepaint();
            }
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

        // WHY: the Preset field only stored the enum. Its tooltip promised that choosing a preset applies it
        // and starts the preview, yet the profile kept the previous curves. The change is caught here, in the
        // GUI event that made it, so loading a scene, an undo or a curve edit never re-applies a preset over
        // hand-tuned curves the way an OnValidate check would.
        protected override bool DrawCustomProperty(SerializedProperty property)
        {
            if (property == null || property.propertyPath != PresetPropertyPath)
            {
                return false;
            }

            EditorGUI.BeginChangeCheck();
            DrawStandardProperty(property);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyInspectorPresetSelection(property.serializedObject);
            }

            return true;
        }

        internal const string PresetPropertyPath = "_preset";

        /// <summary>
        /// Applies the Preset value just picked in <paramref name="serializedObject"/> to every selected motion:
        /// copies the preset curves into its profile (Custom keeps the curves) and starts the Edit Mode preview.
        /// </summary>
        internal static void ApplyInspectorPresetSelection(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            Object[] targets = serializedObject.targetObjects;
            for (int index = 0; index < targets.Length; index++)
            {
                UIMeshRigPointMotion motion = targets[index] as UIMeshRigPointMotion;
                if (motion == null || motion.Preset == UIMeshRigMotionPreset.Custom)
                {
                    continue;
                }

                Undo.RecordObject(motion, "Apply UI mesh rig motion preset");
                motion.ApplyPreset(motion.Preset);
                UIMeshRigEditorUtility.MarkChanged(motion);
                if (!Application.isPlaying && motion.isActiveAndEnabled)
                {
                    UIMeshRigMotionPreviewDriver.StartPreview(motion);
                }
            }

            serializedObject.Update();
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

            if (!Application.isPlaying && motion.PreviewInEditMode && motion.IsPlaying && !motion.IsPaused)
            {
                // WHY: see UIMeshRigPointEditor - a capped live lease instead of a per-tick constant repaint.
                EnsureLiveRepaint();
            }
        }
    }

    /// <summary>
    /// Inspector blocks shared by every rig adapter. One implementation, so the uGUI, world, SpriteRenderer
    /// and UI Toolkit inspectors cannot drift apart the way three hand-written copies did.
    /// </summary>
    internal static class UIMeshRigOwnerInspector
    {
        private static readonly GUIContent LayoutContent = new GUIContent("Layout",
            "Simple Bounce uses one point; Character adds Root/Torso/Chest/Head; Flag / Cloth adds a travelling wave.");

        // WHY: the duplicate-key check runs on every inspector event; one reused set replaces a new HashSet per draw.
        private static readonly HashSet<string> BindingKeyBuffer = new HashSet<string>();

        // WHY: shared by the Inspector toolbar and the Scene overlay, which used to allocate both label
        // arrays on every event.
        internal static readonly string[] ModeToolbarLabels = { "Setup", "Pose / Animate" };
        internal static readonly string[] PoseToolLabels = { "Move", "Rotate", "Scale" };

        internal const string AnimationPreviewHint =
            "An Animation window or Timeline preview is running. The rig deforms as in Pose / Animate, Setup does " +
            "not rebind moved points, bind handles are hidden, and Pose / Animate cannot be left (that would key a " +
            "reset of every point) until the preview stops. Record keys in Pose / Animate.";

        public static void DrawQuickStart(IUIMeshRigOwner rig, ref UIMeshRigLayoutPreset preset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ready-Made Rig", EditorStyles.boldLabel);
            preset = (UIMeshRigLayoutPreset)EditorGUILayout.EnumPopup(LayoutContent, preset);
            if (GUILayout.Button("Apply Layout & Preview", GUILayout.Height(28f)))
            {
                UIMeshRigEditorUtility.ApplyLayoutAsUndoStep(rig, preset, true);
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

            HashSet<string> bindingKeys = BindingKeyBuffer;
            bindingKeys.Clear();
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
            bool animationPreview = UIMeshRigAuthoring.IsAnimationPreviewActive();
            if (animationPreview)
            {
                EditorGUILayout.HelpBox(AnimationPreviewHint, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(UIMeshRigEditorUtility.IsModeSwitchLocked(rig)))
            {
                UIMeshRigAuthoringMode requestedMode = (UIMeshRigAuthoringMode)GUILayout.Toolbar(
                    (int)rig.AuthoringMode,
                    UIMeshRigOwnerInspector.ModeToolbarLabels);
                if (requestedMode != rig.AuthoringMode)
                {
                    UIMeshRigEditorUtility.RecordRig(rig, "Change UI mesh rig mode");
                    rig.SetAuthoringMode(requestedMode);
                    UIMeshRigEditorUtility.MarkRigAndPointsChanged(rig);
                }
            }

            if (rig.AuthoringMode == UIMeshRigAuthoringMode.Pose)
            {
                UIMeshRigSceneTool currentTool = UIMeshRigEditorUtility.GetSceneTool(rig);
                UIMeshRigSceneTool requestedTool = (UIMeshRigSceneTool)GUILayout.Toolbar(
                    (int)currentTool,
                    UIMeshRigOwnerInspector.PoseToolLabels);
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

            // WHY: capturing during an Animation preview binds the sampled pose, and AnimationMode then puts the
            // Transforms back on stop, so the bind and the restored points no longer agree.
            using (new EditorGUI.DisabledScope(animationPreview))
            {
                if (GUILayout.Button("Capture Rest Pose"))
                {
                    UIMeshRigEditorUtility.RecordRig(rig, "Capture UI mesh rig rest pose");
                    rig.CaptureRestPose();
                    UIMeshRigEditorUtility.MarkRigAndPointsChanged(rig);
                }
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

        /// <summary>
        /// Clip popup and Preview / Stop button for the Animator above the rig. Draws nothing without an Animator
        /// or in Play Mode, where the Animator plays the clip itself.
        /// </summary>
        public static void DrawClipPreview(IUIMeshRigOwner rig, ref int clipIndex)
        {
            if (Application.isPlaying)
            {
                return;
            }

            Animator animator = UIMeshRigClipPreviewDriver.FindAnimator(rig);
            if (animator == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animator Clip Preview", EditorStyles.boldLabel);
            AnimationClip[] clips = UIMeshRigClipPreviewDriver.GetClips(animator);
            if (clips.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "The Animator above this rig has no clips. Assign a controller with states to preview them here.",
                    MessageType.Info);
                return;
            }

            bool previewing = UIMeshRigClipPreviewDriver.IsPreviewing(rig);
            clipIndex = Mathf.Clamp(clipIndex, 0, clips.Length - 1);
            using (new EditorGUI.DisabledScope(previewing))
            {
                clipIndex = EditorGUILayout.Popup("Clip", clipIndex, UIMeshRigClipPreviewDriver.GetClipNames(animator));
            }

            if (previewing)
            {
                EditorGUILayout.LabelField(
                    "Playing",
                    UIMeshRigClipPreviewDriver.PreviewTime.ToString("F2") + " s",
                    EditorStyles.miniLabel);
                if (GUILayout.Button("Stop Clip Preview"))
                {
                    UIMeshRigClipPreviewDriver.StopPreview();
                }

                return;
            }

            bool otherPreview = UIMeshRigAuthoring.IsAnimationPreviewActive();
            using (new EditorGUI.DisabledScope(otherPreview))
            {
                if (GUILayout.Button("Preview Clip"))
                {
                    UIMeshRigClipPreviewDriver.StartPreview(rig, clips[clipIndex]);
                }
            }

            EditorGUILayout.LabelField(
                otherPreview
                    ? "Another Animation window or Timeline preview is running. Stop it to preview here."
                    : "Samples the clip in Edit Mode without pressing Play. Stopping restores every animated property.",
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>
        /// Sprite and renderer setups the rig draws differently from what the artwork suggests, explained where
        /// the rig is configured.
        /// </summary>
        public static void DrawSpriteWarnings(Sprite sprite, SpriteRenderer spriteRenderer)
        {
            if (sprite != null && sprite.packed &&
                (sprite.packingMode == SpritePackingMode.Tight || sprite.packingRotation != SpritePackingRotation.None))
            {
                EditorGUILayout.HelpBox(
                    "The sprite is packed tightly or rotated in a Sprite Atlas. The rig draws one rectangle of the " +
                    "atlas, so neighbouring sprites can show through, and a Sprite Renderer rig draws it undeformed. " +
                    "Pack rig sprites as Full Rect without rotation.",
                    MessageType.Warning);
            }

            if (spriteRenderer != null && (spriteRenderer.flipX || spriteRenderer.flipY))
            {
                EditorGUILayout.HelpBox(
                    "Flip X / Flip Y mirror the drawn sprite but not the points, so the deformation and the handles " +
                    "appear on the mirrored side. Flip the rig with a negative Transform scale to mirror the points too.",
                    MessageType.Warning);
            }
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

        // WHY: every Scene view event used to allocate a 65-point array per ellipse (two per point) and two
        // toolbar label arrays. The ellipse is written into one shared buffer; Handles copies it on draw.
        private static readonly Vector3[] EllipseBuffer = new Vector3[EllipseSegments + 1];

        private static readonly GUIContent LabelsToggleContent = new GUIContent("Labels",
            "Off: only the selected point is named, so a dense rig stays readable.");

        private static readonly GUIContent RingsToggleContent = new GUIContent("All rings",
            "Off: unselected points draw one faint outer ring instead of two solid ellipses.");

        // WHY: EditorPrefs is backed by the registry on Windows, and these two flags were read once per point
        // per Scene view event. They are read once per domain and written only when toggled.
        private static bool? _showAllLabels;
        private static bool? _showAllRings;

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
            // the pose gizmo is simply added on top while posing. The one exception is an Animation preview:
            // a bind edit there is keyed into the clip while recording, and in Setup it moves a Transform that
            // AnimationMode then restores, leaving the point off its new bind.
            if (!UIMeshRigAuthoring.IsAnimationPreviewActive())
            {
                DrawSetupHandles(rig, selectedPoint);
            }
            if (rig.AuthoringMode == UIMeshRigAuthoringMode.Pose)
            {
                DrawPoseHandle(rig, selectedPoint);
            }

            return selectedPoint;
        }

        internal static bool ShowAllLabels
        {
            get
            {
                _showAllLabels ??= EditorPrefs.GetBool(ShowAllLabelsKey, false);
                return _showAllLabels.Value;
            }
            set
            {
                if (_showAllLabels == value)
                {
                    return;
                }

                _showAllLabels = value;
                EditorPrefs.SetBool(ShowAllLabelsKey, value);
            }
        }

        internal static bool ShowAllRings
        {
            get
            {
                _showAllRings ??= EditorPrefs.GetBool(ShowAllRingsKey, false);
                return _showAllRings.Value;
            }
            set
            {
                if (_showAllRings == value)
                {
                    return;
                }

                _showAllRings = value;
                EditorPrefs.SetBool(ShowAllRingsKey, value);
            }
        }

        /// <summary>Drops the cached Scene-overlay toggles so the next read comes from <see cref="EditorPrefs" />.</summary>
        internal static void ReloadOverlayPreferences()
        {
            _showAllLabels = null;
            _showAllRings = null;
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

            using (new EditorGUI.DisabledScope(UIMeshRigEditorUtility.IsModeSwitchLocked(rig)))
            {
                UIMeshRigAuthoringMode requestedMode = (UIMeshRigAuthoringMode)GUILayout.Toolbar(
                    (int)rig.AuthoringMode,
                    UIMeshRigOwnerInspector.ModeToolbarLabels);
                if (requestedMode != rig.AuthoringMode)
                {
                    UIMeshRigEditorUtility.RecordRig(rig, "Change UI mesh rig mode");
                    rig.SetAuthoringMode(requestedMode);
                    UIMeshRigEditorUtility.MarkRigAndPointsChanged(rig);
                }
            }

            if (rig.AuthoringMode == UIMeshRigAuthoringMode.Pose)
            {
                UIMeshRigSceneTool currentTool = UIMeshRigEditorUtility.GetSceneTool(rig);
                UIMeshRigSceneTool requestedTool = (UIMeshRigSceneTool)GUILayout.Toolbar(
                    (int)currentTool,
                    UIMeshRigOwnerInspector.PoseToolLabels);
                if (requestedTool != currentTool)
                {
                    Undo.RecordObject(rig as Object, "Change UI mesh rig scene tool");
                    UIMeshRigEditorUtility.SetSceneTool(rig, requestedTool);
                    UIMeshRigEditorUtility.MarkChanged(rig as Object);
                }
            }

            GUILayout.BeginHorizontal();
            ShowAllLabels = GUILayout.Toggle(ShowAllLabels, LabelsToggleContent, EditorStyles.miniButton);
            ShowAllRings = GUILayout.Toggle(ShowAllRings, RingsToggleContent, EditorStyles.miniButton);
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
            Vector3[] ellipsePoints = EllipseBuffer;
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
            UIMeshRigSceneTool tool = UIMeshRigEditorUtility.GetSceneTool(rig);
            EditorGUI.BeginChangeCheck();
            Vector3 position = pointTransform.position;
            Quaternion rotation = pointTransform.rotation;
            Vector3 scale = pointTransform.localScale;

            switch (tool)
            {
                case UIMeshRigSceneTool.Rotate:
                    // WHY: the rig reads only the rotation about its own forward axis. The free rotation handle
                    // tilted points out of the plane and keyed X/Y rotation channels the mesh never uses.
                    rotation = Handles.Disc(
                        pointTransform.rotation,
                        pointTransform.position,
                        rig.RigTransform.forward,
                        HandleUtility.GetHandleSize(pointTransform.position),
                        false,
                        0f);
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

            ApplyPoseHandle(rig, point, tool, position, rotation, scale);
        }

        /// <summary>
        /// Writes the result of a pose handle into the point Transform, touching only the channel of
        /// <paramref name="tool"/> and only the axes that actually moved, so Animation recording keys exactly
        /// what the user dragged.
        /// </summary>
        // WHY: position, rotation and scale used to be re-assigned together after any drag. The round trip
        // through world space added float noise to untouched channels, and recording turned that noise into
        // keys on properties the user never moved.
        internal static void ApplyPoseHandle(
            IUIMeshRigOwner rig,
            UIMeshRigPoint point,
            UIMeshRigSceneTool tool,
            Vector3 handlePosition,
            Quaternion handleRotation,
            Vector3 handleScale)
        {
            Transform pointTransform = point.transform;
            UIMeshRigEditorUtility.RecordPointAndRig(rig, point, "Pose UI mesh rig point");
            switch (tool)
            {
                case UIMeshRigSceneTool.Rotate:
                    ApplyPlanarRotation(rig, pointTransform, handleRotation);
                    break;
                case UIMeshRigSceneTool.Scale:
                    Vector3 currentScale = pointTransform.localScale;
                    pointTransform.localScale = new Vector3(
                        HasMoved(currentScale.x, handleScale.x) ? handleScale.x : currentScale.x,
                        HasMoved(currentScale.y, handleScale.y) ? handleScale.y : currentScale.y,
                        currentScale.z);
                    break;
                default:
                    ApplyPlanarMove(pointTransform, handlePosition);
                    break;
            }

            pointTransform.hasChanged = true;
            rig.NotifyPoseChanged();
            UIMeshRigEditorUtility.MarkChanged(rig as Object, point, pointTransform);
        }

        private static void ApplyPlanarMove(Transform pointTransform, Vector3 handlePosition)
        {
            Transform parent = pointTransform.parent;
            Vector3 currentLocal = parent != null ? parent.InverseTransformPoint(pointTransform.position) : pointTransform.position;
            Vector3 movedLocal = parent != null ? parent.InverseTransformPoint(handlePosition) : handlePosition;
            float deltaX = HasMoved(currentLocal.x, movedLocal.x) ? movedLocal.x - currentLocal.x : 0f;
            float deltaY = HasMoved(currentLocal.y, movedLocal.y) ? movedLocal.y - currentLocal.y : 0f;
            if (deltaX == 0f && deltaY == 0f)
            {
                return;
            }

            if (pointTransform is RectTransform pointRect)
            {
                Vector2 anchored = pointRect.anchoredPosition;
                pointRect.anchoredPosition = new Vector2(anchored.x + deltaX, anchored.y + deltaY);
                return;
            }

            Vector3 localPosition = pointTransform.localPosition;
            pointTransform.localPosition = new Vector3(localPosition.x + deltaX, localPosition.y + deltaY, localPosition.z);
        }

        private static void ApplyPlanarRotation(IUIMeshRigOwner rig, Transform pointTransform, Quaternion handleRotation)
        {
            Quaternion worldDelta = handleRotation * Quaternion.Inverse(pointTransform.rotation);
            worldDelta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }

            Vector3 rigAxis = rig.RigTransform.forward;
            float signedAngle = Vector3.Dot(axis, rigAxis) < 0f ? -angle : angle;
            if (Mathf.Abs(signedAngle) <= 0.0001f || float.IsNaN(signedAngle))
            {
                return;
            }

            Transform parent = pointTransform.parent;
            Vector3 localAxis = parent == rig.RigTransform
                ? Vector3.forward
                : parent != null ? parent.InverseTransformDirection(rigAxis) : rigAxis;
            pointTransform.localRotation = Quaternion.AngleAxis(signedAngle, localAxis) * pointTransform.localRotation;
        }

        private static bool HasMoved(float current, float moved)
        {
            return Mathf.Abs(moved - current) > 0.00001f * Mathf.Max(1f, Mathf.Abs(current));
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

        /// <summary>
        /// Applies a layout as a standalone authoring command: it opens a new Undo group first, so it never
        /// absorbs an earlier unrelated edit. Used by the inspector's Apply Layout button.
        /// </summary>
        public static UIMeshRigPoint ApplyLayoutAsUndoStep(
            IUIMeshRigOwner rig,
            UIMeshRigLayoutPreset preset,
            bool previewInEditMode)
        {
            Undo.IncrementCurrentGroup();
            return ApplyLayout(rig, preset, previewInEditMode);
        }

        /// <summary>
        /// Replaces the rig's points with a preset layout inside the caller's current Undo group, so a menu command
        /// that creates or converts a rig and then applies its starter layout is reverted by a single Undo.
        /// </summary>
        /// <remarks>
        /// Standalone callers that must not merge with a preceding edit use <see cref="ApplyLayoutAsUndoStep" />.
        /// </remarks>
        public static UIMeshRigPoint ApplyLayout(
            IUIMeshRigOwner rig,
            UIMeshRigLayoutPreset preset,
            bool previewInEditMode)
        {
            UIMeshRigPoint[] existingPoints = rig.RigTransform.GetComponentsInChildren<UIMeshRigPoint>(true);
            for (int index = 0; index < existingPoints.Length; index++)
            {
                if (UIMeshRigOwnerResolver.OwnsPoint(rig, existingPoints[index]) &&
                    !CanDeletePoint(rig, existingPoints[index]))
                {
                    return null;
                }
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply UI mesh rig layout");
            RecordRig(rig, "Apply UI mesh rig layout");

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
            // RegisterCreatedObjectUndo stops earlier RecordObject tracking while points are created.
            Undo.RecordObject(rig as Object, "Apply UI mesh rig layout");
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
            UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
            // WHY: Redo restores a created object from its registration snapshot; placement, key and rest pose
            // assigned afterwards must be recorded or Redo brings back the default "Point" key and pose.
            Undo.RecordObjects(
                new UnityEngine.Object[] { pointObject, pointRect, point },
                "Configure UI mesh rig point");
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
            point.SetBindingKey(GetUniqueBindingKey(rig, point, pointName));
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
            Undo.RecordObjects(
                new UnityEngine.Object[] { pointObject, pointRect },
                "Configure UI mesh rig preset point");
            UIMeshRigLayoutBuilder.ApplyPointTransform(rig, pointRect, layout);
            pointObject.layer = rig.RigTransform.gameObject.layer;

            UIMeshRigPoint point = Undo.AddComponent<UIMeshRigPoint>(pointObject);
            Undo.RecordObject(point, "Configure UI mesh rig preset point");
            UIMeshRigLayoutBuilder.ConfigurePointOnOwner(rig, point, layout);
            point.SetBindingKey(GetUniqueBindingKey(rig, point, layout.Name));
            if (layout.MotionPreset != UIMeshRigMotionPreset.Custom)
            {
                UIMeshRigPointMotion motion = Undo.AddComponent<UIMeshRigPointMotion>(pointObject);
                Undo.RecordObject(motion, "Configure UI mesh rig preset motion");
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
            if (!UIMeshRigOwnerResolver.OwnsPoint(rig, source))
            {
                return null;
            }

            if (source.transform == rig.RigTransform)
            {
                Debug.LogWarning("[UI Mesh Rig] Cannot duplicate a point on the rig root. " +
                                 "Move the point to a child GameObject first.", source);
                return null;
            }

            Transform sourceTransform = source.transform;
            Transform parent = sourceTransform.parent;
            Vector3 localPosition = sourceTransform.localPosition;
            Quaternion localRotation = sourceTransform.localRotation;
            Vector3 localScale = sourceTransform.localScale;
            GameObject duplicateObject = UnityEngine.Object.Instantiate(
                source.gameObject, sourceTransform.position, sourceTransform.rotation, parent);
            duplicateObject.name = ObjectNames.GetUniqueName(GetSiblingNames(parent), source.gameObject.name);
            Undo.RegisterCreatedObjectUndo(duplicateObject, "Duplicate UI mesh rig point");
            Transform duplicateTransform = duplicateObject.transform;
            Undo.RegisterCompleteObjectUndo(duplicateTransform, "Preserve duplicated UI mesh rig pose");
            // Retain exact local TRS under the original rotated/scaled parent after native creation
            // bookkeeping. Recapturing the rest pose here would discard the authored deformation.
            duplicateTransform.localPosition = localPosition;
            duplicateTransform.localRotation = localRotation;
            duplicateTransform.localScale = localScale;
            UIMeshRigPoint duplicate = duplicateObject.GetComponent<UIMeshRigPoint>();
            rig.NotifyPointChanged();
            UIMeshRigPoint[] duplicatedPoints = duplicateObject.GetComponentsInChildren<UIMeshRigPoint>(true);
            for (int index = 0; index < duplicatedPoints.Length; index++)
            {
                UIMeshRigPoint duplicatedPoint = duplicatedPoints[index];
                if (UIMeshRigOwnerResolver.OwnsPoint(rig, duplicatedPoint))
                {
                    string requestedKey = duplicatedPoint == duplicate ? duplicateObject.name : duplicatedPoint.BindingKey;
                    Undo.RecordObject(duplicatedPoint, "Assign UI mesh rig binding key");
                    duplicatedPoint.SetBindingKey(GetUniqueBindingKey(rig, duplicatedPoint, requestedKey));
                    MarkChanged(duplicatedPoint);
                }
            }

            MarkChanged(rig as Object, duplicate, duplicate.transform);
            return duplicate;
        }

        public static void DeletePoint(IUIMeshRigOwner rig, UIMeshRigPoint point)
        {
            if (!CanDeletePoint(rig, point))
            {
                return;
            }

            Undo.RecordObject(rig as Object, "Delete UI mesh rig point");
            Undo.DestroyObjectImmediate(point.gameObject);
            rig.NotifyPointChanged();
            MarkChanged(rig as Object);
        }

        private static bool CanDeletePoint(IUIMeshRigOwner rig, UIMeshRigPoint point)
        {
            if (!UIMeshRigOwnerResolver.OwnsPoint(rig, point))
            {
                return false;
            }

            if (point.transform == rig.RigTransform)
            {
                Debug.LogWarning("[UI Mesh Rig] Cannot delete a point on the rig root. " +
                                 "Move the point to a child GameObject first.", point);
                return false;
            }

            Component[] descendants = point.GetComponentsInChildren<Component>(true);
            for (int index = 0; index < descendants.Length; index++)
            {
                if (descendants[index] is IUIMeshRigOwner nestedOwner && !ReferenceEquals(nestedOwner, rig))
                {
                    Debug.LogWarning("[UI Mesh Rig] Cannot delete a point containing a nested rig. " +
                                     "Move the nested rig outside the point hierarchy first.", point);
                    return false;
                }
            }

            return true;
        }

        private static string GetUniqueBindingKey(IUIMeshRigOwner rig, UIMeshRigPoint point, string requestedKey)
        {
            IReadOnlyList<UIMeshRigPoint> points = rig.RigPoints;
            List<string> keys = new List<string>();
            for (int index = 0; index < points.Count; index++)
            {
                UIMeshRigPoint existingPoint = points[index];
                if (existingPoint != null && existingPoint != point)
                {
                    keys.Add(existingPoint.BindingKey);
                }
            }

            return ObjectNames.GetUniqueName(keys.ToArray(), requestedKey);
        }

        public static void Focus(UIMeshRigPoint point)
        {
            if (point == null)
            {
                return;
            }

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

        /// <summary>
        /// True while leaving Pose / Animate must wait: SetAuthoringMode(Setup) resets every point, which an
        /// Animation recording keys into the clip and a plain preview silently undoes on stop.
        /// </summary>
        public static bool IsModeSwitchLocked(IUIMeshRigOwner rig)
        {
            return rig != null && rig.AuthoringMode == UIMeshRigAuthoringMode.Pose &&
                   UIMeshRigAuthoring.IsAnimationPreviewActive();
        }

        /// <summary>
        /// Rebuilds an owner that renders without a Canvas right away. A Canvas rebuilds on the next repaint
        /// by itself, so the uGUI graphic needs nothing here.
        /// </summary>
        public static void RebuildNow(IUIMeshRigOwner rig)
        {
            switch (rig)
            {
                case UIMeshRigWorldRenderer world when world != null && world.isActiveAndEnabled:
                    world.RebuildNow();
                    break;
                case UIMeshRigSpriteRenderer sprite when sprite != null && sprite.isActiveAndEnabled:
                    sprite.RebuildNow();
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
