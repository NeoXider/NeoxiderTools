# Inventory item state (IInventoryItemState)

**What it is:** Capture/restore JSON for unique per-instance item payload (`InventoryItemInstance.ComponentStates`), used with `InventoryItemData.Supports Instance State`. Files: `IInventoryItemState.cs`, `InventoryItemStateBehaviour.cs`, `InventoryItemStateUtility.cs`.

**How to use:** Enable **Supports Instance State** on item data; implement capture/restore on the world prefab; pickup/drop pipelines call the utility to pack/unpack state into the container save blob.

Full walkthrough (weapon ammo example): [InventoryItemState.md](../../../Docs/Tools/Inventory/InventoryItemState.md) (Russian).

← [Tools / Inventory](README.md)
