using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Windows
{
    /// <summary>
    /// GUI отрисовка для окна поиска и удаления Missing Scripts
    /// </summary>
    public class FindAndRemoveMissingScriptsWindowGUI : EditorWindowGUI
    {
        private readonly List<MissingScriptInfo> _objectsWithMissing = new();
        private int _currentPrefab;
        private int _currentScene;
        private string _initialScenePath;
        private string[] _prefabPaths;
        private string[] _scenePaths;
        private Vector2 _scroll;
        private int _searchStep;
        private int _selectedIndex = -1;
        private string _status = "";

        /// <summary>
        /// Отрисовка GUI
        /// </summary>
        public override void OnGUI(EditorWindow window)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Find in Current Scene Only"))
            {
                SearchCurrentSceneOnly();
            }
            
            if (GUILayout.Button("Find in All Scenes & Prefabs"))
            {
                _objectsWithMissing.Clear();
                _status = "Searching...";
                _initialScenePath = SceneManager.GetActiveScene().path;
                EditorApplication.update += SearchAll;
            }
            
            EditorGUILayout.EndHorizontal();

            GUILayout.Label(_status);

            if (_objectsWithMissing.Count > 0)
            {
                GUILayout.Label($"Found {_objectsWithMissing.Count} objects with Missing Scripts:");
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(350));
                try
                {
                    for (int i = 0; i < _objectsWithMissing.Count; i++)
                    {
                        MissingScriptInfo info = _objectsWithMissing[i];
                        GUIStyle style = new(EditorStyles.label);
                        style.normal.textColor = i == _selectedIndex ? Color.green : Color.red;
                        EditorGUILayout.BeginHorizontal();
                        try
                        {
                            if (GUILayout.Button($"{info.assetPath}: {info.hierarchyPath}", style))
                            {
                                _selectedIndex = i;
                                SelectAndPing(info, true);
                            }

                            if (GUILayout.Button("X", GUILayout.Width(70)))
                            {
                                RemoveMissingScriptFromObject(info);
                            }
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

                if (GUILayout.Button("Remove All Missing Scripts"))
                {
                    RemoveAllMissingScripts();
                }
            }
        }

        /// <summary>
        /// Отписка от событий при закрытии
        /// </summary>
        public void OnDisable()
        {
            EditorApplication.update -= SearchAll;
        }

        /// <summary>
        /// Поиск Missing Scripts только в текущей сцене
        /// </summary>
        private void SearchCurrentSceneOnly()
        {
            _objectsWithMissing.Clear();
            _status = "Searching current scene...";
            _initialScenePath = SceneManager.GetActiveScene().path;
            
            Scene currentScene = SceneManager.GetActiveScene();
            
            if (string.IsNullOrEmpty(currentScene.path))
            {
                _status = "Scene is not saved. Please save the scene first.";
                EditorWindow.focusedWindow?.Repaint();
                return;
            }

            foreach (GameObject go in currentScene.GetRootGameObjects())
            {
                FindMissingInHierarchy(go, currentScene.path, true, "/");
            }

            _status = _objectsWithMissing.Count > 0 
                ? $"Found {_objectsWithMissing.Count} objects with Missing Scripts in current scene." 
                : "No Missing Scripts found in current scene.";
            
            EditorWindow.focusedWindow?.Repaint();
        }

        private void SearchAll()
        {
            if (_searchStep == 0)
            {
                _scenePaths = AssetDatabase.FindAssets("t:Scene") ?? new string[0];
                _prefabPaths = AssetDatabase.FindAssets("t:Prefab") ?? new string[0];
                _currentScene = 0;
                _currentPrefab = 0;
                _searchStep = 1;
                _status = "Searching scenes...";
            }
            else if (_searchStep == 1)
            {
                if (_currentScene < _scenePaths.Length)
                {
                    string sceneAssetPath = AssetDatabase.GUIDToAssetPath(_scenePaths[_currentScene]);
                    _status = $"Scanning scene: {sceneAssetPath}";
                    Scene scene = EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Single);
                    foreach (GameObject go in scene.GetRootGameObjects())
                    {
                        FindMissingInHierarchy(go, sceneAssetPath, true, "/");
                    }

                    _currentScene++;
                    EditorUtility.DisplayProgressBar("Searching Scenes", sceneAssetPath,
                        (float)_currentScene / _scenePaths.Length);
                }
                else
                {
                    _searchStep = 2;
                    _status = "Searching prefabs...";
                    EditorUtility.ClearProgressBar();
                }
            }
            else if (_searchStep == 2)
            {
                if (_currentPrefab < _prefabPaths.Length)
                {
                    string prefabAssetPath = AssetDatabase.GUIDToAssetPath(_prefabPaths[_currentPrefab]);
                    _status = $"Scanning prefab: {prefabAssetPath}";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                    if (prefab != null)
                    {
                        FindMissingInHierarchy(prefab, prefabAssetPath, false, "/");
                    }

                    _currentPrefab++;
                    EditorUtility.DisplayProgressBar("Searching Prefabs", prefabAssetPath,
                        (float)_currentPrefab / _prefabPaths.Length);
                }
                else
                {
                    _searchStep = 3;
                    _status = $"Found {_objectsWithMissing.Count} objects with Missing Scripts.";
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update -= SearchAll;
                    if (!string.IsNullOrEmpty(_initialScenePath) && _initialScenePath != SceneManager.GetActiveScene().path)
                    {
                        EditorSceneManager.OpenScene(_initialScenePath, OpenSceneMode.Single);
                    }

                    EditorWindow.focusedWindow?.Repaint();
                }
            }
        }

        private void FindMissingInHierarchy(GameObject go, string assetPath, bool isScene, string parentPath)
        {
            string thisPath = parentPath + go.name;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            {
                _objectsWithMissing.Add(new MissingScriptInfo
                {
                    assetPath = assetPath,
                    hierarchyPath = thisPath,
                    objectName = go.name,
                    isScene = isScene
                });
            }

            foreach (Transform child in go.transform)
            {
                FindMissingInHierarchy(child.gameObject, assetPath, isScene, thisPath + "/");
            }
        }

        private void SelectAndPing(MissingScriptInfo info, bool forceOpenScene = false)
        {
            if (info.isScene)
            {
                Scene scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Single);
                string[] parts = info.hierarchyPath.Trim('/').Split('/');
                GameObject root = null;
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    if (go.name == parts[0])
                    {
                        root = go;
                        break;
                    }
                }

                if (root != null)
                {
                    GameObject found = FindGameObjectByHierarchyPath(root, info.hierarchyPath);
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
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.assetPath);
                if (prefab != null)
                {
                    GameObject found = FindGameObjectByHierarchyPath(prefab, info.hierarchyPath);
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
            string[] parts = path.Trim('/').Split('/');
            GameObject current = root;
            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null)
                {
                    return null;
                }

                current = child.gameObject;
            }

            return current;
        }

        private void RemoveAllMissingScripts()
        {
            int removed = 0;
            string previousScenePath = _initialScenePath ?? SceneManager.GetActiveScene().path;
            foreach (MissingScriptInfo info in _objectsWithMissing)
            {
                GameObject go = null;
                if (info.isScene)
                {
                    Scene scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Single);
                    string[] parts = info.hierarchyPath.Trim('/').Split('/');
                    GameObject root = null;
                    foreach (GameObject r in scene.GetRootGameObjects())
                    {
                        if (r.name == parts[0])
                        {
                            root = r;
                            break;
                        }
                    }

                    if (root != null)
                    {
                        go = FindGameObjectByHierarchyPath(root, info.hierarchyPath);
                    }
                }
                else
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.assetPath);
                    if (prefab != null)
                    {
                        go = FindGameObjectByHierarchyPath(prefab, info.hierarchyPath);
                    }
                }

                if (go != null)
                {
                    int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (before > 0)
                    {
                        Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        removed += before;
                    }
                }
            }

            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[FindAndRemoveMissingScriptsWindow] Removed {removed} missing scripts from all found objects.");
            _status = $"Removed {removed} missing scripts.";
            _objectsWithMissing.Clear();
            EditorWindow.focusedWindow?.Repaint();
        }

        private void RemoveMissingScriptFromObject(MissingScriptInfo info)
        {
            string previousScenePath = _initialScenePath ?? SceneManager.GetActiveScene().path;
            GameObject go = null;
            if (info.isScene)
            {
                Scene scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Single);
                string[] parts = info.hierarchyPath.Trim('/').Split('/');
                GameObject root = null;
                foreach (GameObject r in scene.GetRootGameObjects())
                {
                    if (r.name == parts[0])
                    {
                        root = r;
                        break;
                    }
                }

                if (root != null)
                {
                    go = FindGameObjectByHierarchyPath(root, info.hierarchyPath);
                }
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.assetPath);
                if (prefab != null)
                {
                    go = FindGameObjectByHierarchyPath(prefab, info.hierarchyPath);
                }
            }

            if (go != null)
            {
                int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (before > 0)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    Debug.Log(
                        $"[FindAndRemoveMissingScriptsWindow] Removed {before} missing scripts from {info.assetPath}: {info.hierarchyPath}");
                }
            }

            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            _objectsWithMissing.Remove(info);
            EditorWindow.focusedWindow?.Repaint();
        }

        private class MissingScriptInfo
        {
            public string assetPath;
            public string hierarchyPath;
            public bool isScene;
            public string objectName;
        }
    }
}

