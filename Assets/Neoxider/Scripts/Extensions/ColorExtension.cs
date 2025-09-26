using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for Unity's Color struct
    /// </summary>
    public static class ColorExtension
    {
        /// <summary>
        ///     Creates a new color with modified alpha value
        /// </summary>
        /// <param name="color">Source color</param>
        /// <param name="alpha">New alpha value (0-1)</param>
        /// <returns>New color with modified alpha</returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>
        ///     Creates a new color with specified RGBA values
        /// </summary>
        /// <param name="color">Source color</param>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        /// <returns>New color with specified values</returns>
        public static Color With(this Color color, float? r = null, float? g = null, float? b = null, float? a = null)
        {
            return new Color(
                r.HasValue ? Mathf.Clamp01(r.Value) : color.r,
                g.HasValue ? Mathf.Clamp01(g.Value) : color.g,
                b.HasValue ? Mathf.Clamp01(b.Value) : color.b,
                a.HasValue ? Mathf.Clamp01(a.Value) : color.a
            );
        }

        /// <summary>
        ///     Creates a new color with modified RGB values, preserving alpha
        /// </summary>
        /// <param name="color">Source color</param>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <returns>New color with modified RGB values</returns>
        public static Color WithRGB(this Color color, float r, float g, float b)
        {
            return new Color(
                Mathf.Clamp01(r),
                Mathf.Clamp01(g),
                Mathf.Clamp01(b),
                color.a
            );
        }

        /// <summary>
        ///     Creates a darker version of the color
        /// </summary>
        /// <param name="color">Source color</param>
        /// <param name="amount">Amount to darken (0-1)</param>
        /// <returns>Darkened color</returns>
        public static Color Darken(this Color color, float amount)
        {
            amount = Mathf.Clamp01(amount);
            return new Color(
                color.r * (1 - amount),
                color.g * (1 - amount),
                color.b * (1 - amount),
                color.a
            );
        }

        /// <summary>
        ///     Creates a lighter version of the color
        /// </summary>
        /// <param name="color">Source color</param>
        /// <param name="amount">Amount to lighten (0-1)</param>
        /// <returns>Lightened color</returns>
        public static Color Lighten(this Color color, float amount)
        {
            amount = Mathf.Clamp01(amount);
            return new Color(
                color.r + (1 - color.r) * amount,
                color.g + (1 - color.g) * amount,
                color.b + (1 - color.b) * amount,
                color.a
            );
        }

        /// <summary>
        ///     Converts a Color to its HEX string representation (#RRGGBBAA)
        /// </summary>
        public static string ToHexString(this Color color)
        {
            return
                $"#{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}{(int)(color.a * 255):X2}";
        }
    }
}