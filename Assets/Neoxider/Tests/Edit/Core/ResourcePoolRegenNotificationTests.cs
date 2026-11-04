using Neo.Core.Resources;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Regen and timed heal on a full pool must not report changes that did not happen.
    /// </summary>
    [TestFixture]
    public class ResourcePoolRegenNotificationTests
    {
        [Test]
        public void Tick_FullPool_RaisesNoChangeNotifications()
        {
            ResourcePoolModel model = CreateRegeneratingModel(100f);
            int changed = 0;
            model.OnResourceChanged += (id, current, max) => changed++;

            model.Tick(1f);
            model.Tick(3.5f);

            Assert.That(changed, Is.Zero, "Completed regen/heal cycles on a full pool change nothing.");
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(100f));
        }

        [Test]
        public void Tick_RefillToFull_NotifiesUntilFullThenStops()
        {
            ResourcePoolModel model = CreateRegeneratingModel(100f);
            int changed = 0;
            model.OnResourceChanged += (id, current, max) => changed++;

            model.Decrease("HP", 10f);
            Assert.That(changed, Is.EqualTo(1));

            model.Tick(1f);
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(97f));
            Assert.That(changed, Is.EqualTo(3), "Regen (+5) and heal (+2) each changed the pool.");

            model.Tick(2f);
            Assert.That(model.GetCurrent("HP"), Is.EqualTo(100f));
            Assert.That(changed, Is.EqualTo(4), "Regen fills the pool; the heal finds no headroom and stays silent.");

            model.Tick(5f);
            Assert.That(changed, Is.EqualTo(4));
        }

        [Test]
        public void Tick_UncappedPool_KeepsRegenerating()
        {
            ResourcePoolModel model = CreateRegeneratingModel(10f, max: 0f);
            int changed = 0;
            model.OnResourceChanged += (id, current, max) => changed++;

            model.Tick(1f);

            Assert.That(model.GetCurrent("HP"), Is.EqualTo(17f));
            Assert.That(changed, Is.EqualTo(2));
        }

        private static ResourcePoolModel CreateRegeneratingModel(float current, float max = 100f)
        {
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("HP", new ResourcePoolEntry
            {
                Current = current,
                Max = max,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f,
                IgnoreCanHeal = true,
                RegenPerSecond = 5f,
                RegenInterval = 1f,
                HealAmount = 2f,
                HealDelay = 1f
            });
            return model;
        }
    }
}
