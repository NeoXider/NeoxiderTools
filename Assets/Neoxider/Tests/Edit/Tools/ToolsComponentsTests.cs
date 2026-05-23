using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ToolsComponentsTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ComponentsTestObject");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void Counter_AddSubtract_UpdatesValueCorrectly()
        {
            Counter counter = _go.AddComponent<Counter>();

            // Default is CounterValueMode.Int.
            counter.Value.Value = 10;

            counter.Add(5);
            Assert.AreEqual(15, counter.Value.Value, "Addition failed");

            counter.Subtract(3);
            Assert.AreEqual(12, counter.Value.Value, "Subtraction failed");
        }

        [Test]
        public void Counter_MultiplyDivide_UpdatesValueCorrectly()
        {
            Counter counter = _go.AddComponent<Counter>();
            counter.Value.Value = 10;

            counter.Multiply(2);
            Assert.AreEqual(20, counter.Value.Value, "Multiplication failed");

            counter.Divide(4);
            Assert.AreEqual(5, counter.Value.Value, "Division failed");
        }

        [Test]
        public void UnityLifecycleEvents_AwakeStartEnableDisable_FiresEvents()
        {
            UnityLifecycleEvents lifecycle = _go.AddComponent<UnityLifecycleEvents>();

            bool startFired = false;
            bool enableFired = false;
            bool disableFired = false;

            lifecycle.OnStart.AddListener(() => startFired = true);
            lifecycle.OnEnableEvent.AddListener(() => enableFired = true);
            lifecycle.OnDisableEvent.AddListener(() => disableFired = true);

            // Use reflection to invoke methods since Unity EditMode doesn't automatically trigger all lifecycle callbacks like PlayMode
            typeof(UnityLifecycleEvents)
                .GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(lifecycle, null);
            typeof(UnityLifecycleEvents).GetMethod("OnEnable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(lifecycle, null);
            typeof(UnityLifecycleEvents).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(lifecycle, null);

            Assert.IsTrue(startFired, "OnStart should have fired");
            Assert.IsTrue(enableFired, "OnEnableEvent should have fired");
            Assert.IsTrue(disableFired, "OnDisableEvent should have fired");
        }

        [Test]
        public void RandomRange_GetRandom_ReturnsWithinRange()
        {
            RandomRange randomRange = _go.AddComponent<RandomRange>();

            randomRange.Min = 5f;
            randomRange.Max = 10f;

            for (int i = 0; i < 50; i++)
            {
                randomRange.Generate();
                float val = randomRange.ValueFloat;
                Assert.IsTrue(val >= 5f && val <= 10f, $"Returned {val} which is outside [5, 10]");
            }
        }
    }
}
