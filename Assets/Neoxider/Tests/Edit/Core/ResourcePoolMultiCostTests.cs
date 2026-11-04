using System;
using System.Collections.Generic;
using Neo.Core.Resources;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="ResourcePoolModel.CanSpendAll(IReadOnlyList{ResourceCost},out string)" /> and
    ///     <see cref="ResourcePoolModel.TrySpendAll(IReadOnlyList{ResourceCost},out string)" />: a multi-resource cost
    ///     is validated as sequential payments and paid completely or not at all, with listeners running only after
    ///     the last payment.
    /// </summary>
    [TestFixture]
    public class ResourcePoolMultiCostTests
    {
        [Test]
        public void TrySpendAll_AffordableCosts_PaysEveryCostBeforeAnyListenerRuns()
        {
            ResourcePoolModel model = CreateModel(50f, 100f);
            List<string> events = new();
            model.OnResourceChanged += (id, current, max) =>
            {
                events.Add(id + " " + current + " (mana " + model.GetCurrent("mana") + ", hp " +
                           model.GetCurrent("hp") + ")");
            };
            List<ResourceCost> costs = new() { new ResourceCost("mana", 30f), new ResourceCost("hp", 10f) };

            bool paid = model.TrySpendAll(costs, out string failReason, out int failedIndex);

            Assert.That(paid, Is.True);
            Assert.That(failReason, Is.Null);
            Assert.That(failedIndex, Is.EqualTo(-1));
            CollectionAssert.AreEqual(new[] { "mana 20 (mana 20, hp 90)", "hp 90 (mana 20, hp 90)" }, events,
                "Each payment is announced in order with its own value, after both were applied.");
        }

        [Test]
        public void TrySpendAll_ListenerOfTheFirstPayment_CannotInvalidateTheSecond()
        {
            ResourcePoolModel model = CreateModel(20f, 10f);
            model.OnResourceChanged += (id, current, max) =>
            {
                if (id == "hp")
                {
                    model.Decrease("mana", 100f);
                }
            };
            ResourceCost[] costs = { new("hp", 5f), new("mana", 10f) };

            Assert.That(model.TrySpendAll(costs, out _), Is.True);

            Assert.That(model.GetCurrent("hp"), Is.EqualTo(5f));
            Assert.That(model.GetCurrent("mana"), Is.Zero,
                "Both costs were paid; the drain the hp listener started runs afterwards.");
        }

        [Test]
        public void SpendAll_CostsOnOnePool_AreCoveredByTheBalanceTheEarlierOnesLeave()
        {
            ResourcePoolModel model = CreateModel(50f, 100f);
            int changes = 0;
            int depletions = 0;
            model.OnResourceChanged += (id, current, max) => changes++;
            model.OnResourceDepleted += id => depletions++;
            ResourceCost[] tooMuch = { new("mana", 30f), new("MANA", 30f) };

            Assert.That(model.CanSpendAll(tooMuch, out string canReason, out int canIndex), Is.False);
            Assert.That(model.TrySpendAll(tooMuch, out string reason, out int index), Is.False);

            Assert.That(canReason, Is.EqualTo("Not enough resource."));
            Assert.That(reason, Is.EqualTo("Not enough resource."));
            Assert.That(canIndex, Is.EqualTo(1));
            Assert.That(index, Is.EqualTo(1), "Ids compare case-insensitively, so the second cost is short.");
            Assert.That(model.GetCurrent("mana"), Is.EqualTo(50f), "A failed list pays nothing.");
            Assert.That(changes, Is.Zero);

            ResourceCost[] exact = { new("mana", 30f), new("mana", 20f) };
            Assert.That(model.TrySpendAll(exact, out _), Is.True);

            Assert.That(model.GetCurrent("mana"), Is.Zero);
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(depletions, Is.EqualTo(1), "Only the payment that empties the pool reports depletion.");
        }

        [Test]
        public void SpendAll_DecreaseLimit_AppliesToEachCostOnItsOwn()
        {
            ResourcePoolModel model = new();
            ResourcePoolEntry mana = Pool(100f);
            mana.MaxDecreaseAmount = 40f;
            model.AddPool("mana", mana);

            Assert.That(model.TrySpendAll(new[] { new ResourceCost("mana", 30f), new ResourceCost("mana", 20f) },
                out _), Is.True, "Each cost stays within the per-operation limit, although together they exceed it.");
            Assert.That(model.GetCurrent("mana"), Is.EqualTo(50f));

            // WHY: 45 left after the first cost covers 41, so only the per-operation limit can reject it; the balance
            // check runs first, exactly as in TrySpend.
            Assert.That(model.TrySpendAll(new[] { new ResourceCost("mana", 5f), new ResourceCost("mana", 41f) },
                out string reason, out int index), Is.False);
            Assert.That(reason, Is.EqualTo("Amount exceeds decrease limit."));
            Assert.That(index, Is.EqualTo(1));
            Assert.That(model.GetCurrent("mana"), Is.EqualTo(50f));
        }

        [Test]
        public void SpendAll_UnknownOrNonFiniteCost_RejectsTheListWithoutPayingEarlierCosts()
        {
            ResourcePoolModel model = CreateModel(50f, 100f);

            Assert.That(model.TrySpendAll(new[] { new ResourceCost("mana", 10f), new ResourceCost("gold", 5f) },
                out string unknown, out int unknownIndex), Is.False);
            Assert.That(model.TrySpendAll(new[] { new ResourceCost("mana", 10f), new ResourceCost("hp", float.NaN) },
                out string nonFinite, out int nonFiniteIndex), Is.False);
            Assert.That(model.TrySpendAll(new[] { new ResourceCost(null, 1f) }, out string nullId), Is.False);

            Assert.That(unknown, Is.EqualTo("Unknown resource."));
            Assert.That(unknownIndex, Is.EqualTo(1));
            Assert.That(nonFinite, Is.EqualTo("Amount must be finite."));
            Assert.That(nonFiniteIndex, Is.EqualTo(1));
            Assert.That(nullId, Is.EqualTo("Unknown resource."));
            Assert.That(model.GetCurrent("mana"), Is.EqualTo(50f));
            Assert.That(model.GetCurrent("hp"), Is.EqualTo(100f));
        }

        [Test]
        public void SpendAll_EmptyListsAndNonpositiveCosts_SucceedWithoutAPool_AndNullThrows()
        {
            ResourcePoolModel model = CreateModel(50f, 100f);
            int changes = 0;
            model.OnResourceChanged += (id, current, max) => changes++;

            Assert.That(model.TrySpendAll(Array.Empty<ResourceCost>(), out string emptyReason), Is.True);
            Assert.That(emptyReason, Is.Null);
            Assert.That(model.TrySpendAll(new[] { new ResourceCost("gold", 0f), new ResourceCost("mana", -5f) },
                out _), Is.True);
            Assert.That(changes, Is.Zero);
            Assert.That(model.GetCurrent("mana"), Is.EqualTo(50f));
            Assert.Throws<ArgumentNullException>(() => model.CanSpendAll(null, out _));
            Assert.Throws<ArgumentNullException>(() => model.TrySpendAll(null, out _));
        }

        [Test]
        public void CanSpendAll_AgreesWithSequentialTrySpend_OnRandomLists()
        {
            System.Random random = new(20261104);
            List<ResourceCost> costs = new();
            string[] ids = { "mana", "hp", "Mana", "gold", "rage" };
            for (int round = 0; round < 2000; round++)
            {
                costs.Clear();
                int count = random.Next(0, 5);
                for (int i = 0; i < count; i++)
                {
                    float amount = random.Next(0, 12) == 0 ? -1f : (float)Math.Round(random.NextDouble() * 60d, 1);
                    costs.Add(new ResourceCost(ids[random.Next(ids.Length)], amount));
                }

                float limit = random.Next(0, 3) == 0 ? 25f : -1f;
                float rage = random.Next(0, 4) == 0 ? 1e8f : 90f;
                ResourcePoolModel validated = CreateRandomModel(limit, rage);
                ResourcePoolModel sequential = CreateRandomModel(limit, rage);

                bool expected = true;
                for (int i = 0; i < costs.Count && expected; i++)
                {
                    expected = sequential.TrySpend(costs[i].ResourceId, costs[i].Amount, out _);
                }

                Assert.That(validated.CanSpendAll(costs, out _), Is.EqualTo(expected),
                    "Round " + round + ": " + string.Join(", ", costs));
                Assert.That(validated.TrySpendAll(costs, out _), Is.EqualTo(expected));
                if (expected)
                {
                    Assert.That(validated.GetCurrent("mana"), Is.EqualTo(sequential.GetCurrent("mana")));
                    Assert.That(validated.GetCurrent("hp"), Is.EqualTo(sequential.GetCurrent("hp")));
                    Assert.That(validated.GetCurrent("rage"), Is.EqualTo(sequential.GetCurrent("rage")));
                }
            }
        }

        [Test]
        public void TrySpendAll_WarmReusedCostList_DoesNotAllocate()
        {
            ResourcePoolModel model = CreateModel(1e6f, 1e6f);
            int changes = 0;
            model.OnResourceChanged += (id, current, max) => changes++;
            List<ResourceCost> costs = new() { new ResourceCost("mana", 1f), new ResourceCost("hp", 1f) };
            bool allPaid = model.TrySpendAll(costs, out _);
            Action payments = () =>
            {
                for (int i = 0; i < 500; i++)
                {
                    allPaid &= model.TrySpendAll(costs, out _);
                }
            };

            GcAlloc.AssertNone(payments, "Validation, payment and the batched notifications reuse their storage.");

            Assert.That(allPaid, Is.True);
            Assert.That(changes, Is.EqualTo(2 * 501));
        }

        [Test]
        public void HealthComponent_SpendAll_ForwardsToTheModelAndRefreshesTheReactiveMirrors()
        {
            GameObject host = new("ResourcePoolMultiCostTests");
            try
            {
                HealthComponent health = host.AddComponent<HealthComponent>();
                health.EnsureInitialized();
                ResourceCost[] costs = { new(RpgResourceId.Mana, 20f), new(RpgResourceId.Hp, 5f) };

                Assert.That(health.CanSpend(RpgResourceId.Mana, 51f, out string tooMuch), Is.False);
                Assert.That(tooMuch, Is.EqualTo("Not enough resource."));
                Assert.That(health.CanSpendAll(costs, out _), Is.True);
                Assert.That(health.TrySpendAll(costs, out _), Is.True);

                Assert.That(health.GetCurrent(RpgResourceId.Mana), Is.EqualTo(30f));
                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(95f));
                Assert.That(health.ManaCurrentValue, Is.EqualTo(30f));
                Assert.That(health.HpCurrentValue, Is.EqualTo(95f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static ResourcePoolModel CreateModel(float mana, float hp)
        {
            ResourcePoolModel model = new();
            model.AddPool("mana", Pool(mana));
            model.AddPool("hp", Pool(hp));
            return model;
        }

        private static ResourcePoolModel CreateRandomModel(float manaLimit, float rage)
        {
            ResourcePoolModel model = CreateModel(50f, 100f);
            ResourcePoolEntry mana = Pool(50f);
            mana.MaxDecreaseAmount = manaLimit;
            model.AddPool("mana", mana);
            model.AddPool("rage", Pool(rage));
            return model;
        }

        private static ResourcePoolEntry Pool(float value)
        {
            return new ResourcePoolEntry
            {
                Current = value,
                Max = value,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f
            };
        }
    }
}
