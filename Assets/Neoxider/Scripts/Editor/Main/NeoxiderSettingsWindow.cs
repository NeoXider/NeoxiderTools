using UnityEditor;
using UnityEngine;

public class NeoxiderSettingsWindow : EditorWindow
{
    public static bool EnableAttributeSearch = true;

    [MenuItem("Tools/Neoxider/Settings")]
    public static void ShowWindow()
    {
        GetWindow<NeoxiderSettingsWindow>("Neoxider Settings");
    }

    private void OnEnable()
    {

    }

    private void OnGUI()
    {
        GUILayout.Label("Neoxider Global Settings", EditorStyles.boldLabel);

        EnableAttributeSearch = EditorGUILayout.Toggle("Enable Attribute Search", EnableAttributeSearch);

        if (GUI.changed)
        {
            // Update the static setting when the checkbox is changed
        }
    }
}