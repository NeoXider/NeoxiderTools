using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DeviceHaptics = Neo.Haptics.Haptics;
using HapticType = Neo.Haptics.HapticType;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="DeviceHaptics.Played" /> observers run in isolation from each other, and dispatching to them does
    ///     not allocate per pulse.
    /// </summary>
    [TestFixture]
    public class HapticsObserverIsolationTests
    {
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
            DeviceHaptics.MinRepeatInterval = 0f;
            DeviceHaptics.ResetThrottle();
        }

        [TearDown]
        public void TearDown()
        {
            DeviceHaptics.ResetThrottle();
            DeviceHaptics.MinRepeatInterval = _previousInterval;
            DeviceHaptics.Enabled = _previousEnabled;
            DeviceHaptics.EnabledProvider = _previousProvider;
        }

        [Test]
        public void ThrowingObserver_DoesNotSkipObserversSubscribedAfterIt()
        {
            List<HapticType> before = new();
            List<HapticType> after = new();
            Action<HapticType> first = before.Add;
            Action<HapticType> throwing = type => throw new InvalidOperationException("haptics isolation failure");
            Action<HapticType> last = after.Add;
            DeviceHaptics.Played += first;
            DeviceHaptics.Played += throwing;
            DeviceHaptics.Played += last;
            try
            {
                LogAssert.Expect(LogType.Exception, new Regex("haptics isolation failure"));
                LogAssert.Expect(LogType.Exception, new Regex("haptics isolation failure"));

                DeviceHaptics.Play(HapticType.Light);
                DeviceHaptics.Play(HapticType.Heavy);

                CollectionAssert.AreEqual(new[] { HapticType.Light, HapticType.Heavy }, before);
                CollectionAssert.AreEqual(new[] { HapticType.Light, HapticType.Heavy }, after,
                    "An observer after the faulty one must still see every pulse.");
            }
            finally
            {
                DeviceHaptics.Played -= first;
                DeviceHaptics.Played -= throwing;
                DeviceHaptics.Played -= last;
            }
        }

        [Test]
        public void ObserverChangesBetweenPulses_AreSeenByTheNextPulse()
        {
            List<HapticType> first = new();
            List<HapticType> second = new();
            Action<HapticType> firstObserver = first.Add;
            Action<HapticType> secondObserver = second.Add;
            DeviceHaptics.Played += firstObserver;
            try
            {
                DeviceHaptics.Play(HapticType.Light);
                DeviceHaptics.Played += secondObserver;
                DeviceHaptics.Play(HapticType.Medium);
                DeviceHaptics.Played -= firstObserver;
                DeviceHaptics.Play(HapticType.Heavy);

                CollectionAssert.AreEqual(new[] { HapticType.Light, HapticType.Medium }, first);
                CollectionAssert.AreEqual(new[] { HapticType.Medium, HapticType.Heavy }, second);
            }
            finally
            {
                DeviceHaptics.Played -= firstObserver;
                DeviceHaptics.Played -= secondObserver;
            }
        }

        [Test]
        public void Unsubscribe_ReleasesTheObserverWithoutAnotherPulse()
        {
            List<HapticType> seen = new();
            Action<HapticType> observer = seen.Add;
            DeviceHaptics.Played += observer;
            try
            {
                DeviceHaptics.Play(HapticType.Light);
                Assert.That(StaticFieldsReference(observer), Is.True, "Control: the pulse cached the observer.");
            }
            finally
            {
                DeviceHaptics.Played -= observer;
            }

            Assert.That(StaticFieldsReference(observer), Is.False,
                "No static field (delegate or cached invocation list) may keep an unsubscribed observer alive.");
            CollectionAssert.AreEqual(new[] { HapticType.Light }, seen);
        }

        [Test]
        public void Play_WarmWithTwoObservers_DoesNotAllocate()
        {
            int firstCalls = 0;
            int secondCalls = 0;
            Action<HapticType> first = type => firstCalls++;
            Action<HapticType> second = type => secondCalls++;
            DeviceHaptics.Played += first;
            DeviceHaptics.Played += second;
            try
            {
                DeviceHaptics.Play(HapticType.Selection);
                Action pulses = () =>
                {
                    for (int i = 0; i < 900; i++)
                    {
                        DeviceHaptics.Play((HapticType)(i % 9));
                    }
                };

                GcAlloc.AssertNone(pulses, "Observer dispatch reuses the cached invocation list on every pulse.");

                Assert.That(firstCalls, Is.EqualTo(901));
                Assert.That(secondCalls, Is.EqualTo(901));
            }
            finally
            {
                DeviceHaptics.Played -= first;
                DeviceHaptics.Played -= second;
            }
        }

        private static bool StaticFieldsReference(Delegate observer)
        {
            foreach (FieldInfo field in typeof(DeviceHaptics).GetFields(BindingFlags.Static | BindingFlags.Public |
                                                                        BindingFlags.NonPublic))
            {
                object value = field.GetValue(null);
                if (value is Delegate single && Array.IndexOf(single.GetInvocationList(), observer) >= 0)
                {
                    return true;
                }

                if (value is Delegate[] list && Array.IndexOf(list, observer) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
