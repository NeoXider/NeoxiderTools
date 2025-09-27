using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FindAndRemoveMissingScriptsWindow : EditorWindow
{
    private readonly List<MissingScriptInfo> objectsWithMissing = new();
    private int currentPrefab;
    private int currentScene;
    private string initialScenePath;
    private string[] prefabPaths;
    private string[] scenePaths;
    private Vector2 scroll;

    private int searchStep;
    private int selectedIndex = -1;
    private string status = "";

    private void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts in All Scenes & Prefabs"))
        {
            objectsWithMissing.Clear();
            status = "Searching...";
            initialScenePath = SceneManager.GetActiveScene().path;
            EditorApplication.update += SearchAll;
        }

        GUILayout.Label(status);

        if (objectsWithMissing.Count > 0)
        {
            GUILayout.Label($"Found {objectsWithMissing.Count} objects with Missing Scripts:");
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(350));
            try
            {
                for (var i = 0; i < objectsWithMissing.Count; i++)
                {
                    var info = objectsWithMissing[i];
                    // Цвета: выбранный — зелёный, остальные — красный
                    var style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = i == selectedIndex ? Color.green : Color.red;
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        // Кнопка выбора объекта
                        if (GUILayout.Button($"{info.assetPath}: {info.hierarchyPath}", style))
                        {
                            selectedIndex = i;
                            SelectAndPing(info, true);
                        }

                        // Кнопка удаления справа
                        if (GUILayout.Button("X", GUILayout.Width(70))) RemoveMissingScriptFromObject(info);
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            finally
            {
                GUILayout.EndScrollView();
            }

            if (GUILayout.Button("Remove All Missing Scripts")) RemoveAllMissingScripts();
        }
    }

    [MenuItem("Tools/Neoxider/Find & Remove Missing Scripts (All Scenes & Prefabs)")]
    public static void ShowWindow()
    {
        GetWindow<FindAndRemoveMissingScriptsWindow>("Find Missing Scripts");
    }

    private void SearchAll()
    {
        if (searchStep == 0)
        {
            scenePaths = AssetDatabase.FindAssets("t:Scene") ?? new string[0];
            prefabPaths = AssetDatabase.FindAssets("t:Prefab") ?? new string[0];
            currentScene = 0;
            currentPrefab = 0;
            searchStep = 1;
            status = "Searching scenes...";
        }
        else if (searchStep == 1)
        {
            if (currentScene < scenePaths.Length)
            {
                var sceneAssetPath = AssetDatabase.GUIDToAssetPath(scenePaths[currentScene]);
                status = $"Scanning scene: {sceneAssetPath}";
                var scene = EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Single);
                foreach (var go in scene.GetRootGameObjects()) FindMissingInHierarchy(go, sceneAssetPath, true, "/");
                currentScene++;
                EditorUtility.DisplayProgressBar("Searching Scenes", sceneAssetPath,
                    (float)currentScene / scenePaths.Length);
            }
            else
            {
                searchStep = 2;
                status = "Searching prefabs...";
                EditorUtility.ClearProgressBar();
            }
        }
        else if (searchStep == 2)
        {
            if (currentPrefab < prefabPaths.Length)
            {
                var prefabAssetPath = AssetDatabase.GUIDToAssetPath(prefabPaths[currentPrefab]);
                status = $"Scanning prefab: {prefabAssetPath}";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                if (prefab != null) FindMissingInHierarchy(prefab, prefabAssetPath, false, "/");
                currentPrefab++;
                EditorUtility.DisplayProgressBar("Searching Prefabs", prefabAssetPath,
                    (float)currentPrefab / prefabPaths.Length);
            }
            else
            {
                searchStep = 3;
                status = $"Found {objectsWithMissing.Count} objects with Missing Scripts.";
                EditorUtility.ClearProgressBar();
                EditorApplication.update -= SearchAll;
                // Возвращаемся к исходной сцене после поиска
                if (!string.IsNullOrEmpty(initialScenePath) && initialScenePath != SceneManager.GetActiveScene().path)
                    EditorSceneManager.OpenScene(initialScenePath, OpenSceneMode.Single);
                Repaint();
            }
        }
    }

    private void FindMissingInHierarchy(GameObject go, string assetPath, bool isScene, string parentPath)
    {
        var thisPath = parentPath + go.name;
        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            objectsWithMissing.Add(new MissingScriptInfo
            {
                assetPath = assetPath,
                hierarchyPath = thisPath,
                objectName = go.name,
                isScene = isScene
            });
        foreach (Transform child in go.transform)
            FindMissingInHierarchy(child.gameObject, assetPath, isScene, thisPath + "/");
    }

    private void SelectAndPing(MissingScriptInfo info, bool forceOpenScene = false)
    {
        var previousScenePath = initialScenePath ?? SceneManager.GetActiveScene().path;

        if (info.isScene)
        {
            // Всегда открываем нужную сцену
            var scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Single);
            var parts = info.hierarchyPath.Trim('/').Split('/');
            GameObject root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == parts[0])
                {
                    root = go;
                    break;
                }

            if (root != null)
            {
                var found = FindGameObjectByHierarchyPath(root, info.hierarchyPath);
                if (found != null)
                {
                    Selection.activeObject = found;
                    EditorGUIUtility.PingObject(found);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not found",
                        $"Object {info.hierarchyPath} not found in scene {info.assetPath}", "OK");
                }
            }
            // Не возвращаемся к предыдущей сцене при выборе (только при удалении)
        }
        else
        {
            // Префаб: загружаем и выделяем объект по пути
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.assetPath);
            if (prefab != null)
            {
                var found = FindGameObjectByHierarchyPath(prefab, info.hierarchyPath);
                if (found != null)
                {
                    Selection.activeObject = found;
                    EditorGUIUtility.PingObject(found);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not found",
                        $"Object {info.hierarchyPath} not found in prefab {info.assetPath}", "OK");
                }
            }
        }
    }

    private GameObject FindGameObjectByHierarchyPath(GameObject root, string path)
    {
        var parts = path.Trim('/').Split('/');
        var current = root;
        for (var i = 1; i < parts.Length; i++) // i=1, т.к. root уже есть
        {
            var child = current.transform.Find(parts[i]);
            if (child == null) return null;
            current = child.gameObject;
        }

        return current;
    }

    private void RemoveAllMissingScripts()
    {
        var removed = 0;
        var previousScenePath = initialScenePath ?? SceneManager.GetActiveScene().path;
        foreach (var info in objectsWithMissing)
        {
            GameObject go = null;
            if (info.isScene)
            {
                var scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Single);
                var parts = info.hierarchyPath.Trim('/').Split('/');
                GameObject root = null;
                foreach (var r in scene.GetRootGameObjects())
                    if (r.name == parts[0])
                    {
                        root = r;
                        break;
                    }

                if (root != null)
                    go = FindGameObjectByHierarchyPath(root, info.hierarchyPath);
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.assetPath);
                if (prefab != null)
                    go = FindGameObjectByHierarchyPath(prefab, info.hierarchyPath);
            }

            if (go != null)
            {
                var before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (before > 0)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    removed += before;
                }
            }
        }

        // Возвращаемся к предыдущей сцене
        if (!string.IsNullOrEmpty(previousScenePath))
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        Debug.Log($"[FindAndRemoveMissingScriptsWindow] Removed {removed} missing scripts from all found objects.");
        status = $"Removed {removed} missing scripts.";
        objectsWithMissing.Clear();
        Repaint();
    }

    private void RemoveMissingScriptFromObject(MissingScriptInfo info)
    {
        var previousScenePath = initialScenePath ?? SceneManager.GetActiveScene().path;
        GameObject go = null;
        if (info.isScene)
        {
            var scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Single);
            var parts = info.hierarchyPath.Trim('/').Split('/');
            GameObject root = null;
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == parts[0])
                {
                    root = r;
                    break;
                }

            if (root != null)
                go = FindGameObjectByHierarchyPath(root, info.hierarchyPath);
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.assetPath);
            if (prefab != null)
                go = FindGameObjectByHierarchyPath(prefab, info.hierarchyPath);
        }

        if (go != null)
        {
            var before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (before > 0)
            {
                Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                Debug.Log(
                    $"[FindAndRemoveMissingScriptsWindow] Removed {before} missing scripts from {info.assetPath}: {info.hierarchyPath}");
            }
        }

        // Возвращаемся к предыдущей сцене
        if (!string.IsNullOrEmpty(previousScenePath))
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        // Удаляем из списка
        objectsWithMissing.Remove(info);
        Repaint();
    }

    private class MissingScriptInfo
    {
        public string assetPath; // scene or prefab path
        public string hierarchyPath; // e.g. /Canvas/Button
        public bool isScene; // true = scene, false = prefab
        public string objectName;
    }
}