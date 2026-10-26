using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Core.Tests
{
    public sealed class Audit20261023NeoCoreResourcesComponentsHealthComponentTests
    {
        private GameObject _gameObject;
        private HealthComponent _health;
        private ResourceEntryInspector _hp;
        private ResourceEntryInspector _mana;
        private string _saveKey;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("AuditHealthComponent");
            _gameObject.SetActive(false);
            _health = _gameObject.AddComponent<HealthComponent>();
            _hp = new ResourceEntryInspector
            {
                id = RpgResourceId.Hp, current = 40f, max = 80f, restoreOnAwake = false
            };
            _mana = new ResourceEntryInspector
            {
                id = RpgResourceId.Mana, current = 12f, max = 48f, restoreOnAwake = false
            };
            SetAuthoringField("_pools", new List<ResourceEntryInspector> { _hp, _mana });
            SetAuthoringField("_autoSave", false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            if (_saveKey != null)
            {
                SaveProvider.DeleteKey(_saveKey);
                _saveKey = null;
            }
        }

        [Test]
        public void Initialize_LoadOnAwake_PublishesSavedStateExactlyOnce()
        {
            SeedSavedHp(17f, 120f);
            SetAuthoringField("_loadOnAwake", true);
            int readyCount = 0;
            int reactiveCount = 0;
            float currentAtReady = -1f;
            _hp.CurrentState.AddListener(_ => reactiveCount++);
            _health.OnPoolsChanged.AddListener(() =>
            {
                readyCount++;
                Assert.That(readyCount, Is.EqualTo(1), "Initialization must not recursively rebuild the pools.");
                currentAtReady = _health.GetCurrent(RpgResourceId.Hp);
            });

            _health.EnsureInitialized();
            _health.EnsureInitialized();

            Assert.That(readyCount, Is.EqualTo(1));
            Assert.That(reactiveCount, Is.EqualTo(1));
            Assert.That(currentAtReady, Is.EqualTo(17f));
            Assert.That(_health.GetMax(RpgResourceId.Hp), Is.EqualTo(120f));
            Assert.That(_hp.CurrentValue, Is.EqualTo(17f));
            Assert.That(_hp.PercentValue, Is.EqualTo(17f / 120f));
        }

        [Test]
        public void Initialize_PoolsChangedListener_CanSpendWithoutRebuilding()
        {
            int readyCount = 0;
            bool spent = false;
            _health.OnPoolsChanged.AddListener(() =>
            {
                readyCount++;
                Assert.That(readyCount, Is.EqualTo(1));
                spent = _health.TrySpend(RpgResourceId.Hp, 5f, out string reason);
                Assert.That(reason, Is.Null);
            });

            _health.EnsureInitialized();

            Assert.That(readyCount, Is.EqualTo(1));
            Assert.That(spent, Is.True);
            Assert.That(_health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(35f));
            Assert.That(_hp.CurrentValue, Is.EqualTo(35f));
        }

        [Test]
        public void Initialize_ReactiveListener_CanReadWithoutRebuilding()
        {
            int notificationCount = 0;
            float observedCurrent = -1f;
            _hp.CurrentState.AddListener(_ =>
            {
                notificationCount++;
                // Bound the old recursion so a failing regression does not overflow the test process.
                if (notificationCount == 1)
                {
                    observedCurrent = _health.GetCurrent(RpgResourceId.Hp);
                }
            });

            _health.EnsureInitialized();

            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(observedCurrent, Is.EqualTo(40f));
            Assert.That(_health.GetCurrent(RpgResourceId.Mana), Is.EqualTo(12f));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Initialize_FirstHpNotification_SeesAllManaMirrors(bool loadOnAwake, bool missingManaStates)
        {
            float expectedHp = loadOnAwake ? 17f : 40f;
            float expectedMana = loadOnAwake ? 21f : 12f;
            float expectedManaMax = loadOnAwake ? 56f : 48f;
            if (loadOnAwake)
            {
                EnablePersistence();
                ResourceProfileData saved = new();
                saved.Entries.Add(new ResourceEntryData { Id = RpgResourceId.Hp, Current = 17f, Max = 120f });
                saved.Entries.Add(new ResourceEntryData { Id = RpgResourceId.Mana, Current = 21f, Max = 56f });
                SaveProvider.SetString(_saveKey, JsonUtility.ToJson(saved));
                SetAuthoringField("_loadOnAwake", true);
            }

            if (missingManaStates)
            {
                _mana.CurrentState = null;
                _mana.PercentState = null;
                _mana.MaxState = null;
            }

            List<string> publications = new();
            float[] observedMana = null;
            float observedHp = -1f;
            bool allManaStatesPresent = false;
            int hpCurrentNotifications = 0;
            _hp.CurrentState.AddListener(value =>
            {
                publications.Add("HP current");
                hpCurrentNotifications++;
                if (hpCurrentNotifications != 1)
                {
                    return;
                }

                observedHp = value;
                allManaStatesPresent = _mana.CurrentState != null && _mana.PercentState != null && _mana.MaxState != null;
                if (!allManaStatesPresent)
                {
                    return;
                }

                _mana.CurrentState.AddListener(_ => publications.Add("Mana current"));
                _mana.PercentState.AddListener(_ => publications.Add("Mana percent"));
                _mana.MaxState.AddListener(_ => publications.Add("Mana max"));
                _health.EnsureInitialized();
                observedMana = new[]
                {
                    _health.ManaCurrentValue, _health.ManaMaxValue, _health.ManaPercentValue,
                    _mana.CurrentValue, _mana.MaxValue, _mana.PercentValue,
                    _health.GetCurrent(RpgResourceId.Mana), _health.GetMax(RpgResourceId.Mana)
                };
            });
            _hp.PercentState.AddListener(_ => publications.Add("HP percent"));
            _hp.MaxState.AddListener(_ => publications.Add("HP max"));
            _health.OnPoolsChanged.AddListener(() => publications.Add("ready"));

            _health.EnsureInitialized();
            _health.EnsureInitialized();

            Assert.That(allManaStatesPresent, Is.True, "Later reactive objects must exist before HP's first callback.");
            Assert.That(observedHp, Is.EqualTo(expectedHp));
            CollectionAssert.AreEqual(new[]
            {
                expectedMana, expectedManaMax, expectedMana / expectedManaMax,
                expectedMana, expectedManaMax, expectedMana / expectedManaMax,
                expectedMana, expectedManaMax
            }, observedMana);
            CollectionAssert.AreEqual(new[]
            {
                "HP current", "HP percent", "HP max", "Mana current", "Mana percent", "Mana max", "ready"
            }, publications);
        }

        [Test]
        public void Initialize_FirstHpListener_CanSpendLaterPoolWithoutOverwritingResult()
        {
            float manaBeforeSpend = -1f;
            bool spent = false;
            string failReason = null;
            int hpNotifications = 0;
            int readyCount = 0;
            List<float> manaPublications = new();
            _mana.CurrentState.AddListener(value => manaPublications.Add(value));
            _hp.CurrentState.AddListener(_ =>
            {
                hpNotifications++;
                if (hpNotifications == 1)
                {
                    manaBeforeSpend = _health.ManaCurrentValue;
                    spent = _health.TrySpend(RpgResourceId.Mana, 5f, out failReason);
                }
            });
            _health.OnPoolsChanged.AddListener(() => readyCount++);

            _health.EnsureInitialized();
            _health.EnsureInitialized();

            Assert.That(manaBeforeSpend, Is.EqualTo(12f));
            Assert.That(spent, Is.True);
            Assert.That(failReason, Is.Null);
            Assert.That(_health.GetCurrent(RpgResourceId.Mana), Is.EqualTo(7f));
            Assert.That(_health.ManaCurrentValue, Is.EqualTo(7f));
            Assert.That(_mana.PercentValue, Is.EqualTo(7f / 48f));
            Assert.That(hpNotifications, Is.EqualTo(1));
            Assert.That(readyCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { 7f, 7f }, manaPublications,
                "The spend and the initial Mana publication must both expose the committed value.");
        }

        [Test]
        public void ConditionProperties_BeforeAwake_ReadConfiguredPools()
        {
            Assert.That(_health.HpCurrentValue, Is.EqualTo(40f));
            Assert.That(_health.HpMaxValue, Is.EqualTo(80f));
            Assert.That(_health.HpPercentValue, Is.EqualTo(0.5f));
            Assert.That(_health.ManaCurrentValue, Is.EqualTo(12f));
            Assert.That(_health.ManaMaxValue, Is.EqualTo(48f));
            Assert.That(_health.ManaPercentValue, Is.EqualTo(0.25f));
        }

        [TestCase("Decrease", 30f, 80f, 0)]
        [TestCase("Increase", 50f, 80f, 0)]
        [TestCase("Restore", 80f, 80f, 0)]
        [TestCase("SetMax", 20f, 20f, 1)]
        [TestCase("TrySpend", 30f, 80f, 0)]
        public void Mutation_SyncsReactiveOnce_BeforeChangedEvent(string operation, float expectedCurrent,
            float expectedMax, int expectedMaxNotifications)
        {
            _health.EnsureInitialized();
            int currentNotifications = 0;
            int percentNotifications = 0;
            int maxNotifications = 0;
            int changedCount = 0;
            float percentSeenByReactiveListener = -1f;
            _hp.CurrentState.AddListener(_ =>
            {
                currentNotifications++;
                percentSeenByReactiveListener = _hp.PercentValue;
            });
            _hp.PercentState.AddListener(_ => percentNotifications++);
            _hp.MaxState.AddListener(_ => maxNotifications++);
            _hp.OnChanged.AddListener((current, max) =>
            {
                changedCount++;
                Assert.That(current, Is.EqualTo(expectedCurrent));
                Assert.That(max, Is.EqualTo(expectedMax));
                Assert.That(_hp.CurrentValue, Is.EqualTo(current));
                Assert.That(_hp.MaxValue, Is.EqualTo(max));
                Assert.That(_hp.PercentValue, Is.EqualTo(current / max));
                Assert.That(_health.HpCurrentValue, Is.EqualTo(current));
            });

            switch (operation)
            {
                case "Decrease":
                    Assert.That(_health.Decrease(RpgResourceId.Hp, 10f), Is.EqualTo(10f));
                    break;
                case "Increase":
                    Assert.That(_health.Increase(RpgResourceId.Hp, 10f), Is.EqualTo(10f));
                    break;
                case "Restore":
                    _health.Restore(RpgResourceId.Hp);
                    break;
                case "SetMax":
                    _health.SetMax(RpgResourceId.Hp, 20f);
                    break;
                case "TrySpend":
                    Assert.That(_health.TrySpend(RpgResourceId.Hp, 10f, out string reason), Is.True);
                    Assert.That(reason, Is.Null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }

            Assert.That(changedCount, Is.EqualTo(1));
            Assert.That(currentNotifications, Is.EqualTo(1));
            Assert.That(percentNotifications, Is.EqualTo(1));
            Assert.That(maxNotifications, Is.EqualTo(expectedMaxNotifications));
            Assert.That(percentSeenByReactiveListener, Is.EqualTo(expectedCurrent / expectedMax));
            Assert.That(_health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(expectedCurrent));

            _health.Decrease(RpgResourceId.Hp, 0f);
            Assert.That(currentNotifications, Is.EqualTo(1), "A no-op must not force another notification.");
            Assert.That(changedCount, Is.EqualTo(1));
        }

        [Test]
        public void Increase_AtMax_DoesNotRepublishUnchangedReactiveStates()
        {
            _health.Restore(RpgResourceId.Hp);
            int notificationCount = 0;
            _hp.CurrentState.AddListener(_ => notificationCount++);
            _hp.PercentState.AddListener(_ => notificationCount++);
            _hp.MaxState.AddListener(_ => notificationCount++);

            float healed = _health.Increase(RpgResourceId.Hp, 10f);

            Assert.That(healed, Is.EqualTo(0f));
            Assert.That(_health.HpCurrentValue, Is.EqualTo(80f));
            Assert.That(notificationCount, Is.EqualTo(0));
        }

        [Test]
        public void SetMax_Negative_ReportsAppliedMaximum()
        {
            _health.EnsureInitialized();
            float reportedMax = -1f;
            float observedMax = -1f;
            _hp.OnChangeMax.AddListener(value =>
            {
                reportedMax = value;
                observedMax = _health.HpMaxValue;
            });

            _health.SetMaxHp(-5f);

            Assert.That(reportedMax, Is.EqualTo(0f));
            Assert.That(observedMax, Is.EqualTo(0f));
            Assert.That(_hp.MaxValue, Is.EqualTo(0f));
            Assert.That(_hp.PercentValue, Is.EqualTo(0f));
        }

        [Test]
        public void Load_RestoresOnce_WithoutDamageOrDeathEvents()
        {
            _health.EnsureInitialized();
            SeedSavedHp(0f, 80f);
            int currentNotifications = 0;
            int damageCount = 0;
            int deathCount = 0;
            _hp.CurrentState.AddListener(_ => currentNotifications++);
            _hp.OnDamage.AddListener(_ => damageCount++);
            _hp.OnDeath.AddListener(() => deathCount++);

            _health.Load();

            Assert.That(_health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(0f));
            Assert.That(_hp.CurrentValue, Is.EqualTo(0f));
            Assert.That(currentNotifications, Is.EqualTo(1));
            Assert.That(damageCount, Is.EqualTo(0));
            Assert.That(deathCount, Is.EqualTo(0));
        }

        [Test]
        public void Initialize_NullAndBlankEntries_SaveOnlyValidPools()
        {
            SetAuthoringField("_pools", new List<ResourceEntryInspector>
            {
                null, new ResourceEntryInspector { id = " " }, _hp
            });
            EnablePersistence();

            _health.Decrease(RpgResourceId.Hp, 5f);
            _health.Save();

            ResourceProfileData saved = JsonUtility.FromJson<ResourceProfileData>(SaveProvider.GetString(_saveKey));
            Assert.That(saved.Entries.Count, Is.EqualTo(1));
            Assert.That(saved.Entries[0].Id, Is.EqualTo(RpgResourceId.Hp));
            Assert.That(saved.Entries[0].Current, Is.EqualTo(35f));
            Assert.That(_health.HpCurrentValue, Is.EqualTo(35f));
            Assert.That(_health.ManaCurrentValue, Is.EqualTo(0f));
        }

        [Test]
        public void Initialize_NullPoolList_SavesAnEmptyProfile()
        {
            SetAuthoringField("_pools", null);
            EnablePersistence();

            _health.Save();

            ResourceProfileData saved = JsonUtility.FromJson<ResourceProfileData>(SaveProvider.GetString(_saveKey));
            Assert.That(saved.Entries, Is.Empty);
            Assert.That(_health.GetCurrent(RpgResourceId.Hp), Is.EqualTo(0f));
            Assert.That(_health.HpPercentValue, Is.EqualTo(0f));
        }

        [Test]
        public void Initialize_MissingReactiveObjects_RestoresLiveState()
        {
            _hp.CurrentState = null;
            _hp.PercentState = null;
            _hp.MaxState = null;

            _health.EnsureInitialized();

            Assert.That(_hp.CurrentValue, Is.EqualTo(40f));
            Assert.That(_hp.PercentValue, Is.EqualTo(0.5f));
            Assert.That(_hp.MaxValue, Is.EqualTo(80f));
            _health.Damage(5f);
            Assert.That(_hp.CurrentValue, Is.EqualTo(35f));
            Assert.That(_hp.PercentValue, Is.EqualTo(35f / 80f));
        }

        private void EnablePersistence()
        {
            _saveKey = "Audit20261023NeoCoreResourcesComponents_" + Guid.NewGuid().ToString("N");
            SetAuthoringField("_saveKey", _saveKey);
        }

        private void SeedSavedHp(float current, float max)
        {
            EnablePersistence();
            ResourceProfileData saved = new();
            saved.Entries.Add(new ResourceEntryData { Id = RpgResourceId.Hp, Current = current, Max = max });
            SaveProvider.SetString(_saveKey, JsonUtility.ToJson(saved));
        }

        private void SetAuthoringField(string name, object value)
        {
            // Reflection configures private Inspector fields only; all exercised behavior is public API/events.
            FieldInfo field = typeof(HealthComponent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(_health, value);
        }
    }
}
