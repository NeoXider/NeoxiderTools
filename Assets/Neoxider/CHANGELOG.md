
## [Unreleased]

### Added
- **Inspector mascot as a live "slime linter".** The banner slime now reflects the inspected component's health: a remembered console-error count (attributed to the component type by parsing stack traces, kept for the session) plus a cheap cached scan for missing object references and NaN/Infinity float fields. Faces: neutral/blink when healthy, worried on missing references, angry on errors/invalid numbers, a brief "surprised" reaction the moment a new error appears (auto-opening a compact issue list with a **Clear** action), and a "watching" face in Play Mode. The spectrum half-frame matches the mood (amber shimmer / red pulse) and flows faster in a healthy Play Mode.
- **Click the slime = solo.** Poking the mascot collapses every other component on the GameObject (backing up their expanded state); poke again to restore. The issue-count badge on the chip opens/closes the problem list.
- Performance: the console hook is O(1) per error (dedup + type cap), the validation scan runs only for inspected objects, is throttled (~2 s) and property-capped, and remembered errors persist via `SessionState` (survive domain reloads, reset with the editor).

## [10.0.1] - 2026-07-18

Patch release: three audit-fix cycles over the whole package — 52 independently verified correctness bugs fixed, plus a consistency pass over asmdefs, docs and package metadata.

### Fixed
- **Abilities**: nested event-driven effects no longer corrupt shared scratch lists (area damage + thorns-style reactions now hit every target; stacked reactive modifiers all fire; shields survive re-entrant damage). Team override survives `UnitTemplate.ApplyTo`; piercing projectiles hit distinct units instead of re-hitting the first; per-hit RNG is decorrelated; spawn ops fall back to the cast point instead of world origin; play-mode teardown no longer resurrects the system hub; unrealized projectile casts expire instead of leaking; caster grants are order-independent; `max_health_bonus` / `max_mana_bonus` now actually resize resource pools.
- **Core/Level**: `GetXpToNextLevel` off-by-one on all curve types; `HealthComponent.Load` no longer clamped by `MaxDecreaseAmount` (and no longer fires death events while loading); `Increase()` on unlimited pools no longer computes negative headroom; `TextLevel` resubscribes after disable/enable; removed per-frame forced reactive sync; `SetLevel` overflow guard; weighted-random zero-weight fallback; `StringExtension.Truncate` small-length guard.
- **Tools**: `Timer` stop/start async race; `Spawner.Clear` double-release and stale delayed-destroy handles; `KeyboardMover` fixed-update compensation; `SwipeController` swapped start/end positions; `TimerObject` RealTime mode persists `isActive`; `MouseInputManager` picks the nearest hit; mouse movers guard `deltaTime == 0`; pooled-component cache and additive-scene pool lifetime leaks.
- **Save/Quest/Audio/Shop/Network**: global `Save()` no longer deletes data of fields with `autoSaveOnQuit` off; file saves flush on pause/focus loss (mobile); `QuestManager` tolerates registry mutation from completion handlers and migrates saved states when objectives are added; music track timer is pause-aware and unscaled; networked wallet rate limit is per-connection; `GlobalSave` initializes on first launch; `NetworkReactiveSync` validates the reactive property type; saveable behaviours persist on disable.
- **Bonus/Cards/NoCode/UI**: slot line evaluation only pays contiguous runs; `SetRewardAvailableNow` makes rewards claimable immediately; `SpinController` window-size bounds; `DeckModel.Draw` preserves duplicate-card order; `NoCodeFormattedText` retries late-spawned sources; `LineRoulett` honors its serialized slow-down time; `FakeLoad` static state resets between plays.
- **Survivor demo**: enemies freeze during the level-up pause; upgrade-granted projectile abilities get archetypes; pooled templates are cleaned across reloads; Shop demo guards a missing wallet.
- TMP smeared-text fix follow-up: obsolete `enableWordWrapping` replaced in sample UI.

### Changed
- 30 new regression tests (EditMode total 889).
- Dead asmdef references removed (`Neo.Tools`, `Neo.Network.Core`, an old Odin GUID); PlayMode test assembly renamed to `Neo.Tests.PlayMode` to avoid consumer name collisions; deprecated `com.unity.textmeshpro` dependency dropped (TMP ships in `com.unity.ugui` on Unity 6).
- Docs: pages matched to real APIs (SaveProvider static facade, StatePredicate family, StateMachineEvaluationContext, QuestStatus values), hidden-sample paths corrected, ~120 generator-artifact rows purged from 61 pages, `[NeoDoc]` added to the last 5 undocumented components (+ new AuraWeapon page).
- Inspector polish: the property block is a real rounded card (accent tint + 1px edge); the segmented left rainbow line is replaced by a continuous HSV spectrum half-frame hugging the card (rounded corners, fading arms, seamless animated hue); the banner mascot is drawn as a close-up that nearly fills its chip.

## [10.0.0] - 2026-07-18

Major release. Headline: a new data-driven combat core (`Neo.Abilities`) that supersedes `Neo.Rpg`, a redesigned inspector, and a modular survivor demo built on the new system.

### Added
- **`Neo.Abilities` module — a Dota-derived, data-driven ability/modifier system.** Author new abilities entirely in data (ScriptableObjects), no code required. Pure-C# deterministic domain plus Unity wrappers:
  - Units with teams and resource pools (reuses `Neo.Core.Resources.ResourcePoolModel`), and an **open property registry** aggregated `base -> +Add -> xMul -> Max`.
  - **Modifiers** unify buffs/debuffs/DoTs/auras/shields/stuns: typed property contributions, boolean states (any-true-wins), interval ticks with guaranteed expiry, stack policies (Independent/Refresh/Stack + per-stack scaling), and declarative event reactions (e.g. absorb-on-take-damage).
  - **Cast pipeline** with cost/cooldown/charges/targeting/team-filter/range validation, instant or homing-projectile delivery, area effects, and an open effect-op registry (damage/heal/apply-modifier/remove-modifier/dispel/resource/spawn) plus motion/utility atoms: **knockback, pull, teleport** (routed through the `IAbilityWorldAdapter.TryMoveUnit` seam so navigation/physics stay pluggable), **execute** (health-fraction damage over missing/max/current HP), and **chain** (deterministic nearest-first bounces with per-hop falloff).
  - **Leveled values and specials**: every effect amount is a `LeveledValue` (per-level array plus property scaling from caster or target, driven by ability or unit level); named `Specials` on an `AbilityDefinition` are reusable Dota-style `%value%` entries referenced by key from any effect node.
  - **Live combat properties** read from the property registry at damage time by `DamageService`: `crit_chance` / `crit_multiplier`, `lifesteal_percent`, and physical-only `evasion_chance` — modifiers change them mid-fight with no extra wiring.
  - **Receipt-driven**: everything observable flows through one event bus; casts carry deterministic seeds and serializable ids (authority-ready seams for a future Mirror bridge).
  - Authoring assets: `AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`, `AbilityLibrary`. Scene components: `AbilitySystemBehaviour`, `AbilityUnitBehaviour`, `AbilityCasterBehaviour`, `AbilityProjectileBehaviour`.
  - **UI Toolkit "Ability Designer"** window (`Tools -> Neoxider -> Ability Designer`) plus SO inspectors.
  - Docs under `Docs/Abilities/` and 93 EditMode tests.
  - `Neo.Rpg` is **superseded by `Neo.Abilities`** and slated for removal in a later release.
