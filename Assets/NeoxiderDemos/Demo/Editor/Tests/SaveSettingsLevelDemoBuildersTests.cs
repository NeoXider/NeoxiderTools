using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Samples;
using Neo.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     End-to-end checks of the Save, Settings and Level demo builders on copies of the real scenes: the
    ///     hand-authored services are reused and configured, controls and module events are wired persistently, and
    ///     a second build changes nothing. Save and Level types are reached by name and through serialized fields, so
    ///     this assembly needs no extra references.
    /// </summary>
    public sealed class SaveSettingsLevelDemoBuildersTests
    {
        private const string SaveScene = "Scenes/Save/SaveDemo.unity";
        private const string SettingsScene = "Scenes/Settings/SettingsDemo.unity";
        private const string LevelScene = "Scenes/Level/LevelFlowDemo.unity";

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
        }

        [Test]
        public void SaveBuilder_ConfiguresTheFileProviderAndWiresBothSaveLayers()
        {
            DemoSceneBuilderInfo builder = Builder("SaveDemoBuilder.Build");
            OpenSceneCopy(SaveScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Component controller = Single("SaveDemoController");
            Assert.That(Property<bool>(controller, "IsSceneBuilt"), Is.True);

            Component manager = Single("SaveManager");
            Assert.That(DemoSceneBuildContext.IsGenerated(manager.gameObject), Is.False,
                "The hand-authored 'Save Demo' SaveManager must be reused.");
            Object settings = new SerializedObject(Single("SaveProviderSettingsComponent")).FindProperty("_settings")
                .objectReferenceValue;
            Assert.That(settings, Is.Not.Null, "The demo must select its own provider settings asset.");
            Assert.That(EditorUtility.IsPersistent(settings), Is.True);
            SerializedObject provider = new(settings);
            Assert.That(provider.FindProperty("_providerType").intValue, Is.EqualTo(1), "File provider expected.");
            Assert.That(provider.FindProperty("_fileName").stringValue, Is.EqualTo("neo_save_demo.json"));

            Assert.That(DemoWiring.Describe(FindButton("Btn_Save").onClick), Is.EqualTo("SaveDemoController.SaveToDisk"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_FlushNow (NoCode)").onClick),
                Is.EqualTo("DemoLogFeed.Log; SaveManager.FlushNow"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_+$10M").onClick),
                Is.EqualTo("SaveDemoController.AddWalletDollars"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Nova").onClick), Is.EqualTo("SaveDemoController.SetName"));
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            AssertIdempotent(builder, first);
        }

        [Test]
        public void SettingsBuilder_OnTheRealScene_ShowsTheDeferredSaveUnderADemoPrefix()
        {
            DemoSceneBuilderInfo builder = Builder("SettingsDemoBuilder.Build");
            OpenSceneCopy(SettingsScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            SettingsDemoController controller = Single<SettingsDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(DemoSceneBuildContext.IsGenerated(controller.Settings.gameObject), Is.False,
                "The hand-authored GameSettingsComponent must be reused.");

            string prefix = new SerializedObject(controller.Settings).FindProperty("_saveKeyPrefix").stringValue;
            Assert.That(prefix, Is.EqualTo("Neo.Demo.Settings."), "The demo must not overwrite a game's settings keys.");
            Assert.That(new SerializedObject(controller).FindProperty("_saveKeyPrefix").stringValue, Is.EqualTo(prefix));
            Assert.That(new SerializedObject(controller).FindProperty("_deferredValue").objectReferenceValue, Is.Not.Null);

            AssertIdempotent(builder, first);
        }

        [Test]
        public void LevelBuilder_ConfiguresMapsGridAndNoCodeBindings()
        {
            DemoSceneBuilderInfo builder = Builder("LevelFlowDemoBuilder.Build");
            OpenSceneCopy(LevelScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Component controller = Single("LevelDemoController");
            Assert.That(Property<bool>(controller, "IsSceneBuilt"), Is.True);

            Component manager = Single("LevelManager");
            Assert.That(DemoSceneBuildContext.IsGenerated(manager.gameObject), Is.False,
                "The hand-authored 'Level Demo' LevelManager must be reused.");
            SerializedObject maps = new(manager);
            Assert.That(maps.FindProperty("_maps").arraySize, Is.EqualTo(2));
            SerializedProperty world = maps.FindProperty("_maps").GetArrayElementAtIndex(0);
            Assert.That(world.FindPropertyRelative("isInfinity").boolValue, Is.False);
            Assert.That(world.FindPropertyRelative("countLevels").intValue, Is.EqualTo(5));
            Assert.That(maps.FindProperty("_maps").GetArrayElementAtIndex(1).FindPropertyRelative("isInfinity").boolValue,
                Is.True);
            Assert.That(maps.FindProperty("_lvlBtns").arraySize, Is.EqualTo(5));
            Assert.That(DemoWiring.Describe(PublicEvent(manager, "OnChangeLevel")),
                Is.EqualTo("LevelDemoController.OnMapLevelChanged"));

            Component level = Single("LevelComponent");
            Assert.That(new SerializedObject(level).FindProperty("_levelCurve").objectReferenceValue, Is.Not.Null);
            object levelState = level.GetType().GetField("LevelState").GetValue(level);
            UnityEventBase levelChanged = (UnityEventBase)levelState.GetType().GetProperty("OnChanged").GetValue(levelState);
            Assert.That(DemoWiring.Describe(levelChanged), Is.EqualTo("DemoValueReadout.SetInt"),
                "The level label is a NoCode binding of LevelState.OnChanged.");

            Assert.That(DemoWiring.Describe(FindButton("Btn_Complete level").onClick),
                Is.EqualTo("DemoLogFeed.Log; LevelManager.SaveLevel"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_World 1").onClick), Is.EqualTo("LevelManager.SetMapId"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_+25 XP · NoCode").onClick),
                Is.EqualTo("LevelNoCodeAction.Execute"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_+10 XP").onClick), Is.EqualTo("LevelDemoController.AddXp"));
            Component firstCell = FindNamed("Level 1", "LevelButton");
            Assert.That(DemoWiring.Describe(PublicEvent(firstCell, "OnCurrentVisual")),
                Is.EqualTo("GameObject.SetActive; GameObject.SetActive"));

            AssertIdempotent(builder, first);
            Assert.That(DemoWiring.Count(PublicEvent(manager, "OnChangeLevel")), Is.EqualTo(1),
                "Listeners on the hand-authored LevelManager must not pile up across rebuilds.");
        }

        private void AssertIdempotent(DemoSceneBuilderInfo builder, DemoSceneResult first)
        {
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        private static DemoSceneBuilderInfo Builder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private void OpenSceneCopy(string relativeScene)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source) == null)
            {
                Assert.Ignore("Demo scene not found: " + source);
            }

            string folderName = "__NeoSaveSettingsLevelDemoTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + Path.GetFileName(relativeScene);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Scene copy did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private static T Property<T>(object owner, string name)
        {
            PropertyInfo info = owner.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, owner.GetType().Name + "." + name);
            return (T)info.GetValue(owner);
        }

        private static UnityEventBase PublicEvent(Component owner, string field)
        {
            FieldInfo info = owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, owner.GetType().Name + "." + field);
            return (UnityEventBase)info.GetValue(owner);
        }

        private Button FindButton(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.name == name)
                    {
                        return button;
                    }
                }
            }

            Assert.Fail("Button '" + name + "' not found.");
            return null;
        }

        private Component FindNamed(string objectName, string typeName)
        {
            foreach (Component component in All(typeName))
            {
                if (component.name == objectName)
                {
                    return component;
                }
            }

            Assert.Fail(typeName + " on '" + objectName + "' not found.");
            return null;
        }

        private Component Single(string typeName)
        {
            List<Component> found = All(typeName);
            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
        }

        private List<Component> All(string typeName)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component != null && component.GetType().Name == typeName)
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
    }
}
