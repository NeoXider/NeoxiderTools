using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
#if MIRROR
using Mirror;
using Neo.Network;
#endif
using Neo.Core.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    public sealed class SampleAssetValidationTests
    {
        private static readonly Regex MonoScriptReferenceRegex =
            new(@"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}",
                RegexOptions.Compiled);

        private static readonly Regex MissingMonoScriptReferenceRegex =
            new(@"m_Script:\s*\{fileID:\s*0\}", RegexOptions.Compiled);

        private static readonly Regex SourcePrefabReferenceRegex =
            new(@"m_SourcePrefab:\s*\{fileID:\s*100100000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}",
                RegexOptions.Compiled);

        private static readonly Regex CurveKeyRegex =
            new(@"time:\s*(?<time>-?\d+(?:\.\d+)?)\s*\r?\n\s*value:\s*(?<value>-?\d+(?:\.\d+)?)",
                RegexOptions.Compiled);

        private static readonly Regex YamlDocumentHeaderRegex =
            new(@"^--- !u!(?<class>\d+) &(?<id>-?\d+)(?<stripped> stripped)?", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex PrefabInstanceLinkRegex =
            new(@"m_PrefabInstance:\s*\{fileID:\s*(-?\d+)\}", RegexOptions.Compiled);

        private const string MonoBehaviourClassId = "114";
        private const string PrefabInstanceClassId = "1001";
        private const int MaxPrefabNesting = 16;
        private const int MaxCopyProbes = 64;

        [Test]
        public void SamplePrefabs_WithNetworkBehaviours_HaveNetworkIdentityInParents()
        {
#if MIRROR
            List<string> failures = new List<string>();
            int inspectedBehaviours = 0;
            string[] prefabGuids = FindAssetsInAssetDatabaseSampleRoots("t:Prefab", false, out string[] skippedSamples);

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    failures.Add(path + ": prefab could not be loaded.");
                    continue;
                }

                inspectedBehaviours += InspectNetworkBehaviours(prefab, path, failures);
            }

            // WHY: the legacy test identity and every prefab check stay, but current samples author network
            // components in scenes (for example Money), so the nonempty typed census includes scenes.
            string[] sceneGuids = FindAssetsInAssetDatabaseSampleRoots("t:Scene");
            int inspectedScenes = 0;
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SampleSceneCoverageTests.RequireNativeSceneAsset(path);
                using (Audit20261023NeoEditorTestsPreviewSceneScope preview = new Audit20261023NeoEditorTestsPreviewSceneScope(path))
                {
                    GameObject[] roots = preview.Scene.GetRootGameObjects();
                    Assert.That(roots, Is.Not.Empty, path);
                    foreach (GameObject root in roots)
                        inspectedBehaviours += InspectNetworkBehaviours(root, path, failures);
                    inspectedScenes++;
                }
            }

            Assert.That(inspectedScenes, Is.EqualTo(sceneGuids.Length).And.GreaterThan(0));
            if (inspectedBehaviours == 0 && skippedSamples.Length > 0)
                Assert.Ignore("The visible samples hold no NetworkBehaviour; the samples that may are hidden or not imported: " +
                    string.Join(", ", skippedSamples) + ".");
            Assert.That(inspectedBehaviours, Is.GreaterThan(0), "No native sample NetworkBehaviour inspected in prefabs or scenes.");
            TestContext.WriteLine("Typed sample network census: " + inspectedBehaviours +
                " NetworkBehaviours; " + prefabGuids.Length + " prefabs; " + inspectedScenes + " scenes.");
            AssertNoFailures(failures);
#else
            Assert.Ignore("Mirror is required for the typed NetworkBehaviour/NetworkIdentity sample scan. Generic sample asset checks run separately.");
#endif
        }

