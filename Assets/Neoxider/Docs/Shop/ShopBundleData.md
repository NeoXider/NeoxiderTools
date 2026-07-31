# ShopBundleData

**Purpose:** A `ScriptableObject` describing a **bundle** — a set of `ShopItemData` sold for one combined price. On successful purchase the Shop adds every bundle item to `ShopProfileData.OwnedItemIds` and (optionally) grants each item's `InventoryItemData` to the attached inventory.

Available since version **8.5.0**.

## Currency Override

Use `Currency Override Save Key` for asset-safe multi-currency bundles.

- Empty key: use the Shop default currency.
- Non-empty key: Shop resolves `Money.FindBySaveKey(key)` and spends from that wallet.
- The GameObject `Currency Override` field was removed: bundles are `ScriptableObject` assets and select currency only by save key.

## Setup

1. `Right Click > Create > Neoxider > Shop > Shop Bundle Data`.
2. Set `Id` (or leave empty — auto-filled from `_nameBundle`).
3. Fill `_items` with `ShopItemData` assets the player will receive.
4. Set `_bundlePrice`.
5. Add the asset to the `_bundles` array of the [Shop](./Shop.md) controller.

## Key fields (Inspector)

| Field | Description |
|-------|-------------|
| `_id` | Stable identifier. Auto-filled from `_nameBundle`. |
| `_nameBundle`, `_description` | UI text. |
| `_sprite`, `_icon` | Preview and icon. |
| `_bundlePrice` | Bundle price. `0` = free bundle. |
| `_isSinglePurchase` | When `true`, the bundle id lands in `OwnedBundleIds` and cannot be purchased again. |
| `_items` | `ShopItemData` array the player receives. Per-item inventory granting is configured on each item (see [ShopItemData](./ShopItemData.md)). |
| `_currencyOverrideSaveKey` | Optional `Money.SaveKey`. When set, the bundle is charged from the matching `Money`; when empty, the Shop default `moneySpendSource` is used, then `Money.I`. |

## Purchase behaviour

`Shop.BuyBundle(id)`:

1. If `_purchaseFlow` is `Browse` or `EquipOnly` — no-op.
2. If the bundle is `isSinglePurchase` and already in `OwnedBundleIds` — no-op.
3. Charges `_bundlePrice` through the resolved `IMoneySpend` (`_currencyOverrideSaveKey` first, then Shop default).
4. For each `ShopItemData` in `_items`: when `isSinglePurchase`, add to `OwnedItemIds`; when `InventoryItem != null` and the Shop has an inventory, call `AddItemData(item, amount)`.
5. Events: `OnPurchasedId(itemId)` per item, `OnPurchasedBundle(bundle)` after all grants. Inventory grants happen via [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md), which subscribes to `OnPurchasedId`.

## Code API

```csharp
ShopBundleData bundle = ...;
bundle.Id;
bundle.price;
bundle.isSinglePurchase;
foreach (var item in bundle.Items) { ... }
bundle.CurrencyOverrideSaveKey;
```

## See also

- [Shop](./Shop.md) · [ShopItemData](./ShopItemData.md) · [Module root](../README.md)
