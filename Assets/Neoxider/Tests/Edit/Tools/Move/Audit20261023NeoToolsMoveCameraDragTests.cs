using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsMoveCameraDragTests
    {
        private GameObject _owner;
        private CameraRotationController _controller;
        private int _starts;
        private int _ends;

        [SetUp]
        public void SetUp()
        {
            _starts = 0;
            _ends = 0;
            _owner = new GameObject("Audit20261023NeoToolsMoveCameraDrag");
            _controller = _owner.AddComponent<CameraRotationController>();
            _controller.xAxisSettings.isEnabled = false;
            _controller.yAxisSettings = new AxisRotationSettings(true, 10f, -180f, 180f);
            _controller.zAxisSettings.isEnabled = false;
            UnityEvent started = new UnityEvent();
            UnityEvent ended = new UnityEvent();
            started.AddListener(() => _starts++);
            ended.AddListener(() => _ends++);
            SetField("onRotateStart", started);
            SetField("onRotateEnd", ended);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
        }

        [TestCase(-45f, 45f)]
        [TestCase(0f, 360f)]
        [TestCase(-360f, 0f)]
        [TestCase(360f, 720f)]
        public void InitialAngle_UsesEquivalentAuthoredRangeWithoutJump(float min, float max)
        {
            _controller.xAxisSettings = new AxisRotationSettings(true, 10f, min, max);
            _controller.yAxisSettings.isEnabled = false;
            _owner.transform.localRotation = Quaternion.Euler(350f, 0f, 0f);
            Quaternion initial = _owner.transform.localRotation;
            Invoke("Start");

            _controller.ProcessDragSample(Vector2.zero, true, true, 1f);
            _controller.ProcessDragSample(Vector2.zero, true, true, 1f);
            AssertRotation(initial);

            _controller.ProcessDragSample(Vector2.up, true, true, 1f);

            AssertRotation(Quaternion.Euler(349f, 0f, 0f));
            Assert.That(_starts, Is.EqualTo(1));
            Assert.That(_ends, Is.Zero);
        }

        [TestCase(0.25f, 5f)]
        [TestCase(0.5f, 10f)]
        public void Movement_PreservesLegacyPixelTimesDeltaTimeSensitivity(float deltaTime, float expectedYaw)
        {
            _controller.ProcessDragSample(new Vector2(100f, 100f), true, true, deltaTime);

            _controller.ProcessDragSample(new Vector2(120f, 100f), true, true, deltaTime);

            AssertRotation(Quaternion.Euler(0f, expectedYaw, 0f));
        }

        [Test]
        public void DisabledAxis_DoesNotUseItsUnusedLimits()
        {
            _controller.xAxisSettings.minAngle = float.NaN;
            _controller.xAxisSettings.maxAngle = float.NaN;
            _owner.transform.localRotation = Quaternion.Euler(350f, 0f, 0f);
            _controller.ProcessDragSample(Vector2.zero, true, true, 1f);

            _controller.ProcessDragSample(new Vector2(2f, 0f), true, true, 1f);

            AssertRotation(Quaternion.Euler(350f, 2f, 0f));
        }

        [Test]
        public void ModifierAfterMouseDown_StartsFromFreshBaselineAndBalancesTransitions()
        {
            _controller.ProcessDragSample(new Vector2(100f, 100f), true, false, 1f);
            Assert.That(_starts, Is.Zero);
            _controller.ProcessDragSample(new Vector2(1000f, 100f), true, true, 1f);
            AssertRotation(Quaternion.identity);
            Assert.That(_starts, Is.EqualTo(1));

            _controller.ProcessDragSample(new Vector2(1002f, 100f), true, true, 1f);
            Quaternion moved = Quaternion.Euler(0f, 2f, 0f);
            AssertRotation(moved);
            _controller.ProcessDragSample(new Vector2(2000f, 100f), true, false, 1f);
            Assert.That(_ends, Is.EqualTo(1));

            _controller.ProcessDragSample(new Vector2(3000f, 100f), true, true, 1f);
            AssertRotation(moved);
            Assert.That(_starts, Is.EqualTo(2));
            _controller.ProcessDragSample(new Vector2(3000f, 100f), false, true, 1f);
            _controller.ProcessDragSample(new Vector2(3000f, 100f), false, true, 1f);
            Assert.That(_ends, Is.EqualTo(2));
        }

        [Test]
        public void MissingPointer_EndsOnceAndResetsNextEntry()
        {
            _controller.ProcessDragSample(Vector2.zero, true, true, 1f);
            _controller.ProcessDragSample(null, true, true, 1f);
            _controller.ProcessDragSample(null, true, true, 1f);
            Assert.That(_ends, Is.EqualTo(1));

            _controller.ProcessDragSample(new Vector2(500f, 600f), true, true, 1f);

            Assert.That(_starts, Is.EqualTo(2));
            AssertRotation(Quaternion.identity);
        }

        [Test]
        public void DisableCleanup_IsIdempotentAndNewEntryRecapturesPose()
        {
            _controller.ProcessDragSample(Vector2.zero, true, true, 1f);
            _controller.enabled = false;
            // Explicit callback delivery checks cleanup; native lifecycle delivery remains a Play Mode acceptance case.
            Invoke("OnDisable");
            Invoke("OnDisable");
            Assert.That(_ends, Is.EqualTo(1));
            _owner.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            _controller.ProcessDragSample(new Vector2(100f, 0f), true, true, 1f);
            Assert.That(_starts, Is.EqualTo(1));

            _controller.enabled = true;
            _controller.ProcessDragSample(new Vector2(1000f, 0f), true, true, 1f);

            Assert.That(_starts, Is.EqualTo(2));
            AssertRotation(Quaternion.Euler(0f, 20f, 0f));
        }

        [TestCase("OnApplicationFocus", false)]
        [TestCase("OnApplicationPause", true)]
        public void FocusAndPauseCleanup_EndActiveDrag(string callback, bool value)
        {
            _controller.ProcessDragSample(Vector2.zero, true, true, 1f);

            Invoke(callback, value);
            Invoke(callback, value);

            Assert.That(_ends, Is.EqualTo(1));
        }

        [Test]
        public void PausedSample_UpdatesBaselineWithoutAccumulatingPointerMovement()
        {
            _controller.ProcessDragSample(Vector2.zero, true, true, 1f);
            _controller.ProcessDragSample(new Vector2(100f, 0f), true, true, 0f);
            AssertRotation(Quaternion.identity);

            _controller.ProcessDragSample(new Vector2(102f, 0f), true, true, 1f);

            AssertRotation(Quaternion.Euler(0f, 2f, 0f));
        }

        private void AssertRotation(Quaternion expected)
        {
            Assert.That(Quaternion.Angle(_owner.transform.localRotation, expected), Is.LessThan(0.05f));
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(CameraRotationController).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(_controller, value);
        }

        private void Invoke(string name, params object[] arguments)
        {
            MethodInfo method = typeof(CameraRotationController).GetMethod(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(_controller, arguments);
        }
    }
}
