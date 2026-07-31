using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using NUnit.Framework;
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

        [Test]
        public void Decrease_WhenHpDepleted_InvokesOnDeathOnce()
        {
            GameObject go = new("HealthDeathOnce");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                ResourceEntryInspector hp = GetPoolEntry(health, RpgResourceId.Hp);
                int deathCount = 0;
                hp.OnDeath.AddListener(() => deathCount++);

                health.Decrease(RpgResourceId.Hp, 1000f);

                Assert.That(deathCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Decrease_WhenCustomPoolDepleted_InvokesPoolOnDeath()
        {
            GameObject go = new("HealthCustomPoolDeath");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                ResourceEntryInspector mana = GetPoolEntry(health, RpgResourceId.Mana);
                int deathCount = 0;
                mana.OnDeath.AddListener(() => deathCount++);

                health.Decrease(RpgResourceId.Mana, 1000f);

                Assert.That(deathCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Decrease_Overkill_ReturnsOnlyRemovedAmount()
        {
            var model = new ResourcePoolModel();
            model.AddPool(RpgResourceId.Hp, new ResourcePoolEntry
            {
                Current = 5f,
                Max = 100f,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f
            });

            float removed = model.Decrease(RpgResourceId.Hp, 10f);

            // WHY: regression — the unlimited branch used to report the full requested amount (10).
            Assert.That(removed, Is.EqualTo(5f));
            Assert.That(model.GetCurrent(RpgResourceId.Hp), Is.EqualTo(0f));
        }

        [Test]
        public void Increase_UncappedPoolWithIncreaseLimit_AddsInsteadOfDraining()
        {
            var model = new ResourcePoolModel();
            model.AddPool(RpgResourceId.Mana, new ResourcePoolEntry
            {
                Current = 50f,
                Max = 0f, // WHY: Max <= 0 means no cap
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = 10f
            });

            float added = model.Increase(RpgResourceId.Mana, 5f);

            // WHY: regression — headroom (Max - Current) went negative for uncapped pools and zeroed them.
            Assert.That(added, Is.EqualTo(5f));
            Assert.That(model.GetCurrent(RpgResourceId.Mana), Is.EqualTo(55f));
        }

        [Test]
        public void SetCurrent_IgnoresMaxDecreaseLimit_AndDoesNotFireDepleted()
        {
            var model = new ResourcePoolModel();
            model.AddPool(RpgResourceId.Hp, new ResourcePoolEntry
            {
                Current = 100f,
                Max = 100f,
                MaxDecreaseAmount = 10f,
                MaxIncreaseAmount = -1f
            });
            int depletedCount = 0;
            model.OnResourceDepleted += _ => depletedCount++;

            model.SetCurrent(RpgResourceId.Hp, 0f);

            Assert.That(model.GetCurrent(RpgResourceId.Hp), Is.EqualTo(0f));
            Assert.That(depletedCount, Is.EqualTo(0));
        }

        [Test]
        public void Load_RestoresSavedCurrent_DespiteMaxDecreaseLimit()
        {
            GameObject go = new("HealthLoad");
            HealthComponent health = go.AddComponent<HealthComponent>();
            const string saveKey = "Test_HealthComponent_Load_MaxDecrease";

            try
            {
                SetPrivateField(health, "_saveKey", saveKey);
                ResourceEntryInspector hp = GetPoolEntry(health, RpgResourceId.Hp);
                hp.maxDecreaseAmount = 10f;

                health.EnsureInitialized();
                for (int i = 0; i < 8; i++)
                {
                    health.Decrease(RpgResourceId.Hp, 10f);
                }

                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(20f));

                health.Save();
                health.Restore(RpgResourceId.Hp);
                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(100f));

                health.Load();

                // WHY: regression — Load used Restore+Decrease, so the damage cap clamped 80 to 10 (HP=90).
                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(20f));
            }
            finally
            {
                // WHY: destroy first — OnDisable auto-saves and would re-create the key after DeleteKey.
                Object.DestroyImmediate(go);
                Neo.Save.SaveProvider.DeleteKey(saveKey);
            }
        }

        [Test]
        public void Load_WithZeroSavedHp_DoesNotInvokeOnDeath()
        {
            GameObject go = new("HealthLoadZero");
            HealthComponent health = go.AddComponent<HealthComponent>();
            const string saveKey = "Test_HealthComponent_Load_ZeroHp";

            try
            {
                SetPrivateField(health, "_saveKey", saveKey);
                health.EnsureInitialized();
                health.Decrease(RpgResourceId.Hp, 1000f);
                health.Save();
                health.Restore(RpgResourceId.Hp);

                ResourceEntryInspector hp = GetPoolEntry(health, RpgResourceId.Hp);
                int deathCount = 0;
                hp.OnDeath.AddListener(() => deathCount++);

                health.Load();

                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(0f));
                Assert.That(deathCount, Is.EqualTo(0), "Loading saved state must not fire OnDeath");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Neo.Save.SaveProvider.DeleteKey(saveKey);
            }
        }

        [Test]
        public void Tick_RegenDoesNotHealFromZeroWhenCanHealIsFalse()
        {
            var model = new ResourcePoolModel();
            model.AddPool(RpgResourceId.Hp, new ResourcePoolEntry
            {
                Current = 0f,
                Max = 10f,
                RegenPerSecond = 5f,
                RegenInterval = 1f,
                IgnoreCanHeal = false
            });

            model.Tick(1f);

            Assert.That(model.GetCurrent(RpgResourceId.Hp), Is.EqualTo(0f));
        }

        [Test]
        public void Damage_And_Heal_OneArg_OperateOnHpPool()
        {
            GameObject go = new("HealthDamageHeal");
            HealthComponent health = go.AddComponent<HealthComponent>();

            try
            {
                health.Damage(30f);
                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(70f));

                health.Heal(10f);
                Assert.That(health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(80f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static ResourceEntryInspector GetPoolEntry(HealthComponent health, string id)
        {
            FieldInfo field = typeof(HealthComponent)
                .GetField("_pools", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            var pools = (List<ResourceEntryInspector>)field.GetValue(health);
            ResourceEntryInspector entry = pools.Find(pool => pool.id == id);
            Assert.That(entry, Is.Not.Null);
            return entry;
        }
    }
}
