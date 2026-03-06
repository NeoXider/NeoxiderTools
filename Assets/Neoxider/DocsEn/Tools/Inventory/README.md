# Tools / Inventory

The inventory module covers item storage, pickup, drop, hand/equip visuals, and basic UI binding.

## Main pieces

- `InventoryComponent` manages entries, counts, save/load integration, and inventory events.
- `PickableItem` turns a world object into a pickup source.
- `InventoryDropper` spawns dropped world items from inventory entries.
- `InventoryHand` visualizes the currently selected item in hand.
- `InventoryView` and related UI helpers bind inventory data to UI.

## Quick start

1. Add `InventoryComponent` to the target object.
2. Assign an `InventoryDatabase` if you want item metadata and validation.
3. Add `PickableItem` to world drops and point it to an inventory, or use collector inventory resolution.
4. Add `InventoryDropper` if items should return to the world.
5. Bind counts and views with `InventoryView`, `InventoryItemCountText`, or `InventoryTotalCountText`.

## Russian docs (per-component)

| Page | Description |
|------|-------------|
| [Inventory README](../../../Docs/Tools/Inventory/README.md) | Overview |
| [InventoryComponent](../../../Docs/Tools/Inventory/InventoryComponent.md), [PickableItem](../../../Docs/Tools/Inventory/PickableItem.md), [InventoryDropper](../../../Docs/Tools/Inventory/InventoryDropper.md) | Core runtime |
| [InventoryView](../../../Docs/Tools/Inventory/InventoryView.md), [InventoryHand](../../../Docs/Tools/Inventory/InventoryHand.md), [HandView](../../../Docs/Tools/Inventory/HandView.md) | UI and hand view |
| [InventoryItemCountText](../../../Docs/Tools/Inventory/InventoryItemCountText.md), [InventoryTotalCountText](../../../Docs/Tools/Inventory/InventoryTotalCountText.md) | Count display |

## See also

- [Save](../../Save/README.md)
- [Shop](../../Shop/README.md)
