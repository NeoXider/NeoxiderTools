# ShopInventoryGrantBridge

**Purpose:** optional bridge component linking [Shop](../../Shop/Shop.md) and [InventoryComponent](./InventoryComponent.md). Listens to `Shop.OnPurchasedId(itemId)` and, for every match in its `Mappings` table, calls `InventoryComponent.AddItemData(...)` and fires `OnGranted(data, amount)`.

**Assembly/source:** `Neo.Shop.Bridges`, `Scripts/Shop/Bridges/ShopInventoryGrantBridge.cs`. The public
type remains `Neo.Tools.ShopInventoryGrantBridge` for source and serialized-scene compatibility.

Available since **8.5.0**.

## Why not on `ShopItemData`

Putting `_inventoryItem` / `_inventoryAmount` directly on `ShopItemData` would couple the reusable Shop
catalog to Inventory. The dedicated `Neo.Shop.Bridges` assembly references both `Neo.Shop` and
`Neo.Tools.Inventory`; neither base module references the other. This keeps both modules independently
usable and avoids a dependency cycle while preserving the same scene workflow.

## Setup

1. Add `Shop Inventory Grant Bridge` (`Add Component > Neoxider > Tools/Inventory > ShopInventoryGrantBridge`) on the same GameObject as `Shop` (or any descendant).
2. Optionally assign `_shop` (when null, the bridge searches `GetComponentInParent<Shop>()` in `Awake`).
3. Assign `_inventory` or enable `_useInventorySingleton` to use `InventoryComponent.Instance`.
4. Fill `Mappings`:
   - `Shop Item Id` — stable `ShopItemData.Id`.
   - `Inventory Item` — matching `InventoryItemData`.
   - `Amount` — units granted per purchase (≥ 1).

When any mapped `ShopItemData` is purchased (directly or as part of a bundle — Shop raises `OnPurchasedId` for each item inside `BuyBundle`), the bridge looks up its mappings and grants the inventory.

## Key fields

| Field | Description |
|-------|-------------|
| `_shop` | Source of events. When null — auto-found via `GetComponentInParent<Shop>()` at `Awake`. |
| `_inventory` | Target inventory. When null and `_useInventorySingleton == true`, falls back to `InventoryComponent.Instance`. |
| `_useInventorySingleton` | Singleton fallback toggle when `_inventory` is null. |
| `_mappings` | List of `{ Shop Item Id, Inventory Item, Amount }`. |
| `OnGranted` | UnityEvent with arguments `(InventoryItemData, int amountAdded)`. |

## Code API

```csharp
ShopInventoryGrantBridge bridge = ...;

bridge.SetShop(shop);
bridge.SetInventory(inventory);
bridge.GrantForShopItemId("sword_basic"); // NoCode-friendly UnityEvent target
bridge.GrantDirect(inventoryItemData, 3); // direct grant (DLC, code paths)
bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping {
    ShopItemId = "season_pass_reward",
    InventoryItem = chest,
    Amount = 1
});
```

## See also

- [Shop](../../Shop/Shop.md) · [InventoryComponent](./InventoryComponent.md) · [Inventory module root](./README.md)
