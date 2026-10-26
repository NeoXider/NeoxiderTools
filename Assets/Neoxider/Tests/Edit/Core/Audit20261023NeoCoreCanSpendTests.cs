using Neo.Core.Resources;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoCoreCanSpendTests
    {
        [Test]
        public void CanSpend_AboveDecreaseLimit_FailsWithoutChangingBalanceOrEvents()
        {
            ResourcePoolModel model = CreateModel(20f, 4f);
            int changed = 0;
            int depleted = 0;
            model.OnResourceChanged += (id, current, max) => changed++;
            model.OnResourceDepleted += id => depleted++;

            bool canSpend = model.CanSpend("mana", 10f, out string reason);

            Assert.That(canSpend, Is.False);
            Assert.That(reason, Is.EqualTo("Amount exceeds decrease limit."));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(20f));
            Assert.That(model.GetMax("Mana"), Is.EqualTo(100f));
            Assert.That(changed, Is.Zero);
            Assert.That(depleted, Is.Zero);
        }

        [Test]
        public void CanSpend_AffordableCost_PreservesBalanceAndBothTimerRemainders()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("Mana", new ResourcePoolEntry
            {
                Current = 20f,
                Max = 100f,
                MaxDecreaseAmount = 4f,
                MaxIncreaseAmount = -1f,
                RegenPerSecond = 2f,
                RegenInterval = 1f,
                HealAmount = 3f,
                HealDelay = 2f
            });
            model.Tick(0.5f);
            int changed = 0;
            int depleted = 0;
            model.OnResourceChanged += (id, current, max) => changed++;
            model.OnResourceDepleted += id => depleted++;

            Assert.That(model.CanSpend("MANA", 4f, out string reason), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(model.CanSpend("Mana", 4f, out reason), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(20f));
            Assert.That(model.GetMax("Mana"), Is.EqualTo(100f));
            Assert.That(changed, Is.Zero);
            Assert.That(depleted, Is.Zero);

            model.Tick(0.5f);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(22f));
            Assert.That(changed, Is.EqualTo(1));
            model.Tick(1f);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(27f));
            Assert.That(changed, Is.EqualTo(3));
            Assert.That(depleted, Is.Zero);
        }

        [Test]
        public void CanSpend_ZeroEffectiveCost_MatchesTrySpendWithoutChangingStateEventsOrTimers()
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("Mana", new ResourcePoolEntry
            {
                Current = 100_000_000f,
                Max = 0f,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f,
                RegenPerSecond = 8f,
                RegenInterval = 1f,
                HealAmount = 8f,
                HealDelay = 2f
            });
            model.Tick(0.5f);
            int changed = 0;
            int depleted = 0;
            model.OnResourceChanged += (id, current, max) => changed++;
            model.OnResourceDepleted += id => depleted++;

            Assert.That(model.CanSpend("mana", 1f, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("Amount does not decrease resource."));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(100_000_000f));
            Assert.That(changed, Is.Zero);
            Assert.That(depleted, Is.Zero);
            Assert.That(model.TrySpend("Mana", 1f, out string spendReason), Is.False);
            Assert.That(spendReason, Is.EqualTo(reason));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(100_000_000f));
            Assert.That(model.GetMax("Mana"), Is.Zero);
            Assert.That(changed, Is.Zero);
            Assert.That(depleted, Is.Zero);

            model.Tick(0.5f);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(100_000_008f));
            Assert.That(changed, Is.EqualTo(1));
            model.Tick(1f);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(100_000_024f));
            Assert.That(changed, Is.EqualTo(3));
            Assert.That(depleted, Is.Zero);
        }

        [Test]
        public void CanSpend_ZeroEffectiveCostAboveDecreaseLimit_PreservesLimitFailurePrecedence()
        {
            ResourcePoolModel model = CreateModel(current: 100_000_000f, limit: 0f, max: 0f);
            int events = 0;
            model.OnResourceChanged += (id, current, max) => events++;
            model.OnResourceDepleted += id => events++;

            Assert.That(model.CanSpend("Mana", 1f, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("Amount exceeds decrease limit."));
            Assert.That(model.TrySpend("Mana", 1f, out string spendReason), Is.False);
            Assert.That(spendReason, Is.EqualTo(reason));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(100_000_000f));
            Assert.That(events, Is.Zero);
        }

        [TestCase(1f, 0.1f, 0.9f)]
        [TestCase(100_000_000f, 5f, 99_999_992f)]
        public void CanSpend_EffectiveRoundedCosts_PreserveImmediateTrySpendSuccess(
            float current, float amount, float expectedCurrent)
        {
            ResourcePoolModel model = CreateModel(current: current, max: 0f);
            int changed = 0;
            int depleted = 0;
            model.OnResourceChanged += (id, value, max) => changed++;
            model.OnResourceDepleted += id => depleted++;

            Assert.That(model.CanSpend("Mana", amount, out string reason), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(current));
            Assert.That(changed, Is.Zero);
            Assert.That(depleted, Is.Zero);

            Assert.That(model.TrySpend("Mana", amount, out string spendReason), Is.True);
            Assert.That(spendReason, Is.Null);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(expectedCurrent));
            Assert.That(current - model.GetCurrent("Mana"), Is.Not.EqualTo(amount));
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(depleted, Is.Zero);
        }

        [TestCase(20f, -1f, 10f, true, null, 10f)]
        [TestCase(20f, 4f, 10f, false, "Amount exceeds decrease limit.", 20f)]
        [TestCase(20f, 4f, 4f, true, null, 16f)]
        [TestCase(20f, 0f, 1f, false, "Amount exceeds decrease limit.", 20f)]
        [TestCase(3f, 4f, 10f, false, "Not enough resource.", 3f)]
        [TestCase(0f, -1f, 1f, false, "Not enough resource.", 0f)]
        [TestCase(20f, -1f, 20f, true, null, 0f)]
        [TestCase(20f, -1f, 0f, true, null, 20f)]
        [TestCase(20f, -1f, -5f, true, null, 20f)]
        public void CanSpend_MatchesImmediateTrySpendWithUnchangedValidationSemantics(
            float current, float limit, float amount, bool expected, string expectedReason, float expectedCurrent)
        {
            ResourcePoolModel model = CreateModel(current, limit);
            int changed = 0;
            int depleted = 0;
            model.OnResourceChanged += (id, value, max) => changed++;
            model.OnResourceDepleted += id => depleted++;

            bool canSpend = model.CanSpend("Mana", amount, out string preflightReason);

            Assert.That(canSpend, Is.EqualTo(expected));
            Assert.That(preflightReason, Is.EqualTo(expectedReason));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(current));
            Assert.That(changed, Is.Zero);
            Assert.That(depleted, Is.Zero);

            bool spent = model.TrySpend("Mana", amount, out string spendReason);

            Assert.That(spent, Is.EqualTo(canSpend));
            Assert.That(spendReason, Is.EqualTo(expectedReason));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(expectedCurrent));
            Assert.That(changed, Is.EqualTo(expected && amount > 0f ? 1 : 0));
            Assert.That(depleted, Is.EqualTo(current > 0f && expectedCurrent == 0f ? 1 : 0));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Unknown")]
        public void CanSpend_MissingPositiveCost_RejectsWithoutChangingOtherPools(string id)
        {
            ResourcePoolModel model = CreateModel();
            int events = 0;
            model.OnResourceChanged += (resourceId, current, max) => events++;
            model.OnResourceDepleted += resourceId => events++;

            Assert.That(model.CanSpend(id, 1f, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("Unknown resource."));
            Assert.That(model.TrySpend(id, 1f, out string spendReason), Is.False);
            Assert.That(spendReason, Is.EqualTo(reason));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(20f));
            Assert.That(events, Is.Zero);
        }

        [TestCase(null, 0f)]
        [TestCase("", -1f)]
        [TestCase("Unknown", 0f)]
        public void CanSpend_MissingNonpositiveCost_PreservesSuccessfulNoop(string id, float amount)
        {
            ResourcePoolModel model = CreateModel();
            int events = 0;
            model.OnResourceChanged += (resourceId, current, max) => events++;
            model.OnResourceDepleted += resourceId => events++;

            Assert.That(model.CanSpend(id, amount, out string reason), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(model.TrySpend(id, amount, out string spendReason), Is.True);
            Assert.That(spendReason, Is.Null);
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(20f));
            Assert.That(events, Is.Zero);
        }

        [TestCase("Mana", float.NaN)]
        [TestCase("Mana", float.PositiveInfinity)]
        [TestCase("Mana", float.NegativeInfinity)]
        [TestCase(null, float.NaN)]
        [TestCase(null, float.PositiveInfinity)]
        [TestCase(null, float.NegativeInfinity)]
        public void CanSpend_NonFiniteAmount_RejectsBeforeIdLookupWithoutEvents(string id, float amount)
        {
            ResourcePoolModel model = CreateModel();
            int events = 0;
            model.OnResourceChanged += (resourceId, current, max) => events++;
            model.OnResourceDepleted += resourceId => events++;

            Assert.That(model.CanSpend(id, amount, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("Amount must be finite."));
            Assert.That(model.TrySpend(id, amount, out string spendReason), Is.False);
            Assert.That(spendReason, Is.EqualTo(reason));
            Assert.That(model.GetCurrent("Mana"), Is.EqualTo(20f));
            Assert.That(events, Is.Zero);
        }

        private static ResourcePoolModel CreateModel(float current = 20f, float limit = -1f, float max = 100f)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("Mana", new ResourcePoolEntry
            {
                Current = current,
                Max = max,
                MaxDecreaseAmount = limit,
                MaxIncreaseAmount = -1f
            });
            return model;
        }
    }
}
