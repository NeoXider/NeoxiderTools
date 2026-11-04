using System;
using System.Collections.Generic;
using System.IO;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Contracts of the StateMachine, Condition and NoCode demo builders: scenes are authored with persistent wiring,
    ///     rebuild without changes, and the shipped pre-kit scenes migrate while their hand-authored module objects
    ///     survive. Module types are checked by name so this assembly needs no extra module references.
    /// </summary>
    public sealed class LogicDemoBuildersTests
    {
        private const string ConditionScene = "Scenes/Condition/NeoConditionDemo.unity";
        private const string BindingScene = "Scenes/NoCode/NoCodeBindingDemo.unity";
        private const string ActionsScene = "Scenes/NoCode/NoCodeActionsDemo.unity";

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
        public void StateMachineBuilder_AuthorsCodeAndNoCodePathsIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("StateMachineDemoBuilder.Build");
            OpenEmptyScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            StateMachineDemoController controller = Single<StateMachineDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(DemoWiring.Describe(FindButton("Btn_Provoke").onClick),
                Is.EqualTo("StateMachineDemoController.Provoke"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Calm").onClick), Is.EqualTo("StateMachineDemoController.Calm"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Force Alert").onClick),
                Is.EqualTo("StateMachineBehaviourBase.ChangeState; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Back to initial").onClick),
                Is.EqualTo("StateMachineBehaviourBase.GoToInitialState; DemoLogFeed.Log"));

            SerializedObject guard = new(SingleByTypeName("StateMachineBehaviourBase"));
            Object data = guard.FindProperty("stateMachineData").objectReferenceValue;
            Assert.That(data, Is.Not.Null, "The guard plays a saved StateMachineData asset.");
            Assert.That(EditorUtility.IsPersistent(data), Is.True);
            Assert.That(guard.FindProperty("contextOverrides").arraySize, Is.EqualTo(3));
            Assert.That(CallCount(guard, "onStateChanged"), Is.EqualTo(1));

            SerializedObject machine = new(data);
            Assert.That(machine.FindProperty("states").arraySize, Is.EqualTo(2));
            Assert.That(machine.FindProperty("transitions").arraySize, Is.EqualTo(2));
            Assert.That(machine.FindProperty("initialState").objectReferenceValue, Is.Not.Null);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(new SerializedObject(data).FindProperty("transitions").arraySize, Is.EqualTo(2),
                "Rebuilding must not append transitions to the shared asset.");
        }

        [Test]
        public void NoCodeBindingBuilder_AuthorsEveryBindingIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("NoCodeBindingDemoBuilder.Build");
            OpenEmptyScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(DemoWiring.Describe(FindButton("Btn_+10").onClick), Is.EqualTo("Counter.Add; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_-10").onClick), Is.EqualTo("Counter.Add; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_RefreshBinding()").onClick),
                Is.EqualTo("NoCodeBindText.RefreshBinding; DemoLogFeed.Log"));
            Assert.That(AllByTypeName("SetProgress"), Has.Count.EqualTo(2));
            Assert.That(AllByTypeName("NoCodeBindText"), Has.Count.EqualTo(2));
            Assert.That(AllByTypeName("NoCodeFormattedText"), Has.Count.EqualTo(3));
            foreach (Component binding in AllByTypeName("SetProgress"))
            {
                Assert.That(new SerializedObject(binding).FindProperty("_binding._sourceRoot").objectReferenceValue,
                    Is.Not.Null, "Every binding reads the Counter.");
            }

            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        [Test]
        public void NoCodeBindingBuilder_MigratesTheShippedSceneAndKeepsTheCounter()
        {
            DemoSceneBuilderInfo builder = Builder("NoCodeBindingDemoBuilder.Build");
            OpenSceneCopy(BindingScene);
            GameObject counter = Root("Counter");
            Assert.That(counter, Is.Not.Null, "The shipped scene has a hand-authored 'Counter'.");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Canvas"), Is.Null, "The pre-kit layout is retired.");
            Assert.That(Root("Counter"), Is.SameAs(counter), "The hand-authored source survives.");
            Assert.That(FindButton("Btn_+10").onClick.GetPersistentTarget(0), Is.SameAs(counter.GetComponent("Counter")));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
        }

        [Test]
        public void ConditionBuilder_MigratesTheShippedSceneAndWiresTheRules()
        {
            DemoSceneBuilderInfo builder = Builder("NeoConditionDemoBuilder.Build");
            OpenSceneCopy(ConditionScene);
            GameObject health = Root("Health");
            GameObject score = Root("Score");
            Assert.That(health, Is.Not.Null, "The shipped scene has a hand-authored 'Health'.");
            Assert.That(score, Is.Not.Null, "The shipped scene has a hand-authored 'Score'.");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Canvas"), Is.Null, "The pre-kit layout is retired.");
            Assert.That(Root("[NeoCondition Demo Setup]"), Is.Null, "The editor-only setup helper is retired.");
            Assert.That(Root("Health"), Is.SameAs(health));
            Assert.That(Root("Score"), Is.SameAs(score));

            foreach (string rule in new[] { "CheckDead", "CheckLowHP", "CheckWin" })
            {
                SerializedObject condition = new(RuleByName(rule));
                Assert.That(CallCount(condition, "_onResult"), Is.EqualTo(2), rule + " drives its lamp and its banner/badge.");
                Assert.That(CallCount(condition, "_onTrue"), Is.EqualTo(1), rule + " logs OnTrue.");
                Assert.That(condition.FindProperty("_useUnscaledTime").boolValue, Is.True, rule);
                AssertNoMissingTargets(condition);
            }

            Assert.That(DemoWiring.Describe(FindButton("Btn_Take damage 25").onClick),
                Is.EqualTo("Health.TakeDamage; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Evaluate in code").onClick),
                Is.EqualTo("ConditionDemoUI.EvaluateInCode"));
            SerializedObject ui = new(SingleByTypeName("ConditionDemoUI"));
            Assert.That(ui.FindProperty("_conditions").arraySize, Is.EqualTo(3));
            Assert.That(ui.FindProperty("_log").objectReferenceValue, Is.Not.Null);
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(CallCount(new SerializedObject(RuleByName("CheckDead")), "_onResult"), Is.EqualTo(2),
                "Rule listeners must not pile up.");
        }

        [Test]
        public void NoCodeActionsBuilder_MigratesTheShippedSceneAndKeepsTheBridges()
        {
            DemoSceneBuilderInfo builder = Builder("NoCodeActionsDemoBuilder.Build");
            OpenSceneCopy(ActionsScene);
            GameObject level = Root("Level System");
            Assert.That(level, Is.Not.Null, "The shipped scene has a hand-authored 'Level System'.");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Canvas"), Is.Null, "The pre-kit layout is retired.");
            Assert.That(Root("Level System"), Is.SameAs(level));
            Assert.That(DemoWiring.Describe(FindButton("Btn_+120 XP").onClick), Is.EqualTo("LevelNoCodeAction.Execute"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Accept").onClick), Is.EqualTo("QuestNoCodeAction.Execute"));

            foreach (Component bridge in AllByTypeName("QuestNoCodeAction"))
            {
                SerializedObject serialized = new(bridge);
                Assert.That(CallCount(serialized, "_onResultMessage"), Is.EqualTo(2), bridge.name);
                AssertNoMissingTargets(serialized);
            }

            foreach (Component bridge in AllByTypeName("LevelNoCodeAction"))
            {
                AssertNoMissingTargets(new SerializedObject(bridge));
            }

            AssertNoMissingTargets(new SerializedObject(level.GetComponent("LevelComponent")));

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

        private static int CallCount(SerializedObject owner, string eventPath)
        {
            SerializedProperty calls = owner.FindProperty(eventPath + ".m_PersistentCalls.m_Calls");
            Assert.That(calls, Is.Not.Null, eventPath);
            return calls.arraySize;
        }

        private static void AssertNoMissingTargets(SerializedObject owner)
        {
            owner.Update();
            SerializedProperty iterator = owner.GetIterator();
            bool enter = true;
            List<string> missing = new();
            while (iterator.Next(enter))
            {
                enter = iterator.propertyType == SerializedPropertyType.Generic;
                if (iterator.name == "m_Target" && iterator.propertyType == SerializedPropertyType.ObjectReference &&
                    iterator.objectReferenceValue == null)
                {
                    missing.Add(iterator.propertyPath);
                }
            }

            Assert.That(missing, Is.Empty, owner.targetObject.name + ": persistent listeners without a target: " +
                                           string.Join(", ", missing));
        }

        private void OpenEmptyScene()
        {
            _scene = DemoTestScenes.OpenEmpty("__NeoLogicDemoTests", out _folder);
        }

        private void OpenSceneCopy(string relativeScenePath)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScenePath;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(source), Is.Not.Null, "Shipped scene missing: " + source);
            string folderName = "__NeoLogicDemoTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + Path.GetFileName(relativeScenePath);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private GameObject Root(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
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

        private Component SingleByTypeName(string typeName)
        {
            List<Component> found = AllByTypeName(typeName);
            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
        }

        private Component RuleByName(string objectName)
        {
            foreach (Component rule in AllByTypeName("NeoCondition"))
            {
                if (rule.gameObject.name == objectName)
                {
                    return rule;
                }
            }

            Assert.Fail("NeoCondition '" + objectName + "' not found.");
            return null;
        }

        private List<Component> AllByTypeName(string typeName)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour != null && behaviour.GetType().Name == typeName)
                    {
                        found.Add(behaviour);
                    }
                }
            }

            return found;
        }
    }
}
