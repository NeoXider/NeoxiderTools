using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Windows
{
    /// <summary>
    ///     GUI отрисовка для окна Scene Saver
    /// </summary>
    public class SceneSaverGUI : EditorWindowGUI
    {
        private string _currentScenePath;
        private string _lastSaveStatus = "";

        /// <summary>
        ///     Получение состояния включенности скрипта
        /// </summary>
        public bool IsScriptEnabled { get; private set; } = true;

        /// <summary>
        ///     Получение интервала в минутах
        /// </summary>
        public float IntervalMinutes { get; private set; } = 3f;

        /// <summary>
        ///     Получение флага сохранения даже если не изменено
        /// </summary>
        public bool SaveEvenIfNotDirty { get; private set; }

        /// <summary>
        ///     Обновление пути текущей сцены
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
        ///     Отрисовка GUI
        /// </summary>
        public override void OnGUI(EditorWindow window)
        {
            IsScriptEnabled = EditorGUILayout.Toggle("Enable Scene Saver Script", IsScriptEnabled);
            IntervalMinutes = EditorGUILayout.FloatField("Interval (minutes)", IntervalMinutes);
            SaveEvenIfNotDirty = EditorGUILayout.Toggle("Save Even If Not Dirty", SaveEvenIfNotDirty);
            EditorGUILayout.LabelField("Current Scene", _currentScenePath);
            EditorGUILayout.LabelField("Last Save Status", _lastSaveStatus);

            if (GUILayout.Button("Save Now"))
            {
                SaveSceneClone();
            }
        }

        /// <summary>
        ///     Сохранение копии сцены
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