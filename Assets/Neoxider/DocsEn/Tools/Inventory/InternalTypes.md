# Inventory Core — Internal Types

Internal core types for the inventory system. Not MonoBehaviours — used internally by `InventoryComponent` and `InventoryManager`.

| Type | Description |
|------|-------------|
| `AggregatedInventory` | Merges multiple inventories into one. |
| `IInventoryItemState` | Item state interface. |
| `IInventoryStorage` | Item storage interface. |
| `InventoryConstraints` | Inventory constraints (max slots, max stack). |
| `InventoryEntry` | Inventory item entry (data + count). |
| `InventoryItemComponentState` | Item component state. |
| `InventoryItemInstance` | Item instance with unique ID. |
| `InventoryItemRecord` | Item record for serialization. |
| `InventoryManager` | Inventory manager (add/remove logic). |
| `InventorySaveData` | Inventory save data. |
| `InventorySlotState` | Slot state (empty, occupied, locked). |
| `InventorySlotTransferRules` | Slot transfer rules. |
| `InventoryStackRules` | Item stacking rules. |
| `InventoryStorageMode` | Storage mode (List, Slots). |
| `InventoryTransferService` | Cross-inventory transfer service. |
| `ISlottedInventory` | Slotted inventory interface. |
| `SlotGridInventory` | Grid inventory (2D slots). |
| `InventoryDatabase` | ScriptableObject — all items database. |
| `InventoryInitialStateData` | Initial inventory state (preset). |

### InventoryComponent Partials
| File | Description |
|------|-------------|
| `InventoryComponent.Grid` | Grid functionality. |
| `InventoryComponent.Operations` | Operations (add, remove, move). |
| `InventoryComponent.Persistence` | Save/load. |
| `InventoryComponent.Queries` | Queries (search, filter, count). |

### Runtime Utilities
| Type | Description |
|------|-------------|
| `InventoryItemStateBehaviour` | MonoBehaviour wrapper for IInventoryItemState. |
| `InventoryItemStateUtility` | Utility for item state operations. |

## See Also
- [InventoryComponent](InventoryComponent.md) — ← [Tools/Inventory](README.md)
