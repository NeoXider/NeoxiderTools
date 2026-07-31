using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Central visual language for the Neoxider inspector (v10).
    ///     Provides a theme-aware palette (Unity dark/light editor skins), a restrained brand accent
    ///     (indigo -> cyan) and cheap rounded-rect / gradient drawing primitives that all shared
    ///     inspector helpers build on so the whole package shares one signature look.
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

        /// <summary>Overlay applied on hover.</summary>
        public static Color HoverOverlay => IsDark
            ? new Color(1f, 1f, 1f, 0.05f)
            : new Color(0f, 0f, 0f, 0.045f);

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
                    _bannerGradient = BuildHorizontalGradient(256,
                        (0.00f, BrandIndigo),
                        (0.55f, new Color(0.322f, 0.451f, 0.949f, 1f)),
                        (1.00f, BrandCyan));
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
                        (0f, new Color(0.470f, 0.372f, 0.960f, 1f)),
                        (1f, new Color(0.286f, 0.478f, 0.905f, 1f)));
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
                        (0f, new Color(0.560f, 0.470f, 1f, 1f)),
                        (1f, new Color(0.360f, 0.588f, 0.980f, 1f)));
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
