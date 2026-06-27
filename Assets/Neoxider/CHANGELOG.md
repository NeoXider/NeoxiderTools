
## [Unreleased]

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
