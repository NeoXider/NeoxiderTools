using UnityEngine;

namespace Neo
{
    /// <summary>
    /// Predefined soft colors for use with ColorAttribute
    /// </summary>
    public enum ColorEnum
    {
        /// <summary>Soft red color (R: 1.0, G: 0.5, B: 0.5)</summary>
        SoftRed,

        /// <summary>Soft green color (R: 0.5, G: 1.0, B: 0.5)</summary>
        SoftGreen,

        /// <summary>Soft blue color (R: 0.5, G: 0.5, B: 1.0)</summary>
        SoftBlue,

        /// <summary>Soft yellow color (R: 1.0, G: 1.0, B: 0.5)</summary>
        SoftYellow,

        /// <summary>Soft gray color (R: 0.5, G: 0.5, B: 0.5)</summary>
        SoftGray,

        /// <summary>Soft purple color (R: 0.5, G: 0.0, B: 0.5)</summary>
        SoftPurple,

        /// <summary>Soft cyan color (R: 0.5, G: 1.0, B: 1.0)</summary>
        SoftCyan,

        /// <summary>Soft orange color (R: 1.0, G: 0.5, B: 0.0)</summary>
        SoftOrange
    }

    /// <summary>
    /// Attribute to color fields in the Unity Inspector.
    /// Can be used with custom colors or predefined soft colors.
    /// </summary>
    /// <example>
    /// <code>
    /// [Color(1.0, 0.5, 0.5)] // Custom red color
    /// public string redField;
    /// 
    /// [Color(ColorEnum.SoftBlue)] // Predefined soft blue
    /// public int blueField;
    /// </code>
    /// </example>
    public class ColorAttribute : PropertyAttribute
    {
        /// <summary>
        /// The color to be applied to the field in the inspector
        /// </summary>
        public Color color { get; private set; }

        /// <summary>
        /// Creates a new ColorAttribute with custom RGBA values
        /// </summary>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        public ColorAttribute(double r, double g, double b, double a = 1f)
        {
            color = new Color((float)r, (float)g, (float)b, (float)a);
        }

        /// <summary>
        /// Creates a new ColorAttribute with a predefined soft color
        /// </summary>
        /// <param name="colorEnum">The predefined color to use</param>
        public ColorAttribute(ColorEnum colorEnum)
        {
            color = GetColor(colorEnum);
        }

        /// <summary>
        /// Converts a ColorEnum value to a Color
        /// </summary>
        /// <param name="colorEnum">The ColorEnum value to convert</param>
        /// <returns>The corresponding Color value</returns>
        public static Color GetColor(ColorEnum colorEnum)
        {
            return colorEnum switch
            {
                ColorEnum.SoftRed => new Color(1f, 0.5f, 0.5f),
                ColorEnum.SoftGreen => new Color(0.5f, 1f, 0.5f),
                ColorEnum.SoftBlue => new Color(0.5f, 0.5f, 1f),
                ColorEnum.SoftYellow => new Color(1f, 1f, 0.5f),
                ColorEnum.SoftGray => new Color(0.5f, 0.5f, 0.5f),
                ColorEnum.SoftPurple => new Color(0.5f, 0f, 0.5f),
                ColorEnum.SoftCyan => new Color(0.5f, 1f, 1f),
                ColorEnum.SoftOrange => new Color(1f, 0.5f, 0f),
                _ => Color.white
            };
        }
    }
}