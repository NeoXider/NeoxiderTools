#if MIRROR
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Mirror;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Local not-ready-client branch controls. Native activation admits Mirror's identity cache;
    ///     the synthetic connection state sends no packet and supplies no remote authority evidence.
    /// </summary>
    public sealed class MoneyClientRequestPlayModeTests
    {
        [TestCase(0f)]
        [TestCase(100000000f)]
        public void PureClient_WithStaleUnaffordableOrZeroEffectiveSnapshot_StillReturnsPending(float localBalance)
        {
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "This fixture requires an exclusive isolated runner without an active session.");
            FieldInfo clientState = typeof(NetworkClient).GetField("connectState", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(clientState, Is.Not.Null);
            object previousState = clientState.GetValue(null);
            bool previousReady = NetworkClient.ready;
            ISaveProvider previousProvider = SaveProvider.CurrentProvider;
            ShopPurchasePlayModeTests.MemorySaveProvider provider = new();
            GameObject root = new(nameof(MoneyClientRequestPlayModeTests));
            root.SetActive(false);
            try
            {
                SaveProvider.SetProvider(provider);
                NetworkIdentity identity = root.AddComponent<NetworkIdentity>();
                Money money = root.AddComponent<Money>();
                SetField(money, "_setInstanceOnAwake", false);
                SetField(money, "_dontDestroyOnLoad", false);
                SetField(money, "_persistMoney", false);
                SetField(money, "_moneySave", "MoneyRequestTest_" + Guid.NewGuid().ToString("N"));
                root.SetActive(true);
                Assert.That(money.netIdentity, Is.SameAs(identity),
                    "Mirror must initialize its actual NetworkBehaviour cache through native Awake.");

                SetField(money, "_persistMoney", true);
                money.AddOverflow(localBalance);
                money.SetLevelMoney(7f);
                int walletEvents = 0;
                int persistenceEvents = 0;
                money.CurrentMoney.AddListener(value => walletEvents++);
                money.AllMoney.AddListener(value => walletEvents++);
                money.LevelMoney.AddListener(value => walletEvents++);
                money.LastChangeMoney.AddListener(value => walletEvents++);
                provider.OnKeyChanged += key => persistenceEvents++;
                money.isNetworked = true;
                clientState.SetValue(null, Enum.Parse(clientState.FieldType, "Connecting"));
                NetworkClient.ready = false;
                Assert.That(money.CanSpend(1f), Is.False);
                LogAssert.Expect(LogType.Warning,
                    new Regex(@"Command .*CmdMoneyOp.* while NetworkClient is not ready"));

                MoneySpendResult result = money.TrySpend(1f);

                Assert.That(result.Status, Is.EqualTo(MoneySpendStatus.RequestedServerAuthority));
                Assert.That(result.Amount, Is.EqualTo(1f));
                Assert.That(result.BalanceBefore, Is.EqualTo(localBalance));
                Assert.That(result.BalanceAfter, Is.EqualTo(localBalance));
                Assert.That(money.money, Is.EqualTo(localBalance));
                Assert.That(money.allMoney, Is.EqualTo(localBalance));
                Assert.That(money.levelMoney, Is.EqualTo(7f));
                Assert.That(money.LastChangeMoneyValue, Is.EqualTo(localBalance));
                Assert.That(provider.GetFloat(money.SaveKey), Is.EqualTo(localBalance));
                Assert.That(provider.GetFloat(money.SaveKey + nameof(Money.AllMoney)), Is.EqualTo(localBalance));
                Assert.That(walletEvents, Is.Zero);
                Assert.That(persistenceEvents, Is.Zero);
                Assert.That(GetField(money, "_lastCmdTimePerConnection").GetValue(money), Is.Null);
            }
            finally
            {
                clientState.SetValue(null, previousState);
                NetworkClient.ready = previousReady;
                Object.DestroyImmediate(root);
                SaveProvider.SetProvider(previousProvider);
            }
        }

        private static FieldInfo GetField(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new MissingFieldException(target.GetType().FullName, name);
        }

        private static void SetField(object target, string name, object value)
        {
            GetField(target, name).SetValue(target, value);
        }
    }
}
#endif
