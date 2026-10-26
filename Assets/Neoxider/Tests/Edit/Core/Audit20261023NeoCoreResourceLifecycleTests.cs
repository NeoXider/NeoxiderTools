using System;
using System.Collections.Generic;
using Neo.Core.Resources;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoCoreResourceLifecycleTests
    {
        [Test]
        public void Decrease_NestedDecrease_EmitsOneDepletionAndReturnsOwnAmount()
        {
            List<string> events = new List<string>();
            ResourcePoolModel model = CreateNestedModel(events);

            float removed = model.Decrease("HP", 1f);

            Assert.That(removed, Is.EqualTo(1f));
            Assert.That(model.GetCurrent("HP"), Is.Zero);
            CollectionAssert.AreEqual(new string[] { "Changed", "Changed", "Depleted" }, events);

            Assert.That(model.Decrease("HP", 1f), Is.Zero);
            Assert.That(model.TrySpend("HP", 1f, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("Not enough resource."));
            CollectionAssert.AreEqual(new string[] { "Changed", "Changed", "Depleted", "Changed" }, events);
        }

        [Test]
        public void TrySpend_NestedDecrease_EmitsOneDepletionAndReturnsOwnSuccess()
        {
            List<string> events = new List<string>();
            ResourcePoolModel model = CreateNestedModel(events);

            bool spent = model.TrySpend("HP", 1f, out string reason);

            Assert.That(spent, Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(model.GetCurrent("HP"), Is.Zero);
            CollectionAssert.AreEqual(new string[] { "Changed", "Changed", "Depleted" }, events);

            Assert.That(model.Decrease("HP", 1f), Is.Zero);
            Assert.That(model.TrySpend("HP", 1f, out reason), Is.False);
            Assert.That(reason, Is.EqualTo("Not enough resource."));
            CollectionAssert.AreEqual(new string[] { "Changed", "Changed", "Depleted", "Changed" }, events);
        }

        [Test]
        public void Decrease_BelowStoredPrecision_ReturnsZeroAndRetainsChangedNotification()
        {
            ResourcePoolModel model = CreatePrecisionModel();
            int changed = 0;
            int depleted = 0;
            float notifiedCurrent = 0f;
            model.OnResourceChanged += (id, current, max) =>
            {
                changed++;
                notifiedCurrent = current;
            };
            model.OnResourceDepleted += id => depleted++;

            float removed = model.Decrease("HP", 1f);

            Assert.That(removed, Is.Zero);
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(100_000_000f));
            Assert.That(notifiedCurrent, Is.EqualTo(100_000_000f));
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(depleted, Is.Zero);
        }

        [TestCase(5f, -1f)]
        [TestCase(9f, -1f)]
        [TestCase(20f, 5f)]
        public void Decrease_ReturnsActualStoredDeltaAfterRoundingAndLimit(float amount, float limit)
        {
            ResourcePoolModel model = CreatePrecisionModel(limit);
            int changed = 0;
            int depleted = 0;
            model.OnResourceChanged += (id, current, max) => changed++;
            model.OnResourceDepleted += id => depleted++;

            float removed = model.Decrease("HP", amount);

            Assert.That(removed, Is.EqualTo(8f));
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(99_999_992f));
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(depleted, Is.Zero);
        }

        [Test]
        public void Decrease_ChangedListenerReplacesBalance_ReturnsItsCapturedStoredDelta()
        {
            ResourcePoolModel model = CreatePrecisionModel();
            List<float> changedValues = new List<float>();
            int depleted = 0;
            model.OnResourceChanged += (id, current, max) =>
            {
                changedValues.Add(current);
                if (changedValues.Count == 1)
                {
                    model.SetCurrent(id, 40f);
                }
            };
            model.OnResourceDepleted += id => depleted++;

            float removed = model.Decrease("HP", 5f);

            Assert.That(removed, Is.EqualTo(8f));
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(40f));
            CollectionAssert.AreEqual(new float[] { 99_999_992f, 40f }, changedValues);
            Assert.That(depleted, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Depletion_ChangedListenerRevivesPool_StillReportsTheCommittedTransition(bool spend)
        {
            ResourcePoolModel model = CreateModel();
            List<string> events = new List<string>();
            model.OnResourceChanged += (id, current, max) =>
            {
                events.Add("Changed");
                if (current == 0f)
                {
                    model.Increase(id, 5f);
                }
            };
            model.OnResourceDepleted += id => events.Add("Depleted");

            if (spend)
            {
                Assert.That(model.TrySpend("HP", 10f, out string reason), Is.True);
                Assert.That(reason, Is.Null);
            }
            else
            {
                Assert.That(model.Decrease("HP", 10f), Is.EqualTo(10f));
            }

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(5f));
            CollectionAssert.AreEqual(new string[] { "Changed", "Changed", "Depleted" }, events);
        }

        [Test]
        public void Tick_ThrowingChangedListener_CleansGuardAndSnapshotForNextTick()
        {
            ResourcePoolModel model = CreateModel();
            int changed = 0;
            model.OnResourceChanged += (id, current, max) =>
            {
                changed++;
                if (changed == 1)
                {
                    throw new InvalidOperationException("Expected listener failure.");
                }
            };

            Assert.Throws<InvalidOperationException>(() => model.Tick(1f));
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(20f));

            model.Tick(1f);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(30f));
            Assert.That(changed, Is.EqualTo(2));
        }

        private static ResourcePoolModel CreateNestedModel(List<string> events)
        {
            ResourcePoolModel model = CreateModel();
            model.OnResourceChanged += (id, current, max) =>
            {
                events.Add("Changed");
                if (current == 9f)
                {
                    model.Decrease(id, 9f);
                }
            };
            model.OnResourceDepleted += id => events.Add("Depleted");
            return model;
        }

        private static ResourcePoolModel CreatePrecisionModel(float decreaseLimit = -1f)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", new ResourcePoolEntry
            {
                Current = 100_000_000f,
                Max = 0f,
                MaxDecreaseAmount = decreaseLimit,
                MaxIncreaseAmount = -1f,
                IgnoreCanHeal = true
            });
            return model;
        }

        private static ResourcePoolModel CreateModel()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", new ResourcePoolEntry
            {
                Current = 10f,
                Max = 100f,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f,
                IgnoreCanHeal = true,
                RegenPerSecond = 10f,
                RegenInterval = 1f
            });
            return model;
        }
    }
}
