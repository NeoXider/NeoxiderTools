using System;
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

        /// <summary>Gets the [NeoDoc] path for the type, or null if not set.</summary>
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

        /// <summary>Gets the best doc path: from [NeoDoc] attribute, then by convention (TypeName.md).</summary>
        public static string GetDocPathForType(string packageRoot, Type componentType)
        {
            string fromAttr = GetNeoDocPathFromAttribute(componentType);
            if (!string.IsNullOrEmpty(fromAttr))
            {
                string resolved = ResolveDocPath(packageRoot, fromAttr);
                if (!string.IsNullOrEmpty(resolved))
                {
                    TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(resolved);
                    if (asset != null)
                    {
                        return resolved;
                    }
                }
            }

            return FindDocByConvention(packageRoot, componentType);
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

        /// <summary>Converts markdown to Unity rich text for Inspector preview: headings (size + bold + accent), bold.</summary>
        public static string MarkdownToUnityRichText(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return raw;
            }

            const string headingColor = "#C8E0FF";
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

                lines[i] = BoldToRich(line);
            }

            return string.Join("\n", lines);
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
                ApplyNeoxiderMarkdownStyle(); // only runs when package is present
                return;
            }

            /* Viewer unavailable (package not installed or error): select & ping .md in Project */
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
                // Style overlay is optional; do not break opening the window
            }
        }
    }
}