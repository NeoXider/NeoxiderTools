using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo
{
    /// <summary>
    ///     Static access point for Neoxider settings
    /// </summary>
    public static class NeoxiderSettings
    {
        private const string SettingsPath = "ProjectSettings/NeoxiderSettings.json";
        private static NeoxiderData currentSettings;
        private static CreateSceneHierarchy sceneHierarchyInstance;

        #region Editor Methods

#if UNITY_EDITOR
        /// <summary>
        ///     Open settings window in Unity Editor
        /// </summary>
        [MenuItem("Tools/Neoxider/Settings")]
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
                if (currentSettings == null) LoadSettings();
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
                    sceneHierarchyInstance = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
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
        ///     Get relative path from Assets folder
        /// </summary>
        public static string GetRelativePath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
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
            if (sceneHierarchyInstance != null) Object.DestroyImmediate(sceneHierarchyInstance);
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
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    currentSettings = JsonUtility.FromJson<NeoxiderData>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load Neoxider settings: {e.Message}");
            }

            if (currentSettings == null) currentSettings = new NeoxiderData();

            // Create scene hierarchy instance
            if (sceneHierarchyInstance == null)
                sceneHierarchyInstance = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
        }

        /// <summary>
        ///     Save current settings to file
        /// </summary>
        public static void SaveSettings()
        {
            if (currentSettings == null) return;

            try
            {
                var json = JsonUtility.ToJson(currentSettings, true);
                File.WriteAllText(SettingsPath, json);

#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save Neoxider settings: {e.Message}");
            }
        }

        #endregion
    }
}