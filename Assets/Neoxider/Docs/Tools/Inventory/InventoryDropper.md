# InventoryDropper

**Purpose:** A component for dropping items from the inventory into the game world. Spawns the item's WorldDropPrefab with physics, colliders, and auto-configured `PickableItem` for re-pickup. Supports keyboard input, throw impulse, and random spawn offset.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Item source. Auto-finds if empty. |
| **Drop Point** | Spawn position Transform. Defaults to this object. |
| **Drop Key** | Key for dropping (default `G`). |
| **Drop Selected On Key** | Drop the item selected in `InventoryComponent.SelectedItemId`. |
| **Fallback Drop Prefab** | Default prefab if the item has no `WorldDropPrefab`. |
| **Throw Direction** | Throw direction (local space). |
| **Throw Impulse** | Force applied on throw. |
| **Random Radius** | Random offset radius around the drop point. |
| **Add Rigidbody 3D / 2D** | Automatically add a `Rigidbody` to the spawned item. |
| **Configure Pickable Item** | Automatically configure `PickableItem` on the dropped object. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `int DropSelected(int amount = 1)` | Drop the currently selected item. |
| `int DropById(int itemId, int amount = 1)` | Drop an item by its ID. |
| `int DropSlot(int slotIndex, int amount = 0)` | Drop from a specific physical slot; zero takes the whole slot. |
| `int DropPackedIndex(int packedIndex, int amount = 1)` | Drop from a non-empty record index; empty physical slots are skipped. |
| `int DropFirst(int amount = 1)` | Drop the first available item. |
| `int DropLast(int amount = 1)` | Drop the last available item. |
| `void SetDropEnabled(bool enabled)` | Enable/disable dropping capability. |
| `long PendingReturnCount { get; }` | Quantity still owned by this dropper after a conflicted return. |
| `List<InventoryItemRecord> GetPendingReturnRecords()` | Deep copies of pending records; inspecting them does not transfer ownership. |
| `long RetryPendingReturns()` | Retry recovery to each original inventory; return actual units restored. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnItemDropped` | `int itemId, int amount, GameObject dropped` | Item successfully dropped into the world. |
| `OnDropFailed` | `int itemId, int amount` | Drop attempt failed. |

## Mutation behavior

`CanDrop` / `SetDropEnabled(false)` gates every entry point, including physical/packed index drops,
first/last drops, and calls made by `InventoryHand`. A disabled dropper leaves inventory contents intact.
`DropSlot` accepts amount `0` for the whole slot and rejects negative amounts. Other drop methods require
a positive amount. Each operation takes from one selected record; the return value is the actual count.

Missing prefabs are checked before extracting a record, so a failed request does not rearrange a grid
or publish temporary inventory events. Other spawn/configuration failures release the partially created
object and return the extracted record to its original physical slot first, including requests made by
packed index, item id, first/last, and selected-item helpers. Instance id, state version, and payloads
are retained. A compatible remaining part of the same instance can be merged back into that origin.

The extraction receipt is captured before inventory observers run. If an extraction observer or its
Auto Save fails, spawning is skipped and the exact extracted quantity is recovered before the first
fault is rethrown. Recovery updates the pending quantity before publishing its own inventory events;
a throwing recovery observer cannot replay an already returned quantity.

If a callback occupies the original slot, recovery never overwrites its new contents. It tries ordinary
compatible capacity elsewhere and retains only the unaccepted remainder in this dropper. New drops are
blocked while any return is pending. Free capacity and call `RetryPendingReturns()`; inspect
`PendingReturnCount` even when that retry throws, because a return may already be committed. Retry is
allowed while `CanDrop` is false and never extracts another item.

Pending records belong to the component. Keep the dropper and originating inventory alive and drain
pending returns before destroying/unloading them. Inventory JSON saves do not persist this recovery
list. Snapshot copies are for inspection, not permission to grant the same records manually.

`OnItemDropped` runs after successful spawning; an exception in a success listener propagates and does
not re-add the item already in the world. Recursive drop/recovery requests on this dropper are blocked
while it is settling an operation. A normal spawn/configuration failure logs the failure and returns
zero after recovery; observer exceptions propagate after ownership has been settled.

В 11.0 возврат сначала использует исходный физический слот. При конфликте остаток остаётся у dropper,
не теряется и не перезаписывает занятые слоты; освободите место и вызовите `RetryPendingReturns()`.

## Examples

### No-Code Example (Inspector)
Attach `InventoryDropper` to the player. Create an empty child in front of the player as the `Drop Point`. Enable `Drop Selected On Key = true`, set `Drop Key = G`. Now pressing `G` throws the selected item forward, and it can be picked up again.

### Code Example
```csharp
[SerializeField] private InventoryDropper _dropper;

public void DropCurrentWeapon()
{
    int dropped = _dropper.DropSelected();
    Debug.Log($"Dropped: {dropped} items.");
}
```

## See Also
- [InventoryHand](InventoryHand.md)
- [PickableItem](PickableItem.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](README.md)
