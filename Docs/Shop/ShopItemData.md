# ShopItemData

**What it is:** a `ScriptableObject` (`Scripts/Shop/ShopItemData.cs`, namespace `Neo.Shop`) describing one shop
item: stable `Id`, display data, base price, optional category and optional per-item currency. Inventory grants are
configured in a separate bridge — see [ShopInventoryGrantBridge](./Bridges.md).

**How to use:**
1. Create it via `Create > Neoxider > Shop > Shop Item Data` and set a unique `Id`.
2. Add it to the Shop's `_shopItemDatas` and buy it with `shop.TryBuy(item)` / `Buy(item)`.

---

## Currency Override

Use `Currency Override Save Key` for asset-safe multi-currency setup.

- Empty key: use the Shop default currency.
- Non-empty key: Shop resolves `Money.FindBySaveKey(key)` and spends from that wallet.
- The GameObject `Currency Override` field was removed: a `ScriptableObject` should not store scene wallet references.

## Setup

1. `Right Click > Create > Neoxider > Shop > Shop Item Data`.
2. Set `Id` (or leave empty — see **Id auto-fill** below).
3. Configure remaining fields.
4. Add the asset to the `_shopItemDatas` array of the [Shop](./Shop.md) controller.

## Key fields (Inspector)

| Field | Description |
|-------|-------------|
| `_id` | **Stable identifier**. Ownership, equipped state, and `Shop` lookup key. **Do not change after release** — it invalidates saves. |
| `_isSinglePurchase` | Buyable only once? When `true`, after the first purchase the item id is added to `ShopProfileData.OwnedItemIds`. |
| `_nameItem`, `_description` | UI text. |
| `_price` | Base price. Runtime discounts are applied with `Shop.SetRuntimePrice(id, price)`. |
| `_sprite`, `_icon` | Preview sprite and small icon. |
| `_category` | Optional category string (`"weapons"`, `"skins"`, ...). Used by `Shop.GetItemsInCategory(category)`. Empty string = no category. |
| `_currencyOverrideSaveKey` | Optional `Money.SaveKey`. When set, the item is charged from the matching `Money`; when empty, the Shop default `moneySpendSource` is used, then `Money.I`. |

> Inventory grants are configured in [`ShopInventoryGrantBridge`](./Bridges.md) — its mapping table maps `ShopItemData.Id → InventoryItemData + Amount`.

## Id auto-fill

| When | Behavior |
|------|----------|
| **Editor** (`OnValidate`) | Empty `_id` → derived from `_nameItem` (spaces → `_`), same pattern as [QuestConfig](../Quest/QuestConfig.md). |
| **Runtime** (since **8.5.1**) | [Shop](./Shop.md) calls `EnsureMissingItemIds()` in `Awake` **before** loading the save: `nameItem` → asset file name → `{base}_{index in Shop array}`. Same when `SetItems(...)` replaces the catalog. Writes go through `AssignIdIfEmpty` only while `_id` is empty. |

For production builds, set unique `Id` values explicitly in the Inspector — runtime backfill is session memory and does not replace shipping assets with stable keys.

## Code API

```csharp
ShopItemData data = ...;
data.Id;                  // "sword_basic"
data.price;               // base price
data.Category;            // "weapons"
data.CurrencyOverrideSaveKey;
```

## See also

- [Shop](./Shop.md) · [ShopItem](./ShopItem.md) · [Module root](./README.md)
