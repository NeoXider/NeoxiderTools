using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Profile persistence failure compensates the debit; postcommit observer failure preserves the purchase.
    /// </summary>
    [TestFixture]
    public class ShopPurchaseRollbackTests
    {
        private Audit20261023NeoEditorTestsSaveScope _saveScope;
        private FailingProfileProvider _provider;
        private GameObject _shopGo;
        private GameObject _moneyGo;
        private ShopBehaviour _shop;
        private Money _money;
        private ShopItemData _item;

        [SetUp]
        public void SetUp()
        {
            _saveScope = new Audit20261023NeoEditorTestsSaveScope();
            _provider = new FailingProfileProvider(SaveProvider.CurrentProvider);
            SaveProvider.SetProvider(_provider);
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
            try
            {
                Object.DestroyImmediate(_shopGo);
                Object.DestroyImmediate(_moneyGo);
                Object.DestroyImmediate(_item);
            }
            finally
            {
                _saveScope?.Dispose();
                _saveScope = null;
            }
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
        ///     Declare the exact commit error without suppressing unrelated refund/save errors.
        /// </summary>
        private static void ExpectRollback(string itemId)
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@"\[Shop\] Purchase of '{itemId}' failed while committing profile"));
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
        public void ProfileSaveThatThrows_RefundsThePriceAndLeavesTheItemUnowned()
        {
            _provider.FailNextProfileWrite = true;

            ExpectRollback("sword");
            Assert.DoesNotThrow(() => _shop.Buy("sword"),
                "a failing profile commit must not propagate out of Buy");

            Assert.AreEqual(500f, _money.CurrentMoney.CurrentValue,
                "the price must be refunded when the profile commit fails");
            Assert.IsFalse(_shop.IsOwned("sword"),
                "the profile must be rolled back when persistence fails");
        }

        [Test]
        public void ProfileSaveThatThrows_ReportsAPurchaseFailure()
        {
            bool failed = false;
            _provider.FailNextProfileWrite = true;
            _shop.OnPurchaseFailedId.AddListener(_ => failed = true);

            ExpectRollback("sword");
            _shop.Buy("sword");

            Assert.IsTrue(failed, "a rolled back purchase must surface as a failure, not as success");
        }

        [Test]
        public void FailedPurchase_CanBeRetriedAfterPersistenceRecovers()
        {
            _provider.FailNextProfileWrite = true;

            ExpectRollback("sword");
            _shop.Buy("sword");

            _shop.Buy("sword");

            Assert.IsTrue(_shop.IsOwned("sword"));
            Assert.AreEqual(400f, _money.CurrentMoney.CurrentValue,
                "exactly one price must be charged across the failed and the successful attempt");
        }

        [Test]
        public void PostcommitObserverThatThrows_PreservesPaidOwnershipAndReportsFault()
        {
            int faults = 0;
            bool failed = false;
            _shop.OnPurchasedId.AddListener(_ => throw new System.InvalidOperationException("observer failed"));
            _shop.OnNotificationFault.AddListener(_ => faults++);
            _shop.OnPurchaseFailedId.AddListener(_ => failed = true);
            LogAssert.Expect(LogType.Error, new Regex(@"\[Shop\] Observer 'OnPurchasedId' failed after purchase 'sword'"));

            Assert.DoesNotThrow(() => _shop.Buy("sword"));

            Assert.That(_money.money, Is.EqualTo(400f));
            Assert.That(_shop.IsOwned("sword"), Is.True);
            Assert.That(_provider.GetString("Shop"), Does.Contain("sword"));
            Assert.That(faults, Is.EqualTo(1));
            Assert.That(failed, Is.False);
        }

        private sealed class FailingProfileProvider : ISaveProvider
        {
            private readonly ISaveProvider _inner;
            public FailingProfileProvider(ISaveProvider inner) { _inner = inner; }
            public bool FailNextProfileWrite { get; set; }
            public SaveProviderType ProviderType => _inner.ProviderType;
            public event System.Action OnDataSaved { add { _inner.OnDataSaved += value; } remove { _inner.OnDataSaved -= value; } }
            public event System.Action OnDataLoaded { add { _inner.OnDataLoaded += value; } remove { _inner.OnDataLoaded -= value; } }
            public event System.Action<string> OnKeyChanged { add { _inner.OnKeyChanged += value; } remove { _inner.OnKeyChanged -= value; } }
            public int GetInt(string key, int defaultValue = 0) => _inner.GetInt(key, defaultValue);
            public void SetInt(string key, int value) => _inner.SetInt(key, value);
            public float GetFloat(string key, float defaultValue = 0f) => _inner.GetFloat(key, defaultValue);
            public void SetFloat(string key, float value) => _inner.SetFloat(key, value);
            public string GetString(string key, string defaultValue = "") => _inner.GetString(key, defaultValue);
            public void SetString(string key, string value)
            {
                if (key == "Shop" && FailNextProfileWrite)
                {
                    FailNextProfileWrite = false;
                    throw new System.IO.IOException("profile store unavailable");
                }
                _inner.SetString(key, value);
            }
            public bool GetBool(string key, bool defaultValue = false) => _inner.GetBool(key, defaultValue);
            public void SetBool(string key, bool value) => _inner.SetBool(key, value);
            public bool HasKey(string key) => _inner.HasKey(key);
            public void DeleteKey(string key) => _inner.DeleteKey(key);
            public void DeleteAll() => _inner.DeleteAll();
            public void Save() => _inner.Save();
            public void Load() => _inner.Load();
        }
    }
}
