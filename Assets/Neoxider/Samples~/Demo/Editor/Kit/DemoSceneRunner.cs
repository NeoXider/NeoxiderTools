using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor
{
    /// <summary>A discovered <see cref="DemoSceneBuilderAttribute" /> method.</summary>
    public sealed class DemoSceneBuilderInfo
    {
        internal DemoSceneBuilderInfo(MethodInfo method, DemoSceneBuilderAttribute attribute)
        {
            Method = method;
            Attribute = attribute;
            Id = method.DeclaringType != null ? method.DeclaringType.FullName + "." + method.Name : method.Name;
            ParameterInfo[] parameters = method.GetParameters();
            TakesContext = parameters.Length == 1 && parameters[0].ParameterType == typeof(DemoSceneBuildContext);
            Error = Validate(method, attribute, parameters);
        }

        public MethodInfo Method { get; }
        public DemoSceneBuilderAttribute Attribute { get; }
        public string Id { get; }
        public string ScenePath => DemoSampleRoots.Normalize(Attribute.ScenePath);
        public DemoSample Sample => Attribute.Sample;
        public int Order => Attribute.Order;
        public bool TakesContext { get; }

        /// <summary>Why the method cannot run (null when valid).</summary>
        public string Error { get; }

        /// <summary>Full asset path of the target scene (throws when the sample is not visible).</summary>
        public string ResolveAssetPath()
        {
            return ScenePath.StartsWith("Assets/", StringComparison.Ordinal)
                ? ScenePath
                : DemoSampleRoots.ToAssetPath(Sample, ScenePath);
        }

        internal void Invoke(DemoSceneBuildContext context)
        {
            Method.Invoke(null, TakesContext ? new object[] { context } : Array.Empty<object>());
        }

        private static string Validate(MethodInfo method, DemoSceneBuilderAttribute attribute, ParameterInfo[] parameters)
        {
            if (!method.IsStatic)
            {
                return "builder must be static";
            }

            if (method.ContainsGenericParameters)
            {
                return "builder must not be generic";
            }

            if (method.ReturnType != typeof(void))
            {
                return "builder must return void";
            }

            if (parameters.Length > 1 || (parameters.Length == 1 && parameters[0].ParameterType != typeof(DemoSceneBuildContext)))
            {
                return "builder signature must be () or (DemoSceneBuildContext)";
            }

            string path = DemoSampleRoots.Normalize(attribute.ScenePath);
            if (path.Length == 0 || !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return "scene path '" + attribute.ScenePath + "' must end with .unity";
            }

            return null;
        }
    }

    /// <summary>Outcome of building one scene.</summary>
    public sealed class DemoSceneResult
    {
        internal DemoSceneResult(string scenePath)
        {
            ScenePath = scenePath;
        }

        public string ScenePath { get; }
        public List<string> Builders { get; } = new();
        public bool Saved { get; internal set; }
        public double Milliseconds { get; internal set; }
        public int Created { get; internal set; }
        public int Reused { get; internal set; }
        public int Removed { get; internal set; }
        public int Adopted { get; internal set; }
        public int Listeners { get; internal set; }
        public List<string> Notes { get; } = new();
        public List<string> Warnings { get; } = new();
        public List<string> Errors { get; } = new();
        public bool Success => Errors.Count == 0;

        public string Summary()
        {
            StringBuilder builder = new();
            builder.Append(Success ? "OK   " : "FAIL ").Append(ScenePath)
                .Append(" | builders ").Append(Builders.Count)
                .Append(" | +").Append(Created).Append(" created, ").Append(Reused).Append(" reused, -")
                .Append(Removed).Append(" removed")
                .Append(" | listeners ").Append(Listeners)
                .Append(" | ").Append(Saved ? "saved" : "not saved")
                .Append(" | ").Append(Milliseconds.ToString("0", CultureInfo.InvariantCulture)).Append(" ms");
            foreach (string error in Errors)
            {
                builder.Append("\n   error: ").Append(error);
            }

            foreach (string warning in Warnings)
            {
                builder.Append("\n   warning: ").Append(warning);
            }

            foreach (string note in Notes)
            {
                builder.Append("\n   note: ").Append(note);
            }

            return builder.ToString();
        }
    }

    /// <summary>Outcome of a runner invocation (all scenes).</summary>
    public sealed class DemoSceneRunReport
    {
        public List<DemoSceneResult> Scenes { get; } = new();

        /// <summary>Errors not tied to one scene (invalid builders, unmatched filters, no sample root).</summary>
        public List<string> Errors { get; } = new();

        public bool Succeeded => Errors.Count == 0 && Scenes.TrueForAll(scene => scene.Success);

        public int FailedScenes => Scenes.FindAll(scene => !scene.Success).Count;

        public string Summary()
        {
            StringBuilder builder = new();
            builder.Append(DemoSceneRunner.LogPrefix).Append(Succeeded ? "SUCCESS" : "FAILED").Append(": ")
                .Append(Scenes.Count - FailedScenes).Append('/').Append(Scenes.Count).Append(" scenes built");
            foreach (string error in Errors)
            {
                builder.Append("\n error: ").Append(error);
            }

            foreach (DemoSceneResult scene in Scenes)
            {
                builder.Append("\n ").Append(scene.Summary());
            }

            return builder.ToString();
        }

        /// <summary>Machine-readable report for the batch entry (-neoDemoReport).</summary>
        public string ToJson()
        {
            StringBuilder builder = new();
            builder.Append("{\"succeeded\":").Append(Succeeded ? "true" : "false").Append(",\"errors\":");
            AppendArray(builder, Errors);
            builder.Append(",\"scenes\":[");
            for (int i = 0; i < Scenes.Count; i++)
            {
                DemoSceneResult scene = Scenes[i];
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append("{\"scene\":").Append(Quote(scene.ScenePath))
                    .Append(",\"success\":").Append(scene.Success ? "true" : "false")
                    .Append(",\"saved\":").Append(scene.Saved ? "true" : "false")
                    .Append(",\"created\":").Append(scene.Created)
                    .Append(",\"reused\":").Append(scene.Reused)
                    .Append(",\"removed\":").Append(scene.Removed)
                    .Append(",\"listeners\":").Append(scene.Listeners)
                    .Append(",\"ms\":").Append(scene.Milliseconds.ToString("0", CultureInfo.InvariantCulture))
                    .Append(",\"builders\":");
                AppendArray(builder, scene.Builders);
                builder.Append(",\"errors\":");
                AppendArray(builder, scene.Errors);
                builder.Append(",\"warnings\":");
                AppendArray(builder, scene.Warnings);
                builder.Append('}');
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private static void AppendArray(StringBuilder builder, List<string> values)
        {
            builder.Append('[');
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(Quote(values[i]));
            }

            builder.Append(']');
        }

        private static string Quote(string value)
        {
            StringBuilder builder = new("\"");
            foreach (char c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            return builder.Append('"').ToString();
        }
    }

    /// <summary>
    ///     Discovers <see cref="DemoSceneBuilderAttribute" /> builders and rebuilds their scenes: open, run every
    ///     builder of the scene (a throwing builder is reported and the others still run), prune, validate, save.
    ///     A scene with errors is not saved, so a broken builder never overwrites a good scene file.
    /// </summary>
    public static class DemoSceneRunner
    {
        public const string LogPrefix = "[DemoSceneKit] ";
        public const string InfoCardPassId = "DemoSceneRunner.InfoCardPass";

        private const string MenuRoot = "Neoxider/Demo/";

        [MenuItem(MenuRoot + "Rebuild All Demo Scenes", false, 300)]
        private static void RebuildAllFromMenu()
        {
            ShowReport(RebuildAll());
        }

        [MenuItem(MenuRoot + "Rebuild Active Demo Scene", false, 301)]
        private static void RebuildActiveFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            ShowReport(RebuildAll(new[] { scene.path }));
        }

        [MenuItem(MenuRoot + "Rebuild Active Demo Scene", true)]
        private static bool CanRebuildActive()
        {
            Scene scene = SceneManager.GetActiveScene();
            return !EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrEmpty(scene.path) &&
                   FindBuildersForScene(scene.path).Count > 0;
        }

        [MenuItem(MenuRoot + "Author Info Card In Active Scene", false, 320)]
        private static void AuthorInfoCardFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            DemoSceneRunReport report = new();
            if (!string.IsNullOrEmpty(scene.path) && FindBuildersForScene(scene.path).Count > 0)
            {
                report.Errors.Add(scene.path + " has a scene builder; use Rebuild Active Demo Scene instead.");
                ShowReport(Finish(report));
                return;
            }

            DemoSceneResult result = AuthorInfoCards(scene, null);
            if (result != null)
            {
                if (result.Success && !string.IsNullOrEmpty(scene.path))
                {
                    result.Saved = EditorSceneManager.SaveScene(scene);
                }

                report.Scenes.Add(result);
            }
            else
            {
                report.Errors.Add("No ModuleDemoSceneInfo without an authored card in " + scene.name + ".");
            }

            ShowReport(Finish(report));
        }

        [MenuItem(MenuRoot + "Author Missing Info Cards (All Demo Scenes)", false, 321)]
        private static void AuthorAllInfoCardsFromMenu()
        {
            ShowReport(AuthorMissingInfoCards());
        }

        [MenuItem(MenuRoot + "List Scene Builders", false, 340)]
        private static void ListBuildersFromMenu()
        {
            StringBuilder builder = new(LogPrefix + "Registered scene builders:");
            foreach (DemoSceneBuilderInfo info in DiscoverBuilders())
            {
                builder.Append("\n  ").Append(info.Sample).Append(':').Append(info.ScenePath).Append(" <- ").Append(info.Id);
                if (info.Error != null)
                {
                    builder.Append("  [INVALID: ").Append(info.Error).Append(']');
                }
            }

            Debug.Log(builder.ToString());
        }

        /// <summary>Every method marked with <see cref="DemoSceneBuilderAttribute" />, sorted by scene then order.</summary>
        public static List<DemoSceneBuilderInfo> DiscoverBuilders()
        {
            List<DemoSceneBuilderInfo> builders = new();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<DemoSceneBuilderAttribute>())
            {
                DemoSceneBuilderAttribute attribute = method.GetCustomAttribute<DemoSceneBuilderAttribute>();
                if (attribute != null)
                {
                    builders.Add(new DemoSceneBuilderInfo(method, attribute));
                }
            }

            builders.Sort((a, b) =>
            {
                int bySample = a.Sample.CompareTo(b.Sample);
                if (bySample != 0)
                {
                    return bySample;
                }

                int byScene = string.Compare(a.ScenePath, b.ScenePath, StringComparison.OrdinalIgnoreCase);
                return byScene != 0 ? byScene : a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Id, b.Id);
            });
            return builders;
        }

        /// <summary>Valid builders whose resolved scene path equals <paramref name="sceneAssetPath" />.</summary>
        public static List<DemoSceneBuilderInfo> FindBuildersForScene(string sceneAssetPath)
        {
            List<DemoSceneBuilderInfo> result = new();
            string target = DemoSampleRoots.Normalize(sceneAssetPath);
            foreach (DemoSceneBuilderInfo info in DiscoverBuilders())
            {
                if (info.Error == null && TryResolve(info, out string path, out _) &&
                    string.Equals(path, target, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(info);
                }
            }

            return result;
        }

        /// <summary>
        ///     Rebuilds every registered scene, or only those matching <paramref name="sceneFilter" /> (full asset
        ///     path, path relative to the sample root, or scene file name). With
        ///     <paramref name="authorMissingInfoCards" /> every other Demo scene with an unauthored
        ///     <see cref="ModuleDemoSceneInfo" /> also gets a standalone "[Demo] Info Card" root.
        /// </summary>
        public static DemoSceneRunReport RebuildAll(IReadOnlyCollection<string> sceneFilter = null,
            bool authorMissingInfoCards = false)
        {
            return Run(sceneFilter, true, authorMissingInfoCards);
        }

        /// <summary>
        ///     Only the info-card pass: every Demo scene without a builder whose <see cref="ModuleDemoSceneInfo" />
        ///     has no authored card gets a standalone "[Demo] Info Card" root (optionally filtered).
        /// </summary>
        public static DemoSceneRunReport AuthorMissingInfoCards(IReadOnlyCollection<string> sceneFilter = null)
        {
            return Run(sceneFilter, false, true);
        }

        private static DemoSceneRunReport Run(IReadOnlyCollection<string> sceneFilter, bool runBuilders,
            bool authorMissingInfoCards)
        {
            DemoSceneRunReport report = new();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                report.Errors.Add("Leave Play Mode before rebuilding demo scenes.");
                return Finish(report);
            }

            if (!DemoSampleRoots.TryResolveDemoRoot(out string demoRoot, out string rootError))
            {
                report.Errors.Add(rootError);
                return Finish(report);
            }

            SortedDictionary<string, List<DemoSceneBuilderInfo>> scenes = GroupBuilders(report);
            HashSet<string> builderScenes = new(scenes.Keys, StringComparer.OrdinalIgnoreCase);
            List<string> filter = NormalizeFilter(sceneFilter);
            HashSet<string> matchedFilters = new(StringComparer.OrdinalIgnoreCase);
            if (runBuilders)
            {
                ApplyFilter(scenes, filter, demoRoot, matchedFilters);
            }
            else
            {
                scenes.Clear();
            }

            List<string> infoCardScenes = authorMissingInfoCards
                ? CollectInfoCardScenes(demoRoot, builderScenes, filter, matchedFilters)
                : new List<string>();
            if (filter != null)
            {
                foreach (string wanted in filter)
                {
                    if (!matchedFilters.Contains(wanted))
                    {
                        report.Errors.Add("No scene matches '" + wanted + "'" +
                                          (authorMissingInfoCards ? "." : " (no builder registered for it)."));
                    }
                }
            }

            bool interactive = !Application.isBatchMode;
            SceneSetup[] setup = null;
            if (interactive)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    report.Errors.Add("Cancelled: open scenes have unsaved changes.");
                    return Finish(report);
                }

                setup = EditorSceneManager.GetSceneManagerSetup();
            }

            try
            {
                foreach (KeyValuePair<string, List<DemoSceneBuilderInfo>> entry in scenes)
                {
                    try
                    {
                        report.Scenes.Add(RebuildSceneFile(entry.Key, entry.Value));
                    }
                    catch (Exception exception)
                    {
                        report.Scenes.Add(Unexpected(entry.Key, exception));
                    }
                }

                foreach (string path in infoCardScenes)
                {
                    try
                    {
                        AuthorInfoCardsInSceneFile(path, demoRoot, report);
                    }
                    catch (Exception exception)
                    {
                        report.Scenes.Add(Unexpected(path, exception));
                    }
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
                RestoreSetup(setup);
            }

            return Finish(report);
        }

        /// <summary>
        ///     Runs builders against an already open scene without opening or saving it (tests, custom tools).
        ///     Errors are collected in the result; validation runs when every builder succeeded.
        /// </summary>
        public static DemoSceneResult BuildOpenScene(Scene scene, IReadOnlyList<DemoSceneBuilderInfo> builders,
            string sampleRoot = null)
        {
            DemoSceneResult result = new(scene.path);
            Stopwatch watch = Stopwatch.StartNew();
            DemoSceneBuildContext context = new(scene, sampleRoot) { UseRootMarkers = !IsPagesOnly(builders) };
            context.Begin();
            foreach (DemoSceneBuilderInfo builder in builders)
            {
                result.Builders.Add(builder.Id);
                if (builder.Error != null)
                {
                    result.Errors.Add(builder.Id + ": " + builder.Error);
                    continue;
                }

                context.CurrentBuilder = builder.Id;
                try
                {
                    builder.Invoke(context);
                }
                catch (Exception exception)
                {
                    Exception cause = exception is TargetInvocationException invocation && invocation.InnerException != null
                        ? invocation.InnerException
                        : exception;
                    result.Errors.Add(builder.Id + " threw " + cause.GetType().Name + ": " + cause.Message);
                    Debug.LogException(cause);
                }
            }

            CompleteBuild(context, result);
            result.Milliseconds = watch.Elapsed.TotalMilliseconds;
            return result;
        }

        /// <summary>
        ///     Runs one build delegate against an already open scene without opening or saving it, with the same
        ///     prune and validation pass as a registered builder (tests, custom tools, one-off migrations).
        /// </summary>
        /// <param name="useRootMarkers">False builds marker-less roots, as for NeoxiderPages scenes.</param>
        public static DemoSceneResult BuildOpenScene(Scene scene, Action<DemoSceneBuildContext> build,
            bool useRootMarkers = true, string builderId = "DemoSceneRunner.BuildOpenScene")
        {
            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            DemoSceneResult result = new(scene.path);
            result.Builders.Add(builderId);
            Stopwatch watch = Stopwatch.StartNew();
            DemoSceneBuildContext context = new(scene) { UseRootMarkers = useRootMarkers, CurrentBuilder = builderId };
            context.Begin();
            try
            {
                build(context);
            }
            catch (Exception exception)
            {
                result.Errors.Add(builderId + " threw " + exception.GetType().Name + ": " + exception.Message);
                Debug.LogException(exception);
            }

            CompleteBuild(context, result);
            result.Milliseconds = watch.Elapsed.TotalMilliseconds;
            return result;
        }

        /// <summary>
        ///     Authors a standalone info card for every <see cref="ModuleDemoSceneInfo" /> of an open scene that has
        ///     none (or only a previously generated one). Returns null when there is nothing to do. Does not save.
        /// </summary>
        public static DemoSceneResult AuthorInfoCards(Scene scene, string sampleRoot)
        {
            List<ModuleDemoSceneInfo> targets = new();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ModuleDemoSceneInfo info in root.GetComponentsInChildren<ModuleDemoSceneInfo>(true))
                {
                    if (!info.HasAuthoredOverlay || IsInfoPassCard(info))
                    {
                        targets.Add(info);
                    }
                }
            }

            if (targets.Count == 0)
            {
                return null;
            }

            targets.Sort((a, b) => string.CompareOrdinal(DemoSceneBuildContext.PathOf(a.transform),
                DemoSceneBuildContext.PathOf(b.transform)));
            DemoSceneResult result = new(scene.path);
            result.Builders.Add(InfoCardPassId);
            Stopwatch watch = Stopwatch.StartNew();
            DemoSceneBuildContext context = new(scene, sampleRoot) { CurrentBuilder = InfoCardPassId };
            context.Begin();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    DemoSceneKit.InfoCardCanvas(context, targets[i], null, i == 0 ? "Info Card" : "Info Card " + (i + 1));
                }
            }
            catch (Exception exception)
            {
                result.Errors.Add(InfoCardPassId + " threw " + exception.GetType().Name + ": " + exception.Message);
                Debug.LogException(exception);
            }

            CompleteBuild(context, result);
            result.Milliseconds = watch.Elapsed.TotalMilliseconds;
            return result;
        }

        private static bool IsInfoPassCard(ModuleDemoSceneInfo info)
        {
            for (Transform node = info.OverlayRoot != null ? info.OverlayRoot.transform : null; node != null; node = node.parent)
            {
                if (DemoSceneBuildContext.IsGeneratedRoot(node.gameObject))
                {
                    return DemoSceneBuildContext.TryGetRootKey(node.gameObject, out string key) &&
                           key.StartsWith("Info Card", StringComparison.Ordinal);
                }
            }

            return false;
        }

        /// <summary>
        ///     True when every builder targets the NeoxiderPages sample: those scenes must import without the Demo
        ///     sample assembly, so they get marker-less roots and no Demo sample components.
        /// </summary>
        private static bool IsPagesOnly(IReadOnlyList<DemoSceneBuilderInfo> builders)
        {
            if (builders == null || builders.Count == 0)
            {
                return false;
            }

            foreach (DemoSceneBuilderInfo builder in builders)
            {
                if (builder.Sample != DemoSample.NeoxiderPages)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CompleteBuild(DemoSceneBuildContext context, DemoSceneResult result)
        {
            try
            {
                context.Finish();
            }
            catch (Exception exception)
            {
                result.Errors.Add("Finish failed: " + exception.Message);
                Debug.LogException(exception);
            }

            if (result.Errors.Count == 0)
            {
                Validate(context, result);
            }

            result.Created = context.Created;
            result.Reused = context.Reused;
            result.Removed = context.Removed;
            result.Adopted = context.Adopted;
            result.Notes.AddRange(context.Notes);
            result.Warnings.AddRange(context.Warnings);
        }

        private static DemoSceneResult RebuildSceneFile(string scenePath, List<DemoSceneBuilderInfo> builders)
        {
            string createdNote = null;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                if (!TryCreateSceneAsset(scenePath, out string createError))
                {
                    DemoSceneResult missing = new(scenePath);
                    foreach (DemoSceneBuilderInfo builder in builders)
                    {
                        missing.Builders.Add(builder.Id);
                    }

                    missing.Errors.Add("Scene asset not found and could not be created: " + scenePath + " (" +
                                       createError + ").");
                    Log(missing);
                    return missing;
                }

                createdNote = "Created the missing scene asset " + scenePath + " (empty scene).";
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (Exception exception)
            {
                DemoSceneResult failed = new(scenePath);
                failed.Errors.Add("Could not open the scene: " + exception.Message);
                Log(failed);
                return failed;
            }

            string sampleRoot = builders.Count > 0 && TryResolveSampleRoot(builders[0], out string root) ? root : null;
            DemoSceneResult result = BuildOpenScene(scene, builders, sampleRoot);
            if (createdNote != null)
            {
                result.Notes.Add(createdNote);
            }

            SaveIfClean(scene, result);
            Log(result);
            return result;
        }

        /// <summary>
        ///     Creates an empty scene asset at <paramref name="scenePath" /> ("Assets/....unity"), so a new builder needs
        ///     no hand-made .unity file; <see cref="RebuildAll" /> calls it for every missing builder scene. Missing
        ///     folders are created through the AssetDatabase, so they get their .meta files. Opens the new scene in
        ///     Single mode (the open scenes are replaced without a prompt). A scene created for a build that then fails
        ///     stays on disk as an empty scene, which the next pass reuses.
        /// </summary>
        public static bool TryCreateSceneAsset(string scenePath, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(scenePath) || !scenePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                error = "the path must be an Assets/....unity path";
                return false;
            }

            try
            {
                string folder = scenePath.Substring(0, scenePath.LastIndexOf('/'));
                DemoSceneAssets.EnsureFolder(folder);
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    error = "could not create the folder " + folder;
                    return false;
                }

                // WHY Single mode: the runner opens every scene in Single mode anyway (interactive runs saved or
                // restore the user's scenes around the pass), and an additive NewScene throws next to an unsaved
                // untitled scene in batch runs.
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    error = "EditorSceneManager.SaveScene returned false";
                    return false;
                }

                AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    error = "the saved scene was not imported";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        private static List<string> CollectInfoCardScenes(string demoRoot, HashSet<string> builderScenes,
            List<string> filter, HashSet<string> matchedFilters)
        {
            List<string> paths = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { demoRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (builderScenes.Contains(path) || !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (filter == null || MatchAny(path, filter, demoRoot, matchedFilters))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        private static void AuthorInfoCardsInSceneFile(string path, string demoRoot, DemoSceneRunReport report)
        {
            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            catch (Exception exception)
            {
                DemoSceneResult failed = new(path);
                failed.Errors.Add("Could not open the scene: " + exception.Message);
                report.Scenes.Add(failed);
                Log(failed);
                return;
            }

            DemoSceneResult result = AuthorInfoCards(scene, demoRoot);
            if (result == null)
            {
                return;
            }

            SaveIfClean(scene, result);
            report.Scenes.Add(result);
            Log(result);
        }

        private static DemoSceneResult Unexpected(string scenePath, Exception exception)
        {
            DemoSceneResult failed = new(scenePath);
            failed.Errors.Add("Unexpected " + exception.GetType().Name + ": " + exception.Message);
            Debug.LogException(exception);
            Log(failed);
            return failed;
        }

        private static void SaveIfClean(Scene scene, DemoSceneResult result)
        {
            if (!result.Success)
            {
                result.Notes.Add("Not saved because of errors; the scene file on disk is unchanged.");
                return;
            }

            result.Saved = EditorSceneManager.SaveScene(scene);
            if (!result.Saved)
            {
                result.Errors.Add("EditorSceneManager.SaveScene returned false for " + scene.path + ".");
            }
        }

        private static SortedDictionary<string, List<DemoSceneBuilderInfo>> GroupBuilders(DemoSceneRunReport report)
        {
            SortedDictionary<string, List<DemoSceneBuilderInfo>> scenes = new(StringComparer.OrdinalIgnoreCase);
            List<DemoSceneBuilderInfo> discovered = DiscoverBuilders();
            if (discovered.Count == 0)
            {
                report.Errors.Add("No [DemoSceneBuilder] methods were found (is Neo.Demo.Editor compiled?).");
            }

            foreach (DemoSceneBuilderInfo info in discovered)
            {
                if (info.Error != null)
                {
                    report.Errors.Add("Invalid builder " + info.Id + ": " + info.Error);
                    continue;
                }

                if (!TryResolve(info, out string path, out string error))
                {
                    report.Errors.Add("Builder " + info.Id + ": " + error);
                    continue;
                }

                if (!scenes.TryGetValue(path, out List<DemoSceneBuilderInfo> list))
                {
                    list = new List<DemoSceneBuilderInfo>();
                    scenes.Add(path, list);
                }

                list.Add(info);
            }

            return scenes;
        }

        private static List<string> NormalizeFilter(IReadOnlyCollection<string> filter)
        {
            if (filter == null)
            {
                return null;
            }

            List<string> result = new();
            foreach (string raw in filter)
            {
                string wanted = DemoSampleRoots.Normalize(raw);
                if (wanted.Length > 0 && !result.Contains(wanted))
                {
                    result.Add(wanted);
                }
            }

            return result;
        }

        private static void ApplyFilter(SortedDictionary<string, List<DemoSceneBuilderInfo>> scenes, List<string> filter,
            string demoRoot, HashSet<string> matchedFilters)
        {
            if (filter == null)
            {
                return;
            }

            List<string> drop = new();
            foreach (string path in scenes.Keys)
            {
                if (!MatchAny(path, filter, demoRoot, matchedFilters))
                {
                    drop.Add(path);
                }
            }

            foreach (string path in drop)
            {
                scenes.Remove(path);
            }
        }

        private static bool MatchAny(string path, List<string> filter, string demoRoot, HashSet<string> matchedFilters)
        {
            bool any = false;
            foreach (string wanted in filter)
            {
                if (Matches(path, wanted, demoRoot))
                {
                    matchedFilters.Add(wanted);
                    any = true;
                }
            }

            return any;
        }

        private static bool Matches(string path, string wanted, string demoRoot)
        {
            if (string.Equals(path, wanted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, demoRoot + "/" + wanted, StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/" + wanted, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string file = System.IO.Path.GetFileName(path);
            return string.Equals(file, wanted, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), wanted, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryResolve(DemoSceneBuilderInfo info, out string path, out string error)
        {
            try
            {
                path = info.ResolveAssetPath();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                path = null;
                error = exception.Message;
                return false;
            }
        }

        private static bool TryResolveSampleRoot(DemoSceneBuilderInfo info, out string root)
        {
            try
            {
                root = DemoSampleRoots.Resolve(info.Sample);
                return true;
            }
            catch (Exception)
            {
                root = null;
                return false;
            }
        }

        private static void Validate(DemoSceneBuildContext context, DemoSceneResult result)
        {
            int listeners = 0;
            SortedSet<char> missingGlyphs = new();
            int glyphLabels = 0;
            string firstGlyphLabel = null;
            foreach (GameObject root in context.TouchedRoots)
            {
                if (root == null)
                {
                    continue;
                }

                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        result.Errors.Add("Missing script under '" + root.name + "'.");
                        continue;
                    }

                    listeners += ValidatePersistentCalls(component, result);
                    ValidateAssets(component, context, result);
                    if (component is TMP_Text label && CollectMissingGlyphs(label, missingGlyphs))
                    {
                        glyphLabels++;
                        firstGlyphLabel ??= DemoSceneBuildContext.PathOf(label.transform);
                    }
                }
            }

            if (glyphLabels > 0)
            {
                result.Warnings.Add("Characters missing from the static font atlas: " + DescribeGlyphs(missingGlyphs) +
                                    " in " + glyphLabels + " label(s), first '" + firstGlyphLabel + "'. TMP draws them " +
                                    "from a dynamic fallback at runtime (and may modify that font asset); prefer " +
                                    "characters of the atlas.");
            }

            HashSet<Component> validated = new();
            foreach (Component component in context.ValidatedComponents)
            {
                if (component != null && !DemoSceneBuildContext.IsGenerated(component.gameObject) && validated.Add(component))
                {
                    listeners += ValidatePersistentCalls(component, result);
                }
            }

            foreach (DemoTrackedEvent tracked in context.TrackedEvents)
            {
                if (tracked.Owner != null && !validated.Contains(tracked.Owner) &&
                    !DemoSceneBuildContext.IsGenerated(tracked.Owner.gameObject))
                {
                    listeners += ValidateTrackedEvent(tracked, result);
                }
            }

            if (!context.UseRootMarkers)
            {
                ValidateWithoutDemoAssembly(context, result);
            }

            ValidateNetworkIdentities(context, result);

            foreach (GameObject sceneRoot in context.Scene.GetRootGameObjects())
            {
                foreach (ModuleDemoSceneInfo info in sceneRoot.GetComponentsInChildren<ModuleDemoSceneInfo>(true))
                {
                    if (!info.HasAuthoredOverlay)
                    {
                        result.Warnings.Add("ModuleDemoSceneInfo on '" + DemoSceneBuildContext.PathOf(info.transform) +
                                            "' has no authored card (call frame.InfoCard(info)).");
                    }
                }
            }

            result.Listeners = listeners;
        }

        private static int ValidatePersistentCalls(Component component, DemoSceneResult result)
        {
            int count = 0;
            SerializedObject serialized = new(component);
            SerializedProperty iterator = serialized.GetIterator();
            bool enter = true;
            while (iterator.Next(enter))
            {
                enter = iterator.propertyType == SerializedPropertyType.Generic;
                if (!enter || iterator.name != "m_PersistentCalls")
                {
                    continue;
                }

                enter = false;
                SerializedProperty calls = iterator.FindPropertyRelative("m_Calls");
                if (calls == null || !calls.isArray)
                {
                    continue;
                }

                for (int i = 0; i < calls.arraySize; i++)
                {
                    count++;
                    SerializedProperty call = calls.GetArrayElementAtIndex(i);
                    Object target = call.FindPropertyRelative("m_Target")?.objectReferenceValue;
                    string method = call.FindPropertyRelative("m_MethodName")?.stringValue;
                    string where = DemoSceneBuildContext.PathOf(component.transform) + " (" + component.GetType().Name + "." +
                                   iterator.propertyPath.Replace(".m_PersistentCalls", string.Empty) + ")";
                    if (target == null)
                    {
                        result.Errors.Add("Persistent listener without a target on " + where + ".");
                    }
                    else if (string.IsNullOrEmpty(method) || !HasMethod(target.GetType(), method))
                    {
                        result.Errors.Add("Persistent listener '" + method + "' not found on " + target.GetType().Name +
                                          " for " + where + ".");
                    }
                }
            }

            return count;
        }

        /// <summary>
        ///     Adds the characters of <paramref name="label" /> that its static font atlas lacks (rich-text tags and
        ///     control characters included in the scan are ASCII and present). Dynamic fonts are skipped.
        /// </summary>
        private static bool CollectMissingGlyphs(TMP_Text label, SortedSet<char> missing)
        {
            TMP_FontAsset font = label.font;
            string text = label.text;
            if (font == null || string.IsNullOrEmpty(text) || font.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                return false;
            }

            bool any = false;
            foreach (char character in text)
            {
                if (character < ' ' || char.IsSurrogate(character) || font.HasCharacter(character, false, false))
                {
                    continue;
                }

                missing.Add(character);
                any = true;
            }

            return any;
        }

        private static string DescribeGlyphs(SortedSet<char> glyphs)
        {
            StringBuilder builder = new();
            foreach (char glyph in glyphs)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append('\'').Append(glyph).Append("' (U+")
                    .Append(((int)glyph).ToString("X4", CultureInfo.InvariantCulture)).Append(')');
            }

            return builder.ToString();
        }

        /// <summary>Calls of an event on a hand-authored component that a builder re-owned (ctx.OwnListeners / TrackListeners).</summary>
        private static int ValidateTrackedEvent(DemoTrackedEvent tracked, DemoSceneResult result)
        {
            int count = tracked.Event.GetPersistentEventCount();
            string where = DemoSceneBuildContext.PathOf(tracked.Owner.transform) + " (" + tracked.Owner.GetType().Name +
                           " event " + tracked.Event.GetType().Name + ")";
            for (int i = 0; i < count; i++)
            {
                Object target = tracked.Event.GetPersistentTarget(i);
                string method = tracked.Event.GetPersistentMethodName(i);
                if (target == null)
                {
                    result.Errors.Add("Persistent listener '" + method + "' without a target on " + where + ".");
                }
                else if (string.IsNullOrEmpty(method) || !HasMethod(target.GetType(), method))
                {
                    string message = "Persistent listener '" + method + "' not found on " + target.GetType().Name +
                                     " for " + where + ".";
                    // WHY: a call into a hand-authored object is the author's wiring, not the builder's output.
                    if (DemoSceneBuildContext.IsGeneratedTarget(target))
                    {
                        result.Errors.Add(message);
                    }
                    else
                    {
                        result.Warnings.Add(message + " (hand-authored call, kept)");
                    }
                }
            }

            return count;
        }

        /// <summary>
        ///     Scenes with marker-less roots (the NeoxiderPages sample) must open without the Demo sample assembly:
        ///     any component from it would be a missing script in a project that imported only that sample.
        /// </summary>
        private static void ValidateWithoutDemoAssembly(DemoSceneBuildContext context, DemoSceneResult result)
        {
            Assembly demoAssembly = typeof(DemoGeneratedRoot).Assembly;
            foreach (GameObject sceneRoot in context.Scene.GetRootGameObjects())
            {
                foreach (Component component in sceneRoot.GetComponentsInChildren<Component>(true))
                {
                    if (component != null && component.GetType().Assembly == demoAssembly)
                    {
                        result.Errors.Add(component.GetType().Name + " on '" +
                                          DemoSceneBuildContext.PathOf(component.transform) + "' comes from the Demo " +
                                          "sample assembly (" + demoAssembly.GetName().Name + "); this scene must also " +
                                          "work when only its own sample is imported.");
                    }
                }
            }
        }

        /// <summary>
        ///     Mirror stops Play Mode for scene NetworkIdentity objects whose sceneId was never saved, so every scene
        ///     identity gets a stable id (scene path + hierarchy path) when it has none and the id is recorded as a
        ///     prefab-instance override.
        /// </summary>
        private static void ValidateNetworkIdentities(DemoSceneBuildContext context, DemoSceneResult result)
        {
#if MIRROR
            foreach (Mirror.NetworkIdentity identity in context.FindAll<Mirror.NetworkIdentity>(true))
            {
                string path = DemoSceneBuildContext.PathOf(identity.transform);
                if (identity.sceneId == 0)
                {
                    identity.sceneId = StableSceneId(context.Scene.path + "/" + path);
                    result.Notes.Add("Assigned a stable sceneId to the NetworkIdentity on '" + path + "'.");
                }

                // WHY: Mirror assigns ids from OnValidate in memory only; without a recorded override a NetworkIdentity
                // inside a prefab instance (the --System-- Money) was saved with 0 and Play Mode stopped on entry.
                if (PrefabUtility.IsPartOfPrefabInstance(identity))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
                }

                EditorUtility.SetDirty(identity);
            }
#endif
        }

        private static ulong StableSceneId(string key)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            uint hash = offset;
            for (int i = 0; i < key.Length; i++)
            {
                hash ^= key[i];
                hash *= prime;
            }

            return hash == 0 ? 1UL : hash;
        }

        private static bool HasMethod(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                       BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            for (Type current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(flags))
                {
                    if (method.Name == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ValidateAssets(Component component, DemoSceneBuildContext context, DemoSceneResult result)
        {
            string where = DemoSceneBuildContext.PathOf(component.transform);
            if (context.IsRuntimeAssetAllowed(component))
            {
                return;
            }

            switch (component)
            {
                case Image image:
                    if (image.type == Image.Type.Filled && image.sprite == null)
                    {
                        result.Errors.Add("Filled Image without a sprite on '" + where + "'.");
                    }

                    RequirePersistent(image.sprite, "sprite", where, result);
                    break;
                case SpriteRenderer sprite:
                    RequirePersistent(sprite.sprite, "sprite", where, result);
                    break;
                case TMP_Text text when text.font == null:
                    result.Warnings.Add("Text without a font asset on '" + where + "'.");
                    break;
            }

            if (component is Renderer renderer)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    RequirePersistent(material, "material", where, result);
                }
            }
        }

        private static void RequirePersistent(Object asset, string kind, string where, DemoSceneResult result)
        {
            if (asset != null && !EditorUtility.IsPersistent(asset))
            {
                result.Errors.Add("Runtime-generated " + kind + " '" + asset.name + "' on '" + where +
                                  "' would be lost on save; use an asset (DemoSceneAssets).");
            }
        }

        /// <summary>
        ///     Scene asset paths for tools that walk demo scenes (captures): every registered builder scene, or with a
        ///     filter the matching scenes among the registered ones and every scene of the Demo / NeoxiderPages samples.
        ///     Unmatched filter entries are added to <paramref name="errors" />.
        /// </summary>
        internal static List<string> ResolveScenePaths(IReadOnlyCollection<string> sceneFilter, List<string> errors)
        {
            List<string> result = new();
            if (!DemoSampleRoots.TryResolveDemoRoot(out string demoRoot, out string rootError))
            {
                errors.Add(rootError);
                return result;
            }

            SortedSet<string> registered = new(StringComparer.OrdinalIgnoreCase);
            foreach (DemoSceneBuilderInfo info in DiscoverBuilders())
            {
                if (info.Error == null && TryResolve(info, out string path, out _) &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                {
                    registered.Add(path);
                }
            }

            List<string> filter = NormalizeFilter(sceneFilter);
            if (filter == null || filter.Count == 0)
            {
                result.AddRange(registered);
                return result;
            }

            SortedSet<string> candidates = new(registered, StringComparer.OrdinalIgnoreCase);
            List<string> folders = new() { demoRoot };
            if (TryResolveSample(DemoSample.NeoxiderPages, out string pagesRoot))
            {
                folders.Add(pagesRoot);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", folders.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(path);
                }
            }

            HashSet<string> matched = new(StringComparer.OrdinalIgnoreCase);
            foreach (string path in candidates)
            {
                if (MatchAny(path, filter, demoRoot, matched))
                {
                    result.Add(path);
                }
            }

            foreach (string wanted in filter)
            {
                if (!matched.Contains(wanted))
                {
                    errors.Add("No demo scene matches '" + wanted + "'.");
                }
            }

            return result;
        }

        private static bool TryResolveSample(DemoSample sample, out string root)
        {
            try
            {
                root = DemoSampleRoots.Resolve(sample);
                return true;
            }
            catch (Exception)
            {
                root = null;
                return false;
            }
        }

        internal static void RestoreSetup(SceneSetup[] setup)
        {
            if (setup == null || setup.Length == 0)
            {
                return;
            }

            foreach (SceneSetup entry in setup)
            {
                if (string.IsNullOrEmpty(entry.path))
                {
                    return;
                }
            }

            try
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static DemoSceneRunReport Finish(DemoSceneRunReport report)
        {
            if (report.Succeeded)
            {
                Debug.Log(report.Summary());
            }
            else
            {
                Debug.LogError(report.Summary());
            }

            return report;
        }

        private static void Log(DemoSceneResult result)
        {
            if (result.Success)
            {
                Debug.Log(LogPrefix + result.Summary());
            }
            else
            {
                Debug.LogError(LogPrefix + result.Summary());
            }
        }

        private static void ShowReport(DemoSceneRunReport report)
        {
            string message = report.Succeeded
                ? (report.Scenes.Count + " scene(s) built and saved.")
                : (report.FailedScenes + " scene(s) failed, " + report.Errors.Count + " error(s). See the Console.");
            EditorUtility.DisplayDialog("Neoxider demo scenes", message, "OK");
        }
    }
}
