using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Level.Tests
{
    public class Audit20261023NeoLevelProgressTests
    {
        private ISaveProvider _previousProvider;
        private MemorySaveProvider _provider;
        private GameObject _root;
        private LevelManager _manager;
        private string _saveKey;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new MemorySaveProvider();
            SaveProvider.SetProvider(_provider);
            _saveKey = Guid.NewGuid().ToString("N");
            _root = new GameObject(nameof(Audit20261023NeoLevelProgressTests));
            _root.SetActive(false);
            _manager = _root.AddComponent<LevelManager>();
            _manager.OnChangeLevel = new UnityEvent<int>();
            _manager.OnChangeMap = new UnityEvent<int>();
            _manager.OnChangeMaxLevel = new UnityEvent<int>();
            SetField(_manager, "_saveKey", _saveKey);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            SaveProvider.SetProvider(_previousProvider);
        }

        [TestCase(-11, 5, 4)]
        [TestCase(int.MinValue, 3, 1)]
        [TestCase(int.MinValue, int.MaxValue, int.MaxValue - 1)]
        [TestCase(int.MaxValue, 2, 1)]
        [TestCase(int.MaxValue, int.MaxValue, 0)]
        [TestCase(8, 0, 0)]
        [TestCase(8, -3, 0)]
        public void GetLoopLevel_WrapsAcrossNegativeAndIntegerBoundaries(int level, int count, int expected)
        {
            Assert.That(LevelManager.GetLoopLevel(level, count), Is.EqualTo(expected));
        }

        [TestCase(-8, 4, 0)]
        [TestCase(20, 4, 3)]
        [TestCase(2, int.MinValue, 0)]
        public void SetLevel_ClampsFiniteSelectionAndEventPayload(int requested, int count, int expected)
        {
            ConfigureMaps(new Map { isInfinity = false, countLevels = count });
            int payload = -1;
            _manager.OnChangeLevel.AddListener(value => payload = value);

            _manager.SetLevel(requested);

            Assert.That(_manager.CurrentLevel, Is.EqualTo(expected));
            Assert.That(payload, Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(5)]
        public void SetLastLevel_SelectsStoredLoopFrontier(int progress)
        {
            SaveProvider.SetInt(ProgressKey(0), progress);
            ConfigureMaps(new Map { isLoopLevel = true, countLevels = 3 });

            _manager.SetLastLevel();

            Assert.That(_manager.CurrentLevel, Is.EqualTo(progress % 3));
            Assert.That(_manager.MaxLevel, Is.EqualTo(progress));
        }

        [Test]
        public void SaveLevel_AdvancesAndPersistsAcrossMultipleLoops()
        {
            ConfigureMaps(new Map { isLoopLevel = true, countLevels = 3 });
            List<int> progressEvents = new();
            _manager.OnChangeMaxLevel.AddListener(progressEvents.Add);

            for (int completion = 0; completion < 8; completion++)
            {
                _manager.SetLastLevel();
                Assert.That(_manager.CurrentLevel, Is.EqualTo(completion % 3));
                _manager.SaveLevel();
                Assert.That(_manager.MaxLevel, Is.EqualTo(completion + 1));
                Assert.That(SaveProvider.GetInt(ProgressKey(0)), Is.EqualTo(completion + 1));
            }

            Assert.That(progressEvents, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        }

        [Test]
        public void SaveLevel_LoopingMapWithoutLevelsDoesNotCreateProgress()
        {
            ConfigureMaps(new Map { isLoopLevel = true, countLevels = 0 });

            _manager.SaveLevel();

            Assert.That(_manager.MaxLevel, Is.Zero);
            Assert.That(SaveProvider.HasKey(ProgressKey(0)), Is.False);
        }

        [Test]
        public void SaveLevel_ReplayingEarlierFiniteLevelDoesNotAdvanceFrontier()
        {
            SaveProvider.SetInt(ProgressKey(0), 2);
            ConfigureMaps(new Map { isInfinity = false, countLevels = 4 });
            _manager.SetLevel(0);
            int events = 0;
            _manager.OnChangeMaxLevel.AddListener(value => events++);

            _manager.SaveLevel();

            Assert.That(_manager.MaxLevel, Is.EqualTo(2));
            Assert.That(SaveProvider.GetInt(ProgressKey(0)), Is.EqualTo(2));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void Map_LoadRepairsNegativeProgressBeforeNextSave()
        {
            SaveProvider.SetInt(ProgressKey(0), -9);
            Map map = new();

            map.Load(0, _saveKey);
            Assert.That(map.level, Is.Zero);
            map.SaveLevel();

            Assert.That(map.level, Is.EqualTo(1));
            Assert.That(SaveProvider.GetInt(ProgressKey(0)), Is.EqualTo(1));
        }

        [Test]
        public void Map_SaveFailureRestoresProgressAndRetryRecordsOneCompletion()
        {
            Map map = new();
            map.Load(0, _saveKey);
            _provider.RejectWrites = true;

            Assert.Throws<InvalidOperationException>(map.SaveLevel);
            Assert.That(map.level, Is.Zero);
            Assert.That(SaveProvider.HasKey(ProgressKey(0)), Is.False);

            _provider.RejectWrites = false;
            map.SaveLevel();

            Assert.That(map.level, Is.EqualTo(1));
            Assert.That(SaveProvider.GetInt(ProgressKey(0)), Is.EqualTo(1));
        }

        [Test]
        public void InfiniteProgress_AtIntegerLimitDoesNotWrapSelectionOrSave()
        {
            SaveProvider.SetInt(ProgressKey(0), int.MaxValue);
            ConfigureMaps(new Map());
            _manager.SetLastLevel();

            _manager.NextLevel();
            _manager.SaveLevel();

            Assert.That(_manager.CurrentLevel, Is.EqualTo(int.MaxValue));
            Assert.That(_manager.MaxLevel, Is.EqualTo(int.MaxValue));
            Assert.That(SaveProvider.GetInt(ProgressKey(0)), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void SingletonInitialization_AllowsNoButtonsAndSkipsEmptySlots()
        {
            SaveProvider.SetInt(ProgressKey(1), 2);
            ConfigureMaps(null, new Map { isInfinity = false, countLevels = 4 });
            SetField(_manager, "_lvlBtns", null);
            SetField(_manager, "_onAwakeNextMap", true);

            LevelManager resolved = LevelManager.I;
            _manager.SetLastLevel();

            Assert.That(resolved, Is.SameAs(_manager));
            Assert.That(_manager.MapId, Is.EqualTo(1));
            Assert.That(_manager.CurrentLevel, Is.EqualTo(2));
            _manager.SaveLevel();
            Assert.That(SaveProvider.GetInt(ProgressKey(1)), Is.EqualTo(3));
        }

        [Test]
        public void SetLastMap_SelectsLastConfiguredCompletedMapAndRejectsEmptySlot()
        {
            ConfigureMaps(null, new Map { isInfinity = false, countLevels = 0 }, null);
            List<int> selectedMaps = new();
            _manager.OnChangeMap.AddListener(selectedMaps.Add);

            Assert.That(_manager.GetLastIdMap(), Is.EqualTo(-1));
            _manager.SetLastMap();
            _manager.SetMapId(2);

            Assert.That(_manager.MapId, Is.EqualTo(1));
            Assert.That(_manager.Map, Is.Not.Null);
            Assert.That(selectedMaps, Is.EqualTo(new[] { 1 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompletingMap_AutomaticallySkipsCompletedOrEmptyIntermediateMap(bool emptySlot)
        {
            Map next = new() { isInfinity = false, countLevels = 2 };
            Map intermediate = emptySlot ? null : new Map { isInfinity = false, countLevels = 0 };
            ConfigureMaps(new Map { isInfinity = false, countLevels = 1 }, intermediate, next);
            SetField(_manager, "_onAwakeNextMap", true);
            List<int> selectedMaps = new();
            _manager.OnChangeMap.AddListener(selectedMaps.Add);

            _manager.SaveLevel();

            Assert.That(SaveProvider.GetInt(ProgressKey(0)), Is.EqualTo(1));
            Assert.That(_manager.MapId, Is.EqualTo(2));
            Assert.That(_manager.Map, Is.SameAs(next));
            Assert.That(selectedMaps, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void MapButtons_EmitPassedCurrentLockedStatesInLevelOrder()
        {
            SaveProvider.SetInt(ProgressKey(0), 1);
            ConfigureMaps(new Map { isInfinity = false, countLevels = 3 });
            LevelButton[] buttons = new LevelButton[3];
            int[] states = { -1, -1, -1 };
            int passed = 0;
            int current = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i] = CreateButton("Level " + i);
                buttons[i].OnChangeVisual = new UnityEvent<int>();
                buttons[i].OnChangeVisual.AddListener(value => states[index] = value);
                buttons[i].OnEnableVisual = new UnityEvent();
                buttons[i].OnEnableVisual.AddListener(() => passed++);
                buttons[i].OnCurrentVisual = new UnityEvent();
                buttons[i].OnCurrentVisual.AddListener(() => current++);
            }

            SetField(_manager, "_lvlBtns", buttons);
            _manager.SetMapId(0);

            Assert.That(states, Is.EqualTo(new[] { 2, 1, 0 }));
            Assert.That(passed, Is.EqualTo(1));
            Assert.That(current, Is.EqualTo(1));
            Assert.That(buttons[0].activ, Is.True);
            Assert.That(buttons[1].activ, Is.True);
            Assert.That(buttons[2].activ, Is.False);
        }

        [Test]
        public void LevelButton_OptionalVisualEntriesDoNotPreventStateAndEventUpdates()
        {
            LevelButton button = CreateButton("Optional visuals");
            GameObject closed = new("Closed");
            GameObject open = new("Open");
            closed.transform.SetParent(button.transform);
            open.transform.SetParent(button.transform);
            SetField(button, "_closes", new[] { null, closed });
            SetField(button, "_opens", new[] { open, null });
            button.OnChangeVisual = new UnityEvent<int>();
            int payload = -1;
            button.OnChangeVisual.AddListener(value => payload = value);

            button.SetVisual(0, 2);

            Assert.That(button.activ, Is.False);
            Assert.That(button.level, Is.EqualTo(2));
            Assert.That(closed.activeSelf, Is.True);
            Assert.That(open.activeSelf, Is.False);
            Assert.That(payload, Is.Zero);

            SetField(button, "_closes", null);
            SetField(button, "_opens", null);
            button.SetVisual(1, 3);
            Assert.That(button.activ, Is.True);
            Assert.That(button.level, Is.EqualTo(3));
            Assert.That(payload, Is.EqualTo(1));
        }

        [Test]
        public void LevelButton_CanBeBoundAfterAnUnboundClick()
        {
            ConfigureMaps(new Map { isInfinity = false, countLevels = 4 });
            LevelButton button = CreateButton("Late manager");
            button.activ = true;
            button.level = 2;

            button.Click();
            button.SetLevelManager(_manager);
            button.Click();

            Assert.That(_manager.CurrentLevel, Is.EqualTo(2));
        }

        private void ConfigureMaps(params Map[] maps)
        {
            SetField(_manager, "_maps", maps);
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i]?.Load(i, _saveKey);
            }
        }

        private string ProgressKey(int mapId)
        {
            return $"Map_{_saveKey}_{mapId}__level";
        }

        private LevelButton CreateButton(string name)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(_root.transform);
            return buttonObject.AddComponent<LevelButton>();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new();
            public bool RejectWrites { get; set; }
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _values.TryGetValue(key, out object value) && value is int number ? number : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                if (RejectWrites)
                {
                    throw new InvalidOperationException("The test provider rejected the write.");
                }

                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _values.TryGetValue(key, out object value) && value is float number ? number : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _values.TryGetValue(key, out object value) && value is bool flag ? flag : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _values.TryGetValue(key, out object value) && value is string text ? text : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key) => _values.ContainsKey(key);
            public void DeleteKey(string key) => _values.Remove(key);
            public void DeleteAll() => _values.Clear();
            public void Save() => OnDataSaved?.Invoke();
            public void Load() => OnDataLoaded?.Invoke();
        }
    }
}
