using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using WalletState = global::Neo.Tools.Tests.Audit20261023FixtureInventorySystemTestWalletState;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Hot-path contracts of the inventory: clone-free count capture, unchanged event order, packed queries,
    ///     the slot-grid running total, selection events, cached count labels, the reused hand visual and Unity
    ///     null semantics of the bridge/slot adapters.
    /// </summary>
    public sealed class InventoryHotPathTests : Audit20261023NeoToolsInventoryFixture
    {
        private const int MeasuredCycles = 20;
        private const int FewStoredInstances = 4;
        private const int ManyStoredInstances = 256;

        [TestCase(InventoryStorageMode.Aggregated)]
        [TestCase(InventoryStorageMode.SlotGrid)]
        public void Mutation_AllocationDoesNotScaleWithStoredInstancePayloads(InventoryStorageMode mode)
        {
            AllocationCounter.RequireWorkingCounter(ManyStoredInstances);
            long few = MeasureAddRemoveAllocations(mode, FewStoredInstances, out long fewSnapshot);
            long many = MeasureAddRemoveAllocations(mode, ManyStoredInstances, out long manySnapshot);

            // WHY: Positive control on the same inventories: one deep snapshot clones every stored payload, which
            // is the cost the old count capture paid twice per mutation. It must register before the absence of
            // that cost on the mutation path can mean anything.
            Assert.That(manySnapshot - fewSnapshot,
                Is.GreaterThanOrEqualTo(ManyStoredInstances - FewStoredInstances),
                "The counter must see the per-payload clones of GetSnapshotRecords().");

            // WHY: Counts GC.Alloc calls. The old capture cloned 252 extra payloads twice per mutation (thousands of
            // extra allocations over the cycles); the clone-free capture allocates the same per mutation regardless
            // of how many payloads are stored.
            Assert.That(many - few, Is.LessThan(MeasuredCycles),
                $"Mutation allocations grew with stored payloads: {few} with {FewStoredInstances}, {many} with " +
                $"{ManyStoredInstances}.");
        }

        [Test]
        public void DeltaEvents_ReplacingASlot_PublishRemovalBeforeAddition()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 2);
            Assert.That(inventory.AddItemByIdAmount(1, 2), Is.EqualTo(2));
            List<string> events = new();
            inventory.OnItemAdded.AddListener((id, amount) => events.Add($"added {id}:{amount}"));
            inventory.OnItemRemoved.AddListener((id, amount) => events.Add($"removed {id}:{amount}"));
            inventory.OnItemCountChanged.AddListener((id, count) => events.Add($"count {id}:{count}"));
            inventory.OnItemBecameZero.AddListener(id => events.Add($"zero {id}"));
            inventory.OnInventoryChanged.AddListener(() => events.Add("changed"));

            Assert.That(inventory.TrySetSlot(0, new InventorySlotState { ItemId = 3, Count = 4 }), Is.True);

            Assert.That(events, Is.EqualTo(new[]
            {
                "removed 1:2", "count 1:0", "zero 1", "added 3:4", "count 3:4", "changed"
            }));
        }

        [TestCase(InventoryStorageMode.Aggregated)]
        [TestCase(InventoryStorageMode.SlotGrid)]
        public void PackedQueries_MatchTheRecordSnapshot(InventoryStorageMode mode)
        {
            InventoryComponent inventory = CreateInventory(mode, 6);
            Assert.That(inventory.GetFirstItemId(), Is.EqualTo(-1));
            Assert.That(inventory.GetLastItemId(), Is.EqualTo(-1));
            Assert.That(inventory.GetNonEmptySlotCount(), Is.Zero);
            if (mode == InventoryStorageMode.SlotGrid)
            {
                Assert.That(inventory.TrySetSlot(1, new InventorySlotState { ItemId = 5, Count = 3 }), Is.True);
                Assert.That(inventory.TrySetSlot(3, new InventorySlotState
                {
                    ItemId = 9, Count = 1, Instance = new InventoryItemInstance(9)
                }), Is.True);
                Assert.That(inventory.TrySetSlot(5, new InventorySlotState { ItemId = 2, Count = 1 }), Is.True);
            }
            else
            {
                inventory.AddItemByIdAmount(5, 3);
                inventory.AddItemInstance(new InventoryItemInstance(9));
                inventory.AddItemByIdAmount(2, 1);
            }

            List<InventoryItemRecord> records = inventory.GetSnapshotRecords();
            List<InventoryEntry> entries = inventory.GetSnapshotEntries();
            Assert.That(entries.Count, Is.EqualTo(records.Count));
            for (int i = 0; i < records.Count; i++)
            {
                Assert.That(entries[i].ItemId, Is.EqualTo(records[i].EffectiveItemId));
                Assert.That(entries[i].Count, Is.EqualTo(records[i].EffectiveCount));
            }

            Assert.That(inventory.GetNonEmptySlotCount(), Is.EqualTo(3));
            Assert.That(inventory.GetFirstItemId(), Is.EqualTo(5));
            Assert.That(inventory.GetLastItemId(), Is.EqualTo(2));
            Assert.That(inventory.TryTakeFirstRecordByItemId(9, 1, out InventoryItemRecord taken), Is.True);
            Assert.That(taken.IsInstance, Is.True);
            Assert.That(inventory.GetCount(9), Is.Zero);
            Assert.That(inventory.TryTakeFirstRecordByItemId(9, 1, out InventoryItemRecord missing), Is.False);
            Assert.That(missing, Is.Null);
        }

        [TestCase(InventoryStorageMode.Aggregated)]
        [TestCase(InventoryStorageMode.SlotGrid)]
        public void Save_WritesTheSameJsonAsTheSnapshotSerialization(InventoryStorageMode mode)
        {
            string key = Guid.NewGuid().ToString("N");
            InventoryComponent inventory = CreateInventory(mode, 4, saveKey: key);
            inventory.AddItemByIdAmount(1, 3);
            InventoryItemInstance instance = new(9);
            instance.ComponentStates.Add(new InventoryItemComponentState("ammo", "7"));
            Assert.That(inventory.AddItemInstance(instance), Is.EqualTo(1));
            List<InventorySlotState> slots = new();
            if (mode == InventoryStorageMode.SlotGrid)
            {
                for (int i = 0; i < inventory.SlotCapacity; i++)
                {
                    slots.Add(inventory.GetSlot(i));
                }
            }

            InventorySaveData expected = new()
            {
                Version = 1,
                InitialStateMerged = true,
                StorageMode = (int)mode,
                SlotCapacity = 4,
                Slots = slots,
                Records = mode == InventoryStorageMode.SlotGrid
                    ? new List<InventoryItemRecord>()
                    : inventory.GetSnapshotRecords()
            };

            inventory.Save();

            Assert.That(Provider.GetString(key, ""), Is.EqualTo(expected.ToJson()));
        }

        [Test]
        public void SlotGridAdd_RunningTotal_StillHonoursTheTotalCapAcrossStacks()
        {
            InventoryItemData item = CreateItem(1, 3);
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 4, 7,
                database: CreateDatabase(item));
            Assert.That(inventory.AddItemByIdAmount(1, 2), Is.EqualTo(2));

            Assert.That(inventory.AddItemByIdAmount(1, 10), Is.EqualTo(5));

            Assert.That(inventory.GetSlot(0).Count, Is.EqualTo(3));
            Assert.That(inventory.GetSlot(1).Count, Is.EqualTo(3));
            Assert.That(inventory.GetSlot(2).Count, Is.EqualTo(1));
            Assert.That(inventory.GetSlot(3).IsEmpty, Is.True);
            Assert.That(inventory.TotalItemCount, Is.EqualTo(7));
        }

        [Test]
        public void SelectedItemId_RaisesItsEventOnlyOnChange_WithoutAnInventoryMutation()
        {
            InventoryComponent inventory = CreateInventory();
            inventory.AddItemByIdAmount(1, 2);
            inventory.AddItemByIdAmount(2, 5);
            List<int> selections = new();
            int inventoryChanges = 0;
            inventory.OnSelectedItemChanged.AddListener(selections.Add);
            inventory.OnInventoryChanged.AddListener(() => inventoryChanges++);

            inventory.SelectedItemId = 1;
            inventory.SelectedItemId = 1;
            inventory.SelectedItemId = 2;

            Assert.That(selections, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(inventoryChanges, Is.Zero);
            Assert.That(inventory.SelectedItemCount, Is.EqualTo(5));
        }

        [Test]
        public void SelectedCountLabel_FollowsTheSelectionWithoutAnInventoryMutation()
        {
            InventoryComponent inventory = CreateInventory();
            inventory.AddItemByIdAmount(1, 2);
            inventory.AddItemByIdAmount(2, 5);
            GameObject labelObject = CreateObject("SelectedCountLabel");
            TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
            InventoryTotalCountText label = labelObject.AddComponent<InventoryTotalCountText>();
            Configure(label, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_targetText").objectReferenceValue = text;
                serialized.FindProperty("_mode").enumValueIndex = (int)InventoryCountViewMode.Selected;
            });
            InvokeLifecycle(label, "OnEnable");
            try
            {
                inventory.SelectedItemId = 1;
                Assert.That(text.text, Is.EqualTo("2"));

                inventory.SelectedItemId = 2;
                Assert.That(text.text, Is.EqualTo("5"));
            }
            finally
            {
                InvokeLifecycle(label, "OnDisable");
            }
        }

        [Test]
        public void CountLabel_RewritesTextThatOtherCodeChanged()
        {
            InventoryComponent inventory = CreateInventory();
            inventory.AddItemByIdAmount(1, 2);
            GameObject labelObject = CreateObject("ItemCountLabel");
            TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
            InventoryItemCountText label = labelObject.AddComponent<InventoryItemCountText>();
            Configure(label, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_targetText").objectReferenceValue = text;
                serialized.FindProperty("_itemId").intValue = 1;
            });

            label.Refresh();
            Assert.That(text.text, Is.EqualTo("2"));
            text.text = "stale";
            label.Refresh();
            Assert.That(text.text, Is.EqualTo("2"));
            inventory.AddItemById(1);
            label.Refresh();
            Assert.That(text.text, Is.EqualTo("3"));
        }

        [TestCase(InventoryStorageMode.Aggregated, false)]
        [TestCase(InventoryStorageMode.SlotGrid, false)]
        [TestCase(InventoryStorageMode.SlotGrid, true)]
        public void ItemIdReads_MatchTheRecordPath_WithoutCloningThePayload(InventoryStorageMode mode,
            bool physicalHandSlots)
        {
            const int reads = 256;
            InventoryComponent inventory = CreateInventory(mode, 6);
            inventory.AddItemByIdAmount(1, 3);
            InventoryItemInstance instance = new(7);
            instance.ComponentStates.Add(new InventoryItemComponentState("durability", "{\"value\":100}"));
            Assert.That(inventory.AddItemInstance(instance), Is.EqualTo(1));
            inventory.AddItemByIdAmount(2, 1);
            for (int i = -1; i <= 4; i++)
            {
                InventoryItemRecord record = inventory.GetRecordAtSlotIndex(i);
                Assert.That(inventory.GetItemIdAtSlotIndex(i), Is.EqualTo(record != null ? record.EffectiveItemId : -1),
                    "packed index " + i);
            }

            InventoryHand hand = CreateHand(inventory, CreateObject("HandPrefab"));
            Configure(hand, serialized => serialized.FindProperty("_usePhysicalSlotIndices").boolValue =
                physicalHandSlots);
            hand.SetSlotIndex(1);
            Assert.That(hand.EquippedItemId, Is.EqualTo(7), "The instance item sits at index 1 in both layouts.");
            AllocationCounter.RequireWorkingCounter(reads);

            int packedId = -2;
            int equippedId = -2;
            long calls = AllocationCounter.Count(_ =>
            {
                packedId = inventory.GetItemIdAtSlotIndex(1);
                equippedId = hand.EquippedItemId;
            }, reads);

            Assert.That(packedId, Is.EqualTo(7));
            Assert.That(equippedId, Is.EqualTo(7));
            // WHY: the record path cloned the record and its instance payload (several GC.Alloc calls per read).
            Assert.That(calls, Is.LessThan(reads / 4),
                "Item id reads must not clone records; NeoCondition polls InventoryHand.EquippedItemId.");
        }

        [TestCase(InventoryStorageMode.Aggregated)]
        [TestCase(InventoryStorageMode.SlotGrid)]
        public void CountLabels_UnchangedRefresh_DoNotAllocatePerRefresh(InventoryStorageMode mode)
        {
            const int refreshes = 256;
            InventoryComponent inventory = CreateInventory(mode, 8);
            inventory.AddItemByIdAmount(1, 2);
            inventory.AddItemByIdAmount(2, 5);
            inventory.SelectedItemId = 2;
            GameObject itemObject = CreateObject("ItemCountLabelAlloc");
            TextMeshProUGUI itemText = itemObject.AddComponent<TextMeshProUGUI>();
            InventoryItemCountText itemLabel = itemObject.AddComponent<InventoryItemCountText>();
            Configure(itemLabel, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_targetText").objectReferenceValue = itemText;
                serialized.FindProperty("_itemId").intValue = 1;
            });
            GameObject totalObject = CreateObject("SelectedCountLabelAlloc");
            TextMeshProUGUI totalText = totalObject.AddComponent<TextMeshProUGUI>();
            InventoryTotalCountText totalLabel = totalObject.AddComponent<InventoryTotalCountText>();
            Configure(totalLabel, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_targetText").objectReferenceValue = totalText;
                serialized.FindProperty("_mode").enumValueIndex = (int)InventoryCountViewMode.Selected;
            });
            itemLabel.Refresh();
            totalLabel.Refresh();
            Assert.That(itemText.text, Is.EqualTo("2"));
            Assert.That(totalText.text, Is.EqualTo("5"));
            AllocationCounter.RequireWorkingCounter(refreshes);

            long calls = AllocationCounter.Count(_ =>
            {
                itemLabel.Refresh();
                totalLabel.Refresh();
            }, refreshes);

            // WHY: string.Format boxes the count and builds a new string, so re-formatting an unchanged value would
            // cost at least two GC.Alloc calls per label refresh; a quarter of the refresh count is far below that.
            Assert.That(calls, Is.LessThan(refreshes / 4),
                "Count labels must skip formatting while the shown value is unchanged.");
            Assert.That(itemText.text, Is.EqualTo("2"));
            Assert.That(totalText.text, Is.EqualTo("5"));
        }

        [Test]
        public void Hand_UnrelatedChangeOrStackGrowth_KeepsTheEquippedVisual()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.AddItemById(7);
            InventoryHand hand = CreateHand(inventory, CreateObject("HandPrefab"));
            hand.SetSlotIndex(0);
            Assert.That(hand.transform.childCount, Is.EqualTo(1));
            Transform visual = hand.transform.GetChild(0);

            inventory.AddItemById(8);
            hand.RefreshSlotFromInventory();
            inventory.AddItemById(7);
            hand.RefreshSlotFromInventory();

            Assert.That(hand.transform.childCount, Is.EqualTo(1));
            Assert.That(hand.transform.GetChild(0), Is.SameAs(visual));
            Assert.That(hand.EquippedItemId, Is.EqualTo(7));
        }

        [Test]
        public void Hand_EquippedPayloadChange_RebuildsTheVisual()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance instance = new(7);
            instance.ComponentStates.Add(new InventoryItemComponentState("ammo", "1"));
            Assert.That(inventory.AddItemInstance(instance), Is.EqualTo(1));
            InventoryHand hand = CreateHand(inventory, CreateObject("HandPrefab"));
            hand.SetSlotIndex(0);
            GameObject visual = hand.transform.GetChild(0).gameObject;

            InventorySlotState slot = inventory.GetSlot(0);
            slot.Instance.ComponentStates[0].Json = "2";
            Assert.That(inventory.TrySetSlot(0, slot), Is.True);
            hand.RefreshSlotFromInventory();

            Assert.That(visual == null, Is.True, "A changed payload must be restored onto a fresh visual.");
            Assert.That(hand.transform.childCount, Is.EqualTo(1));
        }

        [Test]
        public void Hand_ConsumeDuringUse_DoesNotActivateTheKeptVisual()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.AddItemByIdAmount(7, 5);
            GameObject prefab = CreateObject("HandPrefab");
            prefab.AddComponent<PickableItem>();
            InventoryHand hand = CreateHand(inventory, prefab);
            hand.SetSlotIndex(0);
            PickableItem pickable = hand.GetComponentInChildren<PickableItem>(true);
            int activations = 0;
            pickable.OnActivate.AddListener(() => activations++);
            bool consumed = false;
            hand.OnUseSelectionRequested.AddListener(selection =>
            {
                consumed = inventory.TryConsumeSelection(selection);
                hand.RefreshSlotFromInventory();
            });

            hand.UseEquippedItem();

            Assert.That(consumed, Is.True);
            Assert.That(inventory.GetCount(7), Is.EqualTo(4));
            Assert.That(hand.GetComponentInChildren<PickableItem>(true), Is.SameAs(pickable));
            Assert.That(activations, Is.Zero, "A consume during use must suppress activation as before.");

            hand.OnUseSelectionRequested.RemoveAllListeners();
            hand.UseEquippedItem();
            Assert.That(activations, Is.EqualTo(1));
        }

        [Test]
        public void Hand_SelectorSync_WritesTheCountOnce_AndSkipsUnchangedRepublication()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 3);
            inventory.AddItemById(7);
            Selector selector = CreateHotbarSelector(3);
            InventoryHand hand = CreateHand(inventory, CreateObject("HandPrefab"));
            Configure(hand, serialized => serialized.FindProperty("_selector").objectReferenceValue = selector);
            FieldInfo explicitCount = typeof(Selector).GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(explicitCount, Is.Not.Null, "Selector._count expected");
            Assert.That(explicitCount.GetValue(selector), Is.LessThanOrEqualTo(0), "Starts in items mode.");

            hand.SetSlotIndex(0);

            // WHY: The Count getter already reports Items.Length (3) here; the first sync must still write the
            // count, as it always did, so the selector ends in explicit-count mode.
            Assert.That(explicitCount.GetValue(selector), Is.EqualTo(3));
            int publications = 0;
            selector.OnSelectionChanged.AddListener(_ => publications++);
            List<int> selectedIds = new();
            inventory.OnSelectedItemChanged.AddListener(selectedIds.Add);

            inventory.AddItemById(8);
            hand.RefreshSlotFromInventory();
            Assert.That(publications, Is.Zero, "An unrelated pickup must not re-apply an unchanged selector.");

            Assert.That(inventory.TrySetSlot(0, new InventorySlotState { ItemId = 9, Count = 1 }), Is.True);
            hand.RefreshSlotFromInventory();

            Assert.That(publications, Is.Zero, "The slot index did not change, so the selector is not re-applied.");
            Assert.That(selectedIds, Is.EqualTo(new[] { 9 }), "OnSelectedItemChanged reports the new item instead.");
            Assert.That(hand.EquippedItemId, Is.EqualTo(9));
        }

        [Test]
        public void PickupBridge_DestroyedPickable_DoesNotRunCollection()
        {
            InventoryComponent inventory = CreateInventory();
            PickableItem pickable = CreateObject("Pickup").AddComponent<PickableItem>();
            Configure(pickable, serialized =>
            {
                serialized.FindProperty("_itemId").intValue = 7;
                serialized.FindProperty("_requireCollectorInventory").boolValue = false;
                serialized.FindProperty("_targetInventory").objectReferenceValue = inventory;
                serialized.FindProperty("_autoFindInventory").boolValue = false;
            });
            InventoryPickupBridge bridge = CreateObject("Bridge").AddComponent<InventoryPickupBridge>();
            Configure(bridge, serialized => serialized.FindProperty("_pickableItem").objectReferenceValue = pickable);
            Object.DestroyImmediate(pickable);

            Assert.DoesNotThrow(bridge.Collect);
            Assert.DoesNotThrow(() => bridge.CollectFromGameObject(null));
            Assert.That(inventory.GetCount(7), Is.Zero);
        }

        [Test]
        public void SlotClick_AfterTheGridIsDestroyed_DoesNotSelectIt()
        {
            FieldInfo selectedGrid = typeof(InventorySlotGridView).GetField("_selectedGrid",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(selectedGrid, Is.Not.Null);
            selectedGrid.SetValue(null, null);
            try
            {
                InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 2);
                InventorySlotGridView grid = CreateObject("Grid").AddComponent<InventorySlotGridView>();
                grid.SetInventory(inventory);
                InventorySlotView slot = CreateObject("Slot").AddComponent<InventorySlotView>();
                slot.Bind(grid, 0, null, InventorySlotState.Empty(), false);
                Object.DestroyImmediate(grid);

                slot.OnPointerClick(null);

                Assert.That(selectedGrid.GetValue(null), Is.Null);
            }
            finally
            {
                selectedGrid.SetValue(null, null);
            }
        }

        [Test]
        public void StateUtility_FindsStateOnInactiveChildren()
        {
            GameObject root = CreateObject("StateRoot");
            GameObject child = new("StateChild");
            child.transform.SetParent(root.transform, false);
            child.SetActive(false);
            WalletState wallet = child.AddComponent<WalletState>();
            wallet.Coins = 12;

            Assert.That(InventoryItemStateUtility.HasState(root), Is.True);
            Assert.That(InventoryItemStateUtility.HasState(CreateObject("Stateless")), Is.False);
            InventoryItemInstance captured = InventoryItemStateUtility.CaptureInstance(root, 4);
            wallet.Coins = 0;
            InventoryItemStateUtility.RestoreInstance(root, captured);

            Assert.That(captured.ComponentStates.Count, Is.EqualTo(1));
            Assert.That(captured.ComponentStates[0].Key, Is.EqualTo(wallet.InventoryStateKey));
            Assert.That(wallet.Coins, Is.EqualTo(12));
        }

        private long MeasureAddRemoveAllocations(InventoryStorageMode mode, int storedInstances,
            out long snapshotAllocations)
        {
            InventoryComponent inventory = CreateInventory(mode, storedInstances + 4);
            for (int i = 0; i < storedInstances; i++)
            {
                InventoryItemInstance instance = new(7);
                instance.ComponentStates.Add(new InventoryItemComponentState("durability", "{\"value\":100}"));
                Assert.That(inventory.AddItemInstance(instance), Is.EqualTo(1));
            }

            inventory.AddItemById(1);
            inventory.RemoveItemById(1);
            long mutations = AllocationCounter.Count(_ =>
            {
                inventory.AddItemById(1);
                inventory.RemoveItemById(1);
            }, MeasuredCycles);

            List<InventoryItemRecord> snapshot = null;
            snapshotAllocations = AllocationCounter.Count(_ => snapshot = inventory.GetSnapshotRecords(), 1);
            Assert.That(snapshot.Count, Is.EqualTo(storedInstances));
            Assert.That(inventory.GetCount(1), Is.Zero);
            return mutations;
        }

        private Selector CreateHotbarSelector(int slots)
        {
            GameObject root = new("HotbarSelector");
            CreatedObjects.Add(root);
            for (int i = 0; i < slots; i++)
            {
                GameObject highlight = new($"Highlight{i}");
                highlight.transform.SetParent(root.transform, false);
            }

            Selector selector = root.AddComponent<Selector>();
            selector.startOnAwake = false;
            FieldInfo autoUpdate = typeof(Selector).GetField("_autoUpdateFromChildren",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(autoUpdate, Is.Not.Null, "Selector._autoUpdateFromChildren expected");
            autoUpdate.SetValue(selector, false);
            selector.RefreshItems();
            Assert.That(selector.Items.Length, Is.EqualTo(slots));
            return selector;
        }

        private InventoryHand CreateHand(InventoryComponent inventory, GameObject prefab)
        {
            InventoryHand hand = CreateObject("Hand").AddComponent<InventoryHand>();
            Configure(hand, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_autoFindInventory").boolValue = false;
                serialized.FindProperty("_handAnchor").objectReferenceValue = hand.transform;
                serialized.FindProperty("_fallbackHandPrefab").objectReferenceValue = prefab;
            });
            return hand;
        }

        private static void InvokeLifecycle(Object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName + " expected");
            method.Invoke(target, null);
        }

        /// <summary>
        ///     Counts GC.Alloc calls of <c>iterations</c> calls of a body inside one window of the shared calibrated
        ///     <see cref="GcAllocCounter" />.
        /// </summary>
        /// <remarks>
        ///     WHY: <see cref="GC.GetAllocatedBytesForCurrentThread" /> reads 0 in the Mono Editor, which made the
        ///     earlier byte-based guard unable to fail; the guard first proves the counter with
        ///     <see cref="RequireWorkingCounter" />.
        /// </remarks>
        private static class AllocationCounter
        {
            private static object _sink;

            public static long Count(Action<int> body, int iterations)
            {
                long calls;
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    try
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            body(i);
                        }
                    }
                    finally
                    {
                        calls = counter.End();
                        _sink = null;
                    }
                }

                return calls;
            }

            public static void RequireWorkingCounter(int iterations)
            {
                long counted = Count(_ => _sink = new object(), iterations);
                Assert.That(counted, Is.GreaterThanOrEqualTo(iterations),
                    "The GC.Alloc counter missed deliberate allocations, so this allocation guard would be vacuous.");
            }
        }
    }
}
