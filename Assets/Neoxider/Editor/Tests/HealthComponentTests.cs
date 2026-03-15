using NUnit.Framework;
using Neo.Core.Resources;
using UnityEngine;

namespace Neo.Core.Tests
{
    public class HealthComponentTests
    {
        [Test]
        public void Decrease_ReducesHp()
        {
            GameObject go = new("Health");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                float before = health.GetCurrent(RpgResourceId.Hp);
                float dealt = health.Decrease(RpgResourceId.Hp, 25f);

                Assert.That(dealt, Is.EqualTo(25f));
                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(before - 25f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Increase_AddsHp()
        {
            GameObject go = new("Health");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                health.Decrease(RpgResourceId.Hp, 40f);
                float healed = health.Increase(RpgResourceId.Hp, 20f);

                Assert.That(healed, Is.EqualTo(20f));
                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(80f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TrySpend_WhenEnough_ReturnsTrueAndDecreases()
        {
            GameObject go = new("Health");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                float manaBefore = health.GetCurrent(RpgResourceId.Mana);
                bool ok = health.TrySpend(RpgResourceId.Mana, 25f, out string reason);

                Assert.That(ok, Is.True);
                Assert.That(reason, Is.Null);
                Assert.That(health.GetCurrent(RpgResourceId.Mana), Is.EqualTo(manaBefore - 25f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TrySpend_WhenNotEnough_ReturnsFalse()
        {
            GameObject go = new("Health");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                bool ok = health.TrySpend(RpgResourceId.Mana, 9999f, out string reason);

                Assert.That(ok, Is.False);
                Assert.That(reason, Is.Not.Null.And.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void IsDepleted_WhenHpZero_ReturnsTrue()
        {
            GameObject go = new("Health");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                health.Decrease(RpgResourceId.Hp, 1000f);
                Assert.That(health.IsDepleted(RpgResourceId.Hp), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HpCurrentValue_And_HpPercentValue_ReflectState()
        {
            GameObject go = new("Health");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                Assert.That(health.HpCurrentValue, Is.EqualTo(100f));
                Assert.That(health.HpPercentValue, Is.EqualTo(1f).Within(0.01f));

                health.Decrease(RpgResourceId.Hp, 50f);
                Assert.That(health.HpCurrentValue, Is.EqualTo(50f));
                Assert.That(health.HpPercentValue, Is.EqualTo(0.5f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
