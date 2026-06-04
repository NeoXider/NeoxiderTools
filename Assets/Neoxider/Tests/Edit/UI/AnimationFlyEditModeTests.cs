using System.Collections;
using Neo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Tests.Edit
{
    public sealed class AnimationFlyEditModeTests
    {
        private GameObject _root;
        private Texture2D _texture;
        private Sprite _sprite;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }

            if (_sprite != null)
            {
                Object.DestroyImmediate(_sprite);
            }

            if (_texture != null)
            {
                Object.DestroyImmediate(_texture);
            }
        }

        [UnityTest]
        public IEnumerator PlaySprite_CanvasStartUnderOffsetSpawnParent_UsesParentLocalCoordinates()
        {
            AnimationFly fly = CreateFly(out RectTransform canvasRoot);
            RectTransform spawnParent = CreateRect("SpawnParent", canvasRoot, new Vector2(100f, 50f));
            RectTransform start = CreateRect("Start", canvasRoot, new Vector2(220f, 120f));
            RectTransform end = CreateRect("End", canvasRoot, new Vector2(320f, 180f));
            Vector2? capturedStart = null;

            fly.spawnParent = spawnParent;
            fly.Play(new AnimationFly.AnimationFlyRequest
            {
                Sprite = CreateSprite(),
                Count = 1,
                StartTransform = start,
                EndTransform = end,
                StartSpace = AnimationFlyCoordinateSpace.Canvas,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                Parent = spawnParent,
                CompletionMode = AnimationFlyCompletionMode.KeepAlive,
                RewardTiming = AnimationFlyRewardTiming.Manual,
                OnItemStarted = item => capturedStart = ((RectTransform)item.transform).anchoredPosition
            });

            yield return null;

            Assert.That(capturedStart.HasValue, Is.True);
            Assert.That(capturedStart.Value.x, Is.EqualTo(120f).Within(0.01f));
            Assert.That(capturedStart.Value.y, Is.EqualTo(70f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator PlaySpriteRequest_ClampsTotalCountAndReportsFirstStartedSpriteItem()
        {
            AnimationFly fly = CreateFly(out RectTransform canvasRoot);
            fly.countMultiplier = 1.5f;
            fly.maxBonusCount = 10;
            RectTransform start = CreateRect("Start", canvasRoot, Vector2.zero);
            RectTransform end = CreateRect("End", canvasRoot, new Vector2(20f, 20f));
            Sprite sprite = CreateSprite();

            AnimationFly.AnimationFlyResult result = fly.Play(new AnimationFly.AnimationFlyRequest
            {
                Sprite = sprite,
                Count = 2,
                CountMultiplier = 1.5f,
                MaxCount = 4,
                StartTransform = start,
                EndTransform = end,
                StartSpace = AnimationFlyCoordinateSpace.Canvas,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                Parent = canvasRoot,
                CompletionMode = AnimationFlyCompletionMode.KeepAlive,
                RewardTiming = AnimationFlyRewardTiming.Manual
            });

            Assert.That(result, Is.Not.SameAs(AnimationFly.AnimationFlyResult.Empty));
            Assert.That(result.TotalCount, Is.EqualTo(4));

            yield return null;

            Assert.That(result.StartedCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.StartedCount, Is.LessThanOrEqualTo(result.TotalCount));
            Assert.That(result.ActiveItems, Has.Count.EqualTo(result.StartedCount));
            Image image = result.ActiveItems[0].GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.SameAs(sprite));
        }

        [Test]
        public void BonusPrefabData_EffectiveEndSpace_PreservesExplicitAndLegacyWorldFallbacks()
        {
            var explicitCanvas = new AnimationFly.BonusPrefabData
            {
                isWorldSpace = true,
                endSpace = AnimationFlyCoordinateSpace.Canvas
            };
            var legacyWorld = new AnimationFly.BonusPrefabData
            {
                isWorldSpace = true,
                endSpace = AnimationFlyCoordinateSpace.Auto
            };
            var legacyFallback = new AnimationFly.BonusPrefabData
            {
                isWorldSpace = false,
                endSpace = AnimationFlyCoordinateSpace.Auto
            };

            Assert.That(explicitCanvas.EffectiveEndSpace(AnimationFlyCoordinateSpace.Screen),
                Is.EqualTo(AnimationFlyCoordinateSpace.Canvas));
            Assert.That(legacyWorld.EffectiveEndSpace(AnimationFlyCoordinateSpace.Canvas),
                Is.EqualTo(AnimationFlyCoordinateSpace.World));
            Assert.That(legacyFallback.EffectiveEndSpace(AnimationFlyCoordinateSpace.Screen),
                Is.EqualTo(AnimationFlyCoordinateSpace.Screen));
        }

        private AnimationFly CreateFly(out RectTransform canvasRoot)
        {
            _root = new GameObject("AnimationFlyTestRoot");
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(_root.transform);

            canvasRoot = (RectTransform)canvasObject.transform;
            canvasRoot.sizeDelta = new Vector2(800f, 600f);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject flyObject = new("AnimationFly");
            flyObject.transform.SetParent(_root.transform);
            AnimationFly fly = flyObject.AddComponent<AnimationFly>();
            fly.parentCanvas = canvas;
            fly.spawnParent = canvasRoot;
            fly.spawnSpace = AnimationFlySpawnSpace.Canvas;
            fly.useAnchoredPositionForUI = true;
            fly.delayBetweenBonuses = 0f;
            fly.flyDuration = 100f;
            fly.arcStrength = 0f;
            fly.multY = 0f;
            fly.startRandomOffset = Vector3.zero;
            fly.endRandomOffset = Vector3.zero;
            fly.middleRandomOffset = Vector3.zero;
            fly.scaleMult = 1f;
            return fly;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(64f, 64f);
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        private Sprite CreateSprite()
        {
            _texture = new Texture2D(4, 4);
            _texture.SetPixels(new[]
            {
                Color.white, Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.white
            });
            _texture.Apply();
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            return _sprite;
        }
    }
}
