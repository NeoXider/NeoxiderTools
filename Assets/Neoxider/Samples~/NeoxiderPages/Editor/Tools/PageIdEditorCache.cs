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

        public static PageId[] GetIds(string folder)
        {
            Ensure(folder);
            return _ids ?? Array.Empty<PageId>();
        }

        public static string[] GetLabels(string folder)
        {
            Ensure(folder);
            return _labels ?? new[] { "<None>" };
        }

        private static void Ensure(string folder)
        {
            folder ??= "Assets";

            if (!_dirty && _ids != null && string.Equals(_folder, folder, StringComparison.Ordinal))
            {
                return;
            }

            _folder = folder;
            _dirty = false;

            string[] guids = AssetDatabase.FindAssets("t:PageId", new[] { folder });
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