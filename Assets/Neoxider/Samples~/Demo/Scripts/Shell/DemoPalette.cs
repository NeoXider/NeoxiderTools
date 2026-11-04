using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Shared colors of the bright NeoxiderTools demo look. Scenes authored by the Neo.Demo.Editor
    ///     DemoSceneKit and runtime demo controllers use the same values, so every demo reads as one system.
    /// </summary>
    public static class DemoPalette
    {
        /// <summary>Near-black text on accent buttons and the log feed panel tint.</summary>
        public static readonly Color Ink = new(0.07f, 0.07f, 0.10f, 0.86f);

        public static readonly Color Panel = new(0.11f, 0.12f, 0.17f, 0.98f);

        /// <summary>Background of the centered content card.</summary>
        public static readonly Color Card = new(0.14f, 0.15f, 0.21f, 0.98f);

        /// <summary>Slider/toggle/bar tracks and input backgrounds.</summary>
        public static readonly Color Track = new(1f, 1f, 1f, 0.10f);

        public static readonly Color Text = new(0.93f, 0.94f, 0.98f);
        public static readonly Color Muted = new(0.62f, 0.64f, 0.74f);
        public static readonly Color Accent = new(0.49f, 0.36f, 0.94f);
        public static readonly Color Cyan = new(0.25f, 0.79f, 0.94f);
        public static readonly Color Good = new(0.26f, 0.85f, 0.64f);
        public static readonly Color Danger = new(1f, 0.36f, 0.48f);
        public static readonly Color Warm = new(0.98f, 0.72f, 0.30f);

        /// <summary>Bottom color of the demo backdrop (the old runtime gradient multiplied by the accent).</summary>
        public static Color BackdropBottom(Color accent)
        {
            return new Color(0.03f * accent.r, 0.03f * accent.g, 0.05f * accent.b, 1f);
        }

        /// <summary>Top color of the demo backdrop; blended over <see cref="BackdropBottom" /> by a t-squared alpha ramp.</summary>
        public static Color BackdropTop(Color accent)
        {
            return new Color(
                (0.10f + accent.r * 0.16f) * accent.r,
                (0.10f + accent.g * 0.16f) * accent.g,
                (0.14f + accent.b * 0.18f) * accent.b,
                1f);
        }

        /// <summary>Dark camera clear color tinted slightly toward the accent.</summary>
        public static Color CameraBackground(Color accent)
        {
            return new Color(0.05f + accent.r * 0.03f, 0.05f + accent.g * 0.03f, 0.07f + accent.b * 0.04f, 1f);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
