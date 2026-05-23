# Shop

**Purpose:** Main in-game shop controller. Responsible for:

- generating item UI from `ShopItemData`;
- processing purchases of individual items and **bundles** (`ShopBundleData`);
- persisting state (owned items, equipped id, runtime discounts) as a single JSON blob via `SaveProvider`;
- managing the selected (equipped) item;
- multi-currency support: per-item and per-bundle `IMoneySpend` overrides.

**Inventory integration** lives in a separate bridge вЂ” [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md) (in `Neo.Tools.Inventory`). The bridge listens to `Shop.OnPurchasedId` and grants `InventoryItemData` based on its mapping table. This is intentional: `Neo.Shop.asmdef` does not depend on `Neo.Tools.Inventory` (avoids an asmdef cycle).

Since version **8.5.0** item identity is the stable `string Id` from `ShopItemData` (no longer an array index). Save format is a hard break: legacy keys `Shop0/Shop1/.../ShopEquipped` are no longer read (see CHANGELOG `## [8.5.0] Breaking`).

Since **8.5.1**, if catalog assets still have an empty `Id`, `Shop` backfills unique ids in `Awake` **before** `LoadProfile()` (details: [ShopItemData в†’ Id auto-fill](./ShopItemData.md#id-auto-fill)). This fixes cases where every `ShopListView` cell showed the same state (e.g. **USED** with no price).

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
- Set it to a key such as `Gems` to spend from `Money` whose `SaveKey == "Gems"`.
- The old GameObject override is still supported as fallback for scene setups, but the save key field is the recommended option for ScriptableObjects.

## Setup

1. `Add Component > Neoxider > Shop > Shop` on an empty GameObject.
2. Fill `_shopItemDatas` with `ShopItemData` assets (see [ShopItemData](./ShopItemData.md)).
3. (Optional) `_bundles` вЂ” array of `ShopBundleData`.
4. `_prefab` + `_container` for auto-spawn UI; or pre-place `ShopItem` components and assign `_shopItems`.
5. (Optional) Add [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) on the same GameObject to auto-grant `InventoryItemData` on purchase.

## Purchase flow (`ShopPurchaseFlow`)

| Mode | Behavior |
|------|----------|
| `BuyAndEquip` (default) | Buy в†’ auto-select. Equivalent to the legacy `_useSetItem = true` path. |
| `BuyOnly` | Purchase only; equipped item is not changed. |
| `EquipOnly` | No spending вЂ” toggle selection only (skin/cosmetic UI). |
| `Browse` | Read-only storefront: `Buy()` and `BuyBundle()` are no-ops; preview still works. |

## Key fields (Inspector)

| Field | Description |
|-------|-------------|
| `_purchaseFlow` | Purchase mode (see table above). |
| `_shopItemDatas` | Array of item assets. Source of prices, sprites, descriptions, and stable ids. |
| `_bundles` | Optional bundle assets. |
| `_shopItemPreview` | UI preview slot. |
| `_shopItems` | Auto-populated from children + auto-spawn from `_prefab`. |
| `_container`, `_prefab` | Parent and prefab for auto-spawn. |
| `_keySave` | Single `SaveProvider` key for the JSON `ShopProfileData`. Deleting this key fully wipes shop state. |
| `moneySpendSource` | Default GameObject with `IMoneySpend`. When null, `Money.I` is used. Item/Bundle `CurrencyOverrideSaveKey` takes precedence. |
| `_autoSubscribe` | Auto-subscribe `ShopItem.buttonBuy` to `Buy(index)`. |
| `_changePreviewOnPurchaseFailed` | Switch preview to the item on failed purchase. |
| `_propagateSelectionVisual` (formerly `_useSetItem`) | Call `ShopItem.Select(bool)` on every list entry when equipped changes. |
| `_activateSavedEquipped` | Auto-equip on load (`BuyAndEquip` / `EquipOnly` only): saved item, or the **first** catalog entry when save is empty or the id is invalid. |
| `_prices`, `_keySaveEquipped` | **Deprecated.** Kept as `[SerializeField]` for legacy scene compatibility but ignored at runtime. |

## Public API

### String-based (recommended since 8.5.0)

| Member | Purpose |
|--------|---------|
| `EquippedId : string` | Currently equipped item. |
| `PreviewIdString : string` | Item shown in the preview slot. |
| `Buy(string itemId)` | Buy / equip by id. Respects `_purchaseFlow`. |
| `BuyBundle(string bundleId)` | Buy a bundle. All bundle items go into owned, inventory receives all configured `InventoryItem`s. |
| `Select(string itemId)` | Equip without buying. Pass `""` to clear. |
| `ShowPreview(string itemId)` | Set the preview slot. |
| `IsOwned(string itemId)` / `IsBundleOwned(string bundleId)` | Ownership query. |
| `GetPrice(string itemId)` | Current price (with runtime override applied). |
| `SetRuntimePrice(string itemId, float price)` / `ClearRuntimePrice(string itemId)` | Discounts / temporary price overrides. |
| `GetItemsInCategory(string category)` | Filter by `ShopItemData.Category`. |
| `ShopItemDatas`, `Bundles` | Catalog access. |

### Legacy int API (`[Obsolete]`, will be removed in v9)

| Member | Behavior |
|--------|----------|
| `Id : int` | Proxy: `IndexOfItemDataById(EquippedId)` / `Select(items[i].Id)`. |
| `PreviewId : int` | `IndexOfItemDataById(PreviewIdString)`. |
| `Buy()` | Buys `PreviewIdString` (fallback to `EquippedId`). |
| `Buy(int id)` | Resolves `_shopItemDatas[id].Id` в†’ `Buy(string)`. |
| `ShowPreview(int id)` | Resolves `_shopItemDatas[id].Id` в†’ `ShowPreview(string)`. |
| `Prices : int[]` | Legacy array; ignored at runtime. |

## Events

| Event | Argument | When |
|-------|----------|------|
| `OnSelect` | `int` index | Equip вЂ” legacy. |
| `OnSelectId` | `string` id | Equip. |
| `OnPurchased` | `int` index | Successful purchase вЂ” legacy. |
| `OnPurchasedId` | `string` id | Successful item purchase. |
| `OnPurchaseFailed` | `int` index | Insufficient funds вЂ” legacy. |
| `OnPurchaseFailedId` | `string` id | Insufficient funds (item or bundle). |
| `OnPurchasedBundle` | `ShopBundleData` | Bundle purchased (after all items granted). |
| `OnLoad` | вЂ” | Fired after `Start()`. |

> Inventory grant events (`OnGranted` with `(InventoryItemData, int)`) live on [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md), not on Shop itself.

## Legacy scene compatibility

- Old serialized fields (`_prices`, `_keySaveEquipped`, `_useSetItem` в†’ `_propagateSelectionVisual` via `FormerlySerializedAs`, `_activateSavedEquipped`) are kept вЂ” scenes opened in Unity preserve Inspector data.
- **Save format is a hard break (wipe)**: legacy `Shop0/Shop1` keys are not read; on first launch the shop starts with an empty `ShopProfileData`.
- UnityEvent subscriptions to `OnSelect<int>` / `OnPurchased<int>` keep working вЂ” `Buy(string)` raises both `int` and `string` event variants.

## Tests

- `Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs` вЂ” main PlayMode coverage for purchases, bundles, shop flows, multi-currency, inventory, and `ShopListView`.
- `Assets/Neoxider/Tests/Edit/ShopProfileDataTests.cs` вЂ” EditMode profile, JSON, sanitize, and runtime price override coverage.
- `Assets/Neoxider/Tests/Edit/Save/ShopManagerTests.cs` вЂ” legacy Shop/Save coverage.

## See also

- [Module root](../README.md) В· [ShopItemData](./ShopItemData.md) В· [Money](./Money.md) В· [Russian Shop docs](../../Docs/Shop/README.md)
