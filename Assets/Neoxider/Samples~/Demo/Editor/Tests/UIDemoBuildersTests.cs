using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     End-to-end checks of the UI demo builders (AnimationFly, UI Mesh Rig) in an unsaved scene: wiring,
    ///     serialized references, kit validation and rebuild idempotency (second pass creates and removes nothing).
    /// </summary>
    public sealed class UIDemoBuildersTests
    {
        private Scene _scene;
        private Scene _previousActive;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
            _scene = DemoTestScenes.OpenEmpty("__NeoUIDemoBuildersTests", out _folder);
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
        public void AnimationFlyBuilder_AuthorsAWiredSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = FindBuilder("AnimationFlyDemoBuilder.Build");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());

            AnimationFlyDemoController controller = Single<AnimationFlyDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(controller.DemoButtonCount, Is.GreaterThanOrEqualTo(10));
            Assert.That(controller.DemoSliderCount, Is.EqualTo(6));
            Assert.That(controller.FlyRootBlocksRaycasts, Is.False, "The fly layer must not block button raycasts.");

            SerializedObject serialized = new(controller);
            UnityEngine.Object prefab = serialized.FindProperty("_prefabVisual").objectReferenceValue;
            Assert.That(prefab != null && EditorUtility.IsPersistent(prefab), Is.True, "Pooled flight needs a prefab asset.");
            Assert.That(serialized.FindProperty("_coinSprite").objectReferenceValue, Is.Not.Null);

            Assert.That(DemoWiring.Describe(FindButton("Btn_World -> Wallet").onClick),
                Is.EqualTo("AnimationFlyDemoController.PlayWorldToWallet"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset counters").onClick),
                Is.EqualTo("AnimationFlyDemoController.ResetCounters"));
            Slider duration = (Slider)serialized.FindProperty("_durationSlider").objectReferenceValue;
            Assert.That(DemoWiring.Describe(duration.onValueChanged),
                Does.Contain("DemoValueReadout.SetFloat").And.Contain("AnimationFlyDemoController.SetDuration"));

            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("AnimationFly"));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        [Test]
        public void UIMeshRigBuilder_AddsSpriteRendererAdapterAndSavesOnlyPersistentSprites()
        {
            DemoSceneBuilderInfo builder = FindBuilder("UIMeshRigDemoBuilder.Build");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());

            GameObject rig = FindObject("Sprite Renderer - Character");
            Assert.That(rig, Is.Not.Null);
            Assert.That(CountComponents(rig, "UIMeshRigPoint"), Is.EqualTo(4), "Character layout has four points.");
            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.StartCollapsed, Is.True);

            // WHY: force the adapter to put its runtime sprite clone on the renderer, as opening the scene does.
            Component adapter = rig.GetComponent("UIMeshRigSpriteRenderer");
            MethodInfo rebuild = adapter.GetType().GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(rebuild, Is.Not.Null);
            rebuild.Invoke(adapter, null);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            SpriteRenderer renderer = rig.GetComponent<SpriteRenderer>();
            Assert.That(renderer.sprite != null && EditorUtility.IsPersistent(renderer.sprite), Is.True,
                "The saved SpriteRenderer must reference the source asset, not the runtime clone.");
            Assert.That(CountComponents(rig, "UIMeshRigPoint"), Is.EqualTo(4), "Rebuilds keep the authored points.");
        }

        private static DemoSceneBuilderInfo FindBuilder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static int CountComponents(GameObject root, string typeName)
        {
            int count = 0;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().Name == typeName)
                {
                    count++;
                }
            }

            return count;
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
