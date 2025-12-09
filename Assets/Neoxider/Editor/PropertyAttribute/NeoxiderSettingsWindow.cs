using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Окно настроек Neoxider для редактора Unity
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
            EditorGUILayout.LabelField("Настройки визуального оформления компонентов",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawRainbowSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Rainbow Effects", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Текст (Signature)", EditorStyles.miniLabel);
            bool enableSignature = EditorGUILayout.Toggle("Включить Rainbow Signature",
                CustomEditorSettings.EnableRainbowSignature);

            EditorGUI.BeginDisabledGroup(!enableSignature);
            bool enableSignatureAnim = EditorGUILayout.Toggle("  Анимация текста",
                CustomEditorSettings.EnableRainbowSignatureAnimation);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Линия (Rainbow Line)", EditorStyles.miniLabel);
            bool enableOutline =
                EditorGUILayout.Toggle("Включить Rainbow Outline", CustomEditorSettings.EnableRainbowOutline);
            bool enableComponentOutline = EditorGUILayout.Toggle("Включить Rainbow Line (слева)",
                CustomEditorSettings.EnableRainbowComponentOutline);

            EditorGUI.BeginDisabledGroup(!enableComponentOutline);
            bool enableLineAnim =
                EditorGUILayout.Toggle("  Анимация линии", CustomEditorSettings.EnableRainbowLineAnimation);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Скорость анимации", EditorStyles.miniLabel);
            float speed = EditorGUILayout.Slider("Rainbow Speed", CustomEditorSettings.RainbowSpeed, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                CustomEditorSettings.SetEnableRainbowSignature(enableSignature);
                CustomEditorSettings.SetEnableRainbowSignatureAnimation(enableSignatureAnim);
                CustomEditorSettings.SetEnableRainbowOutline(enableOutline);
                CustomEditorSettings.SetEnableRainbowComponentOutline(enableComponentOutline);
                CustomEditorSettings.SetEnableRainbowLineAnimation(enableLineAnim);
                CustomEditorSettings.SetRainbowSpeed(speed);

                RepaintAllInspectors();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResetButton()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Сбросить все настройки", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Сброс настроек",
                        "Вы уверены что хотите сбросить все настройки Neoxider к значениям по умолчанию?",
                        "Да", "Отмена"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Устранение проблем", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Если компоненты Neo.Tools не отображаются с градиентной линией и кнопками при установке из Package Manager, " +
                "используйте меню: Tools → Neoxider → Fix Editor Assembly References",
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