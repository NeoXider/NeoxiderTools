using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    public class SaveManagerReadModifyWriteTests
    {
        [SetUp]
        public void SetUp()
        {
            ResetSaveStatics();
            SaveProvider.SetProvider(new MemorySaveProvider());
        }

        [TearDown]
        public void TearDown()
        {
            ResetSaveStatics();
        }

        [Test]
        public void Save_PreservesComponentsThatAreNotCurrentlyRegistered()
        {
            const string ghostKey = "Other.Scene.Component:unloaded-scene-object";
            var provider = (MemorySaveProvider)SaveProvider.CurrentProvider;
            provider.SetString("SaveData_All",
                "{ \"AllSavedComponents\": [ { \"ComponentKey\": \"" + ghostKey + "\", \"Fields\": [ { \"Key\": \"ghost\", \"TypeName\": \"System.Int32\", \"Value\": \"7\" } ] } ] }");

            GameObject go = new("CurrentSaveable");
            TestSaveable saveable = go.AddComponent<TestSaveable>();
            saveable.Value = 42;

            try
            {
                SaveManager.Register(saveable);
                SaveManager.Save();

                string json = provider.GetString("SaveData_All");
                string currentKey = SaveIdentityUtility.GetComponentKey(saveable);

                Assert.That(json, Does.Contain(ghostKey));
                Assert.That(json, Does.Contain(currentKey));
                Assert.That(json, Does.Contain("\"Value\": \"42\""));
            }
            finally
            {
                SaveManager.Unregister(saveable);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Save_ReadModifyWrite_PreservesMultipleGhostEntriesAndLoadsCurrentRoundTrip()
        {
            const string firstGhostKey = "SceneA.Component:first-unloaded";
            const string secondGhostKey = "SceneB.Component:second-unloaded";
            var provider = (MemorySaveProvider)SaveProvider.CurrentProvider;
            provider.SetString("SaveData_All",
                "{ \"AllSavedComponents\": [" +
                "{ \"ComponentKey\": \"" + firstGhostKey + "\", \"Fields\": [ { \"Key\": \"ghostA\", \"TypeName\": \"System.Int32\", \"Value\": \"1\" } ] }," +
                "{ \"ComponentKey\": \"" + secondGhostKey + "\", \"Fields\": [ { \"Key\": \"ghostB\", \"TypeName\": \"System.Int32\", \"Value\": \"2\" } ] }" +
                "] }");

            GameObject go = new("RoundTripSaveable");
            TestSaveable saveable = go.AddComponent<TestSaveable>();
            saveable.Value = 77;

            try
            {
                SaveManager.Register(saveable);
                SaveManager.Save();
                string savedJson = provider.GetString("SaveData_All");

                Assert.That(savedJson, Does.Contain(firstGhostKey));
                Assert.That(savedJson, Does.Contain(secondGhostKey));
                Assert.That(savedJson, Does.Contain("\"Value\": \"77\""));

                saveable.Value = 0;
                SaveManager.Load(new List<MonoBehaviour> { saveable });

                Assert.AreEqual(77, saveable.Value);
            }
            finally
            {
                SaveManager.Unregister(saveable);
                Object.DestroyImmediate(go);
            }
        }

        private static void ResetSaveStatics()
        {
            typeof(SaveProvider)
                .GetMethod("ResetStaticState", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);

            typeof(SaveManager)
                .GetMethod("ClearSubsystemCaches", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);
        }

        private sealed class TestSaveable : MonoBehaviour, ISaveableComponent, ISaveIdentityProvider
        {
            [SaveField("value")] public int Value;

            public string SaveIdentity => "current-saveable";

            public void OnDataLoaded()
            {
            }
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, string> _strings = new();

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
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return defaultValue;
            }

            public void SetFloat(string key, float value)
            {
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _strings.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _strings[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return defaultValue;
            }

            public void SetBool(string key, bool value)
            {
            }

            public bool HasKey(string key)
            {
                return _strings.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _strings.Remove(key);
            }

            public void DeleteAll()
            {
                _strings.Clear();
            }

            public void Save()
            {
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }
        }
    }
}
