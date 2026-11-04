using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private void BeginRainbowLineTracking()
        {
            if (CustomEditorSettings.EnableRainbowLineAnimation)
            {
                EnsureRepaint();
            }
        }

        private void EndRainbowLineTracking()
        {
            if (Event.current.type != EventType.Repaint || _neoPanelRect.width <= 0f)
            {
                return;
            }

            DrawRainbowHalfFrame(_neoPanelRect, _frameMood, _framePlayMode);
        }

        // WHY: the frame is one polyline with a fixed vertex budget. Unity interpolates the per-vertex colours, so a
        // smooth spectrum needs no more vertices than the corners need for their curve; the old version drew ~110
        // separately coloured 5px pieces per repaint and allocated a params array for each of them.
        private const int FrameArmPoints = 8;
        private const int FrameArcPoints = 6;
        private const int FrameEdgePoints = 30;

        internal const int FramePointCount =
            FrameArmPoints + FrameArcPoints + FrameEdgePoints + FrameArcPoints + (FrameArmPoints - 1);

        private static readonly Vector3[] FramePoints = new Vector3[FramePointCount];
        private static readonly Color[] FrameColors = new Color[FramePointCount];
        private static readonly float[] FrameDistances = new float[FramePointCount];

        /// <summary>
        ///     Smooth animated spectrum "half-frame" hugging the property card: left edge with rounded
        ///     corners plus short top/bottom arms whose tips fade out. Continuous HSV hue along the path,
        ///     drawn as a single anti-aliased polyline with per-vertex colours.
        /// </summary>
        private static void DrawRainbowHalfFrame(Rect panel, NeoComponentHealth.Mood mood, bool playMode)
        {
            const float lineWidth = 2f;
            float inset = lineWidth * 0.5f + 0.5f;
            // WHY: The stroke centreline must sit at (card radius - inset) to stay concentric with the
            // card corner; a larger radius reads as the line peeling off exactly at the corners.
            float radius = Mathf.Max(2f, NeoInspectorTheme.RadiusCard - inset);

            Rect r = new(panel.x + inset, panel.y + inset,
                Mathf.Max(0f, panel.width - inset * 2f), Mathf.Max(0f, panel.height - inset * 2f));
            if (r.height < radius * 2f + 8f)
            {
                return;
            }

            float arm = Mathf.Clamp(r.width * 0.25f, 20f, 48f);
            if (!BuildFramePath(r, radius, arm))
            {
                return;
            }

            bool animate = CustomEditorSettings.EnableRainbowLineAnimation && NeoInspectorAnimation.IsActive();
            // WHY: In Play Mode the healthy spectrum flows faster - the frame "plays along".
            float speedMul = playMode && mood == NeoComponentHealth.Mood.Ok ? 1.8f : 1f;
            float time = animate
                ? (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed * 1.6f * speedMul
                : 0f;
            float pulseT = animate ? (float)EditorApplication.timeSinceStartup : 0f;

            float length = FrameDistances[FramePointCount - 1];
            float fadeLength = Mathf.Max(8f, arm * 0.85f);
            for (int i = 0; i < FramePointCount; i++)
            {
                float distance = FrameDistances[i];
                float fade = Mathf.Min(Mathf.InverseLerp(0f, fadeLength, distance),
                    Mathf.InverseLerp(length, length - fadeLength, distance));
                FrameColors[i] = FrameColor(distance / length, fade, mood, time, pulseT);
            }

            Handles.BeginGUI();
            Color previous = Handles.color;
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(lineWidth, FrameColors, FramePoints);
            Handles.color = previous;
            Handles.EndGUI();
        }

        /// <summary>
        ///     Fills <see cref="FramePoints" /> with the half-frame path (top arm tip, top-left arc, left edge,
        ///     bottom-left arc, bottom arm tip) and <see cref="FrameDistances" /> with the path length at each vertex.
        ///     Segments share their joint vertices, so the corners have no gaps or kinks.
        /// </summary>
        internal static bool BuildFramePath(Rect r, float radius, float arm)
        {
            int index = 0;
            Vector2 tlCenter = new(r.x + radius, r.y + radius);
            Vector2 blCenter = new(r.x + radius, r.yMax - radius);

            AddLine(ref index, new Vector2(r.x + radius + arm, r.y), new Vector2(r.x + radius, r.y), FrameArmPoints,
                true);
            // WHY: GUI y grows down and AddArc negates sin, so the top-left quadrant sweeps 90° (top of the corner
            // circle) -> 180° (left).
            AddArc(ref index, tlCenter, radius, 90f, 180f, FrameArcPoints);
            AddLine(ref index, new Vector2(r.x, r.y + radius), new Vector2(r.x, r.yMax - radius), FrameEdgePoints,
                false);
            AddArc(ref index, blCenter, radius, 180f, 270f, FrameArcPoints);
            AddLine(ref index, new Vector2(r.x + radius, r.yMax), new Vector2(r.x + radius + arm, r.yMax),
                FrameArmPoints - 1, false);

            if (index != FramePointCount)
            {
                return false;
            }

            float distance = 0f;
            FrameDistances[0] = 0f;
            for (int i = 1; i < FramePointCount; i++)
            {
                distance += Vector3.Distance(FramePoints[i - 1], FramePoints[i]);
                FrameDistances[i] = distance;
            }

            return distance > 0f;
        }

        /// <summary>Path vertex <paramref name="index" /> of the last <see cref="BuildFramePath" /> call (test hook).</summary>
        internal static Vector3 GetFramePoint(int index)
        {
            return FramePoints[index];
        }

        /// <summary>Adds <paramref name="count" /> points from <paramref name="from" /> towards <paramref name="to" />.</summary>
        /// <param name="includeStart">True for the first segment; later ones start after the shared joint.</param>
        private static void AddLine(ref int index, Vector2 from, Vector2 to, int count, bool includeStart)
        {
            int steps = includeStart ? count - 1 : count;
            for (int i = includeStart ? 0 : 1; i <= steps; i++)
            {
                if (index >= FramePointCount)
                {
                    return;
                }

                Vector2 p = Vector2.Lerp(from, to, i / (float)steps);
                FramePoints[index++] = new Vector3(p.x, p.y, 0f);
            }
        }

        /// <summary>Adds <paramref name="count" /> arc points after the shared start point.</summary>
        private static void AddArc(ref int index, Vector2 center, float radius, float fromDeg, float toDeg, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                if (index >= FramePointCount)
                {
                    return;
                }

                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)count) * Mathf.Deg2Rad;
                Vector2 p = center + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * radius;
                FramePoints[index++] = new Vector3(p.x, p.y, 0f);
            }
        }

        /// <summary>Half-frame colour at normalized path position <paramref name="t" /> (0 = top tip, 1 = bottom tip).</summary>
        private static Color FrameColor(float t, float fade, NeoComponentHealth.Mood mood, float time, float pulseT)
        {
            Color color;
            float alphaMax;
            switch (mood)
            {
                // WHY: Worried = amber shimmer, Alarmed = red with a faster, deeper pulse - the frame
                // mirrors the mascot's mood so problems are visible before reading anything.
                case NeoComponentHealth.Mood.Worried:
                    color = Color.HSVToRGB(0.085f + 0.025f * Mathf.Sin((t * 3f + pulseT * 0.9f) * Mathf.PI * 2f),
                        CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
                    alphaMax = 0.78f + 0.14f * Mathf.Sin(pulseT * Mathf.PI * 1.6f);
                    break;

                case NeoComponentHealth.Mood.Alarmed:
                    color = Color.HSVToRGB(
                        Mathf.Repeat(0.985f + 0.030f * Mathf.Sin((t * 3f + pulseT * 1.4f) * Mathf.PI * 2f), 1f),
                        CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
                    alphaMax = 0.72f + 0.24f * Mathf.Sin(pulseT * Mathf.PI * 3.2f);
                    break;

                default:
                    // WHY: HSV hue is circular, so hue + time wraps with no visible seam or banding.
                    color = Color.HSVToRGB(Mathf.Repeat(t + time, 1f),
                        CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
                    alphaMax = 0.88f;
                    break;
            }

            color.a = Mathf.SmoothStep(0f, Mathf.Clamp01(alphaMax), fade);
            return color;
        }

        /// <summary>
        ///     Draws an action button in the Neoxider style: brand gradient, eased hover highlight, pressed state with
        ///     the label dropping a pixel, dimmed when disabled. Click semantics match a GUI.Button (press, then
        ///     release over the button).
        /// </summary>
        internal bool DrawGradientButton(string text, float width, float height = 0)
        {
            return DrawGradientButton(NeoInspectorStyles.TempContent(text), width, height);
        }

        private bool DrawGradientButton(GUIContent content, float width, float height)
        {
            if (height == 0)
            {
                height = GradientButtonSettings.DefaultButtonHeight;
            }

            if (GradientButtonSettings.UseNaturalStyle)
            {
                return width > 0f
                    ? GUILayout.Button(content, EditorStyles.miniButton, NeoInspectorStyles.Width(width))
                    : GUILayout.Button(content, EditorStyles.miniButton, NeoInspectorStyles.HeightExpandWidth(height));
            }

            Rect buttonRect = width > 0f
                ? GUILayoutUtility.GetRect(width, height)
                : GUILayoutUtility.GetRect(0f, height, NeoInspectorStyles.ExpandWidth);

            return DrawGradientButton(buttonRect, content);
        }

        /// <summary>Rect-based core of <see cref="DrawGradientButton(string, float, float)" />.</summary>
        internal static bool DrawGradientButton(Rect buttonRect, GUIContent content)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive, buttonRect);
            bool enabled = GUI.enabled;
            bool isHover = enabled && buttonRect.Contains(Event.current.mousePosition);
            bool isPressed = enabled && GUIUtility.hotControl == controlId;

            if (Event.current.type == EventType.Repaint)
            {
                Rect r = NeoInspectorTheme.Snap(buttonRect);
                float hover = NeoInspectorAnimation.Hover(NeoInspectorAnimation.ControlKey(r), isHover);
                float radius = Mathf.Min(NeoInspectorTheme.RadiusButton - 1f, r.height * 0.5f);

                if (!isPressed && enabled)
                {
                    NeoInspectorTheme.DrawRoundedRect(new Rect(r.x, r.y + 1f, r.width, r.height),
                        new Color(0f, 0f, 0f, 0.16f + 0.06f * hover), radius);
                }

                Color tint = enabled ? Color.white : new Color(0.62f, 0.64f, 0.70f, 0.55f);
                float edgeAlpha = isPressed ? 0.34f : 0.14f + 0.16f * hover;
                NeoInspectorTheme.DrawRoundedTexture(r, NeoInspectorTheme.ButtonGradient,
                    new Color(1f, 1f, 1f, enabled ? edgeAlpha : 0.08f), radius, tint, 1f);

                if (isPressed)
                {
                    NeoInspectorTheme.DrawRoundedRect(r, new Color(0f, 0f, 0f, 0.16f), radius);
                }
                else if (enabled)
                {
                    NeoInspectorTheme.DrawRoundedRect(r, new Color(1f, 1f, 1f, 0.09f * hover), radius);
                    EditorGUI.DrawRect(new Rect(r.x + radius, r.y + 1f, Mathf.Max(0f, r.width - radius * 2f), 1f),
                        new Color(1f, 1f, 1f, 0.14f + 0.08f * hover));
                }

                float textDrop = isPressed ? 1f : 0f;
                GUIStyle textStyle = NeoInspectorStyles.ButtonText;
                Color previousText = textStyle.normal.textColor;
                textStyle.normal.textColor = new Color(1f, 1f, 1f, enabled ? 0.98f : 0.6f);
                if (enabled)
                {
                    GUI.Label(new Rect(r.x, r.y + 1f + textDrop, r.width, r.height), content.text,
                        NeoInspectorStyles.ButtonShadow);
                }

                GUI.Label(new Rect(r.x, r.y + textDrop, r.width, r.height), content, textStyle);
                textStyle.normal.textColor = previousText;
            }

            // WHY: Proper click semantics (press down, release over the button) with repaint on hover change.
            // DisabledScope does not guard a custom event handler; respect it before capturing input.
            if (!enabled)
            {
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                }

                return false;
            }

            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (isHover && Event.current.button == 0)
                    {
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        Event.current.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        if (isHover)
                        {
                            return true;
                        }
                    }

                    break;
            }

            return false;
        }
    }
}
