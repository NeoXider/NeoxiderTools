using System.Collections;
using System.Collections.Generic;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023CoroutineUnityTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            PrefabPreviewExtensions.ClearPreviewSpriteCache();
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [TestCase(ScreenEdge.Left, 100f, 270f, false)]
        [TestCase(ScreenEdge.Left, 100f, 270f, true)]
        [TestCase(ScreenEdge.TopRight, 420f, 390f, false)]
        [TestCase(ScreenEdge.TopRight, 420f, 390f, true)]
        [TestCase(ScreenEdge.BottomLeft, 100f, 150f, false)]
        [TestCase(ScreenEdge.BottomLeft, 100f, 150f, true)]
        [TestCase(ScreenEdge.Center, 260f, 270f, false)]
        [TestCase(ScreenEdge.Center, 260f, 270f, true)]
        public void ScreenEdge_UsesCameraPixelRectangleAndNearRelativeDepth(
            ScreenEdge edge, float expectedX, float expectedY, bool orthographic)
        {
            GameObject cameraObject = CreateObject("Audit viewport camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = orthographic;
            camera.pixelRect = new Rect(100f, 150f, 320f, 240f);
            camera.nearClipPlane = 2f;
            camera.transform.SetPositionAndRotation(new Vector3(4f, -3f, 7f), Quaternion.Euler(12f, 27f, 9f));

            Vector3 world = camera.GetWorldPositionAtScreenEdge(edge, new Vector2(7f, -11f), 5f);
            Vector3 screen = camera.WorldToScreenPoint(world);

            Assert.That(screen.x, Is.EqualTo(expectedX + 7f).Within(0.01f));
            Assert.That(screen.y, Is.EqualTo(expectedY - 11f).Within(0.01f));
            Assert.That(Vector3.Dot(world - camera.transform.position, camera.transform.forward),
                Is.EqualTo(7f).Within(0.001f));
        }

        [Test]
        public void PreviewCache_ClearDestroysGeneratedSpriteAndPreservesBorrowedTexture()
        {
            GameObject source = CreateObject("Audit thumbnail source");
            Sprite first = source.GetPreviewSprite();
            Assert.IsNotNull(first, "The editor GameObject mini-thumbnail supplies the fallback preview.");
            Texture2D texture = first.texture;
            Sprite second = source.GetPreviewSprite();
            Assert.IsNotNull(second);
            if (second.texture == texture)
            {
                Assert.AreSame(first, second);
            }

            PrefabPreviewExtensions.ClearPreviewSpriteCache();

            Assert.IsTrue(first == null, "The cache owns the generated native Sprite.");
            Assert.IsTrue(texture != null, "AssetPreview owns the thumbnail texture.");
            Sprite replacement = source.GetPreviewSprite();
            Assert.IsNotNull(replacement);
            Assert.AreNotSame(first, replacement);
        }

        [Test]
        public void PreviewCache_ClearPreservesSpriteRendererAsset()
        {
            Texture2D texture = new(2, 2);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero);
            _objects.Add(texture);
            _objects.Add(sprite);
            SpriteRenderer renderer = CreateObject("Audit borrowed sprite").AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            Assert.AreSame(sprite, renderer.gameObject.GetPreviewSprite());
            PrefabPreviewExtensions.ClearPreviewSpriteCache();

            Assert.IsTrue(sprite != null);
            Assert.AreSame(sprite, renderer.sprite);
            Assert.AreSame(sprite, renderer.gameObject.GetPreviewSprite());
        }

        [Test]
        public void SmoothLookAt_CoincidentTargetCompletesAndPreservesRotation()
        {
            Transform target = CreateObject("Audit coincident look target").transform;
            target.SetPositionAndRotation(new Vector3(3f, 2f, 1f), Quaternion.Euler(17f, 43f, 9f));
            Quaternion original = target.rotation;
            IEnumerator routine = target.SmoothLookAtRoutine(target.position, 2f);

            Assert.IsFalse(routine.MoveNext());
            Assert.That(Quaternion.Angle(original, target.rotation), Is.LessThan(0.001f));
        }

        [TestCase(2f, -1f, 0.5f, 1f, "#FF007FFF")]
        [TestCase(-3f, 0f, 1f, 8f, "#0000FFFF")]
        public void ColorHex_ClampsHdrChannelsAndPreservesByteTruncation(
            float red, float green, float blue, float alpha, string expected)
        {
            string hex = new Color(red, green, blue, alpha).ToHexString();
            Assert.AreEqual(expected, hex);
            Assert.IsTrue(hex.ToColorSafe(out Color parsed));
            Assert.That(parsed.r, Is.InRange(0f, 1f));
            Assert.That(parsed.a, Is.InRange(0f, 1f));
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            _objects.Add(gameObject);
            return gameObject;
        }
    }
}
