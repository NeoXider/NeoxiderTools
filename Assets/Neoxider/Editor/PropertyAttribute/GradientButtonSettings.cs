using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Visual styling for Neo ButtonAttribute buttons.
    /// </summary>
    public static class GradientButtonSettings
    {
        // === Color schemes (when UseNaturalStyle = false) ===

        private static readonly Color Scheme1_Top = new(0.2f, 0.8f, 1f, 1f);
        private static readonly Color Scheme1_Bottom = new(0.6f, 0.2f, 1f, 1f);

        private static readonly Color Scheme2_Top = new(0.3f, 0.4f, 0.9f, 1f);
        private static readonly Color Scheme2_Bottom = new(0.7f, 0.3f, 0.9f, 1f);

        private static readonly Color Scheme3_Top = new(0.35f, 0.4f, 0.95f, 1f);
        private static readonly Color Scheme3_Bottom = new(0.45f, 0.3f, 0.85f, 1f);

        private static readonly Color Scheme4_Top = new(1f, 0.45f, 0.3f, 1f);
        private static readonly Color Scheme4_Bottom = new(0.95f, 0.3f, 0.6f, 1f);

        private static readonly Color Scheme5_Top = new(0.3f, 0.95f, 0.5f, 1f);
        private static readonly Color Scheme5_Bottom = new(0.2f, 0.7f, 0.4f, 1f);

        private static readonly Color Scheme6_Top = new(0.26f, 0.29f, 0.38f, 1f);
        private static readonly Color Scheme6_Bottom = new(0.19f, 0.21f, 0.30f, 1f);
        private static readonly Color Scheme6_Accent = new(0.48f, 0.63f, 0.98f, 1f);

        private static readonly int ActiveScheme = 6;

        /// <summary>
        ///     When false, uses the branded (but restrained) gradient button style.
        /// </summary>
        public static bool UseNaturalStyle => false;

        public static Color TopColor => ActiveScheme switch
        {
            1 => Scheme1_Top,
            2 => Scheme2_Top,
            3 => Scheme3_Top,
            4 => Scheme4_Top,
            5 => Scheme5_Top,
            6 => Scheme6_Top,
            _ => Scheme6_Top
        };

        public static Color BottomColor => ActiveScheme switch
        {
            1 => Scheme1_Bottom,
            2 => Scheme2_Bottom,
            3 => Scheme3_Bottom,
            4 => Scheme4_Bottom,
            5 => Scheme5_Bottom,
            6 => Scheme6_Bottom,
            _ => Scheme6_Bottom
        };

        public static Color AccentColor => ActiveScheme == 6 ? Scheme6_Accent : new Color(1f, 1f, 1f, 0.3f);

        public static float HoverBrightness => 0.08f;

        public static float DefaultButtonHeight => 20f;
        public static float CornerRadius => 4f;
        public static float ButtonSpacing => 4f;

        public static int GradientSegments => 20;
        public static int CornerMaskSteps => 16;

        public static Color HighlightColor => ActiveScheme == 6
            ? new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.65f)
            : new Color(1f, 1f, 1f, 0.22f);

        public static float HighlightWidth => 1.25f;

        public static bool EnableNeonGlow => false;
        public static Color NeonGlowColor => AccentColor;

        public static Color TextColor => Color.white;
        public static FontStyle TextStyle => FontStyle.Bold;
        public static TextAnchor TextAlignment => TextAnchor.MiddleCenter;

        public static Color InspectorBackgroundColor => new(0.22f, 0.22f, 0.22f, 1f);
    }
}
