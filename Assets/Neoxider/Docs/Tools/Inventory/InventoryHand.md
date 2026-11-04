# InventoryHand

**What it is:** `MonoBehaviour` that shows one selected inventory item on an anchor Transform (e.g., a hand bone), with slot switching (via `Selector` or code), dropping (via `InventoryDropper`), use and physical slot indices. `Scripts/Tools/Inventory/Runtime/InventoryHand.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add `InventoryHand`, assign **Inventory** and **Hand Anchor**, and optionally a **Selector** and **Dropper**.
2. Give item data a **World Drop Prefab** (or set **Fallback Hand Prefab**); add [HandView](HandView.md) to tune its pose.
3. Switch with `SetSlotIndex`/`SelectNext`, use with `UseEquippedItem`, and handle `OnUseSelectionRequested`.

---

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
| `void UseEquippedItem()` | Captures selection, fires legacy/type and exact-selection events; activates the original visual only if still equipped/present. |
| `bool TryGetEquippedSelection(out InventorySelection selection)` | Capture the selected stack/UID with storage ownership and index semantics. |
| `int DropEquipped(int amount = 1)` | Drop the item via the linked `InventoryDropper`. |
| `int EquippedItemId { get; }` | The item ID currently in hand (or -1). Reads the live slot without cloning, so it is cheap to poll every frame. |
| `int SlotIndex { get; }` | The current slot index. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnEquippedChanged` | `int itemId` | The equipped item changed. `-1` = empty hand. |
| `OnUseItemRequested` | `int itemId` | The player pressed the use key. |
| `OnUseSelectionRequested` | `InventorySelection` | The exact item captured before callbacks; skipped if the legacy callback removed/replaced it. |

The hand owns its spawned visual and releases it when disabled, destroyed, or unequipped, even when
Hand Anchor belongs to another object. The configured anchor and source prefab are never destroyed.
The linked dropper's `CanDrop` flag also applies to `DropEquipped` through physical or packed indices.
Use [InventoryUseAction](InventoryUseAction.md) or `Inventory.TryConsumeSelection(selection)` to consume
the selected instance. A type-only `TryConsume(itemId, amount)` can select another same-type record.
The legacy event runs first for compatibility. Recursive use during the same request is ignored, and
callbacks that switch slots never activate the newly selected visual as part of the old request.

The hand re-syncs on every inventory change but keeps its spawned visual while the slot, item id, prefab,
anchor and (for instances) the stored payload are unchanged; picking up unrelated items or growing the
equipped stack no longer re-instantiates it, and runtime state on the visual survives. A changed payload,
another slot or another item rebuilds and restores the visual. A use whose callbacks refreshed the hand
(for example by consuming a unit) still does not activate the visual, as before.

A linked `Selector` gets its `Count` on the first sync after the hand is enabled (switching it to an explicit
count, as before) and afterwards only when the selectable slot count changes; `Set` runs only when the index
changes. **Behaviour change:** an inventory change that leaves the selected slot index unchanged no longer
re-publishes `Selector.OnSelectionChanged`/`OnSelectionChangedGameObject`, re-saves the selector or replicates
it. To react when a different item lands in the selected slot, subscribe to
`InventoryComponent.OnSelectedItemChanged` (raised when the hand's selected item id changes) or to
`OnInventoryChanged`.

**По-русски:** для уникальных вещей подписывайтесь на `OnUseSelectionRequested`, где есть UID и слот.
Выбор фиксируется до событий; смена слота в обработчике не активирует предмет из нового слота.
Связанный `Selector` больше не публикует `OnSelectionChanged` повторно, если изменение инвентаря не меняет
индекс слота; чтобы узнать о другом предмете в выбранном слоте, подпишитесь на
`InventoryComponent.OnSelectedItemChanged`.

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
