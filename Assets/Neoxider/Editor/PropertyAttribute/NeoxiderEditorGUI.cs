using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Shared drawing helpers for Neoxider module inspectors (summary cards, sections, badges, rows).
    ///     Built on <see cref="NeoInspectorTheme" /> so every module shares the same premium, theme-aware look.
    /// </summary>
    public static class NeoxiderEditorGUI
    {
        private static GUIStyle _summaryTitleStyle;
        private static GUIStyle _summarySubtitleStyle;
        private static GUIStyle _sectionTitleStyle;
        private static GUIStyle _sectionSubtitleStyle;
        private static GUIStyle _badgeLabelStyle;
        private static GUIStyle _overviewStyle;
        private static GUIStyle _cardBoxStyle;
        private static GUIStyle _compactCardBoxStyle;
        private static GUIStyle _sectionBoxStyle;
        private static GUIStyle _captionStyle;
        private static GUIStyle _keyLabelStyle;
        private static GUIStyle _valueLabelStyle;

        private static GUIStyle SummaryTitleStyle =>
            _summaryTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                wordWrap = true
            };

        private static GUIStyle SummarySubtitleStyle =>
            _summarySubtitleStyle ??= new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true
            };

        private static GUIStyle SectionTitleStyle =>
            _sectionTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                wordWrap = true
            };

        private static GUIStyle SectionSubtitleStyle =>
            _sectionSubtitleStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };

        private static GUIStyle BadgeLabelStyle =>
            _badgeLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 0, 0),
                clipping = TextClipping.Clip
            };

        private static GUIStyle OverviewStyle =>
            _overviewStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };

        private static GUIStyle CaptionStyle =>
            _captionStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                richText = true
            };

        private static GUIStyle KeyLabelStyle =>
            _keyLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };

        private static GUIStyle ValueLabelStyle =>
            _valueLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                richText = true
            };

        private static GUIStyle CardBoxStyle =>
            _cardBoxStyle ??= new GUIStyle
            {
                padding = new RectOffset(12, 12, 10, 9),
                margin = new RectOffset(2, 2, 4, 4)
            };

        private static GUIStyle CompactCardBoxStyle =>
            _compactCardBoxStyle ??= new GUIStyle
            {
                padding = new RectOffset(12, 12, 7, 6),
                margin = new RectOffset(2, 2, 4, 4)
            };

        private static GUIStyle SectionBoxStyle =>
            _sectionBoxStyle ??= new GUIStyle
            {
                padding = new RectOffset(13, 12, 11, 10),
                margin = new RectOffset(2, 2, 3, 3)
            };

        public static void DrawSummaryCard(string title, string subtitle, params Badge[] badges)
        {
            DrawSummaryCard(title, subtitle, false, true, badges);
        }

        public static void DrawSummaryCard(string title, string subtitle, bool compact, params Badge[] badges)
        {
            DrawSummaryCard(title, subtitle, compact, true, badges);
        }

        public static void DrawSummaryCard(string title, string subtitle, bool compact, bool showOverviewLabel,
            params Badge[] badges)
        {
            Rect rect = EditorGUILayout.BeginVertical(compact ? CompactCardBoxStyle : CardBoxStyle);
            DrawPanel(rect, NeoInspectorTheme.PanelBackground, NeoInspectorTheme.BrandIndigo,
                NeoInspectorTheme.RadiusCard);

            using (new EditorGUILayout.HorizontalScope())
            {
                SummaryTitleStyle.normal.textColor = NeoInspectorTheme.TitleText;
                EditorGUILayout.LabelField(title, SummaryTitleStyle);
                if (showOverviewLabel)
                {
                    GUILayout.FlexibleSpace();
                    Rect pill = GUILayoutUtility.GetRect(62f, 18f, NeoInspectorStyles.Width(62f));
                    pill = NeoInspectorTheme.Snap(new Rect(pill.x, pill.y + 1f, pill.width, 16f));
                    NeoInspectorTheme.DrawRoundedRect(pill, NeoInspectorTheme.AccentFill(NeoInspectorTheme.BrandViolet),
                        NeoInspectorTheme.RadiusPill);
                    OverviewStyle.normal.textColor = NeoInspectorTheme.AccentText(NeoInspectorTheme.BrandViolet);
                    GUI.Label(pill, NeoInspectorStyles.CachedContent("Overview"), OverviewStyle);
                }
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                SummarySubtitleStyle.normal.textColor = NeoInspectorTheme.MutedText;
                EditorGUILayout.LabelField(subtitle, SummarySubtitleStyle);
            }

            if (badges != null && badges.Length > 0)
            {
                NeoInspectorStyles.Space(compact ? 2f : 5f);
                DrawBadges(badges);
            }

            EditorGUILayout.EndVertical();
        }

        public static void BeginSection(string title, string subtitle = null)
        {
            Rect rect = EditorGUILayout.BeginVertical(SectionBoxStyle);
            DrawPanel(rect, NeoInspectorTheme.SectionBackground, NeoInspectorTheme.BrandViolet,
                NeoInspectorTheme.RadiusSection);

            SectionTitleStyle.normal.textColor = NeoInspectorTheme.TitleText;
            EditorGUILayout.LabelField(title, SectionTitleStyle);

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                SectionSubtitleStyle.normal.textColor = NeoInspectorTheme.MutedText;
                EditorGUILayout.LabelField(subtitle, SectionSubtitleStyle);
            }

            NeoInspectorStyles.Space(3f);
        }

        public static void EndSection()
        {
            EditorGUILayout.EndVertical();
        }

        public static void DrawCaption(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            CaptionStyle.normal.textColor = NeoInspectorTheme.MutedText;
            EditorGUILayout.LabelField(text, CaptionStyle);
        }

        public static void DrawKeyValueRow(string key, string value, Color? accent = null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Color previous = GUI.contentColor;
                if (accent.HasValue)
                {
                    GUI.contentColor = accent.Value;
                }
                else
                {
                    KeyLabelStyle.normal.textColor = NeoInspectorTheme.MutedText;
                }

                GUILayout.Label(key, KeyLabelStyle, NeoInspectorStyles.Width(110f));
                GUI.contentColor = previous;
                ValueLabelStyle.normal.textColor = NeoInspectorTheme.TitleText;
                GUILayout.Label(string.IsNullOrWhiteSpace(value) ? "—" : value, ValueLabelStyle);
            }
        }

        /// <summary>Height of one row of <see cref="DrawBadges" />: a 16px badge plus 2px above and below.</summary>
        internal const float BadgeRowHeight = 20f;

        /// <summary>Horizontal gap between two badges of one row.</summary>
        internal const float BadgeGap = 5f;

        // WHY: the first frame of a badge row has no measured width yet; the inspector margins, the card padding and
        // a possible scrollbar take roughly this much of the view width.
        private const float BadgeRowViewInset = 64f;

        private static readonly int BadgeRowHint = "NeoxiderEditorGUI.Badges".GetHashCode();
        private static readonly List<float> s_badgeWidths = new();

        /// <summary>Width of one row of badges as measured at its last Repaint, kept per control id.</summary>
        private sealed class BadgeRowState
        {
            public float Width = -1f;
            public float ViewWidth;
        }

        /// <summary>
        ///     Draws <paramref name="badges" /> as pills in rows: a badge that does not fit the rest of a row starts
        ///     the next one. The rows take exactly the height they need.
        /// </summary>
        public static void DrawBadges(IReadOnlyList<Badge> badges)
        {
            if (badges == null || badges.Count == 0)
            {
                return;
            }

            // WHY: GUILayoutUtility.GetRect answers the Layout event with a 1px dummy rect. Wrapping against it asked
            // Layout for one row per badge while Repaint, which knows the real width, used fewer, and the unused rows
            // stayed in the card as blank space. The row count therefore comes from a width that is the same in both
            // events: the width this row had at its last Repaint (shifted by any change of the view width), or an
            // estimate from the view width before the first Repaint. One rect of exactly that height is reserved.
            int id = GUIUtility.GetControlID(BadgeRowHint, FocusType.Passive);
            BadgeRowState state = (BadgeRowState)GUIUtility.GetStateObject(typeof(BadgeRowState), id);
            float viewWidth = EditorGUIUtility.currentViewWidth;
            float wrapWidth = state.Width > 0f
                ? state.Width + (viewWidth - state.ViewWidth)
                : viewWidth - BadgeRowViewInset - EditorGUI.indentLevel * 15f;

            s_badgeWidths.Clear();
            for (int i = 0; i < badges.Count; i++)
            {
                s_badgeWidths.Add(BadgeWidth(badges[i]));
            }

            int rows = CountBadgeRows(s_badgeWidths, wrapWidth);
            Rect area = GUILayoutUtility.GetRect(0f, rows * BadgeRowHeight, NeoInspectorStyles.ExpandWidth);
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (Mathf.Abs(area.width - state.Width) > 0.5f || Mathf.Abs(viewWidth - state.ViewWidth) > 0.5f)
            {
                state.Width = area.width;
                state.ViewWidth = viewWidth;
                if (CountBadgeRows(s_badgeWidths, area.width) != rows)
                {
                    // WHY: the reserved height came from an estimate (first frame, resized view); one more pass lays
                    // the card out with the measured width.
                    HandleUtility.Repaint();
                }
            }

            float x = area.x;
            float y = area.y + 2f;
            for (int i = 0; i < badges.Count; i++)
            {
                Badge badge = badges[i];
                float width = s_badgeWidths[i];
                if (x > area.x && x + width > area.xMax)
                {
                    x = area.x;
                    y += BadgeRowHeight;
                }

                Rect badgeRect = NeoInspectorTheme.Snap(new Rect(x, y, Mathf.Min(width, area.width), 16f));
                NeoInspectorTheme.DrawRoundedRect(badgeRect, badge.BackgroundColor, NeoInspectorTheme.RadiusPill);
                BadgeLabelStyle.normal.textColor = NeoInspectorTheme.ReadableOn(badge.BackgroundColor);
                BadgeLabelStyle.Draw(badgeRect, badge.Text, false, false, false, false);
                x += width + BadgeGap;
            }
        }

        /// <summary>Drawn width of one badge pill (at least 44px).</summary>
        internal static float BadgeWidth(Badge badge)
        {
            return Mathf.Max(44f, NeoInspectorStyles.TextWidth(BadgeLabelStyle, badge.Text) + 6f);
        }

        /// <summary>
        ///     Rows <see cref="DrawBadges" /> needs for badges of <paramref name="widths" /> in a row
        ///     <paramref name="rowWidth" /> wide, with the same wrap rule the drawing uses.
        /// </summary>
        internal static int CountBadgeRows(IReadOnlyList<float> widths, float rowWidth)
        {
            if (widths == null || widths.Count == 0)
            {
                return 0;
            }

            int rows = 1;
            float x = 0f;
            for (int i = 0; i < widths.Count; i++)
            {
                float width = widths[i];
                if (x > 0f && x + width > rowWidth)
                {
                    rows++;
                    x = 0f;
                }

                x += width + BadgeGap;
            }

            return rows;
        }

        private static void DrawPanel(Rect rect, Color background, Color accent, float radius)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            NeoInspectorTheme.DrawRoundedRect(rect, background, NeoInspectorTheme.Separator, radius, 1f);
            NeoInspectorTheme.DrawAccentRail(rect, accent, 3f, 6f);
        }

        // WHY: the shared section bar and switch live here and not on CustomEditorBase. They ARE the package's
        // inspector language, but they were private instance methods, reachable only by an editor that inherits
        // CustomEditorBase. A PropertyDrawer cannot inherit it, so anything drawn inside a nested serialized class
        // fell back to raw IMGUI and read as a foreign island in an otherwise styled inspector. There is exactly one
        // definition of what a Neoxider toggle and a Neoxider section header look like.

        /// <summary>Height of the standard section header bar (one 18px row plus 3px padding above and below).</summary>
        public const float SectionHeaderHeight = 24f;

        /// <summary>Width of the standard switch.</summary>
        public const float PillToggleWidth = 32f;

        /// <summary>Height of the rect the standard switch is centred in (one row).</summary>
        public const float PillToggleHeight = 18f;

        private const float SwitchTrackHeight = 16f;
        private const float SwitchKnobInset = 2f;

        private static readonly Color SwitchOnColor = new(0.24f, 0.70f, 0.47f, 1f);
        private static readonly Color OverrideMarkerColor = new(0.0f, 0.58f, 0.92f, 1f);

        /// <summary>Accent used by the standard section bar, matching the auto-generated [Header] sections.</summary>
        /// <remarks>Open and closed sections share the accent; the open state shows in the chevron and the tint.</remarks>
        public static Color SectionAccent(bool expanded)
        {
            return CustomEditorSettings.ScriptNameColor;
        }

        /// <summary>Title colour that pairs with <see cref="SectionAccent" />: the theme title colour in both states.</summary>
        public static Color SectionTitleColor(bool expanded)
        {
            return NeoInspectorTheme.TitleText;
        }

        /// <summary>Draws the standard Neoxider switch and returns the (possibly toggled) value.</summary>
        /// <param name="rect">Where to draw; normally <see cref="PillToggleWidth" /> x <see cref="PillToggleHeight" />.</param>
        /// <param name="value">Current value.</param>
        public static bool DrawPillToggle(Rect rect, bool value)
        {
            return DrawSwitch(rect, value, false);
        }

        /// <summary>
        ///     The switch behind <see cref="DrawPillToggle" />. <paramref name="mixed" /> draws the multi-object
        ///     "different values" state (a dash instead of the knob); clicking it sets every object to true.
        /// </summary>
        internal static bool DrawSwitch(Rect rect, bool value, bool mixed)
        {
            float trackHeight = Mathf.Min(rect.height, SwitchTrackHeight);
            Rect track = NeoInspectorTheme.Snap(new Rect(rect.x, rect.y + (rect.height - trackHeight) * 0.5f,
                rect.width, trackHeight));
            bool enabled = GUI.enabled;

            if (Event.current.type == EventType.Repaint)
            {
                int key = NeoInspectorAnimation.ControlKey(track);
                float position = mixed ? 0.5f : NeoInspectorAnimation.TogglePosition(key, value);
                float hover = enabled
                    ? NeoInspectorAnimation.Hover(key, track.Contains(Event.current.mousePosition))
                    : 0f;
                Color off = NeoInspectorTheme.IsDark
                    ? new Color(0.28f, 0.30f, 0.35f, 1f)
                    : new Color(0.72f, 0.74f, 0.79f, 1f);
                Color trackColor = Color.Lerp(off, SwitchOnColor, position);
                trackColor = Color.Lerp(trackColor, Color.white, 0.10f * hover);
                if (!enabled)
                {
                    trackColor.a *= 0.45f;
                }

                NeoInspectorTheme.DrawRoundedRect(track, trackColor, track.height * 0.5f);

                float knobSize = track.height - SwitchKnobInset * 2f;
                if (mixed)
                {
                    Rect dash = NeoInspectorTheme.Snap(new Rect(track.center.x - 5f, track.center.y - 1f, 10f, 2f));
                    NeoInspectorTheme.DrawRoundedRect(dash, new Color(1f, 1f, 1f, enabled ? 0.95f : 0.5f), 1f);
                }
                else
                {
                    float minX = track.x + SwitchKnobInset;
                    float maxX = track.xMax - SwitchKnobInset - knobSize;
                    Rect knob = new(Mathf.Lerp(minX, maxX, position), track.y + SwitchKnobInset, knobSize, knobSize);
                    Rect shadow = new(knob.x, knob.y + 0.75f, knob.width, knob.height);
                    NeoInspectorTheme.DrawRoundedRect(shadow, new Color(0f, 0f, 0f, enabled ? 0.22f : 0.1f),
                        knobSize * 0.5f);
                    Color knobColor = Color.white;
                    if (!enabled)
                    {
                        knobColor.a = 0.6f;
                    }

                    NeoInspectorTheme.DrawRoundedRect(knob, knobColor, knobSize * 0.5f);
                }
            }

            if (GUI.Button(track, GUIContent.none, GUIStyle.none))
            {
                value = mixed || !value;
                NeoInspectorAnimation.NotifyToggled(NeoInspectorAnimation.ControlKey(track), value);
            }

            return value;
        }

        /// <summary>
        ///     Draws a labelled boolean row as label + switch, the way every other bool in the package is drawn.
        ///     Rect-based, so a PropertyDrawer can use it too. Goes through BeginProperty, so prefab overrides (bold
        ///     label, override bar, Revert context menu), Undo and multi-object editing (a mixed value shows a dash)
        ///     behave like a built-in toggle.
        /// </summary>
        public static void DrawPillToggleField(Rect rowRect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rowRect, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(rowRect, label);

            // WHY: left-aligned at the start of the value column like Unity's own toggle, so the switch sits next to
            // its label instead of drifting to the far edge of a wide inspector.
            Rect pillRect = new(contentRect.x, contentRect.y + (contentRect.height - PillToggleHeight) * 0.5f,
                Mathf.Min(PillToggleWidth, Mathf.Max(0f, contentRect.width)), PillToggleHeight);
            bool mixed = property.hasMultipleDifferentValues;
            bool current = property.boolValue;
            EditorGUI.BeginChangeCheck();
            bool toggled = DrawSwitch(pillRect, current, mixed);
            if (EditorGUI.EndChangeCheck())
            {
                // WHY: with mixed values the first object can already hold the new value; assign anyway so every
                // selected object ends up equal.
                property.boolValue = toggled;
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        ///     Draws the standard section bar - chevron, icon, title and a count pill - and returns the (possibly
        ///     toggled) expanded state.
        /// </summary>
        /// <param name="rect">Bar rect, normally <see cref="SectionHeaderHeight" /> tall.</param>
        /// <param name="expanded">Current expanded state.</param>
        /// <param name="title">Section title.</param>
        /// <param name="count">Number shown in the trailing pill; a negative count hides the pill.</param>
        /// <param name="accent">Bar accent; see <see cref="SectionAccent" />.</param>
        /// <param name="iconName">Editor icon name, e.g. <c>Folder Icon</c>. Null or empty draws no icon.</param>
        /// <param name="titleColor">Kept for compatibility; titles use the theme title colour for contrast.</param>
        public static bool DrawSectionHeaderRect(Rect rect, bool expanded, string title, int count, Color accent,
            string iconName, Color titleColor)
        {
            return DrawSectionHeader(rect, expanded, expanded ? 1f : 0f, title, count, accent, iconName, false);
        }

        /// <summary>
        ///     The section bar behind <see cref="DrawSectionHeaderRect" />. <paramref name="openness" /> (0..1) turns
        ///     the chevron and blends the tint while a section animates; <paramref name="hasOverride" /> marks a
        ///     closed section that hides prefab overrides.
        /// </summary>
        internal static bool DrawSectionHeader(Rect rect, bool expanded, float openness, string title, int count,
            Color accent, string iconName, bool hasOverride)
        {
            rect = NeoInspectorTheme.Snap(rect);
            Event current = Event.current;
            bool enabled = GUI.enabled;

            if (Event.current.type == EventType.Repaint)
            {
                bool pro = NeoInspectorTheme.IsDark;
                int key = NeoInspectorAnimation.ControlKey(rect);
                float hover = enabled ? NeoInspectorAnimation.Hover(key, rect.Contains(current.mousePosition)) : 0f;
                float open = Mathf.Clamp01(openness);

                Color background = Color.Lerp(NeoInspectorTheme.HeaderRowBackground, accent,
                    (pro ? 0.06f : 0.05f) + 0.05f * open);
                Color hoverOverlay = NeoInspectorTheme.HoverOverlay;
                background = Color.Lerp(background, new Color(hoverOverlay.r, hoverOverlay.g, hoverOverlay.b, 1f),
                    hoverOverlay.a * hover);
                Color edge = Color.Lerp(NeoInspectorTheme.Separator,
                    new Color(accent.r, accent.g, accent.b, pro ? 0.42f : 0.50f),
                    Mathf.Max(open * 0.85f, hover * 0.6f));
                NeoInspectorTheme.DrawRoundedRect(rect, background, edge, NeoInspectorTheme.RadiusSection, 1f);
                NeoInspectorTheme.DrawAccentRail(rect, accent, 3f, 5f);

                if (hasOverride)
                {
                    EditorGUI.DrawRect(new Rect(rect.x - 6f, rect.y + 4f, 2f, rect.height - 8f), OverrideMarkerColor);
                }

                Color accentText = NeoInspectorTheme.AccentText(accent);
                Color chevronColor = Color.Lerp(NeoInspectorTheme.MutedText, accentText, 0.35f + 0.65f * open);
                NeoInspectorTheme.DrawChevron(new Rect(rect.x + 8f, rect.y, 12f, rect.height), open, chevronColor);

                float x = rect.x + 24f;
                Texture icon = NeoInspectorStyles.Icon(iconName);
                if (icon != null)
                {
                    Rect iconRect = NeoInspectorTheme.Snap(new Rect(x, rect.y + (rect.height - 14f) * 0.5f, 14f, 14f));
                    Color oldGuiColor = GUI.color;
                    Color tint = Color.Lerp(Color.white, accentText, pro ? 0.35f : 0.25f);
                    tint.a = enabled ? 1f : 0.5f;
                    GUI.color = oldGuiColor * tint;
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                    GUI.color = oldGuiColor;
                    x += 19f;
                }

                float countWidth = 0f;
                if (count >= 0)
                {
                    GUIStyle countStyle = NeoInspectorStyles.SectionCount(accentText);
                    string countText = NeoInspectorStyles.CountText(count);
                    countWidth = Mathf.Max(22f, NeoInspectorStyles.TextWidth(countStyle, countText) + 12f);
                    Rect countRect = NeoInspectorTheme.Snap(new Rect(rect.xMax - countWidth - 7f,
                        rect.y + (rect.height - 16f) * 0.5f, countWidth, 16f));
                    NeoInspectorTheme.DrawRoundedRect(countRect,
                        NeoInspectorTheme.AccentFill(accent, 0.8f + 0.4f * open), NeoInspectorTheme.RadiusPill);
                    GUI.Label(countRect, NeoInspectorStyles.CachedContent(countText), countStyle);
                    countWidth += 7f;
                }

                Color titleColor = NeoInspectorTheme.TitleText;
                if (!enabled)
                {
                    titleColor.a *= 0.5f;
                }

                Rect titleRect = new(x, rect.y, Mathf.Max(0f, rect.xMax - x - countWidth - 6f), rect.height);
                GUI.Label(titleRect, NeoInspectorStyles.CachedContent(title), NeoInspectorStyles.SectionTitle(titleColor));
            }

            if (current.type == EventType.MouseDown && current.button == 0 && enabled &&
                rect.Contains(current.mousePosition))
            {
                expanded = !expanded;
                GUI.FocusControl(null);
                current.Use();
            }

            return expanded;
        }

        /// <summary>
        ///     Faint vertical guide on the left of an open section body, in the section accent, so the body reads as
        ///     part of its header without a second box around it.
        /// </summary>
        internal static void DrawSectionBodyGuide(Rect bodyRect, Color accent)
        {
            if (Event.current.type != EventType.Repaint || bodyRect.height <= 4f)
            {
                return;
            }

            Color guide = new(accent.r, accent.g, accent.b, NeoInspectorTheme.IsDark ? 0.30f : 0.40f);
            Rect line = NeoInspectorTheme.Snap(new Rect(bodyRect.x + 4f, bodyRect.y + 1f, 1f, bodyRect.height - 4f));
            EditorGUI.DrawRect(line, guide);
        }

        /// <summary>
        ///     Draws the panel a section body sits in - rounded background plus the accent rail - so a nested
        ///     block reads as part of the section above it rather than as loose fields.
        /// </summary>
        public static void DrawSectionBodyPanel(Rect rect, Color accent)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            NeoInspectorTheme.DrawRoundedRect(rect, NeoInspectorTheme.SectionBackground,
                NeoInspectorTheme.Separator, NeoInspectorTheme.RadiusSection, 1f);
            NeoInspectorTheme.DrawAccentRail(rect, accent, 3f, 5f);
        }

        public readonly struct Badge
        {
            public Badge(string text, Color backgroundColor)
            {
                Text = text;
                BackgroundColor = backgroundColor;
            }

            public string Text { get; }
            public Color BackgroundColor { get; }
        }
    }
}
