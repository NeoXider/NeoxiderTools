using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    public class InventorySystemTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
            ClearInventorySingleton();
        }

        [Test]
        public void AggregatedInventory_KeepsSimpleStacksAndInstancesSeparately()
        {
            AggregatedInventory inventory = new();
            InventoryConstraints constraints = new();
            constraints.SetItemMaxStack(1, 10);
            inventory.SetConstraints(constraints);

            Assert.That(inventory.Add(1, 3), Is.EqualTo(3));

            InventoryItemInstance instance = new(2);
            instance.ComponentStates.Add(new InventoryItemComponentState("wallet", "{\"coins\":42}"));
            Assert.That(inventory.AddInstance(instance), Is.EqualTo(1));

            Assert.That(inventory.GetCount(1), Is.EqualTo(3));
            Assert.That(inventory.GetCount(2), Is.EqualTo(1));
            Assert.That(inventory.CreateRecordSnapshot().Count, Is.EqualTo(2));
            Assert.That(inventory.CreateRecordSnapshot()[1].IsInstance, Is.True);
        }

        [Test]
        public void SlotGridInventory_NonStackableItemsOccupyDifferentSlots()
        {
            SlotGridInventory inventory = new(3);
            InventoryConstraints constraints = new();
            constraints.SetItemMaxStack(5, 1);
            inventory.SetConstraints(constraints);

            Assert.That(inventory.Add(5, 3), Is.EqualTo(3));
            Assert.That(inventory.GetCount(5), Is.EqualTo(3));
            Assert.That(inventory.GetSlot(0).Count, Is.EqualTo(1));
            Assert.That(inventory.GetSlot(1).Count, Is.EqualTo(1));
            Assert.That(inventory.GetSlot(2).Count, Is.EqualTo(1));
        }

        [Test]
        public void InventoryItemStateUtility_CapturesAndRestoresState()
        {
            GameObject walletObject = CreateGameObject("Wallet");
            TestWalletState wallet = walletObject.AddComponent<TestWalletState>();
            wallet.Coins = 77;

            InventoryItemInstance instance = InventoryItemStateUtility.CaptureInstance(walletObject, 10);
            wallet.Coins = 0;
            InventoryItemStateUtility.RestoreInstance(walletObject, instance);

            Assert.That(wallet.Coins, Is.EqualTo(77));
            Assert.That(instance.ComponentStates.Count, Is.EqualTo(1));
        }

        [Test]
        public void InventoryComponent_SaveLoad_RestoresInstancePayload()
        {
            SaveProvider.SetProvider(new DictionarySaveProvider());

            InventoryItemData walletData = CreateItemData(10, 1, true);
            InventoryDatabase database = CreateDatabase(walletData);
            const string saveKey = "InventoryTests.InstancePayload";

            InventoryComponent source = CreateInventoryComponent("Inventory_Source", InventoryStorageMode.Aggregated, database, 0, saveKey);
            InventoryItemInstance instance = new(10);
            instance.ComponentStates.Add(new InventoryItemComponentState(nameof(TestWalletState), "{\"coins\":55}"));
            source.AddItemInstance(instance);
            source.Save();

            InventoryComponent loaded = CreateInventoryComponent("Inventory_Loaded", InventoryStorageMode.Aggregated, database, 0, saveKey);
            loaded.Load();

            List<InventoryItemInstance> instances = loaded.GetSnapshotInstances();
            Assert.That(instances.Count, Is.EqualTo(1));
            Assert.That(instances[0].ItemId, Is.EqualTo(10));
            Assert.That(instances[0].ComponentStates[0].Json, Is.EqualTo("{\"coins\":55}"));
        }

        [Test]
        public void InventoryComponent_SaveLoad_RestoresMultipleInstancePayloads()
        {
            SaveProvider.SetProvider(new DictionarySaveProvider());

            InventoryItemData itemData = CreateItemData(11, 1, true);
            InventoryDatabase database = CreateDatabase(itemData);
            const string saveKey = "InventoryTests.MultiInstancePayload";

            InventoryComponent source = CreateInventoryComponent("Inventory_MultiSave", InventoryStorageMode.Aggregated, database, 0, saveKey);

            InventoryItemInstance a = new(11);
            a.ComponentStates.Add(new InventoryItemComponentState("A", "{\"n\":1}"));
            InventoryItemInstance b = new(11);
            b.ComponentStates.Add(new InventoryItemComponentState("A", "{\"n\":2}"));
            InventoryItemInstance c = new(11);
            c.ComponentStates.Add(new InventoryItemComponentState("A", "{\"n\":3}"));

            Assert.That(source.AddItemInstance(a), Is.EqualTo(1));
            Assert.That(source.AddItemInstance(b), Is.EqualTo(1));
            Assert.That(source.AddItemInstance(c), Is.EqualTo(1));
            source.Save();

            InventoryComponent loaded = CreateInventoryComponent("Inventory_MultiLoad", InventoryStorageMode.Aggregated, database, 0, saveKey);
            loaded.Load();

            List<InventoryItemInstance> instances = loaded.GetSnapshotInstances();
            Assert.That(instances.Count, Is.EqualTo(3));
            Assert.That(instances[0].ComponentStates[0].Json, Is.EqualTo("{\"n\":1}"));
            Assert.That(instances[1].ComponentStates[0].Json, Is.EqualTo("{\"n\":2}"));
            Assert.That(instances[2].ComponentStates[0].Json, Is.EqualTo("{\"n\":3}"));
        }

        [Test]
        public void InventoryComponent_SaveLoad_RestoresMultipleInstancePayloadsInSlotGrid()
        {
            SaveProvider.SetProvider(new DictionarySaveProvider());

            InventoryItemData itemData = CreateItemData(12, 1, true);
            InventoryDatabase database = CreateDatabase(itemData);
            const string saveKey = "InventoryTests.MultiInstanceSlots";

            InventoryComponent source = CreateInventoryComponent("Inventory_MultiSlotSave", InventoryStorageMode.SlotGrid, database, 4, saveKey);

            InventoryItemInstance x = new(12);
            x.ComponentStates.Add(new InventoryItemComponentState("k", "{\"x\":10}"));
            InventoryItemInstance y = new(12);
            y.ComponentStates.Add(new InventoryItemComponentState("k", "{\"x\":20}"));

            Assert.That(source.AddItemInstance(x), Is.EqualTo(1));
            Assert.That(source.AddItemInstance(y), Is.EqualTo(1));
            source.Save();

            InventoryComponent loaded = CreateInventoryComponent("Inventory_MultiSlotLoad", InventoryStorageMode.SlotGrid, database, 4, saveKey);
            loaded.Load();

            Assert.That(loaded.GetSlot(0).IsInstance, Is.True);
            Assert.That(loaded.GetSlot(1).IsInstance, Is.True);
            Assert.That(loaded.GetSlot(0).Instance.ComponentStates[0].Json, Is.EqualTo("{\"x\":10}"));
            Assert.That(loaded.GetSlot(1).Instance.ComponentStates[0].Json, Is.EqualTo("{\"x\":20}"));
        }

        [Test]
        public void InventoryComponent_LoadsLegacyEntriesIntoSlotGrid()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            InventoryItemData ammoData = CreateItemData(7, 1, false);
            InventoryDatabase database = CreateDatabase(ammoData);
            const string saveKey = "InventoryTests.LegacyGrid";
            provider.SetString(saveKey, "{\"Entries\":[{\"ItemId\":7,\"Count\":3}]}");

            InventoryComponent inventory = CreateInventoryComponent("Inventory_LegacyGrid", InventoryStorageMode.SlotGrid,
                database, 5, saveKey);
            inventory.Load();

            Assert.That(inventory.GetSlot(0).EffectiveItemId, Is.EqualTo(7));
            Assert.That(inventory.GetSlot(1).EffectiveItemId, Is.EqualTo(7));
            Assert.That(inventory.GetSlot(2).EffectiveItemId, Is.EqualTo(7));
        }

        [Test]
        public void InventoryTransferService_MovesInstancePayloadBetweenContainers()
        {
            SaveProvider.SetProvider(new DictionarySaveProvider());

            InventoryItemData gunData = CreateItemData(20, 1, true);
            InventoryDatabase database = CreateDatabase(gunData);

            InventoryComponent source = CreateInventoryComponent("Inventory_SourceSlots", InventoryStorageMode.SlotGrid, database,
                3, "InventoryTests.SourceSlots");
            InventoryComponent target = CreateInventoryComponent("Inventory_TargetSlots", InventoryStorageMode.SlotGrid, database,
                3, "InventoryTests.TargetSlots");

            InventoryItemInstance gun = new(20);
            gun.ComponentStates.Add(new InventoryItemComponentState(nameof(TestWalletState), "{\"coins\":12}"));
            source.AddItemInstance(gun);

            int moved = InventoryTransferService.Transfer(source, 0, target, 1);

            Assert.That(moved, Is.EqualTo(1));
            Assert.That(source.GetSlot(0).IsEmpty, Is.True);
            Assert.That(target.GetSlot(1).IsInstance, Is.True);
            Assert.That(target.GetSlot(1).Instance.ComponentStates[0].Json, Is.EqualTo("{\"coins\":12}"));
        }

        private InventoryComponent CreateInventoryComponent(string name, InventoryStorageMode mode, InventoryDatabase database,
            int slotCount, string saveKey)
        {
            GameObject gameObject = CreateGameObject(name);
            InventoryComponent inventory = gameObject.AddComponent<InventoryComponent>();
            SetField(inventory, "_setInstanceOnAwake", false);
            SetField(inventory, "_database", database);
            SetField(inventory, "_storageMode", mode);
            SetField(inventory, "_slotCount", slotCount);
            SetField(inventory, "_autoLoad", false);
            SetField(inventory, "_autoSave", false);
            SetField(inventory, "_invokeEventsOnLoad", false);
            SetField(inventory, "_saveKey", saveKey);
            SetField(inventory, "_runtimeInitialized", false);
            SetField(inventory, "_storage", null);
            ClearInventorySingleton();
            return inventory;
        }

        private InventoryItemData CreateItemData(int itemId, int maxStack, bool supportsInstanceState)
        {
            InventoryItemData item = ScriptableObject.CreateInstance<InventoryItemData>();
            _createdObjects.Add(item);
            SetField(item, "_itemId", itemId);
            SetField(item, "_displayName", $"Item {itemId}");
            SetField(item, "_maxStack", maxStack);
            SetField(item, "_supportsInstanceState", supportsInstanceState);
            return item;
        }

        private InventoryDatabase CreateDatabase(params InventoryItemData[] items)
        {
            InventoryDatabase database = ScriptableObject.CreateInstance<InventoryDatabase>();
            _createdObjects.Add(database);
            SetField(database, "_items", new List<InventoryItemData>(items));
            return database;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static void ClearInventorySingleton()
        {
            FieldInfo instanceField = typeof(Singleton<InventoryComponent>).GetField("_instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, null);
        }

        private sealed class TestWalletState : InventoryItemStateBehaviour
        {
            [Serializable]
            private sealed class WalletState
            {
                public int coins;
            }

            public int Coins { get; set; }

            public override string CaptureInventoryState()
            {
                return JsonUtility.ToJson(new WalletState
                {
                    coins = Coins
                });
            }

            public override void RestoreInventoryState(string json)
            {
                WalletState state = JsonUtility.FromJson<WalletState>(json);
                Coins = state != null ? state.coins : 0;
            }
        }

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, string> _strings = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _strings.TryGetValue(key, out string raw) && int.TryParse(raw, out int value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _strings[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _strings.TryGetValue(key, out string raw) && float.TryParse(raw, out float value) ? value : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _strings[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
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
                return _strings.TryGetValue(key, out string raw) && bool.TryParse(raw, out bool value) ? value : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _strings[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return _strings.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _strings.Remove(key);
                OnKeyChanged?.Invoke(key);
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
