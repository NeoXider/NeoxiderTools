using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Windows
{
    /// <summary>
    /// GUI отрисовка для окна Scene Saver
    /// </summary>
    public class SceneSaverGUI : EditorWindowGUI
    {
        private bool _isScriptEnabled = true;
        private float _intervalMinutes = 3f;
        private bool _saveEvenIfNotDirty;
        private string _currentScenePath;
        private string _lastSaveStatus = "";

        /// <summary>
        /// Обновление пути текущей сцены
        /// </summary>
        public void UpdateCurrentScenePath()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            _currentScenePath = activeScene.path;

            if (string.IsNullOrEmpty(_currentScenePath))
            {
                _currentScenePath = "Untitled";
            }
        }

        /// <summary>
        /// Отрисовка GUI
        /// </summary>
        public override void OnGUI(EditorWindow window)
        {
            _isScriptEnabled = EditorGUILayout.Toggle("Enable Scene Saver Script", _isScriptEnabled);
            _intervalMinutes = EditorGUILayout.FloatField("Interval (minutes)", _intervalMinutes);
            _saveEvenIfNotDirty = EditorGUILayout.Toggle("Save Even If Not Dirty", _saveEvenIfNotDirty);
            EditorGUILayout.LabelField("Current Scene", _currentScenePath);
            EditorGUILayout.LabelField("Last Save Status", _lastSaveStatus);

            if (GUILayout.Button("Save Now"))
            {
                SaveSceneClone();
            }
        }

        /// <summary>
        /// Получение состояния включенности скрипта
        /// </summary>
        public bool IsScriptEnabled => _isScriptEnabled;

        /// <summary>
        /// Получение интервала в минутах
        /// </summary>
        public float IntervalMinutes => _intervalMinutes;

        /// <summary>
        /// Получение флага сохранения даже если не изменено
        /// </summary>
        public bool SaveEvenIfNotDirty => _saveEvenIfNotDirty;

        /// <summary>
        /// Сохранение копии сцены
        /// </summary>
        public void SaveSceneClone()
        {
            if (EditorApplication.isPlaying)
            {
                _lastSaveStatus = "Skipping auto-save in Play mode.";
                return;
            }

            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(currentScene.path))
            {
                _lastSaveStatus = "Scene not saved yet, skipping auto-save.";
                return;
            }

            if (!currentScene.isDirty && !_saveEvenIfNotDirty)
            {
                _lastSaveStatus = "Scene is not dirty, skipping auto-save.";
                return;
            }

            try
            {
                UpdateCurrentScenePath();
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(_currentScenePath);
                string autoSaveFolder = System.IO.Path.Combine("Assets", "Scenes", "AutoSaves");
                string newScenePath = System.IO.Path.Combine(autoSaveFolder, $"{sceneName}_AutoSave.unity");

                if (!System.IO.Directory.Exists(autoSaveFolder))
                {
                    System.IO.Directory.CreateDirectory(autoSaveFolder);
                    AssetDatabase.Refresh();
                }

                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene, newScenePath, true);

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
            catch (System.Exception e)
            {
                _lastSaveStatus = $"Error: {e.Message}";
                Debug.LogError($"[SceneSaver] {e.Message}");
            }
        }
    }
}

