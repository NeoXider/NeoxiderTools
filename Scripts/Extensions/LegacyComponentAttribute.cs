using System;

namespace Neo
{
    /// <summary>
    ///     Marks a component as legacy while keeping it available for backward compatibility in existing scenes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LegacyComponentAttribute : Attribute
    {
        public LegacyComponentAttribute(string replacement = null, bool hideFromCreateMenu = true)
        {
            Replacement = string.IsNullOrWhiteSpace(replacement) ? string.Empty : replacement;
            HideFromCreateMenu = hideFromCreateMenu;
        }

        /// <summary>
        ///     Gets the recommended replacement that should be used for new setups.
        /// </summary>
        public string Replacement { get; }

        /// <summary>
        ///     Gets whether the component should be excluded from the custom Neoxider create window.
        /// </summary>
        public bool HideFromCreateMenu { get; }
    }
}