#if MIRROR
        private static int InspectNetworkBehaviours(GameObject root, string path, List<string> failures)
        {
            NetworkBehaviour[] behaviours = root.GetComponentsInChildren<NetworkBehaviour>(true);
            foreach (NetworkBehaviour behaviour in behaviours)
            {
                bool requiresIdentity = RequiresNetworkIdentity(behaviour);
                TestContext.WriteLine("Typed network component: " + path + ": " + behaviour.GetType().FullName +
                    (requiresIdentity ? " [network identity required]" : " [optional solo configuration]"));
                if (requiresIdentity && behaviour.GetComponentInParent<NetworkIdentity>(true) == null)
                    failures.Add($"{path}: {behaviour.GetType().FullName} on '{behaviour.name}' has no NetworkIdentity in parents.");
            }
            return behaviours.Length;
        }

        private static bool RequiresNetworkIdentity(NetworkBehaviour behaviour)
        {
            return !(behaviour is INeoOptionalNetworked optional) || optional.IsNetworked;
        }

        [Test]
        public void SampleNetworkCensus_OptionalSoloThenEnabled_RequiresIdentityOnlyWhenConfigured()
        {
            Assert.That(Application.isPlaying, Is.False);
            GameObject parent = new GameObject("OwnedNetworkCensusParent");
            try
            {
                GameObject child = new GameObject("OwnedOptionalCounter");
                child.transform.SetParent(parent.transform);
                Neo.Tools.Counter counter = child.AddComponent<Neo.Tools.Counter>();
                counter.isNetworked = false;
                Assert.That(counter.GetComponentInParent<NetworkIdentity>(true), Is.Null);
                Assert.That(RequiresNetworkIdentity(counter), Is.False);
                List<string> failures = new List<string>();
                Assert.That(InspectNetworkBehaviours(parent, "OwnedCensus.prefab", failures), Is.EqualTo(1));
                Assert.That(failures, Is.Empty, "A typed optional solo component still counts without requiring identity.");

                counter.isNetworked = true;
                Assert.That(RequiresNetworkIdentity(counter), Is.True);
                Assert.That(InspectNetworkBehaviours(parent, "OwnedCensus.prefab", failures), Is.EqualTo(1));
                CollectionAssert.AreEqual(new[]
                {
                    "OwnedCensus.prefab: Neo.Tools.Counter on 'OwnedOptionalCounter' has no NetworkIdentity in parents."
                }, failures);

                parent.AddComponent<NetworkIdentity>();
                failures.Clear();
                Assert.That(InspectNetworkBehaviours(parent, "OwnedCensus.prefab", failures), Is.EqualTo(1));
                Assert.That(failures, Is.Empty, "An actual ancestor identity satisfies the configured requirement.");
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }

        [Test]
        public void SampleNetworkCensus_NonOptionalBehaviour_StillRequiresAnActualIdentity()
        {
            Assert.That(Application.isPlaying, Is.False);
            GameObject parent = new GameObject("OwnedRequiredNetworkCensus", typeof(NetworkIdentity));
            try
            {
                GameObject child = new GameObject("OwnedRequiredNetworkBehaviour");
                child.transform.SetParent(parent.transform);
                child.SetActive(false);
                NetworkBehaviour behaviour = child.AddComponent<Audit20261023NeoNetworkSingleton>();
                Assert.That(behaviour is INeoOptionalNetworked, Is.False);
                Assert.That(RequiresNetworkIdentity(behaviour), Is.True);
                Assert.That(behaviour.GetComponentInParent<NetworkIdentity>(true), Is.SameAs(parent.GetComponent<NetworkIdentity>()));
                List<string> failures = new List<string>();
                Assert.That(InspectNetworkBehaviours(parent, "OwnedRequired.prefab", failures), Is.EqualTo(1));
                Assert.That(failures, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }
#endif

        [Test]
        public void SamplePrefabs_HaveNoMissingMonoBehaviours()
        {
            List<string> failures = new List<string>();
            string[] prefabGuids = FindAssetsInAssetDatabaseSampleRoots("t:Prefab");
            HashSet<string> hiddenSampleGuids = null;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    failures.Add(path + ": prefab could not be loaded.");
                    continue;
                }

                int missingScriptCount = CountMissingMonoBehavioursRecursive(prefab);
                if (missingScriptCount > 0)
                {
                    ReportMissingScripts(path, missingScriptCount, ref hiddenSampleGuids, failures);
                }
            }

            AssertNoFailures(failures);
        }

        [Test]
        public void PackagePrefabs_HaveNoMissingMonoBehaviours()
        {
            List<string> failures = new List<string>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", GetExistingPackagePrefabRoots());
            Assert.That(prefabGuids, Is.Not.Empty);

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    failures.Add(path + ": prefab could not be loaded.");
                    continue;
                }

                int missingScriptCount = CountMissingMonoBehavioursRecursive(prefab);
                if (missingScriptCount > 0)
                {
                    failures.Add($"{path}: contains {missingScriptCount} missing MonoBehaviour script(s).");
                }
            }

            AssertNoFailures(failures);
        }

        [Test]
        public void SampleYamlAssets_DoNotReferenceMissingMonoScripts()
        {
            List<string> failures = new List<string>();
            int inspected = 0;
            foreach (string path in EnumerateSampleYamlAssets())
            {
                inspected++;
                string text = File.ReadAllText(Audit20261023NeoEditorTestsPaths.ToDiskPath(path));
                if (MissingMonoScriptReferenceRegex.IsMatch(text))
                {
                    failures.Add($"{path}: contains direct missing MonoBehaviour script reference.");
                }

                foreach (Match match in MonoScriptReferenceRegex.Matches(text))
                {
                    string guid = match.Groups[1].Value;
                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)) &&
                        !GuidExistsInHiddenSampleMeta(guid))
                    {
                        failures.Add($"{path}: references missing MonoScript guid {guid}.");
                    }
                }
            }

            Assert.That(inspected, Is.GreaterThan(0), "No sample YAML assets inspected.");
            AssertNoFailures(failures);
        }

        [Test]
        public void SampleTerrainData_HasNoMissingTreePrefabs()
        {
            List<string> failures = new List<string>();
            string[] terrainGuids = FindAssetsInAssetDatabaseSampleRoots("t:TerrainData");

            foreach (string guid in terrainGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TerrainData terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (terrain == null)
                {
                    failures.Add(path + ": terrain could not be loaded.");
                    continue;
                }

                TreePrototype[] prototypes = terrain.treePrototypes ?? new TreePrototype[0];
                for (int i = 0; i < prototypes.Length; i++)
                {
                    if (prototypes[i].prefab == null)
                    {
                        failures.Add($"{path}: tree prefab at index {i} is missing.");
                    }
                }

                foreach (TreeInstance instance in terrain.treeInstances)
                {
                    if (instance.prototypeIndex < 0 || instance.prototypeIndex >= prototypes.Length)
                    {
                        failures.Add(
                            $"{path}: tree instance references invalid prototype index {instance.prototypeIndex}.");
                    }
                }
            }

            AssertNoFailures(failures);
        }

        [Test]
        public void SampleScenes_HaveNoMissingMonoBehaviours()
        {
            List<string> failures = new List<string>();
            int inspected = 0;
            string[] sceneGuids = FindAssetsInAssetDatabaseSampleRoots("t:Scene");
            HashSet<string> hiddenSampleGuids = null;

            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SampleSceneCoverageTests.RequireNativeSceneAsset(path);
                using (Audit20261023NeoEditorTestsPreviewSceneScope preview = new(path))
                {
                    Scene scene = preview.Scene;
                    GameObject[] roots = scene.GetRootGameObjects();
                    Assert.That(roots, Is.Not.Empty, path);
                    inspected++;
                    int missingScriptCount = 0;
                    for (int i = 0; i < roots.Length; i++)
                    {
                        missingScriptCount += CountMissingMonoBehavioursRecursive(roots[i]);
                    }

                    if (missingScriptCount > 0)
                    {
                        ReportMissingScripts(path, missingScriptCount, ref hiddenSampleGuids, failures);
                    }
                }
            }
            Assert.That(inspected, Is.EqualTo(sceneGuids.Length).And.GreaterThan(0),
                "Every discovered native sample scene must be inspected.");
            AssertNoFailures(failures);
        }

        [Test]
        public void ProgressionDemo_LevelCurveUsesIncreasingXpRequirements()
        {
            List<string> failures = new List<string>();
            string[] guids = FindAssetsInAssetDatabaseSampleRoots("DemoLevelCurve", true);
            List<string> hiddenCurvePaths = FindHiddenSampleAssets("DemoLevelCurve.asset");
            Assert.That(guids.Length + hiddenCurvePaths.Count, Is.GreaterThan(0),
                "DemoLevelCurve asset was not found in active sample roots.");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelCurveDefinition curve = AssetDatabase.LoadAssetAtPath<LevelCurveDefinition>(path);
                if (curve == null)
                {
                    failures.Add(path + ": expected a native LevelCurveDefinition.");
                    continue;
                }

                int level1 = curve.GetRequiredXpForLevel(1);
                int level2 = curve.GetRequiredXpForLevel(2);
                int level3 = curve.GetRequiredXpForLevel(3);
                int firstStep = level2 - level1;
                int secondStep = level3 - level2;

                if (curve.Mode != LevelCurveMode.Curve)
                {
                    failures.Add($"{path}: expected Curve mode so the demo uses increasing XP requirements.");
                }

                if (firstStep <= 0 || secondStep <= firstStep)
                {
                    failures.Add($"{path}: expected increasing XP steps, got {firstStep} then {secondStep}.");
                }

                if (curve.EvaluateLevel(175) != 2 || curve.GetXpToNextLevel(175) != 75)
                {
                    failures.Add($"{path}: expected 175 XP to be level 2 with 75 XP to next level.");
                }

                if (curve.EvaluateLevel(2675) != 8 || curve.GetXpToNextLevel(2675) != 925)
                {
                    failures.Add($"{path}: expected 2675 XP to be level 8 with 925 XP to next level.");
                }
            }

            foreach (string path in hiddenCurvePaths)
            {
                ValidateHiddenDemoLevelCurve(path, failures);
            }

            AssertNoFailures(failures);
        }

        private static void AssertNoFailures(IReadOnlyList<string> failures)
        {
            if (failures.Count == 0)
            {
                return;
            }

            Assert.Fail(string.Join("\n", failures));
        }

        private static IEnumerable<string> EnumerateSampleYamlAssets()
        {
            foreach (string pattern in new[] { "*.prefab", "*.unity" })
                foreach (string path in Audit20261023NeoEditorTestsPaths.EnumerateSampleFiles(pattern))
                    yield return path;
        }

        internal static int CountMissingMonoBehavioursRecursive(GameObject root)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);
            }

            return count;
        }

        private static string[] GetExistingSampleRoots() => Audit20261023NeoEditorTestsPaths.SampleRoots();

        private static string[] GetExistingAssetDatabaseSampleRoots()
        {
            List<string> roots = new List<string>();
            foreach (string root in Audit20261023NeoEditorTestsPaths.SampleRoots(false))
            {
                if (AssetDatabase.IsValidFolder(root))
                {
                    roots.Add(root);
                }
            }

            return roots.ToArray();
        }

        internal static string[] FindAssetsInAssetDatabaseSampleRoots(string filter, bool allowHiddenPackaging = false)
        {
            return FindAssetsInAssetDatabaseSampleRoots(filter, allowHiddenPackaging, out _);
        }

        /// <summary>
        ///     Finds <paramref name="filter" /> assets in the visible current-version sample roots. Each visible manifest
        ///     sample is validated; samples that are hidden (Samples~) or not imported are skipped and returned in
        ///     <paramref name="skippedSamples" />. The check is ignored only when no sample is visible at all, or when
        ///     the visible subset holds no such asset while a skipped sample may.
        /// </summary>
        internal static string[] FindAssetsInAssetDatabaseSampleRoots(string filter, bool allowHiddenPackaging,
            out string[] skippedSamples)
        {
            skippedSamples = Array.Empty<string>();
            string[] roots = GetExistingAssetDatabaseSampleRoots();
            if (allowHiddenPackaging && roots.Length == 0) return Array.Empty<string>();
            if (!allowHiddenPackaging)
            {
                roots = SelectVisibleNativeSampleRoots(Audit20261023NeoEditorTestsPaths.Manifest, roots, out skippedSamples,
                    FindVisibleNonCanonicalCopy);
            }

            string[] guids = AssetDatabase.FindAssets(filter, roots);
            if (!allowHiddenPackaging && guids.Length == 0 && skippedSamples.Length > 0)
                Assert.Ignore("No " + filter + " assets in the visible samples (" + string.Join(", ", roots) +
                    "); the samples that may hold them are hidden or not imported: " + string.Join(", ", skippedSamples) + ".");
            if (!allowHiddenPackaging) Assert.That(guids, Is.Not.Empty, "No visible sample assets inspected for " + filter);
            return guids;
        }

        /// <summary>
        ///     One visible current-version root per manifest sample. Hidden or not imported samples are reported and
        ///     skipped; the calling test is ignored only when no manifest sample is visible. A skipped sample whose
        ///     assets are visible somewhere else (<paramref name="findVisibleCopy" />, e.g. an old import at
        ///     <c>Assets/Samples/&lt;name&gt;</c>) is reported with that path: it is not the current-version sample, so
        ///     it is not validated as one.
        /// </summary>
        internal static string[] SelectVisibleNativeSampleRoots(Audit20261023NeoEditorTestsPaths.PackageManifest manifest,
            IEnumerable<string> visibleRoots, out string[] skippedSamples,
            Func<Audit20261023NeoEditorTestsPaths.SampleManifest, string> findVisibleCopy = null)
        {
            Audit20261023NeoEditorTestsPaths.TrySelectNativeSampleRoots(manifest, visibleRoots,
                out string[] selectedRoots, out string[] missing);
            List<string> skipped = new List<string>();
            List<string> report = new List<string>();
            foreach (Audit20261023NeoEditorTestsPaths.SampleManifest sample in manifest.samples)
            {
                string imported = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, sample);
                string development = Audit20261023NeoEditorTestsPaths.DevelopmentRoot(sample);
                string selected = Array.IndexOf(selectedRoots, imported) >= 0 ? imported
                    : Array.IndexOf(selectedRoots, development) >= 0 ? development : null;
                if (selected != null)
                {
                    report.Add("Native sample " + sample.displayName + " @ " + manifest.version + ": " + selected);
                    continue;
                }

                string identity = Array.Find(missing, entry => entry.StartsWith(sample.displayName + " (", StringComparison.Ordinal))
                    ?? sample.displayName + " (" + manifest.version + "; " + sample.path + ")";
                string copy = findVisibleCopy?.Invoke(sample);
                if (string.IsNullOrEmpty(copy))
                {
                    skipped.Add(identity);
                    report.Add("Skipped sample (hidden or not imported): " + sample.displayName + " @ " + manifest.version);
                }
                else
                {
                    skipped.Add(identity + " [visible at non-canonical path " + copy + "; not validated]");
                    report.Add("Skipped sample " + sample.displayName + " @ " + manifest.version + ": visible at " +
                        "non-canonical path " + copy + ", which is neither " + imported + " nor " + development +
                        "; not validated as the current-version sample.");
                }
            }

            skippedSamples = skipped.ToArray();
            if (selectedRoots.Length == 0)
                Assert.Ignore("No native sample is visible: " + string.Join(", ", skippedSamples) +
                    ". Rename Samples~ to Samples or import the samples; hidden YAML packaging checks run separately.");
            foreach (string line in report) TestContext.WriteLine(line);
            return selectedRoots;
        }

        /// <summary>
        ///     Folder outside the canonical roots where the AssetDatabase sees the packaged sample's assets (their
        ///     GUIDs), or null. Probes at most <see cref="MaxCopyProbes" /> metas of the packaged sample.
        /// </summary>
        private static string FindVisibleNonCanonicalCopy(Audit20261023NeoEditorTestsPaths.SampleManifest sample)
        {
            string packaged = Path.GetFullPath(Path.Combine(Audit20261023NeoEditorTestsPaths.PackageDiskRoot, sample.path))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(packaged)) return null;
            int probed = 0;
            foreach (string meta in Directory.EnumerateFiles(packaged, "*.meta", SearchOption.AllDirectories))
            {
                if (++probed > MaxCopyProbes) break;
                string guid = ReadMetaGuid(meta);
                string visible = string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(visible) || visible.Contains("/Samples~/")) continue;
                string relative = meta.Substring(packaged.Length, meta.Length - packaged.Length - ".meta".Length)
                    .Replace('\\', '/');
                return visible.EndsWith(relative, StringComparison.Ordinal)
                    ? visible.Substring(0, visible.Length - relative.Length)
                    : (Path.GetDirectoryName(visible) ?? visible).Replace('\\', '/');
            }

            return null;
        }

        private static string ReadMetaGuid(string metaPath)
        {
            foreach (string line in File.ReadLines(metaPath))
            {
                if (line.StartsWith("guid: ", StringComparison.Ordinal))
                    return line.Substring("guid: ".Length).Trim();
            }

            return null;
        }

        /// <summary>
        ///     Splits the script and source-prefab GUIDs a scene or prefab YAML references into resolved prefab paths,
        ///     GUIDs that live in a hidden sample, and references that resolve nowhere.
        /// </summary>
        internal static void ClassifyReferences(string yaml, Func<string, string> resolvePath,
            ICollection<string> hiddenSampleGuids, List<string> resolvedPrefabs, List<string> inHiddenSample,
            List<string> missing)
        {
            foreach (Regex pattern in new[] { MonoScriptReferenceRegex, SourcePrefabReferenceRegex })
            {
                foreach (Match match in pattern.Matches(yaml))
                {
                    string guid = match.Groups[1].Value;
                    string path = resolvePath(guid);
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (pattern == SourcePrefabReferenceRegex && !resolvedPrefabs.Contains(path))
                            resolvedPrefabs.Add(path);
                        continue;
                    }

                    List<string> target = hiddenSampleGuids.Contains(guid) ? inHiddenSample : missing;
                    if (!target.Contains(guid)) target.Add(guid);
                }
            }

            if (MissingMonoScriptReferenceRegex.IsMatch(yaml) && !missing.Contains("m_Script {fileID: 0}"))
                missing.Add("m_Script {fileID: 0}");
        }

        /// <summary>
        ///     A visible asset whose missing scripts all come from a hidden sample is reported as skipped, not failed.
        ///     A reference that resolves nowhere fails, and so do missing scripts beyond the hidden-sample script slots
        ///     the YAML accounts for (a renamed class or a file/class name mismatch next to a hidden dependency).
        /// </summary>
        private static void ReportMissingScripts(string path, int missingScriptCount,
            ref HashSet<string> hiddenSampleGuids, List<string> failures)
        {
            hiddenSampleGuids ??= CollectHiddenSampleMetaGuids();
            List<string> inHiddenSample = new List<string>();
            List<string> missing = new List<string>();
            CollectUnresolvedReferences(path, hiddenSampleGuids, new HashSet<string>(StringComparer.Ordinal),
                inHiddenSample, missing);
            if (inHiddenSample.Count > 0 && missing.Count == 0)
            {
                Dictionary<string, string> prefabTexts = new Dictionary<string, string>(StringComparer.Ordinal);
                int hiddenSlots = CountHiddenSampleScriptSlots(ReadAssetText(path, prefabTexts),
                    AssetDatabase.GUIDToAssetPath, prefab => ReadAssetText(prefab, prefabTexts), hiddenSampleGuids);
                if (hiddenSlots >= missingScriptCount)
                {
                    TestContext.WriteLine($"Skipped {path}: its {missingScriptCount} missing script(s) belong to a " +
                        $"hidden sample ({hiddenSlots} hidden-sample script slot(s): " + string.Join(", ", inHiddenSample) + ").");
                    return;
                }

                failures.Add($"{path}: contains {missingScriptCount} missing MonoBehaviour script(s), but only " +
                    $"{hiddenSlots} belong to a hidden sample (" + string.Join(", ", inHiddenSample) + "); " +
                    (missingScriptCount - hiddenSlots) + " are missing for another reason (renamed class, file/class " +
                    "name mismatch or a compile error).");
                return;
            }

            failures.Add($"{path}: contains {missingScriptCount} missing MonoBehaviour script(s)." +
                (missing.Count > 0 ? " Unresolved: " + string.Join(", ", missing) : string.Empty));
        }

        /// <summary>
        ///     How many MonoBehaviour slots a scene or prefab YAML loads with a script only a hidden sample owns:
        ///     its own components plus those of every visible nested prefab, once per instance. Stripped stubs repeat
        ///     a nested prefab's component and are not counted again, except the stubs of a prefab instance whose
        ///     source prefab itself lives in a hidden sample (an upper bound, since that source cannot be read).
        /// </summary>
        internal static int CountHiddenSampleScriptSlots(string yaml, Func<string, string> resolvePath,
            Func<string, string> readPrefab, ICollection<string> hiddenSampleGuids)
        {
            return CountHiddenSampleScriptSlots(yaml, resolvePath, readPrefab, hiddenSampleGuids, 0);
        }

        private static int CountHiddenSampleScriptSlots(string yaml, Func<string, string> resolvePath,
            Func<string, string> readPrefab, ICollection<string> hiddenSampleGuids, int depth)
        {
            if (string.IsNullOrEmpty(yaml) || depth > MaxPrefabNesting) return 0;
            MatchCollection headers = YamlDocumentHeaderRegex.Matches(yaml);
            string[] bodies = new string[headers.Count];
            HashSet<string> hiddenSourceInstances = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            for (int i = 0; i < headers.Count; i++)
            {
                int start = headers[i].Index + headers[i].Length;
                int end = i + 1 < headers.Count ? headers[i + 1].Index : yaml.Length;
                bodies[i] = yaml.Substring(start, end - start);
                if (headers[i].Groups["class"].Value != PrefabInstanceClassId || headers[i].Groups["stripped"].Success)
                    continue;
                Match source = SourcePrefabReferenceRegex.Match(bodies[i]);
                if (!source.Success) continue;
                string guid = source.Groups[1].Value;
                string path = resolvePath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        count += CountHiddenSampleScriptSlots(readPrefab(path), resolvePath, readPrefab, hiddenSampleGuids,
                            depth + 1);
                }
                else if (hiddenSampleGuids.Contains(guid))
                {
                    hiddenSourceInstances.Add(headers[i].Groups["id"].Value);
                }
            }

            for (int i = 0; i < headers.Count; i++)
            {
                if (headers[i].Groups["class"].Value != MonoBehaviourClassId) continue;
                Match script = MonoScriptReferenceRegex.Match(bodies[i]);
                if (!script.Success) continue;
                string guid = script.Groups[1].Value;
                if (!string.IsNullOrEmpty(resolvePath(guid)) || !hiddenSampleGuids.Contains(guid)) continue;
                if (!headers[i].Groups["stripped"].Success)
                {
                    count++;
                    continue;
                }

                Match link = PrefabInstanceLinkRegex.Match(bodies[i]);
                if (link.Success && hiddenSourceInstances.Contains(link.Groups[1].Value)) count++;
            }

            return count;
        }

        private static string ReadAssetText(string path, Dictionary<string, string> cache)
        {
            if (cache.TryGetValue(path, out string text)) return text;
            string diskPath = Audit20261023NeoEditorTestsPaths.ToDiskPath(path);
            text = File.Exists(diskPath) ? File.ReadAllText(diskPath) : string.Empty;
            cache[path] = text;
            return text;
        }

        private static void CollectUnresolvedReferences(string path, ICollection<string> hiddenSampleGuids,
            HashSet<string> visited, List<string> inHiddenSample, List<string> missing)
        {
            string diskPath = Audit20261023NeoEditorTestsPaths.ToDiskPath(path);
            if (!visited.Add(path) || !File.Exists(diskPath))
            {
                return;
            }

            List<string> prefabs = new List<string>();
            ClassifyReferences(File.ReadAllText(diskPath), AssetDatabase.GUIDToAssetPath, hiddenSampleGuids, prefabs,
                inHiddenSample, missing);
            foreach (string prefab in prefabs)
            {
                if (prefab.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    CollectUnresolvedReferences(prefab, hiddenSampleGuids, visited, inHiddenSample, missing);
            }
        }

        private static HashSet<string> CollectHiddenSampleMetaGuids()
        {
            HashSet<string> guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in GetExistingSampleRoots())
            {
                string diskRoot = Audit20261023NeoEditorTestsPaths.ToDiskPath(root);
                if (AssetDatabase.IsValidFolder(root) || !Directory.Exists(diskRoot))
                {
                    continue;
                }

                foreach (string metaPath in Directory.EnumerateFiles(diskRoot, "*.meta", SearchOption.AllDirectories))
                {
                    foreach (string line in File.ReadLines(metaPath))
                    {
                        if (line.StartsWith("guid: ", StringComparison.Ordinal))
                        {
                            guids.Add(line.Substring("guid: ".Length).Trim());
                            break;
                        }
                    }
                }
            }

            return guids;
        }

        private static List<string> FindHiddenSampleAssets(string fileName)
        {
            List<string> paths = new List<string>();
            foreach (string root in GetExistingSampleRoots())
            {
                if (AssetDatabase.IsValidFolder(root) || !Directory.Exists(Audit20261023NeoEditorTestsPaths.ToDiskPath(root)))
                {
                    continue;
                }

                foreach (string path in Directory.EnumerateFiles(Audit20261023NeoEditorTestsPaths.ToDiskPath(root), fileName, SearchOption.AllDirectories))
                {
                    paths.Add(path.Replace('\\', '/'));
                }
            }

            return paths;
        }

        private static void ValidateHiddenDemoLevelCurve(string path, List<string> failures)
        {
            string text = File.ReadAllText(Audit20261023NeoEditorTestsPaths.ToDiskPath(path));
            if (!Regex.IsMatch(text, @"^\s*_mode:\s*1\s*$", RegexOptions.Multiline))
            {
                failures.Add($"{path}: expected Curve mode so the demo uses increasing XP requirements.");
            }

            Dictionary<int, int> levels = new Dictionary<int, int>();
            foreach (Match match in CurveKeyRegex.Matches(text))
            {
                int time = Mathf.RoundToInt(float.Parse(match.Groups["time"].Value,
                    System.Globalization.CultureInfo.InvariantCulture));
                int value = Mathf.RoundToInt(float.Parse(match.Groups["value"].Value,
                    System.Globalization.CultureInfo.InvariantCulture));
                levels[time] = value;
            }

            if (!levels.TryGetValue(1, out int level1) ||
                !levels.TryGetValue(2, out int level2) ||
                !levels.TryGetValue(3, out int level3) ||
                !levels.TryGetValue(8, out int level8) ||
                !levels.TryGetValue(9, out int level9))
            {
                failures.Add($"{path}: expected curve keys for levels 1, 2, 3, 8 and 9.");
                return;
            }

            int firstStep = level2 - level1;
            int secondStep = level3 - level2;
            if (firstStep <= 0 || secondStep <= firstStep)
            {
                failures.Add($"{path}: expected increasing XP steps, got {firstStep} then {secondStep}.");
            }

            if (level2 > 175 || level3 <= 175 || level3 - 175 != 75)
            {
                failures.Add($"{path}: expected 175 XP to be level 2 with 75 XP to next level.");
            }

            if (level8 > 2675 || level9 <= 2675 || level9 - 2675 != 925)
            {
                failures.Add($"{path}: expected 2675 XP to be level 8 with 925 XP to next level.");
            }
        }

        private static bool GuidExistsInHiddenSampleMeta(string guid)
        {
            foreach (string root in GetExistingSampleRoots())
            {
                if (AssetDatabase.IsValidFolder(root) || !Directory.Exists(Audit20261023NeoEditorTestsPaths.ToDiskPath(root)))
                {
                    continue;
                }

                foreach (string metaPath in Directory.EnumerateFiles(Audit20261023NeoEditorTestsPaths.ToDiskPath(root), "*.meta", SearchOption.AllDirectories))
                {
                    if (File.ReadAllText(metaPath).Contains("guid: " + guid))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string[] GetExistingPackagePrefabRoots()
        {
            string root = Audit20261023NeoEditorTestsPaths.PackageAssetRoot + "/Prefabs";
            Assert.That(AssetDatabase.IsValidFolder(root), Is.True, "Package prefab root is unavailable: " + root);
            return new[] { root };
        }
    }
}
