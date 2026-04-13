using TMPro;
using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    public class AutoTMPFontAssignerEditor : EditorWindow
    {
        private int assignedCount;
        private TMP_FontAsset targetFont;

        private void OnGUI()
        {
            GUILayout.Label("Auto TMP Font Assigner", EditorStyles.boldLabel);
            GUILayout.Space(5);

            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "Target TMP Font", targetFont, typeof(TMP_FontAsset), false);

            GUILayout.Space(10);

            GUI.enabled = targetFont != null;
            if (GUILayout.Button("Assign Font To All TMP_Text"))
            {
                AssignFontToAllTMPText();
            }

            GUI.enabled = true;

            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Iterates all TMP_Text components in open scenes and assigns the selected font.\n" +
                "Skips text in assets (prefabs, ScriptableObjects, etc.).",
                MessageType.Info);
        }

        [MenuItem("Tools/UIKit/Auto TMP Font Assigner")]
        public static void ShowWindow()
        {
            GetWindow<AutoTMPFontAssignerEditor>("Auto TMP Font Assigner");
        }

        private void AssignFontToAllTMPText()
        {
            assignedCount = 0;

            // Finds all TMP_Text, including on inactive objects
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();

            foreach (TMP_Text text in allTexts)
            {
                GameObject go = text.gameObject;
                // Skip objects not in loaded scenes
                if (string.IsNullOrEmpty(go.scene.name))
                {
                    continue;
                }

                if (text.font != targetFont)
                {
                    Undo.RecordObject(text, "Assign TMP Font");
                    text.font = targetFont;
                    EditorUtility.SetDirty(text);
                    assignedCount++;
                }
            }

            Debug.Log(
                $"<color=green>[AutoTMPFontAssigner]</color> Font assigned to <color=yellow>{assignedCount}</color> TMP_Text object(s).");
        }
    }
}
