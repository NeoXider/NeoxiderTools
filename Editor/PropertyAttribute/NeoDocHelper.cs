using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.Editor
{
    /// <summary>
    ///     Resolves doc paths and opens .md in window. No hard reference to MarkdownRenderer; uses reflection when available.
    /// </summary>
    public static class NeoDocHelper
    {
        private const string DocsFolder = "Docs";

        /// <summary>Gets the [NeoDoc] path declared on the type itself, or null if not set.</summary>
        public static string GetNeoDocPathFromAttribute(Type componentType)
        {
            if (componentType == null)
            {
                return null;
            }

            object[] attrs = componentType.GetCustomAttributes(false);
            foreach (object a in attrs)
            {
                if (a != null && a.GetType().Name == "NeoDocAttribute")
                {
                    PropertyInfo pathProp =
                        a.GetType().GetProperty("DocPath", BindingFlags.Public | BindingFlags.Instance);
                    if (pathProp != null)
                    {
                        string path = pathProp.GetValue(a) as string;
                        return string.IsNullOrEmpty(path) ? null : path;
                    }
                }
            }

            return null;
        }

        /// <summary>Resolves full project-relative path: packageRoot + "/Docs/" + relativePath.</summary>
        public static string ResolveDocPath(string packageRoot, string relativePathFromDocs)
        {
            if (string.IsNullOrEmpty(packageRoot) || string.IsNullOrEmpty(relativePathFromDocs))
            {
                return null;
            }

            string path = packageRoot.TrimEnd('/') + "/" + DocsFolder + "/" + relativePathFromDocs.TrimStart('/');
            return path.Replace('\\', '/');
        }

        /// <summary>Tries to find a doc by convention: TypeName.md under packageRoot/Docs.</summary>
        public static string FindDocByConvention(string packageRoot, Type componentType)
        {
            if (componentType == null || string.IsNullOrEmpty(packageRoot))
            {
                return null;
            }

            string docsPath = packageRoot.TrimEnd('/') + "/" + DocsFolder;
            string name = componentType.Name + ".md";
            string[] guids = AssetDatabase.FindAssets(name + " t:TextAsset", new[] { docsPath });
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p != null && p.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }

            return null;
        }

        // WHY: The inspector asks for the doc path on every IMGUI event; resolving it loads a TextAsset and runs an
        // AssetDatabase.FindAssets query. The answer only changes when assets do, so cache it until projectChanged.
        private static readonly Dictionary<(string, Type), string> s_docPathCache = new();
        private static readonly Dictionary<(string, int, bool), string> s_richPreviewCache = new();
        private static readonly Dictionary<(string, Type), string> s_brokenLinkCache = new();
        private static readonly Dictionary<(string, Type), string> s_ownBrokenLinkCache = new();
        private static bool s_cacheInvalidationHooked;

        private static void HookCacheInvalidation()
        {
            if (s_cacheInvalidationHooked)
            {
                return;
            }

            s_cacheInvalidationHooked = true;
            EditorApplication.projectChanged += ClearCaches;
        }

        internal static void ClearCaches()
        {
            s_docPathCache.Clear();
            s_richPreviewCache.Clear();
            s_brokenLinkCache.Clear();
            s_ownBrokenLinkCache.Clear();
        }

        /// <summary>Gets the best doc path: from [NeoDoc] attribute, then by convention (TypeName.md).</summary>
        public static string GetDocPathForType(string packageRoot, Type componentType)
        {
            HookCacheInvalidation();
            (string, Type) key = (packageRoot, componentType);
            if (!s_docPathCache.TryGetValue(key, out string path))
            {
                path = ResolveDocPathForType(packageRoot, componentType);
                s_docPathCache[key] = path;
            }

            return path;
        }

        /// <summary>
        ///     Rich-text preview of the first <paramref name="maxLines" /> lines of a doc, converted once per
        ///     file until assets change. Null when the doc is missing or empty.
        /// </summary>
        public static string GetDocRichTextPreview(string fullPath, int maxLines)
        {
            HookCacheInvalidation();
            // WHY: the heading and code colours depend on the editor skin, so the converted text is cached per skin.
            (string, int, bool) key = (fullPath, maxLines, EditorGUIUtility.isProSkin);
            if (!s_richPreviewCache.TryGetValue(key, out string rich))
            {
                string preview = GetDocPreview(fullPath, maxLines);
                rich = string.IsNullOrEmpty(preview) ? null : MarkdownToUnityRichText(preview);
                s_richPreviewCache[key] = rich;
            }

            return rich;
        }

        /// <summary>
        ///     The [NeoDoc] path of <paramref name="componentType" /> (or its nearest documented base type) when that
        ///     page does not exist, so the inspector can report a broken link instead of "no documentation".
        ///     Null when the link resolves or no [NeoDoc] is declared.
        /// </summary>
        public static string GetBrokenNeoDocPath(string packageRoot, Type componentType)
        {
            HookCacheInvalidation();
            (string, Type) key = (packageRoot, componentType);
            if (!s_brokenLinkCache.TryGetValue(key, out string broken))
            {
                broken = FindBrokenNeoDocPath(packageRoot, componentType);
                s_brokenLinkCache[key] = broken;
            }

            return broken;
        }

        /// <summary>
        ///     The [NeoDoc] path declared on <paramref name="componentType" /> itself when that page does not exist,
        ///     even if an inherited or convention page is shown instead; null when the type declares no [NeoDoc] or
        ///     its link resolves.
        /// </summary>
        internal static string GetOwnBrokenNeoDocPath(string packageRoot, Type componentType)
        {
            HookCacheInvalidation();
            (string, Type) key = (packageRoot, componentType);
            if (!s_ownBrokenLinkCache.TryGetValue(key, out string broken))
            {
                string fromAttr = GetNeoDocPathFromAttribute(componentType);
                broken = !string.IsNullOrEmpty(fromAttr) && ResolveExistingNeoDoc(packageRoot, componentType) == null
                    ? fromAttr
                    : null;
                s_ownBrokenLinkCache[key] = broken;
            }

            return broken;
        }

        private static string FindBrokenNeoDocPath(string packageRoot, Type componentType)
        {
            for (Type type = componentType; type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                string fromAttr = GetNeoDocPathFromAttribute(type);
                if (string.IsNullOrEmpty(fromAttr))
                {
                    continue;
                }

                string resolved = ResolveDocPath(packageRoot, fromAttr);
                return !string.IsNullOrEmpty(resolved) && AssetDatabase.LoadAssetAtPath<TextAsset>(resolved) != null
                    ? null
                    : fromAttr;
            }

            return null;
        }

        private static string ResolveExistingNeoDoc(string packageRoot, Type type)
        {
            string fromAttr = GetNeoDocPathFromAttribute(type);
            if (string.IsNullOrEmpty(fromAttr))
            {
                return null;
            }

            string resolved = ResolveDocPath(packageRoot, fromAttr);
            return !string.IsNullOrEmpty(resolved) && AssetDatabase.LoadAssetAtPath<TextAsset>(resolved) != null
                ? resolved
                : null;
        }

        // WHY: [NeoDoc] is not inherited (AttributeUsage Inherited = false), so a subclass of a documented component
        // showed "No documentation linked". Order: own link, own page by convention, then the nearest base link.
        private static string ResolveDocPathForType(string packageRoot, Type componentType)
        {
            string own = ResolveExistingNeoDoc(packageRoot, componentType);
            if (own != null)
            {
                return own;
            }

            string byConvention = FindDocByConvention(packageRoot, componentType);
            if (byConvention != null)
            {
                return byConvention;
            }

            for (Type type = componentType?.BaseType; type != null && type != typeof(MonoBehaviour) &&
                                                      type != typeof(ScriptableObject); type = type.BaseType)
            {
                string inherited = ResolveExistingNeoDoc(packageRoot, type);
                if (inherited != null)
                {
                    return inherited;
                }
            }

            return null;
        }

        /// <summary>Returns first few lines of .md for preview, or null.</summary>
        public static string GetDocPreview(string fullPath, int maxLines = 3)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                return null;
            }

            string[] lines = asset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int take = Mathf.Min(maxLines, lines.Length);
            if (take == 0)
            {
                return null;
            }

            return string.Join("\n", lines, 0, take).Trim();
        }

        /// <summary>
        ///     Converts markdown to Unity rich text for the Inspector preview: headings (size + bold + accent), bold,
        ///     inline code, links (text only) and bullet points. Colours follow the editor skin so headings stay
        ///     readable on the light skin too.
        /// </summary>
        public static string MarkdownToUnityRichText(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return raw;
            }

            bool dark = EditorGUIUtility.isProSkin;
            string headingColor = dark ? "#C8E0FF" : "#23407A";
            string codeColor = dark ? "#F2C879" : "#8A4B00";
            string linkColor = dark ? "#8EC5FF" : "#1C5DB8";
            string[] lines = raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("#### "))
                {
                    lines[i] = $"<size=11><b><color={headingColor}>{EscapeRich(line.Substring(5))}</color></b></size>";
                    continue;
                }

                if (line.StartsWith("### "))
                {
                    lines[i] = $"<size=12><b><color={headingColor}>{EscapeRich(line.Substring(4))}</color></b></size>";
                    continue;
                }

                if (line.StartsWith("## "))
                {
                    lines[i] = $"<size=14><b><color={headingColor}>{EscapeRich(line.Substring(3))}</color></b></size>";
                    continue;
                }

                if (line.StartsWith("# "))
                {
                    lines[i] = $"<size=16><b><color={headingColor}>{EscapeRich(line.Substring(2))}</color></b></size>";
                    continue;
                }

                if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
                {
                    line = "  •  " + line.Substring(2);
                }

                lines[i] = BoldToRich(LinksToRich(CodeToRich(line, codeColor), linkColor));
            }

            return string.Join("\n", lines);
        }

        /// <summary>Wraps `inline code` spans in the code colour (backticks removed).</summary>
        private static string CodeToRich(string s, string color)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('`') < 0)
            {
                return s;
            }

            string[] parts = s.Split('`');
            if (parts.Length < 3)
            {
                return s;
            }

            System.Text.StringBuilder builder = new(s.Length + 32);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1 && i < parts.Length - 1)
                {
                    builder.Append("<color=").Append(color).Append('>').Append(EscapeRich(parts[i]))
                        .Append("</color>");
                }
                else
                {
                    if (i % 2 == 1)
                    {
                        builder.Append('`');
                    }

                    builder.Append(parts[i]);
                }
            }

            return builder.ToString();
        }

        /// <summary>Replaces [text](target) with the link text in the link colour; the target is not clickable here.</summary>
        private static string LinksToRich(string s, string color)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf("](", StringComparison.Ordinal) < 0)
            {
                return s;
            }

            System.Text.StringBuilder builder = new(s.Length);
            int index = 0;
            while (index < s.Length)
            {
                int open = s.IndexOf('[', index);
                int middle = open >= 0 ? s.IndexOf("](", open, StringComparison.Ordinal) : -1;
                int close = middle >= 0 ? s.IndexOf(')', middle + 2) : -1;
                if (open < 0 || middle < 0 || close < 0)
                {
                    builder.Append(s, index, s.Length - index);
                    break;
                }

                builder.Append(s, index, open - index);
                builder.Append("<color=").Append(color).Append('>')
                    .Append(s, open + 1, middle - open - 1).Append("</color>");
                index = close + 1;
            }

            return builder.ToString();
        }

        private static string EscapeRich(string s)
        {
            return s.Replace("<", "<\u200B").Replace(">", ">\u200B");
        }

        private static string BoldToRich(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.Contains("**"))
            {
                return s;
            }

            string[] parts = s.Split(new[] { "**" }, StringSplitOptions.None);
            for (int i = 1; i < parts.Length; i += 2)
            {
                parts[i] = "<b>" + parts[i] + "</b>";
            }

            return string.Concat(parts);
        }

        /// <summary>Opens the .md in MarkdownRenderer window if available, else selects and pings the asset.</summary>
        public static void OpenDocInWindow(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return;
            }

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
            if (asset == null)
            {
                Debug.LogWarning("[NeoDoc] Asset not found: " + fullPath);
                return;
            }

            if (TryOpenWithMarkdownRenderer(fullPath))
            {
                ApplyNeoxiderMarkdownStyle(); // WHY: only runs when package is present
                return;
            }

            // WHY: Viewer unavailable (package not installed or error): select & ping .md in Project
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static bool TryOpenWithMarkdownRenderer(string fullPath)
        {
            try
            {
                Type viewerType = GetMarkdownViewerType();
                if (viewerType == null)
                {
                    return false;
                }

                MethodInfo openMethod = viewerType.GetMethod("Open", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(string) }, null);
                if (openMethod == null)
                {
                    return false;
                }

                openMethod.Invoke(null, new object[] { fullPath });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Type GetMarkdownViewerType()
        {
            return Type.GetType("UIMarkdownRenderer.MarkdownViewer, Rtl.MarkdownRenderer.Editor", false)
                   ?? Type.GetType("UIMarkdownRenderer.MarkdownViewer, com.rtl.markdownrenderer", false);
        }

        /// <summary>Applies Neoxider dark theme overlay to the Markdown Viewer window (call after Open).</summary>
        private static void ApplyNeoxiderMarkdownStyle()
        {
            try
            {
                Type viewerType = GetMarkdownViewerType();
                if (viewerType == null)
                {
                    return;
                }

                MethodInfo getWindowMethod = null;
                foreach (MethodInfo m in typeof(EditorWindow).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "GetWindow")
                    {
                        continue;
                    }

                    ParameterInfo[] p = m.GetParameters();
                    if (p.Length < 1 || p[0].ParameterType != typeof(Type))
                    {
                        continue;
                    }

                    getWindowMethod = m;
                    break;
                }

                if (getWindowMethod == null)
                {
                    return;
                }

                object[] args = getWindowMethod.GetParameters().Length == 1
                    ? new object[] { viewerType }
                    : new object[] { viewerType, false, null, true };
                EditorWindow window = (EditorWindow)getWindowMethod.Invoke(null, args);
                if (window?.rootVisualElement == null)
                {
                    return;
                }

                string[] guids = AssetDatabase.FindAssets("NeoMarkdownOverrides t:StyleSheet");
                if (guids == null || guids.Length == 0)
                {
                    return;
                }

                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet == null)
                {
                    return;
                }

                if (!window.rootVisualElement.styleSheets.Contains(sheet))
                {
                    window.rootVisualElement.styleSheets.Add(sheet);
                }
            }
            catch
            {
                // WHY: Style overlay is optional; do not break opening the window
            }
        }
    }
}
