# Shop module

Shop controller, items, bundles, currency, inventory integration, and purchase UI. Scripts in `Scripts/Shop/`.

Since version **8.5.0**: stable string-ids for items, JSON profile save (`ShopProfileData`), bundles, categories, multi-currency, and an optional [Inventory](../Tools/Inventory/README.md) link via the [ShopInventoryGrantBridge](Bridges.md) component.

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
| [ShopCategorySelector](./ShopCategorySelector.md) | Previous/next category selector |
| [ShopListViewCategoryBar](./ShopListViewCategoryBar.md) | Adapter driving `ShopListView` from a generic `Neo.UI.CategoryBar` |
| [ShopPurchaseButtonView](./ShopPurchaseButtonView.md) | Reactive Buy/Select/Selected/Unaffordable button state per item slot |
| [ShopVariantsPanel](./ShopVariantsPanel.md) | Furniture/equipment variants panel: unowned/owned/equipped states, buy-then-equip |
| [ShopResolver](./ShopResolver.md) | Shared parent-first, scene-fallback lookup policy for optional shop references |
| [Shop bridges](./Bridges.md) | Canonical setup, fallback/lifecycle rules and API of the optional Inventory grant bridge |
| [EquipmentManager](./Equipment/EquipmentManager.md) | Independent equipment state and optional sprite slots per category |
| [ShopItem](./ShopItem.md), [TextMoney](./TextMoney.md) | Reusable item cells and reactive wallet text |

## Runtime API

Use catalog assets or stable ids from ordinary C#:

```csharp
Shop shop = GetComponent<Shop>();
shop.SetMoneySpendSource(wallet.gameObject);
shop.OnPurchasedId.AddListener(HandlePurchased);
if (shop.CanAfford(item))
{
    shop.Buy(item);
}
```

`CanAfford` is a precheck; purchase success comes from `OnPurchasedId`. `Select` changes equipment directly and does not check ownership. Use `Buy` for the purchase flow. `Select((ShopItemData)null)` clears selection and its highlights.

## Dynamic Storefront Views

Recommended setup for category stores: keep one `Shop` for catalog, save, currency, and purchases; disable `Auto Spawn Items` on that `Shop`; then let one or more `ShopListView` components own the visible lists.

For scene/NoCode setup, assign the catalog and wallet to `Shop`, assign a row prefab to `ShopListView`, and add `ShopPurchaseButtonView` to the row for affordability. Use category buttons/selectors to filter the list and `ShopVariantsPanel` for equipment variants. Rows notify dependent views when rebound; runtime wallet replacement updates affordability immediately. Disabled views reconnect on enable.

In **11.0.0**, free/paid profile mutation and persistence share rollback. A failed profile save restores `Money` spending above its cap without increasing lifetime income. Successful saves commit before view and purchase notifications: observer errors are reported through `OnNotificationFault` / `LastNotificationFault` and leave the purchase committed. Bundle notifications continue with later item ids. Nested `Buy` / `BuyBundle` calls are ignored until the current call settles; call again after it returns. [Shop](./Shop.md#purchase-failure-and-profile-data) explains this event-contract migration and best-effort Inventory delivery.

Networked Money now defaults to server-only client mutation authority. Choose cooperative `None` or wallet `OwnerOnly` explicitly when needed; see [Money](Money.md#client-mutation-authority-1100).

## Tests

Shop has both EditMode and PlayMode coverage:

- `Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs` — purchases, bundles, runtime prices, `Browse` / `EquipOnly` flows, inventory integration, multi-currency, and `ShopListView`.
- `Assets/Neoxider/Tests/Edit/Shop/ShopProfileDataTests.cs` — JSON round-trip, sanitize/dedupe, runtime price overrides, and clone.
- `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopRegressionTests.cs` — failed/free purchases, capped refunds, invalid numbers/profile collections, equipment restore, row rebinding, and view lifecycle/source changes.
- `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopMoneyAuthorityTests.cs` — authority/default serialization and local admission-unit coverage.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoShopCommitPlayModeTests.cs` — post-delivery observer faults, wallet/profile/inventory conservation, native TextMoney lifecycle and paused limiter cadence.
- `Assets/Neoxider/Tests/Edit/Save/ShopManagerTests.cs` — legacy Shop/Save coverage.

Run them through Unity Test Runner → EditMode / PlayMode.

Currency contracts are documented in [InterfaceMoney](./InterfaceMoney.md).

## See also

- [Save](../Save/README.md)
- [Tools/Inventory](../Tools/Inventory/README.md)
