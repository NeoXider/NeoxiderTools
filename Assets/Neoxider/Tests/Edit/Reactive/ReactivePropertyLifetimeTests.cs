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
    /// <summary>
    ///     Covers the notification contract of <see cref="ReactivePropertyBase{T,TEvent}" />: re-entrant writes,
    ///     failure isolation, and the automatic cleanup of subscriptions whose target is gone.
    /// </summary>
    [TestFixture]
    public class ReactivePropertyLifetimeTests
    {
        private sealed class Subscriber
        {
            public int Calls;
            public int LastValue;

            public void Handle(int value)
            {
                Calls++;
                LastValue = value;
            }
        }

        private sealed class DestroyableSubscriber : MonoBehaviour
        {
            public int Calls;

            public void Handle(int value)
            {
                Calls++;
            }
        }

        [Test]
        public void ReentrantSet_StillNotifiesEveryOuterListenerExactlyOnce()
        {
            ReactivePropertyInt property = new(0);
            int firstCalls = 0;
            int secondCalls = 0;
            int thirdCalls = 0;

            property.AddListener(_ => firstCalls++);
            property.AddListener(value =>
            {
                secondCalls++;
                if (value == 1)
                {
                    // WHY: the classic leak - a listener reacting by writing back into the same property.
                    property.Value = 2;
                }
            });
            property.AddListener(_ => thirdCalls++);

            property.Value = 1;

            // Outer notification for 1 plus the nested notification for 2 = two calls each.
            Assert.AreEqual(2, firstCalls, "first listener");
            Assert.AreEqual(2, secondCalls, "re-entrant listener");
            Assert.AreEqual(2, thirdCalls, "listener registered after the re-entrant one");
        }

        [Test]
        public void ReentrantSet_OuterListenersSeeTheValueOfTheirOwnNotification()
        {
            ReactivePropertyInt property = new(0);
            int observedByLast = -1;

            property.AddListener(value =>
            {
                if (value == 1)
                {
                    property.Value = 2;
                }
            });
            property.AddListener(value => observedByLast = value);

            property.Value = 1;

            // The nested notification runs first and delivers 2; the outer one must still deliver its own value.
            Assert.AreEqual(1, observedByLast);
        }

        [Test]
        public void ThrowingListener_DoesNotStopTheOthers()
        {
            ReactivePropertyInt property = new(0);
            int reached = 0;

            property.AddListener(_ => throw new InvalidOperationException("boom"));
            property.AddListener(_ => reached++);

            // WHY: assert the exact exception is reported. Ignoring every failing message would also hide a
            // different error raised on the way there.
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: boom"));

            Assert.DoesNotThrow(() => property.Value = 1);

            Assert.AreEqual(1, reached);
        }

        [Test]
        public void Subscribe_ReturnsHandleThatUnsubscribes()
        {
            ReactivePropertyInt property = new(0);
            int calls = 0;

            IDisposable handle = property.Subscribe(_ => calls++);
            property.Value = 1;
            handle.Dispose();
            property.Value = 2;

            Assert.AreEqual(1, calls);
            Assert.AreEqual(0, property.ListenerCount);
        }

        [Test]
        public void Subscribe_WithImmediateInvoke_DeliversCurrentValue()
        {
            ReactivePropertyInt property = new(7);
            int observed = -1;

            property.Subscribe(value => observed = value, true);

            Assert.AreEqual(7, observed);
        }

        /// <summary>
        ///     WHY: the subscriber is created inside a separate non-inlined method so its stack slot is gone by the
        ///     time the collection runs. A local in the test body stays reachable in a Debug build, which would make
        ///     this test pass or fail depending on the compiler's optimisation level rather than on the code.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubscribeWeaklyInSeparateScope(ReactivePropertyInt property)
        {
            Subscriber subscriber = new();
            property.AddWeakListener(subscriber.Handle);
            property.Value = 1;
            Assert.AreEqual(1, subscriber.Calls, "weak listener should fire while the target is alive");
        }

        [Test]
        public void WeakListener_IsDroppedOnceTheTargetIsCollected()
        {
            ReactivePropertyInt property = new(0);
            SubscribeWeaklyInSeparateScope(property);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            property.Value = 2;

            Assert.AreEqual(0, property.ListenerCount,
                "a weak subscription must unsubscribe itself once its target is collected");
        }

        [Test]
        public void WeakListener_KeepsWorkingWhileTheTargetIsReferenced()
        {
            ReactivePropertyInt property = new(0);
            Subscriber subscriber = new();
            property.AddWeakListener(subscriber.Handle);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            property.Value = 5;

            Assert.AreEqual(1, subscriber.Calls);
            Assert.AreEqual(5, subscriber.LastValue);
        }

        [Test]
        public void SubscribeWeakWithExplicitTarget_DeliversAndUnsubscribes()
        {
            ReactivePropertyInt property = new(0);
            Subscriber subscriber = new();

            IDisposable handle = property.SubscribeWeak(subscriber, static (target, value) => target.Handle(value));
            property.Value = 3;

            Assert.AreEqual(1, subscriber.Calls);
            Assert.AreEqual(3, subscriber.LastValue);

            handle.Dispose();
            property.Value = 4;
            Assert.AreEqual(1, subscriber.Calls, "disposing the handle must stop delivery");
        }

        [Test]
        public void DestroyedUnityTarget_IsUnsubscribedInsteadOfThrowing()
        {
            ReactivePropertyInt property = new(0);
            GameObject host = new(nameof(DestroyedUnityTarget_IsUnsubscribedInsteadOfThrowing));
            DestroyableSubscriber subscriber = host.AddComponent<DestroyableSubscriber>();
            property.AddListener(subscriber.Handle);

            property.Value = 1;
            Assert.AreEqual(1, subscriber.Calls);

            UnityEngine.Object.DestroyImmediate(host);

            // WHY: dropping the listener is reported as a throttled warning, which never fails a test, so no
            // LogAssert guard is needed here. Declaring an Expect would be flaky - the throttle can swallow it.
            Assert.DoesNotThrow(() => property.Value = 2);

            Assert.AreEqual(0, property.ListenerCount,
                "a listener owned by a destroyed object must be dropped automatically");
        }

        [Test]
        public void AddListener_IsIdempotentForTheSameDelegate()
        {
            ReactivePropertyInt property = new(0);
            int calls = 0;
            UnityAction<int> listener = _ => calls++;

            property.AddListener(listener);
            property.AddListener(listener);
            property.Value = 1;

            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, property.ListenerCount);
        }

        [Test]
        public void RemoveListener_RemovesWeakSubscriptionsToo()
        {
            ReactivePropertyInt property = new(0);
            Subscriber subscriber = new();

            property.AddWeakListener(subscriber.Handle);
            property.RemoveListener(subscriber.Handle);
            property.Value = 1;

            Assert.AreEqual(0, subscriber.Calls);
            Assert.AreEqual(0, property.ListenerCount);
        }

        [Test]
        public void RemoveAllListenersInsideCallback_DoesNotResurrectListeners()
        {
            ReactivePropertyInt property = new(0);
            int secondCalls = 0;

            property.AddListener(_ => property.RemoveAllListeners());
            property.AddListener(_ => secondCalls++);

            property.Value = 1;

            // The snapshot guarantees the second listener still receives the notification it was registered for.
            Assert.AreEqual(1, secondCalls);
            Assert.AreEqual(0, property.ListenerCount);
        }

        [Test]
        public void SetValueWithoutNotify_DoesNotNotifyButUpdatesValue()
        {
            ReactivePropertyInt property = new(0);
            int calls = 0;
            property.AddListener(_ => calls++);

            property.SetValueWithoutNotify(9);

            Assert.AreEqual(9, property.CurrentValue);
            Assert.AreEqual(0, calls);
        }
    }
}
