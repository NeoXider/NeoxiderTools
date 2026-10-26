using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

#pragma warning disable CS0618
namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023NeoRpgCombatHealthTests
    {
        private GameObject _actor;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _actor = new GameObject("CombatHealthRegression");
            _health = _actor.AddComponent<Health>();
            _health.SetMaxHp(10, true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_actor);
        }

        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(0)]
        public void NonpositiveAmounts_DoNotChangeHealthOrRaiseCombatEvents(int amount)
        {
            _health.SetHp(5);
            int events = 0;
            _health.OnDamage = new UnityEvent<int>();
            _health.OnHeal = new UnityEvent<int>();
            _health.OnDeath = new UnityEvent();
            _health.OnDamage.AddListener(_ => events++);
            _health.OnHeal.AddListener(_ => events++);
            _health.OnDeath.AddListener(() => events++);

            _health.TakeDamage(amount);
            Assert.That(_health.HpValue, Is.EqualTo(5));
            _health.Heal(amount);

            Assert.That(_health.HpValue, Is.EqualTo(5));
            Assert.That(_health.HpPercentValue, Is.EqualTo(0.5f));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void Heal_IntMaxValue_SaturatesAtMaximum()
        {
            _health.SetHp(5);

            _health.Heal(int.MaxValue);

            Assert.That(_health.HpValue, Is.EqualTo(10));
            Assert.That(_health.HpPercentValue, Is.EqualTo(1f));
            Assert.That(_health.IsAlive, Is.True);
        }

        [Test]
        public void PositiveLimits_PreserveCappedRequestedEventAmounts()
        {
            int damageEvent = 0;
            int healEvent = 0;
            _health.maxDamageAmount = 3;
            _health.maxHealAmount = 2;
            _health.OnDamage = new UnityEvent<int>();
            _health.OnHeal = new UnityEvent<int>();
            _health.OnDamage.AddListener(value => damageEvent = value);
            _health.OnHeal.AddListener(value => healEvent = value);

            _health.TakeDamage(9);
            Assert.That(_health.HpValue, Is.EqualTo(7));
            Assert.That(damageEvent, Is.EqualTo(3));
            _health.Heal(9);
            Assert.That(_health.HpValue, Is.EqualTo(9));
            _health.Heal(9);

            Assert.That(_health.HpValue, Is.EqualTo(10));
            Assert.That(healEvent, Is.EqualTo(2), "Legacy events report capped requests, including overheal.");
        }

        [TestCase(3, 3)]
        [TestCase(0, 0)]
        [TestCase(-5, 0)]
        public void SetMaxHp_ClampsCurrentAndReportsEffectiveMaximum(int requested, int expected)
        {
            int notifiedMaximum = -1;
            _health.OnChangeMaxHp = new UnityEvent<int>();
            _health.OnChangeMaxHp.AddListener(value => notifiedMaximum = value);

            _health.SetMaxHp(requested);

            Assert.That(_health.MaxHp, Is.EqualTo(expected));
            Assert.That(_health.HpValue, Is.EqualTo(expected));
            Assert.That(_health.HpPercentValue, Is.EqualTo(expected > 0 ? 1f : 0f));
            Assert.That(notifiedMaximum, Is.EqualTo(expected));
        }

        [Test]
        public void Damage_DeathIsRaisedOnceUntilHealthIsRestored()
        {
            int deaths = 0;
            _health.OnDeath = new UnityEvent();
            _health.OnDeath.AddListener(() => deaths++);

            _health.TakeDamage(int.MaxValue);
            _health.TakeDamage(1);
            Assert.That(_health.HpValue, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));

            _health.Heal(5);
            _health.TakeDamage(5);

            Assert.That(_health.HpValue, Is.Zero);
            Assert.That(deaths, Is.EqualTo(2));
        }

        [Test]
        public void Damage_ListenerDealsLethalDamage_ReportsOneDeath()
        {
            int deaths = 0;
            _health.OnDeath = new UnityEvent();
            _health.OnDeath.AddListener(() => deaths++);
            _health.OnDamage = new UnityEvent<int>();
            _health.OnDamage.AddListener(_ =>
            {
                if (_health.IsAlive)
                {
                    _health.TakeDamage(10);
                }
            });

            _health.TakeDamage(1);

            Assert.That(_health.HpValue, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void SetHp_UpdatesSerializedHealthForSnapshots()
        {
            _health.SetHp(7);
            using SerializedObject snapshot = new(_health);

            Assert.That(snapshot.FindProperty("hp").intValue, Is.EqualTo(7));
            Assert.That(_health.HpValue, Is.EqualTo(7));
        }

        [Test]
        public void SetHeal_BeforeAwake_PreservesConfiguration()
        {
            _health.SetHeal(3, 0.25f);

            Assert.That(_health.healAmount, Is.EqualTo(3));
            Assert.That(_health.healDelay, Is.EqualTo(0.25f));
        }
    }
}
#pragma warning restore CS0618
