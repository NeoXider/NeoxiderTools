using System.Collections.Generic;
using Neo.Core.Resources;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoCoreResourceTests
    {
        [TestCase(0f)]
        [TestCase(4f)]
        public void TrySpend_AboveDecreaseLimit_FailsWithoutPartialPayment(float limit)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            ResourcePoolEntry entry = CreateEntry(20f);
            entry.MaxDecreaseAmount = limit;
            model.AddPool("Mana", entry);
            int changed = 0;
            model.OnResourceChanged += (id, current, max) => changed++;

            bool spent = model.TrySpend("mana", 10f, out string reason);

            Assert.That(spent, Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(20f));
            Assert.That(changed, Is.Zero);
        }

        [Test]
        public void TrySpend_WithinDecreaseLimit_PaysEntireCost()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            ResourcePoolEntry entry = CreateEntry(20f);
            entry.MaxDecreaseAmount = 4f;
            model.AddPool("Mana", entry);

            Assert.That(model.TrySpend("MANA", 4f, out string reason), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(16f));
        }

        [TestCase(null)]
        [TestCase("")]
        public void MissingId_UsesUnknownResourceResultsAndPreservesOtherPools(string missingId)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", CreateEntry(20f));

            Assert.That(model.GetCurrent(missingId), Is.Zero);
            Assert.That(model.GetMax(missingId), Is.Zero);
            Assert.That(model.IsDepleted(missingId), Is.True);
            Assert.That(model.TrySpend(missingId, 5f, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("Unknown resource."));
            Assert.That(model.Increase(missingId, 5f), Is.Zero);
            Assert.That(model.Decrease(missingId, 5f), Is.Zero);
            model.RemovePool(missingId);
            model.SetCurrent(missingId, 5f);
            model.SetMax(missingId, 5f);
            model.Restore(missingId);
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(20f));
            Assert.That(model.GetMax("HP"), Is.EqualTo(100f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteInput_DoesNotPoisonStateOrEmitChanges(float input)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", CreateEntry(20f));
            int changed = 0;
            model.OnResourceChanged += (id, current, max) => changed++;

            Assert.That(model.TrySpend("HP", input, out string reason), Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);
            Assert.That(model.Increase("HP", input), Is.Zero);
            Assert.That(model.Decrease("HP", input), Is.Zero);
            model.SetCurrent("HP", input);
            model.SetMax("HP", input);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(20f));
            Assert.That(model.GetMax("HP"), Is.EqualTo(100f));
            Assert.That(changed, Is.Zero);
        }

        [Test]
        public void AddPool_NormalizesInitialBounds()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", CreateEntry(120f));
            model.AddPool("Mana", CreateEntry(-10f));
            model.AddPool("Invalid", CreateEntry(float.NaN));

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(100f));
            Assert.That(model.GetCurrent("Mana"), Is.Zero);
            Assert.That(model.Decrease("Mana", 5f), Is.Zero);
            Assert.That(model.GetCurrent("Invalid"), Is.Zero);
        }

        [Test]
        public void SetMax_Zero_PreservesAnUncappedPool()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("Energy", CreateEntry(20f));

            model.SetMax("Energy", 0f);

            Assert.That(model.GetCurrent("Energy"), Is.EqualTo(20f));
            Assert.That(model.Increase("Energy", 5f), Is.EqualTo(5f));
            model.SetCurrent("Energy", 200f);
            Assert.That(model.GetCurrent("Energy"), Is.EqualTo(200f));
        }

        [Test]
        public void Increase_ReturnsItsOwnDeltaWhenAListenerSpendsResource()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", CreateEntry(50f));
            bool spent = false;
            model.OnResourceChanged += (id, current, max) =>
            {
                if (!spent)
                {
                    spent = true;
                    model.Decrease(id, 5f);
                }
            };

            float healed = model.Increase("HP", 20f);

            Assert.That(healed, Is.EqualTo(20f));
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(65f));
        }

        [Test]
        public void Tick_LongFrame_AppliesIncreaseLimitPerScheduledCycle()
        {
            ResourcePoolModel longFrame = CreateScheduledModel();
            ResourcePoolModel shortFrames = CreateScheduledModel();

            longFrame.Tick(3.5f);
            shortFrames.Tick(1f);
            shortFrames.Tick(1f);
            shortFrames.Tick(1f);
            shortFrames.Tick(0.5f);

            Assert.That(longFrame.GetCurrent("HP"), Is.EqualTo(25f));
            Assert.That(shortFrames.GetCurrent("HP"), Is.EqualTo(25f));
            longFrame.Tick(0.5f);
            shortFrames.Tick(0.5f);
            Assert.That(longFrame.GetCurrent("HP"), Is.EqualTo(30f));
            Assert.That(shortFrames.GetCurrent("HP"), Is.EqualTo(30f));
        }

        [TestCase(-0.5f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Tick_InvalidDelta_DoesNotConsumeFutureRegen(float delta)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            ResourcePoolEntry entry = CreateEntry(10f);
            entry.RegenPerSecond = 10f;
            entry.RegenInterval = 1f;
            model.AddPool("HP", entry);

            model.Tick(delta);
            model.Tick(1f);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(20f));
        }

        [Test]
        public void Tick_MoreThanIntegerMaxCycles_StillAppliesElapsedRegen()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            ResourcePoolEntry entry = CreateEntry(10f);
            entry.RegenPerSecond = 1f;
            entry.RegenInterval = 0.000000001f;
            model.AddPool("HP", entry);

            model.Tick(3f);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(13f).Within(0.0001f));
        }

        [Test]
        public void Tick_ReplacingPoolDuringRegen_DoesNotApplyRemovedPoolsHeal()
        {
            ResourcePoolModel model = CreateScheduledModel();
            bool replaced = false;
            model.OnResourceChanged += (id, current, max) =>
            {
                if (!replaced)
                {
                    replaced = true;
                    model.RemovePool(id);
                    model.AddPool(id, CreateEntry(7f));
                }
            };

            model.Tick(1f);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(7f));
            model.Tick(1f);
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(7f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_ReRegisterSameEntryDuringRegen_StartsNewRegistrationNextTick(bool removeFirst)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            ResourcePoolEntry entry = CreateReRegisterEntry();
            model.AddPool("HP", entry);
            int changed = 0;
            model.OnResourceChanged += (id, current, max) =>
            {
                changed++;
                if (changed == 1)
                {
                    if (removeFirst)
                    {
                        model.RemovePool("hp");
                    }

                    entry.Current = 7f;
                    model.AddPool("hp", entry);
                }
            };

            model.Tick(1f);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(7f));
            Assert.That(changed, Is.EqualTo(1));
            model.Tick(0.5f);
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(7f));
            Assert.That(changed, Is.EqualTo(1));
            model.Tick(0.5f);
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(10f));
            Assert.That(changed, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_ReRegisterAnotherSnapshotEntry_SkipsItsOldRegistration(bool removeFirst)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            ResourcePoolEntry hp = CreateReRegisterEntry();
            ResourcePoolEntry mana = CreateReRegisterEntry();
            model.AddPool("HP", hp);
            model.AddPool("Mana", mana);
            string firstId = null;
            string replacedId = null;
            List<string> events = new List<string>();
            model.OnResourceChanged += (id, current, max) =>
            {
                events.Add(id);
                if (replacedId != null)
                {
                    return;
                }

                // Choose the other pool at runtime; dictionary enumeration order is irrelevant.
                firstId = id;
                replacedId = id == "HP" ? "Mana" : "HP";
                ResourcePoolEntry replacement = replacedId == "HP" ? hp : mana;
                if (removeFirst)
                {
                    model.RemovePool(replacedId.ToUpperInvariant());
                }

                replacement.Current = 7f;
                model.AddPool(replacedId.ToLowerInvariant(), replacement);
            };

            model.Tick(1f);

            Assert.That(replacedId, Is.Not.Null);
            Assert.That(model.GetCurrent(replacedId), Is.EqualTo(7f));
            Assert.That(model.GetCurrent(firstId), Is.EqualTo(13f));
            CollectionAssert.AreEqual(new string[] { firstId, firstId }, events);
            model.Tick(0.5f);
            Assert.That(model.GetCurrent(replacedId), Is.EqualTo(7f));
            Assert.That(events.Count, Is.EqualTo(2));
            model.Tick(0.5f);
            Assert.That(model.GetCurrent(replacedId), Is.EqualTo(10f));
            Assert.That(model.GetCurrent(firstId), Is.EqualTo(16f));
            Assert.That(events.Count, Is.EqualTo(6));
        }

        [Test]
        public void Tick_FromChangedCallback_DoesNotAdvanceTimersTwice()
        {
            ResourcePoolModel model = CreateScheduledModel();
            bool nested = false;
            model.OnResourceChanged += (id, current, max) =>
            {
                if (!nested)
                {
                    nested = true;
                    model.Tick(1f);
                }
            };

            model.Tick(1f);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(15f));
        }

        private static ResourcePoolEntry CreateEntry(float current)
        {
            return new ResourcePoolEntry
            {
                Current = current,
                Max = 100f,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f,
                IgnoreCanHeal = true
            };
        }

        private static ResourcePoolEntry CreateReRegisterEntry()
        {
            ResourcePoolEntry entry = CreateEntry(10f);
            entry.RegenPerSecond = 1f;
            entry.RegenInterval = 1f;
            entry.HealAmount = 2f;
            entry.HealDelay = 1f;
            return entry;
        }

        private static ResourcePoolModel CreateScheduledModel()
        {
            ResourcePoolEntry entry = CreateEntry(10f);
            entry.MaxIncreaseAmount = 3f;
            entry.RegenPerSecond = 10f;
            entry.RegenInterval = 1f;
            entry.HealAmount = 2f;
            entry.HealDelay = 1f;
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", entry);
            return model;
        }
    }
}
