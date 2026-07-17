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

        // WHY: Neo component container tint (multiplies the wrapping helpBox). Kept subtle and cool so the
        // section cards inside read cleanly in both the dark and light editor skins.
        public static Color NeoBackgroundColor => NeoInspectorTheme.IsDark
            ? new Color(0.80f, 0.82f, 0.92f, 1f)
            : new Color(0.97f, 0.98f, 1f, 1f);

        public static Color ButtonBackgroundColor => new(0.6f, 0.2f, 1f, 1f);
        public static Color ButtonHoverColor => new(0.8f, 0.3f, 1f, 1f);
        public static Color ButtonTextColor => Color.white;
        public static Color ButtonBorderColor => new(0.9f, 0.5f, 1f, 0.8f);

        public static Color SignatureColor => new(0.8f, 0.75f, 1f, 1f);
        public static Color SignatureGlowColor => new(0.9f, 0.8f, 1f, 0.6f);

        public static int SignatureFontSize => 14;
        public static int ButtonTextMaxLength => 16;
        public static FontStyle SignatureFontStyle => FontStyle.Bold;

        public static float SignatureSpacing => 6f;
        public static float ButtonSpacing => 1f;
        public static float ButtonParameterSpacing => 20f;

        public static bool EnableNeonGlow => true;
        public static float GlowIntensity => 0.3f;

        public static bool EnableRainbowSignature => Settings.EnableRainbowSignature;

        public static bool EnableRainbowSignatureAnimation => Settings.EnableRainbowSignatureAnimation;

        public static bool EnableRainbowOutline => Settings.EnableRainbowOutline;

        public static bool EnableRainbowComponentOutline => Settings.EnableRainbowComponentOutline;

        public static bool EnableRainbowLineAnimation => Settings.EnableRainbowLineAnimation;

        public static float RainbowSpeed => Settings.RainbowSpeed;
        public static float RainbowSaturation => Settings.RainbowSaturation;
        public static float RainbowBrightness => Settings.RainbowBrightness;
        public static float RainbowOutlineSize => Settings.RainbowOutlineSize;
        public static float RainbowOutlineAlpha => Settings.RainbowOutlineAlpha;

        public static float RainbowComponentOutlineWidth =>
            Settings.RainbowComponentOutlineWidth;

        public static Color ScriptNameColor => Settings.ScriptNameColor;
        public static int MinFieldsForHeaderCategory => Settings.MinFieldsForHeaderCategory;

        /// <summary>When true, lists and arrays use Unity default drawing instead of custom foldouts (avoids list logic issues).</summary>
        public static bool UseDefaultListAndArrayDrawing => Settings.UseDefaultListAndArrayDrawing;

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

        public static void SetMinFieldsForHeaderCategory(int value)
        {
            Settings.SetMinFieldsForHeaderCategory(value);
        }

        public static void SetUseDefaultListAndArrayDrawing(bool value)
        {
            Settings.SetUseDefaultListAndArrayDrawing(value);
        }
    }
}
