# TODO

Current technical tasks that should stay separate from the changelog. This list does not replace release planning; it records near-term public API improvements.

## Bonus / Slot

- [ ] **Allow per-slot-machine symbol weight overrides.** Keep the weights from `SlotEconomyDefinition` as the default, but let a `SpinController` override them locally without modifying the shared asset. When override is enabled, expose one editable weight per symbol. Add **Normalize Weights** to the Inspector `⋮` menu so designers can normalize all positive weights to a total of `1` after editing. The runtime picker must use the override table when enabled and fall back to the definition otherwise. Cover disabled override, reordered/changed symbol lists, zero/negative weights, normalization, and weighted selection with EditMode tests.

## Shop / UI

- [ ] **Add an optional universal category-bar behaviour.** Extend the current category views with a reusable horizontal/tab `CategoryBar` that owns selection state and a configurable selected visual (frame, marker, offset) without resizing or repositioning authored child graphics. It should expose category id/index events and work with any consumer, with an optional adapter for `ShopListView`; it must not require the Shop module for generic use. Support initial selection, runtime selection, previous/next navigation, disabled entries, and Inspector-authored or runtime-provided categories.
- [ ] **Add an optional furniture/equipment variants panel on top of existing Shop views.** Provide reusable behaviour equivalent to a `FurniturePanel`, using `ShopListView`/`ShopItem` plus optional `EquipmentManager`: render unowned, owned, equipped, and optional empty/unequip states; buy an unowned item and equip it after successful purchase; refresh after ownership/equipment changes. Keep visuals prefab-driven through state callbacks or a small view interface so projects can supply their own sprites, badges, preview dimming, lock/check icons, captions, and layout.
- [ ] **Make purchase affordability a reactive Shop view state.** Add an explicit `Unaffordable` state to `ButtonPrice` or a dedicated `ShopPurchaseButtonView`. The owning view should resolve the same currency source as `Shop` (including per-item currency overrides), subscribe while enabled to balance changes, update the state and `Button.interactable` immediately, and unsubscribe safely. Expose a public `Shop.CanAfford(item/id)`-style query instead of duplicating currency resolution in each project. Preserve `Buy`, `Select`, and `Selected` states and cover balance changes, multi-currency items, owned items, failed purchases, rebinding, and enable/disable lifecycle with tests.

## GridSystem

- Add a generic `GridPlacementService` / rule config on top of the current `FieldGenerator` placement API. A good next shape is `GridPlacementRequest` with `RequireEnabled`, `RequireWalkable`, `RequireUnoccupied`, a custom predicate, and overwrite policy, so gameplay services can reuse placement rules without growing many `FieldGenerator` overloads.
- Consider a non-Mono plain C# `DiceBoard` service over `IGridPlacementBoard` or a `FieldGenerator` adapter, leaving the current `DiceBoardService` as the MonoBehaviour wrapper. This would improve testability and allow Dice mechanics outside scenes, but the existing scene API should stay stable.

## See Also

- [Ideas](IDEAS.md)
- [GridSystem](../Assets/Neoxider/Docs/GridSystem/README.md)
