using System.Reflection;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the 9.6.0 soft-cap contract: Add()/SetMoney() clamp to MaxMoney,
    ///     AddOverflow() ignores the cap, MaxMoney = 0 means unlimited.
    /// </summary>
    [TestFixture]
    public class MoneyCapEditModeTests
    {
        private GameObject _go;
        private Money _money;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("MoneyCapEditModeTests");
            _money = _go.AddComponent<Money>();

            // Session-only: keep tests away from real SaveProvider data.
            FieldInfo persist = typeof(Money).GetField("_persistMoney",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(persist, "Money._persistMoney field expected");
            persist.SetValue(_money, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Add_ClampsToMaxMoney()
        {
            _money.MaxMoney = 30f;
            _money.Add(50f);

            Assert.AreEqual(30f, _money.CurrentMoney.CurrentValue);
        }

        [Test]
        public void AddOverflow_IgnoresMaxMoney()
        {
            _money.MaxMoney = 30f;
            _money.Add(30f);
            _money.AddOverflow(10f);

            Assert.AreEqual(40f, _money.CurrentMoney.CurrentValue);
        }

        [Test]
        public void ZeroMaxMoney_MeansUnlimited()
        {
            _money.MaxMoney = 0f;
            _money.Add(1000f);

            Assert.AreEqual(1000f, _money.CurrentMoney.CurrentValue);
        }
    }
}
