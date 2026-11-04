# Shop

**What it is:** the main shop controller (`MonoBehaviour`, `Scripts/Shop/Shop.cs`, namespace `Neo.Shop`). It owns the
catalog (`ShopItemData` / `ShopBundleData`), runs item and bundle purchases, keeps the equipped item, and persists
owned items, the equipped id and runtime discounts as one JSON blob (`ShopProfileData`) through `SaveProvider`.
Currency comes from `IMoneySpend` wallets, with per-item / per-bundle overrides by `Money.SaveKey`.

**How to use:**
1. Add `Neoxider > Shop > Shop` to a GameObject and fill `_shopItemDatas` (and optionally `_bundles`).
2. Assign `moneySpendSource` (or rely on `Money.I`).
3. Let the Shop spawn rows from `_prefab`, or disable `Auto Spawn Items` and use [ShopListView](./ShopListView.md).
4. Buy from C# with `TryBuy(item)` (typed result) or `Buy(item)`, or wire `ShopItem.buttonBuy` / UnityEvents for NoCode.

```csharp
ShopPurchaseStatus status = shop.TryBuy(skin);
if (status == ShopPurchaseStatus.InsufficientFunds)
{
    ShowNotEnoughCoins();
}
```

---

**Inventory integration** lives in the separate `Neo.Shop.Bridges` assembly:
[ShopInventoryGrantBridge](Bridges.md). The bridge listens to
`Shop.OnPurchasedId` and grants `InventoryItemData` from its mapping table. It references both
`Neo.Shop` and `Neo.Tools.Inventory`; the two base modules do not reference each other. The public bridge
type remains in namespace `Neo.Tools` for compatibility.

Since version **8.5.0**, item identity is the stable `string Id` from `ShopItemData`, not an array index. The save format is a hard break: legacy keys `Shop0/Shop1/.../ShopEquipped` are no longer read.

