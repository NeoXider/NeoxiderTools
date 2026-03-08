using System;
using UnityEditor;
using UnityEngine;
using GUI = UnityEngine.GUI;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private static GUIStyle GetNeoDocBoxStyle()
        {
            if (_neoDocBoxStyle != null)
            {
                return _neoDocBoxStyle;
            }

            _neoDocDarkTexture = new Texture2D(1, 1);
            _neoDocDarkTexture.SetPixel(0, 0, new Color(0.14f, 0.15f, 0.18f, 1f));
            _neoDocDarkTexture.Apply();
            _neoDocBoxStyle = new GUIStyle
            {
                padding = new RectOffset(18, 18, 14, 14),
                normal = { background = _neoDocDarkTexture }
            };
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
                Color accentDark = Color.Lerp(accentBase, Color.black, 0.45f);
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
                                    normal = { textColor = new Color(0.88f, 0.90f, 0.92f, 1f) }
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
                            GUIStyle openBtnStyle = new(EditorStyles.miniButton)
                            {
                                fixedHeight = 24,
                                fontStyle = FontStyle.Bold,
                                alignment = TextAnchor.MiddleCenter,
                                normal = { textColor = Color.white },
                                hover = { textColor = Color.white },
                                active = { textColor = Color.white },
                                focused = { textColor = Color.white }
                            };
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                Rect btnRect = GUILayoutUtility.GetRect(new GUIContent(" Open in window "),
                                    openBtnStyle, GUILayout.MinWidth(140), GUILayout.Height(24));
                                bool isHover = btnRect.Contains(Event.current.mousePosition);
                                Color prevBg = GUI.backgroundColor;
                                GUI.backgroundColor = isHover ? Color.Lerp(docAccent, Color.white, 0.3f) : docAccent;
                                if (GUI.Button(btnRect, " Open in window ", openBtnStyle))
                                {
                                    NeoDocHelper.OpenDocInWindow(docPath);
                                }

                                GUI.backgroundColor = prevBg;
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
