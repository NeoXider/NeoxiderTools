# InventoryComponent

**What it is:** Scene `MonoBehaviour` inventory facade: `Aggregated` vs `Slot Grid` storage, add/remove/`TryConsume`, one SaveProvider blob per container (`Save Key`), UnityEvents, optional `InventoryDropper`. Path: `Scripts/Tools/Inventory/Runtime/InventoryComponent.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add the component and assign `Inventory Database` (optional `Restrict To Database`).
2. Set `Storage Mode` and, for slot grid, `Slot Count`.
3. Give each container a unique `Save Key`.
4. Configure `Load Mode` and initial state assets as needed.
5. Wire UI to events; assign `Dropper` for world drops.
6. For slot UI, add `InventorySlotGridView` (slot grid mode only).

---

## Features

- Multiple `InventoryComponent` instances = multiple inventories (hotbar, backpack, chests).
- Instance-based items: enable `Supports Instance State` on `InventoryItemData`; state lives inside the same save JSON as the container.
- Full field/event/API tables: see the Russian doc [InventoryComponent.md](../../../Docs/Tools/Inventory/InventoryComponent.md) (canonical, `[NeoDoc]` target).

## Mechanics walkthroughs (Inspector-focused)

Step-by-step scenarios (aggregated currency, Minecraft-style grids + click transfer, RE-style instance state) are written in the Russian page **«Примеры механик»** in [InventoryComponent.md](../../../Docs/Tools/Inventory/InventoryComponent.md).

## See also

- [Inventory README](../../../Docs/Tools/Inventory/README.md)
- [InventorySlotGridView](../../../Docs/Tools/Inventory/InventorySlotGridView.md)
- [InventoryItemState](../../../Docs/Tools/Inventory/InventoryItemState.md)

← [Tools / Inventory](README.md)
