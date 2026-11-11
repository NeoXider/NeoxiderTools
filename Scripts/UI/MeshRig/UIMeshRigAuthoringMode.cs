using UnityEngine;

namespace Neo.UI
{
    /// <summary>Defines whether rig handles edit the bind pose or deform the UI mesh.</summary>
    public enum UIMeshRigAuthoringMode
    {
        Setup = 0,
        Pose = 1
    }

    /// <summary>Scene-view transform tool used while posing a selected rig point.</summary>
    public enum UIMeshRigSceneTool
    {
        Move = 0,
        Rotate = 1,
        Scale = 2
    }

    /// <summary>
    /// Edit Mode authoring rules shared by every rig adapter, so the uGUI, world and SpriteRenderer rigs
    /// cannot disagree about when a moved point may rewrite its bind.
    /// </summary>
    internal static class UIMeshRigAuthoring
    {
        /// <summary>
        /// True when a point moved in Edit Mode may be re-bound to where it now stands: Setup mode, outside
        /// Play Mode, and no Animation window or Timeline preview is sampling the scene.
        /// </summary>
        internal static bool IsRebindAllowed(UIMeshRigAuthoringMode mode)
        {
            return !Application.isPlaying && mode == UIMeshRigAuthoringMode.Setup && !IsAnimationPreviewActive();
        }

        /// <summary>
        /// True while the editor samples animation into the scene (Animation window preview or recording,
        /// Timeline preview). A rig is treated as posed then, so the previewed clip is visible in Setup too.
        /// </summary>
        internal static bool IsAnimationPreviewActive()
        {
#if UNITY_EDITOR
            // WHY: a sampled clip moves the points and raises Transform.hasChanged exactly like a drag. Rebinding
            // on it wrote the animated pose into the serialized bind centre and anchors, which AnimationMode does
            // not restore, so every scrub moved the bind further and stopping the preview left it corrupted.
            return !Application.isPlaying && UnityEditor.AnimationMode.InAnimationMode();
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Subscribes or unsubscribes an Edit Mode refresh that renderers without a Canvas use to show a sampled
        /// clip. Passing the same method group again is a no-op, so OnEnable and OnDisable can call it freely.
        /// </summary>
        // WHY: the world and SpriteRenderer rigs rebuild only in LateUpdate, and nothing in the module asked the
        // editor for a player-loop tick while the Animation window or Timeline scrubbed a clip. A Canvas rebuilds
        // on its own repaint; these two renderers showed a stale mesh until something else ticked the scene.
        internal static void SetAnimationPreviewRefresh(UnityEditor.EditorApplication.CallbackFunction refresh, bool subscribe)
        {
            if (refresh == null)
            {
                return;
            }

            UnityEditor.EditorApplication.update -= refresh;
            if (subscribe)
            {
                UnityEditor.EditorApplication.update += refresh;
            }
        }
#endif
    }
}
