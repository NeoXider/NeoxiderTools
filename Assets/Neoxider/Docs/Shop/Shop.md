# Shop

**Purpose:** Main in-game shop controller. Responsible for:

- generating item UI from `ShopItemData`;
- processing purchases of individual items and bundles (`ShopBundleData`);
- persisting state (owned items, equipped id, runtime discounts) as a single JSON blob via `SaveProvider`;
- managing the selected (equipped) item;
- multi-currency support through per-item and per-bundle `IMoneySpend` overrides.

**Inventory integration** lives in a separate bridge: [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md) in `Neo.Tools.Inventory`. The bridge listens to `Shop.OnPurchasedId` and grants `InventoryItemData` from its mapping table. `Neo.Shop.asmdef` intentionally does not depend on `Neo.Tools.Inventory`, which avoids an asmdef cycle.

Since version **8.5.0**, item identity is the stable `string Id` from `ShopItemData`, not an array index. The save format is a hard break: legacy keys `Shop0/Shop1/.../ShopEquipped` are no longer read.

Since **8.5.1**, if catalog assets still have an empty `Id`, `Shop` backfills unique ids in `Awake` before `LoadProfile()` (details: [ShopItemData -> Id auto-fill](./ShopItemData.md#id-auto-fill)). This fixes cases where every `ShopListView` cell showed the same state.

## Dynamic Views

`Shop` can be used only as the purchase/catalog controller while external views own all visible cells.

- Disable `Auto Spawn Items` when using `ShopListView`.
- Use `ShopListView` to create/reuse `ShopItem` cells and filter by `ShopItemData.Category`.
- Use `ShopCategoryButton` for Inspector-only category tabs.
- Runtime catalog helpers: `SetItems(...)`, `SetBundles(...)`, `SetMoneySpendSource(...)`, `SetAutoSpawnItems(...)`.
- Refresh helpers/events: `RefreshVisuals()`, `OnShopChanged`, `GetCategories(...)`.

This keeps one `Shop` as the source of truth for save, ownership, prices, currency, bundles, and inventory bridge events.

## Currency Override by Save Key

`ShopItemData` and `ShopBundleData` can select a currency by `Money.SaveKey`.

- Leave `Currency Override Save Key` empty to use the Shop default (`moneySpendSource`, then `Money.I`).
- Set it to a key such as `Gems` to spend from the `Money` instance whose `SaveKey == "Gems"`.
- The old GameObject override is still supported as a scene fallback, but the save key field is the recommended option for ScriptableObjects.

## Setup

1. `Add Component > Neoxider > Shop > Shop` on an empty GameObject.
2. Fill `_shopItemDatas` with `ShopItemData` assets (see [ShopItemData](./ShopItemData.md)).
3. Optionally fill `_bundles` with `ShopBundleData` assets.
4. Use `_prefab` + `_container` for auto-spawn UI, or pre-place `ShopItem` components and assign `_shopItems`.
5. Optionally add [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) on the same GameObject to auto-grant `InventoryItemData` on purchase.

## Purchase Flow (`ShopPurchaseFlow`)

| Mode | Behavior |
|------|----------|
| `BuyAndEquip` (default) | Buy -> auto-select. Equivalent to the legacy `_useSetItem = true` path. |
| `BuyOnly` | Purchase only; equipped item is not changed. |
| `EquipOnly` | No spending; toggle selection only, useful for skin/cosmetic UI. |
| `Browse` | Read-only storefront: `Buy()` and `BuyBundle()` are no-ops; preview still works. |

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_purchaseFlow` | Purchase mode. |
| `_shopItemDatas` | Array of item assets. Source of prices, sprites, descriptions, and stable ids. |
| `_bundles` | Optional bundle assets. |
| `_shopItemPreview` | UI preview slot. |
| `_shopItems` | Auto-populated from children plus optional auto-spawn from `_prefab`. |
| `_container`, `_prefab` | Parent and prefab for auto-spawn. |
| `_keySave` | Single `SaveProvider` key for the JSON `ShopProfileData`. Deleting this key fully wipes shop state. |
| `moneySpendSource` | Default GameObject with `IMoneySpend`. When null, `Money.I` is used. Item/Bundle `CurrencyOverrideSaveKey` takes precedence. |
| `_autoSubscribe` | Auto-subscribe `ShopItem.buttonBuy` to the item purchase action. |
| `_changePreviewOnPurchaseFailed` | Switch preview to the item on failed purchase. |
| `_propagateSelectionVisual` (formerly `_useSetItem`) | Call `ShopItem.Select(bool)` on every list entry when equipped changes. |
| `_activateSavedEquipped` | Auto-equip on load (`BuyAndEquip` / `EquipOnly` only): saved item, or the first catalog entry when save is empty or invalid. |
| `_prices`, `_keySaveEquipped` | Deprecated. Kept as serialized fields for legacy scene compatibility but ignored at runtime. |

## Public API

### Typed Asset API (canonical before v9)

Prefer these overloads when gameplay/UI code already works with catalog assets. They keep code independent from array order and make the v9 removal of int-indexed calls straightforward.

| Member | Purpose |
|--------|---------|
| `Buy(ShopItemData itemData)` | Buy / equip by item asset. |
| `BuyBundle(ShopBundleData bundleData)` | Buy a bundle by bundle asset. |
| `Select(ShopItemData itemData)` | Equip by item asset; pass `null` to clear selection. |
| `ShowPreview(ShopItemData itemData)` | Set preview by item asset; pass `null` to clear preview. |
| `IsOwned(ShopItemData itemData)` / `IsBundleOwned(ShopBundleData bundleData)` | Ownership query by typed asset. |
| `GetPrice(ShopItemData itemData)` | Current item price with runtime override applied. |
| `CanAfford(ShopItemData itemData)` | Affordability with the same currency resolution the purchase uses. |
| `SetRuntimePrice(ShopItemData itemData, float price)` / `ClearRuntimePrice(ShopItemData itemData)` | Runtime discounts by typed item asset. |

### String Id API

Use this API when code stores or receives ids rather than assets.

| Member | Purpose |
|--------|---------|
| `EquippedId : string` | Currently equipped item. |
| `PreviewIdString : string` | Item shown in the preview slot. |
| `Buy(string itemId)` | Buy / equip by id. Respects `_purchaseFlow`. |
| `BuyBundle(string bundleId)` | Buy a bundle by id. |
| `Select(string itemId)` | Equip without buying. Pass `""` to clear. |
| `ShowPreview(string itemId)` | Set the preview slot. |
| `IsOwned(string itemId)` / `IsBundleOwned(string bundleId)` | Ownership query. |
| `GetPrice(string itemId)` | Current price with runtime override applied. |
| `CanAfford(string itemId)` | True when the item could be bought right now: owned/free items always; priced items query the same wallet the purchase would use (per-item currency override included). Wallets without `IMoneyCanSpend` stay optimistic. |
| `ResolveCurrencyMoney(string itemId)` | The `Money` the purchase would spend from (null for custom non-`Money` wallets) — for balance subscriptions in views like `ShopPurchaseButtonView`. |
| `SetRuntimePrice(string itemId, float price)` / `ClearRuntimePrice(string itemId)` | Discounts / temporary price overrides. |
| `GetItemsInCategory(string category)` | Filter by `ShopItemData.Category`. |
| `ShopItemDatas`, `Bundles` | Catalog access. |

### Legacy Int API (`[Obsolete]`, removed in v9)

| Member | Behavior |
|--------|----------|
| `Id : int` | Proxy: `IndexOfItemDataById(EquippedId)` / `Select(items[i].Id)`. |
| `PreviewId : int` | `IndexOfItemDataById(PreviewIdString)`. |
| `Buy()` | Buys `PreviewIdString` with fallback to `EquippedId`. |
| `Buy(int id)` | Resolves `_shopItemDatas[id].Id` -> `Buy(string)`. |
| `ShowPreview(int id)` | Resolves `_shopItemDatas[id].Id` -> `ShowPreview(string)`. |
| `Prices : int[]` | Legacy array; ignored at runtime. |

## Events

| Event | Argument | When |
|-------|----------|------|
| `OnSelect` | `int` index | Equip; legacy. |
| `OnSelectId` | `string` id | Equip. |
| `OnPurchased` | `int` index | Successful purchase; legacy. |
| `OnPurchasedId` | `string` id | Successful item purchase. |
| `OnPurchaseFailed` | `int` index | Insufficient funds; legacy. |
| `OnPurchaseFailedId` | `string` id | Insufficient funds for item or bundle. |
| `OnPurchasedBundle` | `ShopBundleData` | Bundle purchased after all items are granted. |
| `OnLoad` | none | Fired after `Start()`. |

Inventory grant events (`OnGranted` with `(InventoryItemData, int)`) live on [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md), not on `Shop`.

## Legacy Scene Compatibility

- Old serialized fields (`_prices`, `_keySaveEquipped`, `_useSetItem` -> `_propagateSelectionVisual` via `FormerlySerializedAs`, `_activateSavedEquipped`) are kept so scenes preserve Inspector data.
- Save format is a hard break: legacy `Shop0/Shop1` keys are not read; on first launch the shop starts with an empty `ShopProfileData`.
- UnityEvent subscriptions to `OnSelect<int>` / `OnPurchased<int>` keep working: `Buy(string)` raises both int and string event variants.

## Tests

- `Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs` - main PlayMode coverage for purchases, bundles, shop flows, multi-currency, inventory, `ShopListView`, and typed asset API.
- `Assets/Neoxider/Tests/Edit/ShopProfileDataTests.cs` - EditMode profile, JSON, sanitize, and runtime price override coverage.
- `Assets/Neoxider/Tests/Edit/Save/ShopManagerTests.cs` - legacy Shop/Save coverage.

## See Also

- [Module root](../README.md)
- [ShopItemData](./ShopItemData.md)
- [ShopBundleData](./ShopBundleData.md)
- [Money](./Money.md)
- [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md)
