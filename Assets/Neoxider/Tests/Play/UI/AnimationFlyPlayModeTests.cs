using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Tests.Play
{
    public sealed class AnimationFlyPlayModeTests
    {
        private readonly List<GameObject> _spawnedRoots = new();

        [TearDown]
        public void TearDown()
        {
            AnimationFly.DestroyInstance();

            foreach (GameObject go in _spawnedRoots)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _spawnedRoots.Clear();
        }

        [UnityTest]
        public IEnumerator Play_WorldToCanvasWithNonRootSpawnParent_StartsAtWorldScreenPoint()
        {
            TestRig rig = BuildRig();
            GameObject prefab = CreateUiPrefab("FlyVisualPrefab");
            Transform worldStart = CreateWorldPoint("WorldStart", new Vector3(1.5f, -0.75f, 0f));
            RectTransform canvasEnd = CreateUiPoint("CanvasEnd", rig.Canvas.transform, new Vector2(-160f, 96f));

            RectTransform startedRect = null;
            Vector2 startedScreenPoint = Vector2.zero;
            Vector2 expectedScreenPoint = rig.Camera.WorldToScreenPoint(worldStart.position);

            AnimationFly.AnimationFlyResult result = rig.Fly.Play(new AnimationFly.AnimationFlyRequest
            {
                Prefab = prefab,
                Count = 1,
                StartTransform = worldStart,
                EndTransform = canvasEnd,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                Parent = rig.SpawnParent,
                CompletionMode = AnimationFlyCompletionMode.KeepAlive,
                StartRandomOffset = Vector3.zero,
                EndRandomOffset = Vector3.zero,
                MiddleRandomOffset = Vector3.zero,
                OnItemStarted = item =>
                {
                    startedRect = item.GetComponent<RectTransform>();
                    startedScreenPoint = RectTransformUtility.WorldToScreenPoint(rig.Camera, startedRect.position);
                }
            });

            yield return null;

            Assert.That(startedRect, Is.Not.Null, "AnimationFly should spawn a UI RectTransform visual.");
            Assert.That(startedRect.parent, Is.EqualTo(rig.SpawnParent),
                "The visual should be spawned under the requested non-root UI parent.");
            Assert.That(startedScreenPoint.x, Is.EqualTo(expectedScreenPoint.x).Within(1f));
            Assert.That(startedScreenPoint.y, Is.EqualTo(expectedScreenPoint.y).Within(1f));

            yield return WaitUntilCompleted(result);
        }

        [UnityTest]
        public IEnumerator Play_OnAllArrived_GrantsRewardOnceForMultipleItems()
        {
            TestRig rig = BuildRig();
            GameObject prefab = CreateUiPrefab("FlyVisualPrefab");
            Transform worldStart = CreateWorldPoint("WorldStart", new Vector3(-1f, 0.5f, 0f));
            RectTransform canvasEnd = CreateUiPoint("CanvasEnd", rig.Canvas.transform, new Vector2(128f, -72f));

            int startedCount = 0;
            int arrivedCount = 0;
            int allArrivedCount = 0;
            int rewardCount = 0;

            AnimationFly.AnimationFlyResult result = rig.Fly.Play(new AnimationFly.AnimationFlyRequest
            {
                Prefab = prefab,
                Count = 3,
                StartTransform = worldStart,
                EndTransform = canvasEnd,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                Parent = rig.SpawnParent,
                CompletionMode = AnimationFlyCompletionMode.KeepAlive,
                RewardTiming = AnimationFlyRewardTiming.OnAllArrived,
                StartRandomOffset = Vector3.zero,
                EndRandomOffset = Vector3.zero,
                MiddleRandomOffset = Vector3.zero,
                OnItemStarted = _ => startedCount++,
                OnItemArrived = _ => arrivedCount++,
                OnAllArrived = () => allArrivedCount++,
                OnReward = () => rewardCount++
            });

            yield return WaitUntilCompleted(result);

            Assert.That(startedCount, Is.EqualTo(3));
            Assert.That(arrivedCount, Is.EqualTo(3));
            Assert.That(allArrivedCount, Is.EqualTo(1));
            Assert.That(rewardCount, Is.EqualTo(1),
                "OnAllArrived reward timing should grant once after every visual item arrives.");
        }

        [UnityTest]
        public IEnumerator Play_DisableAndPool_DoesNotCompoundScaleOnReuse()
        {
            TestRig rig = BuildRig();
            GameObject prefab = CreateUiPrefab("PooledFlyVisualPrefab");
            Transform worldStart = CreateWorldPoint("WorldStart", Vector3.zero);
            RectTransform canvasEnd = CreateUiPoint("CanvasEnd", rig.Canvas.transform, new Vector2(20f, 20f));
            var startedScales = new List<Vector3>();

            rig.Fly.scaleMult = 2f;
            rig.Fly.defaultCompletionMode = AnimationFlyCompletionMode.DisableAndPool;

            AnimationFly.AnimationFlyRequest BuildRequest()
            {
                return new AnimationFly.AnimationFlyRequest
                {
                    Prefab = prefab,
                    Count = 1,
                    StartTransform = worldStart,
                    EndTransform = canvasEnd,
                    StartSpace = AnimationFlyCoordinateSpace.World,
                    EndSpace = AnimationFlyCoordinateSpace.Canvas,
                    SpawnSpace = AnimationFlySpawnSpace.Canvas,
                    Parent = rig.SpawnParent,
                    CompletionMode = AnimationFlyCompletionMode.DisableAndPool,
                    StartRandomOffset = Vector3.zero,
                    EndRandomOffset = Vector3.zero,
                    MiddleRandomOffset = Vector3.zero,
                    OnItemStarted = item => startedScales.Add(item.transform.localScale)
                };
            }

            yield return WaitUntilCompleted(rig.Fly.Play(BuildRequest()));
            yield return WaitUntilCompleted(rig.Fly.Play(BuildRequest()));

            Assert.That(startedScales, Has.Count.EqualTo(2));
            Assert.That(startedScales[0].x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(startedScales[1].x, Is.EqualTo(2f).Within(0.001f),
                "Pooled AnimationFly visuals should reset to their base scale before applying scaleMult again.");
        }

        private TestRig BuildRig()
        {
            Camera camera = Track(new GameObject("AnimationFlyTestCamera")).AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.tag = "MainCamera";

            Canvas canvas = Track(new GameObject("AnimationFlyTestCanvas")).AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800f, 600f);

            RectTransform spawnParent = new GameObject("NestedSpawnParent").AddComponent<RectTransform>();
            spawnParent.SetParent(canvas.transform, false);
            spawnParent.anchorMin = new Vector2(0.5f, 0.5f);
            spawnParent.anchorMax = new Vector2(0.5f, 0.5f);
            spawnParent.pivot = new Vector2(0.5f, 0.5f);
            spawnParent.sizeDelta = new Vector2(420f, 260f);
            spawnParent.anchoredPosition = new Vector2(123f, -57f);

            AnimationFly fly = Track(new GameObject("AnimationFly")).AddComponent<AnimationFly>();
            fly.parentCanvas = canvas;
            fly.animationCamera = camera;
            fly.spawnParent = spawnParent;
            fly.spawnSpace = AnimationFlySpawnSpace.Canvas;
            fly.useAnchoredPositionForUI = true;
            fly.defaultCompletionMode = AnimationFlyCompletionMode.KeepAlive;
            fly.flyDuration = 0.02f;
            fly.delayBetweenBonuses = 0f;
            fly.arcStrength = 0f;
            fly.middlePoint = 0.5f;
            fly.startRandomOffset = Vector3.zero;
            fly.endRandomOffset = Vector3.zero;
            fly.middleRandomOffset = Vector3.zero;
            fly.rotateDuringFlight = false;
            fly.scaleMult = 1f;

            Canvas.ForceUpdateCanvases();
            return new TestRig(camera, canvas, spawnParent, fly);
        }

        private GameObject CreateUiPrefab(string name)
        {
            GameObject prefab = Track(new GameObject(name));
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(24f, 24f);
            Image image = prefab.AddComponent<Image>();
            image.raycastTarget = false;
            return prefab;
        }

        private Transform CreateWorldPoint(string name, Vector3 position)
        {
            Transform transform = Track(new GameObject(name)).transform;
            transform.position = position;
            return transform;
        }

        private RectTransform CreateUiPoint(string name, Transform parent, Vector2 anchoredPosition)
        {
            RectTransform rect = new GameObject(name).AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(16f, 16f);
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        private GameObject Track(GameObject go)
        {
            _spawnedRoots.Add(go);
            return go;
        }

        private static IEnumerator WaitUntilCompleted(AnimationFly.AnimationFlyResult result)
        {
            for (int i = 0; i < 120 && !result.IsCompleted; i++)
            {
                yield return null;
            }

            Assert.That(result.IsCompleted, Is.True, "AnimationFly request did not complete within the test timeout.");
        }

        private readonly struct TestRig
        {
            public TestRig(Camera camera, Canvas canvas, RectTransform spawnParent, AnimationFly fly)
            {
                Camera = camera;
                Canvas = canvas;
                SpawnParent = spawnParent;
                Fly = fly;
            }

            public Camera Camera { get; }
            public Canvas Canvas { get; }
            public RectTransform SpawnParent { get; }
            public AnimationFly Fly { get; }
        }
    }
}
