using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;

public class AutoTMPFontAssignerEditor : EditorWindow
{
    private TMP_FontAsset targetFont;
    private int assignedCount = 0;

    [MenuItem("Tools/UIKit/Auto TMP Font Assigner")]
    public static void ShowWindow()
    {
        GetWindow<AutoTMPFontAssignerEditor>("Auto TMP Font Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto TMP Font Assigner", EditorStyles.boldLabel);
        GUILayout.Space(5);

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "Target TMP Font", targetFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);

        GUI.enabled = targetFont != null;
        if (GUILayout.Button("Assign Font To All TMP_Text")) AssignFontToAllTMPText();
        GUI.enabled = true;

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Проходит по всем компонентам TMP_Text в открытых сценах и назначает указанный шрифт.\n" +
            "Игнорирует тексты в ассетах (префабах, ScriptableObject и т.п.).",
            MessageType.Info);
    }

    private void AssignFontToAllTMPText()
    {
        assignedCount = 0;

        // Находит все TMP_Text, даже в неактивных объектах
        var allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        foreach (var text in allTexts)
        {
            var go = text.gameObject;
            // Пропускаем объекты, не находящиеся в загруженных сценах
            if (string.IsNullOrEmpty(go.scene.name))
                continue;

            if (text.font != targetFont)
            {
                Undo.RecordObject(text, "Assign TMP Font");
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                assignedCount++;
            }
        }

        Debug.Log(
            $"<color=green>[AutoTMPFontAssigner]</color> Шрифт назначен: <color=yellow>{assignedCount}</color> объектов TMP_Text.");
    }
}