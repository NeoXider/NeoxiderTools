# InventoryItemStateBehaviour

**What it is:** abstract base for a world/dropped-item component that carries extra per-instance state (ammo, durability, enchantments) into and out of the inventory. Implements `IInventoryItemState`. Path: `Scripts/Tools/Inventory/Runtime/InventoryItemStateBehaviour.cs`, namespace `Neo.Tools`.

**How to use:**
1. Inherit from `InventoryItemStateBehaviour` on your item prefab.
2. Override `CaptureInventoryState()` to serialize your custom fields to a string (e.g. JSON) — called on pickup.
3. Override `RestoreInventoryState(string json)` to apply that state back — called when the item is spawned/dropped again.
4. The inventory system doesn't know your fields; `InventoryItemStateUtility` just collects every `InventoryItemStateBehaviour` on the object and calls capture/restore for you.

---

## Fields

| Field | Description |
|-------|-------------|
| **Inventory State Key** | Optional override key used inside the inventory instance payload; defaults to the concrete type's full name. |

## API

| Member | Description |
|--------|-------------|
| `InventoryStateKey` | The key this behaviour's captured state is stored under. |
| `CaptureInventoryState()` (abstract) | Return a string (typically JSON) describing this instance's extra state. |
| `RestoreInventoryState(string json)` (abstract) | Apply a previously captured state string back onto this instance. |

## See also

- [InventoryDatabase](./Data/InventoryDatabase.md)
- [Inventory README](./README.md)
