using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Policy for the Neoxider inspector's decorative animation (mascot, rainbow frame, update pulse):
    ///     whether it may run right now and how often it may repaint. The pure overloads take explicit
    ///     inputs so the policy stays testable without editor state.
    /// </summary>
    internal static class NeoInspectorAnimation
    {
        public const int MinFps = 5;
        public const int MaxFps = 60;
        public const int DefaultFps = 30;

        /// <summary>Repaint rate for content that must stay current (update-check status, live condition result).</summary>
        public const int LiveFps = 10;

        public static bool ShouldAnimate(bool animationsEnabled, bool animateInPlayMode, bool isPlaying)
        {
            return animationsEnabled && (animateInPlayMode || !isPlaying);
        }

        /// <summary>True when the current settings and editor state allow decorative animation.</summary>
        public static bool IsActive()
        {
            return ShouldAnimate(CustomEditorSettings.EnableAnimations, CustomEditorSettings.AnimateInPlayMode,
                EditorApplication.isPlaying);
        }

        public static int ClampFps(int fps)
        {
            return Mathf.Clamp(fps, MinFps, MaxFps);
        }

        public static double RepaintInterval(int fps)
        {
            return 1.0 / ClampFps(fps);
        }

        public static bool IsRepaintDue(double now, double lastRepaintAt, int fps)
        {
            return now - lastRepaintAt >= RepaintInterval(fps);
        }
    }
}
