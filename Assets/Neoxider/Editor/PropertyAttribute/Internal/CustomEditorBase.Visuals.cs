using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private Color GetRainbowColor(float speed)
        {
            float time = (float)EditorApplication.timeSinceStartup * speed;
            float hue = Mathf.Repeat(time, 1f);
            return Color.HSVToRGB(hue, CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
        }

        private void DrawTextWithRainbowOutline(string text, GUIStyle baseStyle, params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), baseStyle, options);

            float outlineSize = CustomEditorSettings.RainbowOutlineSize;
            float time = (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed;

            GUIStyle outlineStyle = new(baseStyle);

            for (int angle = 0; angle < 360; angle += 45)
            {
                float radian = angle * Mathf.Deg2Rad;
                float offsetX = Mathf.Cos(radian) * outlineSize;
                float offsetY = Mathf.Sin(radian) * outlineSize;

                float hue = Mathf.Repeat(time + angle / 360f * 0.2f, 1f);
                var outlineColor = Color.HSVToRGB(hue,
                    CustomEditorSettings.RainbowSaturation,
                    CustomEditorSettings.RainbowBrightness * 0.8f);
                outlineColor.a = CustomEditorSettings.RainbowOutlineAlpha;

                outlineStyle.normal.textColor = outlineColor;

                Rect offsetRect = new(rect.x + offsetX, rect.y + offsetY, rect.width, rect.height);
                GUI.Label(offsetRect, text, outlineStyle);
            }

            GUI.Label(rect, text, baseStyle);
        }

        private void DrawRainbowComponentOutlineBegin()
        {
            _componentOutlineRect = EditorGUILayout.BeginVertical();
        }

        private void DrawRainbowComponentOutlineEnd()
        {
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                float time = (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed;
                float borderWidth = CustomEditorSettings.RainbowComponentOutlineWidth;

                Rect rect = _componentOutlineRect;
                rect.x -= borderWidth;
                rect.y -= borderWidth;
                rect.width += borderWidth * 2;
                rect.height += borderWidth * 2;

                DrawRainbowBorder(rect, borderWidth, time);
            }
        }

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

        // WHY: 4 segments per chunk keeps the hue step invisible while joints inside a chunk get real
        // miter joins from a single DrawAAPolyLine call (separate 2-point calls leave wedge gaps).
        private const int ArcChunkSegments = 4;

        private static readonly System.Collections.Generic.List<Vector2> FramePointsScratch = new(160);
        private static readonly Vector3[] ArcBufferScratch = new Vector3[ArcChunkSegments + 1];

        /// <summary>
        ///     Smooth animated spectrum "half-frame" hugging the property card: left edge with rounded
        ///     corners plus short top/bottom arms whose tips fade out. Continuous HSV hue along the path
        ///     (no palette seams) replaces the old segmented left line.
        /// </summary>
        private static void DrawRainbowHalfFrame(Rect panel, NeoComponentHealth.Mood mood, bool playMode)
        {
            const float lineWidth = 2.5f;
            const float step = 5f;
            const int arcSlices = 16;
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

            float arm = Mathf.Clamp(r.width * 0.30f, 24f, 64f);
            bool animate = CustomEditorSettings.EnableRainbowLineAnimation;
            // WHY: In Play Mode the healthy spectrum flows faster — the frame "plays along".
            float speedMul = playMode && mood == NeoComponentHealth.Mood.Ok ? 1.8f : 1f;
            float time = animate
                ? (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed * 1.6f * speedMul
                : 0f;
            float pulseT = animate ? (float)EditorApplication.timeSinceStartup : 0f;

            // WHY: Segments must share exact joint points (arm end == arc start etc.) — walking with an
            // open-ended for-loop leaves sub-step gaps that read as kinks at the corners.
            System.Collections.Generic.List<Vector2> points = FramePointsScratch;
            points.Clear();
            Vector2 tlCenter = new(r.x + radius, r.y + radius);
            Vector2 blCenter = new(r.x + radius, r.yMax - radius);

            AppendLine(points, new Vector2(r.x + radius + arm, r.y), new Vector2(r.x + radius, r.y), step);
            int tlStart = points.Count - 1;
            // WHY: GUI y grows down and AppendArc negates sin, so the top-left quadrant sweeps 90°
            // (top of the corner circle) -> 180° (left); sweeping from 270° traced the wrong
            // quadrant and drew the visible "crooked corner" chord.
            AppendArc(points, tlCenter, radius, 90f, 180f, arcSlices);
            int tlEnd = points.Count - 1;
            AppendLine(points, new Vector2(r.x, r.y + radius), new Vector2(r.x, r.yMax - radius), step);
            int blStart = points.Count - 1;
            AppendArc(points, blCenter, radius, 180f, 270f, arcSlices);
            int blEnd = points.Count - 1;
            AppendLine(points, new Vector2(r.x + radius, r.yMax), new Vector2(r.x + radius + arm, r.yMax), step);

            Handles.BeginGUI();
            int count = points.Count;
            // WHY: Editor IMGUI is single-threaded and the frame animates every repaint — reuse
            // scratch buffers instead of allocating per draw.
            Vector3[] arcBuffer = ArcBufferScratch;
            int i = 0;
            while (i < count - 1)
            {
                if (i == tlStart && tlEnd > tlStart)
                {
                    DrawArcRange(points, tlStart, tlEnd, count, arcBuffer, lineWidth, mood, time, pulseT);
                    i = tlEnd;
                }
                else if (i == blStart && blEnd > blStart)
                {
                    DrawArcRange(points, blStart, blEnd, count, arcBuffer, lineWidth, mood, time, pulseT);
                    i = blEnd;
                }
                else
                {
                    Handles.color = FrameColor(i / (float)(count - 1), mood, time, pulseT);
                    Handles.DrawAAPolyLine(lineWidth, points[i], points[i + 1]);
                    i++;
                }
            }

            Handles.EndGUI();
        }

        /// <summary>Draws one corner arc as a few multi-point AA polylines so its joints stay smooth.</summary>
        private static void DrawArcRange(System.Collections.Generic.List<Vector2> points, int start, int end,
            int totalCount, Vector3[] buffer, float width, NeoComponentHealth.Mood mood, float time, float pulseT)
        {
            for (int s = start; s < end; s += ArcChunkSegments)
            {
                int e = Mathf.Min(s + ArcChunkSegments, end);
                int n = e - s + 1;
                for (int k = 0; k < n; k++)
                {
                    buffer[k] = points[s + k];
                }

                Handles.color = FrameColor((s + e) * 0.5f / (totalCount - 1), mood, time, pulseT);
                Handles.DrawAAPolyLine(width, n, buffer);
            }
        }

        /// <summary>Half-frame colour at normalized path position <paramref name="t" /> (0 = top tip, 1 = bottom tip).</summary>
        private static Color FrameColor(float t, NeoComponentHealth.Mood mood, float time, float pulseT)
        {
            Color color;
            float alphaMax;
            switch (mood)
            {
                // WHY: Worried = amber shimmer, Alarmed = red with a faster, deeper pulse — the frame
                // mirrors the mascot's mood so problems are visible before reading anything.
                case NeoComponentHealth.Mood.Worried:
                    color = Color.HSVToRGB(0.085f + 0.025f * Mathf.Sin((t * 3f + pulseT * 0.9f) * Mathf.PI * 2f),
                        CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
                    alphaMax = 0.8f + 0.15f * Mathf.Sin(pulseT * Mathf.PI * 1.6f);
                    break;

                case NeoComponentHealth.Mood.Alarmed:
                    color = Color.HSVToRGB(
                        Mathf.Repeat(0.985f + 0.030f * Mathf.Sin((t * 3f + pulseT * 1.4f) * Mathf.PI * 2f), 1f),
                        CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
                    alphaMax = 0.7f + 0.3f * Mathf.Sin(pulseT * Mathf.PI * 3.2f);
                    break;

                default:
                    // WHY: HSV hue is circular, so hue + time wraps with no visible seam or banding.
                    color = Color.HSVToRGB(Mathf.Repeat(t + time, 1f),
                        CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
                    alphaMax = 0.95f;
                    break;
            }

            float fade = Mathf.Min(Mathf.InverseLerp(0f, 0.10f, t), Mathf.InverseLerp(1f, 0.90f, t));
            color.a = Mathf.SmoothStep(0f, Mathf.Clamp01(alphaMax), fade);
            return color;
        }

        private static void AppendLine(System.Collections.Generic.List<Vector2> points,
            Vector2 from, Vector2 to, float step)
        {
            float length = Vector2.Distance(from, to);
            int slices = Mathf.Max(1, Mathf.CeilToInt(length / step));
            int start = points.Count > 0 && (points[points.Count - 1] - from).sqrMagnitude < 0.01f ? 1 : 0;
            for (int i = start; i <= slices; i++)
            {
                points.Add(Vector2.Lerp(from, to, i / (float)slices));
            }
        }

        private static void AppendArc(System.Collections.Generic.List<Vector2> points,
            Vector2 center, float radius, float fromDeg, float toDeg, int slices)
        {
            for (int i = 0; i <= slices; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)slices) * Mathf.Deg2Rad;
                Vector2 p = center + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * radius;
                if (points.Count > 0 && (points[points.Count - 1] - p).sqrMagnitude < 0.01f)
                {
                    continue;
                }

                points.Add(p);
            }
        }

        private void DrawRainbowBorder(Rect rect, float borderWidth, float time)
        {
            const int segments = 40;
            float perimeter = (rect.width + rect.height) * 2;

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                float hue = Mathf.Repeat(time + t, 1f);
                var color = Color.HSVToRGB(hue,
                    CustomEditorSettings.RainbowSaturation,
                    CustomEditorSettings.RainbowBrightness);

                float nextT = (float)(i + 1) / segments;
                Vector2 start = GetPointOnRectPerimeter(rect, t);
                Vector2 end = GetPointOnRectPerimeter(rect, nextT);

                Handles.BeginGUI();
                Handles.color = color;
                Handles.DrawAAPolyLine(borderWidth, start, end);
                Handles.EndGUI();
            }
        }

        /// <summary>
        ///     Samples a point on the rectangle perimeter (0–1 along the outline).
        /// </summary>
        private Vector2 GetPointOnRectPerimeter(Rect rect, float t)
        {
            float perimeter = (rect.width + rect.height) * 2;
            float distance = t * perimeter;

            if (distance < rect.width)
            {
                return new Vector2(rect.x + distance, rect.y);
            }

            distance -= rect.width;

            if (distance < rect.height)
            {
                return new Vector2(rect.xMax, rect.y + distance);
            }

            distance -= rect.height;

            if (distance < rect.width)
            {
                return new Vector2(rect.xMax - distance, rect.yMax);
            }

            distance -= rect.width;

            return new Vector2(rect.x, rect.yMax - distance);
        }

        /// <summary>
        ///     Draws a button using Unity natural style or the gradient style.
        /// </summary>
        private bool DrawGradientButton(string text, float width, float height = 0)
        {
            if (height == 0)
            {
                height = GradientButtonSettings.DefaultButtonHeight;
            }

            if (GradientButtonSettings.UseNaturalStyle)
            {
                if (width > 0f)
                {
                    return GUILayout.Button(text, EditorStyles.miniButton, GUILayout.Width(width),
                        GUILayout.Height(height));
                }

                return GUILayout.Button(text, EditorStyles.miniButton, GUILayout.Height(height),
                    GUILayout.ExpandWidth(true));
            }

            Rect buttonRect = width > 0f
                ? GUILayoutUtility.GetRect(width, height)
                : GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));

            int controlId = GUIUtility.GetControlID(FocusType.Passive, buttonRect);
            bool isHover = buttonRect.Contains(Event.current.mousePosition);
            bool isPressed = GUIUtility.hotControl == controlId;

            if (Event.current.type == EventType.Repaint)
            {
                Rect shadowRect = new(buttonRect.x + 1.5f, buttonRect.y + 2.5f, buttonRect.width - 3f,
                    buttonRect.height);
                NeoInspectorTheme.DrawRoundedRect(shadowRect, new Color(0f, 0f, 0f, isPressed ? 0.10f : 0.22f),
                    NeoInspectorTheme.RadiusButton);

                Texture2D gradient = isHover || isPressed
                    ? NeoInspectorTheme.ButtonGradientHover
                    : NeoInspectorTheme.ButtonGradient;
                float edgeAlpha = isPressed ? 0.45f : isHover ? 0.34f : 0.18f;
                NeoInspectorTheme.DrawRoundedTexture(buttonRect, gradient,
                    new Color(1f, 1f, 1f, edgeAlpha), NeoInspectorTheme.RadiusButton, Color.white, 1f);

                if (isPressed)
                {
                    NeoInspectorTheme.DrawRoundedRect(buttonRect, new Color(0f, 0f, 0f, 0.16f),
                        NeoInspectorTheme.RadiusButton);
                }
                else
                {
                    Rect sheen = new(buttonRect.x + 2f, buttonRect.y + 1f, buttonRect.width - 4f,
                        Mathf.Max(1f, buttonRect.height * 0.42f));
                    NeoInspectorTheme.DrawRoundedRect(sheen, new Color(1f, 1f, 1f, isHover ? 0.16f : 0.10f),
                        NeoInspectorTheme.RadiusButton - 2f);
                }

                float textDrop = isPressed ? 1.5f : 0f;
                GUIStyle shadowStyle = new(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0f, 0f, 0f, 0.32f) }
                };
                GUI.Label(new Rect(buttonRect.x, buttonRect.y + 1f + textDrop, buttonRect.width, buttonRect.height),
                    text, shadowStyle);

                GUIStyle textStyle = new(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
                };
                GUI.Label(new Rect(buttonRect.x, buttonRect.y + textDrop, buttonRect.width, buttonRect.height),
                    text, textStyle);
            }

            // WHY: Proper click semantics (press down, release over the button) with repaint on hover change.
            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (isHover && Event.current.button == 0)
                    {
                        GUIUtility.hotControl = controlId;
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

        /// <summary>A small themed chip button (icon or text). <paramref name="filled" /> uses the accent as fill.</summary>
        private bool DrawNeoMiniButton(Rect rect, GUIContent content, Color accent, bool filled)
        {
            bool hover = rect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                Color bg = filled
                    ? (hover ? Color.Lerp(accent, Color.white, 0.14f) : accent)
                    : (hover ? new Color(1f, 1f, 1f, 0.16f) : new Color(1f, 1f, 1f, 0.07f));
                Color edge = new(1f, 1f, 1f, hover ? 0.32f : 0.18f);
                NeoInspectorTheme.DrawRoundedRect(rect, bg, edge, NeoInspectorTheme.RadiusButton - 1f, 1f);

                if (content != null && content.image != null)
                {
                    Rect ir = new(rect.x + (rect.width - 14f) * 0.5f, rect.y + (rect.height - 14f) * 0.5f, 14f, 14f);
                    Color oc = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, hover ? 1f : 0.82f);
                    GUI.DrawTexture(ir, content.image, ScaleMode.ScaleToFit, true);
                    GUI.color = oc;
                }
                else if (content != null && !string.IsNullOrEmpty(content.text))
                {
                    GUIStyle st = new(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = filled ? Color.white : NeoInspectorTheme.TitleText }
                    };
                    GUI.Label(rect, content.text, st);
                }
            }

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }
    }
}
