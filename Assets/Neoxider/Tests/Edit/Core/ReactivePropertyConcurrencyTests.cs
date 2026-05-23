using Neo.Reactive;
using NUnit.Framework;
using UnityEngine.Events;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Tests for the P0-3 ConcurrentModification fix in ReactiveProperty.
    ///     Verifies that listeners can safely add/remove other listeners during notification.
    /// </summary>
    [TestFixture]
    public class ReactivePropertyConcurrencyTests
    {
        [Test]
        public void AddListenerInsideCallback_DoesNotThrow()
        {
            var prop = new ReactivePropertyInt(0);
            int secondListenerCalls = 0;

            prop.AddListener(val =>
            {
                // Add another listener INSIDE the callback
                UnityAction<int> newListener = v => secondListenerCalls++;
                prop.AddListener(newListener);
            });

            // Should not throw ConcurrentModificationException
            Assert.DoesNotThrow(() => prop.Value = 1);
        }

        [Test]
        public void RemoveListenerInsideCallback_DoesNotThrow()
        {
            var prop = new ReactivePropertyInt(0);
            int callCount = 0;
            UnityAction<int> listener = null;

            listener = val =>
            {
                callCount++;
                prop.RemoveListener(listener);
            };

            prop.AddListener(listener);

            // Should not throw ConcurrentModificationException
            Assert.DoesNotThrow(() => prop.Value = 1);
            Assert.AreEqual(1, callCount);

            // Second set should NOT call removed listener
            prop.Value = 2;
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void MultipleListeners_AllReceiveNotification()
        {
            var prop = new ReactivePropertyInt(0);
            int a = 0, b = 0, c = 0;

            prop.AddListener(v => a = v);
            prop.AddListener(v => b = v);
            prop.AddListener(v => c = v);

            prop.Value = 42;

            Assert.AreEqual(42, a);
            Assert.AreEqual(42, b);
            Assert.AreEqual(42, c);
        }

        [Test]
        public void RemoveAllListenersInsideCallback_DoesNotThrow()
        {
            var prop = new ReactivePropertyInt(0);

            UnityAction<int> listenerA = null;
            UnityAction<int> listenerB = null;
            UnityAction<int> listenerC = null;

            listenerA = v => { prop.RemoveListener(listenerA); };
            listenerB = v => { prop.RemoveListener(listenerB); };
            listenerC = v => { prop.RemoveListener(listenerC); };

            prop.AddListener(listenerA);
            prop.AddListener(listenerB);
            prop.AddListener(listenerC);

            // The only guarantee: no ConcurrentModificationException
            Assert.DoesNotThrow(() => prop.Value = 5);
            Assert.DoesNotThrow(() => prop.Value = 10);
        }
    }
}
