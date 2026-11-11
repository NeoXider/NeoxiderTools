using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Neoxider visual settings window for the Unity editor (menu Neoxider > Visual Settings): header, animation,
    ///     spectrum frame, sections and lists of the Neo component inspector. Every change is saved to
    ///     ProjectSettings/NeoInspectorSettings.asset and repaints open inspectors.
    /// </summary>
    public class NeoxiderSettingsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        private static readonly GUIContent ShowBannerContent = new("Show banner",
            "Full Neoxider banner (mascot, title, version pill). Off: every Neo component gets the compact one-row " +
            "header, which keeps the health badge and the update status.");

        private static readonly GUIContent OneBannerContent = new("One banner per GameObject",
            "With several Neo components on one GameObject only the first shows the full banner; the others use " +
            "the compact header.");

        private static readonly GUIContent AnimateMascotContent = new("Animate mascot",
            "Breathing, blinking and the click bounce of the banner mascot. Needs Animate inspector.");

        private static readonly GUIContent AnimateContent = new("Animate inspector",
            "Master switch for the mascot, the flowing spectrum and every transition. Turn it off if the editor " +
            "feels slow: the header is then drawn as a still image.");

        private static readonly GUIContent PlayModeContent = new("Animate in Play Mode",
            "Keep the inspector animation running while the game is running. Off by default so the inspector does " +
            "not compete with the game for editor time.");

        private static readonly GUIContent TransitionsContent = new("Smooth transitions",
            "Eased opening and closing of sections, hover highlights and sliding switches.");

        private static readonly GUIContent FpsContent = new("Max FPS",
            "Upper limit for animation repaints per second. Lower values cost less CPU.");

        private static readonly GUIContent FrameContent = new("Spectrum frame",
            "Colour line along the left side of the property card. It turns amber or red when the component has " +
            "missing references or errors.");

        private static readonly GUIContent FlowContent = new("Flowing colours",
            "Let the spectrum colours flow along the frame. Needs Animate inspector.");

        private static readonly GUIContent SpeedContent = new("Flow speed", "How fast the spectrum colours move.");
        private static readonly GUIContent SaturationContent = new("Saturation", "Colour intensity of the frame.");
        private static readonly GUIContent BrightnessContent = new("Brightness", "Brightness of the frame colours.");

        private static readonly GUIContent AccentContent = new("Section accent",
            "Accent colour of the collapsible [Header] sections (rail, count pill and icon tint).");

        private static readonly GUIContent MinFieldsContent = new("Fields per collapsible section",
            "How many fields a [Header] group needs before it becomes a collapsible section; smaller groups get a " +
            "plain title row.");

        private static readonly GUIContent DefaultListsContent = new("Unity list drawing",
            "Draw lists and arrays with Unity's reorderable list (recommended). Off uses the Neoxider foldout, " +
            "which cannot add or remove elements.");

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.Space(6f);

            NeoxiderEditorGUI.DrawSummaryCard("Neoxider Visual Settings",
                "How Neo component inspectors look and move. Saved per project in " +
                "ProjectSettings/NeoInspectorSettings.asset.", true, false);

            EditorGUILayout.Space(4f);
            DrawHeaderSettings();
            EditorGUILayout.Space(4f);
            DrawAnimationSettings();
            EditorGUILayout.Space(4f);
            DrawFrameSettings();
            EditorGUILayout.Space(4f);
            DrawSectionSettings();
            EditorGUILayout.Space(4f);
            DrawResetButton();

            EditorGUILayout.EndScrollView();
        }

        [MenuItem("Neoxider/Visual Settings", false, 401)]
        public static void ShowWindow()
        {
            NeoxiderSettingsWindow window = GetWindow<NeoxiderSettingsWindow>("Visual Settings");
            window.minSize = new Vector2(360, 300);
        }

        private static bool Switch(GUIContent content, bool value)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            Rect field = EditorGUI.PrefixLabel(rect, content);
            return NeoxiderEditorGUI.DrawPillToggle(
                new Rect(field.x, field.y, NeoxiderEditorGUI.PillToggleWidth, field.height), value);
        }

        private void DrawHeaderSettings()
        {
            NeoxiderEditorGUI.BeginSection("Header", "Banner, mascot and the compact header.");
            EditorGUI.BeginChangeCheck();

            bool showBanner = Switch(ShowBannerContent, CustomEditorSettings.EnableRainbowSignature);
            bool oneBanner;
            using (new EditorGUI.DisabledScope(!showBanner))
            {
                oneBanner = Switch(OneBannerContent, CustomEditorSettings.CompactRepeatedBanners);
            }

            bool animateMascot;
            using (new EditorGUI.DisabledScope(!CustomEditorSettings.EnableAnimations))
            {
                animateMascot = Switch(AnimateMascotContent, CustomEditorSettings.EnableRainbowSignatureAnimation);
            }

            if (EditorGUI.EndChangeCheck())
            {
                CustomEditorSettings.SetEnableRainbowSignature(showBanner);
                CustomEditorSettings.SetCompactRepeatedBanners(oneBanner);
                CustomEditorSettings.SetEnableRainbowSignatureAnimation(animateMascot);
                RepaintAllInspectors();
            }

            NeoxiderEditorGUI.EndSection();
        }

        private void DrawAnimationSettings()
        {
            NeoxiderEditorGUI.BeginSection("Animation",
                "One master switch; Play Mode and the frame-rate cap apply to everything below it.");
            EditorGUI.BeginChangeCheck();

            bool enableAnimations = Switch(AnimateContent, CustomEditorSettings.EnableAnimations);
            bool animateInPlayMode;
            bool transitions;
            int animationFps;
            using (new EditorGUI.DisabledScope(!enableAnimations))
            {
                animateInPlayMode = Switch(PlayModeContent, CustomEditorSettings.AnimateInPlayMode);
                transitions = Switch(TransitionsContent, CustomEditorSettings.EnableTransitions);
                animationFps = EditorGUILayout.IntSlider(FpsContent, CustomEditorSettings.AnimationFps,
                    NeoInspectorAnimation.MinFps, NeoInspectorAnimation.MaxFps);
            }

            if (EditorGUI.EndChangeCheck())
            {
                CustomEditorSettings.SetEnableAnimations(enableAnimations);
                CustomEditorSettings.SetAnimateInPlayMode(animateInPlayMode);
                CustomEditorSettings.SetEnableTransitions(transitions);
                CustomEditorSettings.SetAnimationFps(animationFps);
                RepaintAllInspectors();
            }

            NeoxiderEditorGUI.DrawCaption("With animation off the header is a still image; the health badge still " +
                                          "refreshes twice a second and live data (update check, conditions) ten " +
                                          "times a second.");
            NeoxiderEditorGUI.EndSection();
        }

        private void DrawFrameSettings()
        {
            NeoxiderEditorGUI.BeginSection("Spectrum frame", "The colour line beside the property card.");
            EditorGUI.BeginChangeCheck();

            bool frame = Switch(FrameContent, CustomEditorSettings.EnableRainbowComponentOutline);
            bool flow;
            float speed;
            float saturation;
            float brightness;
            using (new EditorGUI.DisabledScope(!frame))
            {
                using (new EditorGUI.DisabledScope(!CustomEditorSettings.EnableAnimations))
                {
                    flow = Switch(FlowContent, CustomEditorSettings.EnableRainbowLineAnimation);
                    speed = EditorGUILayout.Slider(SpeedContent, CustomEditorSettings.RainbowSpeed, 0f, 1f);
                }

                saturation = EditorGUILayout.Slider(SaturationContent, CustomEditorSettings.RainbowSaturation, 0f, 1f);
                brightness = EditorGUILayout.Slider(BrightnessContent, CustomEditorSettings.RainbowBrightness, 0f, 1f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                CustomEditorSettings.SetEnableRainbowComponentOutline(frame);
                CustomEditorSettings.SetEnableRainbowLineAnimation(flow);
                CustomEditorSettings.SetRainbowSpeed(speed);
                CustomEditorSettings.SetRainbowSaturation(saturation);
                CustomEditorSettings.SetRainbowBrightness(brightness);
                RepaintAllInspectors();
            }

            NeoxiderEditorGUI.EndSection();
        }

        private void DrawSectionSettings()
        {
            NeoxiderEditorGUI.BeginSection("Sections and lists", "How fields are grouped.");
            EditorGUI.BeginChangeCheck();

            Color scriptNameColor = EditorGUILayout.ColorField(AccentContent, CustomEditorSettings.ScriptNameColor);
            int minFieldsForHeaderCategory = EditorGUILayout.IntSlider(MinFieldsContent,
                CustomEditorSettings.MinFieldsForHeaderCategory, 0, 10);
            bool useDefaultListAndArrayDrawing = Switch(DefaultListsContent,
                CustomEditorSettings.UseDefaultListAndArrayDrawing);

            if (EditorGUI.EndChangeCheck())
            {
                CustomEditorSettings.SetScriptNameColor(scriptNameColor);
                CustomEditorSettings.SetMinFieldsForHeaderCategory(minFieldsForHeaderCategory);
                CustomEditorSettings.SetUseDefaultListAndArrayDrawing(useDefaultListAndArrayDrawing);
                RepaintAllInspectors();
            }

            NeoxiderEditorGUI.EndSection();
        }

        private void DrawResetButton()
        {
            NeoxiderEditorGUI.BeginSection("Reset and troubleshooting");
            if (GUILayout.Button("Reset all visual settings", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Reset settings",
                        "Reset all Neoxider visual settings to their defaults?",
                        "Reset", "Cancel"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "If Neo components do not show the Neoxider inspector after a Package Manager install, run " +
                "Neoxider > Tools > Fix Editor Assembly References.",
                MessageType.Info);
            NeoxiderEditorGUI.EndSection();
        }

        private void ResetToDefaults()
        {
            CustomEditorSettings.SetEnableAnimations(true);
            CustomEditorSettings.SetAnimateInPlayMode(false);
            CustomEditorSettings.SetAnimationFps(NeoInspectorAnimation.DefaultFps);
            CustomEditorSettings.SetEnableTransitions(true);
            CustomEditorSettings.SetCompactRepeatedBanners(true);
            CustomEditorSettings.SetEnableRainbowSignature(true);
            CustomEditorSettings.SetEnableRainbowSignatureAnimation(true);
            CustomEditorSettings.SetEnableRainbowComponentOutline(true);
            CustomEditorSettings.SetEnableRainbowLineAnimation(true);
            CustomEditorSettings.SetRainbowSpeed(0.1f);
            CustomEditorSettings.SetRainbowSaturation(0.8f);
            CustomEditorSettings.SetRainbowBrightness(1f);
            CustomEditorSettings.SetScriptNameColor(new Color(0.35f, 1f, 0.35f, 1f));
            CustomEditorSettings.SetMinFieldsForHeaderCategory(3);
            CustomEditorSettings.SetUseDefaultListAndArrayDrawing(true);

            RepaintAllInspectors();
        }

        private static void RepaintAllInspectors()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
