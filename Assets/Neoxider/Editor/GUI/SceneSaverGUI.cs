using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Windows
{
    /// <summary>
    ///     GUI for the Scene Saver window.
    /// </summary>
    public class SceneSaverGUI : EditorWindowGUI
    {
        private string _currentScenePath;
        private string _lastSaveStatus = "";

        /// <summary>
        ///     Whether the saver script is enabled.
        /// </summary>
        public bool IsScriptEnabled { get; private set; } = true;

        /// <summary>
        ///     Auto-save interval in minutes.
        /// </summary>
        public float IntervalMinutes { get; private set; } = 3f;

        /// <summary>
        ///     Whether to save even when the scene is not dirty.
        /// </summary>
        public bool SaveEvenIfNotDirty { get; private set; }

        /// <summary>
        ///     Refreshes the active scene path.
        /// </summary>
        public void UpdateCurrentScenePath()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            _currentScenePath = activeScene.path;

            if (string.IsNullOrEmpty(_currentScenePath))
            {
                _currentScenePath = "Untitled";
            }
        }

        /// <summary>
        ///     Draws the window GUI.
        /// </summary>
        public override void OnGUI(EditorWindow window)
        {
            UpdateCurrentScenePath();
            Scene activeScene = EditorSceneManager.GetActiveScene();
            bool hasSavedScene = !string.IsNullOrEmpty(activeScene.path);

            NeoxiderEditorGUI.DrawSummaryCard("Scene Saver",
                "Automatically saves a backup copy of the active scene to `Assets/Scenes/AutoSaves`.",
                new NeoxiderEditorGUI.Badge(IsScriptEnabled ? "Enabled" : "Disabled",
                    IsScriptEnabled ? new Color(0.18f, 0.62f, 0.32f, 1f) : new Color(0.46f, 0.46f, 0.50f, 1f)),
                new NeoxiderEditorGUI.Badge($"Interval {IntervalMinutes:0.##} min", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge(activeScene.isDirty ? "Scene Dirty" : "Scene Clean",
                    activeScene.isDirty ? new Color(0.78f, 0.46f, 0.18f, 1f) : new Color(0.24f, 0.60f, 0.60f, 1f)));

            if (!hasSavedScene)
            {
                EditorGUILayout.HelpBox(
                    "The active scene is not saved as an asset yet. Auto-save will work after the first normal Save Scene.",
                    MessageType.Info);
            }

            NeoxiderEditorGUI.BeginSection("Auto Save", "Basic automatic save options.");
            IsScriptEnabled = EditorGUILayout.Toggle("Enable Scene Saver Script", IsScriptEnabled);
            IntervalMinutes = EditorGUILayout.FloatField("Interval (minutes)", IntervalMinutes);
            SaveEvenIfNotDirty = EditorGUILayout.Toggle("Save Even If Not Dirty", SaveEvenIfNotDirty);
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            NeoxiderEditorGUI.BeginSection("Current Scene",
                "State of the open scene and the last save result.");
            EditorGUILayout.LabelField("Current Scene", _currentScenePath);
            EditorGUILayout.LabelField("Last Save Status",
                string.IsNullOrEmpty(_lastSaveStatus) ? "No saves yet." : _lastSaveStatus);
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(4f);

            EditorGUI.BeginDisabledGroup(!hasSavedScene && !activeScene.isDirty);
            if (GUILayout.Button("Save Now", GUILayout.Height(24f)))
            {
                SaveSceneClone();
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        ///     Saves a copy of the scene to the auto-save folder.
        /// </summary>
        public void SaveSceneClone()
        {
            if (EditorApplication.isPlaying)
            {
                _lastSaveStatus = "Skipping auto-save in Play mode.";
                return;
            }

            Scene currentScene = EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(currentScene.path))
            {
                _lastSaveStatus = "Scene not saved yet, skipping auto-save.";
                return;
            }

            if (!currentScene.isDirty && !SaveEvenIfNotDirty)
            {
                _lastSaveStatus = "Scene is not dirty, skipping auto-save.";
                return;
            }

            try
            {
                UpdateCurrentScenePath();
                string sceneName = Path.GetFileNameWithoutExtension(_currentScenePath);
                string autoSaveFolder = Path.Combine("Assets", "Scenes", "AutoSaves");
                string newScenePath = Path.Combine(autoSaveFolder, $"{sceneName}_AutoSave.unity");

                if (!Directory.Exists(autoSaveFolder))
                {
                    Directory.CreateDirectory(autoSaveFolder);
                    AssetDatabase.Refresh();
                }

                bool saved = EditorSceneManager.SaveScene(currentScene, newScenePath, true);

                if (saved)
                {
                    _lastSaveStatus = $"Auto-saved: {newScenePath}";
                    Debug.Log($"[SceneSaver] {_lastSaveStatus}");
                }
                else
                {
                    _lastSaveStatus = "Auto-save failed.";
                    Debug.LogWarning($"[SceneSaver] Failed to save scene to {newScenePath}");
                }
            }
            catch (Exception e)
            {
                _lastSaveStatus = $"Error: {e.Message}";
                Debug.LogError($"[SceneSaver] {e.Message}");
            }
        }
    }
}
