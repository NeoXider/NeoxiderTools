using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    public static class PageIdGenerator
    {
        private const string DefaultFolder = "Assets/NeoxiderPages/Pages";

        public static string DefaultFolderPath => DefaultFolder;

        /// <summary>
        ///     Папка для создания новых PageId: существующая папка с PageId, иначе DefaultFolder.
        /// </summary>
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

        public static PageId GetOrCreate(string pageName, string folder = null)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return null;
            }

            folder ??= GetPreferredFolder();
            EnsureFolder(folder);

            string normalized = NormalizeAssetName(pageName);
            string assetPath = $"{folder}/{normalized}.asset";

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

        [MenuItem("Tools/Neoxider/Pages/Generate Default PageIds")]
        public static void GenerateDefaultPageIds()
        {
            string folder = GetPreferredFolder();
            EnsureFolder(folder);

            string[] defaults =
            {
                "PageOpen",
                "PageMenu",
                "PageSettings",
                "PageShop",
                "PageLeader",
                "PageInfo",
                "PageLevels",
                "PageGame",
                "PageWin",
                "PageLose",
                "PagePause",
                "PageEnd",
                "PageMain",
                "PageGrade",
                "PageBonus",
                "PageInventory",
                "PageMap",
                "PagePrivacy",
                "PageOther"
            };

            int created = 0;
            int existed = 0;

            foreach (string name in defaults)
            {
                string assetPath = $"{folder}/{name}.asset";
                bool alreadyExists = AssetDatabase.LoadAssetAtPath<PageId>(assetPath) != null;

                PageId pageId = GetOrCreate(name, folder);
                if (pageId == null)
                {
                    continue;
                }

                if (alreadyExists)
                {
                    existed++;
                }
                else
                {
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PageIdGenerator] Done. Created: {created}, existed: {existed}. Folder: {folder}");
        }

        private static string NormalizeAssetName(string pageName)
        {
            string trimmed = pageName.Trim();

            // Разрешаем ввод как "Menu", так и "PageMenu"
            if (!trimmed.StartsWith("Page", StringComparison.Ordinal))
            {
                trimmed = "Page" + trimmed;
            }

            // Чистим запрещенные символы для имени ассета
            trimmed = Regex.Replace(trimmed, @"[^a-zA-Z0-9_]+", "");
            return string.IsNullOrWhiteSpace(trimmed) ? "Page" : trimmed;
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
                throw new ArgumentException($"Invalid folder path: {folder}");
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