using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo
{
    /// <summary>
    ///     Static access point for Neoxider settings
    /// </summary>
    public static class NeoxiderSettings
    {
        private const string SettingsPath = "ProjectSettings/NeoxiderSettings.json";
        private static NeoxiderData currentSettings;

        /// <summary>Test seam: redirects load/save to another file; null uses the project settings file.</summary>
        internal static string SettingsPathOverride;

        private static string ActiveSettingsPath => string.IsNullOrEmpty(SettingsPathOverride)
            ? SettingsPath
            : SettingsPathOverride;
        private static CreateSceneHierarchy sceneHierarchyInstance;

        #region Editor Methods

#if UNITY_EDITOR
        /// <summary>
        ///     Open settings window in Unity Editor
        /// </summary>
        public static void OpenSettings()
        {
            NeoxiderSettingsWindow.ShowWindow();
        }
#endif

        #endregion

        #region Settings Data Class

        [Serializable]
        public class NeoxiderData
        {
            public bool enableAttributeSearch = true;
            public string rootFolder = "_source";

            public string[] folders =
            {
                "Audio",
                "Prefabs",
                "Scripts",
                "Animations",
                "Sprites",
                "Materials",
                "Textures",
                "Models",
                "Scenes",
                "Resources",
                "Settings",
                "Editor",
                "TTF"
            };

            public bool validateFoldersOnStart = true;

            /// <summary>
            ///     Scene hierarchy template (<see cref="CreateSceneHierarchy" />) as JSON. Empty in files written
            ///     before 11.x; the template defaults are used then.
            /// </summary>
            public string sceneHierarchyJson = string.Empty;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Current settings instance
        /// </summary>
        public static NeoxiderData Current
        {
            get
            {
                if (currentSettings == null)
                {
                    LoadSettings();
                }

                return currentSettings;
            }
            internal set => currentSettings = value;
        }

        /// <summary>
        ///     Scene hierarchy settings
        /// </summary>
        public static CreateSceneHierarchy SceneHierarchy
        {
            get
            {
                if (sceneHierarchyInstance == null)
                {
                    // WHY: the menu items (Create/Sort Scene Hierarchy and their validator) can be the first settings
                    // access after a restart or domain reload; a bare CreateInstance would ignore the saved template.
                    if (currentSettings == null)
                    {
                        LoadSettings();
                    }
                    else
                    {
                        sceneHierarchyInstance = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
                        ApplySavedSceneHierarchy();
                    }
                }

                return sceneHierarchyInstance;
            }
        }

        /// <summary>
        ///     Root folder path for the project
        /// </summary>
        public static string RootFolderPath => Path.Combine(Application.dataPath, Current.rootFolder);

        /// <summary>
        ///     Whether attribute search is enabled
        /// </summary>
        public static bool EnableAttributeSearch => Current.enableAttributeSearch;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Get the full path for a project folder
        /// </summary>
        public static string GetFolderPath(string folderName)
        {
            return Path.Combine(RootFolderPath, folderName);
        }

        /// <summary>
        ///     Check if a folder exists in the project structure
        /// </summary>
        public static bool FolderExists(string folderName)
        {
            return Directory.Exists(GetFolderPath(folderName));
        }

        /// <summary>
        ///     Lists the configured project folders that do not exist on disk: the root folder first (when missing),
        ///     then each entry of <see cref="NeoxiderData.folders" /> by name. Empty when the structure is complete.
        /// </summary>
        public static List<string> FindMissingFolders()
        {
            List<string> missing = new();
            NeoxiderData data = Current;
            if (!Directory.Exists(RootFolderPath))
            {
                missing.Add(data.rootFolder);
            }

            if (data.folders == null)
            {
                return missing;
            }

            foreach (string folder in data.folders)
            {
                if (!string.IsNullOrEmpty(folder) && !FolderExists(folder))
                {
                    missing.Add(folder);
                }
            }

            return missing;
        }

        /// <summary>
        ///     Get relative path from Assets folder
        /// </summary>
        public static string GetRelativePath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }

            return absolutePath;
        }

        #endregion

        #region Settings Management

        /// <summary>
        ///     Reset settings to default values
        /// </summary>
        public static void ResetToDefaults()
        {
            currentSettings = new NeoxiderData();
            if (sceneHierarchyInstance != null)
            {
                Object.DestroyImmediate(sceneHierarchyInstance);
            }

            sceneHierarchyInstance = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
            SaveSettings();
        }

        /// <summary>
        ///     Load settings from file
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(ActiveSettingsPath))
                {
                    string json = File.ReadAllText(ActiveSettingsPath);
                    currentSettings = JsonUtility.FromJson<NeoxiderData>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load Neoxider settings: {e.Message}");
            }

            if (currentSettings == null)
            {
                currentSettings = new NeoxiderData();
            }

            if (sceneHierarchyInstance == null)
            {
                sceneHierarchyInstance = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
            }

            ApplySavedSceneHierarchy();
        }

        /// <summary>
        ///     Test seam: replaces the cached settings and template without touching the file; (null, null) mimics the
        ///     state right after a domain reload. Callers own both objects.
        /// </summary>
        internal static void SetCachedStateForTests(NeoxiderData data, CreateSceneHierarchy hierarchy)
        {
            currentSettings = data;
            sceneHierarchyInstance = hierarchy;
        }

        private static void ApplySavedSceneHierarchy()
        {
            // WHY: the template edited in the settings window used to live only in this in-memory instance, so every
            // change was lost on the next domain reload or editor restart.
            if (currentSettings == null || sceneHierarchyInstance == null ||
                string.IsNullOrEmpty(currentSettings.sceneHierarchyJson))
            {
                return;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(currentSettings.sceneHierarchyJson, sceneHierarchyInstance);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load Neoxider scene hierarchy settings: {e.Message}");
            }
        }

        /// <summary>
        ///     Save current settings to file
        /// </summary>
        public static void SaveSettings()
        {
            if (currentSettings == null)
            {
                return;
            }

            try
            {
                if (sceneHierarchyInstance != null)
                {
                    currentSettings.sceneHierarchyJson = JsonUtility.ToJson(sceneHierarchyInstance);
                }

                string json = JsonUtility.ToJson(currentSettings, true);
                // WHY: ProjectSettings/ is not an asset folder, so no AssetDatabase.Refresh: the settings window
                // saves on every GUI change and each refresh rescanned the whole project (one per typed character).
                File.WriteAllText(ActiveSettingsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save Neoxider settings: {e.Message}");
            }
        }

        #endregion
    }
}
