using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Level;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    // SaveProvider is shared: run through the coordinator's exclusive test runner with exact cleanup.
    public sealed class Audit20261023NeoCoreLevelComponentsLifecycleTests
    {
        private const string SaveKey = "Neo.Tests.Audit20261023NeoCoreLevelComponents.Lifecycle";
        private ISaveProvider _previousProvider;
        private LifecycleSaveProvider _provider;
        private GameObject _host;
        private LevelComponent _component;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new LifecycleSaveProvider();
            SaveProvider.SetProvider(_provider);
            _host = new GameObject(nameof(Audit20261023NeoCoreLevelComponentsLifecycleTests));
            _host.SetActive(false);
            _component = _host.AddComponent<LevelComponent>();
            Configure("_saveKey", SaveKey);
            Configure("_loadOnAwake", true);
            Configure("_autoSave", true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                if (_component != null)
                {
                    Configure("_autoSave", false);
                }

                if (_host != null)
                {
                    Object.Destroy(_host);
                }

                yield return null;
            }
            finally
            {
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [UnityTest]
        public IEnumerator DisableThenEnable_SavesCurrentProgressWithoutReloadingTheOldProfile()
        {
            LevelProfileData initialProfile = new() { TotalXp = 250, CurrentLevel = 3, MaxLevel = 0 };
            string initialJson = JsonUtility.ToJson(initialProfile);
            _provider.SetString(SaveKey, initialJson);
            int loadedCount = 0;
            int savedCount = 0;
            _component.OnProfileLoaded.AddListener(() => loadedCount++);
            _component.OnProfileSaved.AddListener(() => savedCount++);

            // Drive real Awake/OnDisable through activation; do not invoke private lifecycle methods.
            _host.SetActive(true);
            yield return null;

            Assert.That(loadedCount, Is.EqualTo(1));
            Assert.That(_provider.ReadCount, Is.EqualTo(1));
            Assert.That(_component.TotalXp, Is.EqualTo(250));
            Assert.That(_component.Level, Is.EqualTo(3));
            Assert.That(_provider.SaveCount, Is.Zero);

            _component.AddXp(25);
            _host.SetActive(false);
            yield return null;

            Assert.That(_provider.SaveCount, Is.EqualTo(1));
            Assert.That(savedCount, Is.EqualTo(1));
            LevelProfileData firstSave = JsonUtility.FromJson<LevelProfileData>(_provider.LastSavedJson);
            Assert.That(firstSave.TotalXp, Is.EqualTo(275));
            Assert.That(firstSave.CurrentLevel, Is.EqualTo(3));
            Assert.That(firstSave.MaxLevel, Is.Zero);

            // A second initial load would now visibly roll progress back, even if notifications looked normal.
            _provider.SetString(SaveKey, initialJson);
            _host.SetActive(true);
            yield return null;

            Assert.That(loadedCount, Is.EqualTo(1));
            Assert.That(_provider.ReadCount, Is.EqualTo(1));
            Assert.That(_component.TotalXp, Is.EqualTo(275));
            Assert.That(_component.LevelStateValue, Is.EqualTo(3));
            Assert.That(_component.XpStateValue, Is.EqualTo(275));
            Assert.That(_component.XpToNextLevelStateValue, Is.EqualTo(25));
            Assert.That(_provider.SaveCount, Is.EqualTo(1));

            _host.SetActive(false);
            yield return null;

            Assert.That(_provider.SaveCount, Is.EqualTo(2));
            Assert.That(savedCount, Is.EqualTo(2));
            LevelProfileData secondSave = JsonUtility.FromJson<LevelProfileData>(_provider.LastSavedJson);
            Assert.That(secondSave.TotalXp, Is.EqualTo(275));
            Assert.That(secondSave.CurrentLevel, Is.EqualTo(3));
        }

        private void Configure(string name, object value)
        {
            // Configure existing Inspector inputs only; assertions observe public state and persisted output.
            FieldInfo field = typeof(LevelComponent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_component, value);
        }

        private sealed class LifecycleSaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public int ReadCount { get; private set; }
            public int SaveCount { get; private set; }
            public string LastSavedJson { get; private set; }
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public string GetString(string key, string defaultValue = "")
            {
                ReadCount++;
                return _values.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public void Save()
            {
                SaveCount++;
                _values.TryGetValue(SaveKey, out string value);
                LastSavedJson = value;
                OnDataSaved?.Invoke();
            }

            public void Load() => OnDataLoaded?.Invoke();
            public bool HasKey(string key) => _values.ContainsKey(key);
            public void DeleteKey(string key) => _values.Remove(key);
            public void DeleteAll() => _values.Clear();
            public int GetInt(string key, int defaultValue = 0) => throw new NotSupportedException();
            public void SetInt(string key, int value) => throw new NotSupportedException();
            public float GetFloat(string key, float defaultValue = 0f) => throw new NotSupportedException();
            public void SetFloat(string key, float value) => throw new NotSupportedException();
            public bool GetBool(string key, bool defaultValue = false) => throw new NotSupportedException();
            public void SetBool(string key, bool value) => throw new NotSupportedException();
        }
    }
}
