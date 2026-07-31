using Neo.Reactive;
using Neo.Network;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ReactivePropertyTests
    {
        private sealed class TestPayload
        {
            public int Count;
        }

        [Test]
        public void ReactivePropertyInt_Initialization_SetsValue()
        {
            var prop = new ReactivePropertyInt(5);
            Assert.AreEqual(5, prop.CurrentValue);
        }

        [Test]
        public void ReactivePropertyInt_SetValue_FiresEvent()
        {
            var prop = new ReactivePropertyInt(0);
            int eventFiredCount = 0;
            int lastReceivedValue = -1;

            prop.AddListener(val =>
            {
                eventFiredCount++;
                lastReceivedValue = val;
            });

            prop.Value = 10;

            Assert.AreEqual(1, eventFiredCount);
            Assert.AreEqual(10, lastReceivedValue);

            // WHY: Setting the same value again should not fire
            prop.Value = 10;
            Assert.AreEqual(1, eventFiredCount);
        }

        [Test]
        public void ReactivePropertyInt_OnNext_FiresEvent()
        {
            var prop = new ReactivePropertyInt(0);
            bool fired = false;
            prop.AddListener(val => fired = true);

            prop.OnNext(5);

            Assert.IsTrue(fired);
            Assert.AreEqual(5, prop.CurrentValue);
        }

        [Test]
        public void ReactivePropertyInt_SetValueWithoutNotify_DoesNotFire()
        {
            var prop = new ReactivePropertyInt(0);
            bool fired = false;
            prop.AddListener(val => fired = true);

            prop.SetValueWithoutNotify(100);

            Assert.IsFalse(fired);
            Assert.AreEqual(100, prop.CurrentValue);
        }

        [Test]
        public void ReactivePropertyInt_ForceNotify_FiresWithCurrentValue()
        {
            var prop = new ReactivePropertyInt(5);
            int lastVal = 0;
            prop.AddListener(val => lastVal = val);

            prop.ForceNotify();

            Assert.AreEqual(5, lastVal);
        }

        [Test]
        public void ReactivePropertyFloat_WorksLikeInt()
        {
            var prop = new ReactivePropertyFloat(2.5f);
            float lastVal = 0f;
            prop.AddListener(val => lastVal = val);

            prop.Value = 1.0f;
            Assert.AreEqual(1.0f, lastVal);
        }

        [Test]
        public void ReactivePropertyGeneric_SupportsReferenceTypes()
        {
            var initial = new TestPayload { Count = 1 };
            var next = new TestPayload { Count = 2 };
            var prop = new ReactiveProperty<TestPayload>(initial);
            TestPayload received = null;

            prop.AddListener(value => received = value);
            prop.Value = next;

            Assert.AreSame(next, prop.CurrentValue);
            Assert.AreSame(next, received);
        }

        [Test]
        public void ReactivePropertyGeneric_SupportsValueTypes()
        {
            var prop = new ReactiveProperty<double>(1.5d);
            double received = 0d;

            prop.AddListener(value => received = value);
            prop.OnNext(2.5d);

            Assert.AreEqual(2.5d, prop.CurrentValue);
            Assert.AreEqual(2.5d, received);
        }

        [Test]
        public void NetworkReactivePropertyBridge_GenericOverload_UpdatesAndNotifies()
        {
            var prop = new ReactiveProperty<string>("local");
            string received = null;

            prop.AddListener(value => received = value);
            NetworkReactivePropertyBridge.SetFromNetwork(prop, "server");

            Assert.AreEqual("server", prop.CurrentValue);
            Assert.AreEqual("server", received);
        }
    }
}
