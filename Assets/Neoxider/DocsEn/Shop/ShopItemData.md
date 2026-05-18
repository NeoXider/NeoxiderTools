# ShopItemData

**Purpose:** A `ScriptableObject` describing one shop item. Since version **8.5.0** it carries a stable `Id`, an optional category, and an optional per-item currency. Inventory grants are configured in a separate bridge — see [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md).

## Currency Override

Use `Currency Override Save Key` for asset-safe multi-currency setup.

- Empty key: use the Shop default currency.
- Non-empty key: Shop resolves `Money.FindBySaveKey(key)` and spends from that wallet.
- The GameObject `Currency Override` field was removed: a `ScriptableObject` should not store scene wallet references.

## Setup

1. `Right Click > Create > Neoxider > Shop > Shop Item Data`.
2. Set `Id` (or leave empty — `OnValidate` auto-fills from `_nameItem`, mirroring [QuestConfig](../Quest/QuestConfig.md)).
3. Configure remaining fields.
4. Add the asset to the `_shopItemDatas` array of the [Shop](./Shop.md) controller.

## Key fields (Inspector)

| Field | Description |
|-------|-------------|
| `_id` | **Stable identifier**. Used as the ownership / equipped / lookup key. Auto-filled from `_nameItem` on validate when empty. **Do not change after release** — it invalidates saves. |
| `_isSinglePurchase` | Buyable only once? When `true`, after the first purchase the item id is added to `ShopProfileData.OwnedItemIds`. |
| `_nameItem`, `_description` | UI text. |
| `_price` | Base price. Runtime discounts are applied with `Shop.SetRuntimePrice(id, price)`. |
| `_sprite`, `_icon` | Preview sprite and small icon. |
| `_category` | Optional category string (`"weapons"`, `"skins"`, ...). Used by `Shop.GetItemsInCategory(category)`. Empty string = no category. |
| `_currencyOverrideSaveKey` | Optional `Money.SaveKey`. When set, the item is charged from the matching `Money`; when empty, the Shop default `moneySpendSource` is used, then `Money.I`. |

> Inventory grants are configured in [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) — its mapping table maps `ShopItemData.Id → InventoryItemData + Amount`.

## Code API

```csharp
ShopItemData data = ...;
data.Id;                  // "sword_basic"
data.price;               // base price
data.Category;            // "weapons"
data.CurrencyOverrideSaveKey;
```

## See also

- [Shop](./Shop.md) · [ShopItem](./ShopItem.md) · [Module root](../README.md)
