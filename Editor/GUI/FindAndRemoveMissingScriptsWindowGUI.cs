using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Windows
{
    /// <summary>
    ///     GUI drawing for the find-and-remove Missing Scripts window.
    /// </summary>
    public class FindAndRemoveMissingScriptsWindowGUI : EditorWindowGUI
    {
        private readonly List<MissingScriptInfo> _objectsWithMissing = new();
        private int _currentPrefab;
        private int _currentScene;
        private string[] _prefabPaths;
        private string[] _scenePaths;
        private Vector2 _scroll;
        private int _selectedIndex = -1;
        private string _status = "";

        internal int ResultCount => _objectsWithMissing.Count;
        internal bool IsSearching { get; private set; }

        /// <summary>
        ///     Draws the window GUI.
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
                BeginSearch(FindAssetPaths("t:Scene"), FindAssetPaths("t:Prefab"));
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Label(_status);

            if (_objectsWithMissing.Count > 0)
            {
                GUILayout.Label(GetFoundLabel());
                MissingScriptInfo pendingRemoval = null;
                _scroll = GUILayout.BeginScrollView(_scroll, ListHeightOption);
                try
                {
                    for (int i = 0; i < _objectsWithMissing.Count; i++)
                    {
                        MissingScriptInfo info = _objectsWithMissing[i];
                        info.displayLabel ??= $"{info.assetPath}: {info.hierarchyPath}";
                        GUIStyle style = i == _selectedIndex ? SelectedRowStyle : RowStyle;
                        EditorGUILayout.BeginHorizontal();
                        try
                        {
                            if (GUILayout.Button(info.displayLabel, style))
                            {
                                _selectedIndex = i;
                                SelectAndPing(info, true);
                            }

                            if (GUILayout.Button("Remove", RemoveButtonOption))
                            {
                                pendingRemoval = info;
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

                // WHY: removing inside the loop shortened the list mid-event, so the rows after it read the layout
                // entries of their neighbours; the removal now runs after the whole list has been drawn.
                if (pendingRemoval != null)
                {
                    RemoveMissingScriptFromObject(pendingRemoval);
                    _selectedIndex = -1;
                    window.Repaint();
                }
            }
        }

        private static readonly GUILayoutOption[] ListHeightOption = { GUILayout.Height(350) };
        private static readonly GUILayoutOption[] RemoveButtonOption = { GUILayout.Width(70) };
        private static GUIStyle s_rowStyle;
        private static GUIStyle s_selectedRowStyle;
        private static bool s_rowStylesDark;
        private int _foundLabelCount = -1;
        private string _foundLabel;

        private string GetFoundLabel()
        {
            if (_foundLabelCount != _objectsWithMissing.Count)
            {
                _foundLabelCount = _objectsWithMissing.Count;
                _foundLabel = $"Found {_foundLabelCount} objects with Missing Scripts:";
            }

            return _foundLabel;
        }

        // WHY: one style per row per event used to be allocated; pure green/red also read poorly on the light skin.
        private static GUIStyle RowStyle
        {
            get
            {
                EnsureRowStyles();
                return s_rowStyle;
            }
        }

        private static GUIStyle SelectedRowStyle
        {
            get
            {
                EnsureRowStyles();
                return s_selectedRowStyle;
            }
        }

        private static void EnsureRowStyles()
        {
            bool dark = EditorGUIUtility.isProSkin;
            if (s_rowStyle != null && s_rowStylesDark == dark)
            {
                return;
            }

            s_rowStylesDark = dark;
            s_rowStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = dark ? new Color(1f, 0.50f, 0.48f, 1f) : new Color(0.68f, 0.12f, 0.10f, 1f) }
            };
            s_selectedRowStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = dark ? new Color(0.45f, 0.90f, 0.55f, 1f) : new Color(0.08f, 0.45f, 0.18f, 1f) }
            };
        }

        /// <summary>
        ///     Unsubscribes from events when the window is disabled.
        /// </summary>
        public void OnDisable()
        {
            EditorApplication.update -= SearchAll;
            IsSearching = false;
            EditorUtility.ClearProgressBar();
        }

        private static string[] FindAssetPaths(string filter)
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            return paths;
        }

        internal void BeginSearch(string[] scenePaths, string[] prefabPaths)
        {
            OnDisable();
            _objectsWithMissing.Clear();
            _selectedIndex = -1;
            _scenePaths = scenePaths ?? Array.Empty<string>();
            _prefabPaths = prefabPaths ?? Array.Empty<string>();
            _currentScene = 0;
            _currentPrefab = 0;
            _status = "Searching...";
            IsSearching = true;
            EditorApplication.update += SearchAll;
        }

        private void SearchCurrentSceneOnly()
        {
            OnDisable();
            _objectsWithMissing.Clear();
            _selectedIndex = -1;
            _status = "Searching current scene...";

            Scene currentScene = SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(currentScene.path))
            {
                _status = "Scene is not saved. Please save the scene first.";
                EditorWindow.focusedWindow?.Repaint();
                return;
            }

            foreach (GameObject go in currentScene.GetRootGameObjects())
            {
                FindMissingInHierarchy(go, currentScene.path, true, "/", new List<int>());
            }

            _status = _objectsWithMissing.Count > 0
                ? $"Found {_objectsWithMissing.Count} objects with Missing Scripts in current scene."
                : "No Missing Scripts found in current scene.";

            EditorWindow.focusedWindow?.Repaint();
        }

        internal void SearchAll()
        {
            if (!IsSearching)
            {
                return;
            }

            try
            {
                if (_currentScene < _scenePaths.Length)
                {
                    string path = _scenePaths[_currentScene++];
                    _status = $"Scanning scene: {path}";
                    ScanSceneAsset(path);
                    EditorUtility.DisplayProgressBar("Searching Scenes", path,
                        (float)_currentScene / _scenePaths.Length);
                }
                else if (_currentPrefab < _prefabPaths.Length)
                {
                    string path = _prefabPaths[_currentPrefab++];
                    _status = $"Scanning prefab: {path}";
                    ScanPrefabAsset(path);
                    EditorUtility.DisplayProgressBar("Searching Prefabs", path,
                        (float)_currentPrefab / _prefabPaths.Length);
                }
                else
                {
                    OnDisable();
                    _status = $"Found {_objectsWithMissing.Count} objects with Missing Scripts.";
                }
            }
            catch (Exception exception)
            {
                OnDisable();
                _status = $"Search failed: {exception.Message}";
                Debug.LogError($"[FindAndRemoveMissingScriptsWindow] {_status}");
            }

            EditorWindow.focusedWindow?.Repaint();
        }

        internal void ScanSceneAsset(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool preview = !scene.IsValid() || !scene.isLoaded;
            if (preview)
            {
                scene = EditorSceneManager.OpenPreviewScene(path);
            }

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    FindMissingInHierarchy(root, path, true, "/", new List<int>());
                }
            }
            finally
            {
                if (preview)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        internal void ScanPrefabAsset(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                FindMissingInHierarchy(prefab, path, false, "/", new List<int>());
            }
        }

        private void FindMissingInHierarchy(GameObject go, string assetPath, bool isScene, string parentPath,
            List<int> indices)
        {
            string thisPath = parentPath + go.name;
            indices.Add(go.transform.GetSiblingIndex());
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            {
                _objectsWithMissing.Add(new MissingScriptInfo
                {
                    assetPath = assetPath,
                    hierarchyPath = thisPath,
                    hierarchyIndices = indices.ToArray(),
                    objectName = go.name,
                    isScene = isScene
                });
            }

            foreach (Transform child in go.transform)
            {
                FindMissingInHierarchy(child.gameObject, assetPath, isScene, thisPath + "/", indices);
            }

            indices.RemoveAt(indices.Count - 1);
        }

        private static GameObject FindObject(GameObject root, MissingScriptInfo info)
        {
            Transform current = root.transform;
            for (int i = 1; i < info.hierarchyIndices.Length; i++)
            {
                int index = info.hierarchyIndices[i];
                if (index < 0 || index >= current.childCount)
                {
                    return null;
                }

                current = current.GetChild(index);
            }

            return current.name == info.objectName ? current.gameObject : null;
        }

        private static GameObject FindObject(Scene scene, MissingScriptInfo info)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.transform.GetSiblingIndex() == info.hierarchyIndices[0])
                {
                    return FindObject(root, info);
                }
            }

            return null;
        }

        private void SelectAndPing(MissingScriptInfo info, bool forceOpenScene = false)
        {
            GameObject found;
            if (info.isScene)
            {
                Scene scene = SceneManager.GetSceneByPath(info.assetPath);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Additive);
                }

                found = FindObject(scene, info);
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.assetPath);
                found = prefab != null ? FindObject(prefab, info) : null;
            }

            if (found != null)
            {
                Selection.activeObject = found;
                EditorGUIUtility.PingObject(found);
            }
            else
            {
                _status = "Object no longer matches the search result. Scan again.";
            }
        }

        internal int RemoveAllMissingScripts()
        {
            OnDisable();
            int removed = 0;
            foreach (MissingScriptInfo info in _objectsWithMissing.ToArray())
            {
                removed += RemoveMissingScriptFromObject(info);
            }

            _status = $"Removed {removed} missing scripts. Save open scenes to persist their changes.";
            Debug.Log($"[FindAndRemoveMissingScriptsWindow] {_status}");
            EditorWindow.focusedWindow?.Repaint();
            return removed;
        }

        private int RemoveMissingScriptFromObject(MissingScriptInfo info)
        {
            OnDisable();
            try
            {
                int removed = info.isScene ? RepairSceneObject(info) : RepairPrefabObject(info);
                _objectsWithMissing.Remove(info);
                _status = $"Removed {removed} missing scripts. Save open scenes to persist their changes.";
                return removed;
            }
            catch (Exception exception)
            {
                _status = $"Repair failed for {info.assetPath}: {exception.Message}";
                Debug.LogError($"[FindAndRemoveMissingScriptsWindow] {_status}");
                return 0;
            }
        }

        private static int RepairSceneObject(MissingScriptInfo info)
        {
            Scene scene = SceneManager.GetSceneByPath(info.assetPath);
            bool openedForRepair = !scene.IsValid() || !scene.isLoaded;
            if (openedForRepair)
            {
                scene = EditorSceneManager.OpenScene(info.assetPath, OpenSceneMode.Additive);
            }

            bool closeScene = openedForRepair;
            try
            {
                GameObject go = FindObject(scene, info);
                if (go == null)
                {
                    throw new InvalidOperationException("Object no longer matches the search result. Scan again.");
                }

                int removed = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (removed == 0)
                {
                    return 0;
                }

                if (!openedForRepair)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                EditorSceneManager.MarkSceneDirty(scene);
                if (openedForRepair)
                {
                    closeScene = false;
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException("Could not save the repaired scene; it remains open.");
                    }

                    closeScene = true;
                }

                return removed;
            }
            finally
            {
                if (closeScene)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static int RepairPrefabObject(MissingScriptInfo info)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(info.assetPath);
            try
            {
                GameObject go = FindObject(root, info);
                if (go == null)
                {
                    throw new InvalidOperationException("Object no longer matches the search result. Scan again.");
                }

                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, info.assetPath, out bool saved);
                    if (!saved)
                    {
                        throw new InvalidOperationException("Could not save the repaired prefab.");
                    }
                }

                return removed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private class MissingScriptInfo
        {
            public string assetPath;
            public string displayLabel;
            public string hierarchyPath;
            public int[] hierarchyIndices;
            public bool isScene;
            public string objectName;
        }
    }
}
