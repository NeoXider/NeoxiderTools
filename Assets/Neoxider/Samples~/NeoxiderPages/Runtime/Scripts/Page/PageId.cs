using UnityEngine;

namespace Neo.Pages
{
    /// <summary>
    ///     Page identifier as an asset (ScriptableObject).
    ///     Used for extensible page selection without editing an enum.
    /// </summary>
    [CreateAssetMenu(menuName = "Neoxider/Pages/Page Id", fileName = "Page")]
    public sealed class PageId : ScriptableObject
    {
        /// <summary>
        ///     Stable page key.
        ///     By default derived from the asset name.
        ///     Recommended format: <c>PageMenu</c>, <c>PageShop</c>, <c>PageSettings</c>.
        /// </summary>
        public string Id => name;

        /// <summary>
        ///     Display name (without <c>Page</c> prefix when present).
        /// </summary>
        public string DisplayName => name.StartsWith("Page") && name.Length > 4 ? name.Substring(4) : name;
    }
}
