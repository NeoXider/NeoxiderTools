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
    ///     End-to-end checks of the Neo.Bonus demo builders on copies of the real scenes: the hand-authored prefab
    ///     instance is reused, controls and module events are wired persistently, and a second build changes nothing.
    ///     Bonus/Shop types are reached through serialized fields and reflection, so this assembly needs no extra
    ///     references.
    /// </summary>
    public sealed class BonusDemoBuildersTests
    {
        private const string WheelScene = "Scenes/Bonuses/WheelFortuneExample.unity";
        private const string SlotScene = "Scenes/Bonuses/SlotExample.unity";

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
        public void WheelBuilder_WiresTheAuthoredWheelIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("WheelFortuneExampleBuilder.Build");
            OpenSceneCopy(WheelScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            WheelFortuneDemoController controller = Single<WheelFortuneDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);

            Component wheel = Reference(controller, "_wheel");
            Assert.That(DemoSceneBuildContext.IsGenerated(wheel.gameObject), Is.False,
                "The hand-authored Canvas/WheelFortune instance must be reused.");
            Assert.That(DemoWiring.Describe(Event(wheel, "OnWinIdVariant")),
                Is.EqualTo("WheelMoneyWin.Win; WheelFortuneDemoController.OnWheelResult"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Re-arm wheel").onClick),
                Is.EqualTo("WheelFortuneDemoController.Rearm"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Spin from code").onClick),
                Is.EqualTo("WheelFortuneDemoController.SpinFromCode"));

            Component payout = Reference(controller, "_payout");
            SerializedProperty wins = new SerializedObject(payout).FindProperty("wins");
            Assert.That(wins.arraySize, Is.GreaterThan(0));
            Assert.That(new SerializedObject(payout).FindProperty("_wallet").objectReferenceValue,
                Is.SameAs(Reference(controller, "_wallet")));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(DemoWiring.Count(Event(wheel, "OnWinIdVariant")), Is.EqualTo(2));
        }

        [Test]
        public void SlotBuilder_ConfiguresTheAuthoredMachineIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("SlotExampleBuilder.Build");
            OpenSceneCopy(SlotScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            SlotDemoController controller = Single<SlotDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);

            Component slot = Reference(controller, "_slot");
            Assert.That(DemoSceneBuildContext.IsGenerated(slot.gameObject), Is.False,
                "The hand-authored Canvas/SlotUI instance must be reused.");
            SerializedObject machine = new(slot);
            Assert.That(machine.FindProperty("betsData").objectReferenceValue, Is.Not.Null);
            Assert.That(machine.FindProperty("checkSpin._linesData").objectReferenceValue, Is.Not.Null);
            Assert.That(machine.FindProperty("checkSpin._spritesMultiplierData").objectReferenceValue, Is.Not.Null);
            Assert.That(machine.FindProperty("_moneyGameObject").objectReferenceValue,
                Is.SameAs(Reference(controller, "_wallet").gameObject));

            Assert.That(DemoWiring.Describe(Event(slot, "OnChangeBet")), Is.EqualTo("TextMeshProUGUI.SetText"));
            Assert.That(DemoWiring.Describe(Event(slot, "OnWin")), Is.EqualTo("SlotDemoController.OnSpinWon"));
            Assert.That(DemoWiring.Describe(Event(slot, "OnLose")), Is.EqualTo("SlotDemoController.OnSpinLost"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Bet +").onClick), Is.EqualTo("SpinController.AddBet"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Lines +").onClick), Is.EqualTo("SpinController.AddLine"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_+100 coins").onClick), Is.EqualTo("Money.Add"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Force a win").onClick),
                Is.EqualTo("SlotDemoController.ForceWin"));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(DemoWiring.Count(Event(slot, "OnChangeBet")), Is.EqualTo(1));
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

            string folderName = "__NeoBonusDemoTests_" + Guid.NewGuid().ToString("N");
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
