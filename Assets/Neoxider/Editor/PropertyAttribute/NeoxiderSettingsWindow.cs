using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Neoxider visual settings window for the Unity editor.
    /// </summary>
    public class NeoxiderSettingsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            GUILayout.Space(10);

            DrawAnimationSettings();
            GUILayout.Space(10);

            DrawRainbowSettings();
            GUILayout.Space(10);

            DrawResetButton();

            EditorGUILayout.EndScrollView();
        }

        [MenuItem("Neoxider/Visual Settings", false, 401)]
        public static void ShowWindow()
        {
            NeoxiderSettingsWindow window = GetWindow<NeoxiderSettingsWindow>("Visual Settings");
            window.minSize = new Vector2(400, 300);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle headerStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("🌈 Neoxider Editor Settings", headerStyle);
            EditorGUILayout.LabelField("Component inspector visual styling",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            bool enableAnimations = EditorGUILayout.Toggle(
                new GUIContent("Animate inspector",
                    "Master switch for the mascot, the rainbow line and the update pulse. " +
                    "Turn it off if the editor feels slow: the header is then drawn as a still image."),
                CustomEditorSettings.EnableAnimations);

            EditorGUI.BeginDisabledGroup(!enableAnimations);
            bool animateInPlayMode = EditorGUILayout.Toggle(
                new GUIContent("  Animate in Play Mode",
                    "Keep the inspector animation running while the game is running. " +
                    "Off by default so the inspector does not compete with the game for editor time."),
                CustomEditorSettings.AnimateInPlayMode);
            int animationFps = EditorGUILayout.IntSlider(
                new GUIContent("  Max FPS",
                    "Upper limit for animation repaints per second. Lower values cost less CPU."),
                CustomEditorSettings.AnimationFps, NeoInspectorAnimation.MinFps, NeoInspectorAnimation.MaxFps);
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                CustomEditorSettings.SetEnableAnimations(enableAnimations);
                CustomEditorSettings.SetAnimateInPlayMode(animateInPlayMode);
                CustomEditorSettings.SetAnimationFps(animationFps);

                RepaintAllInspectors();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRainbowSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Rainbow Effects", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Text (Signature)", EditorStyles.miniLabel);
            bool enableSignature = EditorGUILayout.Toggle("Enable Rainbow Signature",
                CustomEditorSettings.EnableRainbowSignature);

            EditorGUI.BeginDisabledGroup(!enableSignature || !CustomEditorSettings.EnableAnimations);
            bool enableSignatureAnim = EditorGUILayout.Toggle("  Text animation",
                CustomEditorSettings.EnableRainbowSignatureAnimation);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Line (Rainbow Line)", EditorStyles.miniLabel);
            bool enableOutline =
                EditorGUILayout.Toggle("Enable Rainbow Outline", CustomEditorSettings.EnableRainbowOutline);
            bool enableComponentOutline = EditorGUILayout.Toggle("Enable Rainbow Line (left)",
                CustomEditorSettings.EnableRainbowComponentOutline);

            EditorGUI.BeginDisabledGroup(!enableComponentOutline || !CustomEditorSettings.EnableAnimations);
            bool enableLineAnim =
                EditorGUILayout.Toggle("  Line animation", CustomEditorSettings.EnableRainbowLineAnimation);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Animation speed", EditorStyles.miniLabel);
            float speed = EditorGUILayout.Slider("Rainbow Speed", CustomEditorSettings.RainbowSpeed, 0f, 1f);

            GUILayout.Space(8);

            EditorGUILayout.LabelField("Header", EditorStyles.miniLabel);
            Color scriptNameColor = EditorGUILayout.ColorField("Script name color",
                CustomEditorSettings.ScriptNameColor);

            int minFieldsForHeaderCategory = EditorGUILayout.IntSlider(
                "Minimum fields for Header category",
                CustomEditorSettings.MinFieldsForHeaderCategory,
                0, 10);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Lists and arrays", EditorStyles.miniLabel);
            bool useDefaultListAndArrayDrawing = EditorGUILayout.Toggle(
                "Default Unity list/array drawing",
                CustomEditorSettings.UseDefaultListAndArrayDrawing);

            if (EditorGUI.EndChangeCheck())
            {
                CustomEditorSettings.SetEnableRainbowSignature(enableSignature);
                CustomEditorSettings.SetEnableRainbowSignatureAnimation(enableSignatureAnim);
                CustomEditorSettings.SetEnableRainbowOutline(enableOutline);
                CustomEditorSettings.SetEnableRainbowComponentOutline(enableComponentOutline);
                CustomEditorSettings.SetEnableRainbowLineAnimation(enableLineAnim);
                CustomEditorSettings.SetRainbowSpeed(speed);
                CustomEditorSettings.SetScriptNameColor(scriptNameColor);
                CustomEditorSettings.SetMinFieldsForHeaderCategory(minFieldsForHeaderCategory);
                CustomEditorSettings.SetUseDefaultListAndArrayDrawing(useDefaultListAndArrayDrawing);

                RepaintAllInspectors();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResetButton()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Reset all settings", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset settings",
                        "Reset all Neoxider settings to their defaults?",
                        "Yes", "Cancel"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Troubleshooting", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "If Neo.Tools components do not show the gradient line and action buttons when installed from Package Manager, " +
                "use: Neoxider → Tools → Fix Editor Assembly References",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void ResetToDefaults()
        {
            CustomEditorSettings.SetEnableAnimations(true);
            CustomEditorSettings.SetAnimateInPlayMode(false);
            CustomEditorSettings.SetAnimationFps(NeoInspectorAnimation.DefaultFps);
            CustomEditorSettings.SetEnableRainbowSignature(true);
            CustomEditorSettings.SetEnableRainbowSignatureAnimation(true);
            CustomEditorSettings.SetEnableRainbowOutline(true);
            CustomEditorSettings.SetEnableRainbowComponentOutline(true);
            CustomEditorSettings.SetEnableRainbowLineAnimation(true);
            CustomEditorSettings.SetRainbowSpeed(0.1f);
            CustomEditorSettings.SetScriptNameColor(new Color(0.35f, 1f, 0.35f, 1f));
            CustomEditorSettings.SetMinFieldsForHeaderCategory(3);
            CustomEditorSettings.SetUseDefaultListAndArrayDrawing(true);

            RepaintAllInspectors();
        }

        private void RepaintAllInspectors()
        {
            foreach (UnityEditor.Editor editor in Resources.FindObjectsOfTypeAll<UnityEditor.Editor>())
            {
                editor.Repaint();
            }
        }
    }
}
