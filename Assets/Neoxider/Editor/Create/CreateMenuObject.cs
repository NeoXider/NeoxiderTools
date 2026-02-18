using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo
{
    public class CreateMenuObject
    {
        private static string _startPath;

        /// <summary>
        ///     Динамически определяет путь к корневой папке Neoxider, работая как при установке через Git, так и как обычный пакет
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
                    if (path.Contains("Neoxider") && path.Contains("Editor/Create"))
                    {
                        scriptPath = path;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(scriptPath))
                {
                    // Определяем базовый путь на основе расположения скрипта
                    // Убираем "Editor/Create" из пути
                    string basePath = Path.GetDirectoryName(scriptPath); // Editor/Create
                    basePath = Path.GetDirectoryName(basePath); // Editor
                    basePath = Path.GetDirectoryName(basePath); // Neoxider
                    _startPath = basePath + "/";
                }
                else
                {
                    // Fallback - пробуем стандартные пути
                    string assetsPath = "Assets/Neoxider/";
                    string packagesPath = "Packages/com.neoxider.tools/";

                    // Проверяем, существует ли папка Packages
                    if (AssetDatabase.IsValidFolder("Packages/com.neoxider.tools"))
                    {
                        _startPath = packagesPath;
                    }
                    else if (AssetDatabase.IsValidFolder("Assets/Neoxider"))
                    {
                        _startPath = assetsPath;
                    }
                    else
                    {
                        // Последняя попытка - ищем любой префаб Neoxider
                        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
                        foreach (string guid in prefabGuids)
                        {
                            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (prefabPath.Contains("Neoxider"))
                            {
                                // Находим корень Neoxider
                                int neoxiderIndex = prefabPath.IndexOf("Neoxider");
                                _startPath = prefabPath.Substring(0, neoxiderIndex + "Neoxider".Length) + "/";
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(_startPath))
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
            T prefab = GetResources<T>(path);
            if (prefab == null)
            {
                return Create<T>();
            }

            GameObject parentObject = Selection.activeGameObject;
            T component = Object.Instantiate(prefab, parentObject?.transform);
            component.name = typeof(T).Name;
            Selection.activeGameObject = component.gameObject;
            return component;
        }

        public static T GetResources<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(startPath + path);
        }

        /// <summary>
        ///     Создаёт объект с компонентом типа <paramref name="componentType" />. Если задан <paramref name="prefabPath" /> и префаб найден — инстанциирует префаб, иначе создаёт пустой GameObject и добавляет компонент.
        /// </summary>
        public static MonoBehaviour Create(Type componentType, string prefabPath)
        {
            if (componentType == null || !typeof(MonoBehaviour).IsAssignableFrom(componentType))
            {
                return null;
            }

            MethodInfo createNoArg = typeof(CreateMenuObject).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Create" && m.IsGenericMethod && m.GetParameters().Length == 0);
            MethodInfo createWithPath = typeof(CreateMenuObject).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Create" && m.IsGenericMethod && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));

            if (string.IsNullOrEmpty(prefabPath))
            {
                if (createNoArg == null) return null;
                object result = createNoArg.MakeGenericMethod(componentType).Invoke(null, null);
                return result as MonoBehaviour;
            }

            if (createWithPath == null) return null;
            object created = createWithPath.MakeGenericMethod(componentType).Invoke(null, new object[] { prefabPath });
            return created as MonoBehaviour;
        }

        #region Dynamic Menu

        [MenuItem("GameObject/Neoxider/Create Neoxider Object...", false, 0)]
        private static void ShowCreateNeoxiderMenu()
        {
            GenericMenu menu = new GenericMenu();
            Type[] types = GetTypesWithCreateFromMenu();
            foreach (Type type in types)
            {
                CreateFromMenuAttribute attr = type.GetCustomAttribute<CreateFromMenuAttribute>(false);
                if (attr == null)
                {
                    continue;
                }

                string label = attr.MenuPath.Replace("Neoxider/", "");
                string prefabPath = attr.PrefabPath;
                menu.AddItem(new GUIContent(label), false, () => Create(type, prefabPath));
            }

            menu.ShowAsContext();
        }

        private static Type[] GetTypesWithCreateFromMenu()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !a.ReflectionOnly)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetExportedTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => t != null && t.IsClass && !t.IsAbstract && typeof(MonoBehaviour).IsAssignableFrom(t) &&
                            t.GetCustomAttribute<CreateFromMenuAttribute>(false) != null)
                .OrderBy(t =>
                {
                    CreateFromMenuAttribute a = t.GetCustomAttribute<CreateFromMenuAttribute>(false);
                    return a?.MenuPath ?? t.FullName ?? "";
                })
                .ToArray();
        }

        #endregion
    }
}