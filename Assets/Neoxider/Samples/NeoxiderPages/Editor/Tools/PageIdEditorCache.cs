using System;
using System.Linq;
using UnityEditor;

namespace Neo.Pages.Editor
{
    internal static class PageIdEditorCache
    {
        private static string _folder;
        private static bool _dirty = true;
        private static PageId[] _ids;
        private static string[] _labels;

        static PageIdEditorCache()
        {
            EditorApplication.projectChanged += MarkDirty;
        }

        public static void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>
        ///     Получить все PageId в проекте (folder = null) или в указанной папке.
        /// </summary>
        public static PageId[] GetIds(string folder = null)
        {
            Ensure(folder);
            return _ids ?? Array.Empty<PageId>();
        }

        /// <summary>
        ///     Получить подписи для dropdown: &lt;None&gt; + DisplayName всех PageId.
        /// </summary>
        public static string[] GetLabels(string folder = null)
        {
            Ensure(folder);
            return _labels ?? new[] { "<None>" };
        }

        private static void Ensure(string folder)
        {
            string cacheKey = string.IsNullOrEmpty(folder) ? "Assets" : folder;

            if (!_dirty && _ids != null && string.Equals(_folder, cacheKey, StringComparison.Ordinal))
            {
                return;
            }

            _folder = cacheKey;
            _dirty = false;

            // null/empty = ищем по всему проекту (все папки)
            string[] searchIn = string.IsNullOrEmpty(folder) ? new[] { "Assets" } : new[] { folder };
            string[] guids = AssetDatabase.FindAssets("t:PageId", searchIn);
            _ids = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<PageId>)
                .Where(x => x != null)
                .OrderBy(x => x.DisplayName)
                .ToArray();

            _labels = new string[_ids.Length + 1];
            _labels[0] = "<None>";
            for (int i = 0; i < _ids.Length; i++)
            {
                _labels[i + 1] = _ids[i].DisplayName;
            }
        }
    }
}