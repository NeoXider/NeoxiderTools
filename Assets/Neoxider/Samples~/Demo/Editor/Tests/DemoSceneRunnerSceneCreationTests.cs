using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Contract of <see cref="DemoSceneRunner.TryCreateSceneAsset" />, which RebuildAll uses for a builder whose
    ///     .unity does not exist yet: missing folders get their .meta through the AssetDatabase, the scene is an empty
    ///     saved scene at exactly the builder path, and paths outside Assets are refused without touching the scenes.
    /// </summary>
    public sealed class DemoSceneRunnerSceneCreationTests
    {
        private string _folder;

        [TearDown]
        public void TearDown()
        {
            if (string.IsNullOrEmpty(_folder))
            {
                return;
            }

            // WHY: the created scene is open in Single mode; switch to an untitled scene before deleting its folder.
            if (SceneManager.GetActiveScene().path.StartsWith(_folder + "/", StringComparison.Ordinal))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            AssetDatabase.DeleteAsset(_folder);
            _folder = null;
        }

        [Test]
        public void TryCreateSceneAsset_CreatesMissingFoldersWithMetaAndAnEmptyScene()
        {
            _folder = "Assets/__NeoDemoSceneCreation_" + Guid.NewGuid().ToString("N");
            string path = _folder + "/Scenes/New Module/Created.unity";

            bool created = DemoSceneRunner.TryCreateSceneAsset(path, out string error);

            Assert.That(created, Is.True, error);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);
            Assert.That(AssetDatabase.IsValidFolder(_folder + "/Scenes/New Module"), Is.True);
            Assert.That(File.Exists(_folder + ".meta"), Is.True, "Created folders need their .meta files.");
            Assert.That(File.Exists(_folder + "/Scenes.meta"), Is.True);
            Assert.That(File.Exists(_folder + "/Scenes/New Module.meta"), Is.True);
            Assert.That(File.Exists(path + ".meta"), Is.True);
            Scene scene = SceneManager.GetActiveScene();
            Assert.That(scene.path, Is.EqualTo(path));
            Assert.That(scene.rootCount, Is.Zero, "NewSceneSetup.EmptyScene has no camera or light.");
        }

        [Test]
        public void TryCreateSceneAsset_RefusesPathsOutsideAssetsWithoutTouchingTheScenes()
        {
            Scene before = SceneManager.GetActiveScene();

            Assert.That(DemoSceneRunner.TryCreateSceneAsset("Packages/com.neoxider.tools/Created.unity", out string error),
                Is.False);
            Assert.That(error, Does.Contain("Assets/"));
            Assert.That(DemoSceneRunner.TryCreateSceneAsset("Assets/NotAScene.txt", out error), Is.False);
            Assert.That(DemoSceneRunner.TryCreateSceneAsset(null, out error), Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(before));
        }
    }
}
