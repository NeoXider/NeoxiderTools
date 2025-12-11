using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Settings for CustomEditorBase visual appearance and behavior
    /// </summary>
    public static class CustomEditorSettings
    {
        private static NeoInspectorSettings Settings
        {
            get
            {
                NeoInspectorSettings settings = NeoInspectorSettings.instance;
                settings.EnsureMigratedFromEditorPrefs();
                return settings;
            }
        }

        // Neo component background (dark cyberpunk)
        public static Color NeoBackgroundColor => new(0.15f, 0.1f, 0.2f, 1f);

        // Button colors (cyberpunk neon style)
        public static Color ButtonBackgroundColor => new(0.6f, 0.2f, 1f, 1f);
        public static Color ButtonHoverColor => new(0.8f, 0.3f, 1f, 1f);
        public static Color ButtonTextColor => Color.white;
        public static Color ButtonBorderColor => new(0.9f, 0.5f, 1f, 0.8f);

        // Signature colors (cyberpunk neon)
        public static Color SignatureColor => new(0.8f, 0.75f, 1f, 1f);
        public static Color SignatureGlowColor => new(0.9f, 0.8f, 1f, 0.6f);

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

        // Rainbow effects - Text
        public static bool EnableRainbowSignature => Settings.EnableRainbowSignature;

        public static bool EnableRainbowSignatureAnimation => Settings.EnableRainbowSignatureAnimation;

        // Rainbow effects - Line
        public static bool EnableRainbowOutline => Settings.EnableRainbowOutline;

        public static bool EnableRainbowComponentOutline => Settings.EnableRainbowComponentOutline;

        public static bool EnableRainbowLineAnimation => Settings.EnableRainbowLineAnimation;

        // Rainbow settings
        public static float RainbowSpeed => Settings.RainbowSpeed;
        public static float RainbowSaturation => Settings.RainbowSaturation;
        public static float RainbowBrightness => Settings.RainbowBrightness;
        public static float RainbowOutlineSize => Settings.RainbowOutlineSize;
        public static float RainbowOutlineAlpha => Settings.RainbowOutlineAlpha;

        public static float RainbowComponentOutlineWidth =>
            Settings.RainbowComponentOutlineWidth;

        public static Color ScriptNameColor => Settings.ScriptNameColor;

        // Setters
        public static void SetEnableRainbowSignature(bool value)
        {
            Settings.SetEnableRainbowSignature(value);
        }

        public static void SetEnableRainbowSignatureAnimation(bool value)
        {
            Settings.SetEnableRainbowSignatureAnimation(value);
        }

        public static void SetEnableRainbowOutline(bool value)
        {
            Settings.SetEnableRainbowOutline(value);
        }

        public static void SetEnableRainbowComponentOutline(bool value)
        {
            Settings.SetEnableRainbowComponentOutline(value);
        }

        public static void SetEnableRainbowLineAnimation(bool value)
        {
            Settings.SetEnableRainbowLineAnimation(value);
        }

        public static void SetRainbowSpeed(float value)
        {
            Settings.SetRainbowSpeed(value);
        }

        public static void SetScriptNameColor(Color value)
        {
            Settings.SetScriptNameColor(value);
        }
    }
}