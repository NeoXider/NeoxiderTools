using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Neo.Level.Tests
{
    public class SceneFlowControllerTests
    {
        private GameObject _gameObject;
        private SceneFlowController _controller;
        private RecordingSceneLoader _sceneLoader;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(SceneFlowController));
            _controller = _gameObject.AddComponent<SceneFlowController>();
            _sceneLoader = new RecordingSceneLoader();
            SceneFlowController.SceneLoader = _sceneLoader;
        }

        [TearDown]
        public void TearDown()
        {
            SceneFlowController.SceneLoader = null;

            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [Test]
        public void LoadSceneWithoutArguments_UsesConfiguredSceneName_WhenEnabled()
        {
            _controller.LoadMode = SceneFlowLoadMode.Sync;
            _controller.SceneName = "MainMenu";
            SetPrivateField(_controller, "_useSceneName", true);

            _controller.LoadScene();

            Assert.That(_sceneLoader.LoadCalls, Has.Count.EqualTo(1));
            Assert.That(_sceneLoader.LoadCalls[0].SceneName, Is.EqualTo("MainMenu"));
            Assert.That(_sceneLoader.LoadCalls[0].BuildIndex, Is.Null);
            Assert.That(_sceneLoader.LoadCalls[0].Mode, Is.EqualTo(LoadSceneMode.Single));
        }

        [Test]
        public void LoadSceneWithoutArguments_UsesConfiguredBuildIndex_WhenSceneNameIsDisabled()
        {
            _controller.LoadMode = SceneFlowLoadMode.Sync;
            _controller.SceneBuildIndex = 4;
            _controller.SceneName = "Ignored";
            SetPrivateField(_controller, "_useSceneName", false);

            _controller.LoadScene();

            Assert.That(_sceneLoader.LoadCalls, Has.Count.EqualTo(1));
            Assert.That(_sceneLoader.LoadCalls[0].BuildIndex, Is.EqualTo(4));
            Assert.That(_sceneLoader.LoadCalls[0].SceneName, Is.Null);
            Assert.That(_sceneLoader.LoadCalls[0].Mode, Is.EqualTo(LoadSceneMode.Single));
        }

        [Test]
        public void LoadScene_UsesAdditiveMode_ForAdditiveTransitions()
        {
            _controller.LoadMode = SceneFlowLoadMode.Additive;

            _controller.LoadScene(2);

            Assert.That(_sceneLoader.LoadCalls, Has.Count.EqualTo(1));
            Assert.That(_sceneLoader.LoadCalls[0].BuildIndex, Is.EqualTo(2));
            Assert.That(_sceneLoader.LoadCalls[0].Mode, Is.EqualTo(LoadSceneMode.Additive));
        }

        [Test]
        public void LoadScene_ByName_UsesAdditiveMode_ForAdditiveTransitions()
        {
            _controller.LoadMode = SceneFlowLoadMode.Additive;

            _controller.LoadScene("Overlay");

            Assert.That(_sceneLoader.LoadCalls, Has.Count.EqualTo(1));
            Assert.That(_sceneLoader.LoadCalls[0].SceneName, Is.EqualTo("Overlay"));
            Assert.That(_sceneLoader.LoadCalls[0].Mode, Is.EqualTo(LoadSceneMode.Additive));
        }

        [Test]
        public void Restart_LoadsActiveSceneBuildIndex()
        {
            _controller.LoadMode = SceneFlowLoadMode.Sync;
            _sceneLoader.ActiveSceneBuildIndex = 7;

            _controller.Restart();

            Assert.That(_sceneLoader.LoadCalls, Has.Count.EqualTo(1));
            Assert.That(_sceneLoader.LoadCalls[0].BuildIndex, Is.EqualTo(7));
            Assert.That(_sceneLoader.LoadCalls[0].Mode, Is.EqualTo(LoadSceneMode.Single));
        }

        [Test]
        public void LoadScene_ByEmptyName_DoesNotRequestSceneLoad()
        {
            _controller.LoadMode = SceneFlowLoadMode.Sync;

            _controller.LoadScene("");

            Assert.That(_sceneLoader.LoadCalls, Is.Empty);
        }

        [Test]
        public void LoadScene_ByNegativeIndex_DoesNotRequestSceneLoad()
        {
            _controller.LoadMode = SceneFlowLoadMode.Sync;

            _controller.LoadScene(-1);

            Assert.That(_sceneLoader.LoadCalls, Is.Empty);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field `{fieldName}` was not found.");
            fieldInfo.SetValue(target, value);
        }

        private readonly struct SceneLoadCall
        {
            public SceneLoadCall(int? buildIndex, string sceneName, LoadSceneMode mode)
            {
                BuildIndex = buildIndex;
                SceneName = sceneName;
                Mode = mode;
            }

            public int? BuildIndex { get; }
            public string SceneName { get; }
            public LoadSceneMode Mode { get; }
        }

        private sealed class RecordingSceneLoader : ISceneFlowSceneLoader
        {
            public int ActiveSceneBuildIndex { get; set; }
            public List<SceneLoadCall> LoadCalls { get; } = new();

            public void LoadScene(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
            {
                LoadCalls.Add(new SceneLoadCall(buildIndex, null, mode));
            }

            public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
            {
                LoadCalls.Add(new SceneLoadCall(null, sceneName, mode));
            }

            public AsyncOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
            {
                return null;
            }

            public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
            {
                return null;
            }
        }
    }
}
