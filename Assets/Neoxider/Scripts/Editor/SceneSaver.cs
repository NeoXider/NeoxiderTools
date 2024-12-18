using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

[InitializeOnLoad]
public class SceneSaver : EditorWindow
{
    public float intervalMinutes = 3f;
    public bool isEnabled = true;
    public bool saveEvenIfNotDirty = false;
    private string currentScenePath;
    private double lastSaveTime;
    private string lastSaveStatus = "";

    static SceneSaver()
    {
        EditorApplication.update += BackgroundSaveCheck;
    }

    [MenuItem("Tools/Neoxider/Scene Saver Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneSaver>("Scene Saver Settings");
        window.minSize = new Vector2(250, 100);
        window.UpdateCurrentScenePath();
    }

    private static void BackgroundSaveCheck()
    {
        if (Instance != null && Instance.isEnabled)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - Instance.lastSaveTime >= Instance.intervalMinutes * 60)
            {
                Instance.SaveSceneClone();
                Instance.lastSaveTime = currentTime;
            }
        }
    }

    private static SceneSaver Instance => GetWindow<SceneSaver>();

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

    void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        UpdateCurrentScenePath();
    }

    void UpdateCurrentScenePath()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        currentScenePath = activeScene.path;

        if (string.IsNullOrEmpty(currentScenePath))
        {
            currentScenePath = "Untitled";
        }
    }

    void SaveSceneClone()
    {
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
        isEnabled = EditorGUILayout.Toggle("Enable Scene Saver", isEnabled);
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
