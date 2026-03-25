using System.IO;
using Neo.Editor.Windows;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Editor window for global Neoxider settings.
    /// </summary>
    public class NeoxiderSettingsWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Neoxider/Settings";
        private const string WindowTitle = "Neoxider Settings";
        private NeoxiderSettingsWindowGUI _gui;

        private void OnEnable()
        {
            NeoxiderSettings.LoadSettings();
            if (NeoxiderSettings.Current.validateFoldersOnStart)
            {
                ValidateFolders();
            }

            _gui = new NeoxiderSettingsWindowGUI();
            _gui.Initialize();
        }

        private void OnDisable()
        {
            _gui?.OnDisable();
        }

        private void OnGUI()
        {
            _gui?.OnGUI(this);
        }

        /// <summary>
        ///     Opens the settings window.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            NeoxiderSettingsWindow window = GetWindow<NeoxiderSettingsWindow>(WindowTitle);
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void ValidateFolders()
        {
            string sourcePath = NeoxiderSettings.RootFolderPath;
            bool hasErrors = false;

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogWarning($"Root folder missing: {NeoxiderSettings.Current.rootFolder}");
                hasErrors = true;
            }

            foreach (string folder in NeoxiderSettings.Current.folders)
            {
                if (!NeoxiderSettings.FolderExists(folder))
                {
                    Debug.LogWarning($"Missing folder: {folder}");
                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                Debug.Log("All folders exist");
            }
        }
    }
}
