using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Money persistence and SaveProvider wiring (Edit Mode; no Play lifecycle).
    /// </summary>
    public sealed class MoneyPersistenceEditModeTests
    {
        private GameObject _go;
        private Money _money;
        private MemorySaveProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _provider = new MemorySaveProvider();
            SaveProvider.SetProvider(_provider);
            _go = new GameObject("MoneyTest");
            _money = _go.AddComponent<Money>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }

            SaveProvider.SetProvider(new PlayerPrefsSaveProvider());
        }

        [Test]
        public void Add_WritesFloatKeys_WhenPersistMoneyTrue()
        {
            SetPersistMoney(_money, true);
            _money.SetMoney(0);
            _money.AllMoney.Value = 0;

            _money.Add(42f);

            Assert.That(_provider.GetFloat("Money", -1f), Is.EqualTo(42f));
            Assert.That(_provider.GetFloat("Money" + nameof(Money.AllMoney), -1f), Is.EqualTo(42f));
        }

        [Test]
        public void Add_DoesNotWrite_WhenPersistMoneyFalse()
        {
            SetPersistMoney(_money, false);
            _money.SetMoney(100f);

            _money.Add(10f);

            Assert.That(_provider.Store.ContainsKey("Money"), Is.False);
        }

        [Test]
        public void SetMoney_PersistsBalance()
        {
            SetPersistMoney(_money, true);
            _money.SetMoney(333f);

            Assert.That(_provider.GetFloat("Money", 0f), Is.EqualTo(333f));
        }

        [Test]
        public void ClearSavedMoneyAndReset_RemovesKeysAndZerosRuntime_WhenPersistFalse()
        {
            SetPersistMoney(_money, false);
            SaveProvider.SetFloat("Money", 999f);
            SaveProvider.SetFloat("Money" + nameof(Money.AllMoney), 888f);
            _money.SetMoney(50f);

            _money.ClearSavedMoneyAndReset();

            Assert.That(SaveProvider.HasKey("Money"), Is.False);
            Assert.That(SaveProvider.HasKey("Money" + nameof(Money.AllMoney)), Is.False);
            Assert.That(_money.money, Is.EqualTo(0f));
            Assert.That(_money.allMoney, Is.EqualTo(0f));
        }

        [Test]
        public void ReloadBalanceFromSave_LoadsFromProvider()
        {
            SetPersistMoney(_money, true);
            SaveProvider.SetFloat("Money", 77f);
            SaveProvider.SetFloat("Money" + nameof(Money.AllMoney), 77f);

            _money.ReloadBalanceFromSave();

            Assert.That(_money.money, Is.EqualTo(77f));
            Assert.That(_money.allMoney, Is.EqualTo(77f));
        }

        private static void SetPersistMoney(Money money, bool value)
        {
            FieldInfo f = typeof(Money).GetField("_persistMoney",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(f, Is.Not.Null);
            f.SetValue(money, value);
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            public readonly Dictionary<string, string> Store = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
#pragma warning disable CS0067
            public event System.Action OnDataSaved;
            public event System.Action OnDataLoaded;
            public event System.Action<string> OnKeyChanged;
#pragma warning restore CS0067

            public int GetInt(string key, int defaultValue = 0)
            {
                return Store.TryGetValue(key, out string s) && int.TryParse(s, out int v) ? v : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                Store[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return Store.TryGetValue(key, out string s) && float.TryParse(s, out float v) ? v : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                Store[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return Store.TryGetValue(key, out string s) ? s : defaultValue;
            }

            public void SetString(string key, string value)
            {
                Store[key] = value ?? "";
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return Store.TryGetValue(key, out string s) && bool.TryParse(s, out bool v) ? v : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                Store[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key) => Store.ContainsKey(key);

            public void DeleteKey(string key)
            {
                Store.Remove(key);
            }

            public void DeleteAll() => Store.Clear();

            public void Save()
            {
            }

            public void Load()
            {
            }
        }
    }
}
