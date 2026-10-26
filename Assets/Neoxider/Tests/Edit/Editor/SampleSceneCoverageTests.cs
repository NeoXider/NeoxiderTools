using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    public sealed class SampleSceneCoverageTests
    {
        private static readonly Regex MonoScriptReferenceRegex = new(
            @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}",
            RegexOptions.Compiled);

        private static Dictionary<string, string> _guidToAssetPath;

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
        public void RequiredSmokeDemoScenes_YamlPackaging_HasDemoInfoAndScriptReferences()
        {
            List<string> failures = new List<string>();
            int inspected = 0;
            foreach (string path in RequiredSmokeScenes())
                inspected += CheckSceneYamlPackaging(path, "Neo.Samples.ModuleDemoSceneInfo", failures);
            Assert.That(inspected, Is.EqualTo(RequiredSmokeSceneRelativePaths.Length), "Every required scene must supply actual YAML packaging evidence.");
            AssertNoFailures(failures);
        }

        [Test]
        public void RequiredSmokeDemoScenes_OpenWithDemoInfoAndNoMissingScripts()
        {
            List<string> failures = new List<string>();
            string demoRoot = GetNativeSampleDemoRoot();
            System.Type demoInfoType = FindType("Neo.Samples.ModuleDemoSceneInfo");
            RequireNativeContext(demoRoot, AssetDatabase.IsValidFolder(demoRoot), demoInfoType);

            foreach (string path in RequiredSmokeScenes(demoRoot))
            {
                if (!SceneFileExists(path))
                {
                    failures.Add(path + ": scene asset is missing.");
                    continue;
                }

                RequireNativeSceneAsset(path);
                using (Audit20261023NeoEditorTestsPreviewSceneScope preview = new(path))
                {
                    Scene scene = preview.Scene;
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

                        missingScripts += SampleAssetValidationTests.CountMissingMonoBehavioursRecursive(root);
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
            AssertNoFailures(failures);
        }

        [Test]
        public void RpgRuntimeDemoScenes_YamlPackaging_HasPlayableComponentReferences()
        {
            List<string> failures = new List<string>();
            string root = GetActiveSampleDemoRoot();
            string combat = root + "/Scenes/RpgCombatNpcDemo.unity";
            string vampire = root + "/Scenes/VampireSurvivorMCP.unity";
            int inspected = CheckSceneYamlPackaging(combat, "Neo.Rpg.Demo.RpgCombatDemoController", failures);
            CheckSceneYamlPackaging(combat, "Neo.Rpg.Demo.RpgWorldHealthBars", failures);
            CheckSceneYamlPackaging(combat, "Neo.Rpg.Components.RpgCharacter", failures);
            inspected += CheckSceneYamlPackaging(vampire, "Neo.Rpg.Demo.VampireSurvivorDemoController", failures);
            CheckSceneYamlPackaging(vampire, "Neo.Rpg.Demo.RpgWorldHealthBars", failures);
            CheckSceneYamlPackaging(vampire, "Neo.Rpg.Components.RpgCharacter", failures);
            CheckSceneYamlPackaging(vampire, "Neo.Tools.Spawner", failures);
            Assert.That(inspected, Is.EqualTo(2), "Both RPG scene files must supply actual YAML packaging evidence.");
            AssertNoFailures(failures);
        }

        [Test]
        public void RpgRuntimeDemoScenes_AreWiredForPlayableSmoke()
        {
            List<string> failures = new List<string>();
            string root = GetNativeSampleDemoRoot();
            string combatScenePath = root + "/Scenes/RpgCombatNpcDemo.unity";
            string vampireScenePath = root + "/Scenes/VampireSurvivorMCP.unity";

            System.Type rpgCharacterType = FindType("Neo.Rpg.Components.RpgCharacter");
            System.Type combatControllerType = FindType("Neo.Rpg.Demo.RpgCombatDemoController");
            System.Type vampireControllerType = FindType("Neo.Rpg.Demo.VampireSurvivorDemoController");
            System.Type healthBarsType = FindType("Neo.Rpg.Demo.RpgWorldHealthBars");
            System.Type spawnerType = FindType("Neo.Tools.Spawner");

            RequireNativeContext(root, AssetDatabase.IsValidFolder(root), combatControllerType, vampireControllerType, healthBarsType);

            if (rpgCharacterType == null)
            {
                failures.Add("RpgCharacter type was not found.");
            }

            if (spawnerType == null)
            {
                failures.Add("Neo.Tools.Spawner type was not found.");
            }

            if (failures.Count > 0)
            {
                AssertNoFailures(failures);
            }

            RequireNativeSceneAsset(combatScenePath);
            RequireNativeSceneAsset(vampireScenePath);

            CheckRpgCombatDemo(combatScenePath, combatControllerType, healthBarsType, rpgCharacterType, failures);
            CheckVampireSurvivorDemo(vampireScenePath, vampireControllerType, healthBarsType, rpgCharacterType,
                spawnerType, failures);

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

            using (Audit20261023NeoEditorTestsPreviewSceneScope preview = new(scenePath))
            {
                Scene scene = preview.Scene;
                GameObject[] roots = scene.GetRootGameObjects();

                if (FindInRoots(roots, combatControllerType) == null)
                {
                    failures.Add(scenePath + ": missing RpgCombatDemoController.");
                }

                if (FindInRoots(roots, healthBarsType) == null)
                {
                    failures.Add(scenePath + ": missing RpgWorldHealthBars for enemy HP bars.");
                }

                if (FindTaggedObject(roots, "Player")?.GetComponentInChildren(rpgCharacterType, true) == null)
                {
                    failures.Add(scenePath + ": Player must have an RpgCharacter for combat smoke play.");
                }

                if (CountTaggedCharacters(roots, rpgCharacterType, "Enemy") == 0)
                {
                    failures.Add(scenePath + ": expected at least one Enemy RpgCharacter.");
                }

                AddMissingScriptFailures(scenePath, roots, failures);
            }
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

            using (Audit20261023NeoEditorTestsPreviewSceneScope preview = new(scenePath))
            {
                Scene scene = preview.Scene;
                GameObject[] roots = scene.GetRootGameObjects();

                if (FindInRoots(roots, vampireControllerType) == null)
                {
                    failures.Add(scenePath + ": missing VampireSurvivorDemoController.");
                }

                if (FindInRoots(roots, healthBarsType) == null)
                {
                    failures.Add(scenePath + ": missing RpgWorldHealthBars for enemy HP bars.");
                }

                GameObject player = FindTaggedObject(roots, "Player");
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
        }

        internal static GameObject FindTaggedObject(GameObject[] roots, string tag)
        {
            foreach (GameObject root in roots)
            {
                foreach (Transform descendant in root.GetComponentsInChildren<Transform>(true))
                {
                    if (descendant.CompareTag(tag))
                    {
                        return descendant.gameObject;
                    }
                }
            }

            return null;
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

        internal static void AddMissingScriptFailures(string path, GameObject[] roots, List<string> failures)
        {
            int missingScripts = 0;
            foreach (GameObject root in roots)
            {
                missingScripts += SampleAssetValidationTests.CountMissingMonoBehavioursRecursive(root);
            }

            if (missingScripts > 0)
            {
                failures.Add(path + ": contains " + missingScripts + " missing MonoBehaviour script(s).");
            }
        }

        private static int CheckSceneYamlPackaging(string path, string requiredTypeName, List<string> failures)
        {
            string diskPath = Audit20261023NeoEditorTestsPaths.ToDiskPath(path);
            if (!File.Exists(diskPath))
            {
                failures.Add(path + ": scene asset is missing.");
                return 0;
            }

            string text = File.ReadAllText(diskPath);
            ValidateSceneYamlPackaging(path, text, requiredTypeName, FindScriptGuidFromTypeName(requiredTypeName),
                guid => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)) ||
                    !string.IsNullOrEmpty(FindAssetPathByGuid(guid)), failures);
            TestContext.WriteLine("Scene YAML packaging: " + path + " / " + requiredTypeName);
            return 1;
        }

        internal static void ValidateSceneYamlPackaging(string path, string text, string requiredTypeName,
            string requiredScriptGuid, Func<string, bool> scriptGuidExists, List<string> failures)
        {
            text ??= string.Empty;
            if (text.Contains("m_Script: {fileID: 0"))
            {
                failures.Add(path + ": contains missing MonoBehaviour script reference(s).");
            }

            MatchCollection scriptReferences = MonoScriptReferenceRegex.Matches(text);
            if (scriptReferences.Count == 0)
                failures.Add(path + ": no serialized MonoScript references were inspected.");
            foreach (Match match in scriptReferences)
            {
                string guid = match.Groups[1].Value;
                if (!scriptGuidExists(guid))
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
            foreach (string metaPathRaw in Directory.EnumerateFiles(Audit20261023NeoEditorTestsPaths.PackageDiskRoot, scriptFileName + ".meta",
                         SearchOption.AllDirectories))
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
                foreach (string metaPathRaw in Directory.EnumerateFiles(Audit20261023NeoEditorTestsPaths.PackageDiskRoot, "*.meta",
                             SearchOption.AllDirectories))
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

        private static IEnumerable<string> RequiredSmokeScenes(string root = null)
        {
            root ??= GetActiveSampleDemoRoot();
            foreach (string relativePath in RequiredSmokeSceneRelativePaths)
            {
                yield return root + "/" + relativePath.Substring("Demo/".Length);
            }
        }

        private static string GetActiveSampleDemoRoot() => Audit20261023NeoEditorTestsPaths.DemoRoot;

        private static string GetNativeSampleDemoRoot()
        {
            if (!Audit20261023NeoEditorTestsPaths.TryGetDemoRoot(out string root))
                Assert.Ignore("Current-version Demo Scenes are unavailable. YAML packaging and native integration are separate cases.");
            return root;
        }

        internal static void RequireNativeContext(string root, bool hasVisibleRoot, params System.Type[] sampleTypes)
        {
            if (IsHiddenSamplePath(root) || !hasVisibleRoot)
                Assert.Ignore("Native sample scene integration requires developer/imported assets. Hidden Samples~ YAML is packaging evidence only.");
            foreach (System.Type type in sampleTypes)
                if (type == null) Assert.Ignore("Required compiled sample types are unavailable for " + root);
            foreach (System.Type type in sampleTypes)
                Assert.That(typeof(Component).IsAssignableFrom(type), Is.True, "An active sample type must be a Unity Component: " + type.FullName);
            TestContext.WriteLine("Native sample scene context: " + root);
        }

        internal static void RequireNativeSceneAsset(string path)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null,
                "Active sample scene failed native asset loading: " + path);
        }

        private static bool SceneFileExists(string path)
        {
            return File.Exists(Audit20261023NeoEditorTestsPaths.ToDiskPath(path)) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
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
