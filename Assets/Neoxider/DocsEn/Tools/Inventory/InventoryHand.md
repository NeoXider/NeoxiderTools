# InventoryHand

**Purpose:** Displays one selected inventory item on an anchor Transform (e.g., a hand bone). Supports slot switching (via `Selector` or code), item dropping (via `InventoryDropper`), item use, and synchronization with physical slot indices.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Target inventory. Auto-finds if empty. |
| **Hand Anchor** | Transform where the spawned item is parented (e.g., a hand bone). |
| **Selector** | Optional `Selector` — for cycling slots (scroll wheel, arrows, etc.). |
| **Dropper** | Optional `InventoryDropper` — for dropping items from the hand. |
| **Fallback Hand Prefab** | Default prefab if the item has no `WorldDropPrefab`. |
| **Scale In Hand Mode** | `Fixed` (multiply by fixed value) or `Relative` (1 + offset on top of `HandView.ScaleInHand`). |
| **Disable Colliders In Hand** | Disable all colliders on the spawned item in hand. |
| **Use Physical Slot Indices** | Use physical slot indices (including empty ones) instead of packed. |
| **Drop Key** | Key to drop the equipped item. |
| **Use Key** | Key to use the equipped item (calls `UseEquippedItem()`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SelectNext()` / `void SelectPrevious()` | Cycle to the next/previous slot (wraps around). |
| `void SetSlotIndex(int index)` | Set a specific slot. `-1` = empty hand. |
| `void UseEquippedItem()` | Use the item: fires `OnUseItemRequested` and `PickableItem.Activate()`. |
| `int DropEquipped(int amount = 1)` | Drop the item via the linked `InventoryDropper`. |
| `int EquippedItemId { get; }` | The item ID currently in hand (or -1). |
| `int SlotIndex { get; }` | The current slot index. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnEquippedChanged` | `int itemId` | The equipped item changed. `-1` = empty hand. |
| `OnUseItemRequested` | `int itemId` | The player pressed the use key. |

## Examples

### No-Code Example (Inspector)
Create an empty Transform on the character's hand bone. Attach `InventoryHand`. Drag the hand Transform into `Hand Anchor`. Add a `Selector` to cycle slots via scroll wheel. On play, the first item in the inventory will appear in the hand.

### Code Example
```csharp
[SerializeField] private InventoryHand _hand;

public void EquipSlot(int slotIndex)
{
    _hand.SetSlotIndex(slotIndex);
}

public void UseItem()
{
    _hand.UseEquippedItem();
}
```

## See Also
- [HandView](HandView.md)
- [InventoryDropper](InventoryDropper.md)
- [Selector](../View/Selector.md)
- ← [Tools/Inventory](README.md)
