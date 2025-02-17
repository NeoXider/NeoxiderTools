using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    public class NeoxiderSettingsWindow : EditorWindow
    {
        public static bool EnableAttributeSearch = true;

        [Space]
        [Header("Folders")]
        [SerializeField]
        private string rootFolder = "_source";

        [SerializeField]
        private string[] folders = { "Audio", "Prefabs", "Scripts", "Animations", "Sprites", "TTF", "Materials" };

        [Space]
        [Header("SceneHierarchy")]
        [SerializeField]
        private CreateSceneHierarchy createSceneHierarchy = new();

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

            if (GUILayout.Button("Create Missing Folders"))
            {
                CreateMissingFolders();
            }

            if (GUILayout.Button("Create Hierarchy"))
            {
                createSceneHierarchy.CreateHierarchy();
            }
        }

        private void CreateMissingFolders()
        {
            string sourcePath = Path.Combine(Application.dataPath, rootFolder);

            if (!Directory.Exists(sourcePath))
            {
                Directory.CreateDirectory(sourcePath);
            }

            foreach (var folder in folders)
            {
                string folderPath = Path.Combine(sourcePath, folder);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }

            AssetDatabase.Refresh();
        }
    }
}