- **Modular Survivor demo** (`Samples/Demo/Scenes/SurvivorDemo.unity`) — a complete Vampire-Survivors-style game assembled entirely from a single `SurvivorConfig` data asset on top of `Neo.Abilities` + Core level/resource systems (waves, auto-cast weapons, XP, level-up upgrade cards, escalating difficulty). Swap the data to clip a different game. Bright uGUI, no IMGUI.
- **Demo-shell scenes** — eight module demos rebuilt as bright, self-explaining uGUI scenes on a shared `NeoDemoShell` frame (procedural sprites, header, content card, action log; zero imported assets): Audio, Save, Settings, LevelFlow, StateMachine, NoCode binding, Parallax, and Quest.
- `AbilitySystemBehaviour.Paused` to freeze the ability tick for menus/level-up screens.
- `AbilityUnitBehaviour.SetTemplate` / `SetTeamOverride` for runtime/pooled unit spawning.
- `ResourcePoolModel.SetCurrent(id, value)` — a direct clamped setter (loads/revives/scripted adjustments) that bypasses the heal gate.

### Changed
- **Redesigned the Neoxider custom inspector** with a modern theme (`NeoInspectorTheme`): gradient hero banner, themed rounded section cards with accent rails, gradient action buttons, a readable version pill, a themed property-panel card, and a new avatar logo with an occasional eye-blink. Dark and light editor skins supported. All functionality preserved; still IMGUI.
- **Package-wide comment cleanup**: comments reduced to XML `<summary>` docs plus `WHY:` / `TODO:` / `HACK:` markers, all in English; banner/section dividers and comments restating the code removed across runtime and editor sources.

