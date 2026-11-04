using System;
using UnityEditor;
using UnityEngine;
using GUI = UnityEngine.GUI;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private static bool _neoDocBoxIsDark;
        private static readonly Color DocsAccent = new(0.36f, 0.60f, 1f, 1f);
        private static readonly GUIContent s_openDocContent = new("Open in window",
            "Open the full page in the Markdown viewer (or select the file when no viewer is installed).");

        private static GUIStyle GetNeoDocBoxStyle()
        {
            bool dark = NeoInspectorTheme.IsDark;
            if (_neoDocBoxStyle != null && _neoDocBoxIsDark == dark && _neoDocDarkTexture != null)
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
                padding = new RectOffset(12, 12, 10, 10),
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
            TypeFoldoutKeys keys = GetTypeFoldoutKeys(type);
            string key = keys.DocFoldout;
            bool expanded = _neoFoldouts.TryGetValue(key, out bool v) && v;
            float amount = GetFoldAmount(key, expanded);

            // WHY: a doc is either linked or not - the old "1"/"0" count pill carried no information.
            expanded = DrawNeoSectionHeader(expanded, amount, "Documentation", -1,
                string.IsNullOrEmpty(docPath) ? NeoInspectorTheme.MutedText : DocsAccent, "TextAsset Icon", false);
            _neoFoldouts[key] = expanded;
            amount = GetFoldAmount(key, expanded);

            if (!BeginSectionBody(amount, DocsAccent, out bool fading))
            {
                return;
            }

            if (!string.IsNullOrEmpty(docPath))
            {
                // WHY: an inherited or convention page must not hide a mistyped [NeoDoc] on the type itself.
                string ownBrokenLink = NeoDocHelper.GetOwnBrokenNeoDocPath(_cachedNeoxiderRootPath, type);
                if (ownBrokenLink != null)
                {
                    EditorGUILayout.HelpBox(GetDocLinkMessage(type, ownBrokenLink, true), MessageType.Warning);
                }

                DrawDocumentationPreview(docPath, keys.DocScroll);
            }
            else
            {
                string missingLink = NeoDocHelper.GetBrokenNeoDocPath(_cachedNeoxiderRootPath, type);
                EditorGUILayout.HelpBox(GetDocLinkMessage(type, missingLink, false),
                    missingLink != null ? MessageType.Warning : MessageType.Info);
            }

            EndSectionBody(fading);
        }

        private static Type s_docMessageType;
        private static string s_docMessageLink;
        private static bool s_docMessageFallback;
        private static string s_docMessage;

        /// <summary>
        ///     Help text of the Documentation section: a broken <paramref name="brokenLink" /> (with or without a
        ///     fallback page shown below it) or "no documentation linked" when it is null. Rebuilt only when the
        ///     inspected type or the link changes.
        /// </summary>
        internal static string GetDocLinkMessage(Type type, string brokenLink, bool fallbackShown)
        {
            if (s_docMessage != null && s_docMessageType == type && s_docMessageFallback == fallbackShown &&
                string.Equals(s_docMessageLink, brokenLink, StringComparison.Ordinal))
            {
                return s_docMessage;
            }

            s_docMessageType = type;
            s_docMessageLink = brokenLink;
            s_docMessageFallback = fallbackShown;
            if (brokenLink == null)
            {
                s_docMessage = "No documentation linked. Add [NeoDoc(\"Module/File.md\")] or place " +
                               (type != null ? type.Name : "TypeName") + ".md in Docs.";
            }
            else
            {
                s_docMessage = "[NeoDoc] points to \"" + brokenLink + "\", but that page does not exist under Docs." +
                               (fallbackShown
                                   ? " Showing the fallback page (TypeName.md or a base type's page) instead."
                                   : string.Empty);
            }

            return s_docMessage;
        }

        private void DrawDocumentationPreview(string docPath, string scrollKey)
        {
            string richText = NeoDocHelper.GetDocRichTextPreview(docPath, 40);
            if (richText != null)
            {
                if (!_neoDocScrollPositions.TryGetValue(scrollKey, out Vector2 scroll))
                {
                    scroll = Vector2.zero;
                }

                const float docAreaMinHeight = 160f;
                const float docAreaMaxHeight = 360f;
                const float docHorizontalPadding = 12f * 2f;
                GUIStyle docStyle = NeoInspectorStyles.DocBody;
                float contentWidth = Mathf.Max(100f,
                    EditorGUIUtility.currentViewWidth - 72f - docHorizontalPadding);
                float contentHeight = docStyle.CalcHeight(NeoInspectorStyles.TempContent(richText), contentWidth);
                float areaHeight = Mathf.Round(Mathf.Clamp(contentHeight + 24f, docAreaMinHeight, docAreaMaxHeight));
                NeoInspectorStyles.Space(2);
                scroll = EditorGUILayout.BeginScrollView(scroll, false, false, NeoInspectorStyles.Height(areaHeight));
                _neoDocScrollPositions[scrollKey] = scroll;
                EditorGUILayout.BeginVertical(GetNeoDocBoxStyle());
                EditorGUILayout.LabelField(richText, docStyle);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                NeoInspectorStyles.Space(4);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                Rect chipRect = GUILayoutUtility.GetRect(150f, 22f, NeoInspectorStyles.Width(150f));
                if (Event.current.type == EventType.Repaint)
                {
                    Rect r = NeoInspectorTheme.Snap(chipRect);
                    float hover = NeoInspectorAnimation.Hover(NeoInspectorAnimation.ControlKey(r),
                        r.Contains(Event.current.mousePosition));
                    NeoInspectorTheme.DrawRoundedRect(r, Color.Lerp(DocsAccent, Color.white, 0.12f * hover),
                        new Color(1f, 1f, 1f, 0.18f + 0.18f * hover), NeoInspectorTheme.RadiusButton - 1f, 1f);

                    Texture linkIcon = NeoInspectorStyles.Icon("TextAsset Icon");
                    float textOffset = 0f;
                    if (linkIcon != null)
                    {
                        Rect iconRect = new(r.x + 10f, r.y + (r.height - 14f) * 0.5f, 14f, 14f);
                        GUI.DrawTexture(iconRect, linkIcon, ScaleMode.ScaleToFit, true);
                        textOffset = 12f;
                    }

                    GUI.Label(new Rect(r.x + textOffset, r.y, r.width - textOffset, r.height), s_openDocContent,
                        NeoInspectorStyles.DocChipText);
                }

                if (GUI.Button(chipRect, GUIContent.none, GUIStyle.none))
                {
                    NeoDocHelper.OpenDocInWindow(docPath);
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
