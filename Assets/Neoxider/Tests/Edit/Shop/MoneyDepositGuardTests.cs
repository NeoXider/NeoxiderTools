using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the deposit guard: <see cref="Money.Add" /> is the reward path and must never reduce the balance.
    ///     A negative amount used to bypass every check in <see cref="Money.TrySpend" /> and drive the wallet
    ///     into debt.
    /// </summary>
    [TestFixture]
    public class MoneyDepositGuardTests
    {
        private GameObject _go;
        private Money _money;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject(nameof(MoneyDepositGuardTests));
            _money = _go.AddComponent<Money>();

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

        /// <summary>
        ///     The guard reports every rejection, so each test declares the exact error it expects. Ignoring all
        ///     failing messages instead would let an unrelated error pass unnoticed.
        /// </summary>
        private static void ExpectRejection(string operation, string reason)
        {
            LogAssert.Expect(LogType.Error, new Regex($@"\[Money\] {operation} rejected: {reason}"));
        }

        [Test]
        public void Add_WithNegativeAmount_IsRejectedAndLeavesBalanceUntouched()
        {
            _money.Add(100f);

            ExpectRejection("Add", "negative amount");
            _money.Add(-500f);

            Assert.AreEqual(100f, _money.CurrentMoney.CurrentValue,
                "a negative deposit must not reduce the balance");
        }

        [Test]
        public void AddOverflow_WithNegativeAmount_IsRejected()
        {
            _money.Add(50f);

            ExpectRejection("AddOverflow", "negative amount");
            _money.AddOverflow(-80f);

            Assert.AreEqual(50f, _money.CurrentMoney.CurrentValue);
        }

        [Test]
        public void Add_WithNotANumber_IsRejected()
        {
            _money.Add(25f);

            ExpectRejection("Add", "amount is not a finite number");
            _money.Add(float.NaN);

            ExpectRejection("Add", "amount is not a finite number");
            _money.Add(float.PositiveInfinity);

            Assert.AreEqual(25f, _money.CurrentMoney.CurrentValue);
        }

        [Test]
        public void SetMoney_WithNegativeValue_ClampsToZero()
        {
            _money.Add(10f);
            _money.SetMoney(-40f);

            Assert.AreEqual(0f, _money.CurrentMoney.CurrentValue,
                "a balance below zero would let later spends succeed against a debt");
        }

        [Test]
        public void Add_WithPositiveAmount_StillWorks()
        {
            _money.Add(12.5f);
            _money.Add(2.5f);

            Assert.AreEqual(15f, _money.CurrentMoney.CurrentValue, 0.0001f);
        }

        [Test]
        public void NegativeAdd_DoesNotLetSpendingExceedTheBalance()
        {
            _money.Add(30f);

            ExpectRejection("Add", "negative amount");
            _money.Add(-1000f);

            Assert.IsFalse(_money.Spend(31f), "spending more than the balance must still fail");
            Assert.IsTrue(_money.Spend(30f), "the untouched balance must still be spendable");
        }
    }
}
