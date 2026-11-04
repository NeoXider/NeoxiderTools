using System;
using System.Reflection;
using Neo.Parallax;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Per-frame cost of <see cref="ParallaxLayer" />: a resting camera writes no transform and skips the tile
    ///     pass, a warm frame allocates nothing, and a replaced camera is followed rather than a cached one.
    /// </summary>
    public sealed class ParallaxLayerFrameCostTests
    {
        private const int Iterations = 200;

        private GameObject _layerObject;
        private GameObject _cameraObject;
        private GameObject _secondCameraObject;
        private Texture2D _texture;
        private Sprite _sprite;

        [TearDown]
        public void TearDown()
        {
            DestroyIfAlive(_layerObject);
            DestroyIfAlive(_cameraObject);
            DestroyIfAlive(_secondCameraObject);
            DestroyIfAlive(_sprite);
            DestroyIfAlive(_texture);
        }

        [Test]
        public void LateUpdate_RestingCamera_DoesNotRewriteTheLayerTransform()
        {
            ParallaxLayer layer = CreateLayer(CreateCamera(ref _cameraObject, "Parallax camera"));
            Action lateUpdate = LateUpdateOf(layer);
            lateUpdate();
            _layerObject.transform.hasChanged = false;

            lateUpdate();
            lateUpdate();

            Assert.That(_layerObject.transform.hasChanged, Is.False,
                "With the camera at rest the layer must not be marked changed (and its tiles with it).");
        }

        [Test]
        public void LateUpdate_WarmFrames_DoNotAllocate()
        {
            Camera camera = CreateCamera(ref _cameraObject, "Parallax camera");
            ParallaxLayer layer = CreateLayer(camera);
            Action lateUpdate = LateUpdateOf(layer);
            lateUpdate();
            lateUpdate();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                camera.transform.position = new Vector3(i * 0.01f, 0f, -10f);
                lateUpdate();
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
        }

        [Test]
        public void LateUpdate_RestingCameraAndLayer_SkipTheTilePassUntilTheCameraMoves()
        {
            Camera camera = CreateCamera(ref _cameraObject, "Parallax camera");
            ParallaxLayer layer = CreateLayer(camera);
            Action lateUpdate = LateUpdateOf(layer);
            lateUpdate();
            FieldInfo gridWidthField = typeof(ParallaxLayer).GetField("gridWidthWorld",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(gridWidthField, Is.Not.Null);
            float gridWidth = (float)gridWidthField.GetValue(layer);
            Assert.That(gridWidth, Is.GreaterThan(0f), "positive control: the layer tiles horizontally");
            Transform probe = TileNearestToCameraX(camera.transform.position.x);
            Vector3 settled = probe.position;
            // WHY: a tile moved by hand is the observable stand-in for the skipped pass; the pass would shift it back.
            Vector3 displaced = settled + new Vector3(gridWidth * 3f, 0f, 0f);
            probe.position = displaced;

            lateUpdate();
            lateUpdate();

            Assert.That(probe.position, Is.EqualTo(displaced),
                "with the camera and the layer at rest the tile pass is skipped, so the tile is not shifted");

            // WHY a vertical move: the default multiplier keeps the layer still on Y, so only the camera changes.
            camera.transform.position += new Vector3(0f, 0.25f, 0f);
            lateUpdate();

            Assert.That(_layerObject.transform.position.y, Is.Zero, "positive control: the layer did not move");
            Assert.That(probe.position.x, Is.EqualTo(settled.x).Within(1e-3f),
                "a moved camera runs the tile pass again and recycles the displaced tile into the window");
        }

        [Test]
        public void LateUpdate_FollowsAReplacedCameraInsteadOfTheCachedOne()
        {
            Camera first = CreateCamera(ref _cameraObject, "First camera");
            ParallaxLayer layer = CreateLayer(first);
            Action lateUpdate = LateUpdateOf(layer);
            lateUpdate();

            Camera second = CreateCamera(ref _secondCameraObject, "Second camera");
            layer.SetTargetCamera(second);
            Vector3 start = _layerObject.transform.position;
            second.transform.position += new Vector3(2f, 0f, 0f);
            first.transform.position += new Vector3(-50f, 0f, 0f);
            lateUpdate();

            // WHY -1: the default X multiplier is 0.5, so the layer moves half the camera delta, opposite to it.
            Assert.That(_layerObject.transform.position.x - start.x, Is.EqualTo(-1f).Within(1e-4f));
        }

        private ParallaxLayer CreateLayer(Camera camera)
        {
            _layerObject = new GameObject("ParallaxFrameCostLayer");
            SpriteRenderer renderer = _layerObject.AddComponent<SpriteRenderer>();
            _texture = new Texture2D(8, 8);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            renderer.sprite = _sprite;
            ParallaxLayer layer = _layerObject.AddComponent<ParallaxLayer>();
            SetPrivateField(layer, "templateRenderer", renderer);
            layer.SetTargetCamera(camera);
            InvokePrivate(layer, "Initialise");
            Assert.That(_layerObject.transform.childCount, Is.GreaterThanOrEqualTo(3), "The tile pool must exist.");
            return layer;
        }

        private Transform TileNearestToCameraX(float cameraX)
        {
            Transform layerTransform = _layerObject.transform;
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < layerTransform.childCount; i++)
            {
                Transform child = layerTransform.GetChild(i);
                float distance = Mathf.Abs(child.position.x - cameraX);
                if (distance < nearestDistance)
                {
                    nearest = child;
                    nearestDistance = distance;
                }
            }

            Assert.That(nearest, Is.Not.Null, "The tile pool must exist.");
            return nearest;
        }

        private static Camera CreateCamera(ref GameObject holder, string name)
        {
            holder = new GameObject(name);
            holder.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = holder.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            // WHY pinned: otherwise the aspect, and with it the tile count, follows the editor or batch-mode screen.
            camera.aspect = 1f;
            return camera;
        }

        private static Action LateUpdateOf(ParallaxLayer layer)
        {
            MethodInfo method = typeof(ParallaxLayer).GetMethod("LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            // WHY a delegate and not MethodInfo.Invoke: reflection invocation allocates and would mask the
            // allocation-free contract under test.
            return (Action)Delegate.CreateDelegate(typeof(Action), layer, method);
        }

        private static void SetPrivateField(ParallaxLayer layer, string fieldName, object value)
        {
            FieldInfo field = typeof(ParallaxLayer).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(layer, value);
        }

        private static void InvokePrivate(ParallaxLayer layer, string methodName)
        {
            MethodInfo method = typeof(ParallaxLayer).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            method.Invoke(layer, null);
        }

        private static void DestroyIfAlive(Object target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
