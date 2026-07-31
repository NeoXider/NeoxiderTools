using System;
using UnityEditor;
using UnityEngine;
using GUI = UnityEngine.GUI;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private static bool _neoDocBoxIsDark;

        private static GUIStyle GetNeoDocBoxStyle()
        {
            bool dark = NeoInspectorTheme.IsDark;
            if (_neoDocBoxStyle != null && _neoDocBoxIsDark == dark)
            {
                return _neoDocBoxStyle;
            }

            if (_neoDocDarkTexture == null)
            {
                _neoDocDarkTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            }

            _neoDocDarkTexture.SetPixel(0, 0, NeoInspectorTheme.SectionBackground);
            _neoDocDarkTexture.Apply();
            _neoDocBoxStyle = new GUIStyle
            {
                padding = new RectOffset(18, 18, 14, 14),
                normal = { background = _neoDocDarkTexture }
            };
            _neoDocBoxIsDark = dark;
            return _neoDocBoxStyle;
        }

        private void DrawDocumentationFoldout()
        {
            if (target == null || string.IsNullOrEmpty(_cachedNeoxiderRootPath))
            {
                return;
            }

            Type type = target.GetType();
            string docPath = NeoDocHelper.GetDocPathForType(_cachedNeoxiderRootPath, type);
            string key = "NeoDoc_Foldout_" + type.FullName;
            string scrollKey = "NeoDoc_Scroll_" + type.FullName;
            bool expanded = _neoFoldouts.TryGetValue(key, out bool v) && v;

            using (new EditorGUILayout.VerticalScope())
            {
                Color accentBase = new(0.35f, 0.6f, 1f, 1f);
                var accentDark = Color.Lerp(accentBase, Color.black, 0.45f);
                Color accent = expanded ? accentDark : accentBase;
                int count = string.IsNullOrEmpty(docPath) ? 0 : 1;

                expanded = DrawNeoSectionHeader(expanded, "Documentation", count, accent, "d_TextAsset Icon",
                    expanded ? Color.white : accentBase,
                    expanded
                        ? new Color(1f, 1f, 1f, 0.75f)
                        : new Color(accentBase.r, accentBase.g, accentBase.b, 0.75f));

                if (expanded)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (!string.IsNullOrEmpty(docPath))
                        {
                            string preview = NeoDocHelper.GetDocPreview(docPath, 40);
                            if (!string.IsNullOrEmpty(preview))
                            {
                                string richText = NeoDocHelper.MarkdownToUnityRichText(preview);
                                if (!_neoDocScrollPositions.TryGetValue(scrollKey, out Vector2 scroll))
                                {
                                    scroll = Vector2.zero;
                                }

                                const float docAreaMinHeight = 220f;
                                const float docAreaMaxHeight = 420f;
                                const float docHorizontalPadding = 18f * 2f;
                                GUIStyle docStyle = new(EditorStyles.label)
                                {
                                    wordWrap = true,
                                    richText = true,
                                    normal = { textColor = NeoInspectorTheme.IsDark ? new Color(0.88f, 0.90f, 0.92f, 1f) : new Color(0.18f, 0.20f, 0.24f, 1f) }
                                };
                                float contentWidth = Mathf.Max(100f,
                                    EditorGUIUtility.currentViewWidth - 60f - docHorizontalPadding);
                                float contentHeight = docStyle.CalcHeight(new GUIContent(richText), contentWidth);
                                EditorGUILayout.Space(4);
                                scroll = EditorGUILayout.BeginScrollView(scroll, false, true,
                                    GUILayout.MinHeight(docAreaMinHeight), GUILayout.MaxHeight(docAreaMaxHeight));
                                _neoDocScrollPositions[scrollKey] = scroll;
                                GUIStyle boxStyle = GetNeoDocBoxStyle();
                                EditorGUILayout.BeginVertical(boxStyle, GUILayout.MinHeight(contentHeight + 28));
                                EditorGUILayout.LabelField(richText, docStyle);
                                EditorGUILayout.EndVertical();
                                EditorGUILayout.EndScrollView();
                                EditorGUILayout.Space(4);
                            }

                            Color docAccent = new(0.35f, 0.6f, 1f, 1f);
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                Rect chipRect = GUILayoutUtility.GetRect(new GUIContent("Open in window"),
                                    EditorStyles.miniButton, GUILayout.MinWidth(150), GUILayout.Height(26));
                                bool isHover = chipRect.Contains(Event.current.mousePosition);

                                if (Event.current.type == EventType.Repaint)
                                {
                                    Color chipBg = isHover ? Color.Lerp(docAccent, Color.white, 0.12f) : docAccent;
                                    NeoInspectorTheme.DrawRoundedRect(chipRect, chipBg,
                                        new Color(1f, 1f, 1f, isHover ? 0.35f : 0.20f), NeoInspectorTheme.RadiusPill, 1f);

                                    GUIContent linkIcon = EditorGUIUtility.IconContent("d_TextAsset Icon");
                                    float textOffset = 0f;
                                    if (linkIcon != null && linkIcon.image != null)
                                    {
                                        Rect iconRect = new(chipRect.x + 12f, chipRect.y + 5f, 16f, 16f);
                                        GUI.DrawTexture(iconRect, linkIcon.image, ScaleMode.ScaleToFit, true);
                                        textOffset = 14f;
                                    }

                                    GUIStyle chipTextStyle = new(EditorStyles.boldLabel)
                                    {
                                        alignment = TextAnchor.MiddleCenter,
                                        normal = { textColor = Color.white }
                                    };
                                    GUI.Label(new Rect(chipRect.x + textOffset, chipRect.y, chipRect.width - textOffset,
                                        chipRect.height), "Open in window", chipTextStyle);
                                }

                                if (GUI.Button(chipRect, GUIContent.none, GUIStyle.none))
                                {
                                    NeoDocHelper.OpenDocInWindow(docPath);
                                }

                                GUILayout.FlexibleSpace();
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(
                                "No documentation linked. Add [NeoDoc(\"Module/File.md\")] or place " + type.Name +
                                ".md in Docs.", MessageType.Info);
                        }
                    }
                }
            }

            _neoFoldouts[key] = expanded;
        }
    }
}
