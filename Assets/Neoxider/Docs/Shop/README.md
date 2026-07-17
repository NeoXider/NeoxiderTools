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
| [ShopListViewCategoryBar](./ShopListViewCategoryBar.md) | Adapter driving `ShopListView` from a generic `Neo.UI.CategoryBar` |
| [ShopPurchaseButtonView](./ShopPurchaseButtonView.md) | Reactive Buy/Select/Selected/Unaffordable button state per item slot |
| [ShopVariantsPanel](./ShopVariantsPanel.md) | Furniture/equipment variants panel: unowned/owned/equipped states, buy-then-equip |

## Dynamic Storefront Views

Recommended setup for category stores: keep one `Shop` for catalog, save, currency, and purchases; disable `Auto Spawn Items` on that `Shop`; then let one or more `ShopListView` components own the visible lists.

## Tests

Shop has both EditMode and PlayMode coverage:

- `Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs` — purchases, bundles, runtime prices, `Browse` / `EquipOnly` flows, inventory integration, multi-currency, and `ShopListView`.
- `Assets/Neoxider/Tests/Edit/ShopProfileDataTests.cs` — JSON round-trip, sanitize/dedupe, runtime price overrides, and clone.
- `Assets/Neoxider/Tests/Edit/Save/ShopManagerTests.cs` — legacy Shop/Save coverage.

Run them through Unity Test Runner → EditMode / PlayMode.

## docs (per-component)

| Page | Description |
|------|-------------|
 · Overview
| [Shop](./Shop.md) | Shop controller |
| [ShopItem](./ShopItem.md), [ShopItemData](./ShopItemData.md), [ShopBundleData](./ShopBundleData.md) | Item display, data, and bundles |
| [Money](./Money.md), [InterfaceMoney](./InterfaceMoney.md) | Currency |
| [ButtonPrice](./ButtonPrice.md), [TextMoney](./TextMoney.md) | UI helpers |

## See also

- [Save](../Save/README.md)
- [Tools/Inventory](../Tools/Inventory/README.md)
