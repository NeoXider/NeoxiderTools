using Neo.Editor.Windows;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     Окно редактора для автоматического сохранения сцен
/// </summary>
[InitializeOnLoad]
public class SceneSaver : EditorWindow
{
    private static readonly SceneSaverGUI _staticGUI;
    private static double _lastSaveTime;
    private SceneSaverGUI _gui;

    static SceneSaver()
    {
        _staticGUI = new SceneSaverGUI();
        _staticGUI.UpdateCurrentScenePath();
        _lastSaveTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += BackgroundSaveCheck;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private void OnEnable()
    {
        _gui = new SceneSaverGUI();
        _gui.UpdateCurrentScenePath();
        _lastSaveTime = EditorApplication.timeSinceStartup;
    }

    private void OnGUI()
    {
        _gui?.OnGUI(this);
    }

    /// <summary>
    ///     Показывает окно Scene Saver
    /// </summary>
    [MenuItem("Tools/Neoxider/Scene Saver")]
    public static void ShowWindow()
    {
        SceneSaver window = GetWindow<SceneSaver>("Scene Saver");
        window.minSize = new Vector2(250, 100);
        _staticGUI?.UpdateCurrentScenePath();
    }

    private static void BackgroundSaveCheck()
    {
        if (_staticGUI != null && _staticGUI.IsScriptEnabled && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastSaveTime >= _staticGUI.IntervalMinutes * 60)
            {
                _staticGUI.SaveSceneClone();
                _lastSaveTime = currentTime;
            }
        }
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        _staticGUI?.UpdateCurrentScenePath();
    }
}