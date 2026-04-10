using Neo.Reactive;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ReactivePropertyTests
    {
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

            // Setting the same value again should not fire
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
    }
}
