using System;
using System.Collections.Generic;
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
    ///     End-to-end contracts of the Audio, Parallax, Light and Selector demo builders: each builder runs twice on a
    ///     temporary copy of its real scene, succeeds, assigns the controller references, wires the controls to the
    ///     module components and creates or removes nothing on the second pass.
    /// </summary>
    public sealed class ViewMediaDemoBuilderTests
    {
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
        public void AudioBuilder_WiresAmMusicControlAndSettings()
        {
            DemoSceneResult first = BuildTwice("Scenes/Audio/AudioDemo.unity", "AudioDemoBuilder.Build", out DemoSceneResult second);

            AudioDemoController controller = Single<AudioDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, first.Summary());
            Assert.That(Reference(controller, "_audio").GetType().Name, Is.EqualTo("AM"));
            Assert.That(Reference(controller, "_settings").GetType().Name, Is.EqualTo("AMSettings"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Shot").onClick), Is.EqualTo("AM.Play; AudioDemoController.LogSound"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Theme (loop)").onClick), Does.StartWith("MusicControl.PlayPool"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Stop music").onClick), Does.StartWith("MusicControl.StopMusic"));
            Assert.That(FindButton("Btn_UI click").GetComponent("PlayAudioBtn"), Is.Not.Null);
            Slider musicVolume = (Slider)Reference(controller, "_musicVolume");
            Assert.That(DemoWiring.Describe(musicVolume.onValueChanged),
                Does.Contain("AMSettings.SetMusicVolume").And.Contain("AudioDemoController.LogMusicVolume"));
            AssertStable(first, second);
        }

        [Test]
        public void ParallaxBuilder_AuthorsBoundLayersAndCameraControls()
        {
            DemoSceneResult first = BuildTwice("Scenes/Parallax/ParallaxDemo.unity", "ParallaxDemoBuilder.Build",
                out DemoSceneResult second);

            ParallaxDemoController controller = Single<ParallaxDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, first.Summary());
            SerializedProperty layers = new SerializedObject(controller).FindProperty("_layers");
            Assert.That(layers.arraySize, Is.EqualTo(7));
            Object camera = Reference(controller, "_camera");
            for (int i = 0; i < layers.arraySize; i++)
            {
                Object layer = layers.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(layer, Is.Not.Null);
                Assert.That(new SerializedObject(layer).FindProperty("targetCamera").objectReferenceValue, Is.SameAs(camera),
                    layer.name + " must be bound to the scene camera explicitly.");
            }

            Assert.That(DemoWiring.Describe(FindButton("Btn_Reverse").onClick), Is.EqualTo("ParallaxDemoController.Reverse"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Jump +25").onClick), Is.EqualTo("ParallaxDemoController.Jump"));
            AssertStable(first, second);
        }

        [Test]
        public void LightBuilder_KeepsTheRigAndBindsTheReactiveReadoutOnce()
        {
            DemoSceneResult first = BuildTwice("Scenes/Tools/View/Light.unity", "LightDemoBuilder.Build", out DemoSceneResult second);

            LightDemoController controller = Single<LightDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, first.Summary());
            Assert.That(new SerializedObject(controller).FindProperty("_lights").arraySize, Is.EqualTo(5),
                "The five hand-authored lights must stay and be driven by the card.");
            Assert.That(DemoWiring.Describe(FindButton("Btn_Step 0.1 s").onClick), Is.EqualTo("LightDemoController.Step"));
            Assert.That(CountListenersTo("DemoValueReadout", "LightAnimator"), Is.EqualTo(1),
                "Rebuilding must not duplicate the Intensity.OnChanged readout listener on the hand-authored light.");
            AssertStable(first, second);
        }

        [Test]
        public void SelectorBuilder_AuthorsThreeWiredSelectorsAndRetiresTheLegacyCanvas()
        {
            DemoSceneResult first = BuildTwice("Scenes/Tools/View/Selector.unity", "SelectorDemoBuilder.Build",
                out DemoSceneResult second);

            SelectorDemoController controller = Single<SelectorDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, first.Summary());
            Assert.That(DemoWiring.Describe(FindButton("Btn_Next skin").onClick), Is.EqualTo("Selector.Next"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Damage").onClick), Is.EqualTo("Selector.Previous"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Full HP").onClick), Is.EqualTo("Selector.SetLast"));
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Assert.That(root.name, Is.Not.EqualTo("Canvas"), "The pre-kit 800x600 canvas must be retired.");
            }

            AssertStable(first, second);
        }

        private DemoSceneResult BuildTwice(string relativeScenePath, string builderSuffix, out DemoSceneResult second)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(builderSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, builderSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            OpenCopy(relativeScenePath);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            return first;
        }

        private static void AssertStable(DemoSceneResult first, DemoSceneResult second)
        {
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners), second.Summary());
        }

        private void OpenCopy(string relativeScenePath)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source) == null)
            {
                Assert.Ignore("Demo scene not available in this layout: " + source);
            }

            string folderName = "__NeoViewMediaBuilderTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + System.IO.Path.GetFileName(relativeScenePath);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not open: " + path);
            SceneManager.SetActiveScene(_scene);
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

            Assert.Fail("Missing demo button " + name);
            return null;
        }

        private int CountListenersTo(string targetTypeName, string ownerTypeName)
        {
            int count = 0;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour.GetType().Name != ownerTypeName)
                    {
                        continue;
                    }

                    SerializedProperty iterator = new SerializedObject(behaviour).GetIterator();
                    while (iterator.Next(true))
                    {
                        if (iterator.name != "m_Target" || iterator.propertyType != SerializedPropertyType.ObjectReference)
                        {
                            continue;
                        }

                        Object target = iterator.objectReferenceValue;
                        if (target != null && target.GetType().Name == targetTypeName)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private static Object Reference(Object owner, string field)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(field);
            Assert.That(property, Is.Not.Null, owner.GetType().Name + "." + field);
            Object value = property.objectReferenceValue;
            Assert.That(value, Is.Not.Null, owner.GetType().Name + "." + field + " must be assigned by the builder.");
            return value;
        }
    }
}
