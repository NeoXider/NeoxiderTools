using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Neo.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
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
                    string basePath = Path.GetDirectoryName(scriptPath);
                    basePath = Path.GetDirectoryName(basePath);
                    basePath = Path.GetDirectoryName(basePath);
                    _startPath = basePath + "/";
                }
                else
                {
                    string assetsPath = "Assets/Neoxider/";
                    string packagesPath = "Packages/com.neoxider.tools/";

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
                        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
                        foreach (string guid in prefabGuids)
                        {
                            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (prefabPath.Contains("Neoxider"))
                            {
                                int neoxiderIndex = prefabPath.IndexOf("Neoxider");
                                _startPath = prefabPath.Substring(0, neoxiderIndex + "Neoxider".Length) + "/";
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(_startPath))
                        {
                            _startPath = assetsPath;
                        }
                    }
                }

                return _startPath;
            }
        }

        public static T Create<T>() where T : MonoBehaviour
        {
            return CreateEmpty(typeof(T)) as T;
        }

        public static T Create<T>(string path) where T : MonoBehaviour
        {
            return Create(typeof(T), path) as T;
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

            string resolvedPrefabPath = ResolvePrefabPath(componentType, prefabPath);
            if (!string.IsNullOrEmpty(resolvedPrefabPath))
            {
                MonoBehaviour prefabComponent = CreateFromPrefab(componentType, resolvedPrefabPath);
                if (prefabComponent != null)
                {
                    return prefabComponent;
                }
            }

            return CreateEmpty(componentType);
        }

        private static MonoBehaviour CreateEmpty(Type componentType)
        {
            GameObject myObject = new(componentType.Name);
            var component = myObject.AddComponent(componentType) as MonoBehaviour;
            PlaceInScene(myObject, Selection.activeGameObject, $"Create {componentType.Name}");
            return component;
        }

        private static MonoBehaviour CreateFromPrefab(Type componentType, string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"CreateMenuObject: prefab not found at '{assetPath}'. Creating empty {componentType.Name}.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
                instance.name = prefab.name;
            }

            MonoBehaviour component = instance.GetComponent(componentType) as MonoBehaviour ??
                                      instance.GetComponentInChildren(componentType, true) as MonoBehaviour;
            if (component == null)
            {
                component = instance.AddComponent(componentType) as MonoBehaviour;
                Debug.LogWarning(
                    $"CreateMenuObject: prefab '{assetPath}' does not contain {componentType.Name}; added it to the root instance.",
                    instance);
            }

            PlaceInScene(instance, Selection.activeGameObject, $"Create {prefab.name}");
            return component;
        }

        /// <summary>
        ///     Registers undo, parents the object (context/selection, Canvas for UI, prefab-stage root),
        ///     gives it a unique sibling name and selects it. Shared by all Neoxider creation entry points.
        /// </summary>
        internal static void PlaceInScene(GameObject instance, GameObject requestedParent, string undoName)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RegisterCreatedObjectUndo(instance, undoName);

            GameObject parent = ResolveParent(requestedParent, instance);
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(instance, parent);
            }
            else
            {
                StageUtility.PlaceGameObjectInCurrentStage(instance);
                PositionAtSceneViewPivot(instance);
            }

            // WHY: GetUniqueNameForSibling would count the already-parented instance itself, giving
            // every object a spurious ' (1)'; EnsureUniqueNameForSibling excludes the object it renames.
            GameObjectUtility.EnsureUniqueNameForSibling(instance);
            Selection.activeGameObject = instance;
            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>
        ///     Chooses the parent for a freshly created object. Precedence:
        ///     1. An instance that is itself a Canvas is never rerouted under another Canvas — it goes to the
        ///     requested parent or the stage root, like Unity's own GameObject/UI/Canvas.
        ///     2. UI (RectTransform) with a requested parent that has a Canvas ancestor → the requested parent.
        ///     3. Other UI → a deterministic Canvas: enabled screen-space canvases win over world-space or
        ///     disabled ones; a new Canvas is created only when the stage has none.
        ///     4. Everything else → the requested parent, else the prefab-stage root, else the stage root.
        /// </summary>
        private static GameObject ResolveParent(GameObject requestedParent, GameObject instance)
        {
            bool isCanvasRoot = instance.GetComponent<Canvas>() != null;
            if (!isCanvasRoot && instance.GetComponent<RectTransform>() != null)
            {
                if (requestedParent != null && requestedParent.GetComponentInParent<Canvas>(true) != null)
                {
                    return requestedParent;
                }

                Canvas canvas = GetOrCreateCanvas();
                if (canvas != null)
                {
                    return canvas.gameObject;
                }
            }

            if (requestedParent != null)
            {
                return requestedParent;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            return prefabStage != null ? prefabStage.prefabContentsRoot : null;
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas canvas = FindPreferredCanvas();
            if (canvas != null)
            {
                return canvas;
            }

            // WHY: Unity's own menu item wires Canvas, scaler and the EventSystem matching the active input backend.
            if (MenuItemExists("GameObject/UI/Canvas"))
            {
                EditorApplication.ExecuteMenuItem("GameObject/UI/Canvas");
                canvas = FindPreferredCanvas();
                if (canvas != null)
                {
                    return canvas;
                }
            }

            // WHY: The menu item is unavailable in headless/test contexts;
            // build the minimal Canvas equivalent directly.
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            StageUtility.PlaceGameObjectInCurrentStage(canvasGo);
            EnsureEventSystem();
            return canvas;
        }

        /// <summary>
        ///     Deterministic Canvas lookup for created UI: an enabled screen-space canvas wins over
        ///     world-space or disabled ones, so UI never lands under an arbitrary canvas
        ///     (e.g. a tiny world-space health bar).
        /// </summary>
        private static Canvas FindPreferredCanvas()
        {
            Canvas best = null;
            int bestScore = int.MaxValue;
            foreach (Canvas candidate in StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>())
            {
                int score = GetCanvasScore(candidate);
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static int GetCanvasScore(Canvas canvas)
        {
            if (!canvas.isActiveAndEnabled)
            {
                return 2;
            }

            return canvas.renderMode == RenderMode.WorldSpace ? 1 : 0;
        }

        /// <summary>
        ///     Creates an EventSystem next to a manually built Canvas so the resulting UI is clickable.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (StageUtility.GetCurrentStageHandle()
                    .FindComponentOfType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            // WHY: InputSystemUIInputModule lives in the optional Input System package; resolve it by name
            // and fall back to the legacy module so UI input works on either backend.
            Type inputModuleType =
                Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            eventSystemGo.AddComponent(inputModuleType ?? typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            StageUtility.PlaceGameObjectInCurrentStage(eventSystemGo);
        }

        // WHY: ExecuteMenuItem logs a console Error for missing paths, which head-less contexts
        // (tests, batch mode) treat as failures — probe silently through the internal API instead.
        private static bool MenuItemExists(string menuPath)
        {
            try
            {
                MethodInfo probe = typeof(Menu).GetMethod("MenuItemExists",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return probe != null && (bool)probe.Invoke(null, new object[] { menuPath });
            }
            catch
            {
                return false;
            }
        }

        private static void PositionAtSceneViewPivot(GameObject instance)
        {
            // WHY: mirrors Unity's built-in GameObject menu, which honors the "Create Objects at Origin"
            // preference — stored as "Create3DObject.PlaceAtWorldOrigin" on Unity 6 (old key kept as fallback).
            if (instance.GetComponent<RectTransform>() != null ||
                EditorPrefs.GetBool("Create3DObject.PlaceAtWorldOrigin",
                    EditorPrefs.GetBool("Create3DObjectsAtOrigin", false)))
            {
                return;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                instance.transform.position = sceneView.pivot;
            }
        }

        private static string ResolvePrefabPath(Type componentType, string prefabPath)
        {
            if (!string.IsNullOrEmpty(prefabPath))
            {
                string explicitPath = ToAssetPath(prefabPath);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(explicitPath) != null)
                {
                    return explicitPath;
                }

                Debug.LogWarning(
                    $"CreateMenuObject: prefab path '{explicitPath}' for {componentType.Name} was not found. Trying auto-discovery.");
            }

            return FindPrefabPathForComponent(componentType);
        }

        private static string ToAssetPath(string prefabPath)
        {
            if (prefabPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                prefabPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                return prefabPath;
            }

            return startPath + prefabPath;
        }

        private static string FindPrefabPathForComponent(Type componentType)
        {
            string prefabRoot = startPath + "Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabRoot))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => PrefabContainsComponent(path, componentType))
                .OrderBy(path => GetPrefabMatchScore(path, componentType))
                .ThenBy(path => path.Length)
                .FirstOrDefault();
        }

        private static bool PrefabContainsComponent(string path, Type componentType)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null && prefab.GetComponentInChildren(componentType, true) != null;
        }

        private static int GetPrefabMatchScore(string path, Type componentType)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(fileName, componentType.Name, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return fileName != null && fileName.IndexOf(componentType.Name, StringComparison.OrdinalIgnoreCase) >= 0
                ? 1
                : 2;
        }

        #region Dynamic Menu

        private const string CreateFromMenuAttributeName = "CreateFromMenuAttribute";
        private const string LegacyComponentAttributeName = "LegacyComponentAttribute";

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

        /// <summary>Used when opening the window from Neoxider → Windows (dockable window).</summary>
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

            static bool IsLegacyTypeHiddenFromCreateMenu(Type t)
            {
                if (t == null)
                {
                    return false;
                }

                foreach (object a in t.GetCustomAttributes(false))
                {
                    if (a == null || a.GetType().Name != LegacyComponentAttributeName)
                    {
                        continue;
                    }

                    try
                    {
                        PropertyInfo hideProperty = a.GetType().GetProperty("HideFromCreateMenu");
                        if (hideProperty == null)
                        {
                            return true;
                        }

                        object value = hideProperty.GetValue(a);
                        return value is not bool hide || hide;
                    }
                    catch
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
                .Where(t => !IsLegacyTypeHiddenFromCreateMenu(t))
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
                                HasCreateFromMenuAttribute(t) &&
                                !IsLegacyTypeHiddenFromCreateMenu(t))
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
        private const float BannerHeight = 46f;
        private const float CategoryHeaderHeight = 30f;
        private const float SubFolderHeight = 24f;
        private const float EntryRowHeight = 26f;
        private const float IndentPerLevel = 14f;
        private const float RowMarginX = 6f;

        /// <summary>
        ///     Meaningful color palette by category: UI/looks — purple, audio — pink, control/conditions — orange, data —
        ///     teal, mechanics — green, game/levels — blue, etc.
        /// </summary>
        private static readonly Dictionary<string, Color> CategoryColors = new(StringComparer.OrdinalIgnoreCase)
        {
            { "UI", new Color(0.6f, 0.4f, 1f, 0.9f) },
            { "Tools", new Color(0.35f, 0.75f, 0.35f, 0.9f) },
            { "Shop", new Color(1f, 0.75f, 0f, 0.9f) },
            { "Audio", new Color(0.81f, 0.39f, 0.81f, 0.9f) },
            { "Bonus", new Color(1f, 0.55f, 0.1f, 0.9f) },
            { "Level", new Color(0.3f, 0.59f, 1f, 0.9f) },
            { "Save", new Color(0.36f, 0.69f, 0.84f, 0.9f) },
            { "Condition", new Color(1f, 0.55f, 0.2f, 0.9f) },
            { "Animations", new Color(0.65f, 0.45f, 0.95f, 0.9f) },
            { "GridSystem", new Color(0.36f, 0.69f, 0.84f, 0.9f) },
            { "Parallax", new Color(0.3f, 0.59f, 1f, 0.9f) },
            { "NPC", new Color(0.36f, 0.69f, 0.84f, 0.9f) }
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

        private static readonly (string Category, string Label, string Path)[] PresetEntries =
        {
            ("System", "System Root", "Prefabs/-System--.prefab"),
            ("Player", "First Person Controller", "Prefabs/Tools/First Person Controller.prefab"),
            ("Combat", "Simple Weapon", "Prefabs/Simple Weapon.prefab"),
            ("Combat", "Bullet", "Prefabs/Bullet.prefab"),
            ("Interaction", "Interactive Sphere", "Prefabs/Tools/Interact/Interactive Sphere.prefab"),
            ("Interaction", "Toggle Interactive", "Prefabs/Tools/Interact/Toggle Interactive.prefab"),
            ("Interaction", "Trigger Cube", "Prefabs/Tools/Interact/Trigger Cube.prefab")
        };

        private static readonly Color PresetsCategoryColor = new(0.2f, 0.5f, 0.35f, 0.9f);

        private readonly Dictionary<string, bool> _expanded = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, bool> _presetsExpanded = new(StringComparer.OrdinalIgnoreCase)
        {
            { "System", true },
            { "Player", true },
            { "Combat", true },
            { "Interaction", true }
        };

        private CreateMenuObject.CreateMenuEntry[] _entries = Array.Empty<CreateMenuObject.CreateMenuEntry>();
        private GUIStyle _bannerSubStyle;
        private GUIStyle _bannerTitleStyle;
        private GUIStyle _categoryLabelStyle;
        private GUIStyle _countStyle;
        private GUIStyle _emptyStyle;
        private GUIStyle _entryLabelStyle;
        private GUIStyle _searchLabelStyle;
        private GUIStyle _subFolderLabelStyle;
        private GUIStyle _triangleStyle;
        private MenuNode _root;
        private Vector2 _scroll;
        private string _search = "";

        private void OnGUI()
        {
            InitStyles();
            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }

            DrawHeader();
            DrawSearchBar();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(2f);
            DrawPresetsSection();
            int shown = DrawTree(_root, 0, default);
            if (shown == 0)
            {
                DrawEmptyState(string.IsNullOrWhiteSpace(_search)
                    ? "No components with [CreateFromMenu] were found.\nPress Refresh after scripts finish compiling."
                    : "Nothing found.\nChange the search or clear the field.");
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.EndScrollView();
        }

        private void DrawPresetsSection()
        {
            var prefabTex = (EditorGUIUtility.IconContent("d_Prefab Icon") ??
                             EditorGUIUtility.IconContent("Prefab Icon"))?.image as Texture2D;
            var folderTex = (EditorGUIUtility.IconContent("Folder Icon") ??
                             EditorGUIUtility.IconContent("d_Folder Icon"))?.image as Texture2D;
            Color presetsAccent = new(PresetsCategoryColor.r, PresetsCategoryColor.g, PresetsCategoryColor.b, 1f);

            bool presetsExpanded = !_presetsExpanded.TryGetValue("Presets", out bool v) || v;
            Rect headerRow = GUILayoutUtility.GetRect(0f, CategoryHeaderHeight, GUILayout.ExpandWidth(true));
            if (DrawHeaderCard(Inset(headerRow, 0), "Presets (ready-made prefabs)", folderTex, 0, presetsExpanded,
                    PresetEntries.Length, presetsAccent))
            {
                presetsExpanded = !presetsExpanded;
            }

            _presetsExpanded["Presets"] = presetsExpanded;
            if (!presetsExpanded)
            {
                return;
            }

            string lastCategory = null;
            bool categoryExpanded = true;
            foreach ((string category, string label, string path) in PresetEntries)
            {
                if (lastCategory != category)
                {
                    lastCategory = category;
                    categoryExpanded = _presetsExpanded.TryGetValue($"Presets/{category}", out bool ce) && ce;
                    int catCount = PresetEntries.Count(p => p.Category == category);
                    Rect catRow = GUILayoutUtility.GetRect(0f, SubFolderHeight, GUILayout.ExpandWidth(true));
                    if (DrawHeaderCard(Inset(catRow, 1), category, folderTex, 1, categoryExpanded, catCount,
                            presetsAccent))
                    {
                        categoryExpanded = !categoryExpanded;
                    }

                    _presetsExpanded[$"Presets/{category}"] = categoryExpanded;
                }

                if (!categoryExpanded)
                {
                    continue;
                }

                Rect entryRow = GUILayoutUtility.GetRect(0f, EntryRowHeight, GUILayout.ExpandWidth(true));
                if (DrawEntryRow(Inset(entryRow, 2), path, label, prefabTex, presetsAccent))
                {
                    NeoxiderPresetCreateMenu.CreatePreset(path);
                }
            }

            EditorGUILayout.Space(6f);
        }

        private static GUIContent GetFolderContent(string name)
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
            // WHY: custom row hover states need mouse-move events, which Unity only delivers when requested.
            window.wantsMouseMove = true;
            window.Show();
            window.Focus();
        }

        [MenuItem("Neoxider/Windows/Create Neoxider Object", false, 3)]
        private static void OpenFromWindowMenu()
        {
            CreateMenuObject.CreateMenuEntry[] entries = CreateMenuObject.BuildCreateMenuEntriesForWindow();
            ShowWindow(entries);
        }

        private void InitStyles()
        {
            if (_bannerTitleStyle != null)
            {
                return;
            }

            _bannerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = NeoInspectorTheme.OnAccentText }
            };
            _bannerSubStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.78f) }
            };
            _categoryLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = NeoInspectorTheme.TitleText }
            };
            _subFolderLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = NeoInspectorTheme.TitleText }
            };
            _entryLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = NeoInspectorTheme.TitleText }
            };
            _countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = NeoInspectorTheme.MutedText }
            };
            _triangleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = NeoInspectorTheme.MutedText }
            };
            _emptyStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = NeoInspectorTheme.MutedText }
            };
            _searchLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = NeoInspectorTheme.MutedText }
            };
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
            Rect rect = GUILayoutUtility.GetRect(0f, BannerHeight, GUILayout.ExpandWidth(true));
            Rect banner = new(rect.x + RowMarginX, rect.y + 4f, rect.width - RowMarginX * 2f, rect.height - 4f);
            NeoInspectorTheme.DrawRoundedTexture(banner, NeoInspectorTheme.BannerGradient, NeoInspectorTheme.RadiusCard,
                Color.white);

            Rect title = new(banner.x + 14f, banner.y + 5f, banner.width - 28f, 20f);
            GUI.Label(title, "Create Neoxider Object", _bannerTitleStyle);
            Rect sub = new(banner.x + 14f, title.yMax, banner.width - 28f, 14f);
            GUI.Label(sub, $"GameObject → Neoxider   ·   {_entries.Length} components", _bannerSubStyle);
            GUILayout.Space(6f);
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Search", _searchLabelStyle, GUILayout.Width(44));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(_search))
            {
                Color prev = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.45f, 0.45f);
                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24)))
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
            else if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
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

        private int DrawTree(MenuNode node, int depth, Color accent)
        {
            if (node == null)
            {
                return 0;
            }

            int shown = 0;
            string search = _search?.Trim();

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

                Color childAccent = depth == 0 && CategoryColors.TryGetValue(child.Name, out Color c)
                    ? new Color(c.r, c.g, c.b, 1f)
                    : accent;
                int count = CountVisibleEntries(child, search);
                Rect row = GUILayoutUtility.GetRect(0f, depth == 0 ? CategoryHeaderHeight : SubFolderHeight,
                    GUILayout.ExpandWidth(true));
                if (DrawFolderRow(Inset(row, depth), child, depth, expanded, count, childAccent))
                {
                    expanded = !expanded;
                }

                _expanded[child.FullPath] = expanded;
                shown++;

                if (expanded)
                {
                    shown += DrawTree(child, depth + 1, childAccent);
                }
            }

            GUIContent scriptIconContent = EditorGUIUtility.IconContent("d_cs Script Icon") ??
                                           EditorGUIUtility.IconContent("cs Script Icon");
            var scriptIcon = scriptIconContent?.image as Texture2D;
            foreach (CreateMenuObject.CreateMenuEntry entry in node.Entries.OrderBy(e => e.MenuPath))
            {
                if (!EntryMatches(entry, search))
                {
                    continue;
                }

                string label = GetLeafName(entry.MenuPath);
                Rect row = GUILayoutUtility.GetRect(0f, EntryRowHeight, GUILayout.ExpandWidth(true));
                if (DrawEntryRow(Inset(row, depth + 1), entry.MenuPath, label, scriptIcon, accent))
                {
                    CreateMenuObject.Create(entry.ComponentType, entry.PrefabPath);
                }

                shown++;
            }

            return shown;
        }

        /// <summary>Insets a full-width layout row into a padded, depth-indented card rect.</summary>
        private static Rect Inset(Rect row, int depth)
        {
            float left = RowMarginX + depth * IndentPerLevel;
            return new Rect(row.x + left, row.y + 1f, Mathf.Max(0f, row.width - left - RowMarginX), row.height - 2f);
        }

        private bool DrawFolderRow(Rect rect, MenuNode node, int depth, bool expanded, int count, Color accent)
        {
            GUIContent folder = GetFolderContent(node.Name);
            return DrawHeaderCard(rect, node.Name, folder.image, depth, expanded, count, accent);
        }

        /// <summary>
        ///     Draws a rounded category/folder pill (accent-tinted at depth 0, recessed section deeper) with a
        ///     foldout triangle, icon, bold label and a muted entry count. Returns true when the row was clicked.
        /// </summary>
        private bool DrawHeaderCard(Rect rect, string label, Texture icon, int depth, bool expanded, int count,
            Color accent)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            bool hasAccent = depth == 0 && accent.a > 0f;
            float radius = depth == 0 ? NeoInspectorTheme.RadiusPill : NeoInspectorTheme.RadiusSection;
            Color baseBg = depth == 0 ? NeoInspectorTheme.HeaderRowBackground : NeoInspectorTheme.SectionBackground;

            NeoInspectorTheme.DrawRoundedRect(rect, baseBg, radius);
            if (hasAccent)
            {
                NeoInspectorTheme.DrawRoundedRect(rect, new Color(accent.r, accent.g, accent.b, 0.16f), radius);
                NeoInspectorTheme.DrawAccentRail(rect, accent, 3f, 6f);
            }

            if (hover)
            {
                NeoInspectorTheme.DrawRoundedRect(rect, NeoInspectorTheme.HoverOverlay, radius);
            }

            float x = rect.x + (depth == 0 ? 10f : 8f);
            GUI.Label(new Rect(x, rect.y, 14f, rect.height), expanded ? "▾" : "▸", _triangleStyle);
            x += 16f;
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(x, rect.y + (rect.height - 16f) * 0.5f, 16f, 16f), icon, ScaleMode.ScaleToFit);
                x += 22f;
            }

            const float countWidth = 34f;
            GUI.Label(new Rect(x, rect.y, Mathf.Max(0f, rect.xMax - x - countWidth - 8f), rect.height), label,
                depth == 0 ? _categoryLabelStyle : _subFolderLabelStyle);
            if (count >= 0)
            {
                GUI.Label(new Rect(rect.xMax - countWidth - 8f, rect.y, countWidth, rect.height),
                    count.ToString(), _countStyle);
            }

            return HandleRowClick(rect);
        }

        /// <summary>
        ///     Draws a rounded component row with an icon, label and a hover state (overlay plus accent rail).
        ///     Returns true when the row was clicked.
        /// </summary>
        // WHY: RowTint (2% white) let rows blend into the window background; a solid raised fill with a
        // hairline edge makes every entry read as its own distinct row.
        private static Color EntryRowFill => EditorGUIUtility.isProSkin
            ? new Color(0.166f, 0.178f, 0.208f, 1f)
            : new Color(0.958f, 0.966f, 0.980f, 1f);

        private bool DrawEntryRow(Rect rect, string tooltip, string label, Texture icon, Color accent)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            NeoInspectorTheme.DrawRoundedRect(rect, EntryRowFill, NeoInspectorTheme.Separator,
                NeoInspectorTheme.RadiusRow, 1f);
            if (hover)
            {
                NeoInspectorTheme.DrawRoundedRect(rect, NeoInspectorTheme.HoverOverlay, NeoInspectorTheme.RadiusRow);
                NeoInspectorTheme.DrawAccentRail(rect, accent.a > 0f ? accent : NeoInspectorTheme.BrandCyan, 2.5f, 5f);
            }

            float x = rect.x + 10f;
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(x, rect.y + (rect.height - 16f) * 0.5f, 16f, 16f), icon, ScaleMode.ScaleToFit);
                x += 22f;
            }

            GUI.Label(new Rect(x, rect.y, Mathf.Max(0f, rect.xMax - x - 8f), rect.height),
                new GUIContent(label, tooltip), _entryLabelStyle);
            return HandleRowClick(rect);
        }

        private static bool HandleRowClick(Rect rect)
        {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }

            return false;
        }

        private static int CountVisibleEntries(MenuNode node, string search)
        {
            int total = node.Entries.Count(e => EntryMatches(e, search));
            foreach (MenuNode child in node.Children.Values)
            {
                total += CountVisibleEntries(child, search);
            }

            return total;
        }

        private void DrawEmptyState(string message)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 64f, GUILayout.ExpandWidth(true));
            Rect card = new(rect.x + RowMarginX, rect.y + 6f, rect.width - RowMarginX * 2f, rect.height - 12f);
            NeoInspectorTheme.DrawRoundedRect(card, NeoInspectorTheme.SectionBackground, NeoInspectorTheme.RadiusCard);
            GUI.Label(card, message, _emptyStyle);
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
