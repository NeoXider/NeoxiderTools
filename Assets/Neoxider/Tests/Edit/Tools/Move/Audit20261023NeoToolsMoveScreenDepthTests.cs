using System.Reflection;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsMoveScreenDepthTests
    {
        private GameObject _cameraObject;
        private GameObject _owner;
        private Camera _camera;
        private ScreenPositioner _positioner;
        private readonly Vector2 _pixelOffset = new Vector2(8f, -12f);

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("Audit20261023NeoToolsMoveDepthCamera");
            _cameraObject.tag = "MainCamera";
            _camera = _cameraObject.AddComponent<Camera>();
            _camera.pixelRect = new Rect(40f, 30f, 640f, 480f);
            _camera.nearClipPlane = 0.75f;
            _camera.transform.SetPositionAndRotation(new Vector3(3f, 4f, -8f), Quaternion.Euler(25f, 30f, 10f));
            _owner = new GameObject("Audit20261023NeoToolsMoveDepthPositioner");
            _positioner = _owner.AddComponent<ScreenPositioner>();
            SetField("_targetCamera", _camera);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_cameraObject);
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void Configure_UsesNearPlaneRelativeDepthAndPreservesZOnlyWhenRequested(
            bool orthographic, bool useDepth)
        {
            _camera.orthographic = orthographic;
            SetField("_useDepth", useDepth);
            _owner.transform.position = new Vector3(1f, 2f, 17f);
            float depth = 7f;
            Vector3 expected = _camera.ScreenToWorldPoint(new Vector3(
                _camera.pixelRect.xMax + _pixelOffset.x,
                _camera.pixelRect.yMax + _pixelOffset.y,
                _camera.nearClipPlane + depth));
            // Native projection/readback is not an exact inverse, even for an orthographic camera.
            // Use the independent native point's readback, retaining the full XYZ position oracle below.
            float expectedNativeDepth = _camera.WorldToScreenPoint(expected).z;
            if (!useDepth)
            {
                expected.z = 17f;
            }

            _positioner.Configure(ScreenEdge.TopRight, _pixelOffset, depth);

            Assert.That(Vector3.Distance(_owner.transform.position, expected), Is.LessThan(1e-4f));
            if (useDepth)
            {
                Assert.That(_camera.WorldToScreenPoint(_owner.transform.position).z,
                    Is.EqualTo(expectedNativeDepth).Within(1e-4f));
            }
            else
            {
                Assert.That(_owner.transform.position.z, Is.EqualTo(17f));
            }
        }

        [Test]
        public void OrthographicMigration_SubtractNearClipPreservesFullOldPose()
        {
            _camera.orthographic = true;
            SetField("_useDepth", true);
            float oldCameraRelativeDepth = 8f;
            Vector3 original = _camera.ScreenToWorldPoint(new Vector3(
                _camera.pixelRect.xMax + _pixelOffset.x,
                _camera.pixelRect.yMax + _pixelOffset.y,
                oldCameraRelativeDepth));

            _positioner.Configure(ScreenEdge.TopRight, _pixelOffset,
                oldCameraRelativeDepth - _camera.nearClipPlane);

            Assert.That(Vector3.Distance(_owner.transform.position, original), Is.LessThan(1e-4f));
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(ScreenPositioner).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(_positioner, value);
        }
    }
}
