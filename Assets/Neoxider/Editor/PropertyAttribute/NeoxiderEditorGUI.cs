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
