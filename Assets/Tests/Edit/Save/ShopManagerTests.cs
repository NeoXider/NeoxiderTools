using Neo.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public class ShopManagerTests
    {
        private GameObject _go;
        private Money _money;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject();
#if MIRROR
            _go.AddComponent<Mirror.NetworkIdentity>();
#endif
            _money = _go.AddComponent<Money>();
            // Emulate Start
            _money.SetMoney(0);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Money_Add_IncreasesCurrentAndAllMoney()
        {
            _money.SetMoney(0);
            _money.AllMoney.Value = 0;

            _money.Add(150);

            Assert.AreEqual(150f, _money.money);
            Assert.AreEqual(150f, _money.allMoney);
            Assert.AreEqual(150f, _money.LastChangeMoneyValue);
        }

        [Test]
        public void Money_Spend_DecreasesValuesIfSufficient()
        {
            _money.SetMoney(200);

            bool success = _money.Spend(50);

            Assert.IsTrue(success);
            Assert.AreEqual(150f, _money.money);
            Assert.AreEqual(-50f, _money.LastChangeMoneyValue);
        }

        [Test]
        public void Money_Spend_FailsIfInsufficient()
        {
            _money.SetMoney(20);

            bool success = _money.Spend(50);

            Assert.IsFalse(success);
            Assert.AreEqual(20f, _money.money, "Money should not be deducted");
        }
    }
}
