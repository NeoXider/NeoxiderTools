# Shop module

Shop controller, items, bundles, currency, inventory integration, and purchase UI. Scripts in `Scripts/Shop/`.

Since version **8.5.0**: stable string-ids for items, JSON profile save (`ShopProfileData`), bundles, categories, multi-currency, and an optional [Inventory](../Tools/Inventory/README.md) link via the [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md) component.

## Entry pages

| Page | Description |
|------|-------------|
| [Shop](./Shop.md) | Main shop controller, save behavior, and purchase flow |
| [ShopItemData](./ShopItemData.md) | Item asset: id, category, currency override, optional inventory link |
| [ShopBundleData](./ShopBundleData.md) | Bundle of items sold for a single price |
| [ButtonPrice](./ButtonPrice.md) | Shop button view states and price presentation |
| [Money](./Money.md) | Currency component, reactive values, and save behavior |
| [ShopListView](./ShopListView.md) | Optional category/filter view that creates and reuses `ShopItem` cells |
| [ShopCategoryButton](./ShopCategoryButton.md) | NoCode category tab for `ShopListView` |
| [Russian Shop docs](../../Docs/Shop/README.md) | Full Russian per-component documentation |

## Dynamic Storefront Views

Recommended setup for category stores: keep one `Shop` for catalog, save, currency, and purchases; disable `Auto Spawn Items` on that `Shop`; then let one or more `ShopListView` components own the visible lists.

## Tests

Shop has both EditMode and PlayMode coverage:

- `Assets/Tests/Play/ShopPurchasePlayModeTests.cs` — purchases, bundles, runtime prices, `Browse` / `EquipOnly` flows, inventory integration, multi-currency, and `ShopListView`.
- `Assets/Tests/Edit/ShopProfileDataTests.cs` — JSON round-trip, sanitize/dedupe, runtime price overrides, and clone.
- `Assets/Tests/Edit/Save/ShopManagerTests.cs` — legacy Shop/Save coverage.

Run them through Unity Test Runner → EditMode / PlayMode.

## Russian docs (per-component)

| Page | Description |
|------|-------------|
| [Shop README](../../Docs/Shop/README.md) | Overview |
| [Shop](../../Docs/Shop/Shop.md) | Shop controller |
| [ShopItem](../../Docs/Shop/ShopItem.md), [ShopItemData](../../Docs/Shop/ShopItemData.md), [ShopBundleData](../../Docs/Shop/ShopBundleData.md) | Item display, data, and bundles |
| [Money](../../Docs/Shop/Money.md), [InterfaceMoney](../../Docs/Shop/InterfaceMoney.md) | Currency |
| [ButtonPrice](../../Docs/Shop/ButtonPrice.md), [TextMoney](../../Docs/Shop/TextMoney.md) | UI helpers |

## See also

- [Save](../Save/README.md)
- [Tools/Inventory](../Tools/Inventory/README.md)
