using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the wallet mutation contract: the level payout goes through the shared add path
    ///     (cap, lifetime total, last change), the public reload notifies reactive subscribers, and
    ///     <see cref="Money.AddOverflow"/> is wired into the unified network dispatch.
    /// </summary>
    [TestFixture]
    public sealed class AuditFixesMoneyTests
    {
        private GameObject _go;
        private Money _money;

        [SetUp]
        public void SetUp()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());
            _go = new GameObject("AuditFixesMoney");
            _money = _go.AddComponent<Money>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }

            SaveProvider.SetProvider(new PlayerPrefsSaveProvider());
        }

        [Test]
        public void SetMoneyForLevel_ClampsToMaxMoneyAndFeedsTotals()
        {
            _money.MaxMoney = 500f;
            _money.SetMoney(480f);
            _money.AllMoney.Value = 0f;
            _money.AddLevelMoney(100f);

            float payout = _money.SetMoneyForLevel();

            Assert.That(payout, Is.EqualTo(100f).Within(0.0001f));
            Assert.That(_money.money, Is.EqualTo(500f).Within(0.0001f),
                "The level payout must respect the MaxMoney cap, like Add().");
            Assert.That(_money.allMoney, Is.EqualTo(20f).Within(0.0001f),
                "The applied part of the level payout must grow lifetime AllMoney.");
            Assert.That(_money.LastChangeMoneyValue, Is.EqualTo(20f).Within(0.0001f),
                "LastChangeMoney must hold the delta actually applied.");
            Assert.That(_money.levelMoney, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ReloadBalanceFromSave_NotifiesCurrentMoneySubscribers()
        {
            SaveProvider.SetFloat("Money", 77f);
            SaveProvider.SetFloat("Money" + nameof(Money.AllMoney), 90f);
            var observed = new List<float>();
            _money.CurrentMoney.AddListener(observed.Add);

            _money.ReloadBalanceFromSave();

            Assert.That(observed, Is.EqualTo(new[] { 77f }),
                "A public reload must notify CurrentMoney subscribers (shop gating, HUD counters).");
            Assert.That(_money.money, Is.EqualTo(77f).Within(0.0001f));
            Assert.That(_money.allMoney, Is.EqualTo(90f).Within(0.0001f));
        }

#if MIRROR
        [Test]
        public void AddOverflow_IsWiredIntoTheNetworkDispatch()
        {
            Type opType = typeof(Money).GetNestedType("MoneyOp", BindingFlags.NonPublic);
            Assert.That(opType, Is.Not.Null, "Money.MoneyOp not found.");
            Assert.That(Enum.GetNames(opType), Contains.Item(nameof(Money.AddOverflow)),
                "MoneyOp has no AddOverflow member, so AddOverflow can never reach Cmd/Rpc dispatch.");

            MethodInfo executeOp = typeof(Money).GetMethod(
                "ExecuteOp",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(executeOp, Is.Not.Null, "Money.ExecuteOp not found.");

            _money.MaxMoney = 5f;
            executeOp.Invoke(_money, new[] { Enum.Parse(opType, nameof(Money.AddOverflow)), (object)10f });

            Assert.That(_money.money, Is.EqualTo(10f).Within(0.0001f),
                "The dispatched AddOverflow op must apply the uncapped add.");
        }
#endif

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, string> _store = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
#pragma warning disable CS0067
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;
#pragma warning restore CS0067

            public int GetInt(string key, int defaultValue = 0)
            {
                return _store.TryGetValue(key, out string s) && int.TryParse(s, out int v) ? v : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _store[key] = value.ToString(CultureInfo.InvariantCulture);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _store.TryGetValue(key, out string s)
                       && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                    ? v
                    : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _store[key] = value.ToString(CultureInfo.InvariantCulture);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _store.TryGetValue(key, out string s) ? s : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _store[key] = value ?? string.Empty;
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _store.TryGetValue(key, out string s) && bool.TryParse(s, out bool v) ? v : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _store[key] = value.ToString();
            }

            public bool HasKey(string key)
            {
                return _store.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _store.Remove(key);
            }

            public void DeleteAll()
            {
                _store.Clear();
            }

            public void Save()
            {
            }

            public void Load()
            {
            }
        }
    }
}
