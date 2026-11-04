using System;
using System.Collections.Generic;
using Neo.Reactive;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Allocation and lifetime contracts of <see cref="ReactivePropertyBase{T,TEvent}" /> notifications.
    /// </summary>
    [TestFixture]
    public class ReactivePropertyFoundationTests
    {
        private sealed class CapturingLogHandler : ILogHandler
        {
            public readonly List<(LogType Type, string Message)> Entries = new();

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                Entries.Add((logType, string.Format(format, args)));
            }

            public void LogException(Exception exception, Object context)
            {
                Entries.Add((LogType.Exception, exception.Message));
            }
        }

        private sealed class FoundationReactiveSubscriber : ScriptableObject
        {
            public int Calls;

            public void HandleFoundationValue(int value)
            {
                Calls++;
            }
        }

        private sealed class CountingListener
        {
            public int Calls;
            public int Sum;

            public void Handle(int value)
            {
                Calls++;
                Sum += value;
            }
        }

        private sealed class CountingFloatListener
        {
            public int Calls;

            public void Handle(float value)
            {
                Calls++;
            }
        }

        [Test]
        public void Notify_WarmListeners_DoesNotAllocate()
        {
            ReactivePropertyInt property = new(0);
            CountingListener first = new();
            CountingListener second = new();
            property.AddListener(first.Handle);
            property.AddListener(second.Handle);
            property.Value = 1;
            property.ForceNotify();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 2; i < 1002; i++)
            {
                property.Value = i;
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(first.Calls, Is.EqualTo(1002));
            Assert.That(second.Calls, Is.EqualTo(1002));
        }

        [Test]
        public void NestedNotify_WarmBuffers_DoesNotAllocateAndDeliversEveryValue()
        {
            ReactivePropertyInt property = new(0);
            int clampCalls = 0;
            int observedAboveLimit = 0;
            CountingListener observer = new();
            // WHY: the clamping listener re-enters the setter, which starts a nested notification at depth 1.
            property.AddListener(value =>
            {
                clampCalls++;
                if (value > 10)
                {
                    observedAboveLimit++;
                    property.Value = 10;
                }
            });
            property.AddListener(observer.Handle);
            property.Value = 11;
            property.Value = 5;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 500; i++)
            {
                property.Value = 11;
                property.Value = 5;
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(property.Value, Is.EqualTo(5));
            Assert.That(observedAboveLimit, Is.EqualTo(501));
            Assert.That(clampCalls, Is.EqualTo(501 * 3),
                "Every set of 11 delivers 11 and the nested 10; every set of 5 delivers 5.");
            Assert.That(observer.Calls, Is.EqualTo(501 * 3));
            Assert.That(observer.Sum, Is.EqualTo(501 * (11 + 10 + 5)));
        }

        [TestCase(16)]
        [TestCase(128)]
        public void Notify_ManyDistinctListeners_DeliversEachOnceWithoutAllocating(int listenerCount)
        {
            ReactivePropertyFloat property = new(0f);
            CountingFloatListener[] listeners = new CountingFloatListener[listenerCount];
            for (int i = 0; i < listenerCount; i++)
            {
                listeners[i] = new CountingFloatListener();
                property.AddListener(listeners[i].Handle);
            }

            property.Value = 1f;
            Assert.That(property.ListenerCount, Is.EqualTo(listenerCount),
                "Distinct targets are distinct subscriptions; only the same delegate is deduplicated.");

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 2; i < 202; i++)
            {
                property.Value = i;
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            for (int i = 0; i < listenerCount; i++)
            {
                Assert.That(listeners[i].Calls, Is.EqualTo(201), $"Listener {i} must see every changed value once.");
            }
        }

        [Test]
        public void Notify_CodeAndRuntimeUnityEventListeners_DoesNotAllocate()
        {
            ReactivePropertyFloat property = new(0f);
            CountingFloatListener code = new();
            CountingFloatListener unityEvent = new();
            property.AddListener(code.Handle);
            property.OnChanged.AddListener(unityEvent.Handle);
            property.Value = 1f;
            Action writes = () =>
            {
                for (int i = 2; i < 502; i++)
                {
                    property.Value = i;
                }
            };

            GcAlloc.AssertNone(writes,
                "Set + notify through code listeners and the UnityEvent<float> (what a per-frame timer label does) " +
                "must not allocate once warm.");

            Assert.That(code.Calls, Is.EqualTo(501));
            Assert.That(unityEvent.Calls, Is.EqualTo(501));
        }

        [Test]
        public void AddListener_SameClosureDelegateTwice_IsDeduplicated()
        {
            ReactivePropertyInt property = new(0);
            int delivered = 0;
            for (int i = 0; i < 4; i++)
            {
                property.AddListener(_ => delivered++);
            }

            property.Value = 1;

            Assert.That(property.ListenerCount, Is.EqualTo(1),
                "A lambda built in a loop over the same closure is the same delegate (target and method), so it " +
                "subscribes once; benchmarks need one listener object per subscription.");
            Assert.That(delivered, Is.EqualTo(1));
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(1)]
        [TestCase(16)]
        [TestCase(128)]
        public void Benchmark_SetAndNotify_DistinctListeners(int listenerCount)
        {
            ReactivePropertyFloat property = new(0f);
            CountingFloatListener[] listeners = new CountingFloatListener[listenerCount];
            for (int i = 0; i < listenerCount; i++)
            {
                listeners[i] = new CountingFloatListener();
                property.AddListener(listeners[i].Handle);
            }

            float next = 0f;
            Action write = () =>
            {
                next = next >= 1000000f ? 0f : next + 1f;
                property.Value = next;
            };

            NeoPerf.MeasureSynchronous("reactive.float.set_notify.distinct_listeners" + listenerCount, listenerCount,
                    200, NeoPerf.DefaultWarmupSamples, 60, write)
                .Add("role", "current")
                .Emit();

            Assert.That(listeners[listenerCount - 1].Calls, Is.GreaterThan(0));
        }

        [Test]
        public void WeakSubscription_DestroyedUnityTarget_IsDroppedSilently()
        {
            ReactivePropertyInt property = new(0);
            FoundationReactiveSubscriber subscriber = ScriptableObject.CreateInstance<FoundationReactiveSubscriber>();
            CapturingLogHandler capture = new();
            ILogHandler previousHandler = Debug.unityLogger.logHandler;
            bool previousWarnings = NeoDiagnostics.RuntimeWarningsEnabled;
            try
            {
                NeoDiagnostics.ResetStaticState();
                NeoDiagnostics.Configure(warnings: true);
                Debug.unityLogger.logHandler = capture;
                property.AddWeakListener(subscriber.HandleFoundationValue);
                property.Value = 1;
                Assert.That(subscriber.Calls, Is.EqualTo(1));

                Object.DestroyImmediate(subscriber);
                property.Value = 2;

                Assert.That(subscriber.Calls, Is.EqualTo(1));
                Assert.That(property.ListenerCount, Is.Zero);
                Assert.That(capture.Entries, Is.Empty,
                    "A weak subscriber opted into automatic cleanup; its destroyed target is not a leak to report.");
            }
            finally
            {
                Debug.unityLogger.logHandler = previousHandler;
                NeoDiagnostics.ResetStaticState();
                NeoDiagnostics.Configure(warnings: previousWarnings);
                if (subscriber != null)
                {
                    Object.DestroyImmediate(subscriber);
                }
            }
        }

        [Test]
        public void StrongSubscription_DestroyedUnityTarget_StillWarnsOnce()
        {
            ReactivePropertyInt property = new(0);
            FoundationReactiveSubscriber subscriber = ScriptableObject.CreateInstance<FoundationReactiveSubscriber>();
            CapturingLogHandler capture = new();
            ILogHandler previousHandler = Debug.unityLogger.logHandler;
            bool previousWarnings = NeoDiagnostics.RuntimeWarningsEnabled;
            try
            {
                NeoDiagnostics.ResetStaticState();
                NeoDiagnostics.Configure(warnings: true);
                Debug.unityLogger.logHandler = capture;
                property.AddListener(subscriber.HandleFoundationValue);

                Object.DestroyImmediate(subscriber);
                property.Value = 1;
                property.Value = 2;

                Assert.That(property.ListenerCount, Is.Zero);
                Assert.That(capture.Entries, Has.Count.EqualTo(1));
                Assert.That(capture.Entries[0].Type, Is.EqualTo(LogType.Warning));
                StringAssert.Contains("HandleFoundationValue", capture.Entries[0].Message);
            }
            finally
            {
                Debug.unityLogger.logHandler = previousHandler;
                NeoDiagnostics.ResetStaticState();
                NeoDiagnostics.Configure(warnings: previousWarnings);
                if (subscriber != null)
                {
                    Object.DestroyImmediate(subscriber);
                }
            }
        }
    }
}
