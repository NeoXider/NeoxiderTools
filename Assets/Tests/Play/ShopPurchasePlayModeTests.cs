using System.Collections;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Shop purchase flow requires Play Mode so <see cref="Shop"/> receives Start() and IMoneySpend wiring.
    /// </summary>
    public sealed class ShopPurchasePlayModeTests
    {
        [TearDown]
        public void RestoreDefaultSaveProvider()
        {
            SaveProvider.SetProvider(new PlayerPrefsSaveProvider());
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _store = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event System.Action OnDataSaved;
            public event System.Action OnDataLoaded;
            public event System.Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _store.TryGetValue(key, out string s) && int.TryParse(s, out int v) ? v : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _store[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f) => defaultValue;
            public void SetFloat(string key, float value)
            {
            }

            public string GetString(string key, string defaultValue = "") => defaultValue;
            public void SetString(string key, string value)
            {
            }

            public bool GetBool(string key, bool defaultValue = false) => defaultValue;
            public void SetBool(string key, bool value)
            {
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

        private sealed class FakeMoney : MonoBehaviour, IMoneySpend
        {
            public bool Spend(float count) => true;
        }

        [UnityTest]
        public IEnumerator Buy_FreeItem_SelectsWithoutSpend()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            var root = new GameObject("ShopRoot");
            root.SetActive(false);

            var shop = root.AddComponent<Shop>();
            var child = new GameObject("ShopItem");
            child.transform.SetParent(root.transform);
            ShopItem shopItem = child.AddComponent<ShopItem>();

            var walletGo = new GameObject("Wallet");
            walletGo.AddComponent<FakeMoney>();

            typeof(Shop).GetField("_prices", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(shop, new[] { 0 });
            typeof(Shop).GetField("_shopItems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(shop, new[] { shopItem });
            typeof(Shop).GetField("_useSetItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(shop, false);

            shop.moneySpendSource = walletGo;

            root.SetActive(true);

            yield return null;

            shop.Buy(0);

            Assert.That(shop.Id, Is.EqualTo(0));
            Assert.That(shop.PreviewId, Is.EqualTo(0));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(walletGo);
        }
    }
}
