using System;
using System.Collections.Generic;
using Neo.Editor.Windows;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorMissingScriptsTests
    {
        private string _folder;
        private Scene _previousActive;
        private Scene _protectedScene;
        private GameObject _unsavedObject;
        private FindAndRemoveMissingScriptsWindowGUI _scanner;
        private readonly List<(Scene Scene, string Path, bool Loaded, bool Dirty, GameObject[] Roots)> _borrowedScenes = new();
        private int _previousPreviewCount;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
            _previousPreviewCount = EditorSceneManager.previewSceneCount;
            _protectedScene = default;
            _unsavedObject = null;
            _scanner = null;
            _borrowedScenes.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                _borrowedScenes.Add((scene, scene.path, scene.isLoaded, scene.isDirty,
                    scene.isLoaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>()));
            }

            try
            {
                string folderName = "Audit20261023NeoEditorMissing_" + Guid.NewGuid().ToString("N");
                AssetDatabase.CreateFolder("Assets", folderName);
                _folder = "Assets/" + folderName;
                string protectedPath = _folder + "/Protected.unity";
                Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(protectedPath);
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(protectedPath), Is.Not.Null);
                _protectedScene = EditorSceneManager.OpenScene(protectedPath, OpenSceneMode.Additive);
                Assert.That(EditorSceneManager.IsPreviewScene(_protectedScene), Is.False);
                Assert.That(_protectedScene.path, Is.EqualTo(protectedPath));
                Assert.That(_protectedScene.GetRootGameObjects(), Is.Empty);
                Assert.That(SceneManager.SetActiveScene(_protectedScene), Is.True);
                _unsavedObject = new GameObject("Unsaved work");
                _unsavedObject.transform.position = new Vector3(3f, 4f, 5f);
                EditorSceneManager.MarkSceneDirty(_protectedScene);
                _scanner = new FindAndRemoveMissingScriptsWindowGUI();
            }
            catch
            {
                TearDown();
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _scanner?.OnDisable();
            }
            finally
            {
                _scanner = null;
                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene == _protectedScene || (!string.IsNullOrEmpty(_folder) &&
                        scene.path.StartsWith(_folder + "/", StringComparison.Ordinal)))
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
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

                Assert.That(SceneManager.sceneCount, Is.EqualTo(_borrowedScenes.Count));
                Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(_previousPreviewCount));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousActive));
                for (int i = 0; i < _borrowedScenes.Count; i++)
                {
                    Scene expected = _borrowedScenes[i].Scene;
                    Assert.That(SceneManager.GetSceneAt(i), Is.EqualTo(expected));
                    Assert.That(expected.path, Is.EqualTo(_borrowedScenes[i].Path));
                    Assert.That(expected.isLoaded, Is.EqualTo(_borrowedScenes[i].Loaded));
                    Assert.That(expected.isDirty, Is.EqualTo(_borrowedScenes[i].Dirty));
                    if (_borrowedScenes[i].Loaded)
                    {
                        CollectionAssert.AreEqual(_borrowedScenes[i].Roots, expected.GetRootGameObjects());
                    }
                }
            }
        }

        [Test]
        public void ScanClosedScene_PreservesDirtyUnsavedSceneAndClosesPreview()
        {
            string path = CreateSceneWithDuplicateRootNames();
            int sceneCount = SceneManager.sceneCount;
            int previews = EditorSceneManager.previewSceneCount;

            _scanner.ScanSceneAsset(path);

            Assert.That(_scanner.ResultCount, Is.EqualTo(2));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previews));
            AssertProtectedScene(sceneCount);
        }

        [Test]
        public void RepairClosedScene_PersistsEveryDuplicateNamedObjectWithoutReplacingOpenScenes()
        {
            string path = CreateSceneWithDuplicateRootNames();
            int sceneCount = SceneManager.sceneCount;
            _scanner.ScanSceneAsset(path);

            Assert.That(_scanner.RemoveAllMissingScripts(), Is.EqualTo(2));
            Assert.That(_scanner.ResultCount, Is.Zero);
            AssertProtectedScene(sceneCount);
            Assert.That(CountMissingInSavedScene(path), Is.Zero);
        }

        [Test]
        public void RepairLoadedScene_LeavesUnrelatedEditsUnsavedAndSceneOpen()
        {
            string path = CreateSceneWithDuplicateRootNames();
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            GameObject healthy = scene.GetRootGameObjects()[0];
            healthy.transform.position = Vector3.one * 17f;
            EditorSceneManager.MarkSceneDirty(scene);
            _scanner.ScanSceneAsset(path);

            Assert.That(_scanner.RemoveAllMissingScripts(), Is.EqualTo(2));

            Assert.That(scene.isLoaded, Is.True);
            Assert.That(scene.isDirty, Is.True);
            Assert.That(healthy.transform.position, Is.EqualTo(Vector3.one * 17f));
            Assert.That(CountMissingInSavedScene(path), Is.EqualTo(2),
                "Repair must not implicitly save unrelated edits in an already open scene.");
        }

        [Test]
        public void RepairPrefab_PersistsCorrectDuplicateNamedChildIncludingSlashInName()
        {
            string path = CreatePrefabWithDuplicateChildNames();
            _scanner.ScanPrefabAsset(path);
            Assert.That(_scanner.ResultCount, Is.EqualTo(1));

            Assert.That(_scanner.RemoveAllMissingScripts(), Is.EqualTo(1));

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Assert.That(CountMissing(contents), Is.Zero);
                Assert.That(contents.transform.childCount, Is.EqualTo(2));
                Assert.That(contents.transform.GetChild(0).name, Is.EqualTo("Same/Name"));
                Assert.That(contents.transform.GetChild(1).name, Is.EqualTo("Same/Name"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [Test]
        public void RestartAndCancelSearch_ResetProgressAndStopFurtherScanning()
        {
            string path = CreatePrefabWithDuplicateChildNames();
            _scanner.BeginSearch(Array.Empty<string>(), new[] { path });
            _scanner.SearchAll();
            Assert.That(_scanner.ResultCount, Is.EqualTo(1));

            _scanner.BeginSearch(Array.Empty<string>(), new[] { path });
            Assert.That(_scanner.ResultCount, Is.Zero);
            _scanner.SearchAll();
            Assert.That(_scanner.ResultCount, Is.EqualTo(1));
            _scanner.SearchAll();
            Assert.That(_scanner.IsSearching, Is.False);

            _scanner.BeginSearch(Array.Empty<string>(), new[] { path });
            _scanner.OnDisable();
            _scanner.SearchAll();
            Assert.That(_scanner.IsSearching, Is.False);
            Assert.That(_scanner.ResultCount, Is.Zero);
        }

        private void AssertProtectedScene(int sceneCount)
        {
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_protectedScene));
            Assert.That(_protectedScene.isDirty, Is.True);
            Assert.That(_unsavedObject != null, Is.True);
            Assert.That(_unsavedObject.transform.position, Is.EqualTo(new Vector3(3f, 4f, 5f)));
        }

        private string CreateSceneWithDuplicateRootNames()
        {
            string path = _folder + "/Missing.unity";
            string secondPath = _folder + "/MissingSecond.unity";
            Audit20261023NeoEditorTestsSceneAssets.CreateMissingScriptSceneAsset(path);
            Audit20261023NeoEditorTestsSceneAssets.CreateMissingScriptSceneAsset(secondPath);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Scene secondScene = default;
            try
            {
                Assert.That(SceneManager.SetActiveScene(scene), Is.True);
                GameObject healthy = RequireMissingSeedRoot(scene);
                GameObject first = healthy.transform.GetChild(0).gameObject;
                first.transform.SetParent(null);
                secondScene = EditorSceneManager.OpenScene(secondPath, OpenSceneMode.Additive);
                GameObject secondRoot = RequireMissingSeedRoot(secondScene);
                GameObject second = secondRoot.transform.GetChild(0).gameObject;
                second.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(second, scene);
                healthy.name = "Same/Name";
                first.name = "Same/Name";
                second.name = "Same/Name";
                healthy.transform.SetSiblingIndex(0);
                first.transform.SetSiblingIndex(1);
                second.transform.SetSiblingIndex(2);
                Assert.That(scene.GetRootGameObjects(), Has.Length.EqualTo(3));
                Assert.That(CountMissing(healthy), Is.Zero);
                Assert.That(CountMissing(first), Is.EqualTo(1));
                Assert.That(CountMissing(second), Is.EqualTo(1));
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            }
            finally
            {
                SceneManager.SetActiveScene(_protectedScene);
                try
                {
                    if (secondScene.IsValid() && secondScene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(secondScene, true);
                    }
                }
                finally
                {
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            Assert.That(CountMissingInSavedScene(path), Is.EqualTo(2));
            return path;
        }

        private string CreatePrefabWithDuplicateChildNames()
        {
            string path = _folder + "/Missing.prefab";
            const string seedGuid = "42cabb436c0342fcbe9a2fe50338e2f1";
            string sourcePath = AssetDatabase.GUIDToAssetPath(seedGuid);
            UnityEngine.Object[] selection = Selection.objects;
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath), Is.Not.Null);
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.Empty);
            Assert.That(AssetDatabase.CopyAsset(sourcePath, path), Is.True);
            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(EditorUtility.IsPersistent(prefab), Is.True);
            Assert.That(PrefabUtility.IsPartOfPrefabAsset(prefab), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(path));
            string copiedGuid = AssetDatabase.AssetPathToGUID(path);
            Assert.That(copiedGuid, Is.Not.Empty);
            Assert.That(copiedGuid, Is.Not.EqualTo(seedGuid));
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab), Is.Zero);
            Assert.That(prefab.transform.childCount, Is.EqualTo(2));
            Assert.That(prefab.transform.GetChild(0).name, Is.EqualTo("Same/Name"));
            Assert.That(prefab.transform.GetChild(1).name, Is.EqualTo("Same/Name"));
            Assert.That(CountMissing(prefab.transform.GetChild(0).gameObject), Is.Zero);
            Assert.That(CountMissing(prefab.transform.GetChild(1).gameObject), Is.EqualTo(1));
            CollectionAssert.AreEqual(selection, Selection.objects);

            return path;
        }

        private static GameObject RequireMissingSeedRoot(Scene scene)
        {
            Assert.That(EditorSceneManager.IsPreviewScene(scene), Is.False);
            GameObject[] roots = scene.GetRootGameObjects();
            Assert.That(roots, Has.Length.EqualTo(1));
            GameObject root = roots[0];
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), Is.Zero);
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(CountMissing(root.transform.GetChild(0).gameObject), Is.EqualTo(1));
            return root;
        }

        private static int CountMissingInSavedScene(string path)
        {
            Scene preview = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                int count = 0;
                foreach (GameObject root in preview.GetRootGameObjects())
                {
                    count += CountMissing(root);
                }

                return count;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static int CountMissing(GameObject root)
        {
            int count = 0;
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            }

            return count;
        }
    }
}
