using System;

namespace Neo
{
    /// <summary>
    ///     Marks a MonoBehaviour for quick creation via GameObject → Neoxider.
    ///     The editor builds the menu by reflection; choosing an item creates a GameObject with the component (and prefab if set).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CreateFromMenuAttribute : Attribute
    {
        public CreateFromMenuAttribute(string menuPath, string prefabPath = null)
        {
            MenuPath = menuPath ?? string.Empty;
            PrefabPath = string.IsNullOrEmpty(prefabPath) ? null : prefabPath;
        }

        /// <summary>Submenu path, e.g. "Neoxider/UI/VisualToggle".</summary>
        public string MenuPath { get; }

        /// <summary>
        ///     Prefab path relative to package root, e.g. "Prefabs/UI/VisualToggle.prefab". If empty,
        ///     only a GameObject with the component is created.
        /// </summary>
        public string PrefabPath { get; }
    }
}
