# TODO

Current technical tasks that should stay separate from the changelog. This list does not replace release planning; it records near-term public API improvements.

## Bonus / Slot

- [x] **Allow per-slot-machine symbol weight overrides.** Shipped in 9.11.0: `SlotSymbolWeightOverrides` on `SpinController` (matched by symbol id, definition weights as fallback), **Normalize Weights** in the Inspector `⋮` menu, `PickEconomySymbolId()`, deterministic `PickWeightedId` overloads, and EditMode coverage for disabled override, reordered/changed symbol lists, zero/negative weights, normalization, and weighted selection.

## Shop / UI

- [x] **Add an optional universal category-bar behaviour.** Shipped in 9.11.0: `Neo.UI.CategoryBar` + `CategoryBarItem` (selection state, re-parented selection marker with offset, id/index events, initial/runtime selection, prev/next, disabled entries, Inspector or runtime categories) with the `ShopListViewCategoryBar` adapter; no Shop dependency for generic use.
- [x] **Add an optional furniture/equipment variants panel on top of existing Shop views.** Shipped in 9.11.0: `ShopVariantsPanel` over `ShopListView`/`ShopItem` + optional `EquipmentManager` — unowned/owned/equipped (+ unequip) states through `IShopVariantView` / `ShopVariantStateView`, buy-then-equip, refresh on ownership/equipment changes.
- [x] **Make purchase affordability a reactive Shop view state.** Shipped in 9.11.0: `ButtonPrice.ButtonType.Unaffordable`, `ShopPurchaseButtonView` (balance subscription via `Shop.ResolveCurrencyMoney`, immediate state + `Button.interactable` updates, safe unsubscribe, rebinding), public `Shop.CanAfford(item/id)` + `IMoneyCanSpend`, with EditMode tests.

## GridSystem

- [x] Add a generic `GridPlacementService` / rule config on top of the current `FieldGenerator` placement API. Shipped in 9.11.0: `GridPlacementRequest` with `RequireEnabled`, `RequireWalkable`, `RequireUnoccupied`, custom predicate, and overwrite policy; atomic multi-cell placement with failure reasons.
- [x] Non-Mono plain C# `DiceBoard` service. Shipped in 9.12.0: `DiceBoard` core over `FieldGenerator` with C# events; `DiceBoardService` stays the MonoBehaviour wrapper with an unchanged scene API and forwards settings/events into the core.

## See Also

- [Ideas](IDEAS.md)
- [GridSystem](../Assets/Neoxider/Docs/GridSystem/README.md)
