using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorTestsSceneAssetsTests
    {
        private string _folder;
        private Scene _activeBefore;
        private int _previewCountBefore;
        private List<SceneSnapshot> _borrowedBefore;
        private readonly List<Scene> _ownedScenes = new List<Scene>();

        [SetUp]
        public void SetUp()
        {
            _activeBefore = SceneManager.GetActiveScene();
            _previewCountBefore = EditorSceneManager.previewSceneCount;
            _borrowedBefore = CaptureScenes();
            string name = "Audit20261023NeoEditorSceneAssets_" + Guid.NewGuid().ToString("N");
            _folder = "Assets/" + name;
            Assert.That(AssetDatabase.CreateFolder("Assets", name), Is.Not.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_activeBefore.IsValid() && _activeBefore.isLoaded) SceneManager.SetActiveScene(_activeBefore);
                for (int i = _ownedScenes.Count - 1; i >= 0; i--)
                    if (_ownedScenes[i].IsValid() && _ownedScenes[i].isLoaded)
                        EditorSceneManager.CloseScene(_ownedScenes[i], true);
            }
            finally
            {
                _ownedScenes.Clear();
                if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
            }
            AssertScenes(_borrowedBefore, _activeBefore);
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(_previewCountBefore));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CopyAndRegularNativeSave_PreserveBorrowedUnsavedWorkAndSceneIdentities(bool failPopulation)
        {
            string sentinelPath = _folder + "/BorrowedDirty.unity";
            Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(sentinelPath);
            Scene sentinel = OpenOwned(sentinelPath);
            Assert.That(SceneManager.SetActiveScene(sentinel), Is.True);
            GameObject unsaved = new GameObject("UnsavedOwnedSentinel");
            unsaved.transform.position = new Vector3(3f, 4f, 5f);
            EditorSceneManager.MarkSceneDirty(sentinel);
            byte[] sentinelBytes = File.ReadAllBytes(sentinelPath);
            List<SceneSnapshot> beforeCopy = CaptureScenes();

            string path = _folder + "/Created.unity";
            Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(path);
            AssertScenes(beforeCopy, sentinel);
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.Not.EqualTo(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Audit20261023NeoEditorTestsSceneAssets.EmptySceneAsset))));
            Scene created = OpenOwned(path);
            Assert.That(EditorSceneManager.IsPreviewScene(created), Is.False);
            Assert.That(created.path, Is.EqualTo(path));
            Assert.That(created.GetRootGameObjects(), Is.Empty);
            bool observedFailure = false;
            try
            {
                Assert.That(SceneManager.SetActiveScene(created), Is.True);
                new GameObject("OwnedPopulation");
                if (failPopulation) throw new InvalidOperationException("OwnedPopulationFailure");
                Assert.That(EditorSceneManager.SaveScene(created, path), Is.True);
            }
            catch (InvalidOperationException exception) when (exception.Message == "OwnedPopulationFailure")
            {
                observedFailure = true;
            }
            finally
            {
                SceneManager.SetActiveScene(sentinel);
                EditorSceneManager.CloseScene(created, true);
            }

            Assert.That(observedFailure, Is.EqualTo(failPopulation));
            AssertScenes(beforeCopy, sentinel);
            Assert.That(unsaved != null, Is.True);
            Assert.That(unsaved.transform.position, Is.EqualTo(new Vector3(3f, 4f, 5f)));
            Assert.That(sentinel.isDirty, Is.True);
            Assert.That(File.ReadAllBytes(sentinelPath), Is.EqualTo(sentinelBytes));
            if (!failPopulation)
            {
                using (Audit20261023NeoEditorTestsPreviewSceneScope preview = new Audit20261023NeoEditorTestsPreviewSceneScope(path))
                {
                    GameObject[] roots = preview.Scene.GetRootGameObjects();
                    Assert.That(roots, Has.Length.EqualTo(1));
                    Assert.That(roots[0].name, Is.EqualTo("OwnedPopulation"));
                }
            }
        }

        [Test]
        public void Copy_RejectsExistingPathWithoutChangingBytesGuidOrBorrowedSceneSetup()
        {
            string path = _folder + "/Existing.unity";
            Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            byte[] bytes = File.ReadAllBytes(path);
            Assert.Throws<InvalidOperationException>(() => Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(path));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            AssertScenes(_borrowedBefore, _activeBefore);
        }

        private Scene OpenOwned(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            _ownedScenes.Add(scene);
            return scene;
        }

        private static List<SceneSnapshot> CaptureScenes()
        {
            List<SceneSnapshot> snapshots = new List<SceneSnapshot>();
            for (int i = 0; i < SceneManager.sceneCount; i++) snapshots.Add(new SceneSnapshot(SceneManager.GetSceneAt(i)));
            return snapshots;
        }

        private static void AssertScenes(IReadOnlyList<SceneSnapshot> expected, Scene active)
        {
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; i++)
            {
                Scene current = SceneManager.GetSceneAt(i);
                SceneSnapshot prior = expected[i];
                Assert.That(current, Is.EqualTo(prior.Scene));
                Assert.That(current.path, Is.EqualTo(prior.Path));
                Assert.That(current.isLoaded, Is.EqualTo(prior.Loaded));
                Assert.That(current.isDirty, Is.EqualTo(prior.Dirty));
                if (prior.Loaded) CollectionAssert.AreEqual(prior.Roots, current.GetRootGameObjects());
            }
        }

        private readonly struct SceneSnapshot
        {
            internal readonly Scene Scene;
            internal readonly string Path;
            internal readonly bool Loaded;
            internal readonly bool Dirty;
            internal readonly GameObject[] Roots;

            internal SceneSnapshot(Scene scene)
            {
                Scene = scene;
                Path = scene.path;
                Loaded = scene.isLoaded;
                Dirty = scene.isDirty;
                Roots = Loaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>();
            }
        }
    }
}
