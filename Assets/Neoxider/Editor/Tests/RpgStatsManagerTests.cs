using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Rpg.Tests
{
    public class RpgStatsManagerTests
    {
        [Test]
        public void TakeDamage_ReducesHp()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject go = new("RpgStatsManager");
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();
            manager.EnsureInitialized();

            try
            {
                manager.ResetProfile();
                float before = manager.CurrentHp;
                float dealt = manager.TakeDamage(25f);

                Assert.That(dealt, Is.EqualTo(25f));
                Assert.That(manager.CurrentHp, Is.EqualTo(before - 25f));
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void Heal_IncreasesHp()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject go = new("RpgStatsManager");
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();
            manager.EnsureInitialized();

            try
            {
                manager.ResetProfile();
                manager.TakeDamage(50f);
                float healed = manager.Heal(30f);

                Assert.That(healed, Is.EqualTo(30f));
                Assert.That(manager.CurrentHp, Is.EqualTo(80f));
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void TakeDamage_WhenDead_ReturnsZero()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject go = new("RpgStatsManager");
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();
            manager.EnsureInitialized();

            try
            {
                manager.ResetProfile();
                manager.TakeDamage(1000f);
                Assert.That(manager.IsDead, Is.True);

                float dealt = manager.TakeDamage(100f);
                Assert.That(dealt, Is.EqualTo(0f));
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void SaveAndLoad_RestoresHpAndLevel()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject firstGo = new("RpgStatsManager_First");
            RpgStatsManager firstManager = firstGo.AddComponent<RpgStatsManager>();
            firstManager.SaveKey = "RpgTests.Persistence";
            firstManager.EnsureInitialized();

            firstManager.ResetProfile();
            firstManager.TakeDamage(30f);
            firstManager.SetLevel(5);
            firstManager.SaveProfile();

            RpgStatsManager.DestroyInstance();

            GameObject secondGo = new("RpgStatsManager_Second");
            RpgStatsManager secondManager = secondGo.AddComponent<RpgStatsManager>();
            secondManager.SaveKey = "RpgTests.Persistence";
            secondManager.EnsureInitialized();
            secondManager.LoadProfile();

            try
            {
                Assert.That(secondManager.CurrentHp, Is.EqualTo(70f));
                Assert.That(secondManager.MaxHp, Is.EqualTo(100f));
                Assert.That(secondManager.Level, Is.EqualTo(5));
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void AutoSave_DisabledByDefault_DoesNotPersistAfterDamage()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject go = new("RpgStatsManager");
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();
            manager.SaveKey = "RpgTests.AutoSave.Disabled";
            manager.EnsureInitialized();

            try
            {
                manager.ResetProfile();
                provider.DeleteKey(manager.SaveKey);
                manager.TakeDamage(10f);

                Assert.That(provider.HasKey(manager.SaveKey), Is.False);
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void AutoSave_WhenEnabled_PersistsAfterDamage()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject go = new("RpgStatsManager");
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();
            manager.SaveKey = "RpgTests.AutoSave.Enabled";
            manager.AutoSave = true;
            manager.EnsureInitialized();

            try
            {
                manager.ResetProfile();
                provider.DeleteKey(manager.SaveKey);
                manager.TakeDamage(10f);

                Assert.That(provider.HasKey(manager.SaveKey), Is.True);
                string json = provider.GetString(manager.SaveKey);
                Assert.That(json, Does.Contain("_currentHp"));
                Assert.That(json, Does.Contain("90"), "Profile should contain HP 90 after 10 damage.");
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void SaveProfile_FlushesProvider()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject go = new("RpgStatsManager");
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();
            manager.SaveKey = "RpgTests.Flush";
            manager.EnsureInitialized();

            try
            {
                manager.ResetProfile();

                Assert.That(provider.SaveCallCount, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void TrySpendResource_WhenNoProvider_ReturnsFalse()
        {
            GameObject go = new("RpgStatsManager");
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();

            try
            {
                manager.EnsureInitialized();
                bool ok = manager.TrySpendResource(RpgResourceId.Mana, 10f, out string reason);

                Assert.That(ok, Is.False);
                Assert.That(reason, Is.Not.Null.And.Not.Empty);
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        [Test]
        public void WithHealthComponent_DelegatesHpAndTrySpend()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            GameObject go = new("RpgStatsManager");
            HealthComponent health = go.AddComponent<HealthComponent>();
            RpgStatsManager manager = go.AddComponent<RpgStatsManager>();
            SetPrivateField(manager, "_healthProvider", health);

            try
            {
                manager.EnsureInitialized();
                manager.ResetProfile();

                Assert.That(manager.CurrentHp, Is.EqualTo(100f));
                Assert.That(manager.MaxHp, Is.EqualTo(100f));

                float dealt = manager.TakeDamage(25f);
                Assert.That(dealt, Is.EqualTo(25f));
                Assert.That(manager.CurrentHp, Is.EqualTo(75f));

                Assert.That(manager.TrySpendResource(RpgResourceId.Mana, 25f, out string reason), Is.True);
                Assert.That(manager.TrySpendResource(RpgResourceId.Mana, 100f, out reason), Is.False);
            }
            finally
            {
                RpgStatsManager.DestroyInstance();
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo f = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(f, Is.Not.Null, $"Field '{fieldName}' not found.");
            f.SetValue(target, value);
        }

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
            public int SaveCallCount { get; private set; }
            public int LoadCallCount { get; private set; }

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _values.TryGetValue(key, out object value) && value is int intValue ? intValue : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _values.TryGetValue(key, out object value) && value is float floatValue
                    ? floatValue
                    : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _values.TryGetValue(key, out object value) && value is string stringValue
                    ? stringValue
                    : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _values.TryGetValue(key, out object value) && value is bool boolValue ? boolValue : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return _values.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll()
            {
                _values.Clear();
            }

            public void Save()
            {
                SaveCallCount++;
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                LoadCallCount++;
                OnDataLoaded?.Invoke();
            }
        }
    }
}
