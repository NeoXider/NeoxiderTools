using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class TransformMoveTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestMoveObjects");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        #region DistanceChecker Tests

        [Test]
        public void DistanceChecker_IsWithinDistance_EvaluatesCorrectly()
        {
            DistanceChecker checker = _go.AddComponent<DistanceChecker>();

            var target = new GameObject("Target");
            target.transform.position = new Vector3(5, 0, 0);

            checker.SetCurrentObject(_go.transform);
            checker.SetTarget(target.transform);
            checker.SetDistanceThreshold(6f);

            checker.ForceCheck();
            Assert.IsTrue(checker.IsWithinDistance(), "Target at distance 5 should be within threshold 6");

            target.transform.position = new Vector3(10, 0, 0);
            checker.ForceCheck();
            Assert.IsFalse(checker.IsWithinDistance(), "Target at distance 10 should not be within threshold 6");

            Object.DestroyImmediate(target);
        }

        [Test]
        public void DistanceChecker_Events_TriggerOnThresholdCross()
        {
            DistanceChecker checker = _go.AddComponent<DistanceChecker>();
            checker.onApproach = new UnityEngine.Events.UnityEvent();
            checker.onDepart = new UnityEngine.Events.UnityEvent();

            var target = new GameObject("Target");
            target.transform.position = new Vector3(10, 0, 0); // Start outside

            checker.SetCurrentObject(_go.transform);
            checker.SetTarget(target.transform);
            checker.SetDistanceThreshold(5f);

            bool approachFired = false;
            bool departFired = false;
            checker.onApproach.AddListener(() => approachFired = true);
            checker.onDepart.AddListener(() => departFired = true);

            checker.ForceCheck();
            Assert.IsFalse(approachFired, "Approach should not fire when starting outside");

            // Move inside
            target.transform.position = new Vector3(3, 0, 0);
            checker.ForceCheck();
            Assert.IsTrue(approachFired, "Approach should fire when crossing into threshold");
            Assert.IsFalse(departFired, "Depart should not fire yet");

            // Move outside
            approachFired = false;
            target.transform.position = new Vector3(10, 0, 0);
            checker.ForceCheck();
            Assert.IsTrue(departFired, "Depart should fire when moving outside threshold");
            Assert.IsFalse(approachFired, "Approach should not fire again");

            Object.DestroyImmediate(target);
        }

        #endregion

        #region UniversalRotator Tests

        [Test]
        public void UniversalRotator_RotateTo_Instant_RotatesCorrectly3D()
        {
            UniversalRotator rotator = _go.AddComponent<UniversalRotator>();

            // Set fields using reflection since they are private/serialized
            typeof(UniversalRotator)
                .GetField("rotationMode", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(rotator, UniversalRotator.RotationMode.Mode3D);

            typeof(UniversalRotator)
                .GetField("limitRange", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(rotator, new Vector2(0f, 360f)); // disabling limits

            _go.transform.position = Vector3.zero;
            _go.transform.rotation = Quaternion.identity;

            // Target is along Z axis (forward)
            var targetPoint = new Vector3(0, 0, 5);
            rotator.RotateTo(targetPoint, true);

            Vector3 forward = _go.transform.forward;
            Assert.IsTrue(Vector3.Distance(forward, Vector3.forward) < 0.01f,
                "Should rotate to face target point alongside Z");
        }

        [Test]
        public void UniversalRotator_RotateBy_AppliesDeltaRotation()
        {
            UniversalRotator rotator = _go.AddComponent<UniversalRotator>();

            _go.transform.rotation = Quaternion.Euler(0, 45, 0);

            // Limited Axis defaults to Y, limitRange to [0, 360] -> no limit clamping logic interference.
            rotator.RotateBy(Vector3.up * 45f);

            float currentY = _go.transform.eulerAngles.y;
            Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(currentY, 90f)) < 0.01f,
                $"Should be 90 degrees but was {currentY}");
        }

        #endregion
    }
}
