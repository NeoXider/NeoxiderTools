
## [Unreleased]

### Added
- **GridSystem:** added `FieldGenerator.TryGetCellPositionFromWorld`, `TrySnapWorldToCellCenter`, and `SnapWorldToCellCenter` so grid drag/drop and preview snapping can use the generator's own origin-aware nearest-cell conversion API.
- **GridSystem:** added reusable `GridPlacementEntry`, `GridPlacementResult`, `FieldGenerator.CanPlaceContentFootprint`, and `FieldGenerator.PlaceContentFootprint` for writing multi-cell pieces/items/shapes into grid cells.
- **Samples / Dice:** added a `Dice.prefab` visual used by the Dice Merge demo instead of constructing dice visuals in code.
- **Docs:** added RU/EN API pages for `GridPlacementEntry` and `GridPlacementResult`, placement examples in `FieldGenerator` docs, and current TODO/Ideas notes for GridSystem placement follow-ups.

### Fixed
- **Samples / Dice:** fixed drag preview/drop behavior so the tray dice is hidden while dragging, the preview can be offset above the pointer, optional snap preview uses `FieldGenerator`, final release uses the current snapped/nearest grid cell, and placed dice keep the prefab world scale instead of shrinking under scaled cell prefabs.
- **Samples / Dice:** fixed placed dice visuals disappearing after drag/drop by syncing the demo board view from `DiceBoardService.OnBoardChanged` and refreshing board visuals after successful placement.
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
