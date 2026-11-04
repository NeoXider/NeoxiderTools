using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests.Tools
{
    public sealed class CameraShakeRestoreTests
    {
        private GameObject _gameObject;
        private CameraShake _shake;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("CameraShakeRestore");
            _shake = _gameObject.AddComponent<CameraShake>();
            _shake.OnShakeStart = new UnityEvent();
            _shake.OnShakeStop = new UnityEvent();
            _shake.OnShakeComplete = new UnityEvent();
            _shake.shakeType = CameraShake.ShakeType.Both;
        }

        [TearDown]
        public void TearDown()
        {
            _shake.StopShake();
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void RelativeShake_OnAMovedCamera_StopsWhereTheShakeStarted()
        {
            _shake.relativeShake = true;
            Vector3 followPosition = new(12f, 3f, -4f);
            Quaternion followRotation = Quaternion.Euler(0f, 45f, 0f);
            _gameObject.transform.localPosition = followPosition;
            _gameObject.transform.localRotation = followRotation;

            _shake.StartShake(1f, 0.5f);
            _gameObject.transform.localPosition = followPosition + Vector3.one;
            _shake.StopShake();

            Assert.That(_gameObject.transform.localPosition, Is.EqualTo(followPosition),
                "Stopping a relative shake must not snap a follow camera back to its Awake pose.");
            Assert.That(Quaternion.Angle(_gameObject.transform.localRotation, followRotation), Is.LessThan(0.001f));
        }

        [Test]
        public void InterruptingRelativeShake_KeepsThePreShakePose()
        {
            _shake.relativeShake = true;
            Vector3 followPosition = new(5f, 0f, 0f);
            _gameObject.transform.localPosition = followPosition;

            _shake.StartShake(1f, 0.5f);
            _gameObject.transform.localPosition = followPosition + new Vector3(0.3f, 0.2f, 0f);
            _shake.StartShake(1f, 0.5f);
            _shake.StopShake();

            Assert.That(_gameObject.transform.localPosition, Is.EqualTo(followPosition));
        }
    }
}
