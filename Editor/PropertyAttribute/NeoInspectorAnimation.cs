using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Policy and motion maths for the Neoxider inspector: whether decorative animation and UI transitions may
    ///     run right now, how often the shared ticker may repaint, and the easing curves every transition uses. The
    ///     pure overloads take explicit inputs so the policy stays testable without editor state.
    /// </summary>
    internal static class NeoInspectorAnimation
    {
        public const int MinFps = 5;
        public const int MaxFps = 60;
        public const int DefaultFps = 30;

        /// <summary>Repaint rate for content that must stay current (update-check status, live condition result).</summary>
        public const int LiveFps = 10;

        /// <summary>Duration of a section opening or closing.</summary>
        public const double FoldDuration = 0.18;

        /// <summary>Duration of a hover highlight fading in.</summary>
        public const double HoverDuration = 0.12;

        /// <summary>Duration of the switch knob sliding to its new side.</summary>
        public const double ToggleDuration = 0.16;

        public static bool ShouldAnimate(bool animationsEnabled, bool animateInPlayMode, bool isPlaying)
        {
            return animationsEnabled && (animateInPlayMode || !isPlaying);
        }

        /// <summary>
        ///     Pure policy for UI transitions (foldouts, hover, switches): they follow the master switch and the Play
        ///     Mode rule like decorative animation, and can be turned off on their own.
        /// </summary>
        public static bool ShouldTransition(bool animationsEnabled, bool animateInPlayMode, bool isPlaying,
            bool transitionsEnabled)
        {
            return transitionsEnabled && ShouldAnimate(animationsEnabled, animateInPlayMode, isPlaying);
        }

        /// <summary>
        ///     Pure policy for the decorative channel of the shared repaint ticker: while the Unity editor is in the
        ///     background nothing decorative needs to move, so animation idles until the editor is focused again.
        ///     Live data and the status refresh keep their rate.
        /// </summary>
        public static bool ShouldTick(bool editorFocused)
        {
            return editorFocused;
        }

        /// <summary>True when the current settings and editor state allow decorative animation.</summary>
        public static bool IsActive()
        {
            return ShouldAnimate(CustomEditorSettings.EnableAnimations, CustomEditorSettings.AnimateInPlayMode,
                EditorApplication.isPlaying);
        }

        /// <summary>True when foldouts, hover highlights and switches may animate instead of snapping.</summary>
        public static bool TransitionsActive()
        {
            return ShouldTransition(CustomEditorSettings.EnableAnimations, CustomEditorSettings.AnimateInPlayMode,
                EditorApplication.isPlaying, CustomEditorSettings.EnableTransitions);
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
            // WHY: a last repaint "in the future" (a tick driven with a pinned clock) would otherwise stall the
            // channel until real time caught up with it.
            return now < lastRepaintAt || now - lastRepaintAt >= RepaintInterval(fps);
        }

        /// <summary>Normalized progress of a transition that started at <paramref name="start" />, clamped to 0..1.</summary>
        public static float Progress(double now, double start, double duration)
        {
            if (duration <= 0.0)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)((now - start) / duration));
        }

        /// <summary>Decelerating curve: fast start, soft landing. The default for anything that opens or appears.</summary>
        public static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        /// <summary>Symmetric curve for movement between two resting states (the switch knob).</summary>
        public static float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }

            float f = -2f * t + 2f;
            return 1f - f * f * f * 0.5f;
        }

        /// <summary>
        ///     Damped spring offset around 0: starts at 0, overshoots to roughly <paramref name="amplitude" /> and
        ///     settles back to exactly 0 at <paramref name="t" /> = 1, so a pop never snaps at its end.
        /// </summary>
        public static float SpringPop(float t, float amplitude)
        {
            t = Mathf.Clamp01(t);
            if (t >= 1f)
            {
                return 0f;
            }

            float decay = Mathf.Exp(-4.2f * t) * (1f - t);
            return amplitude * decay * Mathf.Sin(t * Mathf.PI * 2.6f);
        }

        /// <summary>Smooth 0..1..0 breathing wave with period <paramref name="period" /> seconds.</summary>
        public static float Breath(double time, double period)
        {
            if (period <= 0.0)
            {
                return 0f;
            }

            double phase = time / period;
            phase -= System.Math.Floor(phase);
            return 0.5f - 0.5f * Mathf.Cos((float)(phase * System.Math.PI * 2.0));
        }

        // WHY: only one control can be under the mouse and only one can have just been clicked, so one slot per kind
        // of motion is enough. A per-control dictionary would grow with every row ever hovered and had to be keyed
        // by something that is unique across inspector windows; the single slot is keyed by the control's
        // screen position, which already is.
        private static int s_hoverKey;
        private static double s_hoverStart = -1000.0;
        private static int s_toggleKey;
        private static double s_toggleStart = -1000.0;
        private static bool s_toggleTarget;

        /// <summary>
        ///     Stable key of a control drawn at <paramref name="rect" />, unique across inspector windows because it
        ///     is built from the screen position. Only valid inside an OnGUI pass.
        /// </summary>
        public static int ControlKey(Rect rect)
        {
            Vector2 screen = GUIUtility.GUIToScreenPoint(rect.position);
            unchecked
            {
                int hash = Mathf.RoundToInt(screen.x);
                hash = hash * 397 ^ Mathf.RoundToInt(screen.y);
                hash = hash * 397 ^ Mathf.RoundToInt(rect.width);
                return hash == 0 ? 1 : hash;
            }
        }

        /// <summary>
        ///     Hover intensity (0..1) of the control with <paramref name="key" />: eases in over
        ///     <see cref="HoverDuration" /> after the mouse enters and drops to 0 immediately when it leaves.
        ///     Requests the frames the fade needs from the view being drawn.
        /// </summary>
        public static float Hover(int key, bool hovered)
        {
            if (!hovered)
            {
                if (s_hoverKey == key)
                {
                    s_hoverKey = 0;
                }

                return 0f;
            }

            if (!TransitionsActive())
            {
                return 1f;
            }

            double now = EditorApplication.timeSinceStartup;
            if (s_hoverKey != key)
            {
                s_hoverKey = key;
                s_hoverStart = now;
            }

            float t = Progress(now, s_hoverStart, HoverDuration);
            if (t < 1f)
            {
                RequestFrame();
            }

            return EaseOutCubic(t);
        }

        /// <summary>Remembers that the switch with <paramref name="key" /> was just flipped to <paramref name="value" />.</summary>
        public static void NotifyToggled(int key, bool value)
        {
            s_toggleKey = key;
            s_toggleTarget = value;
            s_toggleStart = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        ///     Knob position (0 = off side, 1 = on side) of the switch with <paramref name="key" />. Only the switch
        ///     that was just clicked slides; every other one, and any value changed from outside (Undo, code), is drawn
        ///     at rest.
        /// </summary>
        public static float TogglePosition(int key, bool value)
        {
            float rest = value ? 1f : 0f;
            if (key != s_toggleKey || value != s_toggleTarget || !TransitionsActive())
            {
                return rest;
            }

            float t = Progress(EditorApplication.timeSinceStartup, s_toggleStart, ToggleDuration);
            if (t >= 1f)
            {
                s_toggleKey = 0;
                return rest;
            }

            RequestFrame();
            float eased = EaseInOutCubic(t);
            return value ? eased : 1f - eased;
        }

        /// <summary>Asks the view that is being drawn for one more frame (no-op outside an OnGUI pass).</summary>
        public static void RequestFrame()
        {
            if (Event.current != null)
            {
                HandleUtility.Repaint();
            }
        }

        /// <summary>Test hook: forgets the hover and switch slots.</summary>
        internal static void ResetMotionForTests()
        {
            s_hoverKey = 0;
            s_hoverStart = -1000.0;
            s_toggleKey = 0;
            s_toggleStart = -1000.0;
        }
    }
}
