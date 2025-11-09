using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Settings for CustomEditorBase visual appearance and behavior
    /// </summary>
    public static class CustomEditorSettings
    {
        // Neo component background (dark cyberpunk)
        public static Color NeoBackgroundColor => new Color(0.15f, 0.1f, 0.2f, 1f); // Dark purple-tinted background

        // Button colors (cyberpunk neon style)
        public static Color ButtonBackgroundColor => new Color(0.6f, 0.2f, 1f, 1f); // Bright purple
        public static Color ButtonHoverColor => new Color(0.8f, 0.3f, 1f, 1f); // Lighter purple on hover
        public static Color ButtonTextColor => Color.white;
        public static Color ButtonBorderColor => new Color(0.9f, 0.5f, 1f, 0.8f); // Neon glow border

        // Signature colors (cyberpunk neon)
        public static Color SignatureColor => new Color(0.8f, 0.75f, 1f, 1f);
        public static Color SignatureGlowColor => new Color(0.9f, 0.8f, 1f, 0.6f); // Subtle glow effect

        // Text settings
        public static int SignatureFontSize => 14;
        public static int ButtonTextMaxLength => 16;
        public static FontStyle SignatureFontStyle => FontStyle.Bold;

        // Spacing
        public static float SignatureSpacing => 6f;
        public static float ButtonSpacing => 1f;
        public static float ButtonParameterSpacing => 20f;

        // Cyberpunk effects
        public static bool EnableNeonGlow => true;
        public static float GlowIntensity => 0.3f;
    }
}

