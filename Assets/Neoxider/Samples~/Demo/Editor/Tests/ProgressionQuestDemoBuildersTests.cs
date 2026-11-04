using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Samples;
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
    ///     End-to-end checks of the Progression and Quest demo builders on copies of the real scenes: hand-authored
    ///     module components are reused and configured, controls and module events are wired persistently to the
    ///     controllers and NoCode actions, and a second build changes nothing. Module types are reached through
    ///     serialized fields and reflection, so this assembly needs no extra references.
    /// </summary>
    public sealed class ProgressionQuestDemoBuildersTests
    {
        private const string ProgressionScene = "Scenes/Progression_Demo.unity";
        private const string QuestScene = "Scenes/Quests/QuestDemo.unity";

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
        public void ProgressionBuilder_ConfiguresAuthoredManagerAndWiresBothPaths()
        {
            DemoSceneBuilderInfo builder = Builder("ProgressionDemoBuilder.Build");
            OpenSceneCopy(ProgressionScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            ProgressionDemoUI ui = Single<ProgressionDemoUI>();
            Assert.That(ui.IsSceneBuilt, Is.True);

            Component level = Reference(ui, "_levelComponent");
            Component manager = Reference(ui, "_progression");
            Assert.That(DemoSceneBuildContext.IsGenerated(manager.gameObject), Is.False,
                "The hand-authored ProgressionDemoManager must be reused.");
            SerializedObject managerData = new(manager);
            Assert.That(managerData.FindProperty("_levelProvider").objectReferenceValue, Is.SameAs(level));
            Assert.That(managerData.FindProperty("_levelCurve").objectReferenceValue, Is.Not.Null);
            Assert.That(managerData.FindProperty("_unlockTree").objectReferenceValue, Is.Not.Null);
            Assert.That(managerData.FindProperty("_perkTree").objectReferenceValue, Is.Not.Null);
            Assert.That(managerData.FindProperty("_autoSave").boolValue, Is.False);
            Assert.That(managerData.FindProperty("_loadOnAwake").boolValue, Is.False);

            Assert.That(DemoWiring.Describe(FindButton("Btn_+500 XP").onClick), Is.EqualTo("ProgressionDemoUI.AddXp"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Unlock Forge").onClick),
                Is.EqualTo("ProgressionNoCodeAction.Execute"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Blade Master").onClick),
                Is.EqualTo("ProgressionNoCodeAction.Execute"));
            Assert.That(DemoWiring.Describe(Event(level, "_onLevelUp")), Is.EqualTo("ProgressionDemoUI.HandleLevelUp"));
            Assert.That(DemoWiring.Describe(Event(manager, "_onPerkPurchased")),
                Is.EqualTo("ProgressionDemoUI.HandlePerkPurchased"));

            Component bladeMaster = ListenerTarget(FindButton("Btn_Blade Master").onClick);
            SerializedObject action = new(bladeMaster);
            Assert.That(action.FindProperty("_perkId").stringValue, Is.EqualTo("blade_master"));
            Assert.That(action.FindProperty("_manager").objectReferenceValue, Is.SameAs(manager));
            Assert.That(DemoWiring.Describe(Event(bladeMaster, "_onResultMessage")),
                Is.EqualTo("ProgressionDemoUI.LogNoCodeResult"));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(DemoWiring.Count(Event(level, "_onLevelUp")), Is.EqualTo(1));
        }

        [Test]
        public void QuestBuilder_AuthorsAssetsRewardAndNoCodeNotificationsIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("QuestDemoBuilder.Build");
            OpenSceneCopy(QuestScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            QuestDemoController controller = Single<QuestDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(DemoSceneBuildContext.IsGenerated(controller.gameObject), Is.False,
                "The hand-authored Demo Controller must be reused.");

            SerializedObject controllerData = new(controller);
            SerializedProperty quests = controllerData.FindProperty("_quests");
            Assert.That(quests.arraySize, Is.EqualTo(3));
            for (int i = 0; i < quests.arraySize; i++)
            {
                Object quest = quests.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(quest != null && EditorUtility.IsPersistent(quest), Is.True,
                    "Quest " + i + " must be a saved asset, not a runtime instance.");
            }

            Component manager = Reference(controller, "_manager");
            Assert.That(new SerializedObject(manager).FindProperty("_knownQuests").arraySize, Is.EqualTo(3));
            Assert.That(DemoWiring.Describe(Event(manager, "_onQuestCompleted")), Is.EqualTo("Money.Add"));

            Assert.That(DemoWiring.Describe(FindButton("Btn_Accept").onClick), Is.EqualTo("QuestDemoController.Accept"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Wolf Hunt").onClick), Is.EqualTo("QuestDemoController.Select"));
            Component killThree = ListenerTarget(FindButton("Btn_Kill 3 wolves").onClick);
            Assert.That(DemoWiring.Describe(FindButton("Btn_Kill 3 wolves").onClick),
                Is.EqualTo("QuestNoCodeAction.Execute"));
            SerializedObject action = new(killThree);
            Assert.That(action.FindProperty("_targetId").stringValue, Is.EqualTo("wolf"));
            Assert.That(action.FindProperty("_amount").intValue, Is.EqualTo(3));
            Assert.That(DemoWiring.Describe(Event(killThree, "_onResultMessage")),
                Is.EqualTo("QuestDemoController.LogNoCodeResult"));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(DemoWiring.Count(Event(manager, "_onQuestCompleted")), Is.EqualTo(1));
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

            string folderName = "__NeoProgressionQuestDemoTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + Path.GetFileName(relativeScene);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Scene copy did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private static Component Reference(Object owner, string field)
        {
            Object value = new SerializedObject(owner).FindProperty(field).objectReferenceValue;
            Assert.That(value, Is.Not.Null, owner.GetType().Name + "." + field);
            return (Component)value;
        }

        private static UnityEventBase Event(Component owner, string field)
        {
            FieldInfo info = null;
            for (Type type = owner.GetType(); type != null && info == null; type = type.BaseType)
            {
                info = type.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            Assert.That(info, Is.Not.Null, owner.GetType().Name + "." + field);
            return (UnityEventBase)info.GetValue(owner);
        }

        private static Component ListenerTarget(UnityEventBase unityEvent)
        {
            Assert.That(unityEvent.GetPersistentEventCount(), Is.EqualTo(1));
            Component target = unityEvent.GetPersistentTarget(0) as Component;
            Assert.That(target, Is.Not.Null);
            return target;
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
