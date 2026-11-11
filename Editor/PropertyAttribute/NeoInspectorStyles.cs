using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     GUIStyles, layout options and icons for the always-drawn inspector chrome (banner, badge, section bars,
    ///     switches, foldout rows, action rows), built once per editor skin instead of once per IMGUI event. Tinted
    ///     accessors set the colour on a shared instance, so use the returned style right away. Only valid inside an
    ///     OnGUI pass, like <see cref="EditorStyles" />.
    /// </summary>
    /// <remarks>
    ///     Type scale: 13 bold banner title, 12 bold section titles, 11 body, 10 for captions, pills and badges.
    /// </remarks>
    internal static class NeoInspectorStyles
    {
        private static bool s_builtForDark;
        private static GUIStyle s_glyph;
        private static GUIStyle s_bannerTitle;
        private static GUIStyle s_bannerTagline;
        private static GUIStyle s_versionPill;
        private static GUIStyle s_compactTitle;
        private static GUIStyle s_compactMeta;
        private static GUIStyle s_badge;
        private static GUIStyle s_status;
        private static GUIStyle s_miniChipText;
        private static GUIStyle s_buttonText;
        private static GUIStyle s_buttonShadow;
        private static GUIStyle s_sectionArrow;
        private static GUIStyle s_sectionTitle;
        private static GUIStyle s_sectionCount;
        private static GUIStyle s_pillState;
        private static GUIStyle s_nestedArrow;
        private static GUIStyle s_nestedTitle;
        private static GUIStyle s_nestedCount;
        private static GUIStyle s_plainHeader;
        private static GUIStyle s_warningHeader;
        private static GUIStyle s_networkMarker;
        private static GUIStyle s_actionTitle;
        private static GUIStyle s_actionMeta;
        private static GUIStyle s_parameterLabel;
        private static GUIStyle s_parameterType;
        private static GUIStyle s_eventsSummary;
        private static GUIStyle s_eventsWarning;
        private static GUIStyle s_eventsPreview;
        private static GUIStyle s_docBody;
        private static GUIStyle s_docChipText;
        private static GUIStyle s_healthLine;
        private static GUIStyle s_bodyPanel;
        private static GUIStyle s_actionsPanel;
        private static GUIContent s_refreshIcon;
        private static string s_pillVersion;
        private static string s_pillLabel;
        private static float s_pillWidth;

        // WHY: IMGUI is single-threaded and draws immediately, so one scratch content can carry the text of the call
        // in flight; a fresh GUIContent per measured label was pure per-event garbage.
        private static readonly GUIContent s_tempContent = new();

        private const int CachedCountTexts = 256;
        private static readonly string[] s_countTexts = new string[CachedCountTexts];

        // WHY: in this Unity version every IMGUI text draw or CalcSize costs one GC.Alloc in the native binding, and
        // changing a GUIContent's text costs another. One content per distinct text keeps a redraw at that floor, and
        // a remembered width removes the CalcSize call from steady repaints altogether. Both are bounded.
        private const int MaxCachedTexts = 512;
        private static readonly Dictionary<string, GUIContent> s_contents = new();
        private static readonly Dictionary<(GUIStyle, string), float> s_textWidths = new();
        private static readonly Dictionary<string, GUIContent> s_tooltips = new();

        // WHY: GUILayout.ExpandWidth / Width allocate a GUILayoutOption plus the params array on every call; the
        // chrome asks for the same few options on every IMGUI event of every Neo inspector.
        private static readonly GUILayoutOption[] s_expandWidth = { GUILayout.ExpandWidth(true) };
        private static readonly Dictionary<float, GUILayoutOption[]> s_widthOptions = new();
        private static readonly Dictionary<float, GUILayoutOption[]> s_heightOptions = new();
        private static readonly Dictionary<float, GUILayoutOption[]> s_minWidthOptions = new();
        private static readonly Dictionary<float, GUILayoutOption[]> s_onlyHeightOptions = new();

        private static bool s_iconsBuiltForDark;
        private static readonly Dictionary<string, Texture> s_icons = new();

        /// <summary>Cached <c>GUILayout.ExpandWidth(true)</c>; pass it as the params array.</summary>
        public static GUILayoutOption[] ExpandWidth => s_expandWidth;

        /// <summary>Cached <c>GUILayout.Width(width)</c>; pass it as the params array.</summary>
        public static GUILayoutOption[] Width(float width)
        {
            if (!s_widthOptions.TryGetValue(width, out GUILayoutOption[] options))
            {
                options = new[] { GUILayout.Width(width) };
                s_widthOptions[width] = options;
            }

            return options;
        }

        /// <summary>Cached <c>GUILayout.MinWidth(width)</c>; pass it as the params array.</summary>
        public static GUILayoutOption[] MinWidth(float width)
        {
            if (!s_minWidthOptions.TryGetValue(width, out GUILayoutOption[] options))
            {
                options = new[] { GUILayout.MinWidth(width) };
                s_minWidthOptions[width] = options;
            }

            return options;
        }

        /// <summary>
        ///     Vertical gap in a vertical layout group. <c>GUILayout.Space</c> and <c>EditorGUILayout.Space</c> build a
        ///     GUILayoutOption on every call, Repaint included; this reuses a cached one.
        /// </summary>
        public static void Space(float pixels)
        {
            GUILayoutUtility.GetRect(0f, pixels, Height(pixels));
        }

        /// <summary>Horizontal gap in a horizontal layout group; see <see cref="Space" />.</summary>
        public static void HorizontalSpace(float pixels)
        {
            GUILayoutUtility.GetRect(pixels, 0f, Width(pixels));
        }

        /// <summary>Cached <c>GUILayout.Height(height)</c>; pass it as the params array.</summary>
        public static GUILayoutOption[] Height(float height)
        {
            if (!s_onlyHeightOptions.TryGetValue(height, out GUILayoutOption[] options))
            {
                options = new[] { GUILayout.Height(height) };
                if (s_onlyHeightOptions.Count >= 64)
                {
                    s_onlyHeightOptions.Clear();
                }

                s_onlyHeightOptions[height] = options;
            }

            return options;
        }

        /// <summary>Cached <c>GUILayout.Height(height)</c> + <c>ExpandWidth(true)</c>; pass it as the params array.</summary>
        public static GUILayoutOption[] HeightExpandWidth(float height)
        {
            if (!s_heightOptions.TryGetValue(height, out GUILayoutOption[] options))
            {
                options = new[] { GUILayout.Height(height), GUILayout.ExpandWidth(true) };
                s_heightOptions[height] = options;
            }

            return options;
        }

        /// <summary>
        ///     Built-in editor icon for the current skin. Names may carry the dark-skin "d_" prefix: the light skin
        ///     then gets the light variant instead of a white glyph on a light background. Null when the icon does not
        ///     exist; the lookup runs once per name and skin.
        /// </summary>
        public static Texture Icon(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            bool dark = EditorGUIUtility.isProSkin;
            if (s_iconsBuiltForDark != dark)
            {
                s_iconsBuiltForDark = dark;
                s_icons.Clear();
            }

            if (!s_icons.TryGetValue(name, out Texture icon))
            {
                string baseName = name.StartsWith("d_", System.StringComparison.Ordinal) ? name.Substring(2) : name;
                // WHY: given the base name, IconContent picks the "d_" variant in the dark skin and the @2x image on
                // HiDPI screens on its own; a hard-coded "d_" name forced the dark glyph into the light skin.
                GUIContent content = EditorGUIUtility.IconContent(baseName);
                icon = content != null ? content.image : null;
                s_icons[name] = icon;
            }

            return icon;
        }

        /// <summary>Fallback "N" mark drawn when the mascot art is missing.</summary>
        public static GUIStyle Glyph
        {
            get
            {
                EnsureBuilt();
                return s_glyph;
            }
        }

        public static GUIStyle BannerTitle
        {
            get
            {
                EnsureBuilt();
                return s_bannerTitle;
            }
        }

        public static GUIStyle BannerTagline
        {
            get
            {
                EnsureBuilt();
                return s_bannerTagline;
            }
        }

        /// <summary>Version pill text; always solid white so it stays legible over the bright gradient.</summary>
        public static GUIStyle VersionPill
        {
            get
            {
                EnsureBuilt();
                return s_versionPill;
            }
        }

        /// <summary>Title of the compact one-row header.</summary>
        public static GUIStyle CompactTitle(Color color)
        {
            EnsureBuilt();
            s_compactTitle.normal.textColor = color;
            return s_compactTitle;
        }

        /// <summary>Muted module/version text of the compact header.</summary>
        public static GUIStyle CompactMeta(Color color)
        {
            EnsureBuilt();
            s_compactMeta.normal.textColor = color;
            return s_compactMeta;
        }

        public static GUIStyle Badge
        {
            get
            {
                EnsureBuilt();
                return s_badge;
            }
        }

        /// <summary>Update-strip status label tinted with <paramref name="color" />.</summary>
        public static GUIStyle Status(Color color)
        {
            EnsureBuilt();
            s_status.normal.textColor = color;
            return s_status;
        }

        /// <summary>Centered mini label used inside themed chip buttons.</summary>
        public static GUIStyle MiniChipText(Color color)
        {
            EnsureBuilt();
            s_miniChipText.normal.textColor = color;
            return s_miniChipText;
        }

        /// <summary>Label of the gradient action button.</summary>
        public static GUIStyle ButtonText
        {
            get
            {
                EnsureBuilt();
                return s_buttonText;
            }
        }

        /// <summary>Soft drop shadow drawn one pixel under <see cref="ButtonText" />.</summary>
        public static GUIStyle ButtonShadow
        {
            get
            {
                EnsureBuilt();
                return s_buttonShadow;
            }
        }

        /// <summary>Refresh icon content; falls back to a text glyph when the built-in icon is unavailable.</summary>
        public static GUIContent RefreshIcon
        {
            get
            {
                if (s_refreshIcon == null || (s_refreshIcon.image == null && string.IsNullOrEmpty(s_refreshIcon.text)))
                {
                    Texture icon = Icon("Refresh");
                    s_refreshIcon = icon != null ? new GUIContent(icon) : new GUIContent("⟳");
                }

                return s_refreshIcon;
            }
        }

        /// <summary>Foldout arrow of the standard section bar.</summary>
        public static GUIStyle SectionArrow(Color color)
        {
            EnsureBuilt();
            s_sectionArrow.normal.textColor = color;
            return s_sectionArrow;
        }

        /// <summary>Title of the standard section bar.</summary>
        public static GUIStyle SectionTitle(Color color)
        {
            EnsureBuilt();
            s_sectionTitle.normal.textColor = color;
            return s_sectionTitle;
        }

        /// <summary>Count pill text of the standard section bar.</summary>
        public static GUIStyle SectionCount(Color color)
        {
            EnsureBuilt();
            s_sectionCount.normal.textColor = color;
            return s_sectionCount;
        }

        /// <summary>"ON"/"OFF" caption inside the pill toggle.</summary>
        public static GUIStyle PillState(Color color, TextAnchor alignment)
        {
            EnsureBuilt();
            s_pillState.normal.textColor = color;
            s_pillState.alignment = alignment;
            return s_pillState;
        }

        /// <summary>Arrow of a nested (collection / serializable class) foldout row.</summary>
        public static GUIStyle NestedArrow(Color color)
        {
            EnsureBuilt();
            s_nestedArrow.normal.textColor = color;
            return s_nestedArrow;
        }

        /// <summary>Title of a nested foldout row.</summary>
        public static GUIStyle NestedTitle(Color color)
        {
            EnsureBuilt();
            s_nestedTitle.normal.textColor = color;
            return s_nestedTitle;
        }

        /// <summary>Count pill text of a nested foldout row.</summary>
        public static GUIStyle NestedCount(Color color)
        {
            EnsureBuilt();
            s_nestedCount.normal.textColor = color;
            return s_nestedCount;
        }

        /// <summary>Title of a plain (non-collapsible) header row.</summary>
        public static GUIStyle PlainHeader(Color color)
        {
            EnsureBuilt();
            s_plainHeader.normal.textColor = color;
            return s_plainHeader;
        }

        /// <summary>Title of a warning header row.</summary>
        public static GUIStyle WarningHeader(Color color)
        {
            EnsureBuilt();
            s_warningHeader.normal.textColor = color;
            return s_warningHeader;
        }

        /// <summary>Caption of the network replication marker.</summary>
        public static GUIStyle NetworkMarker(Color color)
        {
            EnsureBuilt();
            s_networkMarker.normal.textColor = color;
            return s_networkMarker;
        }

        /// <summary>Title of an action card.</summary>
        public static GUIStyle ActionTitle(Color color)
        {
            EnsureBuilt();
            s_actionTitle.normal.textColor = color;
            return s_actionTitle;
        }

        /// <summary>Right-aligned meta text of an action card.</summary>
        public static GUIStyle ActionMeta(Color color)
        {
            EnsureBuilt();
            s_actionMeta.normal.textColor = color;
            return s_actionMeta;
        }

        /// <summary>Label of an action parameter row.</summary>
        public static GUIStyle ParameterLabel(Color color)
        {
            EnsureBuilt();
            s_parameterLabel.normal.textColor = color;
            return s_parameterLabel;
        }

        /// <summary>Right-aligned type name of an action parameter row.</summary>
        public static GUIStyle ParameterType(Color color)
        {
            EnsureBuilt();
            s_parameterType.normal.textColor = color;
            return s_parameterType;
        }

        /// <summary>Summary line of the Events section.</summary>
        public static GUIStyle EventsSummary(Color color)
        {
            EnsureBuilt();
            s_eventsSummary.normal.textColor = color;
            return s_eventsSummary;
        }

        /// <summary>Broken-listener hint under a collapsed event.</summary>
        public static GUIStyle EventsWarning
        {
            get
            {
                EnsureBuilt();
                return s_eventsWarning;
            }
        }

        /// <summary>Listener preview under a collapsed event.</summary>
        public static GUIStyle EventsPreview
        {
            get
            {
                EnsureBuilt();
                s_eventsPreview.normal.textColor = NeoInspectorTheme.MutedText;
                return s_eventsPreview;
            }
        }

        /// <summary>Rich-text body of the inline documentation preview.</summary>
        public static GUIStyle DocBody
        {
            get
            {
                EnsureBuilt();
                return s_docBody;
            }
        }

        /// <summary>Caption of the "Open in window" documentation chip.</summary>
        public static GUIStyle DocChipText
        {
            get
            {
                EnsureBuilt();
                return s_docChipText;
            }
        }

        /// <summary>Word-wrapped line of the health panel.</summary>
        public static GUIStyle HealthLine
        {
            get
            {
                EnsureBuilt();
                s_healthLine.normal.textColor = NeoInspectorTheme.TitleText;
                return s_healthLine;
            }
        }

        /// <summary>
        ///     Layout style of a section body: no box, just the inset that leaves room for the guide line drawn on its
        ///     left edge. The old helpBox nested one more frame inside the property card.
        /// </summary>
        public static GUIStyle BodyPanel
        {
            get
            {
                EnsureBuilt();
                return s_bodyPanel;
            }
        }

        /// <summary>Layout style of the Actions body.</summary>
        public static GUIStyle ActionsPanel
        {
            get
            {
                EnsureBuilt();
                return s_actionsPanel;
            }
        }

        /// <summary>Cached decimal text for small counts (section and foldout pills redraw them every event).</summary>
        public static string CountText(int count)
        {
            if (count < 0 || count >= CachedCountTexts)
            {
                return count.ToString();
            }

            return s_countTexts[count] ??= count.ToString();
        }

        /// <summary>
        ///     Scratch content holding <paramref name="text" />; valid only until the next call, like Unity's own
        ///     internal GUIContent.Temp.
        /// </summary>
        public static GUIContent TempContent(string text)
        {
            s_tempContent.text = text;
            s_tempContent.image = null;
            s_tempContent.tooltip = null;
            return s_tempContent;
        }

        /// <summary>Scratch content with a tooltip; same lifetime rule as <see cref="TempContent(string)" />.</summary>
        public static GUIContent TempContent(string text, string tooltip)
        {
            s_tempContent.text = text;
            s_tempContent.image = null;
            s_tempContent.tooltip = tooltip;
            return s_tempContent;
        }

        /// <summary>
        ///     Shared content that keeps <paramref name="text" />, for labels redrawn on every event (section titles,
        ///     counts, pills). Never modify the returned instance. Null or empty text returns <see cref="GUIContent.none" />.
        /// </summary>
        public static GUIContent CachedContent(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return GUIContent.none;
            }

            if (!s_contents.TryGetValue(text, out GUIContent content))
            {
                if (s_contents.Count >= MaxCachedTexts)
                {
                    s_contents.Clear();
                }

                content = new GUIContent(text);
                s_contents.Add(text, content);
            }

            return content;
        }

        /// <summary>
        ///     Shared text-free content that carries <paramref name="tooltip" />, for invisible tooltip zones redrawn
        ///     on every repaint. Never modify the returned instance. Null or empty returns <see cref="GUIContent.none" />.
        /// </summary>
        public static GUIContent CachedTooltip(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip))
            {
                return GUIContent.none;
            }

            if (!s_tooltips.TryGetValue(tooltip, out GUIContent content))
            {
                if (s_tooltips.Count >= MaxCachedTexts)
                {
                    s_tooltips.Clear();
                }

                content = new GUIContent(string.Empty, tooltip);
                s_tooltips.Add(tooltip, content);
            }

            return content;
        }

        /// <summary>
        ///     Width <paramref name="style" /> needs for <paramref name="text" />, measured once per style and text.
        ///     Styles rebuilt for another skin are new instances, so they are measured again.
        /// </summary>
        public static float TextWidth(GUIStyle style, string text)
        {
            if (style == null || string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            (GUIStyle, string) key = (style, text);
            if (!s_textWidths.TryGetValue(key, out float width))
            {
                if (s_textWidths.Count >= MaxCachedTexts)
                {
                    s_textWidths.Clear();
                }

                width = style.CalcSize(CachedContent(text)).x;
                s_textWidths.Add(key, width);
            }

            return width;
        }

        /// <summary>The "v1.2.3" pill text for <paramref name="version" />, allocated once per version string.</summary>
        public static string VersionLabel(string version)
        {
            if (!string.Equals(s_pillVersion, version))
            {
                s_pillVersion = version;
                s_pillLabel = $"v{version}";
                s_pillWidth = -1f;
            }

            return s_pillLabel;
        }

        /// <summary>Pill width for <paramref name="label" />; text measuring runs once per label.</summary>
        public static float VersionPillWidth(string label)
        {
            if (s_pillWidth < 0f || !ReferenceEquals(label, s_pillLabel))
            {
                s_pillWidth = Mathf.Max(42f, TextWidth(VersionPill, label) + 24f);
            }

            return s_pillWidth;
        }

        private static void EnsureBuilt()
        {
            bool dark = EditorGUIUtility.isProSkin;
            if (s_glyph != null && s_builtForDark == dark)
            {
                return;
            }

            s_builtForDark = dark;
            s_pillWidth = -1f;

            s_glyph = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            s_bannerTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.LowerLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
            };

            s_bannerTagline = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = new Color(1f, 1f, 1f, 0.80f) }
            };

            s_versionPill = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };

            s_compactTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };

            s_compactMeta = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };

            s_badge = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };

            s_status = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            s_miniChipText = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(4, 4, 0, 0)
            };

            s_buttonText = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(8, 8, 0, 0),
                normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
            };

            s_buttonShadow = new GUIStyle(s_buttonText)
            {
                normal = { textColor = new Color(0f, 0f, 0f, 0.28f) }
            };

            s_sectionArrow = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            s_sectionTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 1)
            };

            s_sectionCount = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };

            s_pillState = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 9
            };

            s_nestedArrow = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            s_nestedTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 1)
            };

            s_nestedCount = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };

            s_plainHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 1)
            };

            s_warningHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft
            };

            s_networkMarker = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            s_actionTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                clipping = TextClipping.Clip
            };

            s_actionMeta = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Clip
            };

            s_parameterLabel = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            s_parameterType = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Clip
            };

            s_eventsSummary = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                wordWrap = true
            };

            s_eventsWarning = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                wordWrap = true,
                normal =
                {
                    textColor = dark ? new Color(1f, 0.52f, 0.50f, 1f) : new Color(0.70f, 0.13f, 0.12f, 1f)
                }
            };

            s_eventsPreview = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                wordWrap = false,
                clipping = TextClipping.Clip
            };

            s_docBody = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                normal = { textColor = dark ? new Color(0.88f, 0.90f, 0.92f, 1f) : new Color(0.16f, 0.18f, 0.22f, 1f) }
            };

            s_docChipText = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            s_healthLine = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                fontSize = 10,
                richText = false
            };

            s_bodyPanel = new GUIStyle
            {
                padding = new RectOffset(10, 2, 3, 6),
                margin = new RectOffset(0, 0, 0, 2)
            };

            s_actionsPanel = new GUIStyle
            {
                padding = new RectOffset(10, 2, 4, 4),
                margin = new RectOffset(0, 0, 0, 2)
            };
        }
    }
}
