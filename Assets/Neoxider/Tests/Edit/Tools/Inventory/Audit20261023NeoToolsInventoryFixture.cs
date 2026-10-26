using System;
using System.Collections.Generic;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit
{
    public abstract class Audit20261023NeoToolsInventoryFixture
    {
        protected readonly List<Object> CreatedObjects = new();
        protected MemoryProvider Provider;
        private ISaveProvider _previousProvider;

        [SetUp]
        public void SetUpInventoryAudit()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            Provider = new MemoryProvider();
            SaveProvider.SetProvider(Provider);
        }

        [TearDown]
        public void TearDownInventoryAudit()
        {
            try
            {
                for (int i = CreatedObjects.Count - 1; i >= 0; i--)
                {
                    if (CreatedObjects[i] != null)
                    {
                        Object.DestroyImmediate(CreatedObjects[i]);
                    }
                }
            }
            finally
            {
                CreatedObjects.Clear();
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        protected GameObject CreateObject(string name)
        {
            GameObject created = new(name);
            created.SetActive(false);
            CreatedObjects.Add(created);
            return created;
        }

        protected InventoryComponent CreateInventory(InventoryStorageMode mode = InventoryStorageMode.Aggregated,
            int slots = 4, int maxTotal = 0, int maxUnique = 0, InventoryDatabase database = null,
            string saveKey = null)
        {
            InventoryComponent inventory = CreateObject("AuditInventory").AddComponent<InventoryComponent>();
            Configure(inventory, serialized =>
            {
                serialized.FindProperty("_setInstanceOnAwake").boolValue = false;
                serialized.FindProperty("_storageMode").enumValueIndex = (int)mode;
                serialized.FindProperty("_slotCount").intValue = slots;
                serialized.FindProperty("_maxTotalItems").intValue = maxTotal;
                serialized.FindProperty("_maxUniqueItems").intValue = maxUnique;
                serialized.FindProperty("_database").objectReferenceValue = database;
                serialized.FindProperty("_autoLoad").boolValue = false;
                serialized.FindProperty("_autoSave").boolValue = false;
                serialized.FindProperty("_invokeEventsOnLoad").boolValue = false;
                serialized.FindProperty("_saveKey").stringValue = saveKey ?? Guid.NewGuid().ToString("N");
            });
            inventory.GetCount(0);
            return inventory;
        }

        protected InventoryItemData CreateItem(int itemId, int maxStack = -1, bool instanceState = false)
        {
            InventoryItemData item = ScriptableObject.CreateInstance<InventoryItemData>();
            CreatedObjects.Add(item);
            Configure(item, serialized =>
            {
                serialized.FindProperty("_itemId").intValue = itemId;
                serialized.FindProperty("_maxStack").intValue = maxStack;
                serialized.FindProperty("_supportsInstanceState").boolValue = instanceState;
            });
            return item;
        }

        protected InventoryDatabase CreateDatabase(params InventoryItemData[] items)
        {
            InventoryDatabase database = ScriptableObject.CreateInstance<InventoryDatabase>();
            CreatedObjects.Add(database);
            Configure(database, serialized =>
            {
                SerializedProperty entries = serialized.FindProperty("_items");
                entries.arraySize = items.Length;
                for (int i = 0; i < items.Length; i++)
                {
                    entries.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
            });
            return database;
        }

        protected static void Configure(Object target, Action<SerializedObject> configure)
        {
            using SerializedObject serialized = new(target);
            configure(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        protected sealed class MemoryProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new();
            public int StringReads { get; private set; }
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;
            public int GetInt(string key, int defaultValue = 0) => GetValue(key, defaultValue);
            public float GetFloat(string key, float defaultValue = 0f) => GetValue(key, defaultValue);
            public bool GetBool(string key, bool defaultValue = false) => GetValue(key, defaultValue);
            public string GetString(string key, string defaultValue = "")
            {
                StringReads++;
                return GetValue(key, defaultValue);
            }
            public void SetInt(string key, int value) => SetValue(key, value);
            public void SetFloat(string key, float value) => SetValue(key, value);
            public void SetBool(string key, bool value) => SetValue(key, value);
            public void SetString(string key, string value) => SetValue(key, value);
            public bool HasKey(string key) => _values.ContainsKey(key);
            public void DeleteAll() => _values.Clear();
            public void Save() => OnDataSaved?.Invoke();
            public void Load() => OnDataLoaded?.Invoke();
            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }
            private T GetValue<T>(string key, T fallback)
            {
                return _values.TryGetValue(key, out object value) && value is T typed ? typed : fallback;
            }
            private void SetValue(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