### Fixed
- **Re-imported the TMP essential resources** shipped with the project (`Assets/TextMesh Pro`): the stale `LiberationSans SDF` font asset and materials rendered smeared/blurry SDF text in Unity 6; demo scenes now show crisp text.
- `PoolManager` threw a `NullReferenceException` when created at runtime (its preconfigured-pools list was null) — now null-guarded, so a runtime-instantiated `PoolManager` works.
- `AbilitySystem.Revive` could not restore HP after a lethal-damage death (the resource pool's heal-from-zero gate); it now sets health directly and never revives at 0 HP.
- `UnitTemplate` pool regeneration never fired (missing regen interval); templates with `RegenPerSecond > 0` now tick.

## [9.13.1] - 2026-07-17

### Fixed
- **Shop / purchase failure event never fired when out of money (solo mode):** `Shop.Buy` resolved the wallet through `IMoneySpendAuthority.CanConfirmSpendNow`, which returns false for *both* insufficient funds and pending-server-confirmation. Insufficient funds were misread as "awaiting server authority", so `Buy` returned silently and `OnPurchaseFailed` / `OnPurchaseFailedId` never fired for a non-networked wallet. Now affordability is checked first (via `IMoneyCanSpend`): a shortfall reports as a normal failed purchase (and, on a networked client, no longer sends a doomed spend command to the server), while a genuine pending-server case still short-circuits without a failure event. Found by the real Unity Test Runner (735-test suite), not the standalone compile check.
- **Shop / EquipmentManager required a visual slot to equip:** `Equip` / `Unequip` bailed out when a category had no `CategorySlot`, so state-only equipment (driven by `ShopVariantsPanel` or headless logic) silently did nothing. Equipment state is now always tracked, persisted, and broadcast; the `CategorySlot` is optional and only drives the sprite target. Persisted slotless categories are restored on load from the item catalog. `Unequip` on an already-empty slotless category is a no-op (no spurious `OnEquipChanged`).

### Tests
- `EquipmentManagerTests`: slotless equip/unequip state tracking + event, and the empty-category no-op. The existing `ShopAffordabilityTests.Buy_InsufficientFunds_FiresFailedAndDoesNotOwn` now guards the Money regression. Fixed two of the new 9.11.0 tests that only surfaced under the real runner (a grid default-content assumption and an editor-folder scan in `ModulePrinciplesTests`).

## [9.13.0] - 2026-07-17

### Added
- **Bonus / one-call economy spin:** `SpinController.StartEconomySpin()` builds the whole outcome from the assigned `SlotEconomyDefinition` (weighted pick per cell honoring the per-machine overrides, then the special/wild conversion along each active payline), queues it via `ForceNextOutcome`, and starts the spin. The building blocks are public: `BuildEconomyOutcomeMatrix()` (+ a deterministic `Func<int>`-picker overload for tests/replays/server outcomes) and `EvaluateActivePaylinesWithEconomy()` returning one `LineResult` per active payline of the settled grid. Before this the economy asset could only be wired by hand-rolling the pick → special-rule → force-outcome → evaluate chain in game code.

### Tests
- EditMode: `SpinControllerEconomyTests` — weighted fill, empty-economy guard, deterministic special-line conversion on the active payline (off-line cells untouched), and loss reporting on an unsettled grid.

## [9.12.1] - 2026-07-17

### Fixed
- **Tools / SpineController:** the `[NeoDoc]` attribute was declared twice on the class. `NeoDocAttribute` does not allow multiples, so any project with Spine installed (`SPINE_UNITY` defined) failed to compile with CS0579; without Spine the file is compiled out, which is why the error stayed invisible.

### Editor tooling
- **PackageHealthCheck** (`Tools → Neoxider → Package Health Check`) now also catches the two doc-drift classes that path checking alone could never see: (1) public, non-abstract `MonoBehaviour`/`ScriptableObject` types in `Neo.*` runtime assemblies that carry no `[NeoDoc]` attribute at all (the 9.8.2 audit found 39 such gaps by hand), and (2) dead relative `.md` links inside `Docs/` (URL-encoded paths supported; six dead links shipped in 9.8.1 alone). Obsolete types and editor/test/demo assemblies are excluded.

## [9.12.0] - 2026-07-17

### Added
- **GridSystem/Dice / plain C# `DiceBoard` core:** all dice placement/merge logic moved out of the scene component into `Neo.GridSystem.Dice.DiceBoard` — constructible over any generated `FieldGenerator` (tests, server/replay logic, custom loops), with C# events `BoardChanged`/`MergesResolved`. `DiceBoardService` keeps the identical scene API, forwards Inspector settings into the core (exposed via the new `Board` property), and re-raises the core events as its UnityEvents. Closes the remaining GridSystem TODO item.
- **Shop / `ShopListViewCategoryBar` auto categories:** `Build Categories From Shop` fills the bar from the Shop catalog on enable — one entry per distinct `ShopItemData.Category` (first-seen order) with an optional show-all entry (`Include All Entry` / `All Entry Name`); `BuildCategoriesFromShop()` re-runs it after catalog swaps.

### Tests
- EditMode: `DiceBoardCoreTests` (placement, occupied-cell rejection, merge with content cap, single-notification contract, clear, service→core settings/event forwarding), `ShopListViewCategoryBarTests` (selection drives the list view, auto-built categories with and without the All entry).

## [9.11.0] - 2026-07-17

### Added
- **Bonus / per-machine slot weights (`SlotSymbolWeightOverrides`):** `SpinController` now takes an optional `SlotEconomyDefinition` (`Economy`) plus a local weight table layered over it — enable the override to change drop weights for one machine without touching the shared asset. Entries match symbols by id (reordering/extending the definition's symbol list is safe; unmatched symbols fall back to their definition weight), weight `0` disables a symbol, negatives clamp to `0`. New `PickEconomySymbolId()` picks through the override; Inspector `⋮` menu **Normalize Weights** rescales all positive local weights to a total of `1`. `SlotEconomyDefinition.PickWeightedId` gained weight-selector and deterministic-roll overloads for tests/replays/server outcomes.
- **UI / `CategoryBar` + `CategoryBarItem`:** reusable horizontal/tab category bar that owns selection state — initial selection, select by index/id, `Next`/`Prev` with configurable wrap, disabled entries, Inspector-authored or runtime `SetCategories(...)` lists. Item views are authored children or spawned from a prefab; the shared selection marker is re-parented onto the selected item with an offset, never resizing or repositioning authored graphics. Reports through `OnCategorySelected(int)` / `OnCategoryIdSelected(string)` and has no Shop dependency; the optional `ShopListViewCategoryBar` adapter (in `Neo.Shop`) drives a `ShopListView` from the bar.
- **Shop / reactive affordability:** public `Shop.CanAfford(item/id)` — owned and free items are always affordable; priced items query the same wallet the purchase would use (per-item `Currency Override Save Key` included). New `Shop.ResolveCurrencyMoney(itemId)` exposes that wallet for balance subscriptions; new optional `IMoneyCanSpend` interface lets custom wallets answer affordability (`Money` implements it). `ButtonPrice` gained an explicit `Unaffordable` state (optional visual group, label, `OnUnaffordable` event, `CurrentType` accessor) — old prefabs keep showing the Buy visuals. New `ShopPurchaseButtonView` subscribes to shop refreshes and wallet balance while enabled, drives `ButtonPrice` (Buy/Select/Selected/Unaffordable) and `Button.interactable` immediately, and re-subscribes on slot rebinding; it unsubscribes safely on disable.
- **Shop / `ShopVariantsPanel` + `ShopVariantStateView`:** furniture/equipment variants panel over `ShopListView`/`ShopItem` with optional `EquipmentManager`: renders unowned/owned/equipped per slot through the small `IShopVariantView` interface (visuals stay prefab-driven), equips after successful purchase, forwards Shop selection into the equipment manager, refreshes on ownership/equipment/list changes, and supports an empty/unequip control (`Unequip()`). `ShopListView` exposes `Views` and `ButtonAction`.
- **GridSystem / `GridPlacementService` + `GridPlacementRequest`:** plain-C# rule-driven placement over the `FieldGenerator` placement API — `RequireEnabled`/`RequireWalkable`/`RequireUnoccupied`, custom `CellPredicate`, `GridOverwritePolicy` (Reject/Overwrite), `Notify` toggle, single-cell factory `GridPlacementRequest.Single(...)`, atomic multi-cell writes with readable failure reasons.

### Fixed
- **Shop / currency resolution before `Start`:** `Shop` now lazily resolves its default wallet on first use, so `CanAfford`/`Buy` called before `Start` (e.g. from a view's `OnEnable`) use the configured `moneySpendSource` instead of falling back to `Money.I`.

### Repo
- Removed accidentally committed dev debris from version control (`TestRunner.cs`, `test_extensions*.cs`, `debug.log`, `memory.db`, `msp_server.log`, `replay_pid*.log`) and extended `.gitignore` so it cannot return.

### Docs
- New pages: `UI/CategoryBar.md`, `Shop/ShopListViewCategoryBar.md`, `Shop/ShopPurchaseButtonView.md`, `Shop/ShopVariantsPanel.md`, `GridSystem/GridPlacementService.md`; updated `SlotEconomyDefinition.md`, `SpinController.md`, `ButtonPrice.md`, `Shop.md`, and the Shop/UI/GridSystem READMEs.

### Tests
- EditMode coverage: `SlotSymbolWeightOverridesTests` (disabled override, reordered/changed symbol lists, zero/negative weights, normalization, deterministic weighted selection), `CategoryBarTests` (initial/runtime selection, wrap and non-wrap navigation, disabled entries, runtime category lists, events), `ShopAffordabilityTests` (balance changes, multi-currency wallets, owned/free items, failed purchases, `ButtonPrice` state rules, `ShopPurchaseButtonView` subscription/rebinding/lifecycle), `ShopVariantsPanelTests` (state rendering, buy-then-equip, failed purchase, unequip, `EquipmentManager` bridge), `GridPlacementServiceTests` (rule toggles, predicate, overwrite policy, atomic footprints, notifications).

## [9.10.0] - 2026-07-16

### Added
- **Cards / `CardSpriteNameParser`** (runtime, `Neo.Cards`): parses card sprite/file names into suit, rank, card back, or joker. Understands English and Russian tokens (`ace_of_spades`, `дама_червы`), numeric ranks 2–14 (`hearts_02`, `spades_14`), compact forms (`AS`, `KH`, `10c`), and common separators. `GetCanonicalName(suit, rank)` returns the recommended file name (`hearts_02` … `spades_14`), so the same convention works for editor auto-fill and runtime sprite loading.
- **Cards / DeckConfig inspector auto-fill:** new "Auto-Fill From Folder..." button assigns all four suit lists, the back sprite, and both jokers from sprite names in a selected `Assets/` folder (multi-sprite sheets supported). Suit slots are cleared first, so the folder is the source of truth; unrecognized and conflicting names are reported in a summary dialog and the console.

### Changed
- **Cards / DeckConfig validation:** a missing back sprite is now a warning instead of an error — the deck generates and face sprites resolve normally; only face-down display is unavailable.

### Fixed
- **Cards / DeckConfigEditor deck-type casts:** `DeckType` was cast from `enumValueIndex` (0/1/2) instead of the stored enum value (36/52/54), so a `Standard36` sprite deck was previewed and validated as 13 cards per suit instead of 9, and the 54-card joker requirement never triggered from the correct enum member.

### Tests
- EditMode coverage for `CardSpriteNameParser`: standard/compact/Russian names, back and joker detection, invalid names, and canonical-name formatting (also verified standalone against .NET with 37 passing cases).

## [9.9.0] - 2026-07-15

### Added
- **Tools / TimerObject:** public `Tick(float deltaTime)` advances a timer deterministically through the same active-state, pause, time-scale, update-interval, event, milestone, completion, and looping pipeline used by Unity's frame update. This supports tests, replay/server clocks, and custom update loops without reflection or duplicate timer logic.
- **UI / AnimationFly request overrides:** individual flights can now override `Duration`, `SpeedMultiplier`, and `DelayBetweenItems`, copy their rendered UI size from a `RectTransform` with `UiSizeSource` (or use an explicit `UiSize`), and tween from `ScaleMultiplier` to `EndScaleMultiplier` with an optional `ScaleEase`.

### Fixed
- **Tools / TimerObject inheritance:** Unity lifecycle hooks (`Awake`, `OnEnable`, `Update`, `OnDisable`, and `OnValidate`) are now `protected virtual`. Derived timers such as `CooldownReward` reliably inherit the frame update in player builds and may extend lifecycle behavior by overriding and calling `base`. Previously the private base `Update` could leave a derived countdown frozen at `00:00`.
- **Bonus / CooldownReward:** validation now overrides and chains to `TimerObject.OnValidate`, preserving both cooldown-specific synchronization and base timer validation.
- **UI / AnimationFly pooling:** pooled visuals now restore their original scale, rotation, and `RectTransform.sizeDelta` before reuse, preventing size or scale compounding across repeated reward flights.
- **Bonus / CooldownReward runtime creation:** dynamically added components now initialize their completion event before subscribing, avoiding a null event when configured entirely from code.

### Tests
- Added deterministic EditMode coverage for `TimerObject.Tick`, API-contract coverage for all inheritable lifecycle hooks, and a PlayMode regression proving that `CooldownReward` advances through the inherited Unity update without a project-side driver.
- Added EditMode and PlayMode coverage for request-level UI sizing, duration/speed overrides, end-scale tweening, arrival ordering, and pooling-safe visual reuse.

## [9.8.2] - 2026-07-03

### Fixed
- **Missing `[NeoDoc]` attributes:** 32 public `MonoBehaviour`/`ScriptableObject` types (e.g. `LevelComponent`, `ShopItemData`, `RpgAttackDefinition`, `StatusEffectDefinition`, `SaveProviderSettings`, `InteractionRayProvider`, and 26 others) had a matching `.md` page in `Docs/` but no `[NeoDoc(...)]` attribute pointing at it — the Inspector showed "No documentation linked" even though the page existed. All 32 fixed. `PackageHealthCheck` only verified that *existing* `[NeoDoc]` paths resolve; it didn't catch a component missing the attribute entirely — this class of bug was invisible to automation until inspected manually.
- **Duplicate auto-generated stub docs:** 4 `.md` pages (`Cards/Config/DeckConfig.md`, `Core/Level/Data/LevelCurveDefinition.md`, `Rpg/Data/RpgAttackDefinition.md`, `Rpg/Data/RpgAttackPreset.md`) were low-quality auto-generated field dumps (including bogus "fields" like literal `true`/`100f` picked up by whatever tool scaffolded them) duplicating a better hand-written page elsewhere for the same class. Deleted; `[NeoDoc]` now points at the real page. Fixed a doc link in `Core/README.md` left dangling by the deletion.
- **7 more classes had zero doc coverage** (no `[NeoDoc]`, no matching page): `GridCellMarker`, `NoCodeFloatBindingBehaviour` (+ its embedded `ComponentFloatBinding`), `InventoryDatabase`, `InventoryInitialStateData`, `InventoryItemStateBehaviour`, `PageId`, `NeoDebugOverlay`. Wrote real pages for all 7 and linked them.

### Notes
- 21 of the 32 fixed classes above still only have an auto-generated field-dump page as their *only* doc (no hand-written alternative existed to fall back on) — the link now resolves and something real is shown, but the prose quality is low. Flagged as follow-up debt, not rewritten in this pass to keep scope bounded.

## [9.8.1] - 2026-07-03

### Added
- **`link.xml`** — preserves `Neo.*` assemblies and `Assembly-CSharp` from IL2CPP managed code stripping. `NeoCondition`, `ComponentFloatBinding`, `[SaveField]`, `NetworkPropertySync`, and `NetworkReactiveSync` all resolve members by name via reflection (including private, non-serialized fields) — under IL2CPP that member can be legally stripped if nothing else references it, silently breaking the NoCode binding in a release build while working fine in the Editor. See `Docs/IL2CPP.md` for the full explanation and the escape hatch for custom asmdef setups.
- **`THIRD-PARTY-NOTICES.md`** — lists every optional/required third-party dependency (UniTask, DOTween, Mirror, Spine, Odin, MarkdownRenderer), why it's referenced, and where to find its license. None of them are bundled inside the package.

### Changed
- **Minimum Unity version raised to `6000.0`** (was `2022.1`). The package is now developed and validated against Unity 6 only; projects on Unity 2022 LTS should stay on the last `9.7.x` release.

### Fixed
- **`Docs/Tools/Spawner/Spawner.md`** described deny zones (`_denyAreas`/`_denyAreas2D`, `IsPositionAllowed`) as a "Planned (TODO)" feature — they shipped in `9.7.0`. Doc rewritten to match the current API (also documents `Spawn Area`, `Max Waves`, `Spawn On Awake`, `Parent Transform`, which were undocumented).
- **`Docs/StateMachine/README.md`** had a malformed module table (missing separator row, a stray bullet line instead of the README's own entry) that broke rendering.
- Six dead internal doc links fixed (`Bonus/Slot/*` cross-links and `GridSystem/Dice/DiceBoardService.md` → `Merge/README.md`), left over from the RU→EN docs folder reorganization.
- **`Samples~/NeoxiderPages/Runtime/API/UIKitAPI.cs`** — removed unconditional `Debug.Log` calls on every `G.Pause/GoMenu/Start/Restart/Win/Lose/End` call. This is a reference-implementation sample meant to be copied into real projects; the logging was leftover debug instrumentation with no gate, so every game using the pattern verbatim would spam the console on every state transition.

## [9.8.0] - 2026-07-03

### Changed
- **Docs are English-only.** The `Docs/` (RU) tree has been removed and `DocsEn/` renamed to `Docs/`; `[NeoDoc(...)]` attributes and all package/README links now point at the single English tree. `PackageHealthCheck` was rewritten to verify every `[NeoDoc]` path resolves under `Docs/` instead of checking RU/EN parity.
- **README rewritten** around four audiences: NoCode beginners, professional C# API users, multiplayer, and AI-agent development. Added `Docs/NoCode/GettingStarted.md`, a beginner-facing tour of every no-code building block.
- `DOCUMENTATION.md` / `DOCUMENTATION_GUIDELINES.md` rewritten in English (they previously mandated Russian for `.md` pages).
- Removed the root `README_RU.md`; the repo now ships a single English README.
- Filled in genuinely missing `Network/*` documentation pages (`NeoNetworkComponent`, `NetworkSingleton`, `NeoNetworkManager`, `NetworkOwnerFilter`, `NetworkActionRelay`, `NetworkPropertySync`, `NoCode_Network_Spec`, `Lobby`, `Multiplayer_Guide`) and `NeoxiderPages/PM.md`, which were English placeholder stubs.

### Fixed
- **Save / FileSaveProvider:** removed the GC finalizer that called `Save()` (and therefore `JsonUtility`, a main-thread-only Unity API) from the finalizer thread.
- **Save / SaveManager:** `OnApplicationQuit` now calls `SaveProvider.Save()` after writing, so file-backed providers actually flush to disk on quit instead of relying on the removed finalizer.
- **Shop.cs:** translated two legacy-field tooltips from Russian to English.
- **Cards/Editor/DeckConfigEditor.cs:** fixed a mojibake character in a HelpBox warning string.
- **StateMachine.ChangeState:** `OnExit` now runs on the previous state before `CurrentState` is reassigned, so exit-handlers and re-entrant `ChangeState` calls see consistent state.
- **Network/NeoNetworkComponent.cs:** removed a stray Russian word left in an XML doc comment (last remaining Cyrillic string in the runtime/editor code).

### Tests
- No behavior changes to test coverage this release; full EditMode (631 tests) and PlayMode (106 tests) suites verified green after the docs/audit pass.

## [9.7.1] - 2026-07-02

### Fixed
- **Naming:** new 9.7.0 public serialized fields renamed to PascalCase per package convention (`ShopCategorySelector.Category`: `Id/DisplayName/Icon`; `SlotEconomyDefinition.Symbol`: `Name/Id/MoneyReward/BonusReward/IsSpecial/Weight`; `EquipmentManager.CategorySlot`: `CategoryId/SpriteTarget/ImageTarget/ApplyNativeSize/DefaultItemId`). `[FormerlySerializedAs]` keeps existing scene/asset data intact.
- **Tests:** `NetworkRateLimitTests` no longer triggers Mirror's "requires a NetworkIdentity" `OnValidate` error — the probe object now carries a `NetworkIdentity`.

### Tests
- Edit-mode coverage for the 9.7.0 additions: `SlotEconomyDefinition` (payline evaluation, special-line conversion, weighted picker), `EquipmentManager` (equip/unequip/toggle, category replacement, slot visuals), `ShopCategorySelector` (wrap-around cycling, select-by-id, empty-list safety), `Spawner` deny zones (3D/2D rejection, null entries).

## [9.7.0] - 2026-07-02

### Added
- **Shop / ShopCategorySelector:** NoCode category pill with prev/next arrows cycling a serialized category list into `ShopListView.SetCategory` — complements `ShopCategoryButton` for shops browsed sequentially (pattern extracted from a shipped dress-up game).
- **Shop / Equipment (new):** `EquipmentManager` + `EquipItemDefinition` — multi-category dress-up/skins: one item per category, sprite applied to a `SpriteRenderer`/`Image` slot (optional `SetNativeSize`), worn set persisted via `SaveProvider`, `OnEquipChanged` event, `EquipById/Unequip/ToggleById` NoCode API. Pairs with `Shop` ownership for buy-then-wear flows.
- **Bonus / SlotEconomyDefinition:** slot-machine economy SO — weighted symbol table (money/bonus payouts, special flag), `PickWeightedId()`, `ApplySpecialRule()` (one special converts the payline) and `EvaluateLine()` returning a typed `LineResult`. Removes the per-game hand-rolled economy layer over `SpinController`.
- **Bonus / ResourceRegen:** one-component regenerating resource — couples `CooldownReward` (auto-claim forced on) with a capped `Money` wallet and an optional `TimeToText` countdown (shows 0 while full).
- **Network / NetworkReactiveSync:** NoCode replication for `ReactivePropertyFloat/Int/Bool` — inspector counterpart of `NetworkReactivePropertyBridge`; multiplayer wallets/score/HP without hand-written SyncVar code. Inert without Mirror.
- **Network / NetworkPlayerName:** replicated player nickname (trimmed + length-capped server-side, rate-limited command, `OnNameChanged` for TMP labels). Works locally without Mirror.
- **Network / NeoNetworkDiscovery Quick Play:** `QuickPlay()` — one-button LAN flow: auto-join the first server found, or host after `Host If None Found After` seconds; `OnQuickPlayResolved(bool becameHost)`.
- **Network / NetworkEventDispatcher payloads:** `DispatchGlobalInt/Float/String` + matching UnityEvents (rate-limited, authority-checked like the parameterless event).
- **Network / NeoNetworkComponent:** per-connection `RateLimitCheck(sender)` overload — one spamming client no longer starves other clients' commands on shared scene objects (used by `NetworkEventDispatcher`).
- **Network / NetworkPropertySync:** `Skip Hook On Owner` option — the owner ignores the server echo of its own values in `OwnerToServer` mode (prevents rubber-banding).
- **Network / NeoNetworkManager:** inspector toggles for gated `NetworkDiagnostics` runtime logs/warnings (NoCode network debugging).
- **Tools / Spawner deny zones:** `_denyAreas`/`_denyAreas2D` + `Max Rejection Tries` + `IsPositionAllowed(Vector3)` — random points inside deny zones are re-rolled (closes the long-standing in-code TODO documented in 9.5.1).
- **Pages (sample):** `PM.ChangePageByName` now falls back to the page GameObject name and lists known PageId names in its error; `UIPage` gained an inspector `Open` button.
- **Editor / Package Health Check** (`Tools → Neoxider → Package Health Check`): verifies package version parity (package.json ↔ README ↔ PROJECT_SUMMARY ↔ CHANGELOG) and Docs/DocsEn parity — both drifts have shipped before.

### Tests
- `CooldownReward` auto-claim re-arm (covers the 9.6.1 fix), `Money` soft cap (`Add` clamps / `AddOverflow` ignores / 0 = unlimited), network command rate limit.

### Performance
- **Network / NeoMirrorSceneReactivator:** walks scene roots instead of `Resources.FindObjectsOfTypeAll` (no longer touches prefab assets on every scene load).

### Docs
- RU+EN pages for every new component; "Lobby on Neo.Pages" recipe in `Multiplayer_Guide`; deprecated types now have an explicit removal target (10.0); `ReactiveProperty` performance/naming notes; missing `Cookbook.md` metas restored.

## [9.6.2] - 2026-07-02

### Fixed
- **Network / NetworkEventDispatcher:** `CmdDispatchEvent` is now rate-limited (`RateLimitCheck`), closing a spam-amplification hole — any client could flood the global RPC broadcast (the command is `requiresAuthority = false` with default authority `None` by design).
- **Network / NetworkPropertySync:** `Sync Interval` gained a `[Min(0.1)]` floor. An interval below the server rate limit (0.05 s) caused silent Cmd drops in `OwnerToServer` mode: the owner marked the value as sent while every client stayed stuck on the stale value until the next change. Also: a missing target/field no longer poisons the reflection cache — a target assigned later at runtime is picked up (warning logged once).
- **Reactive / ReactiveProperty:** `NotifySubscribers` now takes a real snapshot of code listeners (reusable buffer, no per-notify allocation). Previously removing an earlier listener inside a callback shifted indices and the next listener silently skipped that notification. New edit-mode test covers the case.
- **Network / NetworkSingleton:** `IsInitialized` now actually reflects initialization instead of duplicating `HasInstance`.
- **Network / NeoNetworkManager:** one-time warning when Mirror's private `NetworkIdentity.hasSpawned` field is missing (Mirror upgrade guard) instead of silent scene-player-template degradation.

### Docs
- RU/EN pages updated for the fixes: `NetworkPropertySync` (interval floor + owner rubber-band caveat), `NeoNetworkComponent` (rate limit is per object, not per client), `ReactiveProperty` (snapshot semantics, main-thread only), `NetworkEventDispatcher` (RU; command rate limit).

## [9.6.1] - 2026-07-02

### Fixed
- **Bonus / CooldownReward + Tools / TimerObject:** continuous auto-claim (`_autoClaim`) now re-arms after each grant. Previously the underlying non-looping timer deactivated itself right after the completion event, so auto-claim fired once and `RemainingTime` stopped ticking. `TimerObject` now deactivates a non-looping timer **before** invoking `OnTimerCompleted`, so completion handlers may restart it with `Play()`; `CooldownReward.TakeReward()` restarts the cooldown timer (mirroring `RestartTime()`) and resets the availability flag so `OnRewardAvailable` fires on every cycle.

### Docs
- Synchronized version references in `README.md` and `PROJECT_SUMMARY.md` (were still `9.5.2`).

## [9.6.0] - 2026-06-27

### Added
- **Shop / Money:** optional soft cap `_maxMoney` (0 = unlimited). `Add()` and `SetMoney()` now clamp to it; new `AddOverflow(float)` deposits **ignoring** the cap for bonus/overflow rewards allowed to exceed it. Enables capped resources (energy / stamina / lives) without custom code. Runtime get/set via `MaxMoney`.
- **Bonus / CooldownReward:** `_autoClaim` option — automatically claims the reward the moment it becomes available (continuous regen) without manual `TakeReward()` / event wiring. Stays decoupled from wallets (no `Money` dependency); deposit via `OnRewardClaimed → Money.Add(...)`. Capped-regen recipe: `_autoClaim` + `OnRewardClaimed → Money.Add(1)` + `Money._maxMoney` cap (+ `AddOverflow` for sources allowed past the cap). Runtime get/set via `AutoClaim`, `CooldownSeconds`, `MaxRewardsPerTake`.

### Docs
- **Cookbook (new):** added a cross-module recipes page (RU + EN) consolidating end-to-end examples — capped energy + auto-regen, capped currency, daily reward, slot → wallet, shop buy/equip, reward fly — linked from the docs index.
- **Shop / Money:** documented `_maxMoney` and `AddOverflow`. **Bonus / CooldownReward:** documented `_autoClaim` and the capped-regen recipe.

## [9.5.2] - 2026-06-27

### Fixed
- **Samples / Demo Scenes / Network:** imported Demo Scenes now compile in projects without the optional Mirror package. `TestStart` follows the `Neo.Network` optional-Mirror pattern: Mirror `NetworkBehaviour`/`Command`/`ClientRpc` code is compiled only under `MIRROR`, while non-Mirror projects get a local solo-mode fallback.

### Docs
- **Package docs:** synchronized package version references and sample import paths for `9.5.2`.

## [9.5.1] - 2026-06-27

### Docs
- **Tools / Spawner:** documented planned **deny zones** (areas where spawning is forbidden) as a TODO — `_denyAreas`/`_denyAreas2D` + `_maxRejectionTries` reject candidates inside a deny zone and re-roll, plus `IsPositionAllowed(Vector3)`. "Where allowed" = spawn points / spawn area; "where forbidden" is planned. (#3)

## [9.5.0] - 2026-06-27

### Changed
- **Tools / Spawner:** the single `_spawnTransform` spawn point is now a `Transform[] _spawnPoints` array. Empty → spawn from the spawner's own transform (previous default); with one or more points, a random non-null point is picked per spawn (position and rotation stay consistent for that spawn). New public `ResolveSpawnPoint()`. ⚠ **Breaking:** scenes that referenced the old single `Spawn Transform` field lose that reference on upgrade and fall back to the spawner's own transform — re-assign points in `Spawn Points`. (#3)

## [9.4.0] - 2026-06-25

### Added
- **Extensions / Random:** `GetRandomWeighted<T>(items, weights)` and `GetRandomWeighted<T>(items, weightSelector)` — return the weighted random **element** directly instead of just the index (`GetRandomWeightedIndex`).
- **Extensions / Dictionary:** new `DictionaryExtensions` with `GetOrCreate(key)`, `GetOrCreate(key, factory)`, and `Increment(key, amount = 1)` for `int`/`float` counters — replaces the repeated get-or-create and `dict[k] = dict.GetValueOrDefault(k) + 1` boilerplate. (`GetValueOrDefault` is intentionally left to the BCL to avoid overload ambiguity.)
- **Extensions / Primitive (Math):** `Snap(step)` (float/int), `Wrap(min, max)` (negative-safe cyclic index), `PingPong(length)` (int), and `Approximately(b)` / `Approximately(b, tolerance)` float-equality helpers.

## [9.3.0] - 2026-06-23

### Added
- **Audio / AM:** `Play(AudioClip)` and `PlayMusicByClip(AudioClip)` convenience overloads; `SetMusicVolume`/`SetEfxVolume` (replacing the boolean-trap `SetVolume(volume, bool)`); `startVolumeEfx`/`startVolumeMusic` renamed to PascalCase with `[Obsolete]` forwarders.
- **Bonus / Slot:** `SpinController.SpinResult` value type with `GetLastResult()` and `LastPayout` — one coherent spin outcome (symbol grid, winning lines, payout).
- **Tools / Debug:** `NeoDebugOverlay` — drop-in IMGUI runtime overlay (FPS, active scene, time scale, AM/SaveManager status), toggled with F3.
- **Tests:** `AmEditModeTests`, `SpinControllerPaylineTests`, and a public-PascalCase convention check in `ModulePrinciplesTests`.
- **Docs:** EN parity for Quest and Tools, bilingual Getting Started, and a NeoDoc link checker that now verifies the DocsEn mirror.
- **GridSystem / Dice:** added serializable `DiceValueWeight` and `DicePieceGenerator.GenerateWeighted(...)` for designer-controlled dice value pools, explicit invalid-weight validation, and non-duplicating weighted pairs.
- **GridSystem:** expanded `GridSlotAllocator` with `Capacity`, `HasAvailableSlot`, slot-index preferred allocation, slot-index release, and `Clear(...)` for reusable autobattler benches, tactical rows, backpack rails, and compact board lifecycle management.
- **GridSystem:** extended `GridSlotAllocator` with linear slot-index helpers (`TryGetSlotPosition`, `TryGetSlotIndex`, `IsAvailable(int)`, `Allocate(int, int)`) for rectangular 2D boards such as autobattler benches, tactical rows, hotbars, and card-game lanes.
- **Cards:** added optional finite `HandModel.Capacity`, `RemainingCapacity`, `IsFull`, `TryAdd(...)`, and `AddRangeUntilFull(...)` so CCG hands, autobattler benches, backpack rails, and market rows can reject overflow explicitly while unlimited hands remain the default.
- **GridSystem / Dice:** added `DicePieceGenerator.CreateD6Pool()` and `CreateSequentialPool(minValue, maxValue)` for classic dice rolls and custom numbered pools without duplicating pool construction in games.
- **GridSystem:** added `GridSlotAllocator` for ordered one-cell slot allocation on top of `FieldGenerator`, covering benches, tactical rows, autobattler boards, hotbars, and market rows without duplicating occupancy checks.
- **UI / AnimationFly:** added reusable motion presets for typed requests (`Arc`, `Fountain`, `Magnet`, `FountainMagnet`, `Scatter`) with burst and magnet tuning fields, plus PlayMode coverage for fountain trajectory and deterministic fountain+magnet rewards.
- **Samples / UI:** added an `AnimationFlyDemo` scene with runtime buttons, a real sample sprite asset example, and labeled sliders for count, duration, delay, arc, scale, and rotation so fly-effect flows can be inspected without manual scene editing.

### Fixed
- **Editor scene-dirtying:** `ParallaxLayer`, `CameraAspectRatioScaler`, and `AM` no longer mark the open scene dirty on load (perpetual `*`): removed unconditional `SetDirty` in editor delay-calls, made preview generation transient (`HideAndDontSave`), and drive the aspect-ratio camera only at runtime.
- **Runtime performance:** `RpgProjectile` and `MagneticField` now use NonAlloc physics queries + reused buffers instead of per-frame heap allocations; `Drawer` releases its owned/cloned Materials in `OnDestroy`; `InteractiveObject` caches colliders and reuses the cached camera; `Singleton.I` no longer re-runs `FindObjectsByType` on every access when no instance exists.
- **Cards (async lifetime):** `DrunkardGame`, `BoardComponent`, `DeckComponent`, `HandComponent`, `HandView`, and `CardView` bind UniTask awaits to `GetCancellationTokenOnDestroy` and link tweens to the GameObject, preventing `MissingReferenceException` on scene change mid-animation.
- **Audio / AM:** `OnDestroy` now stops `RandomMusicController` so its looping UniTask cannot run after teardown.
- **Bonus / Slot:** aligned slot element scene gizmo coordinate labels with the `SpinController` console grid index base and guarded `VisualSlotLines` against missing line references.
- **Tools / Compatibility:** switched `MouseInputManager`, `MouseEffect`, `ParallaxLayer`, and `NetworkContextActionRelay` debug IDs to `Object.GetEntityId()` under `UNITY_6000_5_OR_NEWER` (Unity 6.5+), while preserving `GetInstanceID()` for older versions.
- **Samples / NeoxiderPages:** removed hard DOTween/DOTween Pro runtime dependencies from `UIPage` and `BtnChangePage`, stripped legacy `DOTweenAnimation` components from sample prefabs, and declared `com.unity.ugui` as a package dependency so imported page prefabs resolve standard uGUI scripts.
- **Samples / NeoxiderPages:** fixed `UIPageEditor` null-reference spam after removing the legacy `_animation` field and cleaned stale `_animation` serialized references from sample page prefabs.
- **Samples / NeoxiderPages:** hardened `UIPageEditor` against missing serialized fields so partial imports or stale sample objects render warnings instead of throwing inspector `NullReferenceException`s.
- **Samples / NeoxiderPages:** removed dangling prefab component references from `_Page base` and `Shop Page` so Unity no longer reports corrupt prefab imports after the legacy tween cleanup.

## [9.2.0] - 2026-06-04

### Added
- **GridSystem:** added `FieldGenerator.TryGetCellPositionFromWorld`, `TrySnapWorldToCellCenter`, and `SnapWorldToCellCenter` so grid drag/drop and preview snapping can use the generator's own origin-aware nearest-cell conversion API.
- **GridSystem:** added reusable `GridPlacementEntry`, `GridPlacementResult`, `FieldGenerator.CanPlaceContentFootprint`, and `FieldGenerator.PlaceContentFootprint` for writing multi-cell pieces/items/shapes into grid cells.
- **GridSystem / Merge:** added `GridMergeRequest.Increment(...)` factory preset so the common "merge equal content into content+step at the seed" rule no longer needs ~10 delegates wired by hand, plus a `NotifyOnContentChanged` toggle so callers can apply extra state before notifying.
- **Merge:** added `MergeRequest.MaxCascadeIterations` and `MergeResult.CascadeLimitReached` (mirrored on `GridMergeResult`) so the cascade safety limit is configurable and surfaced instead of stopping silently.
- **GridSystem / Dice:** exposed `DiceBoardService.MinMergeGroupSize`, `MergeStep`, `MaxContentId`, and `RequireWalkable` so dice merge rules are tunable without editing the service; `DicePiece` now exposes `CellCount`.
- **Samples / Dice:** added a `Dice.prefab` visual used by the Dice Merge demo instead of constructing dice visuals in code.
- **Docs:** added RU/EN API pages for `GridPlacementEntry` and `GridPlacementResult`, placement examples in `FieldGenerator` docs, and current TODO/Ideas notes for GridSystem placement follow-ups.
- **Tests:** added EditMode coverage for cascade-limit flagging, multi-cell `DicePiece` rotation, single consistent merge notifications, single `OnBoardChanged` per merging placement, and configurable merge step/cap.

### Changed
- **GridSystem / Dice:** `DicePiece.RotateClockwise`/`RotateCounterClockwise` now rotate footprints of any size around the anchor (not just pairs), and the demo controller enumerates orientations / allows rotation for any multi-cell piece.

### Fixed
- **Build / Runtime:** guarded runtime assembly references to `UnityEditor` APIs in prefab previews and button drawers so player builds do not pull editor-only namespaces; added an architecture test for unguarded runtime `UnityEditor` usage.
- **Save:** changed `SaveManager.Save()` to read-modify-write the shared `SaveData_All` container so data for unloaded scene objects is preserved; added EditMode coverage for absent component entries, multiple preserved unloaded entries, and current-component load round-trip.
- **Bonus / Slot:** split paid and free spin payment logic so positive-price spins require an `IMoneySpend` wallet while zero-price spins remain explicitly free; added EditMode coverage for paid/free payment paths.
- **Shop / Money:** added `MoneySpendResult` / `IMoneySpendWithResult` / `IMoneySpendAuthority` so callers can distinguish confirmed, rejected, and pending server-authority spends; `Shop` no longer performs wallet-only server spend requests for pending item/bundle purchases without a matching server-side grant path; added EditMode coverage for detailed spend statuses and pending authority item/bundle purchases.
- **Core / Level & Resources:** fixed XP-backed `SetLevel` threshold sync, `LevelNoCodeAction` false level-up events, duplicate `OnDeath` dispatch, custom resource depleted events, and regen-from-zero contracts; added targeted EditMode coverage.
- **RPG:** fixed buff stack application/projection/snapshot persistence, required-target presets spending resources before target resolution, hit dedup/target resolution for arbitrary `IRpgCombatReceiver` implementations, and reusable projectile initialization state; added targeted EditMode coverage.
- **Samples / Package Validation:** removed a sample-only page-switch component reference from the package `ButtonPageSwitch` prefab, restored the active `DemoLevelCurve` validation asset, made sample scene coverage resolve `Samples~` scene files by filesystem path/YAML, and made PlayMode sample smoke skip hidden `Samples~` runtime scenes that Unity cannot compile directly.
- **Tools / Move:** made `FreeFlyCameraController` movement tests deterministic by pinning external look input when verifying local-forward movement.
- **UI / AnimationFly:** added a typed request/result API, sprite-only fly visuals, built-in disable-and-pool completion, reward timing callbacks, and parent-local Canvas coordinate conversion so world-to-UI rewards spawn in UI while visually starting at the world pickup position; pooled fly visuals now kill/link tweens and reset base transform state before reuse; added EditMode/PlayMode coverage for coordinate, reward timing, and pooled scale contracts.
- **Cards:** fixed duplicate-card removal by index so `HandModel`, `HandPresenter`, and `HandComponent` remove the same indexed card/presenter/component instead of the first equal card data; `HandComponent` now removes only its own card click listeners, `DeckComponent` detaches old model events on reinitialize, and card views own/kill hover tweens; added EditMode coverage for duplicate card ordering and lifecycle contracts.
- **NoCode / Lifecycle:** exposed explicit editor/runtime refresh and subsystem reset hooks used by EditMode validation across NoCode bindings, Save, MouseInputManager, and SwipeController.
- **GridSystem / Dice:** fixed double `OnCellStateChanged` notifications with stale `IsOccupied` during merges — the resolver no longer notifies mid-mutation; `DiceBoardService` applies occupancy and raises one fully-consistent notification per cell.
- **GridSystem / Dice:** fixed `DiceBoardService.Place` raising `OnBoardChanged` twice on a merging placement; placement and follow-up merges now raise it exactly once.
- **GridSystem / Merge:** stopped allocating a full board cell list on every `GridMergeResolver.Resolve` when explicit seeds are supplied.
- **Samples / Dice:** fixed drag preview/drop behavior so the tray dice is hidden while dragging, the preview can be offset above the pointer, optional snap preview uses `FieldGenerator`, final release uses the current snapped/nearest grid cell, and placed dice keep the prefab world scale instead of shrinking under scaled cell prefabs.
- **Samples / Dice:** fixed placed dice visuals disappearing after drag/drop by syncing from `DiceBoardService.OnBoardChanged`, resolving missing demo view references within the view's own hierarchy (no global `FindObjectOfType` that could bind to another board), rebuilding missing cell views before board refresh, keeping board dice under a dedicated `DicePlacedPiecesView` root, refreshing placed visuals after drag preview cleanup, and reusing placed die views across refreshes instead of destroying/recreating them every frame.
- **Samples / Dice:** made the demo empty-content fallback consistent with `DiceBoardService.EmptyContentId` (-1) and cached the fallback solid sprite instead of allocating a new `Sprite` per cell/die.
- **Samples / Dice:** fixed placed dice being destroyed on mouse release. Two reinforcing fixes: (1) the view's visual roots are now deterministic — cached and never recovered via `transform.Find` (which, with play-mode deferred `Destroy` and runtime roots persisted in the saved scene, could return a stale/duplicate root and orphan placed dice), and the view rebuilds cleanly instead of adopting persisted scene cells; (2) on a successful drop the dragged preview dice are now *promoted* into their destination cells and reused as the persistent placed visuals (registered before the model mutates, so `OnCellStateChanged`/merges reuse the same objects), and the preview is only destroyed on a failed drop.
- **GridSystem / Dice:** guarded Dice placement and demo previews against missing `DicePiece.Cells` data so startup/drag/drop cannot throw when a piece is empty or partially initialized.
- **GridSystem / Merge:** hardened `GridMergeResult`, `GridMergeGroupResult`, and `DicePlacementResult` collections as read-only-reference lists so callers cannot reassign result buffers.
- **Docs:** updated `PROJECT_SUMMARY.md` as a compact module/reuse map and linked it more prominently from the main README entry points.

## [9.1.0] - 2026-06-02

### Added
- **Merge:** added `Neo.Merge`, a standalone pure C# connected-group merge engine for grids, inventories, lists, graphs, and custom board-like mechanics.
- **GridSystem:** added `GridMergeResolver` adapter for applying generic merge rules to `FieldGenerator` / `FieldCell.ContentId`.
- **GridSystem / Dice:** added `Neo.GridSystem.Dice` with dice pieces, pool-based piece generation, dice placement, and dice merge resolution.
- **Samples:** added a playable 5x5 Dice Merge demo scene using the Dice sprites under `Assets/Neoxider/Sprites/Dice`.
- **Tests:** added EditMode coverage for generic merge, GridMerge, Dice mechanics, combined Dice/GridMerge behavior, and PlayMode smoke coverage for the Dice demo.

## [9.0.0] - 2026-05-26

### Added
- **Diagnostics:** added `NeoDiagnostics` in the shared `Neo.Extensions` assembly as the package logging gate. Info and warning output is disabled by default, errors remain visible, throttled warnings are supported, and static state resets under domain-reload-disabled play mode.
- **Tests:** added EditMode coverage for `NeoDiagnostics` and architecture coverage that keeps aligned runtime roots from reintroducing raw `Debug.Log*` calls.
- **Samples:** added required smoke scenes for Audio, Level, Network, NoCode, Parallax, Save, Settings, and StateMachine under the active development samples root. Each scene has a `ModuleDemoSceneInfo` marker and minimal module wrapper setup.
- **Tests:** added sample scene coverage that opens required smoke scenes, checks `ModuleDemoSceneInfo`, and verifies missing scripts. Sample validation now supports both active development `Samples` and release/UPM `Samples~` roots.

### Changed
- **Package samples:** aligned `displayName` to `NeoxiderTools` so Unity imports samples under `Assets/Samples/NeoxiderTools/<version>/<sample>`. Validation still accepts the legacy `Assets/Samples/Neoxider Tools` root for already-imported samples.
- **Cards / Bonus / UI / Tools Move:** moved remaining direct runtime logs in the aligned roots behind `NeoDiagnostics` or explicit component debug flags. `AnimationFly` and `UniversalRotator` now cache their `Camera.main` fallback instead of resolving it on every conversion/aim call.
- **Parallax / Tools Input:** added explicit camera injection APIs and throttled/optional `Camera.main` fallback paths for `ParallaxLayer` and `MouseInputManager`, with RU/EN docs and EditMode coverage.
- **Samples:** routed demo setup feedback through one sample diagnostics helper instead of direct setup-script `Debug.Log` calls.
- **Docs:** documented the current `Samples` development path, the `Samples~` UPM source path, and Unity's imported `Assets/Samples/NeoxiderTools/<version>/<sample>` path in `AGENTS.md`, Samples docs, package compatibility docs, and README navigation.
- **Docs / GridSystem:** restored the top-level RU/EN GridSystem docs to readable UTF-8 and documented its constructor role for Match3, TicTacToe, 2048-like SlidingMerge, pathfinding, views, and spawners.

## [8.6.0] - 2026-05-25

### Added
- **Tools / Move:** added `FreeFlyCameraController`, a modular Unity Scene View style free-flight controller for debug/spectator cameras. RMB gates look and flight by default, with configurable keys, speed modifiers, cursor lock snapshot/restore, external input overrides, tests, and RU/EN documentation.
- **Tools / View:** added `SelectorModel`, a plain C# selection-rule class for reusing Selector behavior outside MonoBehaviour while keeping `Selector` as the backwards-compatible scene wrapper.
- **Tests:** added targeted EditMode/smoke coverage for Audio, Parallax, PropertyAttribute, Level scene flow, Settings defaults, Progression save/load, Quest static reset, StateMachine lifecycle, Rpg combat edges, UI navigation, and Tools/Move free-fly behavior.
- **Cards:** expanded custom-card support so card views, boards, deck configs, and runtime models can be reused for non-standard games such as TCG/deckbuilder layouts instead of only classic 36/52/54-card decks.

### Changed
- **Package version:** bumped `com.neoxider.tools` from `8.5.8` to `8.6.0` and synchronized root/package README badges, docs indexes, project summary, compatibility notes, and changelog.
- **Docs:** normalized the main RU docs entry and package compatibility page to readable UTF-8, removed stale planning/backlog docs from navigation, kept one canonical module entry per module, and aligned the English docs with current module behavior.
- **Tools docs:** clarified runtime, editor-only, deprecated/compatibility, feature-doc, and maintenance-doc zones directly in the Tools README so new feature docs are not mixed with old plans.
- **Gameplay ownership:** removed the docs-only `Gameplay` module boundary; gameplay docs now route through concrete runtime modules.
- **NoCode / StateMachine:** clarified the split between testable C# runtime contracts and scene-only inspector wrappers. ScriptableObjects store data/slots, not direct scene object references.
- **Network / Save / Bonus / Cards / StateMachine / UI / Tools:** gated or reduced runtime log noise, keeping diagnostics behind explicit debug/fallback settings where practical.
- **Singleton/static lifecycle:** added or verified domain-reload-disabled reset paths for Quest, Progression, Save, Network, Bootstrap, MouseInputManager, and SwipeController flows.

### Fixed
- **UI:** removed legacy `UIReady` runtime/docs references and migrated package sample usage to `SceneFlowController`. `Assets/Scenes/AutoSaves` backups are intentionally left untouched.
- **Bonus:** cleaned legacy TimeReward/WheelFortune/Slot compatibility issues and documentation around them.
- **GridSystem / Samples:** kept sample/docs paths aligned after `Samples~` cleanup.
- **Shop:** continued migration from integer-facing API toward stable typed/string item APIs ahead of v9.
- **Save:** checked missing `SaveProviderSettings` fallback behavior while gating runtime logs.

## Legacy History

Entries before `8.6.0` were removed from the package changelog during UTF-8 cleanup because the stored text was mojibake/corrupted. Use git history or release tags for exact older notes.
