using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers purchase atomicity: money is taken before the item is granted, so a grant handler that throws
    ///     must roll the profile back and refund the price instead of leaving the player paying for nothing.
    /// </summary>
    [TestFixture]
    public class ShopPurchaseRollbackTests
    {
        private GameObject _shopGo;
        private GameObject _moneyGo;
        private ShopBehaviour _shop;
        private Money _money;
        private ShopItemData _item;

        [SetUp]
        public void SetUp()
        {
            _moneyGo = new GameObject("Money");
            _money = _moneyGo.AddComponent<Money>();
            SetPrivate(_money, "_persistMoney", false);
            _money.Add(500f);

            _shopGo = new GameObject("Shop");
            _shop = _shopGo.AddComponent<ShopBehaviour>();
            _shop.moneySpendSource = _moneyGo;

            _item = CreateItem("sword", 100);
            _shop.SetItems(new[] { _item });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_shopGo);
            Object.DestroyImmediate(_moneyGo);
            Object.DestroyImmediate(_item);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (info == null && target.GetType().BaseType != null)
            {
                info = target.GetType().BaseType.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            }

            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} field expected");
            info.SetValue(target, value);
        }

        /// <summary>
        ///     A rolled back purchase is always reported, so every test that breaks the grant declares the exact
        ///     error. Ignoring all failing messages instead would also hide a failed refund or a failed save.
        /// </summary>
        private static void ExpectRollback(string itemId)
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@"\[Shop\] Purchase of '{itemId}' failed while granting it"));
        }

        private static ShopItemData CreateItem(string id, int price)
        {
            ShopItemData data = ScriptableObject.CreateInstance<ShopItemData>();
            SetPrivate(data, "_id", id);
            SetPrivate(data, "_price", price);
            SetPrivate(data, "_currencyOverrideSaveKey", "");
            SetPrivate(data, "_isSinglePurchase", true);
            return data;
        }

        [Test]
        public void SuccessfulPurchase_TakesTheMoneyAndGrantsTheItem()
        {
            _shop.Buy("sword");

            Assert.AreEqual(400f, _money.CurrentMoney.CurrentValue);
            Assert.IsTrue(_shop.IsOwned("sword"));
        }

        [Test]
        public void GrantHandlerThatThrows_RefundsThePriceAndLeavesTheItemUnowned()
        {
            _shop.OnPurchasedId.AddListener(_ => throw new System.InvalidOperationException("grant failed"));

            ExpectRollback("sword");
            Assert.DoesNotThrow(() => _shop.Buy("sword"),
                "a failing grant must not propagate out of Buy");

            Assert.AreEqual(500f, _money.CurrentMoney.CurrentValue,
                "the price must be refunded when the grant fails");
            Assert.IsFalse(_shop.IsOwned("sword"),
                "the profile must be rolled back when the grant fails");
        }

        [Test]
        public void GrantHandlerThatThrows_ReportsAPurchaseFailure()
        {
            bool failed = false;
            _shop.OnPurchasedId.AddListener(_ => throw new System.InvalidOperationException("grant failed"));
            _shop.OnPurchaseFailedId.AddListener(_ => failed = true);

            ExpectRollback("sword");
            _shop.Buy("sword");

            Assert.IsTrue(failed, "a rolled back purchase must surface as a failure, not as success");
        }

        [Test]
        public void FailedPurchase_CanBeRetriedAfterTheHandlerIsFixed()
        {
            UnityEngine.Events.UnityAction<string> thrower =
                _ => throw new System.InvalidOperationException("grant failed");
            _shop.OnPurchasedId.AddListener(thrower);

            ExpectRollback("sword");
            _shop.Buy("sword");

            _shop.OnPurchasedId.RemoveListener(thrower);
            _shop.Buy("sword");

            Assert.IsTrue(_shop.IsOwned("sword"));
            Assert.AreEqual(400f, _money.CurrentMoney.CurrentValue,
                "exactly one price must be charged across the failed and the successful attempt");
        }
    }
}
