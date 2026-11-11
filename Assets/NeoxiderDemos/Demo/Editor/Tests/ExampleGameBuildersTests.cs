using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     End-to-end checks, on copies of the shipped scenes, of the guides of the owner's hand-authored NoCode example
    ///     games (adventure, ClickerExample, Shooter2D) and of the Progression leftover migration. A guide adds the info
    ///     card, replaces the legacy StandaloneInputModule and moves persisting components to demo-only save keys
    ///     without moving or rebuilding the game; a second pass changes nothing. Module types are reached by name, so
    ///     this assembly needs no extra module references.
    /// </summary>
    public sealed class ExampleGameBuildersTests
    {
        private const string AdventureScene = "Scenes/_ExampleGame/adventure.unity";
        private const string ClickerScene = "Scenes/_ExampleGame/ClickerExample.unity";
        private const string ShooterScene = "Scenes/_ExampleGame/Shooter2D.unity";
        private const string ProgressionScene = "Scenes/Progression_Demo.unity";
        private const string InfoRootName = DemoSceneBuildContext.RootPrefix + "Info Card";
        private const string SkippedHint = "Info card hint skipped";
        private const string MissingGlyphs = "Characters missing from the static font atlas";

        private static readonly SaveKey InventoryKey = new("Neo.Tools.InventoryComponent", "_saveKey", null);
        private static readonly SaveKey MoneyKey = new("Neo.Shop.Money", "_moneySave", null);
        private static readonly SaveKey ShopKey = new("Neo.Shop.Shop", "_keySave", null);
        private static readonly SaveKey ScoreKey = new("Neo.Tools.ScoreManager", "_keySave", null);
        private static readonly SaveKey LevelKey = new("Neo.Level.LevelManager", "_saveKey", null);

        private string _folder;
        private Scene _scene;
        private Scene _previousActive;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (_previousActive.IsValid() && _previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActive);
            }

            if (!string.IsNullOrEmpty(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
                _folder = null;
            }

            _scene = default;
        }

        [Test]
        public void AdventureBuilder_GuidesTheHandAuthoredGameIdempotently()
        {
            AssertGuide("AdventureBuilder.Build", AdventureScene,
                InventoryKey.With("NeoDemo.Adventure.Inventory"));
        }

        [Test]
        public void ClickerExampleBuilder_GuidesTheHandAuthoredGameIdempotently()
        {
            AssertGuide("ClickerExampleBuilder.Build", ClickerScene,
                ShopKey.With("NeoDemo.Clicker.Shop"),
                MoneyKey.With("NeoDemo.Clicker.Money"),
                ScoreKey.With("NeoDemo.Clicker.BestScore"),
                LevelKey.With("NeoDemo.Clicker.Level"));
        }

        [Test]
        public void Shooter2DBuilder_GuidesTheHandAuthoredGameIdempotently()
        {
            AssertGuide("Shooter2DBuilder.Build", ShooterScene,
                MoneyKey.With("NeoDemo.Shooter2D.Money"),
                ScoreKey.With("NeoDemo.Shooter2D.BestScore"),
                LevelKey.With("NeoDemo.Shooter2D.Level"));
        }

        [Test]
        public void ProgressionBuilder_RetiresTheDefaultKeyLeftoversOnce()
        {
            DemoSceneBuilderInfo builder = Builder("ProgressionDemoBuilder.Build");
            Type inventoryType = RequireType(InventoryKey.TypeName);
            OpenSceneCopy(ProgressionScene);
            bool hadInventory = HandAuthoredRoot("PlayerInventory") != null;
            bool hadManagers = HandAuthoredRoot("Progression_Managers") != null;

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(HandAuthoredRoot("PlayerInventory"), Is.Null, "The default-key inventory leftover is retired.");
            Assert.That(HandAuthoredRoot("Progression_Managers"), Is.Null, "The unrelated managers leftover is retired.");
            string notes = string.Join("\n", first.Notes);
            if (hadInventory)
            {
                Assert.That(notes, Does.Contain("Retired hand-authored 'PlayerInventory'"));
            }

            if (hadManagers)
            {
                Assert.That(notes, Does.Contain("Retired hand-authored 'Progression_Managers'"));
            }

            foreach (Component inventory in FindAll(inventoryType))
            {
                Assert.That(Field(inventory, InventoryKey.Field), Does.StartWith("NeoDemo.Progression.Inventory"),
                    "A kept inventory gets the demo key: " + DemoSceneBuildContext.PathOf(inventory.transform));
            }

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
            Assert.That(string.Join("\n", second.Notes), Does.Not.Contain("Retired hand-authored"));
        }

        private void AssertGuide(string builderSuffix, string relativeScene, params SaveKey[] keys)
        {
            DemoSceneBuilderInfo builder = Builder(builderSuffix);
            List<Type> types = new();
            foreach (SaveKey key in keys)
            {
                types.Add(RequireType(key.TypeName));
            }

            OpenSceneCopy(relativeScene);
            List<string> handAuthored = HandAuthoredPaths();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(HandAuthoredPaths(), Is.EqualTo(handAuthored),
                "The guide must not move, add or remove hand-authored objects.");
            AssertNoWarning(first, SkippedHint, "Every hint names an object the hand-authored game still has.");
            AssertNoWarning(first, MissingGlyphs, "The card uses characters of the static TMP atlas only.");

            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.Hints.Count, Is.GreaterThan(0));
            Assert.That(info.OverlayRoot.transform.root.name, Is.EqualTo(InfoRootName));
            AssertInputModule();

            for (int i = 0; i < keys.Length; i++)
            {
                List<Component> components = FindAll(types[i]);
                Assert.That(components.Count, Is.EqualTo(1), keys[i].TypeName + " in " + relativeScene);
                AssertSaveKey(components[0], keys[i]);
            }

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
            Assert.That(HandAuthoredPaths(), Is.EqualTo(handAuthored));
            Assert.That(string.Join("\n", second.Notes), Does.Not.Contain("demo-only key"),
                "A second pass finds the demo keys in place.");
            Assert.That(string.Join("\n", second.Notes), Does.Not.Contain("Replaced StandaloneInputModule"));
        }

        private static void AssertSaveKey(Component component, SaveKey key)
        {
            using SerializedObject serialized = new(component);
            SerializedProperty property = serialized.FindProperty(key.Field);
            Assert.That(property, Is.Not.Null, key.TypeName + "." + key.Field);
            Assert.That(property.stringValue, Is.EqualTo(key.Value), key.TypeName + "." + key.Field);
            if (PrefabUtility.IsPartOfPrefabInstance(component))
            {
                Assert.That(property.prefabOverride, Is.True,
                    key.TypeName + "." + key.Field + " must be recorded as an override of its prefab instance.");
            }
        }

        private static void AssertIdempotent(DemoSceneResult first, DemoSceneResult second)
        {
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        private static void AssertNoWarning(DemoSceneResult result, string marker, string message)
        {
            foreach (string warning in result.Warnings)
            {
                Assert.That(warning, Does.Not.Contain(marker), message);
            }
        }

        private void AssertInputModule()
        {
            Type wanted = DemoSceneKit.ResolveInputModule(DemoInputModule.Auto);
            Assume.That(wanted, Is.Not.EqualTo(typeof(StandaloneInputModule)), "Needs the Input System package.");
            EventSystem eventSystem = Single<EventSystem>();
            Assert.That(DemoSceneBuildContext.IsGenerated(eventSystem.gameObject), Is.False,
                "The hand-authored EventSystem is kept.");
            Assert.That(eventSystem.GetComponent<StandaloneInputModule>(), Is.Null,
                "StandaloneInputModule throws in Input System-only projects.");
            Assert.That(eventSystem.GetComponent(wanted), Is.Not.Null, "EventSystem uses " + wanted.Name + ".");
        }

        private static DemoSceneBuilderInfo Builder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static Type RequireType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            Assume.That(false, fullName + " is not loaded.");
            return null;
        }

        private static string Field(Component component, string field)
        {
            using SerializedObject serialized = new(component);
            SerializedProperty property = serialized.FindProperty(field);
            Assert.That(property, Is.Not.Null, component.GetType().Name + "." + field);
            return property.stringValue;
        }

        private void OpenSceneCopy(string relativeScene)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source) == null)
            {
                Assert.Ignore("Demo scene not found: " + source);
            }

            string folderName = "__NeoExampleGameBuildersTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + Path.GetFileName(relativeScene);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Scene copy did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        /// <summary>Sorted hierarchy paths of every hand-authored object (generated roots excluded).</summary>
        private List<string> HandAuthoredPaths()
        {
            List<string> paths = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (DemoSceneBuildContext.IsGenerated(root))
                {
                    continue;
                }

                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    paths.Add(DemoSceneBuildContext.PathOf(node));
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        private GameObject HandAuthoredRoot(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name && !DemoSceneBuildContext.IsGenerated(root))
                {
                    return root;
                }
            }

            return null;
        }

        private List<Component> FindAll(Type type)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren(type, true))
                {
                    if (!DemoSceneBuildContext.IsGenerated(component.gameObject))
                    {
                        found.Add(component);
                    }
                }
            }

            return found;
        }

        private T Single<T>() where T : Component
        {
            List<T> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            Assert.That(found.Count, Is.EqualTo(1), typeof(T).Name);
            return found[0];
        }

        private readonly struct SaveKey
        {
            public SaveKey(string typeName, string field, string value)
            {
                TypeName = typeName;
                Field = field;
                Value = value;
            }

            public string TypeName { get; }
            public string Field { get; }
            public string Value { get; }

            public SaveKey With(string value)
            {
                return new SaveKey(TypeName, Field, value);
            }
        }
    }
}
