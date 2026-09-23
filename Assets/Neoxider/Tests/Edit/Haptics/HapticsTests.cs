using System.Collections.Generic;
using Neo.Haptics;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class HapticsTests
    {
        private readonly List<HapticType> _played = new();

        [SetUp]
        public void SetUp()
        {
            _played.Clear();
            Haptics.Enabled = true;
            Haptics.EnabledProvider = null;
            Haptics.ResetThrottle();
            Haptics.Played += Record;
        }

        [TearDown]
        public void TearDown()
        {
            Haptics.Played -= Record;
            Haptics.Enabled = true;
            Haptics.EnabledProvider = null;
            Haptics.ResetThrottle();
        }

        private void Record(HapticType type) => _played.Add(type);

        [Test]
        public void Play_ReportsEveryPulseEvenWhereTheDeviceCannotVibrate()
        {
            Assert.IsFalse(Haptics.IsSupported, "The editor never vibrates.");

            Haptics.Play(HapticType.Light);
            Haptics.Play(HapticType.Heavy);

            CollectionAssert.AreEqual(new[] { HapticType.Light, HapticType.Heavy }, _played);
        }

        [Test]
        public void Disabled_IsSilent_AndTheProviderOverridesTheFlag()
        {
            Haptics.Enabled = false;
            Haptics.Play(HapticType.Success);
            Assert.IsEmpty(_played);

            Haptics.Enabled = true;
            bool setting = false;
            Haptics.EnabledProvider = () => setting;
            Haptics.Play(HapticType.Success);
            Assert.IsEmpty(_played, "A settings owner that says no must win over the default flag.");

            setting = true;
            Haptics.Play(HapticType.Success);
            CollectionAssert.AreEqual(new[] { HapticType.Success }, _played);
        }

        [Test]
        public void SameTypeInOneBurst_IsThrottled_ButADifferentTypeStillPlays()
        {
            Haptics.MinRepeatInterval = 10f;
            try
            {
                Haptics.Play(HapticType.Selection);
                Haptics.Play(HapticType.Selection);
                Haptics.Play(HapticType.Light);
            }
            finally
            {
                Haptics.MinRepeatInterval = 0.035f;
            }

            CollectionAssert.AreEqual(new[] { HapticType.Selection, HapticType.Light }, _played);
        }

        [Test]
        public void Pattern_IgnoresMissingArrays()
        {
            Assert.DoesNotThrow(() => Haptics.PlayPattern(null, new[] { 1f }));
            Assert.DoesNotThrow(() => Haptics.PlayPattern(new[] { 0.1f }, null));
            Assert.IsEmpty(_played);
        }
    }
}
