using System;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Settings for CustomEditorBase visual appearance and behavior
    /// </summary>
    /// <remarks>
    ///     The persisted values live in <see cref="NeoInspectorSettings" /> (ProjectSettings/NeoInspectorSettings.asset)
    ///     and are edited in Neoxider > Visual Settings. Several historical names are kept for compatibility; their
    ///     summaries say what they switch in the current inspector.
    /// </remarks>
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

        /// <summary>Space above and below the header of a Neo component.</summary>
        public static float SignatureSpacing => 4f;

        public static float ButtonSpacing => 1f;
        public static float ButtonParameterSpacing => 20f;

        public static bool EnableNeonGlow => true;
        public static float GlowIntensity => 0.3f;

        /// <summary>Master switch for the decorative inspector animation.</summary>
        public static bool EnableAnimations => Settings.EnableAnimations;

        /// <summary>Whether the inspector keeps animating while the game is running (off by default).</summary>
        public static bool AnimateInPlayMode => Settings.AnimateInPlayMode;

        /// <summary>Upper limit for inspector animation repaints per second.</summary>
        public static int AnimationFps => Settings.AnimationFps;

        /// <summary>
        ///     Eased transitions for sections opening and closing, hover highlights and switches. Only runs while
        ///     <see cref="EnableAnimations" /> is on (and, in Play Mode, <see cref="AnimateInPlayMode" />).
        /// </summary>
        public static bool EnableTransitions => Settings.EnableTransitions;

        /// <summary>
        ///     With several Neo components on one GameObject, only the first draws the full banner; the others get the
        ///     compact one-row header.
        /// </summary>
        public static bool CompactRepeatedBanners => Settings.CompactRepeatedBanners;

        /// <summary>Shows the full Neoxider banner; off draws the compact one-row header instead.</summary>
        public static bool EnableRainbowSignature => Settings.EnableRainbowSignature;

        /// <summary>Animates the banner mascot (breathing, blink, click pop) while animation is on.</summary>
        public static bool EnableRainbowSignatureAnimation => Settings.EnableRainbowSignatureAnimation;

        /// <summary>No visible effect: the text signature it outlined was replaced by the banner in v10.</summary>
        [Obsolete("No visible effect since the v10 banner replaced the text signature; the value is only stored.")]
        public static bool EnableRainbowOutline => Settings.EnableRainbowOutline;

        /// <summary>Draws the spectrum half-frame along the left side of the property card.</summary>
        public static bool EnableRainbowComponentOutline => Settings.EnableRainbowComponentOutline;

        /// <summary>Lets the spectrum colours flow along the half-frame while animation is on.</summary>
        public static bool EnableRainbowLineAnimation => Settings.EnableRainbowLineAnimation;

        public static float RainbowSpeed => Settings.RainbowSpeed;
        public static float RainbowSaturation => Settings.RainbowSaturation;
        public static float RainbowBrightness => Settings.RainbowBrightness;
        public static float RainbowOutlineSize => Settings.RainbowOutlineSize;
        public static float RainbowOutlineAlpha => Settings.RainbowOutlineAlpha;

        public static float RainbowComponentOutlineWidth =>
            Settings.RainbowComponentOutlineWidth;

        /// <summary>Accent colour of the collapsible [Header] sections (historically the script-name colour).</summary>
        public static Color ScriptNameColor => Settings.ScriptNameColor;

        public static int MinFieldsForHeaderCategory => Settings.MinFieldsForHeaderCategory;

        /// <summary>When true, lists and arrays use Unity default drawing instead of custom foldouts (avoids list logic issues).</summary>
        public static bool UseDefaultListAndArrayDrawing => Settings.UseDefaultListAndArrayDrawing;

        public static void SetEnableAnimations(bool value)
        {
            Settings.SetEnableAnimations(value);
        }

        public static void SetAnimateInPlayMode(bool value)
        {
            Settings.SetAnimateInPlayMode(value);
        }

        public static void SetAnimationFps(int value)
        {
            Settings.SetAnimationFps(value);
        }

        public static void SetEnableTransitions(bool value)
        {
            Settings.SetEnableTransitions(value);
        }

        public static void SetCompactRepeatedBanners(bool value)
        {
            Settings.SetCompactRepeatedBanners(value);
        }

        public static void SetEnableRainbowSignature(bool value)
        {
            Settings.SetEnableRainbowSignature(value);
        }

        public static void SetEnableRainbowSignatureAnimation(bool value)
        {
            Settings.SetEnableRainbowSignatureAnimation(value);
        }

        [Obsolete("No visible effect since the v10 banner replaced the text signature; the value is only stored.")]
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

        /// <summary>Saturation of the spectrum half-frame (0..1).</summary>
        public static void SetRainbowSaturation(float value)
        {
            Settings.SetRainbowSaturation(value);
        }

        /// <summary>Brightness of the spectrum half-frame (0..1).</summary>
        public static void SetRainbowBrightness(float value)
        {
            Settings.SetRainbowBrightness(value);
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
