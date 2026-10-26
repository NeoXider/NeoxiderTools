using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Level;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.EditMode
{
    public sealed class Audit20261023NeoCoreLevelComponentsTests
    {
        private const string SaveKey = "Neo.Tests.Audit20261023NeoCoreLevelComponents";
        private ISaveProvider _previousProvider;
        private ProfileSaveProvider _provider;
        private GameObject _host;
        private LevelComponent _component;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new ProfileSaveProvider();
            SaveProvider.SetProvider(_provider);
            _host = new GameObject(nameof(Audit20261023NeoCoreLevelComponentsTests));
            _host.SetActive(false);
            _component = _host.AddComponent<LevelComponent>();
            Configure("_saveKey", SaveKey);
            Configure("_autoSave", false);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Object.DestroyImmediate(_host);
            }
            finally
            {
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void EnsureInitialized_LoadListenerReadsCurrentState_WithoutLoadingAgain(bool saved, bool malformed)
        {
            Configure("_startXp", 250);
            if (saved)
            {
                StoreProfile(250, 3, 0);
            }
            else if (malformed)
            {
                _provider.SetString(SaveKey, "{invalid json");
            }

            int loadedCount = 0;
            int[] observed = null;
            _component.OnProfileLoaded.AddListener(() =>
            {
                loadedCount++;
                // Bound re-entry so the old implementation fails an assertion instead of overflowing the stack.
                if (loadedCount == 1)
                {
                    observed = new[]
                    {
                        _component.Level, _component.TotalXp, _component.XpToNextLevel,
                        _component.LevelStateValue, _component.XpStateValue, _component.XpToNextLevelStateValue
                    };
                }
            });

            _component.EnsureInitialized();
            _component.EnsureInitialized();

            Assert.That(loadedCount, Is.EqualTo(1));
            Assert.That(_provider.ReadCount, Is.EqualTo(1));
            Assert.That(observed, Is.EqualTo(new[] { 3, 250, 50, 3, 250, 50 }));
        }

        [Test]
        public void EnsureInitialized_ReactiveListenerReadsLevel_WithoutRepeatingInitialization()
        {
            Configure("_loadOnAwake", false);
            Configure("_startXp", 250);
            int notificationCount = 0;
            int observedLevel = 0;
            _component.LevelState.AddListener(level =>
            {
                notificationCount++;
                if (notificationCount == 1)
                {
                    observedLevel = _component.Level;
                }
            });

            _component.EnsureInitialized();

            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(observedLevel, Is.EqualTo(3));
            Assert.That(_component.XpStateValue, Is.EqualTo(250));
            Assert.That(_provider.ReadCount, Is.Zero);
        }

        [Test]
        public void EnsureInitialized_WhenProviderReadFails_CanRetryAndLoadProgress()
        {
            StoreProfile(250, 3, 0);
            _provider.FailNextRead = true;

            Assert.Throws<InvalidOperationException>(() => _component.EnsureInitialized());
            _component.EnsureInitialized();

            Assert.That(_provider.ReadCount, Is.EqualTo(2));
            Assert.That(_component.Level, Is.EqualTo(3));
            Assert.That(_component.TotalXp, Is.EqualTo(250));
            Assert.That(_component.XpStateValue, Is.EqualTo(250));
        }

        [Test]
        public void GetProfileSnapshot_BeforeAwake_IncludesAutomaticallyLoadedProgress()
        {
            StoreProfile(250, 3, 6);

            LevelProfileData snapshot = _component.GetProfileSnapshot();

            Assert.That(snapshot.TotalXp, Is.EqualTo(250));
            Assert.That(snapshot.CurrentLevel, Is.EqualTo(3));
            Assert.That(snapshot.MaxLevel, Is.EqualTo(6));
            Assert.That(_component.LevelStateValue, Is.EqualTo(snapshot.CurrentLevel));
            Assert.That(_provider.ReadCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetProgress_BeforeAwake_IsNotOverwrittenByDeferredAutoLoad()
        {
            StoreProfile(750, 8, 0);
            Configure("_startXp", 50);

            _component.ResetProgress();

            Assert.That(_component.TotalXp, Is.EqualTo(50));
            Assert.That(_component.Level, Is.EqualTo(1));
            Assert.That(_component.XpStateValue, Is.EqualTo(50));
            Assert.That(_component.GetProfileSnapshot().TotalXp, Is.EqualTo(50));
            Assert.That(_provider.ReadCount, Is.EqualTo(1));
        }

        [TestCase(3, 0, 8)]
        [TestCase(3, 10, 8)]
        [TestCase(10, 3, 2)]
        public void Load_DirectLevel_UsesIncomingCapAndPersistsTheRestoredProfile(
            int previousCap, int savedCap, int savedLevel)
        {
            Configure("_useXp", false);
            Configure("_loadOnAwake", false);
            Configure("_hasMaxLevel", true);
            Configure("_maxLevel", previousCap);
            Configure("_startLevel", previousCap);
            _component.EnsureInitialized();
            StoreProfile(17, savedLevel, savedCap);
            int levelChangeCount = 0;
            _component.OnLevelUp.AddListener(level => levelChangeCount++);

            _component.Load();
            _component.Save();
            LevelProfileData persisted = JsonUtility.FromJson<LevelProfileData>(_provider.GetString(SaveKey));

            Assert.That(_component.Level, Is.EqualTo(savedLevel));
            Assert.That(_component.LevelStateValue, Is.EqualTo(savedLevel));
            Assert.That(_component.MaxLevel, Is.EqualTo(savedCap));
            Assert.That(_component.TotalXp, Is.EqualTo(17));
            Assert.That(persisted.CurrentLevel, Is.EqualTo(savedLevel));
            Assert.That(persisted.TotalXp, Is.EqualTo(17));
            Assert.That(persisted.MaxLevel, Is.EqualTo(savedCap));
            Assert.That(_provider.SaveCount, Is.EqualTo(1));
            Assert.That(levelChangeCount, Is.Zero, "Restoring a profile must not publish intermediate clamps.");

            _component.SetLevel(1);
            Assert.That(levelChangeCount, Is.EqualTo(1), "Level notifications must remain connected after restore.");
        }

        [Test]
        public void ResetProgress_DirectLevel_RestoresConfiguredLevelAfterLoadingALowerCap()
        {
            Configure("_useXp", false);
            Configure("_hasMaxLevel", true);
            Configure("_maxLevel", 10);
            Configure("_startLevel", 8);
            Configure("_startXp", 23);
            StoreProfile(17, 3, 3);
            _component.EnsureInitialized();

            _component.ResetProgress();

            Assert.That(_component.Level, Is.EqualTo(8));
            Assert.That(_component.MaxLevel, Is.EqualTo(10));
            Assert.That(_component.TotalXp, Is.EqualTo(23));
            Assert.That(_component.LevelStateValue, Is.EqualTo(8));
            Assert.That(_component.XpStateValue, Is.EqualTo(23));
        }

        [Test]
        public void SetLevel_LevelObserversSeeTheSynchronizedXp()
        {
            Configure("_loadOnAwake", false);
            _component.EnsureInitialized();
            List<int[]> reactiveSnapshots = new();
            int[] levelEventSnapshot = null;
            _component.LevelState.AddListener(level => reactiveSnapshots.Add(new[]
            {
                level, _component.XpStateValue, _component.XpToNextLevelStateValue
            }));
            _component.OnLevelUp.AddListener(level => levelEventSnapshot = new[]
            {
                level, _component.XpStateValue, _component.XpToNextLevelStateValue
            });

            _component.SetLevel(5);

            Assert.That(levelEventSnapshot, Is.EqualTo(new[] { 5, 400, 100 }));
            Assert.That(reactiveSnapshots, Is.Not.Empty);
            foreach (int[] snapshot in reactiveSnapshots)
            {
                Assert.That(snapshot, Is.EqualTo(new[] { 5, 400, 100 }));
            }
        }

        [TestCase(25, 1, 75)]
        [TestCase(250, 3, 50)]
        public void AddXp_XpObserversSeeTheSynchronizedLevelAndRemainingXp(int amount, int level, int remainingXp)
        {
            Configure("_loadOnAwake", false);
            _component.EnsureInitialized();
            List<int[]> reactiveSnapshots = new();
            int[] xpEventSnapshot = null;
            _component.XpState.AddListener(xp => reactiveSnapshots.Add(new[]
            {
                _component.LevelStateValue, xp, _component.XpToNextLevelStateValue
            }));
            _component.OnXpGained.AddListener(() => xpEventSnapshot = new[]
            {
                _component.LevelStateValue, _component.XpStateValue, _component.XpToNextLevelStateValue
            });

            _component.AddXp(amount);

            int[] expected = { level, amount, remainingXp };
            Assert.That(xpEventSnapshot, Is.EqualTo(expected));
            Assert.That(reactiveSnapshots, Is.Not.Empty);
            foreach (int[] snapshot in reactiveSnapshots)
            {
                Assert.That(snapshot, Is.EqualTo(expected));
            }
        }

        private void Configure(string name, object value)
        {
            // Reflection configures existing Inspector-only inputs; all assertions use public behavior.
            FieldInfo field = typeof(LevelComponent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_component, value);
        }

        private void StoreProfile(int totalXp, int level, int maxLevel)
        {
            LevelProfileData data = new() { TotalXp = totalXp, CurrentLevel = level, MaxLevel = maxLevel };
            _provider.SetString(SaveKey, JsonUtility.ToJson(data));
        }

        private sealed class ProfileSaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, string> _profiles = new(StringComparer.Ordinal);
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public int ReadCount { get; private set; }
            public int SaveCount { get; private set; }
            public bool FailNextRead { get; set; }
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public string GetString(string key, string defaultValue = "")
            {
                ReadCount++;
                if (FailNextRead)
                {
                    FailNextRead = false;
                    throw new InvalidOperationException("Simulated provider read failure.");
                }

                return _profiles.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _profiles[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public void Save()
            {
                SaveCount++;
                OnDataSaved?.Invoke();
            }

            public void Load() => OnDataLoaded?.Invoke();
            public bool HasKey(string key) => _profiles.ContainsKey(key);
            public void DeleteKey(string key) => _profiles.Remove(key);
            public void DeleteAll() => _profiles.Clear();
            public int GetInt(string key, int defaultValue = 0) => throw new NotSupportedException();
            public void SetInt(string key, int value) => throw new NotSupportedException();
            public float GetFloat(string key, float defaultValue = 0f) => throw new NotSupportedException();
            public void SetFloat(string key, float value) => throw new NotSupportedException();
            public bool GetBool(string key, bool defaultValue = false) => throw new NotSupportedException();
            public void SetBool(string key, bool value) => throw new NotSupportedException();
        }
    }
}
