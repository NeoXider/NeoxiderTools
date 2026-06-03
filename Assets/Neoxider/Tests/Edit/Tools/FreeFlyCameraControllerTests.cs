using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public sealed class FreeFlyCameraControllerTests
    {
        private GameObject _go;
        private FreeFlyCameraController _controller;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("FreeFlyCamera");
            _controller = _go.AddComponent<FreeFlyCameraController>();
            _controller.SetRequireLookButton(false);
            _controller.SetMoveOnlyWhileLooking(false);
            _controller.SetBaseSpeed(10f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Tick_WithExternalMoveInput_MovesInLocalForwardDirection()
        {
            _controller.SetRotationAngles(90f, 0f);
            _controller.SetExternalMoveInput(Vector3.forward);

            _controller.Tick(0.5f);

            Assert.That(_go.transform.position.x, Is.EqualTo(5f).Within(0.005f));
            Assert.That(_go.transform.position.z, Is.EqualTo(0f).Within(0.005f));
            Assert.That(_controller.IsFlying, Is.True);
        }

        [Test]
        public void Tick_WithExternalLookInput_ClampsPitch()
        {
            _controller.SetRotationAngles(0f, 0f);
            _controller.SetExternalLookInput(new Vector2(0f, 1000f));

            _controller.Tick(0.016f);

            Assert.That(_go.transform.eulerAngles.x, Is.EqualTo(271f).Within(0.01f));
            Assert.That(_controller.IsLooking, Is.True);
        }

        [Test]
        public void DisabledController_DoesNotMove()
        {
            _controller.SetExternalMoveInput(Vector3.forward);
            _controller.SetControllerEnabled(false);

            _controller.Tick(1f);

            Assert.That(_go.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(_controller.IsFlying, Is.False);
            Assert.That(_controller.IsLooking, Is.False);
        }
    }
}
