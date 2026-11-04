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

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contracts of InventoryUseDemoBuilder: an empty scene is authored with the slot grids, NoCode slot
    ///     buttons and persistent module wiring and rebuilds without changes; the shipped scene keeps its hand-authored
    ///     containers and controller object. Module types are checked by name so this assembly needs no inventory
    ///     reference.
    /// </summary>
    public sealed class InventoryDemoBuilderTests
    {
        private const string ShippedScene = "Scenes/Inventory/InventoryUseDemo.unity";
        private const string BuilderId = "InventoryUseDemoBuilder.Build";

        private static readonly string[] ContainerNames =
        {
            "Hotbar - nine permanent slots",
            "Temporary catch - four slots",
            "Podium - exact unique instance"
        };

        private Scene _scene;
        private Scene _previousActive;
        private string _folder;

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
        public void InventoryBuilder_AuthorsAWiredSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder();
            _scene = DemoTestScenes.OpenEmpty("__NeoInventoryDemoTests", out _folder);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            InventoryDemoController controller = Single<InventoryDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            AssertWiring();
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            AssertWiring();
        }

        [Test]
        public void InventoryBuilder_KeepsTheShippedContainersAndController()
        {
            DemoSceneBuilderInfo builder = Builder();
            OpenSceneCopy(ShippedScene);
            List<GameObject> containers = new();
            foreach (string name in ContainerNames)
            {
                GameObject container = Root(name);
                Assert.That(container, Is.Not.Null, "The shipped scene has a hand-authored '" + name + "'.");
                containers.Add(container);
            }

            GameObject host = Root("Inventory demo - select and use");
            Assert.That(host, Is.Not.Null);
            InventoryDemoController controller = host.GetComponent<InventoryDemoController>();
            Assert.That(controller, Is.Not.Null);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            for (int i = 0; i < ContainerNames.Length; i++)
            {
                Assert.That(Root(ContainerNames[i]), Is.SameAs(containers[i]), ContainerNames[i] + " must survive.");
            }

            Assert.That(Root("Inventory demo - select and use"), Is.SameAs(host));
            Assert.That(Single<InventoryDemoController>(), Is.SameAs(controller), "No second controller is generated.");
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(SlotCount(containers[0]), Is.EqualTo(9));
            Assert.That(SlotCount(containers[1]), Is.EqualTo(4));
            Assert.That(SlotCount(containers[2]), Is.EqualTo(1));
            AssertWiring();

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            AssertWiring();
        }

        private void AssertWiring()
        {
            Assert.That(DemoWiring.Describe(FindButton("Btn_Slot 3").onClick), Is.EqualTo("InventoryHand.SetSlotIndex"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Use selected [E]").onClick),
                Is.EqualTo("InventoryDemoController.UseSelected"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Deposit catch").onClick),
                Is.EqualTo("InventoryDemoController.Deposit"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset demo").onClick),
                Is.EqualTo("InventoryDemoController.ResetDemo"));

            SerializedObject hand = new(SingleByTypeName("InventoryHand"));
            Assert.That(Calls(hand, "OnUseSelectionRequested"), Is.EqualTo(new[] { "UseSelection" }));
            Assert.That(Calls(hand, "OnEquippedChanged"), Is.EqualTo(new[] { "OnEquippedChanged" }));
            Assert.That(hand.FindProperty("_allowUseInput").boolValue, Is.False);

            SerializedObject use = new(SingleByTypeName("InventoryUseAction"));
            Assert.That(use.FindProperty("_listenToHand").boolValue, Is.False,
                "The persistent hand -> action call replaces the runtime subscription (no double consumption).");
            Assert.That(use.FindProperty("_itemId").intValue, Is.EqualTo(InventoryDemoController.CellId));
            Assert.That(Calls(use, "OnUsed"), Is.EqualTo(new[] { "OnCellUsed" }));

            List<Component> inventories = AllByTypeName("InventoryComponent");
            Assert.That(inventories.Count, Is.EqualTo(3));
            foreach (Component inventory in inventories)
            {
                SerializedObject serialized = new(inventory);
                Assert.That(serialized.FindProperty("_setInstanceOnAwake").boolValue, Is.False, inventory.name);
                Assert.That(serialized.FindProperty("_database").objectReferenceValue, Is.Not.Null, inventory.name);
                Assert.That(Calls(serialized, "OnInventoryChanged"), Is.EqualTo(new[] { "Refresh" }), inventory.name);
            }

            int cells = 0;
            foreach (Component grid in AllByTypeName("InventorySlotGridView"))
            {
                SerializedObject serialized = new(grid);
                Assert.That(serialized.FindProperty("_enableClickTransfer").boolValue, Is.False, grid.name);
                Assert.That(serialized.FindProperty("_inventory").objectReferenceValue, Is.Not.Null, grid.name);
                cells += serialized.FindProperty("_manualSlots").arraySize;
            }

            Assert.That(cells, Is.EqualTo(InventoryDemoController.HotbarSlots + 4 + 1));
            Assert.That(AllByTypeName("InventoryTotalCountText").Count, Is.EqualTo(2));
            Assert.That(AllByTypeName("InventoryItemCountText").Count, Is.EqualTo(1));
        }

        private static DemoSceneBuilderInfo Builder()
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(BuilderId, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, BuilderId + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static int SlotCount(GameObject container)
        {
            Component inventory = container.GetComponent("InventoryComponent");
            Assert.That(inventory, Is.Not.Null, container.name);
            return new SerializedObject(inventory).FindProperty("_slotCount").intValue;
        }

        private static string[] Calls(SerializedObject owner, string eventPath)
        {
            owner.Update();
            SerializedProperty calls = owner.FindProperty(eventPath + ".m_PersistentCalls.m_Calls");
            Assert.That(calls, Is.Not.Null, eventPath);
            string[] methods = new string[calls.arraySize];
            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                Assert.That(call.FindPropertyRelative("m_Target").objectReferenceValue, Is.Not.Null, eventPath);
                methods[i] = call.FindPropertyRelative("m_MethodName").stringValue;
            }

            return methods;
        }

        private void OpenSceneCopy(string relativeScenePath)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScenePath;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(source), Is.Not.Null, "Shipped scene missing: " + source);
            string folderName = "__NeoInventoryDemoTests_" + Guid.NewGuid().ToString("N");
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
