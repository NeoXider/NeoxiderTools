using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Editor window for Neoxider global settings
    /// </summary>
    public class NeoxiderSettingsWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Neoxider/Settings";
        private const string WindowTitle = "Neoxider Settings";
        private bool isDirty;

        private Vector2 scrollPosition;
        private SerializedObject serializedHierarchy;
        private bool showFolderSettings = true;
        private bool showHierarchySettings = true;

        private void ResetToDefaults()
        {
            NeoxiderSettings.ResetToDefaults();
            serializedHierarchy = null;
            isDirty = true;
        }

        #region Window Management

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<NeoxiderSettingsWindow>(WindowTitle);
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            NeoxiderSettings.LoadSettings();
            if (NeoxiderSettings.Current.validateFoldersOnStart) ValidateFolders();

            // Initialize serialized object for hierarchy settings
            serializedHierarchy = new SerializedObject(NeoxiderSettings.SceneHierarchy);
        }

        private void OnDisable()
        {
            if (isDirty) NeoxiderSettings.SaveSettings();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space();

            DrawGeneralSettings();
            EditorGUILayout.Space();

            DrawFolderSettings();
            EditorGUILayout.Space();

            DrawHierarchySettings();
            EditorGUILayout.Space();

            DrawActionButtons();

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                isDirty = true;
                NeoxiderSettings.SaveSettings();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Neoxider Global Settings", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(120)))
                    if (EditorUtility.DisplayDialog("Reset Settings",
                            "Are you sure you want to reset all settings to defaults?",
                            "Yes", "No"))
                        ResetToDefaults();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGeneralSettings()
        {
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            NeoxiderSettings.Current.enableAttributeSearch = EditorGUILayout.Toggle(
                new GUIContent("Enable Attribute Search",
                    "Enable searching for custom attributes in scripts"),
                NeoxiderSettings.Current.enableAttributeSearch);

            NeoxiderSettings.Current.validateFoldersOnStart = EditorGUILayout.Toggle(
                new GUIContent("Validate Folders on Start",
                    "Check for missing folders when Unity starts"),
                NeoxiderSettings.Current.validateFoldersOnStart);

            EditorGUI.indentLevel--;
        }

        private void DrawFolderSettings()
        {
            showFolderSettings = EditorGUILayout.Foldout(showFolderSettings, "Folder Structure", true);
            if (showFolderSettings)
            {
                EditorGUI.indentLevel++;

                NeoxiderSettings.Current.rootFolder = EditorGUILayout.TextField(
                    new GUIContent("Root Folder",
                        "The main folder where all project assets will be organized"),
                    NeoxiderSettings.Current.rootFolder);

                EditorGUILayout.LabelField("Project Folders");
                EditorGUI.indentLevel++;

                // Folder list
                for (var i = 0; i < NeoxiderSettings.Current.folders.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    NeoxiderSettings.Current.folders[i] =
                        EditorGUILayout.TextField(NeoxiderSettings.Current.folders[i]);

                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        NeoxiderSettings.Current.folders =
                            NeoxiderSettings.Current.folders.Where((_, index) => index != i).ToArray();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // Add new folder button
                if (GUILayout.Button("Add Folder"))
                {
                    var list = NeoxiderSettings.Current.folders.ToList();
                    list.Add("New Folder");
                    NeoxiderSettings.Current.folders = list.ToArray();
                }

                EditorGUI.indentLevel -= 2;
            }
        }

        private void DrawHierarchySettings()
        {
            showHierarchySettings = EditorGUILayout.Foldout(showHierarchySettings, "Scene Hierarchy", true);
            if (showHierarchySettings)
            {
                EditorGUI.indentLevel++;

                if (serializedHierarchy == null)
                    serializedHierarchy = new SerializedObject(NeoxiderSettings.SceneHierarchy);

                if (serializedHierarchy != null)
                {
                    serializedHierarchy.Update();

                    var iterator = serializedHierarchy.GetIterator();
                    var enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        if (iterator.name == "m_Script") continue;

                        EditorGUILayout.PropertyField(iterator, true);
                        enterChildren = false;
                    }

                    serializedHierarchy.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Missing Folders")) CreateMissingFolders();

            if (GUILayout.Button("Create Scene Hierarchy")) NeoxiderSettings.SceneHierarchy.CreateHierarchy();
        }

        #endregion

        #region Folder Management

        private void CreateMissingFolders()
        {
            try
            {
                var sourcePath = NeoxiderSettings.RootFolderPath;
                CreateFolderIfMissing(sourcePath);

                foreach (var folder in NeoxiderSettings.Current.folders)
                {
                    var folderPath = NeoxiderSettings.GetFolderPath(folder);
                    CreateFolderIfMissing(folderPath);
                }

                AssetDatabase.Refresh();
                Debug.Log("Created missing folders successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create folders: {e.Message}");
            }
        }

        private void ValidateFolders()
        {
            var sourcePath = NeoxiderSettings.RootFolderPath;
            var hasErrors = false;

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogWarning($"Root folder missing: {NeoxiderSettings.Current.rootFolder}");
                hasErrors = true;
            }

            foreach (var folder in NeoxiderSettings.Current.folders)
                if (!NeoxiderSettings.FolderExists(folder))
                {
                    Debug.LogWarning($"Missing folder: {folder}");
                    hasErrors = true;
                }

            if (!hasErrors) Debug.Log("All folders exist");
        }

        private void CreateFolderIfMissing(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"Created folder: {path}");
            }
        }

        #endregion
    }
}