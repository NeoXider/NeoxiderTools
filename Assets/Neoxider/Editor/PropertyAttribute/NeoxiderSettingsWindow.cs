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

            DrawRainbowSettings();
            GUILayout.Space(10);

            DrawResetButton();

            EditorGUILayout.EndScrollView();
        }

        [MenuItem("Tools/Neoxider/Visual Settings")]
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

        private void DrawRainbowSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Rainbow Effects", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Text (Signature)", EditorStyles.miniLabel);
            bool enableSignature = EditorGUILayout.Toggle("Enable Rainbow Signature",
                CustomEditorSettings.EnableRainbowSignature);

            EditorGUI.BeginDisabledGroup(!enableSignature);
            bool enableSignatureAnim = EditorGUILayout.Toggle("  Text animation",
                CustomEditorSettings.EnableRainbowSignatureAnimation);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Line (Rainbow Line)", EditorStyles.miniLabel);
            bool enableOutline =
                EditorGUILayout.Toggle("Enable Rainbow Outline", CustomEditorSettings.EnableRainbowOutline);
            bool enableComponentOutline = EditorGUILayout.Toggle("Enable Rainbow Line (left)",
                CustomEditorSettings.EnableRainbowComponentOutline);

            EditorGUI.BeginDisabledGroup(!enableComponentOutline);
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
                "use: Tools → Neoxider → Fix Editor Assembly References",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void ResetToDefaults()
        {
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
