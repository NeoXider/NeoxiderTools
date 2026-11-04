using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.UI.Editor
{
    /// <summary>
    /// World-space (MeshFilter/MeshRenderer) rig inspector. Fields come from the component's own
    /// <c>[Header]</c> / <c>[Tooltip]</c> attributes through <see cref="CustomEditorBase"/>; only buttons,
    /// diagnostics and the authoring toolbar are drawn here.
    /// </summary>
    [CustomEditor(typeof(UIMeshRigWorldRenderer))]
    public sealed class UIMeshRigWorldRendererEditor : CustomEditorBase
    {
        private UIMeshRigPoint _selectedPoint;
        private UIMeshRigLayoutPreset _layoutPreset = UIMeshRigLayoutPreset.FlagCloth;

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
            UIMeshRigWorldRenderer rig = (UIMeshRigWorldRenderer)target;

            if (GUILayout.Button("Set Native Size"))
            {
                Undo.RecordObject(rig, "Set world UI mesh rig native size");
                rig.SetNativeSize();
                UIMeshRigEditorUtility.MarkChanged(rig);
            }

            UIMeshRigOwnerInspector.DrawQuickStart(rig, ref _layoutPreset);
            UIMeshRigOwnerInspector.DrawDiagnostics(
                rig, rig.Sprite, rig.Columns, rig.Rows, "MeshFilter + MeshRenderer");
            UIMeshRigOwnerInspector.DrawAuthoringControls(rig);
            _selectedPoint = UIMeshRigOwnerInspector.DrawPointList(rig, _selectedPoint);
        }

        private void OnSceneGUI()
        {
            UIMeshRigWorldRenderer rig = (UIMeshRigWorldRenderer)target;
            _selectedPoint = UIMeshRigSceneHandles.Draw(rig, _selectedPoint);
        }
    }

    /// <summary>
    /// Inspector for the plain <see cref="SpriteRenderer"/> adapter. The extra block explains the one thing
    /// no field can: the rendered sprite is a runtime clone, so the imported asset stays untouched.
    /// </summary>
    [CustomEditor(typeof(UIMeshRigSpriteRenderer))]
    public sealed class UIMeshRigSpriteRendererEditor : CustomEditorBase
    {
        private UIMeshRigPoint _selectedPoint;
        private UIMeshRigLayoutPreset _layoutPreset = UIMeshRigLayoutPreset.SimpleBounce;

        // WHY: the native size line and the draw-mode warning are rebuilt only when their value changes; the
        // inspector repaints far more often than either changes.
        private bool _hasNativeSizeText;
        private Vector2 _shownNativeSize;
        private string _nativeSizeText;
        private SpriteDrawMode _shownDrawMode;
        private string _drawModeWarning;

        protected override string NeoxiderModuleName => "UI Mesh Rig";

        /// <summary>"x, y world units" text for <paramref name="nativeSize" />, cached until the size changes.</summary>
        internal string GetNativeSizeText(Vector2 nativeSize)
        {
            if (!_hasNativeSizeText || nativeSize != _shownNativeSize)
            {
                _hasNativeSizeText = true;
                _shownNativeSize = nativeSize;
                _nativeSizeText = nativeSize.ToString("F3") + " world units";
            }

            return _nativeSizeText;
        }

        /// <summary>Warning for a non-Simple <paramref name="drawMode" />, cached until the mode changes.</summary>
        internal string GetDrawModeWarning(SpriteDrawMode drawMode)
        {
            if (_drawModeWarning == null || drawMode != _shownDrawMode)
            {
                _shownDrawMode = drawMode;
                _drawModeWarning = "SpriteRenderer Draw Mode is " + drawMode + ". Sliced and Tiled modes " +
                                   "regenerate their own geometry, so the rig deformation is discarded. Use Simple.";
            }

            return _drawModeWarning;
        }

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
            UIMeshRigSpriteRenderer rig = (UIMeshRigSpriteRenderer)target;

            EditorGUILayout.HelpBox(
                "The SpriteRenderer draws a runtime clone of the source Sprite. The imported asset is never " +
                "modified, so stopping Play Mode cannot leave a deformed sprite in the project.",
                MessageType.Info);

            UIMeshRigOwnerInspector.DrawQuickStart(rig, ref _layoutPreset);
            UIMeshRigOwnerInspector.DrawDiagnostics(
                rig, rig.Sprite, rig.Columns, rig.Rows, "SpriteRenderer (cloned Sprite geometry)");
            if (rig.Sprite != null)
            {
                EditorGUILayout.LabelField("Native size", GetNativeSizeText(rig.NativeSize));
            }

            SpriteRenderer spriteRenderer = rig.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                EditorGUILayout.HelpBox(GetDrawModeWarning(spriteRenderer.drawMode), MessageType.Warning);
            }

            UIMeshRigOwnerInspector.DrawAuthoringControls(rig);
            _selectedPoint = UIMeshRigOwnerInspector.DrawPointList(rig, _selectedPoint);
        }

        private void OnSceneGUI()
        {
            UIMeshRigSpriteRenderer rig = (UIMeshRigSpriteRenderer)target;
            _selectedPoint = UIMeshRigSceneHandles.Draw(rig, _selectedPoint);
        }
    }

    [CustomEditor(typeof(UIMeshRigUIToolkitHost))]
    public sealed class UIMeshRigUIToolkitHostEditor : CustomEditorBase
    {
        protected override string NeoxiderModuleName => "UI Mesh Rig";

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void OnAfterDrawNeoProperties()
        {
            UIMeshRigUIToolkitHost host = (UIMeshRigUIToolkitHost)target;

            switch (host.HostKind)
            {
                case UIMeshRigPanelHostKind.PanelRenderer:
                    EditorGUILayout.HelpBox(
                        "Bound to PanelRenderer, the Unity 6.5+ world-space UI Toolkit renderer. The element " +
                        "is added to the root PanelRenderer hands out on every UI reload.",
                        MessageType.Info);
                    break;
                case UIMeshRigPanelHostKind.UIDocument:
                    EditorGUILayout.HelpBox(
                        "Bound to UIDocument. This is the fallback for editors without PanelRenderer; on " +
                        "Unity 6.5+ add a PanelRenderer instead and this host binds to it automatically.",
                        MessageType.Info);
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        "No UI Toolkit panel on this GameObject. Add a PanelRenderer (Unity 6.5+) or a " +
                        "UIDocument, otherwise the element is built but never shown.",
                        MessageType.Warning);
                    break;
            }

            if (host.HostKind == UIMeshRigPanelHostKind.PanelRenderer && !host.IsAttached)
            {
                EditorGUILayout.HelpBox(
                    "PanelRenderer has not delivered a root yet. Assign its UXML source — the reload " +
                    "callback is the only way this host can reach the panel.",
                    MessageType.Warning);
            }

            EditorGUILayout.LabelField("Attached", host.IsAttached ? "Yes" : "No");
            EditorGUILayout.LabelField(
                "In UXML / UI Builder use the element directly: Library > Custom Controls > Neoxider > UI Mesh Rig.",
                EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Refresh Element"))
            {
                host.Refresh();
                UIMeshRigEditorUtility.MarkChanged(host);
            }
        }
    }

    /// <summary>
    /// Kept as the world-space entry point used by the creation menu. The implementation is the shared,
    /// owner-generic one — the duplicate world-only copy of every helper is gone.
    /// </summary>
    internal static class UIMeshRigWorldEditorUtility
    {
        public static UIMeshRigPoint ApplyLayout(
            UIMeshRigWorldRenderer rig,
            UIMeshRigLayoutPreset preset,
            bool previewInEditMode)
        {
            return UIMeshRigEditorUtility.ApplyLayout(rig, preset, previewInEditMode);
        }

        public static UIMeshRigPoint DuplicatePoint(UIMeshRigWorldRenderer rig, UIMeshRigPoint source)
        {
            return UIMeshRigEditorUtility.DuplicatePoint(rig, source);
        }

        public static void DeletePoint(UIMeshRigWorldRenderer rig, UIMeshRigPoint point)
        {
            UIMeshRigEditorUtility.DeletePoint(rig, point);
        }

        public static void RecordRig(UIMeshRigWorldRenderer rig, string actionName)
        {
            UIMeshRigEditorUtility.RecordRig(rig, actionName);
        }
    }
}
