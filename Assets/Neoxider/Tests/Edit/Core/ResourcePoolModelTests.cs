using Neo.Core.Resources;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Regression tests for the pure <see cref="ResourcePoolModel" /> logic (regen remainder,
    ///     clamp headroom, spend/deplete events) that HealthComponent only exercises indirectly.
    /// </summary>
    [TestFixture]
    public class ResourcePoolModelTests
    {
        private static ResourcePoolModel WithPool(ResourcePoolEntry entry, string id = "HP")
        {
            var model = new ResourcePoolModel();
            model.AddPool(id, entry);
            return model;
        }

        [Test]
        public void Increase_ClampsToMaxHeadroom()
        {
            var model = WithPool(new ResourcePoolEntry
            {
                Current = 90f, Max = 100f, MaxIncreaseAmount = -1f, MaxDecreaseAmount = -1f
            });

            float applied = model.Increase("HP", 50f);

            Assert.AreEqual(10f, applied, 1e-4f);
            Assert.AreEqual(100f, model.GetCurrent("HP"), 1e-4f);
        }

        [Test]
        public void TrySpend_NotEnough_FailsAndKeepsValue()
        {
            var model = WithPool(new ResourcePoolEntry
            {
                Current = 30f, Max = 100f, MaxIncreaseAmount = -1f, MaxDecreaseAmount = -1f
            });

            bool ok = model.TrySpend("HP", 50f, out string reason);

            Assert.IsFalse(ok);
            Assert.IsNotNull(reason);
            Assert.AreEqual(30f, model.GetCurrent("HP"), 1e-4f);
        }

        [Test]
        public void Decrease_ToZero_RaisesDepletedOnce()
        {
            var model = WithPool(new ResourcePoolEntry
            {
                Current = 20f, Max = 100f, MaxIncreaseAmount = -1f, MaxDecreaseAmount = -1f
            });

            int depleted = 0;
            model.OnResourceDepleted += _ => depleted++;

            model.Decrease("HP", 20f);
            Assert.AreEqual(0f, model.GetCurrent("HP"), 1e-4f);
            Assert.AreEqual(1, depleted);

            // WHY: already-depleted pool must not re-raise the event on further decreases.
            model.Decrease("HP", 5f);
            Assert.AreEqual(1, depleted);
        }

        [Test]
        public void Tick_KeepsRegenRemainder_AcrossLongFrame()
        {
            var model = WithPool(new ResourcePoolEntry
            {
                Current = 0f, Max = 100f, MaxIncreaseAmount = -1f, MaxDecreaseAmount = -1f,
                RegenPerSecond = 10f, RegenInterval = 1f, IgnoreCanHeal = true
            });

            // WHY: a 2.5s hitch applies 2 whole intervals (20) and keeps 0.5s of remainder,
            // so the next 0.5s tick completes the third interval instead of losing the time.
            model.Tick(2.5f);
            Assert.AreEqual(20f, model.GetCurrent("HP"), 1e-3f);

            model.Tick(0.5f);
            Assert.AreEqual(30f, model.GetCurrent("HP"), 1e-3f);
        }

        [Test]
        public void Increase_OnDepletedPool_BlockedUnlessIgnoreCanHeal()
        {
            var blocked = WithPool(new ResourcePoolEntry
            {
                Current = 0f, Max = 100f, MaxIncreaseAmount = -1f, MaxDecreaseAmount = -1f
            });
            Assert.AreEqual(0f, blocked.Increase("HP", 25f), 1e-4f);

            var healable = WithPool(new ResourcePoolEntry
            {
                Current = 0f, Max = 100f, MaxIncreaseAmount = -1f, MaxDecreaseAmount = -1f,
                IgnoreCanHeal = true
            });
            Assert.AreEqual(25f, healable.Increase("HP", 25f), 1e-4f);
        }
    }
}
