using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ParallaxLayerEditModeTests
    {
        private GameObject _cameraObject;
        private GameObject _layerObject;
        private Texture2D _texture;
        private Sprite _sprite;

        [TearDown]
        public void TearDown()
        {
            if (_sprite != null)
            {
                Object.DestroyImmediate(_sprite);
            }

            if (_texture != null)
            {
                Object.DestroyImmediate(_texture);
            }

            if (_layerObject != null)
            {
                Object.DestroyImmediate(_layerObject);
            }

            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
            }
        }

        [Test]
        public void Initialise_WithCameraAndSprite_CreatesTilePool()
        {
            Camera camera = CreateCamera();
            SpriteRenderer renderer = CreateLayerRenderer();
            ParallaxLayer layer = _layerObject.AddComponent<ParallaxLayer>();

            SetPrivateField(layer, "targetCamera", camera);
            SetPrivateField(layer, "templateRenderer", renderer);

            InvokePrivate(layer, "Initialise");

            Assert.GreaterOrEqual(_layerObject.transform.childCount, 3);
            Assert.IsFalse(renderer.enabled);
        }

        private Camera CreateCamera()
        {
            _cameraObject = new GameObject("ParallaxCamera");
            Camera camera = _cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            return camera;
        }

        private SpriteRenderer CreateLayerRenderer()
        {
            _layerObject = new GameObject("ParallaxLayer");
            SpriteRenderer renderer = _layerObject.AddComponent<SpriteRenderer>();
            _texture = new Texture2D(8, 8);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            renderer.sprite = _sprite;
            return renderer;
        }

        private static void SetPrivateField(ParallaxLayer layer, string fieldName, object value)
        {
            typeof(ParallaxLayer).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(layer, value);
        }

        private static void InvokePrivate(ParallaxLayer layer, string methodName)
        {
            typeof(ParallaxLayer).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(layer, null);
        }
    }
}
