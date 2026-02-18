using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
            if (componentType == null) return null;
            object[] attrs = componentType.GetCustomAttributes(false);
            foreach (object a in attrs)
            {
                if (a != null && a.GetType().Name == "NeoDocAttribute")
                {
                    var pathProp = a.GetType().GetProperty("DocPath", BindingFlags.Public | BindingFlags.Instance);
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
                return null;
            string path = packageRoot.TrimEnd('/') + "/" + DocsFolder + "/" + relativePathFromDocs.TrimStart('/');
            return path.Replace('\\', '/');
        }

        /// <summary>Tries to find a doc by convention: TypeName.md under packageRoot/Docs.</summary>
        public static string FindDocByConvention(string packageRoot, Type componentType)
        {
            if (componentType == null || string.IsNullOrEmpty(packageRoot)) return null;
            string docsPath = packageRoot.TrimEnd('/') + "/" + DocsFolder;
            string name = componentType.Name + ".md";
            string[] guids = AssetDatabase.FindAssets(name + " t:TextAsset", new[] { docsPath });
            if (guids == null || guids.Length == 0) return null;
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p != null && p.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                    return p;
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
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(resolved);
                    if (asset != null) return resolved;
                }
            }
            return FindDocByConvention(packageRoot, componentType);
        }

        /// <summary>Returns first few lines of .md for preview, or null.</summary>
        public static string GetDocPreview(string fullPath, int maxLines = 3)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
            if (asset == null || string.IsNullOrEmpty(asset.text)) return null;
            string[] lines = asset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int take = Mathf.Min(maxLines, lines.Length);
            if (take == 0) return null;
            return string.Join("\n", lines, 0, take).Trim();
        }

        /// <summary>Opens the .md in MarkdownRenderer window if available, else selects and pings the asset.</summary>
        public static void OpenDocInWindow(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return;
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
            if (asset == null)
            {
                Debug.LogWarning("[NeoDoc] Asset not found: " + fullPath);
                return;
            }

            if (TryOpenWithMarkdownRenderer(fullPath))
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static bool TryOpenWithMarkdownRenderer(string fullPath)
        {
            try
            {
                Type viewerType = Type.GetType("UIMarkdownRenderer.MarkdownViewer, Rtl.MarkdownRenderer.Editor", false)
                    ?? Type.GetType("UIMarkdownRenderer.MarkdownViewer, com.rtl.markdownrenderer", false);
                if (viewerType == null)
                    return false;
                MethodInfo openMethod = viewerType.GetMethod("Open", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (openMethod == null)
                    return false;
                openMethod.Invoke(null, new object[] { fullPath });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
