using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Editor.Tests;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Save.Tests
{
    /// <summary>
    ///     Regressions for <see cref="GlobalSave" /> staging before any read and for the automatic
    ///     <see cref="SaveManager" /> flushes raised together when a mobile app goes to the background.
    /// </summary>
    public sealed class SaveLifecycleRegressionTests
    {
        private const string GlobalKey = "SavesData";

        private Audit20261023NeoEditorTestsSaveScope _saveScope;
        private FieldInfo _globalDataField;
        private object _previousGlobalData;
        private bool _previousGlobalReady;
        private readonly List<GameObject> _objects = new();

        [SetUp]
        public void SetUp()
        {
            _saveScope = new Audit20261023NeoEditorTestsSaveScope();
            _globalDataField = typeof(GlobalSave).GetField("_data", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(_globalDataField, Is.Not.Null);
            _previousGlobalData = _globalDataField.GetValue(null);
            _previousGlobalReady = GlobalSave.IsReady;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }

                _objects.Clear();
                _globalDataField.SetValue(null, _previousGlobalData);
                GlobalSave.IsReady = _previousGlobalReady;
            }
            finally
            {
                _saveScope?.Dispose();
                _saveScope = null;
            }
        }

        [Test]
        public void SaveProviderSettingsComponent_AwakesBeforeEverySaveReader()
        {
            DefaultExecutionOrder order = (DefaultExecutionOrder)Attribute.GetCustomAttribute(
                typeof(SaveProviderSettingsComponent), typeof(DefaultExecutionOrder));

            Assert.That(order, Is.Not.Null, "without an order SaveManager may load from the previous provider");
            Assert.That(order.order, Is.EqualTo(SaveProviderSettingsComponent.ExecutionOrder));
            DefaultExecutionOrder settingsOrder = (DefaultExecutionOrder)Attribute.GetCustomAttribute(
                typeof(Neo.Settings.GameSettingsComponent), typeof(DefaultExecutionOrder));
            Assert.That(order.order, Is.LessThan(settingsOrder.order), "GameSettingsComponent loads in its Awake");
            Assert.That(order.order, Is.LessThan(0), "SaveManager and other readers use the default order 0");
        }

        [Test]
        public void SaveProviderSettingsComponent_RunsBeforeEveryOtherPackageComponent()
        {
            List<string> notLater = new();
            int assemblies = 0;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                // WHY: the docs promise the earliest Awake among package runtime components; test and demo helpers
                // may use their own orders.
                bool packageRuntime = name.StartsWith("Neo.", StringComparison.Ordinal)
                                      && !name.Contains(".Tests")
                                      && !name.StartsWith("Neo.Demo", StringComparison.Ordinal);
                if (!packageRuntime)
                {
                    continue;
                }

                assemblies++;
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                foreach (Type type in types)
                {
                    if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type)
                                     || typeof(SaveProviderSettingsComponent).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    DefaultExecutionOrder order =
                        (DefaultExecutionOrder)Attribute.GetCustomAttribute(type, typeof(DefaultExecutionOrder));
                    if (order != null && order.order <= SaveProviderSettingsComponent.ExecutionOrder)
                    {
                        notLater.Add(type.FullName + " (" + order.order + ")");
                    }
                }
            }

            Assert.That(assemblies, Is.GreaterThan(1), "the package runtime assemblies must be loaded");
            Assert.That(notLater, Is.Empty,
                "a component that awakes with or before the provider switch may read the previous provider");
        }

        [Test]
        public void GlobalSave_SaveProgressBeforeAnyRead_KeepsTheStoredProfile()
        {
            const string stored = "{\"marker\":1}";
            SaveProvider.SetString(GlobalKey, stored);
            _globalDataField.SetValue(null, null);

            GlobalSave.SaveProgress();

            Assert.That(SaveProvider.GetString(GlobalKey), Is.EqualTo(stored),
                "saving before the data was loaded must not overwrite the profile with an empty string");
        }

        [Test]
        public void GlobalSave_AssigningNull_StagesAFreshObject()
        {
            SaveProvider.SetString(GlobalKey, "{\"marker\":1}");

            GlobalSave.data = null;

            Assert.That(GlobalSave.data, Is.Not.Null);
            Assert.That(SaveProvider.GetString(GlobalKey), Is.EqualTo(JsonUtility.ToJson(new GlobalData())));
        }

        [Test]
        public void SaveManager_FocusLossThenPauseInOneFrame_PersistsDataStagedBetweenThem()
        {
            CountingProvider provider = new CountingProvider();
            SaveProvider.SetProvider(provider);
            GameObject host = new GameObject("SaveLifecycleRegressionTests.SaveManager");
            _objects.Add(host);
            SaveManager manager = host.AddComponent<SaveManager>();

            Invoke(manager, "OnApplicationFocus", false);
            Assert.That(provider.SaveCount, Is.EqualTo(1), "focus loss flushes");
            Assert.That(provider.HasUnflushedChanges, Is.False);

            // WHY: stands in for another component's OnApplicationPause handler (for example the deferred settings
            // flush) that stages data after SaveManager's focus-loss flush and before its pause flush.
            SaveProvider.SetString("SaveLifecycleRegressionTests.StagedOnPause", "value");
            Assert.That(provider.HasUnflushedChanges, Is.True);

            Invoke(manager, "OnApplicationPause", true);
            Assert.That(provider.SaveCount, Is.EqualTo(2), "the pause flush in the same frame must not be skipped");
            Assert.That(provider.HasUnflushedChanges, Is.False, "data staged between focus loss and pause is flushed");
        }

        [Test]
        public void SaveManager_RegainingFocusOrResuming_DoesNotFlush_ManualAndQuitAlwaysFlush()
        {
            CountingProvider provider = new CountingProvider();
            SaveProvider.SetProvider(provider);
            GameObject host = new GameObject("SaveLifecycleRegressionTests.SaveManager");
            _objects.Add(host);
            SaveManager manager = host.AddComponent<SaveManager>();

            Invoke(manager, "OnApplicationFocus", true);
            Invoke(manager, "OnApplicationPause", false);
            Assert.That(provider.SaveCount, Is.EqualTo(0), "regaining focus or resuming must not flush");

            manager.FlushNow();
            Assert.That(provider.SaveCount, Is.EqualTo(1), "an explicit flush always flushes");

            Invoke(manager, "OnApplicationQuit");
            Assert.That(provider.SaveCount, Is.EqualTo(2), "quitting always flushes");
        }

        private static void Invoke(SaveManager manager, string methodName, params object[] arguments)
        {
            MethodInfo method = typeof(SaveManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(manager, arguments.Length == 0 ? null : arguments);
        }

        private sealed class CountingProvider : ISaveProvider
        {
            private readonly Dictionary<string, string> _strings = new();

            public int SaveCount { get; private set; }

            public bool HasUnflushedChanges { get; private set; }

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return defaultValue;
            }

            public void SetInt(string key, int value)
            {
                HasUnflushedChanges = true;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                HasUnflushedChanges = true;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _strings.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _strings[key] = value;
                HasUnflushedChanges = true;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                HasUnflushedChanges = true;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return _strings.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _strings.Remove(key);
                HasUnflushedChanges = true;
            }

            public void DeleteAll()
            {
                _strings.Clear();
                HasUnflushedChanges = true;
            }

            public void Save()
            {
                SaveCount++;
                HasUnflushedChanges = false;
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }
        }
    }
}
