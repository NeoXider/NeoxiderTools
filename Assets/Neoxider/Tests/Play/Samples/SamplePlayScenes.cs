#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play-mode helpers for the authored sample scenes. They find the sample set that actually compiles
    ///     (developer <c>Assets/Neoxider/Samples</c> or the imported current-version
    ///     <c>Assets/Samples/NeoxiderTools/&lt;version&gt;</c>; hidden <c>Samples~</c> never compiles), list and load
    ///     its scenes, and reach sample types by name, because this assembly must not reference the optional sample
    ///     assemblies.
    /// </summary>
    internal static class SamplePlayScenes
    {
        internal const string DemoRuntimeAssembly = "Neo.Rpg.Demo";
        internal const string NoVisibleSampleRoot = "(no visible sample root)";
        internal const string ImportedSamplesRoot = "Assets/Samples/NeoxiderTools";
        internal const string DevelopmentSamplesRoot = "Assets/NeoxiderDemos";

        private const double LoadTimeoutSeconds = 30d;
        private const string SceneBuiltProperty = "IsSceneBuilt";

        private static readonly string[] DemoRootCandidates =
        {
            DevelopmentSamplesRoot,
            "Assets/Neoxider/Samples~",
            ImportedSamplesRoot,
            "Assets/Samples/Neoxider Tools"
        };

        private static readonly Regex ScriptReference =
            new(@"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}", RegexOptions.Compiled);

        private static readonly Regex SourcePrefabReference =
            new(@"m_SourcePrefab:\s*\{fileID:\s*100100000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}",
                RegexOptions.Compiled);

        private static HashSet<string> s_hiddenSampleGuids;

        /// <summary>
        ///     Folder that holds the compiled sample set (Demo plus the NeoxiderPages and Character Movement
        ///     Fundamentals samples next to it), or null when no sample is visible to the AssetDatabase.
        /// </summary>
        internal static string FindActiveSampleSetRoot()
        {
            string demoRoot = FindCompiledDemoRoot();
            if (demoRoot != null)
            {
                string set = Parent(demoRoot);
                if (AssetDatabase.IsValidFolder(set))
                {
                    return set;
                }
            }

            string version = CurrentPackageVersion();
            if (!string.IsNullOrEmpty(version) && AssetDatabase.IsValidFolder(ImportedSamplesRoot + "/" + version))
            {
                return ImportedSamplesRoot + "/" + version;
            }

            return AssetDatabase.IsValidFolder(DevelopmentSamplesRoot) ? DevelopmentSamplesRoot : null;
        }

        /// <summary>
        ///     Root of the Demo sample whose scripts compile, or null. The compiled <c>Neo.Rpg.Demo</c> asmdef wins
        ///     (only one copy of it can compile); the shared filesystem resolver is the fallback.
        /// </summary>
        internal static string FindCompiledDemoRoot()
        {
            string asmdef = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(DemoRuntimeAssembly);
            if (!string.IsNullOrEmpty(asmdef))
            {
                string demo = Parent(Parent(Normalize(asmdef)));
                if (AssetDatabase.IsValidFolder(demo))
                {
                    return demo;
                }
            }

            string found = SampleRuntimeSceneSmokeTests.FindSampleDemoRoot(DemoRootCandidates);
            return found != null && !IsHidden(found) && AssetDatabase.IsValidFolder(found) ? found : null;
        }

        /// <summary>Every scene of the active sample set, sorted; a single marker when nothing is visible.</summary>
        internal static IEnumerable<string> DiscoverScenes()
        {
            string root = FindActiveSampleSetRoot();
            List<string> scenes = new List<string>();
            if (root != null)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { root }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) && !scenes.Contains(path))
                    {
                        scenes.Add(path);
                    }
                }
            }

            scenes.Sort(StringComparer.Ordinal);
            if (scenes.Count == 0)
            {
                scenes.Add(NoVisibleSampleRoot);
            }

            return scenes;
        }

        /// <summary>Asset path of a Demo-sample scene; ignores the test when the Demo sample does not compile.</summary>
        internal static string RequireDemoScene(string relativePath)
        {
            string root = FindCompiledDemoRoot();
            if (root == null)
            {
                Assert.Ignore("This scene test needs the demo tree Assets/NeoxiderDemos/Demo (Demo download); " +
                              "the builders are covered by Neo.Demo.Editor.Tests.");
            }

            string path = root + "/" + relativePath;
            Assert.That(File.Exists(path), Is.True, "Sample scene was not found: " + path);
            return path;
        }

        internal static IEnumerator Load(string scenePath)
        {
            Scene scene = EditorSceneManager.LoadSceneInPlayMode(scenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            double deadline = Time.realtimeSinceStartupAsDouble + LoadTimeoutSeconds;
            while (!scene.isLoaded)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "Sample scene did not load within " + LoadTimeoutSeconds + " real-time seconds: " + scenePath);
                yield return null;
            }

            yield return null;
        }

        internal static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        /// <summary>
        ///     Script and prefab GUIDs the scene file and its visible nested prefabs reference that the AssetDatabase
        ///     cannot resolve, split into those that live in a hidden sample (a dependency sample is not visible) and
        ///     those that exist nowhere. Mirrors the EditMode rule in SampleAssetValidationTests.
        /// </summary>
        internal static void FindUnresolvedReferences(string scenePath, List<string> inHiddenSample,
            List<string> missing)
        {
            CollectUnresolvedReferences(scenePath, new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), inHiddenSample, missing);
        }

        /// <summary>
        ///     Every demo controller of the scene that exposes <c>public bool IsSceneBuilt</c> must report true. False
        ///     means the saved scene is not authored or is stale against its builder, which the controller itself
        ///     only reports as a warning. Returns how many controllers were checked.
        /// </summary>
        internal static int CollectUnbuiltControllerFailures(Scene scene, List<string> failures)
        {
            int checkedControllers = 0;
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.gameObject.scene != scene)
                {
                    continue;
                }

                PropertyInfo built = behaviour.GetType().GetProperty(SceneBuiltProperty,
                    BindingFlags.Instance | BindingFlags.Public);
                if (built == null || built.PropertyType != typeof(bool) || built.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                checkedControllers++;
                string at = behaviour.GetType().Name + " on '" + PathOf(behaviour.transform) + "'";
                try
                {
                    if (!(bool)built.GetValue(behaviour))
                    {
                        failures.Add(at + " reports " + SceneBuiltProperty + " = false: the saved scene is not " +
                                     "authored or is stale against its builder (Neoxider/Demo/Rebuild Active Demo Scene).");
                    }
                }
                catch (TargetInvocationException exception)
                {
                    failures.Add(at + "." + SceneBuiltProperty + " threw " +
                                 (exception.InnerException ?? exception).GetType().Name + ".");
                }
            }

            return checkedControllers;
        }

        private static void CollectUnresolvedReferences(string assetPath, HashSet<string> visitedAssets,
            HashSet<string> seenGuids, List<string> inHiddenSample, List<string> missing)
        {
            string diskPath = DiskPathOf(assetPath);
            if (!visitedAssets.Add(assetPath) || !File.Exists(diskPath))
            {
                return;
            }

            string text = File.ReadAllText(diskPath);
            List<string> nestedPrefabs = new List<string>();
            foreach (Regex pattern in new[] { ScriptReference, SourcePrefabReference })
            {
                foreach (Match match in pattern.Matches(text))
                {
                    string guid = match.Groups[1].Value;
                    string resolved = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(resolved))
                    {
                        if (pattern == SourcePrefabReference &&
                            resolved.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                            !nestedPrefabs.Contains(resolved))
                        {
                            nestedPrefabs.Add(resolved);
                        }

                        continue;
                    }

                    if (!seenGuids.Add(guid))
                    {
                        continue;
                    }

                    if (IsHiddenSampleGuid(guid))
                    {
                        inHiddenSample.Add(guid);
                    }
                    else
                    {
                        missing.Add(guid);
                    }
                }
            }

            foreach (string prefab in nestedPrefabs)
            {
                CollectUnresolvedReferences(prefab, visitedAssets, seenGuids, inHiddenSample, missing);
            }
        }

        private static string DiskPathOf(string assetPath)
        {
            string physical = FileUtil.GetPhysicalPath(assetPath);
            return string.IsNullOrEmpty(physical) ? assetPath : physical;
        }

        /// <summary>
        ///     Sample names of the scene builders registered for <paramref name="scenePath" />. Returns false when the
        ///     Neo.Demo.Editor kit does not compile (no builder registry to ask).
        /// </summary>
        internal static bool TryGetSceneBuilderSamples(string scenePath, List<string> samples)
        {
            Type runner = FindType("Neo.Demo.Editor.DemoSceneRunner");
            if (runner == null)
            {
                return false;
            }

            MethodInfo find = runner.GetMethod("FindBuildersForScene", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            Assert.That(find, Is.Not.Null, "DemoSceneRunner.FindBuildersForScene(string) is the builder registry seam.");
            foreach (object builder in (IEnumerable)find.Invoke(null, new object[] { scenePath }))
            {
                samples.Add(Convert.ToString(GetProperty(builder, "Sample")));
            }

            return true;
        }

        /// <summary>
        ///     Resolved scene paths of every registered, valid scene builder whose sample is visible. Builders that
        ///     cannot resolve (their sample is hidden) go to <paramref name="unresolved" />. Returns false when the
        ///     Neo.Demo.Editor kit does not compile.
        /// </summary>
        internal static bool TryGetRegisteredBuilderScenes(List<string> scenes, List<string> unresolved)
        {
            Type runner = FindType("Neo.Demo.Editor.DemoSceneRunner");
            if (runner == null)
            {
                return false;
            }

            MethodInfo discover = runner.GetMethod("DiscoverBuilders", BindingFlags.Public | BindingFlags.Static,
                null, Type.EmptyTypes, null);
            Assert.That(discover, Is.Not.Null, "DemoSceneRunner.DiscoverBuilders() is the builder registry seam.");
            foreach (object info in (IEnumerable)discover.Invoke(null, null))
            {
                string id = Convert.ToString(GetProperty(info, "Id"));
                if (!string.IsNullOrEmpty((string)GetProperty(info, "Error")))
                {
                    continue;
                }

                MethodInfo resolve = info.GetType().GetMethod("ResolveAssetPath",
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                Assert.That(resolve, Is.Not.Null, "DemoSceneBuilderInfo.ResolveAssetPath() resolves the builder scene.");
                try
                {
                    string path = (string)resolve.Invoke(info, null);
                    if (!scenes.Contains(path))
                    {
                        scenes.Add(path);
                    }
                }
                catch (TargetInvocationException exception)
                {
                    unresolved.Add(id + ": " + (exception.InnerException ?? exception).Message);
                }
            }

            return true;
        }

        /// <summary>
        ///     Every <c>ModuleDemoSceneInfo</c> of the scene must point at its authored card, and an enabled info with
        ///     Show Runtime Overlay must show it. Returns how many authored cards the scene carries.
        /// </summary>
        internal static int CollectInfoCardFailures(Scene scene, List<string> failures)
        {
            Type infoType = FindType("Neo.Samples.ModuleDemoSceneInfo");
            if (infoType == null)
            {
                return 0;
            }

            int authored = 0;
            foreach (Object found in Resources.FindObjectsOfTypeAll(infoType))
            {
                if (found is not Behaviour info || info.gameObject.scene != scene)
                {
                    continue;
                }

                string at = PathOf(info.transform);
                if (!(bool)GetProperty(info, "HasAuthoredOverlay"))
                {
                    failures.Add("ModuleDemoSceneInfo on '" + at + "' has no authored info card " +
                                 "(rebuild the scene with Neoxider/Demo/Rebuild All Demo Scenes).");
                    continue;
                }

                authored++;
                GameObject overlay = (GameObject)GetProperty(info, "OverlayRoot");
                if (info.isActiveAndEnabled && (bool)GetProperty(info, "ShowRuntimeOverlay") && !overlay.activeSelf)
                {
                    failures.Add("ModuleDemoSceneInfo on '" + at + "' keeps its info card '" + overlay.name +
                                 "' hidden although Show Runtime Overlay is on.");
                }
            }

            return authored;
        }

        /// <summary>A Filled Image without a sprite renders a full quad whatever its fill amount.</summary>
        internal static void CollectFilledImageFailures(Scene scene, List<string> failures)
        {
            foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (image.gameObject.scene == scene && image.type == Image.Type.Filled && image.sprite == null)
                {
                    failures.Add("Filled Image '" + PathOf(image.transform) + "' has no sprite.");
                }
            }
        }

        internal static Component RequireActiveComponent(string typeName)
        {
            Type type = FindType(typeName);
            Assert.That(type, Is.Not.Null, "Type '" + typeName + "' was not found.");
            foreach (Object found in Resources.FindObjectsOfTypeAll(type))
            {
                if (found is Component component && component.gameObject.scene.IsValid() &&
                    component.gameObject.scene.isLoaded && component.gameObject.activeInHierarchy)
                {
                    return component;
                }
            }

            Assert.Fail("The scene has no active " + typeName + ".");
            return null;
        }

        /// <summary>Presses an authored demo button the way the EventSystem does: through its onClick.</summary>
        internal static void ClickButton(string objectName)
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (button.name != objectName)
                {
                    continue;
                }

                Assert.That(button.interactable, Is.True, objectName + " is not interactable.");
                button.onClick.Invoke();
                return;
            }

            Assert.Fail("Missing authored demo control: " + objectName);
        }

        internal static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        internal static object GetProperty(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, target.GetType().Name + "." + name + " property was not found.");
            return property.GetValue(target);
        }

        internal static object GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, target.GetType().Name + "." + name + " field was not found.");
            return field.GetValue(target);
        }

        internal static string PathOf(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }

        internal static bool IsHidden(string path)
        {
            string normalized = Normalize(path);
            return normalized.Contains("/Samples~/") || normalized.EndsWith("/Samples~", StringComparison.Ordinal);
        }

        private static bool IsHiddenSampleGuid(string guid)
        {
            if (s_hiddenSampleGuids == null)
            {
                s_hiddenSampleGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string hidden = Path.Combine(PackageDiskRoot(), "Samples~");
                if (Directory.Exists(hidden))
                {
                    foreach (string meta in Directory.EnumerateFiles(hidden, "*.meta", SearchOption.AllDirectories))
                    {
                        foreach (string line in File.ReadLines(meta))
                        {
                            if (line.StartsWith("guid: ", StringComparison.Ordinal))
                            {
                                s_hiddenSampleGuids.Add(line.Substring("guid: ".Length).Trim());
                                break;
                            }
                        }
                    }
                }
            }

            return s_hiddenSampleGuids.Contains(guid);
        }

        private static string PackageDiskRoot()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(SaveProvider).Assembly);
            return package != null && !string.IsNullOrEmpty(package.resolvedPath)
                ? package.resolvedPath
                : Path.GetFullPath("Assets/Neoxider");
        }

        private static string CurrentPackageVersion()
        {
            string manifestPath = Path.Combine(PackageDiskRoot(), "package.json");
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            PackageVersionManifest manifest = JsonUtility.FromJson<PackageVersionManifest>(File.ReadAllText(manifestPath));
            return manifest != null ? manifest.version : null;
        }

        private static string Parent(string path)
        {
            string parent = Path.GetDirectoryName(Normalize(path));
            return parent != null ? Normalize(parent) : string.Empty;
        }

        private static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        }

        [Serializable]
        internal sealed class PackageVersionManifest
        {
            public string version;
        }
    }

    /// <summary>
    ///     Keeps one sample-scene test from leaking into the next: key-value saves go to an in-memory provider (a
    ///     scene that installs its own file provider, like SaveDemo, writes its demo-only file, which is deleted
    ///     again when this run created it), objects the scene moved to DontDestroyOnLoad are destroyed, global
    ///     audio/time/cursor state is restored and the test ends on a fresh empty scene.
    /// </summary>
    internal sealed class SamplePlaySceneIsolation
    {
        private static readonly string[] SaveFileSuffixes = { string.Empty, ".tmp", ".bak" };
        private static int s_cleanSceneCount;

        private readonly HashSet<GameObject> _persistentBefore = new HashSet<GameObject>();
        private readonly HashSet<string> _persistentFilesBefore = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ISaveProvider _provider;
        private float _timeScale;
        private float _fixedDeltaTime;
        private bool _listenerPause;
        private float _listenerVolume;
        private CursorLockMode _cursorLock;
        private bool _cursorVisible;
        private bool _active;

        internal void Begin()
        {
            if (_active)
            {
                return;
            }

            _active = true;
            _provider = SaveProvider.CurrentProvider;
            _timeScale = Time.timeScale;
            _fixedDeltaTime = Time.fixedDeltaTime;
            _listenerPause = AudioListener.pause;
            _listenerVolume = AudioListener.volume;
            _cursorLock = Cursor.lockState;
            _cursorVisible = Cursor.visible;
            _persistentBefore.Clear();
            foreach (GameObject root in PersistentRoots())
            {
                _persistentBefore.Add(root);
            }

            _persistentFilesBefore.Clear();
            if (Directory.Exists(Application.persistentDataPath))
            {
                foreach (string file in Directory.GetFiles(Application.persistentDataPath))
                {
                    _persistentFilesBefore.Add(Path.GetFullPath(file));
                }
            }

            // WHY: demo scenes autosave (inventory, settings, quests) through the shared provider; that traffic must
            // never reach the host's key-value saves.
            SaveProvider.SetProvider(new ShopPurchasePlayModeTests.MemorySaveProvider());
        }

        internal IEnumerator End()
        {
            if (!_active)
            {
                yield break;
            }

            foreach (GameObject root in PersistentRoots())
            {
                if (!_persistentBefore.Contains(root))
                {
                    Object.Destroy(root);
                }
            }

            Scene clean = SceneManager.CreateScene("NeoSamplePlayClean" + ++s_cleanSceneCount);
            SceneManager.SetActiveScene(clean);
            List<Scene> loaded = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != clean && scene.isLoaded)
                {
                    loaded.Add(scene);
                }
            }

            foreach (Scene scene in loaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone)
                {
                    yield return null;
                }
            }

            yield return null;
            yield return null;
            ISaveProvider sceneProvider = SaveProvider.CurrentProvider;
            SaveProvider.SetProvider(_provider);
            DeleteSaveFilesCreatedBy(sceneProvider);
            Time.timeScale = _timeScale;
            Time.fixedDeltaTime = _fixedDeltaTime;
            AudioListener.pause = _listenerPause;
            AudioListener.volume = _listenerVolume;
            Cursor.lockState = _cursorLock;
            Cursor.visible = _cursorVisible;
            _persistentBefore.Clear();
            _persistentFilesBefore.Clear();
            _active = false;
        }

        /// <summary>
        ///     Deletes the file (plus its .tmp/.bak siblings) of a file provider the scene installed, but only when it
        ///     sits directly in persistentDataPath and did not exist before <see cref="Begin" />.
        /// </summary>
        private void DeleteSaveFilesCreatedBy(ISaveProvider sceneProvider)
        {
            if (sceneProvider is not FileSaveProvider file || ReferenceEquals(sceneProvider, _provider) ||
                string.IsNullOrEmpty(file.FilePath))
            {
                return;
            }

            string savePath = Path.GetFullPath(file.FilePath);
            string persistentRoot = Path.GetFullPath(Application.persistentDataPath).TrimEnd('\\', '/');
            string saveDirectory = (Path.GetDirectoryName(savePath) ?? string.Empty).TrimEnd('\\', '/');
            if (!string.Equals(saveDirectory, persistentRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (string suffix in SaveFileSuffixes)
            {
                string path = savePath + suffix;
                if (_persistentFilesBefore.Contains(path) || !File.Exists(path))
                {
                    continue;
                }

                try
                {
                    File.Delete(path);
                }
                catch (IOException exception)
                {
                    TestContext.WriteLine("Could not delete the sample save file " + path + ": " + exception.Message);
                }
                catch (UnauthorizedAccessException exception)
                {
                    TestContext.WriteLine("Could not delete the sample save file " + path + ": " + exception.Message);
                }
            }
        }

        private static List<GameObject> PersistentRoots()
        {
            GameObject probe = new GameObject("NeoSamplePlay DontDestroyOnLoad probe");
            Object.DontDestroyOnLoad(probe);
            List<GameObject> roots = new List<GameObject>(probe.scene.GetRootGameObjects());
            roots.Remove(probe);
            Object.DestroyImmediate(probe);
            return roots;
        }
    }

    /// <summary>
    ///     Collects the error, assert and exception logs raised while it is open, plus the warnings demo
    ///     controllers raise when their saved scene is not authored or is stale ("... is not authored. Run
    ///     Neoxider/Demo/Rebuild Active Demo Scene."). Other warnings and plain logs are allowed.
    /// </summary>
    internal sealed class SampleLogCapture : IDisposable
    {
        private static readonly Regex UnbuiltSceneWarning = new(
            @"\b(?:not|no) authored\b|Rebuild (?:the scene|Active Demo Scene|All Demo Scenes)|Scene references are missing",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly object _lock = new object();
        private readonly List<string> _failures = new List<string>();

        internal SampleLogCapture()
        {
            Application.logMessageReceivedThreaded += OnLog;
        }

        internal string[] Failures
        {
            get
            {
                lock (_lock)
                {
                    return _failures.ToArray();
                }
            }
        }

        public void Dispose()
        {
            Application.logMessageReceivedThreaded -= OnLog;
        }

        /// <summary>Whether a log of <paramref name="type" /> fails a sample scene smoke.</summary>
        internal static bool IsFailure(LogType type, string message)
        {
            return type == LogType.Error || type == LogType.Exception || type == LogType.Assert ||
                   type == LogType.Warning && !string.IsNullOrEmpty(message) && UnbuiltSceneWarning.IsMatch(message);
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (!IsFailure(type, message))
            {
                return;
            }

            string kind = type == LogType.Warning ? "Warning (scene not authored or stale)" : type.ToString();
            string firstFrame = string.IsNullOrEmpty(stackTrace) ? string.Empty : stackTrace.Split('\n')[0].Trim();
            lock (_lock)
            {
                _failures.Add(kind + ": " + message + (firstFrame.Length > 0 ? " @ " + firstFrame : string.Empty));
            }
        }
    }
}
#endif
