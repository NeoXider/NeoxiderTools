using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Neo.Reactive;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoReactiveRegressionTests
    {
        private sealed class Subscriber
        {
            public int Calls;
            public int LastValue;
            public bool ThrowOnCall;

            public void Handle(int value)
            {
                Calls++;
                LastValue = value;
                if (ThrowOnCall)
                {
                    throw new InvalidOperationException("audit immediate failure");
                }
            }
        }

        private sealed class UnitySubscriber : ScriptableObject
        {
            public int Calls;

            public void Handle(int value)
            {
                Calls++;
            }
        }

        [TestCase(ReactiveListenerMode.Strong)]
        [TestCase(ReactiveListenerMode.Weak)]
        public void ListenerCount_PrunesDestroyedTargetsWithoutANotification(ReactiveListenerMode mode)
        {
            ReactivePropertyInt property = new(7);
            UnitySubscriber subscriber = ScriptableObject.CreateInstance<UnitySubscriber>();
            try
            {
                property.AddListener(subscriber.Handle, mode);
                Assert.AreEqual(1, property.ListenerCount);

                UnityEngine.Object.DestroyImmediate(subscriber);

                Assert.AreEqual(0, property.ListenerCount);
                Assert.AreEqual(7, property.CurrentValue, "Counting must not emit or change the value.");
                Assert.AreEqual(0, subscriber.Calls);
            }
            finally
            {
                if (subscriber != null)
                {
                    UnityEngine.Object.DestroyImmediate(subscriber);
                }
            }
        }

        [Test]
        public void ListenerCount_PrunesDestroyedTargetSilentlyWithWarningsEnabled()
        {
            ReactivePropertyInt property = new(7);
            UnitySubscriber subscriber = ScriptableObject.CreateInstance<UnitySubscriber>();
            bool previousWarnings = NeoDiagnostics.RuntimeWarningsEnabled;
            int warningCount = 0;
            Application.LogCallback onLog = (message, stackTrace, type) =>
            {
                if (type == LogType.Warning && message.StartsWith("[ReactiveProperty]", StringComparison.Ordinal))
                {
                    warningCount++;
                }
            };

            Application.logMessageReceived += onLog;
            try
            {
                NeoDiagnostics.Configure(warnings: true);
                property.AddListener(subscriber.Handle);
                UnityEngine.Object.DestroyImmediate(subscriber);

                Assert.AreEqual(0, property.ListenerCount);
                Assert.AreEqual(7, property.CurrentValue);
                Assert.AreEqual(0, subscriber.Calls);
                Assert.AreEqual(0, warningCount, "Count-triggered cleanup is silent even with warnings enabled.");

                property.Value = 8;
                Assert.AreEqual(0, subscriber.Calls);
                Assert.AreEqual(0, warningCount, "The pruned registration must not warn on a later notification.");
            }
            finally
            {
                Application.logMessageReceived -= onLog;
                NeoDiagnostics.Configure(warnings: previousWarnings);
                if (subscriber != null)
                {
                    UnityEngine.Object.DestroyImmediate(subscriber);
                }
            }
        }

        [Test]
        public void ListenerCount_PrunesCollectedTargetsWithoutANotification()
        {
            ReactivePropertyInt property = new(7);
            WeakReference subscriber = SubscribeInSeparateScope(property);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(subscriber.IsAlive, "The explicit-target overload must not retain its target.");
            Assert.AreEqual(0, property.ListenerCount);
            Assert.AreEqual(7, property.CurrentValue);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeInSeparateScope(ReactivePropertyInt property)
        {
            Subscriber subscriber = new();
            property.SubscribeWeak(subscriber, static (target, value) => target.Handle(value));
            return new WeakReference(subscriber);
        }

        [Test]
        public void ExplicitWeakImmediate_ThrowingHandlerStillReturnsAUsableHandle()
        {
            ReactivePropertyInt property = new(7);
            Subscriber subscriber = new() { ThrowOnCall = true };
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: audit immediate failure"));

            IDisposable handle = property.SubscribeWeak(subscriber,
                static (target, value) => target.Handle(value), true);

            Assert.AreEqual(1, subscriber.Calls);
            Assert.AreEqual(7, subscriber.LastValue);
            Assert.AreEqual(1, property.ListenerCount);

            subscriber.ThrowOnCall = false;
            property.Value = 8;
            Assert.AreEqual(2, subscriber.Calls);
            Assert.AreEqual(8, subscriber.LastValue);

            handle.Dispose();
            handle.Dispose();
            property.Value = 9;
            Assert.AreEqual(2, subscriber.Calls);
            Assert.AreEqual(0, property.ListenerCount);
        }

        [Test]
        public void ExplicitWeakImmediate_DestroyedTargetIsNeverInvoked()
        {
            ReactivePropertyInt property = new(7);
            UnitySubscriber subscriber = ScriptableObject.CreateInstance<UnitySubscriber>();
            UnityEngine.Object.DestroyImmediate(subscriber);

            IDisposable handle = property.SubscribeWeak(subscriber,
                static (target, value) => target.Handle(value), true);

            Assert.AreEqual(0, subscriber.Calls, "Immediate delivery must respect Unity object liveness.");
            Assert.AreEqual(0, property.ListenerCount);
            handle.Dispose();
        }

        [Test]
        public void RemoveListener_NullLeavesTheExistingRegistrationWorking()
        {
            ReactivePropertyInt property = new(7);
            Subscriber subscriber = new();
            property.AddListener(subscriber.Handle);

            property.RemoveListener(null);
            property.Value = 8;

            Assert.AreEqual(1, subscriber.Calls);
            Assert.AreEqual(8, subscriber.LastValue);
            Assert.AreEqual(1, property.ListenerCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WeakMulticast_PreservesAllCallbacksAndCanBeRemoved(bool invokeImmediately)
        {
            ReactivePropertyInt property = new(7);
            Subscriber first = new();
            Subscriber second = new();
            UnityAction<int> listener = first.Handle;
            listener += second.Handle;

            IDisposable handle = property.SubscribeWeak(listener, invokeImmediately);
            property.AddWeakListener(listener);
            property.Value = 8;

            int expectedCalls = invokeImmediately ? 2 : 1;
            Assert.AreEqual(expectedCalls, first.Calls);
            Assert.AreEqual(expectedCalls, second.Calls);
            Assert.AreEqual(8, first.LastValue);
            Assert.AreEqual(8, second.LastValue);
            Assert.AreEqual(1, property.ListenerCount, "The multicast delegate is one registration.");

            property.RemoveListener(listener);
            property.ForceNotify();
            Assert.AreEqual(expectedCalls, first.Calls);
            Assert.AreEqual(expectedCalls, second.Calls);
            Assert.AreEqual(0, property.ListenerCount);
            handle.Dispose();
        }
    }
}
