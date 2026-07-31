using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    public class NeoxiderCreateMenuTests
    {
        private HashSet<GameObject> _rootsBefore;

        [SetUp]
        public void CaptureSceneRoots()
        {
            _rootsBefore = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        [TearDown]
        public void DestroyCreatedRoots()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!_rootsBefore.Contains(root))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            Selection.activeGameObject = null;
        }

        [Test]
        public void PresetEntries_AllPrefabsExist()
        {
            Type windowType = typeof(CreateMenuObject).Assembly.GetType("Neo.CreateNeoxiderObjectWindow");
            Assert.That(windowType, Is.Not.Null);

            FieldInfo presetsField = windowType.GetField("PresetEntries",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(presetsField, Is.Not.Null);

            var entries = ((Array)presetsField.GetValue(null)).Cast<object>().ToArray();
            Assert.That(entries, Is.Not.Empty);

            foreach (object entry in entries)
            {
                var path = (string)entry.GetType().GetField("Item3").GetValue(entry);
                GameObject prefab = CreateMenuObject.GetResources<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, $"Preset prefab missing: '{path}'");
            }
        }

        [Test]
        public void CreatePreset_MissingPrefab_LogsErrorAndCreatesNothing()
        {
            int rootCount = SceneManager.GetActiveScene().rootCount;
            LogAssert.Expect(LogType.Error, new Regex("Preset prefab not found"));

            NeoxiderPresetCreateMenu.CreatePreset("Prefabs/DoesNotExist.prefab");

            Assert.That(SceneManager.GetActiveScene().rootCount, Is.EqualTo(rootCount));
        }

        [Test]
        public void CreatePreset_CreatesLinkedInstance_AndSelectsIt()
        {
            Selection.activeGameObject = null;
            NeoxiderPresetCreateMenu.CreatePreset("Prefabs/Bullet.prefab");

            GameObject instance = Selection.activeGameObject;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.name, Does.StartWith("Bullet"));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(instance), Is.True);
        }

        [Test]
        public void CreatePreset_ParentsUnderMenuContext()
        {
            var parent = new GameObject("PresetParent");
            var command = new MenuCommand(parent);

            NeoxiderPresetCreateMenu.CreatePreset("Prefabs/Bullet.prefab", command);

            GameObject instance = Selection.activeGameObject;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.transform.parent, Is.EqualTo(parent.transform));
            Assert.That(instance.transform.localPosition, Is.EqualTo(Vector3.zero));
        }

        private static void InvokePlaceInScene(GameObject instance, GameObject parent, string undoName)
        {
            MethodInfo placeInScene = typeof(CreateMenuObject).GetMethod("PlaceInScene",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(placeInScene, Is.Not.Null);
            placeInScene.Invoke(null, new object[] { instance, parent, undoName });
        }

        [Test]
        public void PlaceInScene_UIObject_GetsCanvasParent()
        {
            var uiObject = new GameObject("UiElement", typeof(RectTransform));
            try
            {
                InvokePlaceInScene(uiObject, null, "Create UiElement");

                Assert.That(uiObject.GetComponentInParent<Canvas>(true), Is.Not.Null,
                    "UI objects must be placed under a Canvas (created on demand)");
            }
            finally
            {
                // WHY: the element may land under a pre-existing Canvas that TearDown must not delete.
                UnityEngine.Object.DestroyImmediate(uiObject);
            }
        }

        [Test]
        public void PlaceInScene_CanvasInstance_IsNotNestedUnderAnotherCanvas()
        {
            var existingCanvas = new GameObject("ExistingOverlayCanvas", typeof(Canvas));
            existingCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasInstance = new GameObject("NewCanvas", typeof(Canvas));

            InvokePlaceInScene(canvasInstance, null, "Create NewCanvas");

            Assert.That(canvasInstance.transform.parent, Is.Null,
                "An instance that is itself a Canvas must stay at the stage root, not nest under another Canvas");
        }

        [Test]
        public void PlaceInScene_UIObject_PrefersScreenSpaceCanvasOverWorldSpace()
        {
            var worldCanvas = new GameObject("WorldCanvas", typeof(Canvas));
            worldCanvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var overlayCanvas = new GameObject("OverlayCanvas", typeof(Canvas));
            overlayCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var uiObject = new GameObject("UiElement", typeof(RectTransform));
            try
            {
                InvokePlaceInScene(uiObject, null, "Create UiElement");

                Canvas parentCanvas = uiObject.GetComponentInParent<Canvas>(true);
                Assert.That(parentCanvas, Is.Not.Null);
                Assert.That(parentCanvas.renderMode, Is.Not.EqualTo(RenderMode.WorldSpace),
                    "UI must prefer a screen-space canvas when one exists");
            }
            finally
            {
                // WHY: the element may land under a pre-existing Canvas that TearDown must not delete.
                UnityEngine.Object.DestroyImmediate(uiObject);
            }
        }

        [Test]
        public void PlaceInScene_FirstObject_KeepsPlainName()
        {
            var first = new GameObject("NeoUniqueNameProbe");
            InvokePlaceInScene(first, null, "Create NeoUniqueNameProbe");

            Assert.That(first.name, Is.EqualTo("NeoUniqueNameProbe"),
                "The first object must not receive a spurious ' (1)' suffix");

            var second = new GameObject("NeoUniqueNameProbe");
            InvokePlaceInScene(second, null, "Create NeoUniqueNameProbe");

            Assert.That(second.name, Is.EqualTo("NeoUniqueNameProbe (1)"),
                "A true sibling collision must still be de-duplicated");
        }

        [Test]
        public void CreateHierarchy_CreatesContainers_AndIsIdempotent()
        {
            var creator = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
            try
            {
                creator.CreateHierarchy();

                GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                Assert.That(roots.Count(r => r.name == "--System--"), Is.EqualTo(1));
                Assert.That(roots.Count(r => r.name == "--UI--"), Is.EqualTo(1));

                creator.CreateHierarchy();

                roots = SceneManager.GetActiveScene().GetRootGameObjects();
                Assert.That(roots.Count(r => r.name == "--System--"), Is.EqualTo(1),
                    "Repeated CreateHierarchy must not duplicate containers");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(creator);
            }
        }

        [Test]
        public void CreateHierarchy_FindsNestedInactiveContainer_AndDoesNotDuplicate()
        {
            var group = new GameObject("ContainerGroup");
            var nested = new GameObject("--System--");
            nested.transform.SetParent(group.transform);
            nested.SetActive(false);

            var creator = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
            try
            {
                creator.CreateHierarchy();

                GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                Assert.That(roots.Count(r => r.name == "--System--"), Is.EqualTo(0),
                    "A container nested under a group (even inactive) must be detected, not duplicated at the root");
                Assert.That(roots.Count(r => r.name == "--UI--"), Is.EqualTo(1),
                    "Missing containers must still be created");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(creator);
            }
        }
    }
}
