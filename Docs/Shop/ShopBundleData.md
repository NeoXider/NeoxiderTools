# ShopBundleData

**What it is:** a `ScriptableObject` (`Scripts/Shop/Data/ShopBundleData.cs`, namespace `Neo.Shop`) describing a
**bundle** — a set of `ShopItemData` sold for one combined price. On a successful purchase the Shop marks every
single-purchase bundle item as owned and raises `OnPurchasedId` per item; the optional
[ShopInventoryGrantBridge](./Bridges.md) turns those events into inventory grants.

**How to use:**
1. Create it via `Create > Neoxider > Shop > Shop Bundle Data`, fill `_items` and `_bundlePrice`.
2. Add it to the Shop's `_bundles` and buy it with `shop.TryBuyBundle(bundle)` / `BuyBundle(bundle)`.

---

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
| `_items` | `ShopItemData` array the player receives. Inventory granting is configured per item id in the [bridge](./Bridges.md) mapping table. |
| `_currencyOverrideSaveKey` | Optional `Money.SaveKey`. When set, the bundle is charged from the matching `Money`; when empty, the Shop default `moneySpendSource` is used, then `Money.I`. |

## Purchase behaviour

`Shop.BuyBundle(id)` / `Shop.TryBuyBundle(id)`:

1. If `_purchaseFlow` is `Browse` or `EquipOnly` — no-op (`Unavailable`).
2. If the bundle is `isSinglePurchase` and already in `OwnedBundleIds` — no-op (`AlreadyOwned`).
3. Charges `_bundlePrice` through the resolved `IMoneySpend` (`_currencyOverrideSaveKey` first, then Shop default); a rejected debit raises `OnPurchaseFailedId(bundleId)` (`InsufficientFunds`).
4. Adds every single-purchase item to `OwnedItemIds` (and the bundle to `OwnedBundleIds` when single-purchase), then saves the profile; a failed save rolls back and refunds (`CommitFailed`).
5. Events: `OnShopChanged`, `OnPurchasedId(itemId)` per item, then `OnPurchasedBundle(bundle)` (`Purchased`). Inventory grants happen via [`ShopInventoryGrantBridge`](./Bridges.md), which subscribes to `OnPurchasedId`.

## Code API

```csharp
ShopBundleData bundle = ...;
bundle.Id;
bundle.price;
bundle.isSinglePurchase;
foreach (ShopItemData item in bundle.Items) { ... }
bundle.CurrencyOverrideSaveKey;
```

## See also

- [Shop](./Shop.md) · [ShopItemData](./ShopItemData.md) · [Module root](./README.md)
