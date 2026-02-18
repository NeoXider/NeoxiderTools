using System;
using System.IO;
using Neo.Pages;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    internal static class PageIdGenerator
    {
        private const string DefaultFolder = "Assets/Neoxider/NeoxiderPages/Pages";

        public static string GetPreferredFolder()
        {
            if (AssetDatabase.IsValidFolder(DefaultFolder))
            {
                return DefaultFolder;
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

        public static PageId GetOrCreate(string displayName, string folder)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return null;
            }

            EnsureFolder(folder);

            string normalized = displayName.Trim();
            string assetName = normalized.StartsWith("Page", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : "Page" + normalized;

            string assetPath = $"{folder}/{assetName}.asset";
            PageId existing = AssetDatabase.LoadAssetAtPath<PageId>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            PageId instance = ScriptableObject.CreateInstance<PageId>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return instance;
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
