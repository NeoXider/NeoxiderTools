using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Attribute to color GUI elements in the Unity Inspector.
    /// </summary>
    /// <example>
    ///     <code>
    /// [GUIColor(1.0, 0.5, 0.5)] // Custom red color
    /// public string redField;
    /// </code>
    /// </example>
    public class GUIColorAttribute : PropertyAttribute
    {
        /// <summary>
        ///     The color to be applied to the field in the inspector
        /// </summary>
        public Color color { get; private set; }

        /// <summary>
        ///     Creates a new GUIColorAttribute with custom RGBA values
        /// </summary>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        public GUIColorAttribute(double r, double g, double b, double a = 1f)
        {
            this.color = new Color((float)r, (float)g, (float)b, (float)a);
        }
    }
}
