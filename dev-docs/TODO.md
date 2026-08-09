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

## Package-wide: split logic cores out of MonoBehaviours

Motivation, from consuming the package in Tropic Mania: several modules could not be reused because
the rules only exist inside a MonoBehaviour that also owns DOTween tweens, `Image`/`TMP_Text`
references, `UnityEvent`s and its own save keys. A project needing the same rules in an engine-free
assembly (deterministic EditMode tests, integer-cents money, injected randomness and clock) has to
reimplement them. `DiceBoard`/`DiceBoardService` above is the pattern to follow: plain C# core with
C# events, MonoBehaviour kept as a thin wrapper with an unchanged scene API.

- [ ] **Long-term principle: logic lives in plain C#, MonoBehaviours are only wrappers.** Nearly all
      game logic should live in plain C# classes (testable cores with C# events and injected
      randomness/clock); `MonoBehaviour` stays as controller/presenter/scene wrapper only. Gradually
      extract logic out of the existing fat components instead of adding new logic into them.
- [ ] **Continue decomposing the god-objects.** `RpgCharacter` is now ~1535 lines after extracting the
      plain-C# `RpgCharacterProfileService`, `RpgCharacterResourceService`, and optional
      `RpgCharacterNetworkAdapter`. Resource dictionaries, mutations, reactive queries, derived max/regen,
      pause windows, and regen clocks now live in the resource service; continue splitting stat/effect and
      progression responsibilities. Other large components include `SpinController` (~2000),
      `Selector` (~1900), `AnimationFly` (~1500), and `InteractiveObject` (~1400; query math, target
      contract, and camera resolver already extracted).
- [ ] **`Bonus/Collection/Collection`** — extract a non-Mono set/item core. Today it is a `Singleton`
      over a flat `bool[]` with its own save prefix and no notion of independent sets that are claimed
      and reset separately, so a project with three sets writes its own.
- [ ] **`Bonus/Collection/Box`** — extract the accumulate/threshold/claim rules. They currently live in
      a MonoBehaviour with `float progress`, DOTween and UI fields; a durable piggy bank in integer
      cents with run ids and idempotent claim receipts cannot use it.
- [ ] **`Bonus/TimeReward/CooldownReward`** — extract the cooldown rule out of `TimerObject`. A cooldown
      that must survive a restart and resist clock tampering belongs in plain C# driven by an injected
      clock, not in a component with `UnityEvent`s and its own save key.
- [ ] **`Tools/Dialogue/DialogueController`** — models only a linear Dialogue→Monolog→Sentence tree with
      a typewriter. A pooled-lines case (pick by tag, never repeat back to back, first line in one time
      window and the rest in another, contextual line pre-empting idle) has no overlap and had to be
      written from scratch.
- [ ] **`Tools/Random/ChanceManager`** — already non-Mono and already injectable via `randomProvider`,
      which is the right shape. It is float-weighted only, so a consumer expressing chances as integers
      (per-million jackpot odds) cannot use it without losing exactness. Add an integer weight path.
- [ ] **`Extensions/RandomExtensions.Shuffle`** — static over `UnityEngine.Random`, so it cannot be
      injected and breaks deterministic tests. Add an overload taking a random source.
- [ ] **Add a clock abstraction.** There is no `ITimeSource`/`IClock`: `Tools/Time/Timer` reads
      `Time.deltaTime` directly and `DateTimeExtensions` only takes `nowUtc` as a parameter. Anything
      needing testable pacing or durable cooldowns defines its own.
- [ ] **`Save/ISaveProvider` — add `long` support.** Only int/float/string/bool exist, and
      `SaveProviderExtensions` adds just `int[]`/`float[]`. Every consumer storing money in cents writes
      `long` through `string` with `CultureInfo.InvariantCulture` — seven near-identical private helpers
      in a single project.
- [ ] **`Audio/AMSettings` — persist mute, and stop force-unmuting on start.** `Start()` resolves the
      mixer groups and then unconditionally calls `SetEfx(true); SetMusic(true)`, while only volumes are
      saved. A consumer holding its own mute flag cannot apply it in `Awake` (groups are still null, so
      `SetMusic` no-ops) and is then overwritten, so the player's "sound off" returns on every launch
      unless the consumer re-applies a frame later.

## See Also

- [Ideas](IDEAS.md)
- [GridSystem](../Assets/Neoxider/Docs/GridSystem/README.md)
