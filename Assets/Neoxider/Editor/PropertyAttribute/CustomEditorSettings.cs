using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    /// Settings for CustomEditorBase visual appearance and behavior
    /// </summary>
    public static class CustomEditorSettings
    {
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
        public static bool EnableRainbowSignature => EditorPrefs.GetBool("Neo.EnableRainbowSignature", true);
        public static bool EnableRainbowSignatureAnimation => EditorPrefs.GetBool("Neo.EnableRainbowSignatureAnimation", true);
        
        // Rainbow effects - Line
        public static bool EnableRainbowOutline => EditorPrefs.GetBool("Neo.EnableRainbowOutline", true);
        public static bool EnableRainbowComponentOutline => EditorPrefs.GetBool("Neo.EnableRainbowComponentOutline", true);
        public static bool EnableRainbowLineAnimation => EditorPrefs.GetBool("Neo.EnableRainbowLineAnimation", true);
        
        // Rainbow settings
        public static float RainbowSpeed => EditorPrefs.GetFloat("Neo.RainbowSpeed", 0.1f);
        public static float RainbowSaturation => EditorPrefs.GetFloat("Neo.RainbowSaturation", 0.8f);
        public static float RainbowBrightness => EditorPrefs.GetFloat("Neo.RainbowBrightness", 1f);
        public static float RainbowOutlineSize => EditorPrefs.GetFloat("Neo.RainbowOutlineSize", 1.5f);
        public static float RainbowOutlineAlpha => EditorPrefs.GetFloat("Neo.RainbowOutlineAlpha", 0.6f);
        public static float RainbowComponentOutlineWidth => EditorPrefs.GetFloat("Neo.RainbowComponentOutlineWidth", 2f);
        
        // Setters
        public static void SetEnableRainbowSignature(bool value) => EditorPrefs.SetBool("Neo.EnableRainbowSignature", value);
        public static void SetEnableRainbowSignatureAnimation(bool value) => EditorPrefs.SetBool("Neo.EnableRainbowSignatureAnimation", value);
        public static void SetEnableRainbowOutline(bool value) => EditorPrefs.SetBool("Neo.EnableRainbowOutline", value);
        public static void SetEnableRainbowComponentOutline(bool value) => EditorPrefs.SetBool("Neo.EnableRainbowComponentOutline", value);
        public static void SetEnableRainbowLineAnimation(bool value) => EditorPrefs.SetBool("Neo.EnableRainbowLineAnimation", value);
        public static void SetRainbowSpeed(float value) => EditorPrefs.SetFloat("Neo.RainbowSpeed", value);
    }
}
