using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RestoreFailureState = global::Neo.Tests.Edit.Audit20261023FixtureNeoToolsInventoryRuntimeRestoreFailureState;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023NeoToolsInventoryCommitTests : Audit20261023NeoToolsInventoryFixture
    {
        private bool _previousErrors;

        [SetUp]
        public void EnableExpectedDiagnostics()
        {
            _previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: true);
        }

        [TearDown]
        public void RestoreDiagnostics()
        {
            NeoDiagnostics.Configure(errors: _previousErrors);
        }

        [TestCase("changed", true)]
        [TestCase("saved", true)]
        [TestCase("removed", true)]
        [TestCase("changed", false)]
        [TestCase("saved", false)]
        [TestCase("added", false)]
        public void PairedCommit_ObserverFailureStillAttemptsBothSaves(string observer, bool sourceFault)
        {
            InventoryComponent source = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryComponent target = CreateInventory(InventoryStorageMode.SlotGrid);
            Assert.That(source.AddItemById(7), Is.EqualTo(1), "Positive transfer source must exist.");
            Assert.That(source.GetSlot(0).EffectiveItemId, Is.EqualTo(7));
            Assert.That(source.GetSlot(0).IsInstance, Is.False);
            Assert.That(target.GetSlot(2).IsEmpty, Is.True);
            Assert.That(source.SaveKey, Is.Not.EqualTo(target.SaveKey));
            EnableAutoSave(source);
            EnableAutoSave(target);
            int sourceSaves = 0;
            int targetSaves = 0;
            int sourceWrites = 0;
            int targetWrites = 0;
            int faultCalls = 0;
            Provider.OnKeyChanged += key =>
            {
                if (key == source.SaveKey)
                {
                    sourceWrites++;
                }
                else if (key == target.SaveKey)
                {
                    targetWrites++;
                }
            };
            source.OnSaved.AddListener(() => sourceSaves++);
            target.OnSaved.AddListener(() => targetSaves++);
            InvalidOperationException fault = new("first observer");
            ThrowOnce(sourceFault ? source : target, observer, fault, () => faultCalls++);

            Assert.That(Assert.Throws<InvalidOperationException>(
                () => InventoryTransferService.Transfer(source, 0, target, 2)), Is.SameAs(fault));

            Assert.That(faultCalls, Is.EqualTo(1), "The selected real observer must supply the fault.");
            Assert.That(sourceSaves, Is.EqualTo(1), "Source finalization must finish despite an observer fault.");
            Assert.That(targetSaves, Is.EqualTo(1), "Target finalization must finish despite an observer fault.");
            Assert.That(sourceWrites, Is.EqualTo(1), "OnSaved must follow an actual source provider write.");
            Assert.That(targetWrites, Is.EqualTo(1), "OnSaved must follow an actual target provider write.");
            Assert.That(source.GetCount(7) + target.GetCount(7), Is.EqualTo(1));
            Assert.That(source.GetSlot(0).IsEmpty, Is.True, "The committed source must be empty before JSON inspection.");
            Assert.That(source.TotalItemCount, Is.Zero);
            Assert.That(target.GetSlot(2).EffectiveItemId, Is.EqualTo(7));
            Assert.That(target.GetSlot(2).EffectiveCount, Is.EqualTo(1));
            Assert.That(target.GetSlot(2).IsInstance, Is.False);
            Assert.That(Provider.GetString(source.SaveKey), Is.Not.Empty);
            Assert.That(Provider.GetString(target.SaveKey), Is.Not.Empty);
            InventorySaveData savedSource = InventorySaveData.FromJson(Provider.GetString(source.SaveKey));
            InventorySaveData savedTarget = InventorySaveData.FromJson(Provider.GetString(target.SaveKey));
            Assert.That(savedSource.Slots[0].IsEmpty, Is.True, "Saved source must remain semantically empty.");
            Assert.That(savedTarget.Slots[2].EffectiveItemId, Is.EqualTo(7));
            Assert.That(savedTarget.Slots[2].EffectiveCount, Is.EqualTo(1));
            Assert.That(savedTarget.Slots[2].IsInstance, Is.False, "A saved simple stack must not become an instance.");

            InventoryComponent restoredSource = CreateInventory(saveKey: source.SaveKey);
            InventoryComponent restoredTarget = CreateInventory(saveKey: target.SaveKey);
            restoredSource.Load();
            restoredTarget.Load();
            Assert.That(restoredSource.StorageMode, Is.EqualTo(InventoryStorageMode.SlotGrid));
            Assert.That(restoredSource.SlotCapacity, Is.EqualTo(source.SlotCapacity));
            Assert.That(restoredSource.TotalItemCount, Is.Zero);
            Assert.That(restoredSource.GetSnapshotInstances(), Is.Empty);
            Assert.That(restoredSource.GetSlot(0).IsEmpty, Is.True);
            Assert.That(restoredTarget.TotalItemCount, Is.EqualTo(1));
            Assert.That(restoredTarget.GetSlot(2).EffectiveItemId, Is.EqualTo(7));
            Assert.That(restoredTarget.GetSlot(2).EffectiveCount, Is.EqualTo(1));
            Assert.That(restoredTarget.GetSlot(2).IsInstance, Is.False);
        }

        [Test]
        public void PairedCommit_TwoFaultsRethrowFirstAfterBothPersistenceAttempts()
        {
            InventoryComponent source = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryComponent target = CreateInventory(InventoryStorageMode.SlotGrid);
            source.AddItemById(7);
            EnableAutoSave(source);
            EnableAutoSave(target);
            InvalidOperationException first = new("source changed");
            ThrowOnce(source, "changed", first);
            ThrowOnce(target, "saved", new InvalidOperationException("target saved"));

            Assert.That(Assert.Throws<InvalidOperationException>(
                () => InventoryTransferService.Transfer(source, 0, target, 1)), Is.SameAs(first));

            Assert.That(Provider.HasKey(source.SaveKey), Is.True);
            Assert.That(Provider.HasKey(target.SaveKey), Is.True);
            Assert.That(source.TotalItemCount, Is.Zero);
            Assert.That(target.GetSlot(1).EffectiveCount, Is.EqualTo(1));
        }

        [TestCase(false, "added")]
        [TestCase(false, "changed")]
        [TestCase(false, "saved")]
        [TestCase(true, "added")]
        [TestCase(true, "changed")]
        [TestCase(true, "saved")]
        public void Pickup_ExactReceiptSettlesWorldBeforeFailingNotification(bool instance, string observer)
        {
            InventoryComponent inventory = CreateInventory();
            EnableAutoSave(inventory);
            PickableItem pickup = NewPickup(inventory);
            InventoryItemInstance original = NewInstance();
            if (instance)
            {
                pickup.ConfigureInstance(null, original, inventory);
            }
            else
            {
                pickup.Configure(null, 7, 1, inventory);
            }

            int worldAmountAtAdd = -1;
            inventory.OnItemAdded.AddListener((itemId, amount) => worldAmountAtAdd = pickup.Amount);
            InvalidOperationException fault = new("accepted notification");
            ThrowOnce(inventory, observer, fault);
            Assert.That(Assert.Throws<InvalidOperationException>(() => pickup.Collect()), Is.SameAs(fault));

            Assert.That(worldAmountAtAdd, Is.Zero);
            Assert.That(pickup.Amount, Is.Zero);
            Assert.That(pickup.Collect(), Is.False);
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(Provider.HasKey(inventory.SaveKey), Is.True);
            if (instance)
            {
                Assert.That(inventory.GetSnapshotInstances()[0].InstanceId, Is.EqualTo(original.InstanceId));
                Assert.That(inventory.GetSnapshotInstances()[0].ComponentStates[0].Json, Is.EqualTo("payload"));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pickup_FullGrantFinalizesDespawnDespiteTwoObserverFaults(bool instance)
        {
            InventoryComponent inventory = CreateInventory();
            PickableItem pickup = NewPickup(inventory);
            Configure(pickup, serialized => serialized.FindProperty("_deactivateAfterCollect").boolValue = true);
            if (instance)
            {
                pickup.ConfigureInstance(null, NewInstance(), inventory);
            }
            else
            {
                pickup.Configure(null, 7, 1, inventory);
            }

            Collider collider = pickup.gameObject.AddComponent<BoxCollider>();
            pickup.gameObject.SetActive(true);
            InvalidOperationException first = new("collected observer");
            int despawnAttempts = 0;
            pickup.OnCollected.AddListener((id, count) => throw first);
            pickup.OnAfterCollectDespawn.AddListener(() =>
            {
                despawnAttempts++;
                throw new InvalidOperationException("despawn observer");
            });

            Assert.That(Assert.Throws<InvalidOperationException>(() => pickup.Collect()), Is.SameAs(first));

            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(pickup.Amount, Is.Zero);
            Assert.That(collider.enabled, Is.False);
            Assert.That(despawnAttempts, Is.EqualTo(1));
            Assert.That(pickup.gameObject.activeSelf, Is.False);
            Assert.That(pickup.Collect(), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PartialPickup_FaultLeavesOnlyActualRemainderCollectable(bool instance)
        {
            InventoryComponent inventory = CreateInventory(maxTotal: 1);
            PickableItem pickup = NewPickup(inventory);
            InventoryItemInstance original = NewInstance(2);
            if (instance)
            {
                pickup.ConfigureInstance(null, original, inventory);
            }
            else
            {
                pickup.Configure(null, 7, 2, inventory);
            }

            ThrowOnce(inventory, "changed", new InvalidOperationException("partial"));
            Assert.Throws<InvalidOperationException>(() => pickup.Collect());
            Assert.That(inventory.GetCount(7) + pickup.Amount, Is.EqualTo(2));
            Assert.That(pickup.Amount, Is.EqualTo(1));
            Assert.That(pickup.Collect(), Is.False);
            Assert.That(inventory.RemoveItemById(7), Is.EqualTo(1));
            Assert.That(pickup.Collect(), Is.True);
            Assert.That(pickup.Amount, Is.Zero);
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
        }

        [Test]
        public void Pickup_ReentrantConsumerDoesNotTurnAcceptedGrantIntoZeroReceipt()
        {
            InventoryComponent inventory = CreateInventory();
            PickableItem pickup = NewPickup(inventory);
            pickup.Configure(null, 7, 1, inventory);
            int consumed = 0;
            inventory.OnItemAdded.AddListener((id, amount) => consumed += inventory.RemoveItemByIdAmount(id, amount));

            Assert.That(pickup.Collect(), Is.True);

            Assert.That(inventory.GetCount(7), Is.Zero);
            Assert.That(pickup.Amount, Is.Zero);
            Assert.That(consumed, Is.EqualTo(1));
            Assert.That(pickup.Collect(), Is.False);
        }

        [Test]
        public void ReusablePickup_FailedCompletedGrantCannotReplay()
        {
            InventoryComponent inventory = CreateInventory();
            PickableItem pickup = NewPickup(inventory);
            Configure(pickup, serialized => serialized.FindProperty("_collectOnlyOnce").boolValue = false);
            pickup.Configure(null, 7, 1, inventory);
            ThrowOnce(inventory, "added", new InvalidOperationException("accepted"));

            Assert.Throws<InvalidOperationException>(() => pickup.Collect());

            Assert.That(pickup.Collect(), Is.False);
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
        }

        [TestCase(false, "physical", "removed")]
        [TestCase(false, "packed", "changed")]
        [TestCase(false, "id", "saved")]
        [TestCase(true, "physical", "removed")]
        [TestCase(true, "packed", "changed")]
        [TestCase(true, "id", "saved")]
        public void Drop_ExtractNotificationFaultRestoresExactOrigin(bool instance, string path, string observer)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance original = NewInstance();
            inventory.TrySetSlot(3, instance
                ? new InventorySlotState { ItemId = 7, Count = 1, Instance = original }
                : new InventorySlotState { ItemId = 7, Count = 1 });
            string before = GridJson(inventory);
            InventoryDropper dropper = NewDropper(inventory, CreateObject("DropPrefab"));
            EnableAutoSave(inventory);
            InvalidOperationException fault = new("extracted notification");
            ThrowOnce(inventory, observer, fault);
            int spawned = 0;
            dropper.OnItemDropped.AddListener((id, count, world) => spawned++);

            Assert.That(Assert.Throws<InvalidOperationException>(() => Drop(dropper, path)), Is.SameAs(fault));

            Assert.That(GridJson(inventory), Is.EqualTo(before));
            Assert.That(dropper.PendingReturnCount, Is.Zero);
            Assert.That(spawned, Is.Zero);
            InventorySaveData saved = InventorySaveData.FromJson(Provider.GetString(inventory.SaveKey));
            Assert.That(saved.Slots[3].EffectiveCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublicExtraction_AssignsOwnedRecordBeforeThrowingObserver(bool physical)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance original = NewInstance();
            inventory.TrySetSlot(3, new InventorySlotState { ItemId = 7, Count = 1, Instance = original });
            ThrowOnce(inventory, "removed", new InvalidOperationException("take observer"));
            InventoryItemRecord extracted = null;

            Assert.Throws<InvalidOperationException>(() =>
            {
                if (physical)
                {
                    inventory.TryTakeSlot(3, 1, out extracted);
                }
                else
                {
                    inventory.TryTakeRecordAtPackedIndex(0, 1, out extracted);
                }
            });

            Assert.That(inventory.TotalItemCount, Is.Zero);
            Assert.That(extracted.EffectiveCount, Is.EqualTo(1));
            Assert.That(extracted.Instance.InstanceId, Is.EqualTo(original.InstanceId));
            Assert.That(extracted.Instance.ComponentStates[0].Json, Is.EqualTo("payload"));
        }

        [TestCase("physical")]
        [TestCase("packed")]
        [TestCase("id")]
        [TestCase("first")]
        [TestCase("last")]
        public void RestoreFailure_AllRoutesPreserveSlotIdentityAndPayload(string path)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            GameObject prefab = FailingPrefab(out string stateKey);
            InventoryItemInstance original = NewInstance();
            original.ComponentStates.Add(new InventoryItemComponentState(stateKey, "{}"));
            inventory.TrySetSlot(3, new InventorySlotState { ItemId = 7, Count = 1, Instance = original });
            string before = GridJson(inventory);
            InventoryDropper dropper = NewDropper(inventory, prefab);
            int failures = 0;
            dropper.OnDropFailed.AddListener((id, amount) => failures++);

            ExpectRestoreFailure();
            Assert.That(Drop(dropper, path), Is.Zero);

            Assert.That(GridJson(inventory), Is.EqualTo(before));
            Assert.That(inventory.GetSlot(0).IsEmpty, Is.True);
            Assert.That(dropper.PendingReturnCount, Is.Zero);
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(Resources.FindObjectsOfTypeAll<RestoreFailureState>().Length, Is.EqualTo(1));
        }

        [Test]
        public void RecoveryObserverFaultCannotLoseOrReplayAlreadyReturnedInstance()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            GameObject prefab = FailingPrefab(out string key);
            InventoryItemInstance original = NewInstance();
            original.ComponentStates.Add(new InventoryItemComponentState(key, "{}"));
            inventory.TrySetSlot(3, new InventorySlotState { ItemId = 7, Count = 1, Instance = original });
            InventoryDropper dropper = NewDropper(inventory, prefab);
            ThrowOnce(inventory, "added", new InvalidOperationException("return observer"));

            ExpectRestoreFailure();
            Assert.Throws<InvalidOperationException>(() => dropper.DropSlot(3));

            Assert.That(inventory.GetSlot(3).Instance.InstanceId, Is.EqualTo(original.InstanceId));
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(dropper.PendingReturnCount, Is.Zero);
            Assert.That(dropper.RetryPendingReturns(), Is.Zero);
        }

        [TestCase("added")]
        [TestCase("saved")]
        public void FullOriginConflict_RetainsReceiptUntilCapacityReturns_EvenWhenRetryObserverFails(string observer)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance original = NewInstance();
            inventory.TrySetSlot(3, new InventorySlotState { ItemId = 7, Count = 1, Instance = original });
            InventoryDropper dropper = NewDropper(inventory, CreateObject("UnusedPrefab"));
            bool conflict = false;
            inventory.OnItemRemoved.AddListener((id, count) =>
            {
                if (!conflict)
                {
                    conflict = true;
                    for (int slot = 0; slot < inventory.SlotCapacity; slot++)
                    {
                        inventory.TrySetSlot(slot, new InventorySlotState { ItemId = 8, Count = 1 });
                    }

                    throw new InvalidOperationException("filled origin");
                }
            });

            Assert.Throws<InvalidOperationException>(() => dropper.DropSlot(3));
            Assert.That(inventory.GetCount(8), Is.EqualTo(4));
            Assert.That(inventory.GetCount(7) + dropper.PendingReturnCount, Is.EqualTo(1));
            Assert.That(dropper.GetPendingReturnRecords()[0].Instance.InstanceId, Is.EqualTo(original.InstanceId));
            Assert.That(dropper.DropById(8), Is.Zero, "New extraction waits for pending recovery.");
            inventory.RemoveItemById(8);
            EnableAutoSave(inventory);
            InvalidOperationException fault = new("retry observer");
            ThrowOnce(inventory, observer, fault);

            Assert.That(Assert.Throws<InvalidOperationException>(() => dropper.RetryPendingReturns()), Is.SameAs(fault));

            Assert.That(inventory.GetCount(8), Is.EqualTo(3));
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(dropper.PendingReturnCount, Is.Zero);
            Assert.That(dropper.RetryPendingReturns(), Is.Zero);
            Assert.That(inventory.GetSnapshotInstances()[0].InstanceId, Is.EqualTo(original.InstanceId));
        }

        [Test]
        public void PartialReturn_KeepsOnlyUnacceptedCountPending()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, maxTotal: 3);
            inventory.TrySetSlot(3, new InventorySlotState { ItemId = 7, Count = 3 });
            InventoryDropper dropper = NewDropper(inventory, CreateObject("UnusedPrefab"));
            bool conflict = false;
            inventory.OnItemRemoved.AddListener((id, count) =>
            {
                if (!conflict)
                {
                    conflict = true;
                    inventory.TrySetSlot(3, new InventorySlotState { ItemId = 8, Count = 2 });
                    throw new InvalidOperationException("partial capacity");
                }
            });

            Assert.Throws<InvalidOperationException>(() => dropper.DropSlot(3));
            Assert.That(inventory.GetSlot(3).ItemId, Is.EqualTo(8));
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(dropper.PendingReturnCount, Is.EqualTo(2));
            Assert.That(dropper.GetPendingReturnRecords()[0].Count, Is.EqualTo(2));

            inventory.RemoveItemByIdAmount(8, 2);
            Assert.That(dropper.RetryPendingReturns(), Is.EqualTo(2));
            Assert.That(inventory.GetCount(7), Is.EqualTo(3));
            Assert.That(dropper.PendingReturnCount, Is.Zero);
        }

        [Test]
        public void PartialInstanceExtraction_FaultMergesBackIntoOriginalInstanceSlot()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance original = NewInstance(3);
            inventory.TrySetSlot(3, new InventorySlotState { ItemId = 7, Count = 3, Instance = original });
            InventoryDropper dropper = NewDropper(inventory, CreateObject("UnusedPrefab"));
            string before = GridJson(inventory);
            ThrowOnce(inventory, "removed", new InvalidOperationException("partial instance"));

            Assert.Throws<InvalidOperationException>(() => dropper.DropSlot(3, 1));

            Assert.That(GridJson(inventory), Is.EqualTo(before));
            Assert.That(inventory.GetSlot(0).IsEmpty, Is.True);
            Assert.That(dropper.PendingReturnCount, Is.Zero);
        }

        [Test]
        public void DroppedInstance_RecollectsSameIdentityWhenPickupObserverFaults()
        {
            InventoryComponent inventory = CreateInventory();
            InventoryItemInstance original = NewInstance();
            inventory.AddItemInstance(original);
            InventoryDropper dropper = NewDropper(inventory, CreateObject("InstancePrefab"));
            PickableItem world = null;
            dropper.OnItemDropped.AddListener((id, amount, spawned) =>
            {
                world = spawned.GetComponent<PickableItem>();
                Configure(world, serialized =>
                {
                    serialized.FindProperty("_requireCollectorInventory").boolValue = false;
                    serialized.FindProperty("_destroyAfterCollect").boolValue = false;
                });
                world.SetTargetInventory(inventory);
            });
            Assert.That(dropper.DropById(7), Is.EqualTo(1));
            Assert.That(world.StoredInstance.InstanceId, Is.EqualTo(original.InstanceId));
            ThrowOnce(inventory, "changed", new InvalidOperationException("recollect"));

            Assert.Throws<InvalidOperationException>(() => world.Collect());

            Assert.That(world.Amount, Is.Zero);
            Assert.That(world.Collect(), Is.False);
            Assert.That(inventory.GetSnapshotInstances()[0].InstanceId, Is.EqualTo(original.InstanceId));
            Assert.That(inventory.GetSnapshotInstances()[0].ComponentStates[0].Json, Is.EqualTo("payload"));
        }

        [Test]
        public void ItemAuthoring_DoesNotSerializePresentationPreview_AndPreservesExplicitIcon()
        {
            Texture2D texture = new(2, 2);
            CreatedObjects.Add(texture);
            Sprite authored = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            CreatedObjects.Add(authored);
            GameObject prefab = CreateObject("IconPrefab");
            prefab.AddComponent<SpriteRenderer>().sprite = authored;
            InventoryItemData item = CreateItem(7);
            Configure(item, serialized => serialized.FindProperty("_worldDropPrefab").objectReferenceValue = prefab);
            MethodInfo validate = typeof(InventoryItemData).GetMethod("OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            validate.Invoke(item, null);

            Assert.That(item.Icon, Is.Null);
            Assert.That(prefab.GetPreviewSprite(), Is.SameAs(authored));
            PrefabPreviewExtensions.ClearPreviewSpriteCache();
            Assert.That(prefab.GetPreviewSprite(), Is.SameAs(authored));
            Assert.That(item.Icon, Is.Null);

            Configure(item, serialized => serialized.FindProperty("_icon").objectReferenceValue = authored);
            validate.Invoke(item, null);
            PrefabPreviewExtensions.ClearPreviewSpriteCache();
            Assert.That(item.Icon, Is.SameAs(authored));
            Assert.That(authored != null, Is.True);
        }

        private static InventoryItemInstance NewInstance(int count = 1)
        {
            InventoryItemInstance instance = new(7, count);
            instance.ComponentStates.Add(new InventoryItemComponentState("opaque", "payload"));
            return instance;
        }

        private PickableItem NewPickup(InventoryComponent inventory)
        {
            PickableItem pickup = CreateObject("ReceiptPickup").AddComponent<PickableItem>();
            Configure(pickup, serialized =>
            {
                serialized.FindProperty("_requireCollectorInventory").boolValue = false;
                serialized.FindProperty("_destroyAfterCollect").boolValue = false;
            });
            pickup.SetTargetInventory(inventory);
            return pickup;
        }

        private InventoryDropper NewDropper(InventoryComponent inventory, GameObject prefab)
        {
            InventoryDropper dropper = CreateObject("ReceiptDropper").AddComponent<InventoryDropper>();
            Configure(dropper, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_autoFindInventory").boolValue = false;
                serialized.FindProperty("_fallbackDropPrefab").objectReferenceValue = prefab;
                serialized.FindProperty("_addColliderIfMissing").boolValue = false;
                serialized.FindProperty("_addRigidbody3D").boolValue = false;
                serialized.FindProperty("_configurePickableItem").boolValue = true;
            });
            dropper.OnItemDropped.AddListener((id, amount, world) => CreatedObjects.Add(world));
            return dropper;
        }

        private GameObject FailingPrefab(out string stateKey)
        {
            GameObject prefab = CreateObject("FailingReceiptPrefab");
            stateKey = prefab.AddComponent<RestoreFailureState>().InventoryStateKey;
            return prefab;
        }

        private static void ExpectRestoreFailure()
        {
            LogAssert.Expect(LogType.Error, "[InventoryDropper] Failed to spawn dropped item 7: restore failed");
        }

        private static int Drop(InventoryDropper dropper, string path)
        {
            return path switch
            {
                "physical" => dropper.DropSlot(3),
                "packed" => dropper.DropPackedIndex(0),
                "first" => dropper.DropFirst(),
                "last" => dropper.DropLast(),
                _ => dropper.DropById(7)
            };
        }

        private static void EnableAutoSave(InventoryComponent inventory)
        {
            Configure(inventory, serialized => serialized.FindProperty("_autoSave").boolValue = true);
        }

        private static void ThrowOnce(InventoryComponent inventory, string observer, Exception fault,
            Action onNotification = null)
        {
            bool thrown = false;
            void Notify()
            {
                onNotification?.Invoke();
                if (!thrown)
                {
                    thrown = true;
                    throw fault;
                }
            }

            switch (observer)
            {
                case "added": inventory.OnItemAdded.AddListener((id, count) => Notify()); break;
                case "removed": inventory.OnItemRemoved.AddListener((id, count) => Notify()); break;
                case "saved": inventory.OnSaved.AddListener(Notify); break;
                default: inventory.OnInventoryChanged.AddListener(Notify); break;
            }
        }

        private static string GridJson(InventoryComponent inventory)
        {
            List<InventorySlotState> slots = new();
            for (int i = 0; i < inventory.SlotCapacity; i++)
            {
                slots.Add(inventory.GetSlot(i));
            }

            return new InventorySaveData { Slots = slots }.ToJson();
        }
    }
}
