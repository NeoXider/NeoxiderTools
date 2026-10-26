using System.Collections;
using System.Collections.Generic;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoUIAnimationFlyTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private Texture2D _texture;
        private Sprite _sprite;

        [TearDown]
        public void TearDown()
        {
            // Destroy the owner first; its visuals may live outside its hierarchy.
            foreach (GameObject item in _objects)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }

            _objects.Clear();
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
        public IEnumerator PoolReuse_WithNullParent_DetachesFromPreviousContainer()
        {
            AnimationFly fly = CreateFly();
            GameObject prefab = Track(new GameObject("World visual template"));
            Transform container = Track(new GameObject("Previous container")).transform;
            GameObject first = null;
            AnimationFly.AnimationFlyRequest request = WorldRequest(prefab, container);
            request.OnItemStarted = item => first = item;
            yield return WaitForCompletion(fly.Play(request));
            Assert.That(first.activeSelf, Is.False);

            GameObject reused = null;
            request = WorldRequest(prefab, null);
            request.OnItemStarted = item => reused = item;
            AnimationFly.AnimationFlyResult second = fly.Play(request);

            Assert.That(reused, Is.SameAs(first));
            Assert.That(reused.transform.parent, Is.Null);
            Assert.That(container.childCount, Is.Zero);
            yield return WaitForCompletion(second);
        }

        [UnityTest]
        public IEnumerator SpritePool_KeepsCanvasAndWorldRepresentationsSeparate()
        {
            AnimationFly fly = CreateFly();
            GameObject canvasObject = Track(new GameObject("Fly canvas", typeof(RectTransform), typeof(Canvas)));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _texture = new Texture2D(8, 8);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 8f, 8f), Vector2.one * 0.5f);
            GameObject canvasVisual = null;
            AnimationFly.AnimationFlyRequest canvasRequest = new AnimationFly.AnimationFlyRequest
            {
                Sprite = _sprite,
                Parent = canvas.transform,
                StartSpace = AnimationFlyCoordinateSpace.Canvas,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                Duration = 0.02f,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool,
                OnItemStarted = item => canvasVisual = item
            };
            yield return WaitForCompletion(fly.Play(canvasRequest));
            Assert.That(canvasVisual.GetComponent<Image>().sprite, Is.SameAs(_sprite));

            GameObject worldVisual = null;
            AnimationFly.AnimationFlyRequest worldRequest = new AnimationFly.AnimationFlyRequest
            {
                Sprite = _sprite,
                SpawnSpace = AnimationFlySpawnSpace.World,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.World,
                Duration = 0.02f,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool,
                OnItemStarted = item => worldVisual = item
            };
            AnimationFly.AnimationFlyResult worldResult = fly.Play(worldRequest);

            Assert.That(worldVisual, Is.Not.SameAs(canvasVisual));
            SpriteRenderer renderer = worldVisual.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.SameAs(_sprite));
            Assert.That(worldVisual.transform.parent, Is.Null);
            Assert.That(canvasVisual.activeSelf, Is.False);
            yield return WaitForCompletion(worldResult);
        }

        [UnityTest]
        public IEnumerator DestroyOwner_ReleasesPooledAndActiveVisualsButKeepsCompletedKeepAlive()
        {
            AnimationFly fly = CreateFly();
            GameObject prefab = Track(new GameObject("Ownership visual template"));
            Transform externalParent = Track(new GameObject("External fly parent")).transform;
            GameObject pooled = null;
            AnimationFly.AnimationFlyRequest pooledRequest = WorldRequest(prefab, externalParent);
            pooledRequest.OnItemStarted = item => pooled = item;
            yield return WaitForCompletion(fly.Play(pooledRequest));

            GameObject retained = null;
            AnimationFly.AnimationFlyRequest retainedRequest = WorldRequest(prefab, externalParent);
            retainedRequest.CompletionMode = AnimationFlyCompletionMode.KeepAlive;
            retainedRequest.OnItemStarted = item => retained = Track(item);
            yield return WaitForCompletion(fly.Play(retainedRequest));
            // This call reused the pooled visual and transferred it to the caller.
            Assert.That(retained, Is.SameAs(pooled));
            pooledRequest.OnItemStarted = item => pooled = item;
            yield return WaitForCompletion(fly.Play(pooledRequest));

            GameObject active = null;
            int rewards = 0;
            AnimationFly.AnimationFlyRequest pending = WorldRequest(prefab, externalParent);
            pending.Count = 2;
            pending.Duration = 10f;
            pending.DelayBetweenItems = 100f;
            pending.OnItemStarted = item => active = item;
            pending.OnReward = () => rewards++;
            AnimationFly.AnimationFlyResult incomplete = fly.Play(pending);
            // Keep a separate pooled item while the first pending item is in flight.
            yield return WaitForCompletion(fly.Play(pooledRequest));

            Object.Destroy(fly.gameObject);
            yield return null;
            yield return null;

            Assert.That(active == null, Is.True);
            Assert.That(pooled == null, Is.True);
            Assert.That(retained != null && retained.activeSelf, Is.True);
            Assert.That(externalParent.childCount, Is.EqualTo(1));
            Assert.That(incomplete.StartedCount, Is.EqualTo(1));
            Assert.That(incomplete.IsCompleted, Is.False);
            Assert.That(rewards, Is.Zero);
        }

        private AnimationFly CreateFly()
        {
            AnimationFly fly = Track(new GameObject("Audit AnimationFly")).AddComponent<AnimationFly>();
            fly.arcStrength = 0f;
            fly.useUnscaledTime = true;
            fly.delayBetweenBonuses = 0f;
            return fly;
        }

        [UnityTest]
        public IEnumerator DestroyOwner_ReleasesLegacyInFlightVisualInExternalParent()
        {
            AnimationFly fly = CreateFly();
            fly.flyDuration = 10f;
            fly.spawnSpace = AnimationFlySpawnSpace.World;
            GameObject prefab = Track(new GameObject("Legacy visual template"));
            Transform parent = Track(new GameObject("Legacy external parent")).transform;
            GameObject active = null;
            fly.Execute(prefab, 1, Vector3.zero, Vector3.one, parent, item => active = item);
            Assert.That(parent.childCount, Is.EqualTo(1));

            Object.Destroy(fly.gameObject);
            yield return null;
            yield return null;

            Assert.That(active == null, Is.True);
            Assert.That(parent.childCount, Is.Zero);
        }

        private static AnimationFly.AnimationFlyRequest WorldRequest(GameObject prefab, Transform parent)
        {
            return new AnimationFly.AnimationFlyRequest
            {
                Prefab = prefab,
                Parent = parent,
                StartPosition = Vector3.zero,
                EndPosition = Vector3.one,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.World,
                SpawnSpace = AnimationFlySpawnSpace.World,
                Duration = 0.02f,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool
            };
        }

        private static IEnumerator WaitForCompletion(AnimationFly.AnimationFlyResult result)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!result.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(result.CompletedCount, Is.EqualTo(result.TotalCount));
            Assert.That(result.IsCompleted, Is.True);
        }

        private GameObject Track(GameObject item)
        {
            _objects.Add(item);
            return item;
        }
    }
}
