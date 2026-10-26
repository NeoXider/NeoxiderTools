using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Neo.Network;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Tests
{
    /// <summary>Local admission-unit and serialization regressions. These are not remote/woven Command evidence.</summary>
    public sealed class Audit20261023NeoShopMoneyAuthorityTests
    {
        private ISaveProvider _previousProvider;
        private Audit20261023NeoShopMemoryProvider _provider;
        private GameObject _root;
        private Money _money;
        private string _prefabPath;
        private int _walletEvents;
        private int _persistenceEvents;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new Audit20261023NeoShopMemoryProvider();
            SaveProvider.SetProvider(_provider);
            _root = new GameObject(nameof(Audit20261023NeoShopMoneyAuthorityTests));
            _money = _root.AddComponent<Money>();
            SetPrivate(_money, "_persistMoney", false);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (!string.IsNullOrEmpty(_prefabPath))
                {
                    AssetDatabase.DeleteAsset(_prefabPath);
                }

                Object.DestroyImmediate(_root);
            }
            finally
            {
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [Test]
        public void NewWallet_DefaultsToServerOnly()
        {
            Assert.That(_money.ClientMutationAuthority, Is.EqualTo(NetworkAuthorityMode.ServerOnly));
        }

        [Test]
        public void LegacyPrefabWithoutPolicyToken_ImportsWithServerOnlyDefault()
        {
            _prefabPath = "Assets/Audit20261023NeoShopAuthority_" + Guid.NewGuid().ToString("N") + ".prefab";
            _money.ClientMutationAuthority = NetworkAuthorityMode.None;
            PrefabUtility.SaveAsPrefabAsset(_root, _prefabPath);
            string yaml = File.ReadAllText(_prefabPath, Encoding.UTF8);
            string legacy = Regex.Replace(yaml, @"(?m)^[ \t]*_clientMutationAuthority:[^\r\n]*(?:\r?\n|$)", "");
            Assert.That(legacy, Is.Not.EqualTo(yaml), "The fixture must remove an actually serialized policy field.");
            File.WriteAllText(_prefabPath, legacy, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(_prefabPath, ImportAssetOptions.ForceUpdate);

            GameObject imported = PrefabUtility.LoadPrefabContents(_prefabPath);
            try
            {
                Assert.That(imported.GetComponent<Money>().ClientMutationAuthority,
                    Is.EqualTo(NetworkAuthorityMode.ServerOnly),
                    "An absent legacy token must not silently deserialize as cooperative enum-zero None.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(imported);
            }
        }

        [TestCase(NetworkAuthorityMode.ServerOnly)]
        [TestCase(NetworkAuthorityMode.OwnerOnly)]
        [TestCase(NetworkAuthorityMode.None)]
        [TestCase((NetworkAuthorityMode)99)]
        public void ClientPolicy_DoesNotGateDirectOfflineWalletOperations(NetworkAuthorityMode policy)
        {
            _money.ClientMutationAuthority = policy;
            _money.MaxMoney = 100f;
            _money.Add(200f);
            Assert.That(_money.money, Is.EqualTo(100f));
            _money.AddOverflow(50f);
            Assert.That(_money.Spend(40f), Is.True);
            Assert.That(_money.money, Is.EqualTo(110f));
            _money.SetMoney(-3f);
            Assert.That(_money.money, Is.Zero);
            _money.SetLevelMoney(20f);
            _money.SetMoneyForLevel();
            Assert.That(_money.money, Is.EqualTo(20f));
            Assert.That(_money.allMoney, Is.EqualTo(170f));
            Assert.That(_money.levelMoney, Is.Zero);
        }

        [Test]
        public void PositiveSpendWithoutRepresentableDecrease_RejectsWithoutMutationOrNotifications()
        {
            PrepareObservedWallet(100000000f);

            Assert.That(_money.CanSpend(1f), Is.False);
            Assert.That(_money.CanConfirmSpendNow(1f), Is.False);
            MoneySpendResult result = _money.TrySpend(1f);
            Assert.That(result.Status, Is.EqualTo(MoneySpendStatus.RejectedInsufficientFunds));
            Assert.That(result.Amount, Is.EqualTo(1f));
            Assert.That(result.BalanceBefore, Is.EqualTo(100000000f));
            Assert.That(result.BalanceAfter, Is.EqualTo(100000000f));
            Assert.That(_money.Spend(1f), Is.False);
            AssertObservedWalletUnchanged(100000000f);
        }

        [TestCase(10f, 0.1f, 9.9f)]
        [TestCase(100000000f, 5f, 99999992f)]
        [TestCase(0f, 0f, 0f)]
        [TestCase(100000000f, 0f, 100000000f)]
        public void EffectiveFractionalRoundedAndZeroSpends_KeepNominalAmountAndFloatBalance(
            float initialBalance, float amount, float expectedBalance)
        {
            PrepareObservedWallet(initialBalance);
            _money.MaxMoney = 1f; // Existing overflow is spendable above the income cap.
            Assert.That(_money.CanSpend(amount), Is.True);
            Assert.That(_money.CanConfirmSpendNow(amount), Is.True);
#if MIRROR
            _money.isNetworked = true;
            _money.ClientMutationAuthority = NetworkAuthorityMode.None;
            Assert.That(Admit(1, amount, new NetworkConnectionToClient(3103)), Is.True);
            Assert.That(GetPrivate(_money, "_lastCmdTimePerConnection"), Is.Null);
            _money.isNetworked = false;
#endif

            MoneySpendResult result = _money.TrySpend(amount);

            Assert.That(result.Status, Is.EqualTo(MoneySpendStatus.Confirmed));
            Assert.That(result.Amount, Is.EqualTo(amount), "Receipts retain the nominal input, not a reconstructed delta.");
            Assert.That(result.BalanceBefore, Is.EqualTo(initialBalance));
            Assert.That(result.BalanceAfter, Is.EqualTo(expectedBalance));
            Assert.That(_money.money, Is.EqualTo(expectedBalance));
            Assert.That(_money.allMoney, Is.EqualTo(initialBalance));
            Assert.That(_money.levelMoney, Is.EqualTo(7f));
            Assert.That(_money.LastChangeMoneyValue, Is.EqualTo(-amount));
            Assert.That(_provider.GetFloat(_money.SaveKey), Is.EqualTo(expectedBalance));
            Assert.That(_persistenceEvents, Is.EqualTo(2));
        }

#if MIRROR
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void CommandBody_ZeroEffectiveSpendRejectsBeforePeerBudgetSyncAndEvents(
            bool trustedNullSender, bool existingBudget)
        {
            PrepareObservedWallet(100000000f);
            _money.isNetworked = true;
            _money.ClientMutationAuthority = NetworkAuthorityMode.None;
            NetworkConnectionToClient sender = trustedNullSender ? null : new NetworkConnectionToClient(3104);
            Dictionary<int, double> budget = existingBudget && sender != null
                ? new Dictionary<int, double> { { sender.connectionId, double.NegativeInfinity } }
                : null;
            double localBudgetTime = existingBudget && trustedNullSender ? double.MinValue : double.NegativeInfinity;
            SetPrivate(_money, "_lastCmdTimePerConnection", budget);
            SetPrivate(_money, "_lastCmdTime", localBudgetTime);
            SetPrivate(_money, "_syncCurrentMoney", -1234f);

            Assert.That(Admit(1, 1f, sender), Is.False);
            InvokeCommandBody(1, 1f, sender);

            AssertObservedWalletUnchanged(100000000f);
            Assert.That(GetPrivate(_money, "_syncCurrentMoney"), Is.EqualTo(-1234f));
            Assert.That(GetPrivate(_money, "_lastCmdTime"), Is.EqualTo(localBudgetTime));
            Assert.That(GetPrivate(_money, "_lastCmdTimePerConnection"), Is.SameAs(budget));
            if (budget != null)
            {
                Assert.That(budget.Count, Is.EqualTo(1));
                Assert.That(budget[sender.connectionId], Is.EqualTo(double.NegativeInfinity));
            }

            Assert.That(Admit(1, 5f, sender), Is.True, "A representable debit remains eligible for this sender.");
            MethodInfo limiter = typeof(Money).GetMethod("RateLimit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That((bool)limiter.Invoke(_money, new object[] { sender }), Is.False,
                "The rejected spend must leave the next eligible operation's window available.");
            // This invokes the woven server body locally; it is not remote admission or RPC-delivery evidence.
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Admission_NamedPoliciesRespectTheWalletOwnerForEveryClientOpcode(int opcode)
        {
            _money.isNetworked = true;
            _money.Add(100f);
            NetworkConnectionToClient owner = new(3101);
            NetworkConnectionToClient other = new(3102);
            NetworkIdentity identity = _root.GetComponent<NetworkIdentity>();
            PropertyInfo ownerProperty = typeof(NetworkIdentity).GetProperty(nameof(NetworkIdentity.connectionToClient));
            Assert.That(ownerProperty, Is.Not.Null);
            ownerProperty.SetValue(identity, owner);

            _money.ClientMutationAuthority = NetworkAuthorityMode.ServerOnly;
            Assert.That(Admit(opcode, 1f, owner), Is.False);
            Assert.That(Admit(opcode, 1f, other), Is.False);
            Assert.That(Admit(opcode, 1f, null), Is.True, "Trusted direct-server context keeps the helper contract.");

            _money.ClientMutationAuthority = NetworkAuthorityMode.None;
            Assert.That(Admit(opcode, 1f, owner), Is.True);
            Assert.That(Admit(opcode, 1f, other), Is.True);

            _money.ClientMutationAuthority = NetworkAuthorityMode.OwnerOnly;
            Assert.That(Admit(opcode, 1f, owner), Is.True);
            Assert.That(Admit(opcode, 1f, other), Is.False);
            Assert.That(Admit(opcode, 1f, null), Is.True);
            ownerProperty.SetValue(identity, null);
            Assert.That(Admit(opcode, 1f, owner), Is.False, "An unrelated player connection cannot own a scene wallet.");
            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_money.allMoney, Is.EqualTo(100f));
            Assert.That(_money.levelMoney, Is.Zero);
            Assert.That(GetPrivate(_money, "_lastCmdTimePerConnection"), Is.Null,
                "The pure admission stage must not allocate/consume a peer budget.");
        }

        [TestCase(7)]
        [TestCase(8)]
        [TestCase(255)]
        public void Admission_RefundAndUnknownOpcodesRejectEvenTrustedCallers(int opcode)
        {
            _money.isNetworked = true;
            foreach (NetworkAuthorityMode policy in new[]
                     { NetworkAuthorityMode.ServerOnly, NetworkAuthorityMode.OwnerOnly, NetworkAuthorityMode.None })
            {
                _money.ClientMutationAuthority = policy;
                Assert.That(Admit(opcode, 1f, null), Is.False);
            }

            Assert.That(_money.money, Is.Zero);
            Assert.That(_money.LastChangeMoneyValue, Is.Zero);
        }

        [Test]
        public void Admission_UnknownPolicyAndLocalOnlyWalletRejectBeforeTrustedAllowance()
        {
            _money.isNetworked = true;
            _money.ClientMutationAuthority = (NetworkAuthorityMode)99;
            Assert.That(Admit(0, 1f, null), Is.False);
            _money.ClientMutationAuthority = NetworkAuthorityMode.None;
            _money.isNetworked = false;
            Assert.That(Admit(0, 1f, null), Is.False);
            Assert.That(_money.money, Is.Zero);
            Assert.That(_money.allMoney, Is.Zero);
            Assert.That(GetPrivate(_money, "_lastCmdTimePerConnection"), Is.Null);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Admission_NonFinitePayloadRejectsEveryClientOpcode(float amount)
        {
            _money.isNetworked = true;
            _money.ClientMutationAuthority = NetworkAuthorityMode.None;
            for (int opcode = 0; opcode <= 6; opcode++)
            {
                Assert.That(Admit(opcode, amount, null), Is.False);
            }

            Assert.That(_money.money, Is.Zero);
            Assert.That(_money.allMoney, Is.Zero);
            Assert.That(_money.levelMoney, Is.Zero);
        }

        [TestCase(0, -1f)]
        [TestCase(1, -1f)]
        [TestCase(6, -1f)]
        [TestCase(1, 101f)]
        public void Admission_DepositSignAndSpendAffordabilityAreCheckedBeforeBudget(int opcode, float amount)
        {
            _money.Add(100f);
            _money.isNetworked = true;
            _money.ClientMutationAuthority = NetworkAuthorityMode.None;
            Assert.That(Admit(opcode, amount, null), Is.False);
            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_money.allMoney, Is.EqualTo(100f));
            Assert.That(GetPrivate(_money, "_lastCmdTimePerConnection"), Is.Null);
        }

        private bool Admit(int opcode, float amount, NetworkConnectionToClient sender)
        {
            Type opType = typeof(Money).GetNestedType("MoneyOp", BindingFlags.NonPublic);
            MethodInfo admission = typeof(Money).GetMethod("CanAcceptClientMutation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(admission, Is.Not.Null);
            return (bool)admission.Invoke(_money, new[] { Enum.ToObject(opType, (byte)opcode), amount, (object)sender });
        }

        private void InvokeCommandBody(int opcode, float amount, NetworkConnectionToClient sender)
        {
            MethodInfo commandBody = null;
            foreach (MethodInfo method in typeof(Money).GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name.StartsWith("UserCode_CmdMoneyOp__", StringComparison.Ordinal))
                {
                    commandBody = method;
                    break;
                }
            }

            Assert.That(commandBody, Is.Not.Null, "Actual MIRROR weaving is required for this server-body control.");
            Type opType = typeof(Money).GetNestedType("MoneyOp", BindingFlags.NonPublic);
            commandBody.Invoke(_money, new[] { Enum.ToObject(opType, (byte)opcode), amount, (object)sender });
        }
#endif

        private void PrepareObservedWallet(float balance)
        {
            SetPrivate(_money, "_persistMoney", true);
            _money.AddOverflow(balance);
            _money.SetLevelMoney(7f);
            _walletEvents = 0;
            _persistenceEvents = 0;
            _money.CurrentMoney.AddListener(_ => _walletEvents++);
            _money.AllMoney.AddListener(_ => _walletEvents++);
            _money.LevelMoney.AddListener(_ => _walletEvents++);
            _money.LastChangeMoney.AddListener(_ => _walletEvents++);
            _provider.OnKeyChanged += _ => _persistenceEvents++;
        }

        private void AssertObservedWalletUnchanged(float balance)
        {
            Assert.That(_money.money, Is.EqualTo(balance));
            Assert.That(_money.allMoney, Is.EqualTo(balance));
            Assert.That(_money.levelMoney, Is.EqualTo(7f));
            Assert.That(_money.LastChangeMoneyValue, Is.EqualTo(balance));
            Assert.That(_provider.GetFloat(_money.SaveKey), Is.EqualTo(balance));
            Assert.That(_provider.GetFloat(_money.SaveKey + nameof(Money.AllMoney)), Is.EqualTo(balance));
            Assert.That(_walletEvents, Is.Zero);
            Assert.That(_persistenceEvents, Is.Zero);
        }

        private static object GetPrivate(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void SetPrivate(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
