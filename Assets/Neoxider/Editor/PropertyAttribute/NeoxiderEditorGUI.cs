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
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 2, 2),
                clipping = TextClipping.Clip
            };

        private static GUIStyle OverviewStyle =>
            _overviewStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
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
                    Rect pill = GUILayoutUtility.GetRect(new GUIContent("Overview"), OverviewStyle,
                        GUILayout.Width(62f), GUILayout.Height(16f));
                    Color pillBg = Color.Lerp(NeoInspectorTheme.BrandIndigo, NeoInspectorTheme.BrandCyan, 0.5f);
                    pillBg.a = 0.85f;
                    NeoInspectorTheme.DrawRoundedRect(pill, pillBg, NeoInspectorTheme.RadiusPill);
                    OverviewStyle.normal.textColor = NeoInspectorTheme.ReadableOn(pillBg);
                    GUI.Label(pill, "Overview", OverviewStyle);
                }
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                SummarySubtitleStyle.normal.textColor = NeoInspectorTheme.MutedText;
                EditorGUILayout.LabelField(subtitle, SummarySubtitleStyle);
            }

            if (badges != null && badges.Length > 0)
            {
                EditorGUILayout.Space(compact ? 2f : 5f);
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

            EditorGUILayout.Space(3f);
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

                GUILayout.Label(key, KeyLabelStyle, GUILayout.Width(110f));
                GUI.contentColor = previous;
                ValueLabelStyle.normal.textColor = NeoInspectorTheme.TitleText;
                GUILayout.Label(string.IsNullOrWhiteSpace(value) ? "—" : value, ValueLabelStyle);
            }
        }

        public static void DrawBadges(IReadOnlyList<Badge> badges)
        {
            if (badges == null || badges.Count == 0)
            {
                return;
            }

            Rect rowRect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            float x = rowRect.x;
            float y = rowRect.y + 1f;

            for (int i = 0; i < badges.Count; i++)
            {
                Badge badge = badges[i];
                Vector2 textSize = BadgeLabelStyle.CalcSize(new GUIContent(badge.Text));
                float width = Mathf.Max(58f, textSize.x + 18f);
                Rect badgeRect = new(x, y, width, 18f);

                if (Event.current.type == EventType.Repaint)
                {
                    NeoInspectorTheme.DrawRoundedRect(badgeRect, badge.BackgroundColor, NeoInspectorTheme.RadiusPill);
                    BadgeLabelStyle.normal.textColor = NeoInspectorTheme.ReadableOn(badge.BackgroundColor);
                    BadgeLabelStyle.Draw(badgeRect, badge.Text, false, false, false, false);
                }

                x += width + 6f;
                if (x > rowRect.xMax - 80f && i < badges.Count - 1)
                {
                    Rect nextRowRect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
                    x = nextRowRect.x;
                    y = nextRowRect.y + 1f;
                }
            }
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

        // ------------------------------------------------------------------ shared section / toggle chrome
        //
        // WHY these live here and not on CustomEditorBase: the ON/OFF pill and the green section bar ARE the
        // package's inspector language, but they were private instance methods, reachable only by an editor
        // that inherits CustomEditorBase. A PropertyDrawer cannot inherit it, so anything drawn inside a
        // nested serialized class fell back to raw IMGUI and read as a foreign island in an otherwise styled
        // inspector. Extracted verbatim rather than reimplemented, so there is exactly one definition of what
        // a Neoxider toggle and a Neoxider section header look like.

        /// <summary>Height of the standard section header bar.</summary>
        public const float SectionHeaderHeight = 32f;

        /// <summary>Width of the standard ON/OFF pill.</summary>
        public const float PillToggleWidth = 54f;

        /// <summary>Height of the standard ON/OFF pill.</summary>
        public const float PillToggleHeight = 18f;

        /// <summary>Accent used by the standard section bar, matching the auto-generated [Header] sections.</summary>
        public static Color SectionAccent(bool expanded)
        {
            Color baseGreen = CustomEditorSettings.ScriptNameColor;
            return expanded ? Color.Lerp(baseGreen, Color.black, 0.75f) : baseGreen;
        }

        /// <summary>Title colour that pairs with <see cref="SectionAccent" />.</summary>
        public static Color SectionTitleColor(bool expanded)
        {
            return expanded ? Color.white : CustomEditorSettings.ScriptNameColor;
        }

        /// <summary>Draws the standard Neoxider ON/OFF pill and returns the (possibly toggled) value.</summary>
        /// <param name="rect">Where to draw; normally <see cref="PillToggleWidth" /> x <see cref="PillToggleHeight" />.</param>
        /// <param name="value">Current value.</param>
        public static bool DrawPillToggle(Rect rect, bool value)
        {
            Color oldBg = GUI.backgroundColor;
            Color oldColor = GUI.color;

            if (Event.current.type == EventType.Repaint)
            {
                Color bg = value
                    ? new Color(0.22f, 0.74f, 0.44f, 1f)
                    : NeoInspectorTheme.IsDark
                        ? new Color(0.24f, 0.26f, 0.31f, 1f)
                        : new Color(0.72f, 0.74f, 0.78f, 1f);
                NeoInspectorTheme.DrawRoundedRect(rect, bg, rect.height * 0.5f);

                float knobSize = rect.height - 4f;
                float knobX = value ? rect.xMax - knobSize - 2f : rect.x + 2f;
                Rect knobRect = new(knobX, rect.y + 2f, knobSize, knobSize);
                NeoInspectorTheme.DrawRoundedRect(knobRect, value ? Color.white : new Color(0.92f, 0.94f, 0.97f, 1f),
                    knobSize * 0.5f);
            }

            GUI.color = Color.clear;
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }

            GUI.color = Color.white;

            GUIStyle stateStyle = new(EditorStyles.miniBoldLabel)
            {
                fontSize = 9,
                alignment = value ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                normal =
                {
                    textColor = value
                        ? Color.white
                        : NeoInspectorTheme.IsDark
                            ? new Color(0.72f, 0.76f, 0.82f, 1f)
                            : new Color(0.30f, 0.33f, 0.38f, 1f)
                }
            };
            Rect stateRect = new(rect.x + 8f, rect.y, rect.width - 16f, rect.height);
            GUI.Label(stateRect, value ? "ON" : "OFF", stateStyle);

            GUI.backgroundColor = oldBg;
            GUI.color = oldColor;

            return value;
        }

        /// <summary>
        ///     Draws a labelled boolean row as label + ON/OFF pill, the way every other bool in the package is
        ///     drawn. Rect-based, so a PropertyDrawer can use it too.
        /// </summary>
        public static void DrawPillToggleField(Rect rowRect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rowRect, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(rowRect, label);

            Rect pillRect = new(contentRect.xMax - PillToggleWidth, contentRect.y + 1f,
                PillToggleWidth, PillToggleHeight);
            bool toggled = DrawPillToggle(pillRect, property.boolValue);
            if (toggled != property.boolValue)
            {
                property.boolValue = toggled;
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        ///     Draws the standard green section bar - foldout arrow, icon, title and a count pill - and
        ///     returns the (possibly toggled) expanded state.
        /// </summary>
        /// <param name="rect">Bar rect, normally <see cref="SectionHeaderHeight" /> tall.</param>
        /// <param name="expanded">Current expanded state.</param>
        /// <param name="title">Section title.</param>
        /// <param name="count">Number shown in the trailing pill.</param>
        /// <param name="accent">Bar accent; see <see cref="SectionAccent" />.</param>
        /// <param name="iconName">Editor icon name, e.g. <c>d_Folder Icon</c>. Null or empty draws no icon.</param>
        /// <param name="titleColor">Title colour; see <see cref="SectionTitleColor" />.</param>
        public static bool DrawSectionHeaderRect(Rect rect, bool expanded, string title, int count, Color accent,
            string iconName, Color titleColor)
        {
            bool isHover = rect.Contains(Event.current.mousePosition);
            bool pro = NeoInspectorTheme.IsDark;
            float tintStrength = isHover ? 0.30f : expanded ? 0.23f : 0.14f;
            Color background = Color.Lerp(NeoInspectorTheme.HeaderRowBackground, accent, tintStrength);

            NeoInspectorTheme.DrawRoundedRect(rect, background,
                new Color(accent.r, accent.g, accent.b, expanded ? 0.72f : isHover ? 0.55f : 0.32f),
                NeoInspectorTheme.RadiusSection, 1f);
            NeoInspectorTheme.DrawAccentRail(rect, accent, 4f, 6f);

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                GUI.FocusControl(null);
                Event.current.Use();
            }

            GUIStyle arrowStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = pro
                        ? Color.Lerp(Color.white, accent, 0.25f)
                        : Color.Lerp(accent, Color.black, 0.2f)
                }
            };
            Rect foldoutRect = new(rect.x + 10f, rect.y, 14f, rect.height);
            GUI.Label(foldoutRect, expanded ? "▼" : "▶", arrowStyle);

            float x = rect.x + 28f;

            GUIContent iconContent = string.IsNullOrEmpty(iconName) ? null : EditorGUIUtility.IconContent(iconName);
            if (iconContent != null && iconContent.image != null && Event.current.type == EventType.Repaint)
            {
                Rect iconRect = new(x, rect.y + (rect.height - 16f) * 0.5f, 16f, 16f);
                Color oldGuiColor = GUI.color;
                GUI.color = pro ? Color.Lerp(Color.white, accent, 0.14f) : Color.Lerp(accent, Color.black, 0.15f);
                GUI.DrawTexture(iconRect, (Texture2D)iconContent.image, ScaleMode.ScaleToFit, true);
                GUI.color = oldGuiColor;
                x += 22f;
            }

            Color titleCol = pro ? titleColor : NeoInspectorTheme.TitleText;
            GUIStyle titleStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = titleCol }
            };

            Color pillBg = Color.Lerp(accent, pro ? Color.black : Color.white, expanded ? 0.05f : 0.12f);
            pillBg.a = expanded ? 0.95f : isHover ? 0.90f : 0.80f;
            GUIStyle countStyle = new(EditorStyles.miniBoldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = NeoInspectorTheme.ReadableOn(pillBg) }
            };
            string countText = count.ToString();
            float countWidth = Mathf.Max(26f, countStyle.CalcSize(new GUIContent(countText)).x + 16f);

            Rect titleRect = new(x, rect.y, rect.width - x - countWidth - 22f, rect.height);
            GUI.Label(titleRect, title, titleStyle);

            Rect countBgRect = new(rect.xMax - countWidth - 10f, rect.y + (rect.height - 18f) * 0.5f, countWidth, 18f);
            NeoInspectorTheme.DrawRoundedRect(countBgRect, pillBg, NeoInspectorTheme.RadiusPill);
            GUI.Label(countBgRect, countText, countStyle);

            return expanded;
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
