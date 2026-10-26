using System;
using System.Collections.Generic;
using System.IO;
using Neo.Editor.Windows;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorSceneSaverTests
    {
        private readonly List<Scene> _ownedScenes = new();
        private readonly List<string> _ownedBackupFolders = new();
        private Scene _originalActive;
        private string _fixtureFolder;
        private bool _hadScenesFolder;
        private bool _hadBackupFolder;
        private readonly List<(Scene Scene, string Path, bool Loaded, bool Dirty, GameObject[] Roots)> _borrowedScenes = new();
        private int _previousPreviewCount;

        [SetUp]
        public void SetUp()
        {
            _originalActive = SceneManager.GetActiveScene();
            _previousPreviewCount = EditorSceneManager.previewSceneCount;
            _borrowedScenes.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                _borrowedScenes.Add((scene, scene.path, scene.isLoaded, scene.isDirty,
                    scene.isLoaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>()));
            }

            _hadScenesFolder = Directory.Exists("Assets/Scenes");
            _hadBackupFolder = Directory.Exists("Assets/Scenes/AutoSaves");
            try
            {
                string name = "Audit20261023NeoEditorBackups_" + Guid.NewGuid().ToString("N");
                AssetDatabase.CreateFolder("Assets", name);
                _fixtureFolder = "Assets/" + name;
                AssetDatabase.CreateFolder(_fixtureFolder, "A");
                AssetDatabase.CreateFolder(_fixtureFolder, "B");
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
                foreach (Scene scene in _ownedScenes)
                {
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }

                foreach (string folder in _ownedBackupFolders)
                {
                    AssetDatabase.DeleteAsset(folder);
                }

                if (!string.IsNullOrEmpty(_fixtureFolder))
                {
                    AssetDatabase.DeleteAsset(_fixtureFolder);
                    _fixtureFolder = null;
                }
                DeleteCreatedEmptyFolder("Assets/Scenes/AutoSaves", _hadBackupFolder);
                DeleteCreatedEmptyFolder("Assets/Scenes", _hadScenesFolder);
            }
            finally
            {
                _ownedScenes.Clear();
                _ownedBackupFolders.Clear();
                if (_originalActive.IsValid() && _originalActive.isLoaded)
                {
                    SceneManager.SetActiveScene(_originalActive);
                }

                Assert.That(SceneManager.sceneCount, Is.EqualTo(_borrowedScenes.Count));
                Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(_previousPreviewCount));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_originalActive));
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
        public void SameBasenameScenes_KeepDistinctContentsAndDirtySourcesAcrossRepeatedBackups()
        {
            Scene first = CreateSource("A", "Original_A", out GameObject firstRoot);
            Scene second = CreateSource("B", "Original_B", out GameObject secondRoot);
            string firstPath = first.path;
            string secondPath = second.path;
            byte[] firstSource = File.ReadAllBytes(firstPath);
            byte[] secondSource = File.ReadAllBytes(secondPath);
            firstRoot.name = "Unsaved_A";
            secondRoot.name = "Unsaved_B";
            EditorSceneManager.MarkSceneDirty(first);
            EditorSceneManager.MarkSceneDirty(second);
            Scene activeScene = SceneManager.GetActiveScene();

            _ownedBackupFolders.Add(Path.GetDirectoryName(SceneSaverGUI.GetBackupPath(firstPath)).Replace('\\', '/'));
            _ownedBackupFolders.Add(Path.GetDirectoryName(SceneSaverGUI.GetBackupPath(secondPath)).Replace('\\', '/'));
            Assert.That(SceneSaverGUI.SaveSceneBackup(first, out string firstBackup), Is.True);
            Assert.That(SceneSaverGUI.SaveSceneBackup(second, out string secondBackup), Is.True);
            Assert.That(firstBackup, Is.Not.EqualTo(secondBackup));
            Assert.That(firstBackup, Does.Contain(AssetDatabase.AssetPathToGUID(firstPath)));
            Assert.That(secondBackup, Does.Contain(AssetDatabase.AssetPathToGUID(secondPath)));
            AssertBackupRoot(firstBackup, "Unsaved_A");
            AssertBackupRoot(secondBackup, "Unsaved_B");
            byte[] secondBackupBytes = File.ReadAllBytes(secondBackup);

            firstRoot.name = "Newer_A";
            EditorSceneManager.MarkSceneDirty(first);
            Assert.That(SceneSaverGUI.SaveSceneBackup(first, out string repeatedBackup), Is.True);
            Assert.That(repeatedBackup, Is.EqualTo(firstBackup));
            AssertBackupRoot(firstBackup, "Newer_A");
            Assert.That(File.ReadAllBytes(secondBackup), Is.EqualTo(secondBackupBytes));
            Assert.That(first.path, Is.EqualTo(firstPath));
            Assert.That(second.path, Is.EqualTo(secondPath));
            Assert.That(first.isDirty, Is.True);
            Assert.That(second.isDirty, Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeScene));
            Assert.That(File.ReadAllBytes(firstPath), Is.EqualTo(firstSource));
            Assert.That(File.ReadAllBytes(secondPath), Is.EqualTo(secondSource));
        }

        private Scene CreateSource(string directory, string rootName, out GameObject root)
        {
            string path = _fixtureFolder + "/" + directory + "/Level.unity";
            Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(path);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            _ownedScenes.Add(scene);
            Assert.That(EditorSceneManager.IsPreviewScene(scene), Is.False);
            Assert.That(scene.path, Is.EqualTo(path));
            Assert.That(scene.GetRootGameObjects(), Is.Empty);
            Assert.That(SceneManager.SetActiveScene(scene), Is.True);
            root = new GameObject(rootName);
            Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            return scene;
        }

        private static void AssertBackupRoot(string path, string expectedName)
        {
            Scene active = SceneManager.GetActiveScene();
            Scene copy = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = copy.GetRootGameObjects();
                Assert.That(roots.Length, Is.EqualTo(1));
                Assert.That(roots[0].name, Is.EqualTo(expectedName));
            }
            finally
            {
                EditorSceneManager.CloseScene(copy, true);
                if (active.IsValid() && active.isLoaded)
                {
                    SceneManager.SetActiveScene(active);
                }
            }
        }

        private static void DeleteCreatedEmptyFolder(string path, bool existed)
        {
            if (!existed && Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
