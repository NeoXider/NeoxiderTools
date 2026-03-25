using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Finds assets across the project (Assets + Packages). Used by editor tools
    ///     (script icons, NeoLogo, etc.) so logic is not duplicated when the library lives in different roots.
    /// </summary>
    public static class NeoxiderEditorAssets
    {
        /// <summary>
        ///     Finds the first asset by name/type across the project (Assets and Packages).
        /// </summary>
        /// <param name="nameOrFilter">Asset name or search filter (e.g. "NeoLogo" or "NeoLogo t:Texture2D").</param>
        /// <param name="typeFilter">Optional type filter for FindAssets, e.g. "Texture2D", "MonoScript".</param>
        /// <returns>Asset path or null.</returns>
        public static string FindAssetPath(string nameOrFilter, string typeFilter = null)
        {
            string filter = string.IsNullOrEmpty(typeFilter) ? nameOrFilter : $"{nameOrFilter} t:{typeFilter}";
            string[] guids = AssetDatabase.FindAssets(filter);
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        /// <summary>
        ///     Loads the first asset found by name/filter across the project (Assets + Packages).
        /// </summary>
        public static T FindAndLoad<T>(string nameOrFilter, string typeFilter = null) where T : Object
        {
            string path = FindAssetPath(nameOrFilter, typeFilter ?? typeof(T).Name);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>
        ///     Finds the NeoLogo texture in the project or packages (for component icons, etc.).
        /// </summary>
        public static Texture2D FindNeoLogo()
        {
            return FindAndLoad<Texture2D>("NeoLogo", "Texture2D");
        }
    }
}
