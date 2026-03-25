using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        /// <summary>
        ///     Computes a rainbow color from editor time.
        /// </summary>
        private Color GetRainbowColor(float speed)
        {
            float time = (float)EditorApplication.timeSinceStartup * speed;
            float hue = Mathf.Repeat(time, 1f);
            return Color.HSVToRGB(hue, CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
        }

        /// <summary>
        ///     Draws text with a rainbow outline.
        /// </summary>
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

        /// <summary>
        ///     Begins drawing the rainbow outline around the component block.
        /// </summary>
        private void DrawRainbowComponentOutlineBegin()
        {
            _componentOutlineRect = EditorGUILayout.BeginVertical();
        }

        /// <summary>
        ///     Ends the rainbow component outline pass.
        /// </summary>
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

        /// <summary>
        ///     Starts tracking layout for the vertical rainbow line.
        /// </summary>
        private void BeginRainbowLineTracking()
        {
            if (CustomEditorSettings.EnableRainbowLineAnimation)
            {
                EnsureRepaint();
            }

            Rect rect = EditorGUILayout.GetControlRect(false, 0);

            if (Event.current.type == EventType.Repaint)
            {
                _rainbowLineStartY = rect.y;
            }
        }

        /// <summary>
        ///     Finishes tracking and draws the rainbow line.
        /// </summary>
        private void EndRainbowLineTracking()
        {
            if (Event.current.type == EventType.Repaint)
            {
                float lineWidth = 3f;
                float time = CustomEditorSettings.EnableRainbowLineAnimation
                    ? (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed * 5f
                    : 0f;

                Color[] rainbowColors =
                {
                    new(0.9f, 0.2f, 0.2f),
                    new(1f, 0.5f, 0.2f),
                    new(1f, 0.9f, 0.2f),
                    new(0.3f, 0.9f, 0.3f),
                    new(0.2f, 0.7f, 1f),
                    new(0.3f, 0.3f, 1f),
                    new(0.7f, 0.3f, 1f)
                };

                Rect lastRect = GUILayoutUtility.GetLastRect();
                float lineHeight = lastRect.yMax - _rainbowLineStartY;

                if (lineHeight > 0)
                {
                    const float lineX = 16f;
                    int segments = Mathf.Max(10, Mathf.FloorToInt(lineHeight / 5f));
                    float segmentHeight = lineHeight / segments;

                    for (int i = 0; i < segments; i++)
                    {
                        float t = i / (float)segments;
                        t = Mathf.Repeat(t + time, 1f);

                        int colorIndex = Mathf.FloorToInt(t * (rainbowColors.Length - 1));
                        float localT = t * (rainbowColors.Length - 1) - colorIndex;

                        var color = Color.Lerp(
                            rainbowColors[Mathf.Min(colorIndex, rainbowColors.Length - 1)],
                            rainbowColors[Mathf.Min(colorIndex + 1, rainbowColors.Length - 1)],
                            localT
                        );

                        Rect segmentRect = new(
                            lineX,
                            _rainbowLineStartY + i * segmentHeight,
                            lineWidth,
                            segmentHeight + 1
                        );

                        EditorGUI.DrawRect(segmentRect, color);
                    }
                }
            }
        }

        /// <summary>
        ///     Draws a rainbow border around a rectangle.
        /// </summary>
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

            if (Event.current.type == EventType.Repaint)
            {
                Color topColor = GradientButtonSettings.TopColor;
                Color bottomColor = GradientButtonSettings.BottomColor;

                bool isHover = buttonRect.Contains(Event.current.mousePosition);
                if (isHover)
                {
                    topColor = Color.Lerp(topColor, Color.white, GradientButtonSettings.HoverBrightness);
                    bottomColor = Color.Lerp(bottomColor, Color.white, GradientButtonSettings.HoverBrightness);
                }

                for (int i = 0; i < GradientButtonSettings.GradientSegments; i++)
                {
                    float t = i / (float)GradientButtonSettings.GradientSegments;
                    var segmentColor = Color.Lerp(topColor, bottomColor, t);

                    Rect segmentRect = new(
                        buttonRect.x,
                        buttonRect.y + buttonRect.height * t,
                        buttonRect.width,
                        buttonRect.height / GradientButtonSettings.GradientSegments + 1
                    );

                    EditorGUI.DrawRect(segmentRect, segmentColor);
                }

                DrawRoundedCorners(buttonRect, GradientButtonSettings.CornerRadius, topColor, bottomColor);

                Handles.BeginGUI();

                if (GradientButtonSettings.EnableNeonGlow)
                {
                    Handles.color = new Color(
                        GradientButtonSettings.NeonGlowColor.r,
                        GradientButtonSettings.NeonGlowColor.g,
                        GradientButtonSettings.NeonGlowColor.b,
                        0.15f
                    );

                    const float glowWidth = 4f;
                    Vector3[] points =
                    {
                        new(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.y - 1),
                        new(buttonRect.xMax - GradientButtonSettings.CornerRadius, buttonRect.y - 1),
                        new(buttonRect.xMax + 1, buttonRect.y + GradientButtonSettings.CornerRadius),
                        new(buttonRect.xMax + 1, buttonRect.yMax - GradientButtonSettings.CornerRadius),
                        new(buttonRect.xMax - GradientButtonSettings.CornerRadius, buttonRect.yMax + 1),
                        new(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.yMax + 1),
                        new(buttonRect.x - 1, buttonRect.yMax - GradientButtonSettings.CornerRadius),
                        new(buttonRect.x - 1, buttonRect.y + GradientButtonSettings.CornerRadius),
                        new(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.y - 1)
                    };

                    Handles.DrawAAPolyLine(glowWidth, points);

                    Handles.color = GradientButtonSettings.NeonGlowColor;
                    Handles.DrawAAPolyLine(1.5f, points);
                }
                else
                {
                    Handles.color = GradientButtonSettings.HighlightColor;
                    Handles.DrawAAPolyLine(GradientButtonSettings.HighlightWidth,
                        new Vector3(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.y),
                        new Vector3(buttonRect.xMax - GradientButtonSettings.CornerRadius, buttonRect.y)
                    );
                }

                Handles.EndGUI();
            }

            if (GradientButtonSettings.EnableNeonGlow)
            {
                GUIStyle shadowStyle = new(EditorStyles.label)
                {
                    alignment = GradientButtonSettings.TextAlignment,
                    fontStyle = GradientButtonSettings.TextStyle,
                    normal = { textColor = new Color(0, 0, 0, 0.5f) }
                };

                Rect shadowRect = new(buttonRect.x, buttonRect.y + 1, buttonRect.width, buttonRect.height);
                GUI.Label(shadowRect, text, shadowStyle);
            }

            GUIStyle textStyle = new(EditorStyles.label)
            {
                alignment = GradientButtonSettings.TextAlignment,
                fontStyle = GradientButtonSettings.TextStyle,
                normal = { textColor = GradientButtonSettings.TextColor }
            };

            GUI.Label(buttonRect, text, textStyle);

            return Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition);
        }

        /// <summary>
        ///     Applies rounded-corner masking to the button rect.
        /// </summary>
        private void DrawRoundedCorners(Rect rect, float radius, Color topColor, Color bottomColor)
        {
            Color bgColor = GradientButtonSettings.InspectorBackgroundColor;

            DrawCornerMask(new Rect(rect.x, rect.y, radius, radius), radius, bgColor, true, true);
            DrawCornerMask(new Rect(rect.xMax - radius, rect.y, radius, radius), radius, bgColor, false, true);

            DrawCornerMask(new Rect(rect.x, rect.yMax - radius, radius, radius), radius, bgColor, true, false);
            DrawCornerMask(new Rect(rect.xMax - radius, rect.yMax - radius, radius, radius), radius, bgColor, false,
                false);
        }

        /// <summary>
        ///     Draws a corner mask pixel block for rounded corners.
        /// </summary>
        private void DrawCornerMask(Rect cornerRect, float radius, Color bgColor, bool isLeft, bool isTop)
        {
            Vector2 center = isLeft
                ? isTop ? new Vector2(cornerRect.xMax, cornerRect.yMax) : new Vector2(cornerRect.xMax, cornerRect.y)
                : isTop
                    ? new Vector2(cornerRect.x, cornerRect.yMax)
                    : new Vector2(cornerRect.x, cornerRect.y);

            int steps = GradientButtonSettings.CornerMaskSteps;
            float pixelSize = cornerRect.width / steps;

            for (int x = 0; x < steps; x++)
            {
                for (int y = 0; y < steps; y++)
                {
                    float px = cornerRect.x + (x + 0.5f) / steps * cornerRect.width;
                    float py = cornerRect.y + (y + 0.5f) / steps * cornerRect.height;

                    float dist = Vector2.Distance(new Vector2(px, py), center);

                    if (dist > radius + pixelSize * 0.5f)
                    {
                        Rect pixelRect = new(
                            cornerRect.x + x * pixelSize,
                            cornerRect.y + y * pixelSize,
                            pixelSize + 0.5f,
                            pixelSize + 0.5f
                        );
                        EditorGUI.DrawRect(pixelRect, bgColor);
                    }
                }
            }
        }
    }
}
