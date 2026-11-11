using System;
using System.Collections.Generic;
using System.IO;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contracts of the Tools world demo builders (Follow, DrawExample, MouseEffectorExample,
    ///     CameraConstraint). Each test copies the shipped scene, builds it twice and checks that the hand-authored
    ///     gameplay objects survive, the persistent wiring is in place and the second pass changes nothing. Module
    ///     types are addressed by name so this assembly needs no extra module references.
    /// </summary>
    public sealed class ToolsWorldDemoBuildersTests
    {
        private const string FollowScene = "Scenes/Tools/Move/Follow.unity";
        private const string DrawScene = "Scenes/Tools/DrawExample.unity";
        private const string MouseEffectScene = "Scenes/Tools/MouseEffectorExample.unity";
        private const string CameraConstraintScene = "Scenes/Tools/CameraConstraint.unity";

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
        public void FollowBuilder_DecoratesTheShippedSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("FollowBuilder.Build");
            OpenSceneCopy(FollowScene);
            GameObject target = Root("Circle (1)") ?? Root("Target");
            GameObject middle = Root("Follow");
            Assert.That(target, Is.Not.Null, "The shipped scene has the follow target.");
            Assert.That(middle, Is.Not.Null, "The shipped scene has the hand-authored 'Follow' capsule.");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Target"), Is.SameAs(target), "The target is renamed, not replaced.");
            Assert.That(target.GetComponent("MouseMover2D"), Is.Not.Null, "The target is draggable.");
            Assert.That(Root("Follow"), Is.SameAs(middle), "Hand-authored followers survive.");

            FollowDemoController controller = Single<FollowDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(controller.FollowerCount, Is.EqualTo(5), "Three authored followers, Rail Follower and Turret.");
            Assert.That(Occurrences(DemoWiring.Describe(FindButton("Btn_Teleport").onClick), "Follow.TeleportToTarget"),
                Is.EqualTo(3), "Teleport calls every position follower (not the aim-only ones).");
            Assert.That(DemoWiring.Describe(FindButton("Btn_Drop target").onClick), Is.EqualTo("FollowDemoController.DropTarget"));
            Assert.That(Single<DemoPointerGate>().GatedCount, Is.EqualTo(1));
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            SerializedObject authored = new(middle.GetComponent("Follow"));
            Assert.That(CallCount(authored, "onTargetLost"), Is.EqualTo(1));
            AssertNoMissingTargets(authored);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
            authored.Update();
            Assert.That(CallCount(authored, "onTargetLost"), Is.EqualTo(1), "Listeners on hand-authored events must not pile up.");
        }

        [Test]
        public void DrawExampleBuilder_DecoratesTheShippedSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("DrawExampleBuilder.Build");
            OpenSceneCopy(DrawScene);
            GameObject draw = Root("Draw");
            Assert.That(draw, Is.Not.Null, "The shipped scene has the hand-authored 'Draw' object.");
            Component drawer = draw.GetComponent("Drawer");
            float width = new SerializedObject(drawer).FindProperty("lineWidth").floatValue;

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Draw"), Is.SameAs(draw));
            Assert.That(Single<DrawerDemoController>().IsSceneBuilt, Is.True);
            SerializedObject serialized = new(drawer);
            Assert.That(serialized.FindProperty("lineWidth").floatValue, Is.EqualTo(width), "Authored settings are kept.");
            Assert.That(CallCount(serialized, "PointCount._onChanged"), Is.EqualTo(1));
            Assert.That(CallCount(serialized, "Distance._onChanged"), Is.EqualTo(1));
            Assert.That(CallCount(serialized, "OnLineCreated"), Is.EqualTo(1));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Undo last").onClick), Is.EqualTo("Drawer.DeleteLast; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Pause balls").onClick),
                Is.EqualTo("Spawner.StopSpawn; DemoLogFeed.Log"));
            Assert.That(Single<DemoPointerGate>().GatedCount, Is.EqualTo(1));
            AssertNoMissingTargets(serialized);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
            serialized.Update();
            Assert.That(CallCount(serialized, "PointCount._onChanged"), Is.EqualTo(1));
        }

        [Test]
        public void MouseEffectorExampleBuilder_DecoratesTheShippedSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("MouseEffectorExampleBuilder.Build");
            OpenSceneCopy(MouseEffectScene);
            GameObject effects = Root("Effects");
            Assert.That(effects, Is.Not.Null, "The shipped scene has the hand-authored 'Effects' object.");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Effects"), Is.SameAs(effects));
            Assert.That(Single<MouseEffectDemoController>().IsSceneBuilt, Is.True);
            SerializedObject input = new(SingleByTypeName("MouseInputManager"));
            Assert.That(input.FindProperty("enableHold").boolValue, Is.True, "Hold spawning needs OnHold events.");

            int setActive = 0;
            foreach (Toggle toggle in All<Toggle>())
            {
                if (DemoWiring.Describe(toggle.onValueChanged) == "GameObject.SetActive")
                {
                    setActive++;
                }
            }

            Assert.That(setActive, Is.EqualTo(3), "One SetActive toggle per MouseEffect object.");
            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset counter").onClick),
                Is.EqualTo("MouseEffectDemoController.ResetCounter"));
            Assert.That(Single<DemoPointerGate>().GatedCount, Is.EqualTo(3));
            AssertKitInputModule();

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
        }

        [Test]
        public void CameraConstraintBuilder_DecoratesTheShippedSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("CameraConstraintBuilder.Build");
            OpenSceneCopy(CameraConstraintScene);
            GameObject bounds = Root("9-Sliced");
            GameObject cameraObject = Root("Main Camera");
            Assert.That(bounds, Is.Not.Null, "The shipped scene has the '9-Sliced' bounds object.");
            Assert.That(cameraObject, Is.Not.Null);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("9-Sliced"), Is.SameAs(bounds));
            Assert.That(Root("Main Camera"), Is.SameAs(cameraObject));
            // WHY: the builder starts zoomed in (CameraConstraintBuilder.StartZoom) so both axes can scroll.
            Assert.That(cameraObject.GetComponent<Camera>().orthographicSize, Is.EqualTo(6f));
            Assert.That(Single<CameraConstraintDemoController>().IsSceneBuilt, Is.True);
            Assert.That(DemoWiring.Describe(FindButton("Btn_Jump up").onClick),
                Is.EqualTo("CameraConstraintDemoController.JumpVertical"));

            bool padding = false;
            foreach (Slider slider in All<Slider>())
            {
                padding |= DemoWiring.Describe(slider.onValueChanged).Contains("CameraConstraint.SetEdgePadding");
            }

            Assert.That(padding, Is.True, "The padding slider calls CameraConstraint.SetEdgePadding directly.");
            Assert.That(Root("[Demo] World"), Is.Not.Null, "Bounds markers live in a generated root.");
            AssertKitInputModule();

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
        }

        private static DemoSceneBuilderInfo Builder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static void AssertIdempotent(DemoSceneResult first, DemoSceneResult second)
        {
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        private void AssertKitInputModule()
        {
            Type wanted = DemoSceneKit.ResolveInputModule(DemoInputModule.Auto);
            EventSystem eventSystem = Single<EventSystem>();
            Assert.That(eventSystem.GetComponent(wanted), Is.Not.Null, "EventSystem uses " + wanted.Name + ".");
            if (wanted != typeof(StandaloneInputModule))
            {
                Assert.That(eventSystem.GetComponent<StandaloneInputModule>(), Is.Null,
                    "StandaloneInputModule throws in Input System-only projects.");
            }
        }

        private static int Occurrences(string text, string value)
        {
            int count = 0;
            int index = text.IndexOf(value, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
            }

            return count;
        }

        private static int CallCount(SerializedObject owner, string eventPath)
        {
            SerializedProperty calls = owner.FindProperty(eventPath + ".m_PersistentCalls.m_Calls");
            Assert.That(calls, Is.Not.Null, eventPath);
            return calls.arraySize;
        }

        private static void AssertNoMissingTargets(SerializedObject owner)
        {
            SerializedProperty iterator = owner.GetIterator();
            bool enter = true;
            List<string> missing = new();
            while (iterator.Next(enter))
            {
                enter = iterator.propertyType == SerializedPropertyType.Generic;
                if (iterator.name != "m_Target" || iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.objectReferenceValue == null)
                {
                    missing.Add(iterator.propertyPath);
                }
            }

            Assert.That(missing, Is.Empty, "Persistent listeners without a target: " + string.Join(", ", missing));
        }

        private void OpenSceneCopy(string relativeScenePath)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScenePath;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(source), Is.Not.Null, "Shipped scene missing: " + source);
            string folderName = "__NeoToolsWorldDemoTests_" + Guid.NewGuid().ToString("N");
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
            foreach (Button button in All<Button>())
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail("Button '" + name + "' not found.");
            return null;
        }

        private List<T> All<T>() where T : Component
        {
            List<T> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found;
        }

        private T Single<T>() where T : Component
        {
            List<T> found = All<T>();
            Assert.That(found.Count, Is.EqualTo(1), typeof(T).Name);
            return found[0];
        }

        private Component SingleByTypeName(string typeName)
        {
            List<Component> found = new();
            foreach (MonoBehaviour behaviour in All<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name == typeName)
                {
                    found.Add(behaviour);
                }
            }

            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
        }
    }
}
