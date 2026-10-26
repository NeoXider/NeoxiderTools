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

        private static readonly Regex CurveKeyRegex =
            new(@"time:\s*(?<time>-?\d+(?:\.\d+)?)\s*\r?\n\s*value:\s*(?<value>-?\d+(?:\.\d+)?)",
                RegexOptions.Compiled);

        [Test]
        public void SamplePrefabs_WithNetworkBehaviours_HaveNetworkIdentityInParents()
        {
#if MIRROR
            List<string> failures = new List<string>();
            int inspectedBehaviours = 0;
            string[] prefabGuids = FindAssetsInAssetDatabaseSampleRoots("t:Prefab");

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

            // Keep the legacy test identity and every prefab check. Current samples author network
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
            var failures = new List<string>();
            string[] prefabGuids = FindAssetsInAssetDatabaseSampleRoots("t:Prefab");

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
        public void PackagePrefabs_HaveNoMissingMonoBehaviours()
        {
            var failures = new List<string>();
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
            var failures = new List<string>();
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
            var failures = new List<string>();
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
                        failures.Add($"{path}: contains {missingScriptCount} missing MonoBehaviour script(s).");
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
            var failures = new List<string>();
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
            string[] roots = GetExistingAssetDatabaseSampleRoots();
            if (allowHiddenPackaging && roots.Length == 0) return Array.Empty<string>();
            if (!allowHiddenPackaging)
            {
                Audit20261023NeoEditorTestsPaths.PackageManifest manifest = Audit20261023NeoEditorTestsPaths.Manifest;
                if (!Audit20261023NeoEditorTestsPaths.TrySelectNativeSampleRoots(manifest, roots,
                    out string[] selectedRoots, out string[] missingIdentities))
                    Assert.Ignore("Native sample identities unavailable: " + string.Join(", ", missingIdentities) +
                        ". Hidden YAML packaging checks run separately.");
                roots = selectedRoots;
                for (int index = 0; index < roots.Length; index++)
                    TestContext.WriteLine("Native sample " + manifest.samples[index].displayName + " @ " + manifest.version + ": " + roots[index]);
            }
            string[] guids = AssetDatabase.FindAssets(filter, roots);
            if (!allowHiddenPackaging) Assert.That(guids, Is.Not.Empty, "No visible sample assets inspected for " + filter);
            return guids;
        }

        private static List<string> FindHiddenSampleAssets(string fileName)
        {
            var paths = new List<string>();
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

            var levels = new Dictionary<int, int>();
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
