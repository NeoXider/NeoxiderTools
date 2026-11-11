# InventoryInitialStateData

**What it is:** a `ScriptableObject` describing the initial contents of an inventory (a list of `InventoryEntry`). Path: `Scripts/Tools/Inventory/Data/InventoryInitialStateData.cs`, namespace `Neo.Tools`. Asset creation: menu **Create → Neoxider → Tools → Inventory → Inventory Initial State**.

**How to use:**
1. Create an asset via the menu above.
2. Fill in **Entries** with the item ids/counts the inventory should start with.
3. Assign the asset to the inventory component that applies initial state based on its load mode (e.g. new-game start, no save data found).

---

## Fields

| Field | Description |
|-------|-------------|
| **Entries** | List of `InventoryEntry` values applied based on the owning component's load mode. |

## API

| Member | Description |
|--------|-------------|
| `Entries` | Read-only view of the configured initial entries. |

## See also

- [InventoryDatabase](./InventoryDatabase.md)
- [Inventory README](../README.md)
