using Neo.Save;
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
            SaveProvider.SetProvider(new ThrowAwaySaveProvider());
            _go = new GameObject();
            _money = _go.AddComponent<Money>();
            // Emulate Start
            _money.SetMoney(0);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_go);
            SaveProvider.SetProvider(new PlayerPrefsSaveProvider());
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

        private sealed class ThrowAwaySaveProvider : ISaveProvider
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _store = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
#pragma warning disable CS0067
            public event System.Action OnDataSaved;
            public event System.Action OnDataLoaded;
            public event System.Action<string> OnKeyChanged;
#pragma warning restore CS0067

            public int GetInt(string key, int defaultValue = 0) =>
                _store.TryGetValue(key, out string s) && int.TryParse(s, out int v) ? v : defaultValue;

            public void SetInt(string key, int value)
            {
                _store[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f) =>
                _store.TryGetValue(key, out string s) && float.TryParse(s, out float v) ? v : defaultValue;

            public void SetFloat(string key, float value)
            {
                _store[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "") =>
                _store.TryGetValue(key, out string s) ? s : defaultValue;

            public void SetString(string key, string value)
            {
                _store[key] = value ?? "";
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false) =>
                _store.TryGetValue(key, out string s) && bool.TryParse(s, out bool v) ? v : defaultValue;

            public void SetBool(string key, bool value)
            {
                _store[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key) => _store.ContainsKey(key);

            public void DeleteKey(string key) => _store.Remove(key);

            public void DeleteAll() => _store.Clear();

            public void Save()
            {
            }

            public void Load()
            {
            }
        }
    }
}
