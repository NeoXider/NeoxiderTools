using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Mirror;
using Neo.Core.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    public sealed class SampleAssetValidationTests
    {
        private static readonly string[] SampleRootCandidates =
        {
            "Assets/Neoxider/Samples",
            "Assets/Neoxider/Samples~",
            "Assets/Samples/NeoxiderTools",
            "Assets/Samples/Neoxider Tools"
        };

        private static readonly string[] PackagePrefabRoots =
        {
            "Assets/Neoxider/Prefabs"
        };

        private static readonly Regex MonoScriptReferenceRegex =
            new(@"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}",
                RegexOptions.Compiled);

        private static readonly Regex MissingMonoScriptReferenceRegex =
            new(@"m_Script:\s*\{fileID:\s*0\}", RegexOptions.Compiled);

        [Test]
        public void SamplePrefabs_WithNetworkBehaviours_HaveNetworkIdentityInParents()
        {
            var failures = new List<string>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", GetExistingSampleRoots());

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                foreach (NetworkBehaviour behaviour in prefab.GetComponentsInChildren<NetworkBehaviour>(true))
                {
                    if (behaviour.GetComponentInParent<NetworkIdentity>(true) == null)
                    {
                        failures.Add(
                            $"{path}: {behaviour.GetType().FullName} on '{behaviour.name}' has no NetworkIdentity in parents.");
                    }
                }
            }

            AssertNoFailures(failures);
        }

        [Test]
        public void SamplePrefabs_HaveNoMissingMonoBehaviours()
        {
            var failures = new List<string>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", GetExistingSampleRoots());

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
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

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
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
            foreach (string path in EnumerateSampleYamlAssets())
            {
                string text = File.ReadAllText(path);
                if (MissingMonoScriptReferenceRegex.IsMatch(text))
                {
                    failures.Add($"{path}: contains direct missing MonoBehaviour script reference.");
                }

                foreach (Match match in MonoScriptReferenceRegex.Matches(text))
                {
                    string guid = match.Groups[1].Value;
                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        failures.Add($"{path}: references missing MonoScript guid {guid}.");
                    }
                }
            }

            AssertNoFailures(failures);
        }

        [Test]
        public void SampleTerrainData_HasNoMissingTreePrefabs()
        {
            var failures = new List<string>();
            string[] terrainGuids = AssetDatabase.FindAssets("t:TerrainData", GetExistingSampleRoots());

            foreach (string guid in terrainGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TerrainData terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (terrain == null)
                {
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
            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            var failures = new List<string>();

            try
            {
                foreach (string path in EnumerateSampleScenes())
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    GameObject[] roots = scene.GetRootGameObjects();
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
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                }
            }

            AssertNoFailures(failures);
        }

        [Test]
        public void ProgressionDemo_LevelCurveUsesIncreasingXpRequirements()
        {
            var failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("DemoLevelCurve", GetExistingSampleRoots());
            Assert.That(guids, Is.Not.Empty, "DemoLevelCurve asset was not found in active sample roots.");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelCurveDefinition curve = AssetDatabase.LoadAssetAtPath<LevelCurveDefinition>(path);
                if (curve == null)
                {
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
            foreach (string root in GetExistingSampleRoots())
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string path in Directory.EnumerateFiles(root, "*.prefab", SearchOption.AllDirectories))
                {
                    yield return path;
                }

                foreach (string path in Directory.EnumerateFiles(root, "*.unity", SearchOption.AllDirectories))
                {
                    yield return path;
                }
            }
        }

        private static IEnumerable<string> EnumerateSampleScenes()
        {
            foreach (string root in GetExistingSampleRoots())
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string path in Directory.EnumerateFiles(root, "*.unity", SearchOption.AllDirectories))
                {
                    yield return path.Replace('\\', '/');
                }
            }
        }

        private static int CountMissingMonoBehavioursRecursive(GameObject root)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);
            }

            return count;
        }

        private static string[] GetExistingSampleRoots()
        {
            var roots = new List<string>();
            foreach (string root in SampleRootCandidates)
            {
                if (AssetDatabase.IsValidFolder(root))
                {
                    roots.Add(root);
                }
            }

            Assert.IsNotEmpty(roots,
                "No Neoxider sample root found. Expected Assets/Neoxider/Samples during development, " +
                "Assets/Neoxider/Samples~ for UPM packaging, or Assets/Samples/NeoxiderTools after importing package samples.");
            return roots.ToArray();
        }

        private static string[] GetExistingPackagePrefabRoots()
        {
            var roots = new List<string>();
            foreach (string root in PackagePrefabRoots)
            {
                if (AssetDatabase.IsValidFolder(root))
                {
                    roots.Add(root);
                }
            }

            Assert.IsNotEmpty(roots, "No package prefab root found. Expected Assets/Neoxider/Prefabs.");
            return roots.ToArray();
        }
    }
}
