using System;
using System.Collections.Generic;
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
        ///     Dynamically resolves the root path of Neoxider, whether installed via Git or as a package.
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
        ///     Creates an object with a component of type <paramref name="componentType" />. If <paramref name="prefabPath" /> is
        ///     set
        ///     and the prefab is found, instantiates it; otherwise creates an empty GameObject and adds the component.
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
                .FirstOrDefault(m =>
                    m.Name == "Create" && m.IsGenericMethod && m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(string));

            if (string.IsNullOrEmpty(prefabPath))
            {
                if (createNoArg == null)
                {
                    return null;
                }

                object result = createNoArg.MakeGenericMethod(componentType).Invoke(null, null);
                return result as MonoBehaviour;
            }

            if (createWithPath == null)
            {
                return null;
            }

            object created = createWithPath.MakeGenericMethod(componentType).Invoke(null, new object[] { prefabPath });
            return created as MonoBehaviour;
        }

        #region Dynamic Menu

        private const string CreateFromMenuAttributeName = "CreateFromMenuAttribute";

        internal readonly struct CreateMenuEntry
        {
            public readonly Type ComponentType;
            public readonly string MenuPath;
            public readonly string PrefabPath;

            public CreateMenuEntry(Type componentType, string menuPath, string prefabPath)
            {
                ComponentType = componentType;
                MenuPath = menuPath;
                PrefabPath = prefabPath;
            }
        }

        /// <summary>Used when opening the window from Window → Neoxider (dockable window).</summary>
        internal static CreateMenuEntry[] BuildCreateMenuEntriesForWindow()
        {
            return BuildCreateMenuEntries();
        }

        [MenuItem("GameObject/Neoxider/Create Neoxider Object...", false, 0)]
        private static void ShowCreateNeoxiderMenu()
        {
            CreateMenuEntry[] entries = BuildCreateMenuEntries();
            CreateNeoxiderObjectWindow.ShowWindow(entries);

            if (entries.Length == 0)
            {
                int monoCount = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Count();
                Debug.LogWarning(
                    $"CreateMenuObject: no types with [CreateFromMenu] found. MonoBehaviours in TypeCache: {monoCount}. If 0, try reimporting Neoxider scripts or restarting Unity.");
            }
        }

        /// <summary>
        ///     Collects types with [CreateFromMenu]. Looks up by attribute name via reflection so it does not depend on
        ///     the assembly containing the attribute being loaded.
        /// </summary>
        private static Type[] GetTypesWithCreateFromMenu()
        {
            static bool HasCreateFromMenuAttribute(Type t)
            {
                if (t == null || !t.IsClass || t.IsAbstract)
                {
                    return false;
                }

                foreach (object a in t.GetCustomAttributes(false))
                {
                    if (a != null && a.GetType().Name == CreateFromMenuAttributeName)
                    {
                        return true;
                    }
                }

                return false;
            }

            static string GetMenuPath(Type t)
            {
                foreach (object a in t.GetCustomAttributes(false))
                {
                    if (a != null && a.GetType().Name == CreateFromMenuAttributeName)
                    {
                        try
                        {
                            PropertyInfo p = a.GetType().GetProperty("MenuPath");
                            return p?.GetValue(a) as string ?? t.FullName ?? "";
                        }
                        catch
                        {
                            return t.FullName ?? "";
                        }
                    }
                }

                return t.FullName ?? "";
            }

            Type[] types = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .Where(HasCreateFromMenuAttribute)
                .OrderBy(GetMenuPath)
                .ToArray();

            if (types.Length == 0)
            {
                types = AppDomain.CurrentDomain.GetAssemblies()
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
                                HasCreateFromMenuAttribute(t))
                    .OrderBy(GetMenuPath)
                    .ToArray();
            }

            return types;
        }

        private static CreateMenuEntry[] BuildCreateMenuEntries()
        {
            return GetTypesWithCreateFromMenu()
                .Select(t => new CreateMenuEntry(t, GetMenuPathForType(t), GetPrefabPathForType(t)))
                .Where(e => !string.IsNullOrEmpty(e.MenuPath))
                .OrderBy(e => e.MenuPath)
                .ToArray();
        }

        private static string GetMenuPathForType(Type t)
        {
            foreach (object a in t.GetCustomAttributes(false))
            {
                if (a != null && a.GetType().Name == CreateFromMenuAttributeName)
                {
                    try
                    {
                        PropertyInfo p = a.GetType().GetProperty("MenuPath");
                        return p?.GetValue(a) as string ?? "";
                    }
                    catch
                    {
                    }
                }
            }

            return "";
        }

        private static string GetPrefabPathForType(Type t)
        {
            foreach (object a in t.GetCustomAttributes(false))
            {
                if (a != null && a.GetType().Name == CreateFromMenuAttributeName)
                {
                    try
                    {
                        PropertyInfo p = a.GetType().GetProperty("PrefabPath");
                        return p?.GetValue(a) as string;
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        #endregion
    }

    internal sealed class CreateNeoxiderObjectWindow : EditorWindow
    {
        private static readonly Color HeaderBg = new(0.22f, 0.35f, 0.52f);
        private static readonly Color FolderBg = new(0.28f, 0.28f, 0.32f, 0.5f);
        private static readonly Color EntryBg = new(0.25f, 0.38f, 0.55f, 0.25f);
        private static readonly Color EntryBgLight = new(0.45f, 0.65f, 0.9f, 0.2f);

        /// <summary>
        ///     Meaningful color palette by category: UI/looks — purple, audio — pink, control/conditions — orange, data —
        ///     teal, mechanics — green, game/levels — blue, etc.
        /// </summary>
        private static readonly Dictionary<string, Color> CategoryColors = new(StringComparer.OrdinalIgnoreCase)
        {
            { "UI", new Color(0.6f, 0.4f, 1f, 0.9f) }, // Внешний вид / интерфейс — фиолетовый
            { "Tools", new Color(0.35f, 0.75f, 0.35f, 0.9f) }, // Инструменты, механика — зелёный (операторы)
            { "Shop", new Color(1f, 0.75f, 0f, 0.9f) }, // Магазин, валюта — жёлтый/золотой
            { "Audio", new Color(0.81f, 0.39f, 0.81f, 0.9f) }, // Звук — розовый/маджента (как в примере)
            { "Bonus", new Color(1f, 0.55f, 0.1f, 0.9f) }, // Награды, активность — оранжевый
            { "Level", new Color(0.3f, 0.59f, 1f, 0.9f) }, // Уровни, игра — синий (движение/игра)
            { "Save", new Color(0.36f, 0.69f, 0.84f, 0.9f) }, // Сохранения, данные — бирюзовый (сенсоры)
            { "Condition", new Color(1f, 0.55f, 0.2f, 0.9f) }, // Условия, логика — оранжевый (управление)
            { "Animations", new Color(0.65f, 0.45f, 0.95f, 0.9f) }, // Анимация, вид — фиолетовый
            { "GridSystem", new Color(0.36f, 0.69f, 0.84f, 0.9f) }, // Сетка, структура — бирюзовый
            { "Parallax", new Color(0.3f, 0.59f, 1f, 0.9f) }, // Параллакс, движение — синий
            { "NPC", new Color(0.36f, 0.69f, 0.84f, 0.9f) } // Персонажи, взаимодействие — бирюзовый
        };

        private static readonly Dictionary<string, string> CategoryIcons = new(StringComparer.OrdinalIgnoreCase)
        {
            { "UI", "d_Canvas Icon" },
            { "Tools", "d_SettingsIcon" },
            { "Shop", "d_PrefabVariant Icon" },
            { "Audio", "AudioSource Icon" },
            { "Bonus", "d_Favorite Icon" },
            { "Level", "d_PlayButton On" },
            { "Save", "Folder Icon" },
            { "Condition", "d_Toggle Icon" },
            { "Animations", "Animation Icon" },
            { "GridSystem", "d_GridLayoutGroup Icon" },
            { "Parallax", "Folder Icon" },
            { "NPC", "Folder Icon" },
            { "Physics", "d_Rigidbody Icon" },
            { "Movement", "d_Transform Icon" },
            { "Spawner", "d_Prefab Icon" },
            { "Components", "d_cs Script Icon" },
            { "Dialogue", "d_TextAsset Icon" },
            { "Input", "d_InputField Icon" },
            { "View", "Folder Icon" },
            { "Debug", "Folder Icon" },
            { "Time", "d_Animation.Play" },
            { "Text", "d_TextAsset Icon" },
            { "Interact", "d_Prefab Icon" },
            { "Random", "d_Animation.FilterBySelection" },
            { "Other", "Folder Icon" },
            { "State Machine", "d_AnimatorStateMachine Icon" },
            { "FakeLeaderboard", "Folder Icon" },
            { "Managers", "Folder Icon" },
            { "Camera", "d_Camera Icon" }
        };

        private readonly Dictionary<string, bool> _expanded = new(StringComparer.OrdinalIgnoreCase);

        private CreateMenuObject.CreateMenuEntry[] _entries = Array.Empty<CreateMenuObject.CreateMenuEntry>();
        private GUIStyle _entryButtonStyle;
        private GUIStyle _folderStyle;
        private GUIStyle _headerStyle;
        private MenuNode _root;
        private Vector2 _scroll;
        private string _search = "";

        private void OnGUI()
        {
            InitStyles();
            DrawHeader();
            DrawSearchBar();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            int shown = DrawTree(_root, 0);
            if (shown == 0)
            {
                EditorGUILayout.HelpBox("Ничего не найдено. Измени поиск или очисти поле.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private static GUIContent GetFolderContent(string name, int depth)
        {
            Texture2D icon = null;
            if (CategoryIcons.TryGetValue(name, out string iconName))
            {
                GUIContent c = EditorGUIUtility.IconContent(iconName);
                if (c?.image != null)
                {
                    icon = c.image as Texture2D;
                }
            }

            if (icon == null)
            {
                GUIContent folder = EditorGUIUtility.IconContent("Folder Icon") ??
                                    EditorGUIUtility.IconContent("d_Folder Icon");
                icon = folder?.image as Texture2D;
            }

            return new GUIContent(" " + name, icon, name);
        }

        public static void ShowWindow(CreateMenuObject.CreateMenuEntry[] entries)
        {
            CreateNeoxiderObjectWindow window = GetWindow<CreateNeoxiderObjectWindow>("Create Neoxider Object");
            window.titleContent = new GUIContent("Create Neoxider Object");
            window._entries = entries ?? Array.Empty<CreateMenuObject.CreateMenuEntry>();
            window.RebuildTree();
            window.minSize = new Vector2(320f, 400f);
            window.Show();
            window.Focus();
        }

        [MenuItem("Window/Neoxider/Create Neoxider Object", false, 1200)]
        private static void OpenFromWindowMenu()
        {
            CreateMenuObject.CreateMenuEntry[] entries = CreateMenuObject.BuildCreateMenuEntriesForWindow();
            ShowWindow(entries);
        }

        private void InitStyles()
        {
            if (_headerStyle != null)
            {
                return;
            }

            bool pro = EditorGUIUtility.isProSkin;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 8, 8, 8),
                normal = { textColor = Color.white }
            };
            _folderStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                padding = new RectOffset(6, 4, 3, 3),
                alignment = TextAnchor.MiddleLeft,
                imagePosition = ImagePosition.ImageLeft
            };
            _entryButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                imagePosition = ImagePosition.ImageLeft,
                padding = new RectOffset(10, 4, 4, 4),
                fixedHeight = 22,
                normal = { textColor = pro ? new Color(0.9f, 0.92f, 0.95f) : new Color(0.15f, 0.2f, 0.3f) },
                hover = { background = MakeTex(2, 2, pro ? EntryBg : EntryBgLight) },
                active = { background = MakeTex(2, 2, pro ? EntryBg : EntryBgLight) }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            Texture2D tex = new(w, h);
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                tex.SetPixel(x, y, col);
            }

            tex.Apply();
            return tex;
        }

        private void RebuildTree()
        {
            _root = new MenuNode { Name = "Neoxider", FullPath = "" };
            foreach (CreateMenuObject.CreateMenuEntry entry in _entries)
            {
                if (string.IsNullOrWhiteSpace(entry.MenuPath))
                {
                    continue;
                }

                string relativePath = entry.MenuPath.Replace("Neoxider/", "");
                string[] parts = relativePath.Split('/');
                if (parts.Length == 0)
                {
                    continue;
                }

                MenuNode current = _root;
                string currentPath = "";
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string part = parts[i];
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                    if (!current.Children.TryGetValue(part, out MenuNode child))
                    {
                        child = new MenuNode { Name = part, FullPath = currentPath };
                        current.Children[part] = child;
                        _expanded.TryAdd(currentPath, i == 0);
                    }

                    current = child;
                }

                current.Entries.Add(entry);
            }
        }

        private void DrawHeader()
        {
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(r, HeaderBg);
            GUILayout.Label("Create Neoxider Object", _headerStyle);
            EditorGUILayout.LabelField($"GameObject → Neoxider → Create   ·   {_entries.Length} компонентов",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Поиск", GUILayout.Width(36));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(_search))
            {
                Color prev = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    _search = "";
                    GUI.FocusControl(null);
                }

                GUI.contentColor = prev;
            }

            GUIContent refreshIcon =
                EditorGUIUtility.IconContent("d_Refresh") ?? EditorGUIUtility.IconContent("Refresh");
            if (refreshIcon != null && refreshIcon.image != null)
            {
                if (GUILayout.Button(refreshIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    RefreshEntries();
                }
            }
            else if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshEntries();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void RefreshEntries()
        {
            _entries = CreateMenuObject.BuildCreateMenuEntriesForWindow();
            RebuildTree();
            Repaint();
        }

        private int DrawTree(MenuNode node, int depth)
        {
            if (node == null)
            {
                return 0;
            }

            int shown = 0;
            string search = _search?.Trim();
            const float indentPerLevel = 14f;

            foreach (MenuNode child in node.Children.Values.OrderBy(c => c.Name))
            {
                if (!HasVisibleContent(child, search))
                {
                    continue;
                }

                bool expanded = _expanded.TryGetValue(child.FullPath, out bool state) && state;
                if (!string.IsNullOrEmpty(search))
                {
                    expanded = true;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(depth * indentPerLevel);
                GUIContent folderContent = GetFolderContent(child.Name, depth);
                Rect folderRect = GUILayoutUtility.GetRect(folderContent, _folderStyle, GUILayout.ExpandWidth(true));
                Color catColor = depth == 0 && CategoryColors.TryGetValue(child.Name, out Color c) ? c : FolderBg;
                EditorGUI.DrawRect(folderRect, catColor);
                bool newExpanded = EditorGUI.Foldout(folderRect, expanded, folderContent, true, _folderStyle);
                EditorGUILayout.EndHorizontal();
                _expanded[child.FullPath] = newExpanded;
                shown++;

                if (newExpanded)
                {
                    shown += DrawTree(child, depth + 1);
                }
            }

            GUIContent scriptIconContent = EditorGUIUtility.IconContent("d_cs Script Icon") ??
                                           EditorGUIUtility.IconContent("cs Script Icon");
            Texture2D scriptIcon = scriptIconContent?.image as Texture2D;
            foreach (CreateMenuObject.CreateMenuEntry entry in node.Entries.OrderBy(e => e.MenuPath))
            {
                if (!EntryMatches(entry, search))
                {
                    continue;
                }

                string label = GetLeafName(entry.MenuPath);
                GUIContent entryContent = scriptIcon != null
                    ? new GUIContent(" " + label, scriptIcon, entry.MenuPath)
                    : new GUIContent(label, entry.MenuPath);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * indentPerLevel);
                if (GUILayout.Button(entryContent, _entryButtonStyle))
                {
                    CreateMenuObject.Create(entry.ComponentType, entry.PrefabPath);
                }

                EditorGUILayout.EndHorizontal();
                shown++;
            }

            return shown;
        }

        private static string GetLeafName(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return "";
            }

            string cleaned = fullPath.Replace("Neoxider/", "");
            int idx = cleaned.LastIndexOf('/');
            return idx >= 0 ? cleaned[(idx + 1)..] : cleaned;
        }

        private static bool EntryMatches(CreateMenuObject.CreateMenuEntry entry, string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return true;
            }

            return !string.IsNullOrEmpty(entry.MenuPath) &&
                   entry.MenuPath.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasVisibleContent(MenuNode node, string search)
        {
            if (node == null)
            {
                return false;
            }

            if (node.Entries.Any(e => EntryMatches(e, search)))
            {
                return true;
            }

            return node.Children.Values.Any(c => HasVisibleContent(c, search));
        }

        private sealed class MenuNode
        {
            public readonly Dictionary<string, MenuNode> Children = new(StringComparer.OrdinalIgnoreCase);
            public readonly List<CreateMenuObject.CreateMenuEntry> Entries = new();
            public string FullPath;
            public string Name;
        }
    }
}