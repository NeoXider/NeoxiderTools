# InventoryView

**Purpose:** The main UI component for displaying inventory contents as a list. It can automatically spawn `InventoryItemView` cells from a prefab or update pre-placed cells manually (Manual Mode).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Reference to `InventoryComponent`. Auto-finds if left empty. |
| **View Mode** | `SpawnFromPrefab` — dynamically creates cells. `ManualList` — updates only your pre-placed `InventoryItemView` list. |
| **Source Mode** | Where to pull the item list: `DatabaseItems` (all from DB), `SnapshotItems` (only owned), `Hybrid` (merged). |
| **Show Only Non Zero** | Hide cells for items with a count of 0. |
| **Item View Prefab** | Cell prefab (with `InventoryItemView`) for `SpawnFromPrefab` mode. |
| **Items Root** | Container for spawned cells. Defaults to this Transform. |
| **Manual Views** | List of pre-placed `InventoryItemView` for `ManualList` mode. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetInventory(InventoryComponent inventory)` | Bind to a different inventory and refresh the UI. |
| `void Refresh()` | Force a redraw of all cells. |

## Examples

### No-Code Example (Inspector)
Create a UI panel with a `Vertical Layout Group`. Attach `InventoryView`. Drag your row prefab (with `InventoryItemView`) into `Item View Prefab`. Set `Source Mode = Hybrid`, `Show Only Non Zero = true`. On play, the panel will automatically show your inventory items.

### Code Example
```csharp
[SerializeField] private InventoryView _shopView;

public void OpenShopUI(InventoryComponent shopInventory)
{
    _shopView.SetInventory(shopInventory);
    _shopView.gameObject.SetActive(true);
}
```

## See Also
- [InventoryItemView](InventoryItemView.md)
- [InventorySlotGridView](InventorySlotGridView.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](README.md)
