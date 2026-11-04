using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Demo.GridSystem;
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
    ///     End-to-end checks of the GridSystem demo builders on copies of the real scenes: the pre-kit bootstrap is
    ///     retired, the hand-authored board is reused, buttons and module events are wired persistently, the
    ///     edit-mode preview is authored, and a second build changes nothing. GridSystem types are reached through
    ///     serialized fields and reflection, so this assembly needs no extra references.
    /// </summary>
    public sealed class GridSystemDemoBuildersTests
    {
        private const string Match3Scene = "Scenes/GridSystem/GridSystemMatch3Demo.unity";
        private const string TicTacToeScene = "Scenes/GridSystem/GridSystemTicTacToeDemo.unity";
        private const string DiceScene = "Scenes/GridSystem/GridSystemDiceMergeDemo.unity";
        private const string Match3Builder = "GridSystemMatch3DemoBuilder.Build";
        private const string TicTacToeBuilder = "GridSystemTicTacToeDemoBuilder.Build";
        private const string DiceBuilder = "GridSystemDiceMergeDemoBuilder.Build";

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

        [TestCase(Match3Scene, Match3Builder)]
        [TestCase(TicTacToeScene, TicTacToeBuilder)]
        [TestCase(DiceScene, DiceBuilder)]
        public void Builder_RetiresBootstrapAndRebuildsIdempotently(string scene, string builderId)
        {
            DemoSceneBuilderInfo builder = Builder(builderId);
            OpenSceneCopy(scene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Assert.That(root.name, Is.Not.EqualTo("setup"), "The runtime setup object must be retired.");
                Assert.That(root.name, Is.Not.EqualTo("Canvas"), "The pre-kit runtime canvas must be retired.");
            }

            GridDemoEditModePreview preview = Single<GridDemoEditModePreview>();
            Assert.That(preview.gameObject.activeSelf, Is.True, "The preview is visible in Edit Mode.");
            Assert.That(preview.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.GreaterThan(0));
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        [Test]
        public void Match3Builder_WiresButtonsAndModuleEvents()
        {
            OpenSceneCopy(Match3Scene);
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, new[] { Builder(Match3Builder) });
            Assert.That(result.Success, Is.True, result.Summary());

            GridSystemMatch3DemoUI controller = Single<GridSystemMatch3DemoUI>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Component match3 = Reference(controller, "_match3");
            Assert.That(DemoSceneBuildContext.IsGenerated(match3.gameObject), Is.False,
                "The hand-authored GridSystem_Match3 board must be reused.");
            Assert.That(new SerializedObject(controller).FindProperty("_cutCornersMask").objectReferenceValue, Is.Not.Null,
                "The cut-corners GridShapeMask asset must be assigned.");
            Assert.That(DemoWiring.Describe(FindButton("Btn_Restart").onClick),
                Is.EqualTo("GridSystemMatch3DemoUI.RestartBoard"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Shortest path").onClick),
                Is.EqualTo("GridSystemMatch3DemoUI.RunPathDemo"));
            Assert.That(DemoWiring.Describe(Event(match3, "OnMatchesResolved")),
                Is.EqualTo("GridSystemMatch3DemoUI.HandleMatchesResolved"));
            Assert.That(DemoWiring.Describe(Event(Reference(controller, "_generator"), "OnFieldGenerated")),
                Is.EqualTo("GridSystemMatch3DemoUI.HandleFieldGenerated"));

            Object view = Reference(controller, "_boardView");
            Assert.That(new SerializedObject(view).FindProperty("_tileSprite").objectReferenceValue, Is.Not.Null);
            Assert.That(new SerializedObject(view).FindProperty("_statusText").objectReferenceValue,
                Is.SameAs(Reference(controller, "_statusText")));
        }

        [Test]
        public void TicTacToeBuilder_ResetCallsTheServiceDirectly()
        {
            OpenSceneCopy(TicTacToeScene);
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, new[] { Builder(TicTacToeBuilder) });
            Assert.That(result.Success, Is.True, result.Summary());

            GridSystemTicTacToeDemoUI controller = Single<GridSystemTicTacToeDemoUI>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Component service = Reference(controller, "_board");
            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset board").onClick),
                Is.EqualTo("TicTacToeBoardService.ResetBoard; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(Event(service, "OnPlayerChanged")),
                Is.EqualTo("GridSystemTicTacToeDemoUI.HandlePlayerChanged"));
            Assert.That(DemoWiring.Describe(Event(service, "OnWinnerDetected")),
                Is.EqualTo("GridSystemTicTacToeDemoUI.HandleWinnerDetected"));
            Assert.That(DemoWiring.Describe(Event(service, "OnDrawDetected")),
                Is.EqualTo("GridSystemTicTacToeDemoUI.HandleDraw"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_5×5 · 4 in a row").onClick),
                Is.EqualTo("GridSystemTicTacToeDemoUI.UseFourInARowBoard"));
        }

        [Test]
        public void DiceBuilder_AssignsArtAndRemovesSavedRuntimeViews()
        {
            OpenSceneCopy(DiceScene);
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, new[] { Builder(DiceBuilder) });
            Assert.That(result.Success, Is.True, result.Summary());

            DiceMergeDemoController controller = Single<DiceMergeDemoController>();
            DiceMergeDemoView view = Single<DiceMergeDemoView>();
            SerializedObject serializedView = new(view);
            Assert.That(serializedView.FindProperty("_diceSprites").arraySize, Is.EqualTo(9));
            Assert.That(serializedView.FindProperty("_cellPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedView.FindProperty("_diePrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedView.FindProperty("_controller").objectReferenceValue, Is.SameAs(controller));
            foreach (Transform child in view.transform)
            {
                Assert.That(child.name, Does.Not.StartWith("Dice"),
                    "Runtime view roots saved by an edit-time rebuild must be removed: " + child.name);
            }

            Component dice = Reference(view, "_diceBoard");
            Assert.That(DemoWiring.Describe(Event(dice, "OnMergesResolved")),
                Is.EqualTo("DiceMergeDemoController.HandleMergesResolved"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Hint").onClick), Is.EqualTo("DiceMergeDemoController.ShowHint"));
            Assert.That(new SerializedObject(controller).FindProperty("_log").objectReferenceValue, Is.Not.Null);
        }

        [TestCase(Match3Scene, Match3Builder)]
        [TestCase(TicTacToeScene, TicTacToeBuilder)]
        [TestCase(DiceScene, DiceBuilder)]
        public void Builder_AuthorsTheBoardInAnEmptyScene(string scene, string builderId)
        {
            DemoSceneBuilderInfo builder = Builder(builderId);
            OpenSceneCopy(scene);
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Single<Camera>(), Is.Not.Null);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
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

            string folderName = "__NeoGridDemoTests_" + Guid.NewGuid().ToString("N");
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
            return value as Component;
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
