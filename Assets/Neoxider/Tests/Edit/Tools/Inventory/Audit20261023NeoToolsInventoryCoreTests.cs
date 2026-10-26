using Neo.Tools;
using NUnit.Framework;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023NeoToolsInventoryCoreTests
    {
        [Test]
        public void Aggregated_AddStackForExistingInstanceType_DoesNotUseAnotherUniqueItem()
        {
            AggregatedInventory inventory = new();
            inventory.SetConstraints(new InventoryConstraints { MaxUniqueItems = 1 });
            Assert.That(inventory.AddInstance(new InventoryItemInstance(7)), Is.EqualTo(1));

            Assert.That(inventory.Add(7, 2), Is.EqualTo(2));
            Assert.That(inventory.GetCount(7), Is.EqualTo(3));
            Assert.That(inventory.UniqueCount, Is.EqualTo(1));
            Assert.That(inventory.Add(8), Is.Zero);
        }

        [TestCase(0, 2)]
        [TestCase(1, 1)]
        [TestCase(int.MaxValue, 2)]
        public void MoveSlot_ClampsRequestToSource_AndConservesItems(int requested, int expectedMoved)
        {
            SlotGridInventory inventory = new(2);
            inventory.SetSlot(0, new InventorySlotState { ItemId = 7, Count = 2 });
            inventory.SetSlot(1, new InventorySlotState { ItemId = 7, Count = 3 });
            InventoryConstraints constraints = new();
            constraints.SetItemMaxStack(7, 10);
            inventory.SetConstraints(constraints);

            Assert.That(inventory.MoveSlot(0, 1, requested), Is.EqualTo(expectedMoved));
            Assert.That(inventory.GetSlot(0).EffectiveCount, Is.EqualTo(2 - expectedMoved));
            Assert.That(inventory.GetSlot(1).Count, Is.EqualTo(3 + expectedMoved));
            Assert.That(inventory.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public void MoveToEmptySlot_AfterStackLimitChange_LeavesRemainderInSource()
        {
            SlotGridInventory inventory = new(2);
            inventory.Add(7, 5);
            InventoryConstraints constraints = new();
            constraints.SetItemMaxStack(7, 2);
            inventory.SetConstraints(constraints);

            Assert.That(inventory.MoveSlot(0, 1), Is.EqualTo(2));
            Assert.That(inventory.GetSlot(0).Count, Is.EqualTo(3));
            Assert.That(inventory.GetSlot(1).Count, Is.EqualTo(2));
            Assert.That(inventory.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public void InsertIntoEmptySlot_EnforcesStackCap()
        {
            SlotGridInventory inventory = new(2);
            InventoryConstraints constraints = new();
            constraints.SetItemMaxStack(7, 2);
            inventory.SetConstraints(constraints);

            Assert.That(inventory.TryInsertIntoSlot(0, new InventoryItemRecord(7, 5)), Is.EqualTo(2));
            Assert.That(inventory.GetSlot(0).Count, Is.EqualTo(2));
            Assert.That(inventory.TotalCount, Is.EqualTo(2));
        }

        [Test]
        public void InsertIntoSlot_EnforcesUniqueAndTotalCaps()
        {
            SlotGridInventory inventory = new(3);
            inventory.SetConstraints(new InventoryConstraints { MaxUniqueItems = 1, MaxTotalItems = 3 });
            inventory.Add(7, 2);

            Assert.That(inventory.TryInsertIntoSlot(1, new InventoryItemRecord(8, 1)), Is.Zero);
            Assert.That(inventory.TryInsertIntoSlot(1, new InventoryItemRecord(7, 5)), Is.EqualTo(1));
            Assert.That(inventory.TryInsertIntoSlot(2, new InventoryItemRecord(7, 1)), Is.Zero);
            Assert.That(inventory.TotalCount, Is.EqualTo(3));
            Assert.That(inventory.UniqueCount, Is.EqualTo(1));
            Assert.That(inventory.GetSlot(1).Count, Is.EqualTo(1));
        }

        [Test]
        public void InsertInstance_WhenWholeRequestedCountDoesNotFit_DoesNotPartiallyInsert()
        {
            SlotGridInventory inventory = new(2);
            inventory.SetConstraints(new InventoryConstraints { MaxTotalItems = 3 });
            inventory.Add(7, 2);

            Assert.That(inventory.TryInsertIntoSlot(1, new InventoryItemRecord(new InventoryItemInstance(8, 2))),
                Is.Zero);
            Assert.That(inventory.GetSlot(1).IsEmpty, Is.True);
            Assert.That(inventory.TotalCount, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AddInstance_NegativeItemId_IsRejected(bool slotted)
        {
            IInventoryStorage inventory = CreateStorage(slotted);

            Assert.That(inventory.AddInstance(new InventoryItemInstance(-1)), Is.Zero);
            Assert.That(inventory.TotalCount, Is.Zero);
            Assert.That(inventory.CreateInstanceSnapshot(), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnlimitedStorage_ClampsAddsToRepresentableTotal_AndRecoversAfterRemoval(bool slotted)
        {
            IInventoryStorage inventory = CreateStorage(slotted);
            inventory.Add(7, int.MaxValue - 1);

            Assert.That(inventory.Add(8, 10), Is.EqualTo(1));
            Assert.That(inventory.TotalCount, Is.EqualTo(int.MaxValue));
            Assert.That(inventory.GetCount(8), Is.EqualTo(1));
            Assert.That(inventory.Add(9), Is.Zero);
            Assert.That(inventory.Remove(7), Is.EqualTo(1));
            Assert.That(inventory.Add(9, 2), Is.EqualTo(1));
            Assert.That(inventory.TotalCount, Is.EqualTo(int.MaxValue));
        }

        [TestCase(false, 0)]
        [TestCase(true, 0)]
        [TestCase(false, int.MaxValue)]
        [TestCase(true, int.MaxValue)]
        public void AddInstance_TotalOverflow_IsRejectedWithoutMutation(bool slotted, int maxTotal)
        {
            IInventoryStorage inventory = CreateStorage(slotted);
            inventory.SetConstraints(new InventoryConstraints { MaxTotalItems = maxTotal });
            inventory.Add(7, int.MaxValue - 1);

            Assert.That(inventory.AddInstance(new InventoryItemInstance(8, 2)), Is.Zero);
            Assert.That(inventory.TotalCount, Is.EqualTo(int.MaxValue - 1));
            Assert.That(inventory.GetCount(8), Is.Zero);
            Assert.That(inventory.CreateInstanceSnapshot(), Is.Empty);
        }

        [Test]
        public void LegacyManager_UnlimitedAdds_DoNotOverflowCounts()
        {
            InventoryManager inventory = new();
            inventory.Add(7, int.MaxValue - 1);

            Assert.That(inventory.Add(8, 10), Is.EqualTo(1));
            Assert.That(inventory.Add(7, 10), Is.Zero);
            Assert.That(inventory.TotalCount, Is.EqualTo(int.MaxValue));
            Assert.That(inventory.GetCount(7), Is.EqualTo(int.MaxValue - 1));
            Assert.That(inventory.GetCount(8), Is.EqualTo(1));
        }

        [Test]
        public void SlotRules_RejectNegativeIds_AndAliasedMoves()
        {
            InventorySlotState target = InventorySlotState.Empty();
            Assert.That(InventorySlotTransferRules.TryInsert(ref target, new InventoryItemRecord(-1, 3), 10),
                Is.Zero);
            Assert.That(target.IsEmpty, Is.True);

            InventorySlotState source = new() { ItemId = 7, Count = 3 };
            target = source;
            Assert.That(InventorySlotTransferRules.TryMoveOrSwap(ref source, ref target, 10), Is.Zero);
            Assert.That(source.Count, Is.EqualTo(3));
        }

        private static IInventoryStorage CreateStorage(bool slotted)
        {
            return slotted ? new SlotGridInventory(3) : new AggregatedInventory();
        }
    }
}
