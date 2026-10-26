using System;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsComponentsSceneTests
    {
        private GameObject _root;
        private GameObject _viewRoot;
        private readonly List<RenderTexture> _renderTextures = new List<RenderTexture>();
        private string _lootCloneName;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(Audit20261023NeoToolsComponentsSceneTests));
            _root.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            TextScore view = _viewRoot != null ? _viewRoot.GetComponent<TextScore>() : null;
            if (view != null)
            {
                Audit20261023NeoToolsComponentsAccess.Invoke(view, "OnDisable");
            }

            if (_viewRoot != null)
            {
                Object.DestroyImmediate(_viewRoot);
                _viewRoot = null;
            }

            Camera camera = _root.GetComponent<Camera>();
            if (camera != null)
            {
                camera.targetTexture = null;
            }

            Object.DestroyImmediate(_root);
            foreach (RenderTexture renderTexture in _renderTextures)
            {
                Object.DestroyImmediate(renderTexture);
            }

            _renderTextures.Clear();
            if (_lootCloneName != null)
            {
                foreach (GameObject clone in GetLootClones())
                {
                    Object.DestroyImmediate(clone);
                }
            }

            _lootCloneName = null;
        }

        [Test]
        public void Camera_FitBothToPortraitTexture_PreservesWidthWithoutViewportBars()
        {
            CameraAspectRatioScaler scaler = CreateCamera(out Camera camera);
            camera.targetTexture = CreateRenderTexture(100, 200);

            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "ApplyCameraScale", true);

            Assert.That(camera.orthographicSize, Is.EqualTo(8f * (16f / 9f) / 0.5f).Within(0.0001f));
            Assert.That(camera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void Camera_TargetTextureDimensionsChange_RefreshesCachedProjection()
        {
            CameraAspectRatioScaler scaler = CreateCamera(out Camera camera);
            camera.targetTexture = CreateRenderTexture(160, 90);
            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "ApplyCameraScale", true);
            Assert.That(camera.orthographicSize, Is.EqualTo(8f));
            camera.targetTexture = CreateRenderTexture(100, 200);

            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "ApplyCameraScale", false);

            Assert.That(camera.orthographicSize, Is.EqualTo(8f * (16f / 9f) / 0.5f).Within(0.0001f));
        }

        [Test]
        public void Camera_ProjectionModeChanges_RefreshesCachedFieldOfView()
        {
            CameraAspectRatioScaler scaler = CreateCamera(out Camera camera);
            camera.targetTexture = CreateRenderTexture(160, 90);
            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "ApplyCameraScale", true);
            camera.orthographic = false;
            camera.fieldOfView = 40f;

            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "ApplyCameraScale", false);

            Assert.That(camera.fieldOfView, Is.EqualTo(60f).Within(0.0001f));
        }

        [Test]
        public void Camera_StartWithEditorPreviewDisabled_LeavesCameraUntouched()
        {
            CameraAspectRatioScaler scaler = CreateCamera(out Camera camera);
            Audit20261023NeoToolsComponentsAccess.Configure(scaler, "updateInEditor", false);
            camera.orthographicSize = 9f;
            Rect authoredRect = new Rect(0.1f, 0.2f, 0.3f, 0.4f);
            camera.rect = authoredRect;

            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "Start");

            Assert.That(camera.orthographicSize, Is.EqualTo(9f));
            Assert.That(camera.rect, Is.EqualTo(authoredRect));
        }

        [Test]
        public void Camera_DisableWithEditorPreviewDisabled_PreservesAuthoredViewport()
        {
            CameraAspectRatioScaler scaler = CreateCamera(out Camera camera);
            Audit20261023NeoToolsComponentsAccess.Configure(scaler, "updateInEditor", false);
            Rect authoredRect = new Rect(0.1f, 0.2f, 0.3f, 0.4f);
            camera.rect = authoredRect;

            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "OnDisable");

            Assert.That(camera.rect, Is.EqualTo(authoredRect));
        }

        [Test]
        public void Loot_DestroyInEditMode_DoesNotSpawnObjects()
        {
            GameObject prefab = new GameObject("AuditLoot." + Guid.NewGuid().ToString("N"));
            prefab.transform.SetParent(_root.transform);
            _lootCloneName = prefab.name + "(Clone)";
            Loot loot = _root.AddComponent<Loot>();
            loot.lootItems = new[] { prefab };
            loot.minCount = 1;
            loot.maxCount = 1;
            loot.dropChance = 1f;

            Audit20261023NeoToolsComponentsAccess.Invoke(loot, "OnDestroy");

            Assert.That(GetLootClones(), Is.Empty);
        }

        [Test]
        public void TextScore_ChangingDisplayModeBeforeDisable_DisposesOriginalSubscription()
        {
            TextScore view = CreateTextScore(out ScoreManager first, out ScoreManager second);
            Audit20261023NeoToolsComponentsAccess.Invoke(view, "Start");
            Assert.That(first.Score.ListenerCount, Is.EqualTo(1));
            Audit20261023NeoToolsComponentsAccess.Configure(view, "_best", true);

            Audit20261023NeoToolsComponentsAccess.Invoke(view, "OnDisable");

            Assert.That(first.Score.ListenerCount, Is.Zero);
            Assert.That(first.BestScore.ListenerCount, Is.Zero);
        }

        [Test]
        public void TextScore_ReenableWithNewExplicitSource_BindsOnlyTheNewSource()
        {
            TextScore view = CreateTextScore(out ScoreManager first, out ScoreManager second);
            Audit20261023NeoToolsComponentsAccess.Invoke(view, "Start");
            Audit20261023NeoToolsComponentsAccess.Invoke(view, "OnDisable");
            Audit20261023NeoToolsComponentsAccess.Configure(view, "_scoreSource", second);

            Audit20261023NeoToolsComponentsAccess.Invoke(view, "OnEnable");

            Assert.That(first.Score.ListenerCount, Is.Zero);
            Assert.That(second.Score.ListenerCount, Is.EqualTo(1));
            Audit20261023NeoToolsComponentsAccess.Invoke(view, "OnDisable");
            Assert.That(second.Score.ListenerCount, Is.Zero);
        }

        private CameraAspectRatioScaler CreateCamera(out Camera camera)
        {
            camera = _root.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.fieldOfView = 60f;
            CameraAspectRatioScaler scaler = _root.AddComponent<CameraAspectRatioScaler>();
            Audit20261023NeoToolsComponentsAccess.Configure(scaler, "minSize", 1f);
            Audit20261023NeoToolsComponentsAccess.Configure(scaler, "maxSize", 179f);
            Audit20261023NeoToolsComponentsAccess.Invoke(scaler, "Awake");
            return scaler;
        }

        private RenderTexture CreateRenderTexture(int width, int height)
        {
            RenderTexture texture = new RenderTexture(width, height, 0);
            _renderTextures.Add(texture);
            return texture;
        }

        private TextScore CreateTextScore(out ScoreManager first, out ScoreManager second)
        {
            GameObject firstObject = new GameObject("FirstScore");
            firstObject.transform.SetParent(_root.transform);
            first = firstObject.AddComponent<ScoreManager>();
            GameObject secondObject = new GameObject("SecondScore");
            secondObject.transform.SetParent(_root.transform);
            second = secondObject.AddComponent<ScoreManager>();
            _viewRoot = new GameObject("ScoreView");
            TextScore view = _viewRoot.AddComponent<TextScore>();
            Audit20261023NeoToolsComponentsAccess.Configure(view, "_scoreSource", first);
            return view;
        }

        private List<GameObject> GetLootClones()
        {
            List<GameObject> clones = new List<GameObject>();
            foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.name == _lootCloneName)
                {
                    clones.Add(candidate);
                }
            }

            return clones;
        }
    }
}
