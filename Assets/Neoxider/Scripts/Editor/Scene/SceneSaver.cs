using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

[InitializeOnLoad]
public class SceneSaver : EditorWindow
{
    private static bool isScriptEnabled = true;
    public static float intervalMinutes = 3f;
    public static bool saveEvenIfNotDirty = false;
    private static string currentScenePath;
    private static double lastSaveTime;
    private static string lastSaveStatus = "";

    static SceneSaver()
    {
        EditorApplication.update += BackgroundSaveCheck;
    }

    [MenuItem("Tools/Neoxider/Scene Saver Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneSaver>("Scene Saver Settings");
        window.minSize = new Vector2(250, 100);
        UpdateCurrentScenePath();
    }

    private static void BackgroundSaveCheck()
    {
        if (isScriptEnabled && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastSaveTime >= intervalMinutes * 60)
            {
                SaveSceneClone();
                lastSaveTime = currentTime;
            }
        }
    }

    void OnEnable()
    {
        UpdateCurrentScenePath();
        lastSaveTime = EditorApplication.timeSinceStartup;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
    }

    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        UpdateCurrentScenePath();
    }

    private static void UpdateCurrentScenePath()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        currentScenePath = activeScene.path;

        if (string.IsNullOrEmpty(currentScenePath))
        {
            currentScenePath = "Untitled";
        }
    }

    private static void SaveSceneClone()
    {
        if (EditorApplication.isPlaying) return;

        var currentScene = EditorSceneManager.GetActiveScene();
        if (!currentScene.isDirty && !saveEvenIfNotDirty)
        {
            lastSaveStatus = "Scene is not dirty, skipping auto-save.";
            return;
        }

        UpdateCurrentScenePath();
        string sceneName = Path.GetFileNameWithoutExtension(currentScenePath);
        string newScenePath = Path.Combine("Assets", "Scenes", "AutoSaves", $"{sceneName}_AutoSave.unity");

        Directory.CreateDirectory(Path.Combine("Assets", "Scenes", "AutoSaves"));

        EditorSceneManager.SaveScene(currentScene, newScenePath, true);
        lastSaveStatus = $"Auto-saved scene clone: {newScenePath}";
        Debug.Log(lastSaveStatus);
    }

    void OnGUI()
    {
        isScriptEnabled = EditorGUILayout.Toggle("Enable Scene Saver Script", isScriptEnabled);
        intervalMinutes = EditorGUILayout.FloatField("Interval (minutes)", intervalMinutes);
        saveEvenIfNotDirty = EditorGUILayout.Toggle("Save Even If Not Dirty", saveEvenIfNotDirty);
        EditorGUILayout.LabelField("Current Scene", currentScenePath);
        EditorGUILayout.LabelField("Last Save Status", lastSaveStatus);

        if (GUILayout.Button("Save Now"))
        {
            SaveSceneClone();
        }
    }
}