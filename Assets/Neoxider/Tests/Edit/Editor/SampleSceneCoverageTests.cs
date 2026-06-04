using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    public sealed class SampleSceneCoverageTests
    {
        private static readonly Regex MonoScriptReferenceRegex = new Regex(
            @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}",
            RegexOptions.Compiled);
        private static Dictionary<string, string> _guidToAssetPath;

        private static readonly string[] SampleRootCandidates =
        {
            "Assets/Neoxider/Samples",
            "Assets/Neoxider/Samples~",
            "Assets/Samples/NeoxiderTools",
            "Assets/Samples/Neoxider Tools"
        };

        private static readonly string[] RequiredSmokeSceneRelativePaths =
        {
            "Demo/Scenes/Audio/AudioDemo.unity",
            "Demo/Scenes/Level/LevelFlowDemo.unity",
            "Demo/Scenes/Network/NetworkDemo.unity",
            "Demo/Scenes/NoCode/NoCodeBindingDemo.unity",
            "Demo/Scenes/Parallax/ParallaxDemo.unity",
            "Demo/Scenes/Save/SaveDemo.unity",
            "Demo/Scenes/Settings/SettingsDemo.unity",
            "Demo/Scenes/StateMachine/StateMachineDemo.unity",
            "Demo/Scenes/UI/AnimationFlyDemo.unity"
        };

        [Test]
        public void RequiredSmokeDemoScenes_Exist()
        {
            var missing = new List<string>();
            foreach (string path in RequiredSmokeScenes())
            {
                if (!SceneFileExists(path))
                {
                    missing.Add(path);
                }
            }

            AssertNoFailures(missing);
        }

        [Test]
        public void RequiredSmokeDemoScenes_OpenWithDemoInfoAndNoMissingScripts()
        {
            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            var failures = new List<string>();
            System.Type demoInfoType = FindType("Neo.Samples.ModuleDemoSceneInfo");
            string demoInfoScriptGuid = demoInfoType == null ? FindScriptGuidFromTypeName("Neo.Samples.ModuleDemoSceneInfo") : string.Empty;

            try
            {
                foreach (string path in RequiredSmokeScenes())
                {
                    if (!SceneFileExists(path))
                    {
                        failures.Add(path + ": scene asset is missing.");
                        continue;
                    }

                    if (IsHiddenSamplePath(path))
                    {
                        CheckHiddenSampleSceneYaml(path, "Neo.Samples.ModuleDemoSceneInfo", demoInfoScriptGuid, failures);
                        continue;
                    }

                    if (demoInfoType == null)
                    {
                        failures.Add("Neo.Samples.ModuleDemoSceneInfo type was not found.");
                        continue;
                    }

                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    GameObject[] roots = scene.GetRootGameObjects();
                    if (roots.Length < 3)
                    {
                        failures.Add(path + ": expected camera/light plus a demo root object.");
                    }

                    int demoInfoCount = 0;
                    int missingScripts = 0;
                    foreach (GameObject root in roots)
                    {
                        if (root.GetComponentInChildren(demoInfoType, true) != null)
                        {
                            demoInfoCount++;
                        }

                        missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                    }

                    if (demoInfoCount == 0)
                    {
                        failures.Add(path + ": missing ModuleDemoSceneInfo marker.");
                    }

                    if (missingScripts > 0)
                    {
                        failures.Add(path + ": contains " + missingScripts + " missing MonoBehaviour script(s).");
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
        public void RpgRuntimeDemoScenes_AreWiredForPlayableSmoke()
        {
            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            var failures = new List<string>();
            string root = GetActiveSampleDemoRoot();
            string combatScenePath = root + "/Scenes/RpgCombatNpcDemo.unity";
            string vampireScenePath = root + "/Scenes/VampireSurvivorMCP.unity";

            System.Type rpgCharacterType = FindType("Neo.Rpg.Components.RpgCharacter");
            System.Type combatControllerType = FindType("Neo.Rpg.Demo.RpgCombatDemoController");
            System.Type vampireControllerType = FindType("Neo.Rpg.Demo.VampireSurvivorDemoController");
            System.Type healthBarsType = FindType("Neo.Rpg.Demo.RpgWorldHealthBars");
            System.Type spawnerType = FindType("Neo.Tools.Spawner");

            if (IsHiddenSamplePath(combatScenePath) || IsHiddenSamplePath(vampireScenePath))
            {
                CheckHiddenSampleSceneYaml(combatScenePath, "Neo.Rpg.Demo.RpgCombatDemoController", failures);
                CheckHiddenSampleSceneYaml(combatScenePath, "Neo.Rpg.Demo.RpgWorldHealthBars", failures);
                CheckHiddenSampleSceneYaml(combatScenePath, "Neo.Rpg.Components.RpgCharacter", failures);
                CheckHiddenSampleSceneYaml(vampireScenePath, "Neo.Rpg.Demo.VampireSurvivorDemoController", failures);
                CheckHiddenSampleSceneYaml(vampireScenePath, "Neo.Rpg.Demo.RpgWorldHealthBars", failures);
                CheckHiddenSampleSceneYaml(vampireScenePath, "Neo.Rpg.Components.RpgCharacter", failures);
                CheckHiddenSampleSceneYaml(vampireScenePath, "Neo.Tools.Spawner", failures);
                AssertNoFailures(failures);
                return;
            }

            if (rpgCharacterType == null)
            {
                failures.Add("RpgCharacter type was not found.");
            }

            if (combatControllerType == null)
            {
                failures.Add("RpgCombatDemoController type was not found.");
            }

            if (vampireControllerType == null)
            {
                failures.Add("VampireSurvivorDemoController type was not found.");
            }

            if (healthBarsType == null)
            {
                failures.Add("RpgWorldHealthBars type was not found.");
            }

            if (spawnerType == null)
            {
                failures.Add("Neo.Tools.Spawner type was not found.");
            }

            if (failures.Count > 0)
            {
                AssertNoFailures(failures);
            }

            try
            {
                CheckRpgCombatDemo(combatScenePath, combatControllerType, healthBarsType, rpgCharacterType, failures);
                CheckVampireSurvivorDemo(vampireScenePath, vampireControllerType, healthBarsType, rpgCharacterType,
                    spawnerType, failures);
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

        private static System.Type FindType(string fullName)
        {
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void CheckRpgCombatDemo(
            string scenePath,
            System.Type combatControllerType,
            System.Type healthBarsType,
            System.Type rpgCharacterType,
            List<string> failures)
        {
            if (!SceneFileExists(scenePath))
            {
                failures.Add(scenePath + ": scene asset is missing.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();

            if (FindInRoots(roots, combatControllerType) == null)
            {
                failures.Add(scenePath + ": missing RpgCombatDemoController.");
            }

            if (FindInRoots(roots, healthBarsType) == null)
            {
                failures.Add(scenePath + ": missing RpgWorldHealthBars for enemy HP bars.");
            }

            if (GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren(rpgCharacterType, true) == null)
            {
                failures.Add(scenePath + ": Player must have an RpgCharacter for combat smoke play.");
            }

            if (CountTaggedCharacters(roots, rpgCharacterType, "Enemy") == 0)
            {
                failures.Add(scenePath + ": expected at least one Enemy RpgCharacter.");
            }

            AddMissingScriptFailures(scenePath, roots, failures);
        }

        private static void CheckVampireSurvivorDemo(
            string scenePath,
            System.Type vampireControllerType,
            System.Type healthBarsType,
            System.Type rpgCharacterType,
            System.Type spawnerType,
            List<string> failures)
        {
            if (!SceneFileExists(scenePath))
            {
                failures.Add(scenePath + ": scene asset is missing.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();

            if (FindInRoots(roots, vampireControllerType) == null)
            {
                failures.Add(scenePath + ": missing VampireSurvivorDemoController.");
            }

            if (FindInRoots(roots, healthBarsType) == null)
            {
                failures.Add(scenePath + ": missing RpgWorldHealthBars for enemy HP bars.");
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                failures.Add(scenePath + ": missing Player-tagged object.");
            }
            else if (player.GetComponentInChildren(rpgCharacterType, true) == null)
            {
                failures.Add(scenePath + ": Player must have an RpgCharacter so HP and death can work.");
            }

            if (FindInRoots(roots, spawnerType) == null)
            {
                failures.Add(scenePath + ": expected at least one Spawner.");
            }

            AddMissingScriptFailures(scenePath, roots, failures);
        }

        private static Component FindInRoots(GameObject[] roots, System.Type componentType)
        {
            foreach (GameObject root in roots)
            {
                Component component = root.GetComponentInChildren(componentType, true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static int CountTaggedCharacters(GameObject[] roots, System.Type rpgCharacterType, string tag)
        {
            int count = 0;
            foreach (GameObject root in roots)
            {
                Component[] characters = root.GetComponentsInChildren(rpgCharacterType, true);
                for (int i = 0; i < characters.Length; i++)
                {
                    if (characters[i] != null && characters[i].CompareTag(tag))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void AddMissingScriptFailures(string path, GameObject[] roots, List<string> failures)
        {
            int missingScripts = 0;
            foreach (GameObject root in roots)
            {
                missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            }

            if (missingScripts > 0)
            {
                failures.Add(path + ": contains " + missingScripts + " missing MonoBehaviour script(s).");
            }
        }

        private static void CheckHiddenSampleSceneYaml(string path, string requiredTypeName, List<string> failures)
        {
            CheckHiddenSampleSceneYaml(path, requiredTypeName, FindScriptGuidFromTypeName(requiredTypeName), failures);
        }

        private static void CheckHiddenSampleSceneYaml(
            string path,
            string requiredTypeName,
            string requiredScriptGuid,
            List<string> failures)
        {
            if (!SceneFileExists(path))
            {
                failures.Add(path + ": scene asset is missing.");
                return;
            }

            string text = File.ReadAllText(path);
            if (text.Contains("m_Script: {fileID: 0"))
            {
                failures.Add(path + ": contains missing MonoBehaviour script reference(s).");
            }

            foreach (Match match in MonoScriptReferenceRegex.Matches(text))
            {
                string guid = match.Groups[1].Value;
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)) && string.IsNullOrEmpty(FindAssetPathByGuid(guid)))
                {
                    failures.Add(path + ": references unknown MonoScript guid " + guid + ".");
                }
            }

            bool hasRequiredType = text.Contains(requiredTypeName);
            if (!hasRequiredType && !string.IsNullOrEmpty(requiredScriptGuid))
            {
                hasRequiredType = text.Contains("guid: " + requiredScriptGuid);
            }

            if (!hasRequiredType)
            {
                failures.Add(path + ": missing " + requiredTypeName + " marker/reference.");
            }
        }

        private static string FindScriptGuid(string scriptFileName)
        {
            foreach (string metaPathRaw in Directory.EnumerateFiles("Assets/Neoxider", scriptFileName + ".meta", SearchOption.AllDirectories))
            {
                string guid = ReadGuidFromMeta(metaPathRaw);
                if (!string.IsNullOrEmpty(guid))
                {
                    return guid;
                }
            }

            return string.Empty;
        }

        private static string FindScriptGuidFromTypeName(string fullName)
        {
            int lastDot = fullName.LastIndexOf('.');
            string typeName = lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
            return FindScriptGuid(typeName + ".cs");
        }

        private static string FindAssetPathByGuid(string guid)
        {
            if (_guidToAssetPath == null)
            {
                _guidToAssetPath = new Dictionary<string, string>();
                foreach (string metaPathRaw in Directory.EnumerateFiles("Assets/Neoxider", "*.meta", SearchOption.AllDirectories))
                {
                    string metaGuid = ReadGuidFromMeta(metaPathRaw);
                    if (!string.IsNullOrEmpty(metaGuid) && !_guidToAssetPath.ContainsKey(metaGuid))
                    {
                        _guidToAssetPath.Add(metaGuid,
                            metaPathRaw.Substring(0, metaPathRaw.Length - ".meta".Length).Replace('\\', '/'));
                    }
                }
            }

            return _guidToAssetPath.TryGetValue(guid, out string path) ? path : string.Empty;
        }

        private static string ReadGuidFromMeta(string metaPath)
        {
            foreach (string line in File.ReadLines(metaPath))
            {
                if (line.StartsWith("guid: ", System.StringComparison.Ordinal))
                {
                    return line.Substring("guid: ".Length).Trim();
                }
            }

            return string.Empty;
        }

        private static IEnumerable<string> RequiredSmokeScenes()
        {
            string root = GetActiveSampleDemoRoot();
            foreach (string relativePath in RequiredSmokeSceneRelativePaths)
            {
                yield return root + "/" + relativePath.Substring("Demo/".Length);
            }
        }

        private static string GetActiveSampleDemoRoot()
        {
            foreach (string root in SampleRootCandidates)
            {
                if (Directory.Exists(root + "/Demo"))
                {
                    return root + "/Demo";
                }

                if (Directory.Exists(root + "/Demo Scenes"))
                {
                    return root + "/Demo Scenes";
                }

                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string childPathRaw in Directory.EnumerateDirectories(root))
                {
                    string childPath = childPathRaw.Replace('\\', '/');
                    if (Directory.Exists(childPath + "/Demo Scenes"))
                    {
                        return childPath + "/Demo Scenes";
                    }

                    if (Directory.Exists(childPath + "/Demo"))
                    {
                        return childPath + "/Demo";
                    }
                }
            }

            Assert.Fail(
                "No Neoxider Demo sample root found. Expected Assets/Neoxider/Samples/Demo during development, " +
                "Assets/Neoxider/Samples~/Demo for UPM packaging, or Assets/Samples/NeoxiderTools/<version>/Demo Scenes after import.");
            return SampleRootCandidates[0];
        }

        private static bool SceneFileExists(string path)
        {
            return File.Exists(path) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
        }

        private static bool IsHiddenSamplePath(string path)
        {
            return path.Replace('\\', '/').Contains("/Samples~/");
        }

        private static void AssertNoFailures(IReadOnlyList<string> failures)
        {
            if (failures.Count == 0)
            {
                return;
            }

            Assert.Fail(string.Join("\n", failures));
        }
    }
}
