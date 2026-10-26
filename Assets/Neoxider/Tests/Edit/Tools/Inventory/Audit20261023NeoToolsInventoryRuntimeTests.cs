using System;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RestoreFailureState = global::Neo.Tests.Edit.Audit20261023FixtureNeoToolsInventoryRuntimeRestoreFailureState;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023NeoToolsInventoryRuntimeTests : Audit20261023NeoToolsInventoryFixture
    {
        [TestCase(false)]
        [TestCase(true)]
        public void SlotLayoutChange_NotifiesAndAutoSaves_WithoutCountDeltaEvents(bool swap)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            Assert.That(inventory.AddItemByIdAmount(7, 2), Is.EqualTo(2));
            Assert.That(inventory.GetSlot(0).EffectiveItemId, Is.EqualTo(7));
            Assert.That(inventory.GetSlot(0).Count, Is.EqualTo(2));
            Assert.That(inventory.GetSlot(0).IsInstance, Is.False);
            Assert.That(inventory.GetSlot(2).IsEmpty, Is.True);
            int changed = 0;
            int deltas = 0;
            int saved = 0;
            inventory.OnInventoryChanged.AddListener(() => changed++);
            inventory.OnItemCountChanged.AddListener((itemId, count) => deltas++);
            inventory.OnSaved.AddListener(() => saved++);
            Configure(inventory, serialized => serialized.FindProperty("_autoSave").boolValue = true);

            if (swap)
            {
                Assert.That(inventory.SwapSlots(0, 2), Is.True);
            }
            else
            {
                Assert.That(inventory.MoveSlot(0, 2, 1), Is.EqualTo(1));
            }

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(deltas, Is.Zero);
            Assert.That(saved, Is.EqualTo(1));
            InventorySaveData persisted = InventorySaveData.FromJson(Provider.GetString(inventory.SaveKey));
            Assert.That(persisted.StorageMode, Is.EqualTo((int)InventoryStorageMode.SlotGrid));
            Assert.That(persisted.SlotCapacity, Is.EqualTo(4));
            Assert.That(persisted.Slots[2].IsInstance, Is.False);
            Assert.That(persisted.Slots[2].EffectiveItemId, Is.EqualTo(7));
            Assert.That(persisted.Slots[2].EffectiveCount, Is.EqualTo(swap ? 2 : 1));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(2));
            Assert.That(inventory.MoveSlot(2, 2), Is.Zero);
            Assert.That(changed, Is.EqualTo(1), "An invalid move must not emit a second notification.");
        }

        [Test]
        public void ReplacingPayloadWithSameCount_NotifiesAndPersistsNewState()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance instance = new(7);
            instance.ComponentStates.Add(new InventoryItemComponentState("ammo", "1"));
            inventory.AddItemInstance(instance);
            int changed = 0;
            int deltas = 0;
            inventory.OnInventoryChanged.AddListener(() => changed++);
            inventory.OnItemCountChanged.AddListener((itemId, count) => deltas++);
            Configure(inventory, serialized => serialized.FindProperty("_autoSave").boolValue = true);
            InventorySlotState replacement = inventory.GetSlot(0);
            replacement.Instance.ComponentStates[0].Json = "2";

            Assert.That(inventory.TrySetSlot(0, replacement), Is.True);

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(deltas, Is.Zero);
            InventorySaveData persisted = InventorySaveData.FromJson(Provider.GetString(inventory.SaveKey));
            Assert.That(persisted.Slots[0].Instance.ComponentStates[0].Json, Is.EqualTo("2"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CrossContainerTransfer_ObserversSeeBothFinalStates(bool swap)
        {
            InventoryComponent source = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryComponent target = CreateInventory(InventoryStorageMode.SlotGrid);
            source.AddItemByIdAmount(7, 2);
            if (swap)
            {
                target.TrySetSlot(1, new InventorySlotState { ItemId = 8, Count = 1 });
            }

            List<int> totals = new();
            List<int> sourceIds = new();
            List<int> targetIds = new();
            void Observe()
            {
                totals.Add(source.TotalItemCount + target.TotalItemCount);
                sourceIds.Add(source.GetSlot(0).EffectiveItemId);
                targetIds.Add(target.GetSlot(1).EffectiveItemId);
            }
            source.OnInventoryChanged.AddListener(Observe);
            target.OnInventoryChanged.AddListener(Observe);

            Assert.That(InventoryTransferService.Transfer(source, 0, target, 1), Is.EqualTo(2));

            Assert.That(totals, Is.EqualTo(new[] { swap ? 3 : 2, swap ? 3 : 2 }));
            Assert.That(sourceIds, Is.EqualTo(new[] { swap ? 8 : -1, swap ? 8 : -1 }));
            Assert.That(targetIds, Is.EqualTo(new[] { 7, 7 }));
        }

        [Test]
        public void RejectedSwap_DoesNotPublishTemporaryTargetState()
        {
            InventoryComponent source = CreateInventory(InventoryStorageMode.SlotGrid, maxTotal: 1);
            InventoryComponent target = CreateInventory(InventoryStorageMode.SlotGrid);
            source.AddItemById(7);
            target.AddItemByIdAmount(8, 2);
            int changed = 0;
            source.OnInventoryChanged.AddListener(() => changed++);
            target.OnInventoryChanged.AddListener(() => changed++);

            Assert.That(InventoryTransferService.Transfer(source, 0, target, 0), Is.Zero);
            Assert.That(source.GetSlot(0).EffectiveItemId, Is.EqualTo(7));
            Assert.That(target.GetSlot(0).EffectiveItemId, Is.EqualTo(8));
            Assert.That(source.TotalItemCount, Is.EqualTo(1));
            Assert.That(target.TotalItemCount, Is.EqualTo(2));
            Assert.That(changed, Is.Zero);
        }

        [Test]
        public void Transfer_WhenObserverThrows_DoesNotDuplicateTransferredItems()
        {
            InventoryComponent source = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryComponent target = CreateInventory(InventoryStorageMode.SlotGrid);
            source.AddItemById(7);
            target.OnInventoryChanged.AddListener(() => throw new InvalidOperationException("observer"));

            Assert.Throws<InvalidOperationException>(() => InventoryTransferService.Transfer(source, 0, target, 0));

            Assert.That(source.GetCount(7), Is.Zero);
            Assert.That(target.GetCount(7), Is.EqualTo(1));
        }

        [Test]
        public void SlotInsert_RespectsDatabaseRestriction()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid,
                database: CreateDatabase(CreateItem(7)));
            Configure(inventory, serialized => serialized.FindProperty("_restrictToDatabase").boolValue = true);

            Assert.That(inventory.TryInsertIntoSlot(0, new InventoryItemRecord(8, 2)), Is.Zero);
            Assert.That(inventory.TotalItemCount, Is.Zero);
            Assert.That(inventory.GetSlot(0).IsEmpty, Is.True);
            Assert.That(inventory.TryInsertIntoSlot(0, new InventoryItemRecord(7, 2)), Is.EqualTo(2));
        }

        [Test]
        public void SlotReplacement_RejectsUnrepresentableTotalAndNegativeInstanceId()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, maxTotal: int.MaxValue);
            inventory.AddItemByIdAmount(7, int.MaxValue - 1);

            Assert.That(inventory.TrySetSlot(1, new InventorySlotState { Instance = new InventoryItemInstance(8, 2) }),
                Is.False);
            Assert.That(inventory.TrySetSlot(1, new InventorySlotState { Instance = new InventoryItemInstance(-1) }),
                Is.False);
            Assert.That(inventory.GetSlot(1).IsEmpty, Is.True);
            Assert.That(inventory.TotalItemCount, Is.EqualTo(int.MaxValue - 1));
        }

        [TestCase(InventoryStorageMode.SlotGrid, InventoryStorageMode.Aggregated, 4, 0)]
        [TestCase(InventoryStorageMode.SlotGrid, InventoryStorageMode.SlotGrid, 4, 1)]
        [TestCase(InventoryStorageMode.Aggregated, InventoryStorageMode.SlotGrid, 0, 4)]
        public void SaveLoad_RestoresSavedBackendCapacityAndPayload(InventoryStorageMode savedMode,
            InventoryStorageMode configuredMode, int savedSlots, int configuredSlots)
        {
            InventoryComponent source = CreateInventory(savedMode, savedSlots);
            InventoryItemInstance instance = new(9);
            instance.StateVersion = 4;
            instance.ComponentStates.Add(new InventoryItemComponentState("ammo", "17"));
            Assert.That(source.AddItemInstance(instance), Is.EqualTo(1));
            if (savedMode == InventoryStorageMode.SlotGrid)
            {
                Assert.That(source.TrySetSlot(savedSlots - 1,
                    new InventorySlotState { ItemId = 7, Count = 2 }), Is.True);
            }
            else
            {
                Assert.That(source.AddItemByIdAmount(7, 2), Is.EqualTo(2));
            }
            Assert.That(source.GetCount(7), Is.EqualTo(2));
            Assert.That(source.GetCount(9), Is.EqualTo(1));
            Assert.That(source.TotalItemCount, Is.EqualTo(3));
            source.Save();
            InventorySaveData persisted = InventorySaveData.FromJson(Provider.GetString(source.SaveKey));
            Assert.That(persisted.StorageMode, Is.EqualTo((int)savedMode));
            Assert.That(persisted.SlotCapacity, Is.EqualTo(savedSlots));
            InventoryComponent loaded = CreateInventory(configuredMode, configuredSlots, saveKey: source.SaveKey);
            Assert.That(loaded.StorageMode, Is.EqualTo(configuredMode));
            Assert.That(loaded.SlotCapacity, Is.EqualTo(configuredMode == InventoryStorageMode.SlotGrid ? configuredSlots : 0));

            loaded.Load();

            Assert.That(loaded.StorageMode, Is.EqualTo(savedMode));
            Assert.That(loaded.SlotCapacity, Is.EqualTo(savedMode == InventoryStorageMode.SlotGrid ? savedSlots : 0));
            Assert.That(loaded.GetCount(7), Is.EqualTo(2));
            Assert.That(loaded.GetCount(9), Is.EqualTo(1));
            Assert.That(loaded.TotalItemCount, Is.EqualTo(3));
            Assert.That(loaded.GetSnapshotInstances().Count, Is.EqualTo(1));
            Assert.That(loaded.GetSnapshotInstances()[0].InstanceId, Is.EqualTo(instance.InstanceId));
            Assert.That(loaded.GetSnapshotInstances()[0].StateVersion, Is.EqualTo(4));
            Assert.That(loaded.GetSnapshotInstances()[0].ComponentStates[0].Key, Is.EqualTo("ammo"));
            Assert.That(loaded.GetSnapshotInstances()[0].ComponentStates[0].Json, Is.EqualTo("17"));
            if (savedMode == InventoryStorageMode.SlotGrid)
            {
                Assert.That(loaded.GetSlot(savedSlots - 1).Count, Is.EqualTo(2));
                Assert.That(loaded.GetSlot(savedSlots - 1).IsInstance, Is.False);
                Assert.That(loaded.GetSlot(1).IsEmpty, Is.True);
            }
        }

        [Test]
        public void SaveData_JsonRoundtripPreservesEmptySimpleAndInstanceRowsAndLegacyMetadata()
        {
            InventoryItemInstance instance = new(9) { StateVersion = 4 };
            instance.ComponentStates.Add(new InventoryItemComponentState("ammo", "17"));
            InventoryItemInstance legacy = new(14, 2);
            InventorySaveData original = new()
            {
                Version = 1, InitialStateMerged = true,
                StorageMode = (int)InventoryStorageMode.SlotGrid, SlotCapacity = 3,
                Slots = new List<InventorySlotState>
                {
                    InventorySlotState.Empty(), new() { ItemId = 7, Count = 2 },
                    new() { ItemId = 9, Count = 1, Instance = instance }
                },
                Records = new List<InventoryItemRecord> { new(7, 2), new(instance) },
                Entries = new List<InventoryEntry> { new() { ItemId = 13, Count = 3 } },
                Instances = new List<InventoryItemInstance> { legacy }
            };
            Assert.That(original.Slots[0].Instance, Is.Null);
            Assert.That(original.Slots[1].Instance, Is.Null);
            Assert.That(original.Records[0].Instance, Is.Null);
            string domainJson = JsonUtility.ToJson(original);
            // P5 observed Count=1 in this callback-free domain probe on both engines.
            // It is a serialization observation, not a safe rule for deleting an instance.
            RawSaveDataProbe raw = JsonUtility.FromJson<RawSaveDataProbe>(domainJson);
            Assert.That(raw.Slots[0].ItemId, Is.EqualTo(-1));
            Assert.That(raw.Slots[0].Count, Is.Zero);
            Assert.That(raw.Slots[1].ItemId, Is.EqualTo(7));
            Assert.That(raw.Slots[1].Count, Is.EqualTo(2));
            AssertDomainInlineValue(raw.Slots[0].Instance);
            AssertDomainInlineValue(raw.Slots[1].Instance);
            AssertDomainInlineValue(raw.Records[0].Instance);
            Assert.That(raw.Slots[2].Instance.InstanceId, Is.EqualTo(instance.InstanceId));

            string json = original.ToJson();
            RawWireSaveDataProbe wire = JsonUtility.FromJson<RawWireSaveDataProbe>(json);
            AssertEmptyWireInlineValue(wire.Slots[0].Instance);
            AssertEmptyWireInlineValue(wire.Slots[1].Instance);
            AssertEmptyWireInlineValue(wire.Records[0].Instance);
            Assert.That(wire.Slots[2].Instance.Count, Is.EqualTo(1));
            Assert.That(wire.Slots[2].Instance.InstanceId, Is.EqualTo(instance.InstanceId));
            InventorySaveData restored = InventorySaveData.FromJson(json);

            Assert.That(restored.Version, Is.EqualTo(1));
            Assert.That(restored.InitialStateMerged, Is.True);
            Assert.That(restored.StorageMode, Is.EqualTo((int)InventoryStorageMode.SlotGrid));
            Assert.That(restored.SlotCapacity, Is.EqualTo(3));
            Assert.That(restored.Slots.Count, Is.EqualTo(3));
            Assert.That(restored.Slots[0].IsEmpty, Is.True);
            Assert.That(restored.Slots[0].Instance, Is.Null);
            Assert.That(restored.Slots[1].IsInstance, Is.False);
            Assert.That(restored.Slots[1].EffectiveItemId, Is.EqualTo(7));
            Assert.That(restored.Slots[1].EffectiveCount, Is.EqualTo(2));
            Assert.That(restored.Slots[2].Instance.InstanceId, Is.EqualTo(instance.InstanceId));
            Assert.That(restored.Slots[2].Instance.StateVersion, Is.EqualTo(4));
            Assert.That(restored.Slots[2].Instance.ComponentStates[0].Key, Is.EqualTo("ammo"));
            Assert.That(restored.Slots[2].Instance.ComponentStates[0].Json, Is.EqualTo("17"));
            Assert.That(restored.Records.Count, Is.EqualTo(2));
            Assert.That(restored.Records[0].IsInstance, Is.False);
            Assert.That(restored.Records[0].EffectiveItemId, Is.EqualTo(7));
            Assert.That(restored.Records[0].EffectiveCount, Is.EqualTo(2));
            Assert.That(restored.Records[1].Instance.InstanceId, Is.EqualTo(instance.InstanceId));
            Assert.That(restored.Records[1].Instance.ComponentStates[0].Json, Is.EqualTo("17"));
            Assert.That(restored.Entries[0].ItemId, Is.EqualTo(13));
            Assert.That(restored.Entries[0].Count, Is.EqualTo(3));
            Assert.That(restored.Instances[0].InstanceId, Is.EqualTo(legacy.InstanceId));
            Assert.That(restored.Instances[0].Count, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Load_IdZeroInstanceWithoutGeneratedIdRemainsAnInstance(bool slots)
        {
            InventoryStorageMode mode = slots ? InventoryStorageMode.SlotGrid : InventoryStorageMode.Aggregated;
            InventoryComponent inventory = CreateInventory(mode, slots ? 2 : 0, maxTotal: 2);
            InventoryItemInstance instance = new() { ItemId = 0, Count = 1 };
            InventorySaveData data = new() { StorageMode = (int)mode, SlotCapacity = slots ? 2 : 0 };
            if (slots)
            {
                data.Slots.Add(new InventorySlotState { ItemId = 7, Count = 1 });
                data.Slots.Add(new InventorySlotState { ItemId = 0, Count = 1, Instance = instance });
            }
            else
            {
                data.Records.Add(new InventoryItemRecord(7, 1));
                data.Records.Add(new InventoryItemRecord { ItemId = 0, Count = 1, Instance = instance });
            }
            string json = data.ToJson();
            InventorySaveData decoded = InventorySaveData.FromJson(json);
            InventoryItemInstance decodedInstance = slots ? decoded.Slots[1].Instance : decoded.Records[1].Instance;
            Assert.That(decodedInstance, Is.Not.Null);
            Assert.That(decodedInstance.Count, Is.EqualTo(1));
            Assert.That(string.IsNullOrEmpty(decodedInstance.InstanceId), Is.True);
            Provider.SetString(inventory.SaveKey, json);

            inventory.Load();

            Assert.That(inventory.StorageMode, Is.EqualTo(mode));
            Assert.That(inventory.SlotCapacity, Is.EqualTo(slots ? 2 : 0));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(2));
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(inventory.GetCount(0), Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotInstances().Count, Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotInstances()[0].ItemId, Is.Zero);
            Assert.That(inventory.GetSnapshotInstances()[0].InstanceId, Is.Not.Empty);
            Assert.That(inventory.GetSnapshotInstances()[0].ComponentStates, Is.Empty);
        }

        [Test]
        public void Load_LiteralSlotWithoutItemIdCannotGrantItemZero()
        {
            const string json = "{\"Version\":1,\"StorageMode\":1,\"SlotCapacity\":1," +
                                "\"Slots\":[{\"Count\":1,\"Instance\":{\"ItemId\":0,\"Count\":0}}]}";
            InventorySaveData decoded = InventorySaveData.FromJson(json);
            Assert.That(decoded.Slots.Count, Is.EqualTo(1));
            Assert.That(decoded.Slots[0].ItemId, Is.EqualTo(-1));
            Assert.That(decoded.Slots[0].Count, Is.EqualTo(1));
            Assert.That(decoded.Slots[0].Instance, Is.Null);
            Assert.That(decoded.Slots[0].IsEmpty, Is.True);
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 1);
            Provider.SetString(inventory.SaveKey, json);

            inventory.Load();

            Assert.That(inventory.StorageMode, Is.EqualTo(InventoryStorageMode.SlotGrid));
            Assert.That(inventory.SlotCapacity, Is.EqualTo(1));
            Assert.That(inventory.TotalItemCount, Is.Zero);
            Assert.That(inventory.GetCount(0), Is.Zero);
            Assert.That(inventory.GetSlot(0).IsEmpty, Is.True);
            Assert.That(inventory.GetSnapshotRecords(), Is.Empty);
            Assert.That(inventory.GetSnapshotInstances(), Is.Empty);
        }

        [Test]
        public void Load_LiteralExplicitItemZeroRemainsASimpleSlot()
        {
            const string json = "{\"Version\":1,\"StorageMode\":1,\"SlotCapacity\":1," +
                                "\"Slots\":[{\"ItemId\":0,\"Count\":1,\"Instance\":{\"ItemId\":0,\"Count\":0}}]}";
            InventorySaveData decoded = InventorySaveData.FromJson(json);
            Assert.That(decoded.Slots[0].ItemId, Is.Zero);
            Assert.That(decoded.Slots[0].Count, Is.EqualTo(1));
            Assert.That(decoded.Slots[0].Instance, Is.Null);
            Assert.That(decoded.Slots[0].IsEmpty, Is.False);
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 1);
            Provider.SetString(inventory.SaveKey, json);

            inventory.Load();

            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
            Assert.That(inventory.GetCount(0), Is.EqualTo(1));
            Assert.That(inventory.GetSlot(0).IsInstance, Is.False);
            Assert.That(inventory.GetSlot(0).EffectiveItemId, Is.Zero);
            Assert.That(inventory.GetSlot(0).Count, Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotInstances(), Is.Empty);
        }

        [Test]
        public void Load_LiteralKeylessItemZeroInstanceSurvivesMissingSlotId()
        {
            const string json = "{\"Version\":1,\"StorageMode\":1,\"SlotCapacity\":1," +
                                "\"Slots\":[{\"Count\":1,\"Instance\":{\"ItemId\":0,\"Count\":1}}]}";
            InventorySaveData decoded = InventorySaveData.FromJson(json);
            Assert.That(decoded.Slots[0].ItemId, Is.EqualTo(-1));
            Assert.That(decoded.Slots[0].IsInstance, Is.True);
            Assert.That(decoded.Slots[0].IsEmpty, Is.False);
            Assert.That(decoded.Slots[0].Instance.ItemId, Is.Zero);
            Assert.That(decoded.Slots[0].Instance.Count, Is.EqualTo(1));
            Assert.That(string.IsNullOrEmpty(decoded.Slots[0].Instance.InstanceId), Is.True);
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 1);
            Provider.SetString(inventory.SaveKey, json);

            inventory.Load();

            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
            Assert.That(inventory.GetCount(0), Is.EqualTo(1));
            Assert.That(inventory.GetSlot(0).IsInstance, Is.True);
            Assert.That(inventory.GetSlot(0).EffectiveItemId, Is.Zero);
            Assert.That(inventory.GetSlot(0).EffectiveCount, Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotInstances().Count, Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotInstances()[0].InstanceId, Is.Not.Empty);
        }

        [Test]
        public void Load_LegacyRecordWithoutItemIdRetainsZeroDefault()
        {
            const string json = "{\"Records\":[{\"Count\":1}]}";
            InventorySaveData decoded = InventorySaveData.FromJson(json);
            Assert.That(decoded.Records.Count, Is.EqualTo(1));
            Assert.That(decoded.Records[0].ItemId, Is.Zero);
            Assert.That(decoded.Records[0].Count, Is.EqualTo(1));
            Assert.That(decoded.Records[0].Instance, Is.Null);
            InventoryComponent inventory = CreateInventory();
            Provider.SetString(inventory.SaveKey, json);

            inventory.Load();

            Assert.That(inventory.StorageMode, Is.EqualTo(InventoryStorageMode.Aggregated));
            Assert.That(inventory.SlotCapacity, Is.Zero);
            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
            Assert.That(inventory.GetCount(0), Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotRecords().Count, Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotRecords()[0].IsInstance, Is.False);
            Assert.That(inventory.GetSnapshotRecords()[0].EffectiveItemId, Is.Zero);
            Assert.That(inventory.GetSnapshotInstances(), Is.Empty);
        }

        [TestCase("")]
        [TestCase(",\"Instance\":null")]
        [TestCase(",\"Instance\":{}")]
        [TestCase(",\"Instance\":{\"InstanceId\":\"\",\"ItemId\":0,\"Count\":0,\"StateVersion\":0,\"ComponentStates\":[]}")]
        public void SaveData_WireReaderPreservesAbsentEmptyAndSimpleRows(string instanceField)
        {
            string empty = "{\"ItemId\":-1,\"Count\":0" + instanceField + "}";
            string simple = "{\"ItemId\":7,\"Count\":2" + instanceField + "}";
            string json = "{\"Version\":1,\"StorageMode\":1,\"SlotCapacity\":2,\"Slots\":[" +
                          empty + "," + simple + "],\"Records\":[" + empty + "," + simple + "]}";
            InventorySaveData decoded = InventorySaveData.FromJson(json);
            InventorySaveData roundtrip = InventorySaveData.FromJson(decoded.ToJson());

            foreach (InventorySaveData snapshot in new[] { decoded, roundtrip })
            {
                Assert.That(snapshot.Version, Is.EqualTo(1));
                Assert.That(snapshot.StorageMode, Is.EqualTo((int)InventoryStorageMode.SlotGrid));
                Assert.That(snapshot.SlotCapacity, Is.EqualTo(2));
                Assert.That(snapshot.Slots.Count, Is.EqualTo(2));
                Assert.That(snapshot.Slots[0].IsEmpty, Is.True);
                Assert.That(snapshot.Slots[0].Instance, Is.Null);
                Assert.That(snapshot.Slots[0].ItemId, Is.EqualTo(-1));
                Assert.That(snapshot.Slots[0].Count, Is.Zero);
                Assert.That(snapshot.Slots[1].IsInstance, Is.False);
                Assert.That(snapshot.Slots[1].EffectiveItemId, Is.EqualTo(7));
                Assert.That(snapshot.Slots[1].EffectiveCount, Is.EqualTo(2));
                Assert.That(snapshot.Records.Count, Is.EqualTo(2));
                Assert.That(snapshot.Records[0].Instance, Is.Null);
                Assert.That(snapshot.Records[0].ItemId, Is.EqualTo(-1));
                Assert.That(snapshot.Records[0].Count, Is.Zero);
                Assert.That(snapshot.Records[1].IsInstance, Is.False);
                Assert.That(snapshot.Records[1].EffectiveItemId, Is.EqualTo(7));
                Assert.That(snapshot.Records[1].EffectiveCount, Is.EqualTo(2));
            }
        }

        [TestCase("identity")]
        [TestCase("version")]
        [TestCase("payload")]
        public void SaveData_NonemptyZeroCountInstanceNeverBecomesItsPositiveWrapper(string field)
        {
            InventoryItemInstance instance = new()
            {
                ItemId = 0, Count = 0, InstanceId = field == "identity" ? "explicit-zero" : null,
                StateVersion = field == "version" ? 4 : 0
            };
            if (field == "payload")
            {
                instance.ComponentStates.Add(new InventoryItemComponentState("opaque", "{\"Count\":1}"));
            }
            InventorySaveData original = new()
            {
                Slots = new List<InventorySlotState> { new() { ItemId = 7, Count = 2, Instance = instance } },
                Records = new List<InventoryItemRecord> { new() { ItemId = 7, Count = 2, Instance = instance } }
            };

            InventorySaveData decoded = InventorySaveData.FromJson(original.ToJson());

            foreach (InventoryItemInstance restored in new[] { decoded.Slots[0].Instance, decoded.Records[0].Instance })
            {
                Assert.That(restored, Is.Not.Null, "Nonempty instance metadata must retain instance precedence.");
                Assert.That(restored.ItemId, Is.Zero);
                Assert.That(restored.Count, Is.Zero, "The codec must not apply admission or Clone normalization.");
                Assert.That(restored.InstanceId ?? "", Is.EqualTo(instance.InstanceId ?? ""));
                Assert.That(restored.StateVersion, Is.EqualTo(instance.StateVersion));
                Assert.That(restored.ComponentStates.Count, Is.EqualTo(instance.ComponentStates.Count));
                if (field == "payload")
                {
                    Assert.That(restored.ComponentStates[0].Key, Is.EqualTo("opaque"));
                    Assert.That(restored.ComponentStates[0].Json, Is.EqualTo("{\"Count\":1}"));
                }
            }
            Assert.That(decoded.Slots[0].EffectiveItemId, Is.Zero);
            Assert.That(decoded.Records[0].EffectiveItemId, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Load_IdZeroInstanceRetainsIdentityVersionAndOpaqueState(bool slots)
        {
            InventoryStorageMode mode = slots ? InventoryStorageMode.SlotGrid : InventoryStorageMode.Aggregated;
            InventoryComponent inventory = CreateInventory(mode, slots ? 2 : 0, maxTotal: 2);
            const string payload = "{\"Instance\":{\"Count\":1},\"text\":\"opaque payload\"}";
            InventoryItemInstance instance = new()
            {
                ItemId = 0, Count = 1, InstanceId = "explicit-zero", StateVersion = 4
            };
            instance.ComponentStates.Add(new InventoryItemComponentState("opaque", payload));
            InventorySaveData data = new() { StorageMode = (int)mode, SlotCapacity = slots ? 2 : 0 };
            if (slots)
            {
                data.Slots.Add(new InventorySlotState { ItemId = 7, Count = 1 });
                data.Slots.Add(new InventorySlotState { ItemId = 0, Count = 1, Instance = instance });
            }
            else
            {
                data.Records.Add(new InventoryItemRecord(7, 1));
                data.Records.Add(new InventoryItemRecord { ItemId = 0, Count = 1, Instance = instance });
            }
            Provider.SetString(inventory.SaveKey, data.ToJson());

            inventory.Load();
            inventory.Save();
            InventoryComponent reloaded = CreateInventory(saveKey: inventory.SaveKey, maxTotal: 2);
            reloaded.Load();

            foreach (InventoryComponent restored in new[] { inventory, reloaded })
            {
                Assert.That(restored.StorageMode, Is.EqualTo(mode));
                Assert.That(restored.SlotCapacity, Is.EqualTo(slots ? 2 : 0));
                Assert.That(restored.TotalItemCount, Is.EqualTo(2));
                Assert.That(restored.GetCount(7), Is.EqualTo(1));
                Assert.That(restored.GetCount(0), Is.EqualTo(1));
                List<InventoryItemInstance> instances = restored.GetSnapshotInstances();
                Assert.That(instances.Count, Is.EqualTo(1));
                Assert.That(instances[0].InstanceId, Is.EqualTo("explicit-zero"));
                Assert.That(instances[0].ItemId, Is.Zero);
                Assert.That(instances[0].Count, Is.EqualTo(1));
                Assert.That(instances[0].StateVersion, Is.EqualTo(4));
                Assert.That(instances[0].ComponentStates.Count, Is.EqualTo(1));
                Assert.That(instances[0].ComponentStates[0].Key, Is.EqualTo("opaque"));
                Assert.That(instances[0].ComponentStates[0].Json, Is.EqualTo(payload));
            }
        }

        [TestCase(InventoryStorageMode.Aggregated, -1, 1)]
        [TestCase(InventoryStorageMode.Aggregated, 8, 2)]
        [TestCase(InventoryStorageMode.SlotGrid, -1, 1)]
        [TestCase(InventoryStorageMode.SlotGrid, 8, 2)]
        public void Load_InvalidInstanceCannotFallBackToItsPositiveWrapper(InventoryStorageMode mode,
            int itemId, int count)
        {
            InventoryComponent inventory = CreateInventory(mode, mode == InventoryStorageMode.SlotGrid ? 2 : 0,
                maxTotal: 2);
            InventoryItemInstance instance = new(itemId, count);
            InventorySaveData data = new() { StorageMode = (int)mode };
            if (mode == InventoryStorageMode.SlotGrid)
            {
                data.SlotCapacity = 2;
                data.Slots.Add(new InventorySlotState { ItemId = 7, Count = 1 });
                data.Slots.Add(new InventorySlotState { ItemId = 8, Count = 1, Instance = instance });
            }
            else
            {
                data.Records.Add(new InventoryItemRecord(7, 1));
                data.Records.Add(new InventoryItemRecord { ItemId = 8, Count = 1, Instance = instance });
            }
            string json = data.ToJson();
            InventorySaveData decoded = InventorySaveData.FromJson(json);
            Assert.That(mode == InventoryStorageMode.SlotGrid
                ? decoded.Slots[0].IsInstance : decoded.Records[0].IsInstance, Is.False);
            InventoryItemInstance decodedInstance = mode == InventoryStorageMode.SlotGrid
                ? decoded.Slots[1].Instance : decoded.Records[1].Instance;
            Assert.That(decodedInstance.ItemId, Is.EqualTo(itemId));
            Assert.That(decodedInstance.Count, Is.EqualTo(count));
            Assert.That(decodedInstance.InstanceId, Is.EqualTo(instance.InstanceId));
            Provider.SetString(inventory.SaveKey, json);

            inventory.Load();

            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(inventory.GetCount(8), Is.Zero);
            Assert.That(inventory.GetSnapshotInstances(), Is.Empty);
            if (mode == InventoryStorageMode.SlotGrid)
            {
                Assert.That(inventory.SlotCapacity, Is.EqualTo(2));
                Assert.That(inventory.GetSlot(1).IsEmpty, Is.True);
            }
        }

        [Test]
        public void LegacyEntries_PreserveConfiguredGridModeAndCapacity()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 5,
                database: CreateDatabase(CreateItem(7, 1)));
            Provider.SetString(inventory.SaveKey, "{\"Entries\":[{\"ItemId\":7,\"Count\":3}]}");

            inventory.Load();

            Assert.That(inventory.IsSlotInventory, Is.True);
            Assert.That(inventory.SlotCapacity, Is.EqualTo(5));
            Assert.That(inventory.GetCount(7), Is.EqualTo(3));
            Assert.That(inventory.GetSlot(2).Count, Is.EqualTo(1));
        }

        [Test]
        public void EmptyGridSave_RestoresZeroCapacityGrid()
        {
            InventoryComponent source = CreateInventory(InventoryStorageMode.SlotGrid, 0);
            source.Save();
            InventoryComponent loaded = CreateInventory(saveKey: source.SaveKey);

            loaded.Load();

            Assert.That(loaded.IsSlotInventory, Is.True);
            Assert.That(loaded.SlotCapacity, Is.Zero);
            Assert.That(loaded.AddItemById(7), Is.Zero);
        }

        [TestCase("")]
        [TestCase("AuditInitialOnly")]
        public void InitialOnlyLoad_AppliesInitialItemsWithoutReadingProvider(string saveKey)
        {
            InventoryComponent inventory = CreateInventory(saveKey: saveKey);
            Configure(inventory, serialized =>
            {
                serialized.FindProperty("_loadMode").enumValueIndex = (int)InventoryLoadMode.InitialOnlyIgnoreSave;
                UnityEditor.SerializedProperty entries = serialized.FindProperty("_initialEntries");
                entries.arraySize = 1;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("ItemId").intValue = 7;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("Count").intValue = 2;
            });

            inventory.Load();

            Assert.That(inventory.GetCount(7), Is.EqualTo(2));
            Assert.That(Provider.StringReads, Is.Zero);
        }

        [TestCase(-1, 1)]
        [TestCase(8, 2)]
        public void LoadSlots_RejectsInvalidOrOverCapacityInstance(int itemId, int count)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 2, maxTotal: 2);
            InventorySaveData data = new()
            {
                StorageMode = (int)InventoryStorageMode.SlotGrid,
                SlotCapacity = 2,
                Slots = new List<InventorySlotState>
                {
                    new() { ItemId = 7, Count = 1 },
                    new() { Instance = new InventoryItemInstance(itemId, count) }
                }
            };
            Provider.SetString(inventory.SaveKey, data.ToJson());

            InventorySaveData decoded = InventorySaveData.FromJson(Provider.GetString(inventory.SaveKey));
            Assert.That(decoded.Slots[0].IsInstance, Is.False, "The valid first row must remain a simple stack.");
            Assert.That(decoded.Slots[0].EffectiveItemId, Is.EqualTo(7));
            Assert.That(decoded.Slots[0].EffectiveCount, Is.EqualTo(1));
            Assert.That(decoded.Slots[1].Instance.ItemId, Is.EqualTo(itemId));
            Assert.That(decoded.Slots[1].Instance.Count, Is.EqualTo(count));

            inventory.Load();

            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
            Assert.That(inventory.GetSlot(1).IsEmpty, Is.True);
            Assert.That(inventory.GetSnapshotInstances(), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PartialPickup_PreservesRemainderAndCollidersUntilFullyCollected(bool instances)
        {
            InventoryComponent inventory = CreateInventory(maxTotal: 2,
                database: CreateDatabase(CreateItem(7, instanceState: instances)));
            inventory.AddItemById(7);
            PickableItem pickup = CreatePickup(inventory, 7, 3);
            BoxCollider collider = pickup.gameObject.AddComponent<BoxCollider>();
            pickup.gameObject.SetActive(true);
            List<int> collected = new();
            int rejected = 0;
            pickup.OnCollected.AddListener((itemId, amount) => collected.Add(amount));
            inventory.OnCapacityRejected.AddListener((itemId, amount) => rejected++);

            Assert.That(pickup.Collect(), Is.True);
            Assert.That(inventory.GetCount(7), Is.EqualTo(2));
            Assert.That(pickup.Amount, Is.EqualTo(2));
            Assert.That(rejected, Is.EqualTo(1));
            Assert.That(collider.enabled, Is.True);
            Assert.That(pickup.gameObject.activeSelf, Is.True);
            Assert.That(pickup.Collect(), Is.False);
            inventory.RemoveItemByIdAmount(7, 2);

            Assert.That(pickup.Collect(), Is.True);
            Assert.That(inventory.GetCount(7), Is.EqualTo(2));
            Assert.That(collected, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(collider.enabled, Is.False);
            Assert.That(pickup.Collect(), Is.False);
        }

        [Test]
        public void Pickup_ReentrantStartedListener_DoesNotGrantTwice()
        {
            InventoryComponent inventory = CreateInventory();
            PickableItem pickup = CreatePickup(inventory, 7, 1);
            bool attempted = false;
            bool nestedResult = true;
            pickup.OnCollectStarted.AddListener(() =>
            {
                if (!attempted)
                {
                    attempted = true;
                    nestedResult = pickup.Collect();
                }
            });

            Assert.That(pickup.Collect(), Is.True);
            Assert.That(nestedResult, Is.False);
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
        }

        [Test]
        public void Configure_RearmsPreviouslyCollectedPickup()
        {
            InventoryComponent inventory = CreateInventory();
            PickableItem pickup = CreatePickup(inventory, 7, 1);
            Assert.That(pickup.Collect(), Is.True);

            pickup.Configure(null, 8, 2, inventory);

            Assert.That(pickup.Collect(), Is.True);
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(inventory.GetCount(8), Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FullPickup_DisablesChildCollidersIncludingInactiveChildren(bool physics2D)
        {
            InventoryComponent inventory = CreateInventory();
            PickableItem pickup = CreatePickup(inventory, 7, 1);
            GameObject child = CreateObject("PickupColliderChild");
            child.transform.SetParent(pickup.transform);
            if (physics2D)
            {
                BoxCollider2D collider = child.AddComponent<BoxCollider2D>();
                Assert.That(pickup.Collect(), Is.True);
                Assert.That(collider.enabled, Is.False);
            }
            else
            {
                BoxCollider collider = child.AddComponent<BoxCollider>();
                Assert.That(pickup.Collect(), Is.True);
                Assert.That(collider.enabled, Is.False);
            }
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
        }

        [Test]
        public void InstanceDataAdd_StopsAfterFirstCapacityRejection()
        {
            InventoryComponent inventory = CreateInventory(maxTotal: 1);
            inventory.AddItemById(7);
            InventoryItemData item = CreateItem(8, instanceState: true);
            int rejected = 0;
            inventory.OnCapacityRejected.AddListener((itemId, count) => rejected++);

            Assert.That(inventory.AddItemData(item, 4), Is.Zero);
            Assert.That(rejected, Is.EqualTo(1));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
        }

        [TestCase("packed")]
        [TestCase("slot")]
        [TestCase("first")]
        [TestCase("last")]
        public void DisabledDropper_AllIndexEntryPointsLeaveItemsUntouched(string entryPoint)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.AddItemByIdAmount(7, 2);
            InventoryDropper dropper = CreateDropper(inventory, CreateObject("DropPrefab"));
            dropper.CanDrop = false;
            int changed = 0;
            inventory.OnInventoryChanged.AddListener(() => changed++);

            int dropped = entryPoint switch
            {
                "packed" => dropper.DropPackedIndex(0),
                "slot" => dropper.DropSlot(0),
                "first" => dropper.DropFirst(),
                _ => dropper.DropLast()
            };

            Assert.That(dropped, Is.Zero);
            Assert.That(inventory.GetSlot(0).Count, Is.EqualTo(2));
            Assert.That(changed, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DropWithMissingPrefab_DoesNotReorderGridOrEmitMutations(bool physical)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.TrySetSlot(3, new InventorySlotState { ItemId = 7, Count = 2 });
            InventoryDropper dropper = CreateDropper(inventory);
            int changed = 0;
            int failures = 0;
            inventory.OnInventoryChanged.AddListener(() => changed++);
            dropper.OnDropFailed.AddListener((itemId, count) => failures++);

            Assert.That(physical ? dropper.DropSlot(3) : dropper.DropPackedIndex(0), Is.Zero);

            Assert.That(inventory.GetSlot(3).Count, Is.EqualTo(2));
            Assert.That(inventory.GetSlot(0).IsEmpty, Is.True);
            Assert.That(changed, Is.Zero);
            Assert.That(failures, Is.EqualTo(1));
        }

        [Test]
        public void DropSuccessListenerThrows_DoesNotReAddSpawnedItem()
        {
            InventoryComponent inventory = CreateInventory();
            inventory.AddItemById(7);
            InventoryDropper dropper = CreateDropper(inventory, CreateObject("DropPrefab"));
            GameObject dropped = null;
            int failures = 0;
            dropper.OnDropFailed.AddListener((itemId, count) => failures++);
            dropper.OnItemDropped.AddListener((itemId, count, spawned) =>
            {
                dropped = spawned;
                throw new InvalidOperationException("observer");
            });

            Assert.Throws<InvalidOperationException>(() => dropper.DropById(7));

            Assert.That(dropped, Is.Not.Null);
            Assert.That(inventory.GetCount(7), Is.Zero);
            Assert.That(failures, Is.Zero);
        }

        [Test]
        public void DropRestoreFailure_ReleasesSpawnedObjectAndRestoresInventoryRecord()
        {
            InventoryComponent inventory = CreateInventory();
            GameObject prefab = CreateObject("FailingDropPrefab");
            RestoreFailureState state = prefab.AddComponent<RestoreFailureState>();
            InventoryItemInstance instance = new(7);
            instance.ComponentStates.Add(new InventoryItemComponentState(state.InventoryStateKey, "{}"));
            inventory.AddItemInstance(instance);
            InventoryDropper dropper = CreateDropper(inventory, prefab);
            Configure(dropper, serialized => serialized.FindProperty("_configurePickableItem").boolValue = true);
            int before = Resources.FindObjectsOfTypeAll<RestoreFailureState>().Length;
            bool previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            try
            {
                NeoDiagnostics.Configure(errors: true);
                LogAssert.Expect(LogType.Error, "[InventoryDropper] Failed to spawn dropped item 7: restore failed");
                Assert.That(dropper.DropById(7), Is.Zero);
            }
            finally
            {
                NeoDiagnostics.Configure(errors: previousErrors);
            }

            RestoreFailureState[] remaining = Resources.FindObjectsOfTypeAll<RestoreFailureState>();
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] != state)
                {
                    CreatedObjects.Add(remaining[i].gameObject);
                }
            }
            Assert.That(remaining.Length, Is.EqualTo(before));
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(inventory.GetSnapshotInstances()[0].InstanceId, Is.EqualTo(instance.InstanceId));
            Assert.That(inventory.GetSnapshotInstances()[0].ComponentStates[0].Json, Is.EqualTo("{}"));
        }



        private PickableItem CreatePickup(InventoryComponent inventory, int itemId, int amount)
        {
            PickableItem pickup = CreateObject("AuditPickup").AddComponent<PickableItem>();
            Configure(pickup, serialized =>
            {
                serialized.FindProperty("_requireCollectorInventory").boolValue = false;
                serialized.FindProperty("_destroyAfterCollect").boolValue = false;
            });
            pickup.Configure(null, itemId, amount, inventory);
            return pickup;
        }

        private static void AssertDomainInlineValue(InventoryItemInstance instance)
        {
            if (instance == null)
            {
                return;
            }
            Assert.That(instance.ItemId, Is.Zero, "Unexpected inline item id; do not broaden normalization.");
            Assert.That(instance.Count, Is.EqualTo(1), "P5 observed the domain Count initializer, not a zero placeholder.");
            Assert.That(instance.StateVersion, Is.Zero);
            Assert.That(string.IsNullOrEmpty(instance.InstanceId), Is.True);
            Assert.That(instance.ComponentStates == null || instance.ComponentStates.Count == 0, Is.True);
        }

        [Serializable]
        private sealed class RawSaveDataProbe
        {
            public List<InventorySlotState> Slots = new();
            public List<InventoryItemRecord> Records = new();
        }

        private static void AssertEmptyWireInlineValue(RawWireInstanceProbe instance)
        {
            if (instance == null)
            {
                return;
            }
            Assert.That(instance.ItemId, Is.Zero);
            Assert.That(instance.Count, Is.Zero, "Wire defaults must not invent an item-0 instance.");
            Assert.That(instance.StateVersion, Is.Zero);
            Assert.That(string.IsNullOrEmpty(instance.InstanceId), Is.True);
            Assert.That(instance.ComponentStates == null || instance.ComponentStates.Count == 0, Is.True);
        }

        [Serializable]
        private sealed class RawWireSaveDataProbe
        {
            public List<RawWireRowProbe> Slots;
            public List<RawWireRowProbe> Records;
        }

        [Serializable]
        private sealed class RawWireRowProbe
        {
            public int ItemId;
            public int Count;
            public RawWireInstanceProbe Instance;
        }

        [Serializable]
        private sealed class RawWireInstanceProbe
        {
            public string InstanceId;
            public int ItemId;
            public int Count;
            public int StateVersion;
            public List<InventoryItemComponentState> ComponentStates;
        }

        private InventoryDropper CreateDropper(InventoryComponent inventory, GameObject prefab = null)
        {
            InventoryDropper dropper = CreateObject("AuditDropper").AddComponent<InventoryDropper>();
            Configure(dropper, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_autoFindInventory").boolValue = false;
                serialized.FindProperty("_fallbackDropPrefab").objectReferenceValue = prefab;
                serialized.FindProperty("_addColliderIfMissing").boolValue = false;
                serialized.FindProperty("_addRigidbody3D").boolValue = false;
                serialized.FindProperty("_configurePickableItem").boolValue = false;
            });
            dropper.OnItemDropped.AddListener((itemId, count, spawned) => CreatedObjects.Add(spawned));
            return dropper;
        }
    }
}
