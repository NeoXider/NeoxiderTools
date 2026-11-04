using System;
using System.IO;
using Neo.Pages;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    /// <summary>
    ///     Creates <see cref="PageId" /> assets for the Pages inspectors ("Generate &amp; Assign") and the
    ///     <c>Neoxider &gt; Samples &gt; Generate Default PageIds</c> menu.
    /// </summary>
    internal static class PageIdGenerator
    {
        private const string LegacyFolder = "Assets/Neoxider/NeoxiderPages/Pages";
        private const string DefaultFolder = "Assets/Pages";

        /// <summary>The PageIds the Pages runtime looks up by name (PM, PageSubscriber) plus common screens.</summary>
        internal static readonly string[] DefaultPageNames =
        {
            "PageMenu", "PageGame", "PagePause", "PageSettings", "PageShop", "PageWin", "PageLose", "PageEnd"
        };

        /// <summary>
        ///     Folder for new PageIds: the legacy sample folder when present, otherwise the folder of an existing
        ///     PageId, otherwise <c>Assets/Pages</c>.
        /// </summary>
        public static string GetPreferredFolder()
        {
            if (AssetDatabase.IsValidFolder(LegacyFolder))
            {
                return LegacyFolder;
            }

            string[] guids = AssetDatabase.FindAssets("t:PageId", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir))
                {
                    return dir;
                }
            }

            return DefaultFolder;
        }

        /// <summary>Asset name for a page name: "Menu" -> "PageMenu"; names already starting with "Page" are kept.</summary>
        public static string ToAssetName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return string.Empty;
            }

            string normalized = displayName.Trim();
            return normalized.StartsWith("Page", StringComparison.OrdinalIgnoreCase) ? normalized : "Page" + normalized;
        }

        /// <summary>Asset path <see cref="GetOrCreate" /> uses for <paramref name="displayName" /> in <paramref name="folder" />.</summary>
        public static string GetAssetPath(string displayName, string folder)
        {
            return $"{folder}/{ToAssetName(displayName)}.asset";
        }

        public static PageId GetOrCreate(string displayName, string folder)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return null;
            }

            EnsureFolder(folder);

            string assetPath = GetAssetPath(displayName, folder);
            PageId existing = AssetDatabase.LoadAssetAtPath<PageId>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            PageId instance = ScriptableObject.CreateInstance<PageId>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            PageIdEditorCache.MarkDirty();
            return instance;
        }

        [MenuItem("Neoxider/Samples/Generate Default PageIds", false, 302)]
        private static void GenerateDefaultPageIds()
        {
            string folder = GetPreferredFolder();
            int created = 0;
            foreach (string pageName in DefaultPageNames)
            {
                bool existed = AssetDatabase.LoadAssetAtPath<PageId>(GetAssetPath(pageName, folder)) != null;
                if (GetOrCreate(pageName, folder) != null && !existed)
                {
                    created++;
                }
            }

            Debug.Log("[Pages] Default PageIds in " + folder + ": " + created.ToString() + " created, " +
                      (DefaultPageNames.Length - created).ToString() + " already existed.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder));
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] parts = folder.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets")
            {
                return;
            }

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
