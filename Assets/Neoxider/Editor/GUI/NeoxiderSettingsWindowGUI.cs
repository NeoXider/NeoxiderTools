using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GUI = UnityEngine.GUI;

namespace Neo.Editor.Windows
{
    /// <summary>
    ///     GUI drawing for the Neoxider settings window.
    /// </summary>
    public class NeoxiderSettingsWindowGUI : EditorWindowGUI
    {
        private static readonly GUIContent RemoveFolderContent = new("×", "Remove this folder from the list.");
        private static readonly GUILayoutOption[] RemoveButtonOption = { GUILayout.Width(22) };

        private bool _isDirty;
        private Vector2 _scrollPosition;
        private SerializedObject _serializedHierarchy;
        private bool _showFolderSettings = true;
        private bool _showHierarchySettings = true;

        /// <summary>
        ///     Initializes GUI state.
        /// </summary>
        public void Initialize()
        {
            _serializedHierarchy = new SerializedObject(NeoxiderSettings.SceneHierarchy);
        }

        /// <summary>
        ///     Draws the window GUI.
        /// </summary>
        public override void OnGUI(EditorWindow window)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

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
                _isDirty = true;
                NeoxiderSettings.SaveSettings();
            }
        }

        /// <summary>
        ///     Persists changes when the window closes.
        /// </summary>
        public void OnDisable()
        {
            if (_isDirty)
            {
                NeoxiderSettings.SaveSettings();
            }
        }

        /// <summary>
        ///     Resets settings to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            NeoxiderSettings.ResetToDefaults();
            _serializedHierarchy = null;
            _isDirty = true;
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Neoxider Global Settings", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(120)))
                {
                    if (EditorUtility.DisplayDialog("Reset Settings",
                            "Are you sure you want to reset all settings to defaults?",
                            "Yes", "No"))
                    {
                        ResetToDefaults();
                    }
                }
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
            _showFolderSettings = EditorGUILayout.Foldout(_showFolderSettings, "Folder Structure", true);
            if (_showFolderSettings)
            {
                EditorGUI.indentLevel++;

                NeoxiderSettings.Current.rootFolder = EditorGUILayout.TextField(
                    new GUIContent("Root Folder",
                        "The main folder where all project assets will be organized"),
                    NeoxiderSettings.Current.rootFolder);

                EditorGUILayout.LabelField("Project Folders");
                EditorGUI.indentLevel++;

                int removeIndex = -1;
                for (int i = 0; i < NeoxiderSettings.Current.folders.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    NeoxiderSettings.Current.folders[i] =
                        EditorGUILayout.TextField(NeoxiderSettings.Current.folders[i]);

                    if (GUILayout.Button(RemoveFolderContent, RemoveButtonOption))
                    {
                        removeIndex = i;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                bool addFolder = GUILayout.Button("Add Folder");

                EditorGUI.indentLevel -= 2;

                // WHY: the list changes only after every row of this event is drawn. Removing inside the loop and
                // breaking out of it skipped the remaining rows, so the controls after them read the layout entries
                // of the rows above.
                if (removeIndex >= 0)
                {
                    NeoxiderSettings.Current.folders =
                        NeoxiderSettings.Current.folders.Where((_, index) => index != removeIndex).ToArray();
                    GUI.changed = true;
                }

                if (addFolder)
                {
                    List<string> list = NeoxiderSettings.Current.folders.ToList();
                    list.Add("New Folder");
                    NeoxiderSettings.Current.folders = list.ToArray();
                    GUI.changed = true;
                }
            }
        }

        private void DrawHierarchySettings()
        {
            _showHierarchySettings = EditorGUILayout.Foldout(_showHierarchySettings, "Scene Hierarchy", true);
            if (_showHierarchySettings)
            {
                EditorGUI.indentLevel++;

                if (_serializedHierarchy == null)
                {
                    _serializedHierarchy = new SerializedObject(NeoxiderSettings.SceneHierarchy);
                }

                if (_serializedHierarchy != null)
                {
                    _serializedHierarchy.Update();

                    SerializedProperty iterator = _serializedHierarchy.GetIterator();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        if (iterator.name == "m_Script")
                        {
                            continue;
                        }

                        EditorGUILayout.PropertyField(iterator, true);
                        enterChildren = false;
                    }

                    _serializedHierarchy.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Missing Folders"))
            {
                CreateMissingFolders();
            }

            if (GUILayout.Button("Create Scene Hierarchy"))
            {
                NeoxiderSettings.SceneHierarchy.CreateHierarchy();
            }
        }

        private void CreateMissingFolders()
        {
            try
            {
                string sourcePath = NeoxiderSettings.RootFolderPath;
                CreateFolderIfMissing(sourcePath);

                foreach (string folder in NeoxiderSettings.Current.folders)
                {
                    string folderPath = NeoxiderSettings.GetFolderPath(folder);
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

        private void CreateFolderIfMissing(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"Created folder: {path}");
            }
        }
    }
}
