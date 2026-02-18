using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    namespace UI
    {
        public class CreateMenuObject
        {
            public const string createPatch = "GameObject/UI/Neoxider/";

            private static string _startPath;

            /// <summary>
            ///     Dynamically resolves the prefabs folder path, whether the project is in Assets or installed as a package.
            /// </summary>
            public static string startPath
            {
                get
                {
                    if (!string.IsNullOrEmpty(_startPath))
                    {
                        return _startPath;
                    }

                    // Ищем путь к скрипту через поиск по имени
                    string[] guids = AssetDatabase.FindAssets("CreateMenuObject t:Script");
                    string scriptPath = null;

                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (path.Contains("UI Extension") && path.Contains("Editor"))
                        {
                            scriptPath = path;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(scriptPath))
                    {
                        string[] prefabGuids = AssetDatabase.FindAssets("Canvas LandScape t:Prefab");
                        foreach (string guid in prefabGuids)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            if (path.Contains("UI Extension"))
                            {
                                scriptPath = path;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(scriptPath))
                        {
                            string prefabsDir = Path.GetDirectoryName(scriptPath);
                            if (prefabsDir != null && (prefabsDir.EndsWith("Canvas") || prefabsDir.EndsWith("Layout")))
                            {
                                prefabsDir = Path.GetDirectoryName(prefabsDir);
                            }

                            _startPath = prefabsDir + "/";
                            scriptPath = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        if (scriptPath.EndsWith("/"))
                        {
                            _startPath = scriptPath;
                        }
                        else
                        {
                            string basePath = Path.GetDirectoryName(scriptPath);
                            basePath = basePath?.Replace("\\Editor", "").Replace("/Editor", "");
                            _startPath = basePath + "/Prefabs/";
                        }
                    }
                    else
                    {
                        // Fallback - пробуем стандартные пути
                        string assetsPath = "Assets/Neoxider/UI Extension/Prefabs/";
                        string packagesPath = "Packages/com.neoxider.tools/UI Extension/Prefabs/";

                        // Проверяем, существует ли папка Packages
                        if (AssetDatabase.IsValidFolder("Packages/com.neoxider.tools"))
                        {
                            _startPath = packagesPath;
                        }
                        else if (AssetDatabase.IsValidFolder("Assets/Neoxider/UI Extension/Prefabs"))
                        {
                            _startPath = assetsPath;
                        }
                        else
                        {
                            // Последняя попытка - ищем папку Prefabs
                            string[] prefabGuids = AssetDatabase.FindAssets("Canvas LandScape t:Prefab");
                            if (prefabGuids.Length > 0)
                            {
                                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                                _startPath = Path.GetDirectoryName(prefabPath) + "/";
                            }
                            else
                            {
                                _startPath = assetsPath; // Fallback на стандартный путь
                            }
                        }
                    }

                    return _startPath;
                }
            }

            public static T Create<T>() where T : MonoBehaviour
            {
                GameObject parentObject = Selection.activeGameObject;
                GameObject myObject = new(typeof(T).Name);
                myObject.transform.SetParent(parentObject?.transform);
                T component = myObject.AddComponent<T>();
                Selection.activeGameObject = myObject;
                return component;
            }

            public static T Create<T>(string path) where T : MonoBehaviour
            {
                GameObject parentObject = Selection.activeGameObject;
                T component = GameObject.Instantiate(GetResources<T>(path), parentObject?.transform);
                component.name = typeof(T).Name;
                Selection.activeGameObject = component.gameObject;
                return component;
            }

            public static GameObject Create(string path)
            {
                GameObject prefab = GetResources<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Neoxider UI] Prefab not found: {startPath}{path}");
                    return null;
                }

                Transform parent = Selection.activeGameObject != null ? Selection.activeGameObject.transform : null;
                GameObject obj = GameObject.Instantiate(prefab, parent);
                obj.name = prefab.name;
                Selection.activeGameObject = obj;
                return obj;
            }

            private static GameObject CreatePrefab(string name, string subfolder)
            {
                string relativePath = string.IsNullOrEmpty(subfolder)
                    ? name + ".prefab"
                    : subfolder + "/" + name + ".prefab";
                GameObject obj = Create(relativePath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                }

                return obj;
            }

            public static T GetResources<T>(string path) where T : Object
            {
                return AssetDatabase.LoadAssetAtPath<T>(startPath + path);
            }

            #region MenuItem

            [MenuItem(createPatch + "Canvas LandScape", false, 1)]
            private static void CanvasLandScape()
            {
                string nameGameObject = "Canvas LandScape";
                CreatePrefab(nameGameObject, "Canvas");
            }

            [MenuItem(createPatch + "Canvas Portait", false, 1)]
            private static void CanvasPortait()
            {
                string nameGameObject = "Canvas Portait";
                CreatePrefab(nameGameObject, "Canvas");
            }

            [MenuItem(createPatch + "Horizontal Layout", false, 1)]
            private static void HorizontalLayout()
            {
                string nameGameObject = "Horizontal Layout";
                CreatePrefab(nameGameObject, "Layout");
            }

            [MenuItem(createPatch + "Vertical Layout", false, 1)]
            private static void VerticalLayout()
            {
                string nameGameObject = "Vertical Layout";
                CreatePrefab(nameGameObject, "Layout");
            }

            [MenuItem(createPatch + "ScrollRect", false, 1)]
            private static void ScrollRect()
            {
                string nameGameObject = "ScrollRect";
                CreatePrefab(nameGameObject, "");
            }

            [MenuItem(createPatch + "Money Layout", false, 1)]
            private static void MoneyLayout()
            {
                string nameGameObject = "Money Layout";
                CreatePrefab(nameGameObject, "");
            }

            [MenuItem(createPatch + "Page", false, 1)]
            private static void Page()
            {
                string nameGameObject = "Page";
                CreatePrefab(nameGameObject, "");
            }

            #endregion
        }
    }
}