Since **8.5.1**, if catalog assets still have an empty `Id`, `Shop` backfills unique ids in `Awake` before `LoadProfile()` (details: [ShopItemData -> Id auto-fill](./ShopItemData.md#id-auto-fill)). This fixes cases where every `ShopListView` cell showed the same state.

## Dynamic Views

`Shop` can be used only as the purchase/catalog controller while external views own all visible cells.

- Disable `Auto Spawn Items` when using `ShopListView`.
- Use `ShopListView` to create/reuse `ShopItem` cells and filter by `ShopItemData.Category`.
- Use `ShopCategoryButton` for Inspector-only category tabs.
- Runtime catalog helpers: `SetItems(...)`, `SetBundles(...)`, `SetMoneySpendSource(...)`, `SetAutoSpawnItems(...)`.
- Refresh helpers/events: `RefreshVisuals()`, `OnShopChanged`, `GetCategories(...)`.

With `Auto Spawn Items` on and a `_prefab` assigned, `SetItems(...)` with a larger catalog spawns the missing
Shop-owned rows and binds their buy buttons once (re-subscribing never stacks a second listener). A smaller catalog
keeps the extra rows bound to an empty item.

This keeps one `Shop` as the source of truth for save, ownership, prices, currency, bundles, and inventory bridge events.

## Currency Override by Save Key

`ShopItemData` and `ShopBundleData` can select a currency by `Money.SaveKey`.

- Leave `Currency Override Save Key` empty to use the Shop default (`moneySpendSource`, then `Money.I`).
- Set it to a key such as `Gems` to spend from the `Money` instance whose `SaveKey == "Gems"`.
- If the keyed wallet cannot be found, resolution falls back to the Shop default. Configure distinct, stable wallet keys.
- `moneySpendSource` is the scene GameObject for the default `IMoneySpend`; asset data stores only the currency save key.
- `SetMoneySpendSource(GameObject)` changes the default at runtime and raises `OnShopChanged`, so purchase-button views reconnect even when the item id stays the same. Custom wallets without a balance event need `RefreshVisuals()` after external balance changes.

## Setup

1. `Add Component > Neoxider > Shop > Shop` on an empty GameObject.
2. Fill `_shopItemDatas` with `ShopItemData` assets (see [ShopItemData](./ShopItemData.md)).
3. Optionally fill `_bundles` with `ShopBundleData` assets.
4. Use `_prefab` + `_container` for auto-spawn UI, or pre-place `ShopItem` components and assign `_shopItems`.
5. Optionally add [`ShopInventoryGrantBridge`](Bridges.md) and assign Shop/Inventory explicitly. Its Shop fallback searches parents then the active scene; Inventory fallback uses an existing singleton only. See the canonical bridge page for enable/disable and source-switching rules.

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

### Typed Asset API

Prefer these overloads when gameplay/UI code already works with catalog assets. They keep code independent from array order. The deprecated int-indexed calls remain available for existing scene wiring.

| Member | Purpose |
|--------|---------|
| `Buy(ShopItemData itemData)` | Buy / equip by item asset. |
| `TryBuy(ShopItemData itemData) : ShopPurchaseStatus` | Same flow and events as `Buy`, returns the outcome. |
| `BuyBundle(ShopBundleData bundleData)` | Buy a bundle by bundle asset. |
| `TryBuyBundle(ShopBundleData bundleData) : ShopPurchaseStatus` | Same flow and events as `BuyBundle`, returns the outcome. |
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
| `TryBuy(string itemId) : ShopPurchaseStatus` | `Buy` with a typed outcome (see below). |
| `BuyBundle(string bundleId)` | Buy a bundle by id. |
| `TryBuyBundle(string bundleId) : ShopPurchaseStatus` | `BuyBundle` with a typed outcome. |
| `Select(string itemId)` | Equip without buying or checking ownership. Pass `""` to clear selection and Shop-owned highlights. |
| `ShowPreview(string itemId)` | Set the preview slot. |
| `IsOwned(string itemId)` / `IsBundleOwned(string bundleId)` | Ownership query. |
| `GetPrice(string itemId)` | Current price with runtime override applied. |
| `CanAfford(string itemId)` | True when the item could be bought right now: owned/free items always; priced items query the same wallet the purchase would use (per-item currency override included). Wallets without `IMoneyCanSpend` stay optimistic. |
| `ResolveCurrencyMoney(string itemId)` | The `Money` the purchase would spend from (null for custom non-`Money` wallets) — for balance subscriptions in views like `ShopPurchaseButtonView`. |
| `SetRuntimePrice(string itemId, float price)` / `ClearRuntimePrice(string itemId)` | Discounts / temporary price overrides. |
| `GetItemsInCategory(string category)` | Filter by `ShopItemData.Category` (returns a new array). |
| `GetItemsInCategory(string category, List<ShopItemData> results) : int` | Same filter into a caller-owned list, without allocating. |
| `ShopItemDatas`, `Bundles` | Catalog access. |

Id lookups (`GetPrice`, `CanAfford`, `ResolveCurrencyMoney`, `TryBuy`, ...) go through an id index that is checked
against the live catalog on every hit, so per-row view refreshes stay linear in the number of rows and do not
allocate. Replacing an entry of `ShopItemDatas` in place or calling `SetItems` is picked up on the next lookup.
With duplicate ids the first catalog entry wins, as before, for every catalog applied through `SetItems` (or loaded
in `Awake`). An in-place edit that gives an earlier slot the id of an already indexed later slot keeps resolving to
the later slot until the catalog is re-applied: call `SetItems(shop.ShopItemDatas, false)` after such edits.

### Typed purchase result (`ShopPurchaseStatus`)

`TryBuy` / `TryBuyBundle` run exactly the same flow as `Buy` / `BuyBundle` and raise the same events; the return
value lets C# code branch without subscribing to UnityEvents.

| Status | Meaning |
|--------|---------|
| `Purchased` | Spending (if any) and the profile commit succeeded; purchase events were raised. |
| `AlreadyOwned` | Item or single-purchase bundle already owned: nothing spent. An item is also selected (`BuyAndEquip`) and previewed; an owned bundle has no side effects. |
| `Selected` | `EquipOnly` flow: selected and previewed without spending. |
| `InsufficientFunds` | The wallet rejected the debit (not enough balance or no wallet). `OnPurchaseFailedId` was raised. |
| `CommitFailed` | The debit succeeded but saving the profile failed; profile rolled back, debit refunded, `OnPurchaseFailedId` raised. |
| `PendingServerAuthority` | The wallet cannot confirm the debit locally (networked client). Nothing was spent or granted locally, no failure event was raised, and Shop does not finish this purchase later; run authoritative purchases on the server. |
| `Unavailable` | Empty/unknown id, or the `Browse` flow (or `EquipOnly` for bundles). |
| `Busy` | Another purchase on this shop is still running (nested call from a callback). |

### Legacy Int API (`[Obsolete]`, retained for compatibility)

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
| `OnPurchaseFailed` | `int` index | Rejected item purchase or rolled-back grant; legacy. |
| `OnPurchaseFailedId` | `string` id | Rejected item/bundle purchase or rolled-back grant. |
| `OnPurchasedBundle` | `ShopBundleData` | Committed bundle, after every child purchase notification has been attempted. |
| `OnNotificationFault` | `ShopNotificationFault` | A view/purchase observer failed after the purchase outcome settled; does not refund or report purchase failure. |
| `OnLoad` | none | Fired after `Start()`. |

Inventory grant events (`OnGranted` with `(InventoryItemData, int)`) live on [`ShopInventoryGrantBridge`](Bridges.md), not on `Shop`.

## Purchase Failure and Profile Data

**11.0.0 deliberately changes observer exception semantics.** Item/bundle ownership, automatic `BuyAndEquip` selection, and `SaveProfile` form the compensated commit. If profile mutation/persistence fails, Shop restores its previous profile/JSON, refunds the confirmed debit and raises purchase failure; no purchase/grant notification is emitted. Built-in `Money` refunds bypass `MaxMoney` and do not increase `AllMoney`. Custom wallets need `IMoneyAdd`; their refund implementation can fail or clamp.

After a successful save, item visuals, `OnShopChanged`, purchase events, selection events and preview are observers. Exceptions are caught per notification stage, logged through `NeoDiagnostics`, and published as `OnNotificationFault`. They never refund money, erase ownership or emit `OnPurchaseFailedId`. `LastNotificationFault` retains the latest payload (`PurchaseId`, `ItemId`, `Observer`, `Message`). The fault event can trigger scene feedback; code can inspect the typed payload. An exception in the fault reporter is logged without recursive reporting.

Bundle item ids/ownership decisions are captured before external callbacks. The whole bundle profile is saved before its first `OnPurchasedId`; one child's observer fault does not suppress later child ids or `OnPurchasedBundle`. Automatic selection is already visible in the committed item profile when purchase observers run, while select notifications retain their later order. A single UnityEvent invocation still has normal Unity listener behavior: an exception can stop its remaining listeners. The bridge therefore remains **best effort**; capacity limits, missing mappings or an earlier failing listener can prevent delivery without refunding the committed shop purchase. There is no inventory reservation or delivery acknowledgement.

`Buy` and `BuyBundle` share one guard acquired before currency queries/debit and released in `finally`, including when a failure callback throws. Nested item/bundle purchase calls from wallet, view or purchase callbacks are ignored without spending/granting (`TryBuy` reports `Busy`); no deferred purchase is queued. Schedule the next purchase after the outer call returns.

A purchase notifies in this order: Shop-owned rows and `OnShopChanged`, `OnPurchased`/`OnPurchasedId` (per item for
bundles), then `OnSelect`/`OnSelectId` for `BuyAndEquip`, then the preview. `ShopListView` rebuilds on the first of
these and skips the follow-up notifications that carry no new state, so one purchase costs one list rebuild.

```csharp
shop.OnNotificationFault.AddListener(fault => HandleObserverFailure(fault.PurchaseId, fault.Observer, fault.Message));
shop.Buy(item); // a saved purchase stays committed even if UI/analytics feedback fails
```

Migration: move fallible eligibility checks before `Buy`; do not throw from `OnPurchasedId` to request a refund. Notifications observe a settled purchase. Projects requiring guaranteed full delivery need an explicit delivery contract beyond this legacy best-effort bridge.

`ShopProfileData.Sanitize()` repairs missing collections, removes empty/duplicate ids and non-finite price overrides, and normalizes the schema version. Shop calls it on load/save; a warm call does not allocate. Call it after deserializing a profile yourself. `SetRuntimePrice` / `SetPriceOverride` ignore `NaN` and infinities; finite prices at or below zero remain free. Profile collection properties remain mutable for compatibility.

## Legacy Scene Compatibility

- Old serialized fields (`_prices`, `_keySaveEquipped`, `_useSetItem` -> `_propagateSelectionVisual` via `FormerlySerializedAs`, `_activateSavedEquipped`) are kept so scenes preserve Inspector data.
- Save format is a hard break: legacy `Shop0/Shop1` keys are not read; on first launch the shop starts with an empty `ShopProfileData`.
- UnityEvent subscriptions to `OnSelect<int>` / `OnPurchased<int>` keep working: `Buy(string)` raises both int and string event variants.

## Tests

- `Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs` - main PlayMode coverage for purchases, bundles, shop flows, multi-currency, inventory, `ShopListView`, and typed asset API.
- `Assets/Neoxider/Tests/Edit/Shop/ShopTypedPurchaseAndRefreshTests.cs` - `TryBuy` / `TryBuyBundle` statuses, list refresh coalescing and zero-allocation rebinding, runtime catalog growth, event-driven `TextMoney` binding, indexed id lookups (in-place catalog edits, duplicate ids, zero allocation).
- `Assets/Neoxider/Tests/Edit/Bonus/CodeBuiltComponentEventsTests.cs` - every Shop, Shop.Bridges, Bonus and NPC component added from code has each UnityEvent it declares.
- `Assets/Neoxider/Tests/Edit/Shop/ShopProfileDataTests.cs` - EditMode profile, JSON, sanitize, and runtime price override coverage.
- `Assets/Neoxider/Tests/Edit/Save/ShopManagerTests.cs` - legacy Shop/Save coverage.

## See Also

- [Module root](README.md)
- [ShopItemData](./ShopItemData.md)
- [ShopBundleData](./ShopBundleData.md)
- [Money](./Money.md)
- [ShopInventoryGrantBridge](Bridges.md)
