using System;
using System.Collections.Generic;
using NUnit.Framework;
using Neo.Rpg;
using Neo.Save;
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

            try
            {
                firstManager.ResetProfile();
                firstManager.TakeDamage(30f);
                firstManager.SetLevel(5);
                firstManager.SaveProfile();

                RpgStatsManager.DestroyInstance();

                GameObject secondGo = new("RpgStatsManager_Second");
                RpgStatsManager secondManager = secondGo.AddComponent<RpgStatsManager>();
                secondManager.SaveKey = "RpgTests.Persistence";
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
            finally
            {
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

            try
            {
                manager.ResetProfile();
                provider.DeleteKey(manager.SaveKey);
                manager.TakeDamage(10f);

                Assert.That(provider.HasKey(manager.SaveKey), Is.True);
                Assert.That(provider.GetString(manager.SaveKey), Does.Contain("\"_currentHp\": 90.0"));
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

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;
            public int SaveCallCount { get; private set; }
            public int LoadCallCount { get; private set; }

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
                return _values.TryGetValue(key, out object value) && value is float floatValue ? floatValue : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _values.TryGetValue(key, out object value) && value is string stringValue ? stringValue : defaultValue;
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

            public bool HasKey(string key) => _values.ContainsKey(key);

            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll() => _values.Clear();

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
