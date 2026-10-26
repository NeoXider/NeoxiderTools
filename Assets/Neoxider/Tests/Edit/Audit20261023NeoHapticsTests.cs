using System;
using System.Collections.Generic;
using NUnit.Framework;
using DeviceHaptics = Neo.Haptics.Haptics;
using HapticType = Neo.Haptics.HapticType;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoHapticsTests
    {
        private readonly List<HapticType> _played = new List<HapticType>();
        private bool _previousEnabled;
        private Func<bool> _previousProvider;
        private float _previousInterval;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = DeviceHaptics.EnabledProvider;
            DeviceHaptics.EnabledProvider = null;
            _previousEnabled = DeviceHaptics.Enabled;
            _previousInterval = DeviceHaptics.MinRepeatInterval;
            DeviceHaptics.Enabled = true;
            DeviceHaptics.MinRepeatInterval = float.MaxValue;
            DeviceHaptics.ResetThrottle();
            _played.Clear();
            DeviceHaptics.Played += RecordPulse;
        }

        [TearDown]
        public void TearDown()
        {
            DeviceHaptics.Played -= RecordPulse;
            DeviceHaptics.ResetThrottle();
            DeviceHaptics.MinRepeatInterval = _previousInterval;
            DeviceHaptics.Enabled = _previousEnabled;
            DeviceHaptics.EnabledProvider = _previousProvider;
        }

        [Test]
        public void RepeatThrottle_RemembersEveryTypeAcrossInterleavedRequests()
        {
            HapticType[] types = (HapticType[])Enum.GetValues(typeof(HapticType));
            foreach (HapticType type in types)
            {
                DeviceHaptics.Play(type);
            }

            foreach (HapticType type in types)
            {
                DeviceHaptics.Play(type);
            }

            CollectionAssert.AreEqual(types, _played);
        }

        [Test]
        public void CustomPulse_SharesMediumThrottleAcrossAnotherType()
        {
            DeviceHaptics.Play(0.5f, 0.5f, 0.1f);
            DeviceHaptics.Play(HapticType.Light);
            DeviceHaptics.Play(HapticType.Medium);

            CollectionAssert.AreEqual(new[] { HapticType.Medium, HapticType.Light }, _played);
        }

        [Test]
        public void Pattern_SharesHeavyThrottleAcrossAnotherType()
        {
            DeviceHaptics.PlayPattern(new[] { 0.1f }, new[] { 1f });
            DeviceHaptics.Play(HapticType.Selection);
            DeviceHaptics.Play(HapticType.Heavy);

            CollectionAssert.AreEqual(new[] { HapticType.Heavy, HapticType.Selection }, _played);
        }

        [Test]
        public void ResetThrottle_AllowsEveryPreviouslyPlayedTypeAgain()
        {
            DeviceHaptics.Play(HapticType.Selection);
            DeviceHaptics.Play(HapticType.Light);
            DeviceHaptics.ResetThrottle();
            DeviceHaptics.Play(HapticType.Selection);
            DeviceHaptics.Play(HapticType.Light);

            CollectionAssert.AreEqual(new[]
            {
                HapticType.Selection, HapticType.Light, HapticType.Selection, HapticType.Light
            }, _played);
        }

        [Test]
        public void ZeroRepeatInterval_AllowsImmediateRepeats()
        {
            DeviceHaptics.MinRepeatInterval = 0f;
            DeviceHaptics.Play(HapticType.Light);
            DeviceHaptics.Play(HapticType.Light);

            CollectionAssert.AreEqual(new[] { HapticType.Light, HapticType.Light }, _played);
        }

        [Test]
        public void DisabledRequests_DoNotConsumeThrottleAndMasterSwitchShortCircuitsProvider()
        {
            int providerCalls = 0;
            bool providerEnabled = false;
            DeviceHaptics.EnabledProvider = () =>
            {
                providerCalls++;
                return providerEnabled;
            };
            DeviceHaptics.Enabled = false;
            DeviceHaptics.Play(HapticType.Selection);
            Assert.AreEqual(0, providerCalls);
            DeviceHaptics.Enabled = true;
            DeviceHaptics.Play(HapticType.Selection);
            Assert.AreEqual(1, providerCalls);
            Assert.IsEmpty(_played);

            providerEnabled = true;
            DeviceHaptics.Play(HapticType.Selection);

            CollectionAssert.AreEqual(new[] { HapticType.Selection }, _played);
        }

        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void EmptyPattern_DoesNotNotifyOrThrottleHeavy(int durationCount, int amplitudeCount)
        {
            DeviceHaptics.PlayPattern(new float[durationCount], new float[amplitudeCount]);
            Assert.IsEmpty(_played, "No paired segments means no accepted pulse.");

            DeviceHaptics.Play(HapticType.Heavy);

            CollectionAssert.AreEqual(new[] { HapticType.Heavy }, _played);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void NullPattern_DoesNotNotifyOrThrottleHeavy(bool nullDurations, bool nullAmplitudes)
        {
            float[] durations = nullDurations ? null : new[] { 0.1f };
            float[] amplitudes = nullAmplitudes ? null : new[] { 1f };
            DeviceHaptics.PlayPattern(durations, amplitudes);
            Assert.IsEmpty(_played);

            DeviceHaptics.Play(HapticType.Heavy);

            CollectionAssert.AreEqual(new[] { HapticType.Heavy }, _played);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteCustomPulse_DoesNotNotifyOrThrottleMedium(float invalid)
        {
            DeviceHaptics.Play(invalid, 0.5f, 0.1f);
            DeviceHaptics.Play(0.5f, invalid, 0.1f);
            DeviceHaptics.Play(0.5f, 0.5f, invalid);
            Assert.IsEmpty(_played);

            DeviceHaptics.Play(0.5f, 0.5f, 0.1f);

            CollectionAssert.AreEqual(new[] { HapticType.Medium }, _played);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFinitePairedPattern_DoesNotNotifyOrThrottleHeavy(float invalid)
        {
            DeviceHaptics.PlayPattern(new[] { 0.1f, invalid }, new[] { 0.5f, 1f });
            DeviceHaptics.PlayPattern(new[] { 0.1f, 0.1f }, new[] { 0.5f, invalid });
            Assert.IsEmpty(_played);

            DeviceHaptics.PlayPattern(new[] { 0.1f }, new[] { 1f });

            CollectionAssert.AreEqual(new[] { HapticType.Heavy }, _played);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Pattern_IgnoresUnpairedTail(bool extraDuration)
        {
            float[] durations = extraDuration ? new[] { 0.1f, float.NaN } : new[] { 0.1f };
            float[] amplitudes = extraDuration ? new[] { 1f } : new[] { 1f, float.NaN };

            DeviceHaptics.PlayPattern(durations, amplitudes);

            CollectionAssert.AreEqual(new[] { HapticType.Heavy }, _played);
        }

        [TestCase(-1)]
        [TestCase(9)]
        [TestCase(int.MaxValue)]
        public void UndefinedType_DoesNotNotifyOrResetAnotherTypesCooldown(int invalid)
        {
            DeviceHaptics.Play(HapticType.Light);
            DeviceHaptics.Play((HapticType)invalid);
            DeviceHaptics.Play(HapticType.Light);

            CollectionAssert.AreEqual(new[] { HapticType.Light }, _played);
        }

        private void RecordPulse(HapticType type)
        {
            _played.Add(type);
        }
    }
}
