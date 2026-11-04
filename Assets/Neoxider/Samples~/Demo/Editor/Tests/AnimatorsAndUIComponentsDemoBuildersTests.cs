using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Samples;
using NUnit.Framework;
using TMPro;
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
    ///     End-to-end checks of the Neo.Animations and Neo.UI "UI Components" builders in an unsaved scene: the
    ///     NoCode bindings from the animators and UI components to their targets, the controller references,
    ///     kit validation and rebuild idempotency (the second pass creates and removes nothing). Module types are
    ///     reached by name and reflection, so this assembly needs no Neo.Animations / Neo.UI reference.
    /// </summary>
    public sealed class AnimatorsAndUIComponentsDemoBuildersTests
    {
        private Scene _scene;
        private Scene _previousActive;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
            _scene = DemoTestScenes.OpenEmpty("__NeoAnimatorsUIComponentsTests", out _folder);
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
        public void AnimatorsBuilder_BindsFourAnimatorsToRealTargetsIdempotently()
        {
            DemoSceneBuilderInfo builder = FindBuilder("AnimatorsDemoBuilder.Build");
            Assert.That(builder.ScenePath, Is.EqualTo("Scenes/Animations/AnimatorsDemo.unity"));

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());

            AnimatorsDemoController controller = Single<AnimatorsDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, first.Summary());
            Assert.That(DemoWiring.Describe(FindButton("Btn_Play").onClick), Is.EqualTo("AnimatorsDemoController.PlayAll"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Pause").onClick), Is.EqualTo("AnimatorsDemoController.PauseAll"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Resume").onClick), Is.EqualTo("AnimatorsDemoController.ResumeAll"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Stop").onClick), Is.EqualTo("AnimatorsDemoController.StopAll"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Shake() impulse").onClick),
                Is.EqualTo("TransformAnimator.Shake; DemoLogFeed.Log"), "The impulse is wired straight to the animator.");

            Object vector = Reference(controller, "_vector");
            Assert.That(DemoWiring.Describe(EventField(vector, "OnVectorChanged")), Is.EqualTo("Transform.set_localScale"));
            Object color = Reference(controller, "_color");
            Assert.That(DemoWiring.Describe(EventField(color, "OnColorChanged")),
                Is.EqualTo("SpriteRenderer.set_color; Image.set_color"));
            Object floatAnimator = Reference(controller, "_float");
            Assert.That(DemoWiring.Describe(FloatValueChanged(floatAnimator)),
                Is.EqualTo("Image.set_fillAmount; DemoValueReadout.SetFloat"));
            Assert.That(DemoWiring.Describe(EventField(floatAnimator, "OnAnimationPaused")),
                Is.EqualTo("DemoValueReadout.SetText"));
            Assert.That(Reference(controller, "_transform").GetType().Name, Is.EqualTo("TransformAnimator"));

            TMP_Dropdown type = (TMP_Dropdown)Reference(controller, "_typeDropdown");
            Assert.That(type.options.Count, Is.EqualTo(9), "One option per AnimationType value.");
            Assert.That(DemoWiring.Describe(type.onValueChanged), Is.EqualTo("AnimatorsDemoController.SetAnimationType"));
            Slider speed = (Slider)Reference(controller, "_speedSlider");
            Assert.That(DemoWiring.Describe(speed.onValueChanged), Does.Contain("AnimatorsDemoController.SetSpeed"));

            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("Neo.Animations"));

            AssertStableRebuild(builder, first);
        }

        [Test]
        public void UIComponentsBuilder_WiresSixComponentsThroughTheirPublicApi()
        {
            DemoSceneBuilderInfo builder = FindBuilder("UIComponentsDemoBuilder.Build");
            Assert.That(builder.ScenePath, Is.EqualTo("Scenes/UI/UIComponentsDemo.unity"));

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());

            Component bar = SingleByName("CategoryBar");
            SerializedObject barData = new(bar);
            Assert.That(barData.FindProperty("_categories").arraySize, Is.EqualTo(4));
            Assert.That(barData.FindProperty("_items").arraySize, Is.EqualTo(4));
            Assert.That(barData.FindProperty("_categories.Array.data[3].Disabled").boolValue, Is.True,
                "The 'Locked' entry demonstrates a disabled category.");
            Assert.That(barData.FindProperty("_selectionMarker").objectReferenceValue, Is.Not.Null);
            Assert.That(DemoWiring.Describe(EventField(bar, "OnCategorySelected")), Is.EqualTo("VariantView.SetCurrentState"));
            Assert.That(DemoWiring.Describe(EventField(bar, "OnCategoryIdSelected")), Is.EqualTo("DemoValueReadout.SetText"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Next tab").onClick), Is.EqualTo("CategoryBar.Next; DemoLogFeed.Log"));

            Assert.That(Count("VariantView"), Is.EqualTo(2));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Next state").onClick),
                Is.EqualTo("VariantView.NextState; DemoLogFeed.Log"));

            Component loader = SingleByName("FakeLoad");
            Assert.That(DemoWiring.Describe(EventField(loader, "OnChange")), Is.EqualTo("Image.set_fillAmount"));
            Assert.That(DemoWiring.Describe(EventField(loader, "OnFinisLoad")),
                Is.EqualTo("DemoValueReadout.SetText; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_EndLoad()").onClick), Does.StartWith("FakeLoad.EndLoad"));

            Assert.That(Count("ButtonShake"), Is.EqualTo(3));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Shake()").onClick), Does.StartWith("ButtonShake.Shake"));

            List<Component> anchors = FindAllByName("AnchorMove");
            Assert.That(anchors.Count, Is.EqualTo(3));
            foreach (Component anchor in anchors)
            {
                SerializedObject data = new(anchor);
                Vector2 point = new(data.FindProperty("x").floatValue, data.FindProperty("y").floatValue);
                RectTransform rect = (RectTransform)anchor.transform;
                Assert.That(rect.anchorMin, Is.EqualTo(point), anchor.name);
                Assert.That(rect.anchorMax, Is.EqualTo(point), anchor.name);
            }

            GameObject anchorSliderRow = FindObject("Slider · AnchorMove.SetX(float)");
            Assert.That(anchorSliderRow, Is.Not.Null);
            Slider anchorSlider = anchorSliderRow.GetComponentInChildren<Slider>(true);
            Assert.That(DemoWiring.Describe(anchorSlider.onValueChanged), Does.Contain("AnchorMove.SetX"),
                "The slider drives the centre marker through the one-argument AnchorMove.SetX.");

            GameObject pausePage = FindObject("Pause Page");
            Assert.That(pausePage, Is.Not.Null);
            Assert.That(pausePage.activeSelf, Is.False, "PausePage pauses while active, so the screen is saved closed.");
            Assert.That(pausePage.GetComponent("PausePage"), Is.Not.Null);
            Assert.That(DemoWiring.Describe(FindButton("Btn_Open pause page").onClick),
                Is.EqualTo("GameObject.SetActive; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Resume · close the page").onClick),
                Is.EqualTo("GameObject.SetActive; DemoLogFeed.Log"));
            Assert.That(Count("TransformAnimator"), Is.EqualTo(2), "One scaled and one unscaled clock.");

            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.Hints.Count, Is.EqualTo(6), "One info line per component.");

            AssertStableRebuild(builder, first);
            Assert.That(FindObject("Pause Page").activeSelf, Is.False, "A rebuild keeps the pause screen closed.");
        }

        private void AssertStableRebuild(DemoSceneBuilderInfo builder, DemoSceneResult first)
        {
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        private static DemoSceneBuilderInfo FindBuilder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static Object Reference(Object owner, string field)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(field);
            Assert.That(property, Is.Not.Null, owner.GetType().Name + "." + field);
            Assert.That(property.objectReferenceValue, Is.Not.Null, owner.GetType().Name + "." + field);
            return property.objectReferenceValue;
        }

        private static UnityEventBase EventField(Object owner, string field)
        {
            FieldInfo info = owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, owner.GetType().Name + "." + field);
            UnityEventBase unityEvent = info.GetValue(owner) as UnityEventBase;
            Assert.That(unityEvent, Is.Not.Null, owner.GetType().Name + "." + field);
            return unityEvent;
        }

        private static UnityEventBase FloatValueChanged(Object floatAnimator)
        {
            FieldInfo valueField = floatAnimator.GetType().GetField("Value", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(valueField, Is.Not.Null, "FloatAnimator.Value");
            object value = valueField.GetValue(floatAnimator);
            PropertyInfo changed = value.GetType().GetProperty("OnChanged", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(changed, Is.Not.Null, "FloatAnimator.Value.OnChanged");
            return (UnityEventBase)changed.GetValue(value);
        }

        private GameObject FindObject(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == name)
                    {
                        return node.gameObject;
                    }
                }
            }

            return null;
        }

        private Button FindButton(string name)
        {
            GameObject host = FindObject(name);
            Assert.That(host, Is.Not.Null, name);
            Button button = host.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name);
            return button;
        }

        private List<Component> FindAllByName(string typeName)
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

        private int Count(string typeName)
        {
            return FindAllByName(typeName).Count;
        }

        private Component SingleByName(string typeName)
        {
            List<Component> found = FindAllByName(typeName);
            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
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
