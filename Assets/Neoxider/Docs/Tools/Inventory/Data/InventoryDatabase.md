# InventoryDatabase

**What it is:** a `ScriptableObject` catalog of `InventoryItemData` entries — validates item ids and resolves item data by id. Path: `Scripts/Tools/Inventory/Data/InventoryDatabase.cs`, namespace `Neo.Tools`. Asset creation: menu **Create → Neoxider → Tools → Inventory → Inventory Database**.

**How to use:**
1. Create an asset via the menu above.
2. Fill in **Items** with the `InventoryItemData` assets available in this inventory domain.
3. Assign the database wherever an inventory component needs to validate/resolve item ids (e.g. `InventoryComponent`).
4. Item ids must be unique — a duplicate logs an editor warning (`OnValidate`).

---

## Fields

| Field | Description |
|-------|-------------|
| **Items** | List of `InventoryItemData` assets available in this inventory domain. |

## API

| Member | Description |
|--------|-------------|
| `Items` | Read-only view of the configured item list. |
| `ContainsId(int itemId)` | Whether an item with this id exists in the database. |
| `GetItemData(int itemId)` | Returns the `InventoryItemData` for the id, or `null` if not found. |
| `TryGetItemData(int itemId, out InventoryItemData data)` | Same as `GetItemData`, without a null check on the caller's side. |

## See also

- [InventoryItemData](./InventoryItemData.md)
- [Inventory README](../README.md)
