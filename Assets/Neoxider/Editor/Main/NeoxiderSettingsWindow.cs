using System.Collections.Generic;
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
        private const string MenuPath = "Neoxider/Settings";
        private const string WindowTitle = "Neoxider Settings";
        internal const string FoldersValidatedSessionKey = "Neo.NeoxiderSettingsWindow.FoldersValidated";
        private NeoxiderSettingsWindowGUI _gui;

        private void OnEnable()
        {
            NeoxiderSettings.LoadSettings();
            // WHY: OnEnable also runs after every domain reload while the window is open; the check is meant for
            // editor start, so it runs once per editor session instead of logging again on each recompile.
            if (NeoxiderSettings.Current.validateFoldersOnStart &&
                !SessionState.GetBool(FoldersValidatedSessionKey, false))
            {
                SessionState.SetBool(FoldersValidatedSessionKey, true);
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
        [MenuItem(MenuPath, false, 400)]
        public static void ShowWindow()
        {
            NeoxiderSettingsWindow window = GetWindow<NeoxiderSettingsWindow>(WindowTitle);
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        // WHY: one aggregated warning instead of one line per folder, and nothing at all when the structure is
        // complete; "Create Missing Folders" in the window fixes what is listed.
        private static void ValidateFolders()
        {
            List<string> missing = NeoxiderSettings.FindMissingFolders();
            if (missing.Count > 0)
            {
                Debug.LogWarning(
                    $"[Neoxider] Missing project folders under '{NeoxiderSettings.Current.rootFolder}': " +
                    string.Join(", ", missing) + ". Use Neoxider/Settings > Create Missing Folders.");
            }
        }
    }
}
