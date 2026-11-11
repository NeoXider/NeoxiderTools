using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    /// <summary>
    ///     Project-wide list of <see cref="PageId" /> assets for the Pages inspectors, sorted by display name and
    ///     rebuilt only after the project changed.
    /// </summary>
    internal static class PageIdEditorCache
    {
        private const string NoneLabel = "<None>";

        private static string _folder;
        private static bool _dirty = true;
        private static PageId[] _ids;
        private static string[] _labels;
        private static GUIContent[] _labelContents;

        static PageIdEditorCache()
        {
            EditorApplication.projectChanged += MarkDirty;
        }

        public static void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>
        ///     Get all PageId assets in the project (folder = null) or in the given folder.
        /// </summary>
        public static PageId[] GetIds(string folder = null)
        {
            Ensure(folder);
            return _ids ?? Array.Empty<PageId>();
        }

        /// <summary>
        ///     Get dropdown labels: &lt;None&gt; + DisplayName of all PageId assets.
        /// </summary>
        public static string[] GetLabels(string folder = null)
        {
            Ensure(folder);
            return _labels ?? new[] { NoneLabel };
        }

        /// <summary>Same entries as <see cref="GetLabels" /> as cached GUIContent (no allocation per repaint).</summary>
        public static GUIContent[] GetLabelContents(string folder = null)
        {
            Ensure(folder);
            return _labelContents ?? new[] { new GUIContent(NoneLabel) };
        }

        /// <summary>Index of <paramref name="id" /> in <see cref="GetIds" />, or -1.</summary>
        public static int IndexOf(PageId id, string folder = null)
        {
            if (id == null)
            {
                return -1;
            }

            PageId[] ids = GetIds(folder);
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == id)
                {
                    return i;
                }
            }

            return -1;
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

            // WHY: null/empty folder means search the entire project (all folders).
            string[] searchIn = string.IsNullOrEmpty(folder) ? new[] { "Assets" } : new[] { folder };
            string[] guids = AssetDatabase.FindAssets("t:PageId", searchIn);
            List<PageId> found = new(guids.Length);
            foreach (string guid in guids)
            {
                PageId id = AssetDatabase.LoadAssetAtPath<PageId>(AssetDatabase.GUIDToAssetPath(guid));
                if (id != null)
                {
                    found.Add(id);
                }
            }

            found.Sort(CompareByDisplayName);
            _ids = found.ToArray();

            _labels = new string[_ids.Length + 1];
            _labelContents = new GUIContent[_ids.Length + 1];
            _labels[0] = NoneLabel;
            _labelContents[0] = new GUIContent(NoneLabel);
            for (int i = 0; i < _ids.Length; i++)
            {
                _labels[i + 1] = _ids[i].DisplayName;
                _labelContents[i + 1] = new GUIContent(_labels[i + 1]);
            }
        }

        private static int CompareByDisplayName(PageId left, PageId right)
        {
            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
