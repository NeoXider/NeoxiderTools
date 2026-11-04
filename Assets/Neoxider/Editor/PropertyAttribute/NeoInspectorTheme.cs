using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Central visual language for the Neoxider inspector (v12).
    ///     Provides a theme-aware palette (Unity dark/light editor skins), a restrained brand accent
    ///     (indigo -> azure) and cheap rounded-rect / gradient / vector drawing primitives that all shared
    ///     inspector helpers build on so the whole package shares one signature look. Sizes follow the
    ///     18px IMGUI row grid; edges are snapped to device pixels so 1px strokes stay crisp on HiDPI screens.
    /// </summary>
    /// <remarks>
    ///     Rounded corners are drawn with <see cref="GUI.DrawTexture(Rect,Texture,ScaleMode,bool,float,Color,float,float)" />,
    ///     which renders on <see cref="EventType.Repaint" /> only. All draw helpers guard on Repaint internally.
    ///     Textures are created once and cached for the editor lifetime (never per-repaint).
    /// </remarks>
    public static class NeoInspectorTheme
    {
        /// <summary>Deep indigo brand primary (#6C4CF0).</summary>
        public static readonly Color BrandIndigo = new(0.4235f, 0.2980f, 0.9412f, 1f);

        /// <summary>Cyan brand accent (#3FC6F0).</summary>
        public static readonly Color BrandCyan = new(0.2471f, 0.7765f, 0.9412f, 1f);

        /// <summary>Violet mid tone used for hovers and secondary accents (#8A63F4).</summary>
        public static readonly Color BrandViolet = new(0.5412f, 0.3882f, 0.9569f, 1f);

        private static Texture2D _bannerGradient;
        private static Texture2D _buttonGradient;
        private static Texture2D _buttonGradientHover;

        /// <summary>True when Unity uses the dark "pro" editor skin.</summary>
        public static bool IsDark => EditorGUIUtility.isProSkin;

        /// <summary>Card / panel surface (the raised content blocks).</summary>
        public static Color PanelBackground => IsDark
            ? new Color(0.145f, 0.155f, 0.190f, 1f)
            : new Color(0.945f, 0.952f, 0.968f, 1f);

        /// <summary>Slightly recessed surface used for nested / section content.</summary>
        public static Color SectionBackground => IsDark
            ? new Color(0.115f, 0.125f, 0.155f, 1f)
            : new Color(0.902f, 0.914f, 0.941f, 1f);

        /// <summary>Base colour of an interactive header row before the accent tint is applied.</summary>
        public static Color HeaderRowBackground => IsDark
            ? new Color(0.100f, 0.110f, 0.140f, 1f)
            : new Color(0.878f, 0.892f, 0.925f, 1f);

        /// <summary>Primary readable text (titles).</summary>
        public static Color TitleText => IsDark
            ? new Color(0.930f, 0.950f, 0.980f, 1f)
            : new Color(0.130f, 0.150f, 0.200f, 1f);

        /// <summary>Secondary / muted text (sub-labels, meta).</summary>
        public static Color MutedText => IsDark
            ? new Color(0.610f, 0.655f, 0.735f, 1f)
            : new Color(0.380f, 0.420f, 0.500f, 1f);

        /// <summary>1px separator line colour.</summary>
        public static Color Separator => IsDark
            ? new Color(1f, 1f, 1f, 0.075f)
            : new Color(0f, 0f, 0f, 0.10f);

        /// <summary>Very faint hairline (top edge highlights).</summary>
        public static Color Hairline => IsDark
            ? new Color(1f, 1f, 1f, 0.05f)
            : new Color(1f, 1f, 1f, 0.55f);

        /// <summary>Overlay applied on hover (scaled by the eased hover amount).</summary>
        public static Color HoverOverlay => IsDark
            ? new Color(1f, 1f, 1f, 0.06f)
            : new Color(0f, 0f, 0f, 0.05f);

        /// <summary>Faint zebra tint for value rows.</summary>
        public static Color RowTint => IsDark
            ? new Color(1f, 1f, 1f, 0.022f)
            : new Color(0f, 0f, 0f, 0.028f);

        /// <summary>Text drawn on top of a saturated accent fill (pills, banner).</summary>
        public static readonly Color OnAccentText = new(1f, 1f, 1f, 0.96f);

        public const float RadiusCard = 8f;
        public const float RadiusSection = 7f;
        public const float RadiusRow = 6f;
        public const float RadiusPill = 9f;
        public const float RadiusButton = 7f;

        /// <summary>Horizontal indigo -> cyan brand gradient used by the header banner.</summary>
        public static Texture2D BannerGradient
        {
            get
            {
                if (_bannerGradient == null)
                {
                    // WHY: the azure end is a shade deeper than BrandCyan so white text stays readable across the
                    // whole banner (white on pure cyan was below 2:1).
                    _bannerGradient = BuildHorizontalGradient(256,
                        (0.00f, new Color(0.384f, 0.267f, 0.886f, 1f)),
                        (0.55f, new Color(0.282f, 0.392f, 0.902f, 1f)),
                        (1.00f, new Color(0.176f, 0.553f, 0.851f, 1f)));
                }

                return _bannerGradient;
            }
        }

        /// <summary>Vertical accent gradient used by action buttons (idle).</summary>
        public static Texture2D ButtonGradient
        {
            get
            {
                if (_buttonGradient == null)
                {
                    _buttonGradient = BuildVerticalGradient(64,
                        (0f, new Color(0.447f, 0.361f, 0.937f, 1f)),
                        (1f, new Color(0.329f, 0.420f, 0.902f, 1f)));
                }

                return _buttonGradient;
            }
        }

        /// <summary>Brighter variant of <see cref="ButtonGradient" /> for the hover state.</summary>
        public static Texture2D ButtonGradientHover
        {
            get
            {
                if (_buttonGradientHover == null)
                {
                    _buttonGradientHover = BuildVerticalGradient(64,
                        (0f, new Color(0.537f, 0.455f, 0.988f, 1f)),
                        (1f, new Color(0.384f, 0.514f, 0.957f, 1f)));
                }

                return _buttonGradientHover;
            }
        }

        /// <summary>Fills <paramref name="rect" /> with a solid rounded rectangle.</summary>
        public static void DrawRoundedRect(Rect rect, Color color, float radius)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || color.a <= 0f)
            {
                return;
            }

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
        }

        /// <summary>
        ///     Draws a rounded rectangle with a crisp border. The edge is drawn full-size and the fill is drawn
        ///     inset on top, producing a real 1px (or N px) stroke without relying on ambiguous border semantics.
        /// </summary>
        public static void DrawRoundedRect(Rect rect, Color fill, Color edge, float radius, float edgeWidth = 1f)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (edge.a > 0f && edgeWidth > 0f)
            {
                DrawRoundedRect(rect, edge, radius);
                Rect inner = new(rect.x + edgeWidth, rect.y + edgeWidth,
                    Mathf.Max(0f, rect.width - edgeWidth * 2f), Mathf.Max(0f, rect.height - edgeWidth * 2f));
                DrawRoundedRect(inner, fill, Mathf.Max(0f, radius - edgeWidth));
            }
            else
            {
                DrawRoundedRect(rect, fill, radius);
            }
        }

        /// <summary>Draws a texture (e.g. a gradient) clipped to a rounded rectangle.</summary>
        public static void DrawRoundedTexture(Rect rect, Texture texture, float radius, Color tint)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || texture == null)
            {
                return;
            }

            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true, 0f, tint, 0f, radius);
        }

        /// <summary>Draws a texture clipped to a rounded rectangle with a crisp border on top-edge.</summary>
        public static void DrawRoundedTexture(Rect rect, Texture texture, Color edge, float radius, Color tint,
            float edgeWidth = 1f)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || texture == null)
            {
                return;
            }

            if (edge.a > 0f && edgeWidth > 0f)
            {
                DrawRoundedRect(rect, edge, radius);
                Rect inner = new(rect.x + edgeWidth, rect.y + edgeWidth,
                    Mathf.Max(0f, rect.width - edgeWidth * 2f), Mathf.Max(0f, rect.height - edgeWidth * 2f));
                GUI.DrawTexture(inner, texture, ScaleMode.StretchToFill, true, 0f, tint, 0f,
                    Mathf.Max(0f, radius - edgeWidth));
            }
            else
            {
                DrawRoundedTexture(rect, texture, radius, tint);
            }
        }

        /// <summary>Draws a thin left accent rail with rounded ends.</summary>
        public static void DrawAccentRail(Rect rowRect, Color accent, float width = 3f, float inset = 4f)
        {
            Rect rail = new(rowRect.x, rowRect.y + inset, width, Mathf.Max(0f, rowRect.height - inset * 2f));
            DrawRoundedRect(rail, accent, width * 0.5f);
        }

        /// <summary>Returns white or near-black depending on which reads better on <paramref name="background" />.</summary>
        public static Color ReadableOn(Color background)
        {
            float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance > 0.58f ? new Color(0.10f, 0.11f, 0.14f, 1f) : new Color(1f, 1f, 1f, 0.96f);
        }

        /// <summary>Draws a full-width 1px separator inside the given row rect (bottom aligned).</summary>
        public static void DrawBottomSeparator(Rect rect)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Separator);
        }

        /// <summary>Accent-coloured text that stays readable on the editor surface of the current skin.</summary>
        public static Color AccentText(Color accent)
        {
            return IsDark ? Color.Lerp(accent, Color.white, 0.38f) : Color.Lerp(accent, Color.black, 0.62f);
        }

        /// <summary>Faint accent fill for chips and pills, tuned per skin so the accent text on it stays readable.</summary>
        public static Color AccentFill(Color accent, float strength = 1f)
        {
            Color fill = accent;
            fill.a = (IsDark ? 0.20f : 0.16f) * Mathf.Clamp01(strength);
            return fill;
        }

        /// <summary>Rounds <paramref name="value" /> to the nearest device pixel.</summary>
        public static float Snap(float value)
        {
            float ppp = Mathf.Max(1f, EditorGUIUtility.pixelsPerPoint);
            return Mathf.Round(value * ppp) / ppp;
        }

        /// <summary>
        ///     Rounds the edges of <paramref name="rect" /> to device pixels, so centred chips and 1px strokes do not
        ///     straddle two pixel rows and blur.
        /// </summary>
        public static Rect Snap(Rect rect)
        {
            float xMin = Snap(rect.xMin);
            float yMin = Snap(rect.yMin);
            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, Snap(rect.xMax)), Mathf.Max(yMin, Snap(rect.yMax)));
        }

        // WHY: Handles takes Vector3 arrays; one scratch buffer keeps the chevron allocation-free per repaint.
        private static readonly Vector3[] s_chevronPoints = new Vector3[3];

        /// <summary>
        ///     Draws a vector disclosure chevron centred in <paramref name="area" />: pointing right at
        ///     <paramref name="openness" /> 0 and rotating to point down at 1, so a foldout arrow can turn smoothly.
        ///     Vector strokes stay crisp at any display scale, unlike the old "▶"/"▼" text glyphs.
        /// </summary>
        public static void DrawChevron(Rect area, float openness, Color color, float size = 7f, float width = 1.8f)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || color.a <= 0f)
            {
                return;
            }

            float angle = Mathf.Clamp01(openness) * Mathf.PI * 0.5f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            Vector2 center = area.center;
            float half = size * 0.5f;
            float depth = size * 0.28f;
            SetChevronPoint(0, center, -depth, -half, cos, sin);
            SetChevronPoint(1, center, depth, 0f, cos, sin);
            SetChevronPoint(2, center, -depth, half, cos, sin);

            Color previous = Handles.color;
            Handles.BeginGUI();
            Handles.color = GUI.enabled ? color : new Color(color.r, color.g, color.b, color.a * 0.5f);
            Handles.DrawAAPolyLine(width, 3, s_chevronPoints);
            Handles.EndGUI();
            Handles.color = previous;
        }

        private static void SetChevronPoint(int index, Vector2 center, float x, float y, float cos, float sin)
        {
            s_chevronPoints[index] = new Vector3(center.x + x * cos - y * sin, center.y + x * sin + y * cos, 0f);
        }

        private static Texture2D BuildHorizontalGradient(int width, params (float pos, Color color)[] stops)
        {
            Texture2D tex = new(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int x = 0; x < width; x++)
            {
                float t = width <= 1 ? 0f : x / (float)(width - 1);
                tex.SetPixel(x, 0, SampleStops(stops, t));
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D BuildVerticalGradient(int height, params (float pos, Color color)[] stops)
        {
            Texture2D tex = new(1, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < height; y++)
            {
                // WHY: Row 0 is the bottom of the texture; invert so stop 0 renders at the top of a rect.
                float t = height <= 1 ? 0f : 1f - y / (float)(height - 1);
                tex.SetPixel(0, y, SampleStops(stops, t));
            }

            tex.Apply();
            return tex;
        }

        private static Color SampleStops((float pos, Color color)[] stops, float t)
        {
            if (stops == null || stops.Length == 0)
            {
                return Color.white;
            }

            if (t <= stops[0].pos)
            {
                return stops[0].color;
            }

            for (int i = 0; i < stops.Length - 1; i++)
            {
                (float pos, Color color) a = stops[i];
                (float pos, Color color) b = stops[i + 1];
                if (t <= b.pos)
                {
                    float span = Mathf.Max(1e-5f, b.pos - a.pos);
                    return Color.Lerp(a.color, b.color, (t - a.pos) / span);
                }
            }

            return stops[stops.Length - 1].color;
        }
    }
}
