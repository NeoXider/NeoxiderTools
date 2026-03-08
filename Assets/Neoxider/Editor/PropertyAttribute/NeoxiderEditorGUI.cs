using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public static class NeoxiderEditorGUI
    {
        private static Texture2D _cardBackgroundTexture;
        private static Texture2D _sectionBackgroundTexture;
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

        private static GUIStyle _summaryTitleStyle;
        private static GUIStyle _summarySubtitleStyle;
        private static GUIStyle _sectionTitleStyle;
        private static GUIStyle _sectionSubtitleStyle;
        private static GUIStyle _badgeLabelStyle;
        private static GUIStyle _cardBoxStyle;
        private static GUIStyle _compactCardBoxStyle;
        private static GUIStyle _sectionBoxStyle;
        private static GUIStyle _captionStyle;
        private static GUIStyle _keyLabelStyle;
        private static GUIStyle _valueLabelStyle;

        public static void DrawSummaryCard(string title, string subtitle, params Badge[] badges)
        {
            DrawSummaryCard(title, subtitle, false, badges);
        }

        public static void DrawSummaryCard(string title, string subtitle, bool compact, params Badge[] badges)
        {
            Rect rect = EditorGUILayout.BeginVertical(compact ? CompactCardBoxStyle : CardBoxStyle);
            if (Event.current.type == EventType.Repaint)
            {
                DrawCardChrome(rect, new Color(0.24f, 0.58f, 0.92f, 1f), compact ? 4f : 6f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, SummaryTitleStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Overview", EditorStyles.miniBoldLabel, GUILayout.Width(56f));
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, SummarySubtitleStyle);
            }

            if (badges != null && badges.Length > 0)
            {
                EditorGUILayout.Space(compact ? 1f : 4f);
                DrawBadges(badges);
            }

            EditorGUILayout.EndVertical();
        }

        public static void BeginSection(string title, string subtitle = null)
        {
            Rect rect = EditorGUILayout.BeginVertical(SectionBoxStyle);
            if (Event.current.type == EventType.Repaint)
            {
                DrawCardChrome(rect, new Color(0.54f, 0.36f, 0.96f, 1f), 4f);
            }

            EditorGUILayout.LabelField(title, SectionTitleStyle);

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, SectionSubtitleStyle);
            }

            EditorGUILayout.Space(2f);
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

                GUILayout.Label(key, KeyLabelStyle, GUILayout.Width(110f));
                GUI.contentColor = previous;
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
                float width = Mathf.Max(64f, textSize.x + 16f);
                Rect badgeRect = new(x, y, width, 18f);

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(badgeRect, badge.BackgroundColor);
                    EditorGUI.DrawRect(new Rect(badgeRect.x, badgeRect.yMax - 1f, badgeRect.width, 1f),
                        new Color(1f, 1f, 1f, 0.12f));
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

        private static void DrawCardChrome(Rect rect, Color accent, float topStripeHeight)
        {
            Rect stripe = new(rect.x, rect.y, rect.width, topStripeHeight);
            EditorGUI.DrawRect(stripe, new Color(accent.r, accent.g, accent.b, 0.9f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), new Color(accent.r, accent.g, accent.b, 0.95f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.06f));
        }

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
                richText = true,
                normal = { textColor = new Color(0.82f, 0.86f, 0.92f, 1f) }
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
                normal = { textColor = Color.white },
                padding = new RectOffset(6, 6, 2, 2),
                clipping = TextClipping.Clip
            };

        private static GUIStyle CaptionStyle =>
            _captionStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                richText = true,
                normal = { textColor = new Color(0.72f, 0.76f, 0.83f, 1f) }
            };

        private static GUIStyle KeyLabelStyle =>
            _keyLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.68f, 0.74f, 0.82f, 1f) }
            };

        private static GUIStyle ValueLabelStyle =>
            _valueLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                richText = true,
                normal = { textColor = new Color(0.92f, 0.94f, 0.97f, 1f) }
            };

        private static GUIStyle CardBoxStyle =>
            _cardBoxStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 9, 8),
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = CreateColorTexture(ref _cardBackgroundTexture, new Color(0.16f, 0.17f, 0.21f, 0.96f)) }
            };

        private static GUIStyle CompactCardBoxStyle =>
            _compactCardBoxStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = CreateColorTexture(ref _cardBackgroundTexture, new Color(0.16f, 0.17f, 0.21f, 0.96f)) }
            };

        private static GUIStyle SectionBoxStyle =>
            _sectionBoxStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 12, 10),
                margin = new RectOffset(4, 4, 3, 3),
                normal = { background = CreateColorTexture(ref _sectionBackgroundTexture, new Color(0.14f, 0.15f, 0.19f, 0.96f)) }
            };

        private static Texture2D CreateColorTexture(ref Texture2D texture, Color color)
        {
            if (texture != null)
            {
                return texture;
            }

            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
