using System;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public sealed class InventorySelectionTests : Audit20261023NeoToolsInventoryFixture
    {
        [TestCase(InventoryStorageMode.Aggregated)]
        [TestCase(InventoryStorageMode.SlotGrid)]
        public void ConsumeSelection_RemovesSelectedUidAndKeepsOtherInstance(InventoryStorageMode mode)
        {
            InventoryComponent inventory = CreateInventory(mode);
            InventoryItemInstance first = new(7);
            InventoryItemInstance selected = new(7);
            selected.ComponentStates.Add(new InventoryItemComponentState("collectible", "{\"level\":3,\"mutation\":\"golden\"}"));
            inventory.AddItemInstance(first);
            inventory.AddItemInstance(selected);
            Assert.That(inventory.TryCaptureSelection(1, out InventorySelection selection), Is.True);

            Assert.That(inventory.TryTakeSelection(selection, 1, out InventoryItemRecord taken), Is.True);

            Assert.That(taken.Instance.InstanceId, Is.EqualTo(selected.InstanceId));
            Assert.That(taken.Instance.ComponentStates[0].Json, Is.EqualTo(selected.ComponentStates[0].Json));
            Assert.That(inventory.GetRecordAtSlotIndex(0).Instance.InstanceId, Is.EqualTo(first.InstanceId));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
            Assert.That(inventory.TryConsumeSelection(selection), Is.False);
        }

        [Test]
        public void ReplacedOrMovedInstance_RejectsOldSelectionEvenWithSameItemId()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.AddItemInstance(new InventoryItemInstance(7));
            inventory.AddItemInstance(new InventoryItemInstance(7));
            inventory.TryCaptureSelection(0, out InventorySelection selection);
            inventory.SwapSlots(0, 1);

            Assert.That(inventory.TryConsumeSelection(selection), Is.False);
            Assert.That(inventory.TotalItemCount, Is.EqualTo(2));
        }

        [Test]
        public void ForeignInventory_RejectsSelectionEvenWhenUidAndSlotMatch()
        {
            InventoryComponent first = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryComponent second = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance instance = new(7);
            first.AddItemInstance(instance);
            second.AddItemInstance(instance.Clone());
            first.TryCaptureSelection(0, out InventorySelection selection);

            Assert.That(second.TryConsumeSelection(selection), Is.False);
            Assert.That(first.TotalItemCount, Is.EqualTo(1));
            Assert.That(second.TotalItemCount, Is.EqualTo(1));
        }

        [Test]
        public void PackedIndexShift_DoesNotConsumeReplacementRecord()
        {
            InventoryComponent inventory = CreateInventory();
            inventory.AddItemInstance(new InventoryItemInstance(7));
            inventory.AddItemInstance(new InventoryItemInstance(7));
            inventory.TryCaptureSelection(0, out InventorySelection selection);
            inventory.TryTakeRecordAtPackedIndex(0, 1, out InventoryItemRecord removed);

            Assert.That(inventory.TryConsumeSelection(selection), Is.False);
            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(3)]
        public void InvalidOrInsufficientAmount_DoesNotPartiallyConsumeOrNotify(int amount)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.AddItemByIdAmount(7, 2);
            inventory.TryCaptureSelection(0, out InventorySelection selection);
            int changes = 0;
            inventory.OnInventoryChanged.AddListener(() => changes++);

            Assert.That(inventory.TryConsumeSelection(selection, amount), Is.False);
            Assert.That(inventory.GetCount(7), Is.EqualTo(2));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void DetachedSnapshot_CannotChangeSelectionIdentityOrPayload()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance instance = new(7);
            instance.ComponentStates.Add(new InventoryItemComponentState("level", "3"));
            inventory.AddItemInstance(instance);
            inventory.TryCaptureSelection(0, out InventorySelection selection);
            InventoryItemRecord snapshot = selection.Record;
            snapshot.Instance.InstanceId = "foreign";
            snapshot.Instance.ComponentStates[0].Json = "999";

            Assert.That(selection.InstanceId, Is.EqualTo(instance.InstanceId));
            Assert.That(selection.Record.Instance.ComponentStates[0].Json, Is.EqualTo("3"));
            Assert.That(inventory.IsSelectionCurrent(selection), Is.True);
        }

        [Test]
        public void TransferBetweenCatchHotbarAndPodium_PreservesUidAndState()
        {
            InventoryComponent catchBag = CreateInventory(InventoryStorageMode.SlotGrid, slots: 2);
            InventoryComponent hotbar = CreateInventory(InventoryStorageMode.SlotGrid, slots: 9);
            InventoryComponent podium = CreateInventory(InventoryStorageMode.SlotGrid, slots: 1);
            InventoryItemInstance instance = new(7);
            instance.ComponentStates.Add(new InventoryItemComponentState("collectible", "{\"level\":3,\"mutation\":\"diamond\"}"));
            catchBag.AddItemInstance(instance);
            catchBag.TryCaptureSelection(0, out InventorySelection oldSelection);

            Assert.That(InventoryTransferService.TransferSelection(catchBag, oldSelection, hotbar, 8), Is.EqualTo(1));
            Assert.That(catchBag.IsSelectionCurrent(oldSelection), Is.False);
            Assert.That(InventoryTransferService.TransferSelection(catchBag, oldSelection, podium, 0), Is.Zero);
            Assert.That(hotbar.TryCaptureSelection(8, out InventorySelection hotbarSelection), Is.True);
            Assert.That(InventoryTransferService.TransferSelection(hotbar, hotbarSelection, podium, 0), Is.EqualTo(1));
            Assert.That(podium.GetSlot(0).Instance.InstanceId, Is.EqualTo(instance.InstanceId));
            Assert.That(podium.GetSlot(0).Instance.ComponentStates[0].Json,
                Is.EqualTo(instance.ComponentStates[0].Json));
            Assert.That(catchBag.TotalItemCount + hotbar.TotalItemCount + podium.TotalItemCount, Is.EqualTo(1));
        }

        [Test]
        public void PureBackend_SelectionTakesExactUidAndCannotBeForgedThroughItsSnapshot()
        {
            SlotGridInventory storage = new(9);
            InventoryItemInstance instance = new(7);
            storage.AddInstance(instance);
            Assert.That(InventorySelection.TryCapture(storage, 0, true, out InventorySelection selection), Is.True);
            selection.Record.Instance.InstanceId = "changed copy";
            Assert.That(selection.TryTake(new SlotGridInventory(9), 1, out InventoryItemRecord foreign), Is.False);
            Assert.That(selection.TryTake(storage, 1, out InventoryItemRecord taken), Is.True);
            Assert.That(taken.Instance.InstanceId, Is.EqualTo(instance.InstanceId));
            Assert.That(selection.IsCurrent(storage), Is.False);
            Assert.That(storage.TotalCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UseAction_CommitsExactInstanceAndPublishesEvenWhenListenerThrows(bool effectThrows)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance first = new(7);
            InventoryItemInstance second = new(7);
            inventory.AddItemInstance(first);
            inventory.AddItemInstance(second);
            inventory.TryCaptureSelection(1, out InventorySelection selection);
            InventoryUseAction action = CreateObject("UseAction").AddComponent<InventoryUseAction>();
            int effects = 0;
            int notifications = 0;
            action.OnUsed.AddListener(record =>
            {
                effects++;
                Assert.That(record.Instance.InstanceId, Is.EqualTo(second.InstanceId));
                Assert.That(inventory.GetSlot(1).IsEmpty, Is.True);
                if (effectThrows) throw new InvalidOperationException("effect");
            });
            inventory.OnInventoryChanged.AddListener(() =>
            {
                notifications++;
                if (!effectThrows) throw new InvalidOperationException("observer");
            });

            Assert.Throws<InvalidOperationException>(() => action.TryUse(inventory, selection));
            Assert.That(effects, Is.EqualTo(1));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(inventory.GetSlot(0).Instance.InstanceId, Is.EqualTo(first.InstanceId));
            Assert.That(inventory.GetSlot(1).IsEmpty, Is.True);
            Assert.That(action.TryUse(inventory, selection), Is.False);
            Assert.That(effects, Is.EqualTo(1));
        }

        [Test]
        public void ReusableAction_ReadsCurrentPayloadAndDoesNotConsume()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryItemInstance instance = new(7);
            instance.ComponentStates.Add(new InventoryItemComponentState("level", "1"));
            inventory.AddItemInstance(instance);
            inventory.TryCaptureSelection(0, out InventorySelection selection);
            InventorySlotState current = inventory.GetSlot(0);
            current.Instance.ComponentStates[0].Json = "3";
            inventory.TrySetSlot(0, current);
            InventoryUseAction action = CreateObject("UseAction").AddComponent<InventoryUseAction>();
            Configure(action, serialized => serialized.FindProperty("_consumeAmount").intValue = 0);
            string level = null;
            action.OnUsed.AddListener(record => level = record.Instance.ComponentStates[0].Json);

            Assert.That(action.TryUse(inventory, selection), Is.True);
            Assert.That(level, Is.EqualTo("3"));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Hand_CallbackSelectingAnotherSlot_DoesNotActivateReplacementVisual(bool typedEvent)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.AddItemById(7);
            inventory.AddItemById(8);
            GameObject prefab = CreateObject("HandPrefab");
            prefab.AddComponent<PickableItem>();
            InventoryHand hand = CreateObject("Hand").AddComponent<InventoryHand>();
            Configure(hand, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_handAnchor").objectReferenceValue = hand.transform;
                serialized.FindProperty("_fallbackHandPrefab").objectReferenceValue = prefab;
            });
            hand.SetSlotIndex(0);
            int activations = 0;
            void SwitchSlot()
            {
                hand.SetSlotIndex(1);
                hand.GetComponentInChildren<PickableItem>(true).OnActivate.AddListener(() => activations++);
            }
            if (typedEvent) hand.OnUseSelectionRequested.AddListener(selection => SwitchSlot());
            else hand.OnUseItemRequested.AddListener(itemId => SwitchSlot());

            hand.UseEquippedItem();

            Assert.That(hand.SlotIndex, Is.EqualTo(1));
            Assert.That(activations, Is.Zero);
        }

        [Test]
        public void Hand_RecursiveUseListener_IsInvokedOnceAndGuardResets()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid);
            inventory.AddItemById(7);
            InventoryHand hand = CreateObject("Hand").AddComponent<InventoryHand>();
            Configure(hand, serialized => serialized.FindProperty("_inventory").objectReferenceValue = inventory);
            int uses = 0;
            hand.OnUseSelectionRequested.AddListener(selection => { uses++; hand.UseEquippedItem(); });

            hand.UseEquippedItem();
            hand.UseEquippedItem();

            Assert.That(uses, Is.EqualTo(2));
        }
    }
}
