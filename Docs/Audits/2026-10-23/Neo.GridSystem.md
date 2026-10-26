# Neo.GridSystem: audit

Status: source audit and scoped fixes complete; central Unity compilation and test execution remain outstanding.

- Base revision: `ca198556` (package 10.19.0).
- Assignment: `neo-audit-1023-b03-05`.
- Assembly: `Assets/Neoxider/Scripts/GridSystem/Neo.GridSystem.asmdef`.
- Scope: every one of the 30 manifest C# files reviewed; 15 source files changed. `Neo.GridSystem.Dice` and its documentation/tests remain another worker's responsibility.

Проверены все 30 файлов. Исправлены ошибки границ поля, правил и уведомлений; добавлены 38 регрессионных случаев. Проверки Unity не запускались по контракту.

## Scope

- `Assets/Neoxider/Scripts/GridSystem/FieldCell.cs`
- `Assets/Neoxider/Scripts/GridSystem/FieldDebugDrawer.cs`
- `Assets/Neoxider/Scripts/GridSystem/FieldGenerator.cs`
- `Assets/Neoxider/Scripts/GridSystem/FieldGeneratorConfig.cs`
- `Assets/Neoxider/Scripts/GridSystem/FieldGizmoSettings.cs`
- `Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs`
- `Assets/Neoxider/Scripts/GridSystem/FieldSpawner.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridCellMarker.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridGameBuilder.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridPathfinder.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridPlacementEntry.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridPlacementResult.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridPlacementService.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridShapeMask.cs`
- `Assets/Neoxider/Scripts/GridSystem/GridSlotAllocator.cs`
- `Assets/Neoxider/Scripts/GridSystem/Match3/Match3BoardService.cs`
- `Assets/Neoxider/Scripts/GridSystem/Match3/Match3MatchFinder.cs`
- `Assets/Neoxider/Scripts/GridSystem/Match3/Match3TileState.cs`
- `Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeGroupResult.cs`
- `Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeRequest.cs`
- `Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeResolver.cs`
- `Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeResult.cs`
- `Assets/Neoxider/Scripts/GridSystem/MovementRule.cs`
- `Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeBoardService.cs`
- `Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeDirection.cs`
- `Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeResolver.cs`
- `Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeResult.cs`
- `Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeBoardService.cs`
- `Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeCellState.cs`
- `Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeWinChecker.cs`

## Findings and fixes

Evidence below is a source-level reproduction and the regression assertion added for central execution. No runtime reproduction or Unity test pass is claimed.

| ID / severity | Trigger and observable consequence | Source and completed fix | Regression evidence |
|---|---|---|---|
| G01 / High | Generate a 3x2 field, then change `Config.Size` to a larger, smaller or invalid size without regeneration. Lookup/projection/enumeration could index beyond the actual array, hide existing cells or shift world centers. Gameplay scans and slot mappings disagreed with the live cells. | `FieldGenerator.cs:64`: additive `GeneratedSize`, derived from array dimensions. Bounds, projection, anchoring, shape bounds, Gizmos, slots, Match3 and SlidingMerge use it. `GetAllCells` captures its backing array so regeneration between iterator steps cannot replace it with a smaller array underneath the traversal. | `PendingResize_PreservesGeneratedCellsProjectionSlotsAndCenters` (3 sizes), `Regeneration_AppliesPendingSizeAndNormalizesZeroDepth`, `CellEnumeration_KeepsItsArrayWhenTheFieldIsRegeneratedBetweenSteps`, gameplay scan and directed-swap cases. |
| G02 / Medium | Assign `Config = null`, then generate. Previously a one-cell array was created but lookup/enumeration/neighbors could not expose it consistently. | `FieldGenerator.cs:205`: restore a default configuration before generation. | `GenerateField_WithNullConfig_RestoresUsableDefaultConfiguration` checks generated dimensions, enumeration, cell identity and neighbors. |
| G03 / Medium | A 3x1 route has an occupied middle cell and `PassabilityMode = WalkableOnly`. `IsCellPassable` accepted it, but the separate BFS occupancy check rejected the only route. | `GridPathfinder.cs:177`: use the field's passability policy, then the custom predicate. Default occupied endpoints still fail unless occupancy is ignored. | `Pathfinding_UsesConfiguredOccupancyPolicy` tests both policies; the endpoint/ignore/predicate case checks failure reasons and actual route length. |
| G04 / Medium | Pass a null `FieldCell` endpoint to `FindPath`. It was silently converted to `(0,0,0)`, producing a path for an invalid request. | `FieldGenerator.cs:726`: return an empty path for null endpoints. | `NullCellEndpoints_DoNotBecomeAnOriginPath` checks null start, null end and both null. |
| G05 / Medium | A service footprint contains two entries for the same offset. Validation succeeded, the cell was overwritten twice, and duplicate result/event entries were published. The legacy API already rejected this input. | `GridPlacementService.cs:145`: reject duplicate resolved positions before writes. | `DuplicateFootprint_IsRejectedWithoutWritesOrNotifications` checks both public placement paths, unchanged content/occupancy, empty results and zero notifications. |
| G06 / Medium | Read the rest of a multi-cell placement or merge from the first cell callback. Other cells still contained their old values, exposing an intermediate board to UI/gameplay subscribers. | `FieldGenerator.PlaceContentFootprint`, `GridPlacementService.Place`, `GridMergeResolver.cs:77`: finish writes first, then notify. Merge publishes each returned changed cell after the generic resolver returns. Preview and notification suppression remain supported. | `FootprintNotifications_ObserveTheEntireCommittedPlacement` tests both APIs and both notification modes. `MergeNotifications_ObserveTheFinalGroupAndHonorSuppression` checks full board snapshots on every callback. |
| G07 / Medium | Scan the whole board with `RequireEnabled = false`, `RequireWalkable = false`, no explicit seeds, and a matching group made entirely of disabled cells. The group was never considered because seed enumeration always excluded disabled cells. | `Merge/GridMergeResolver.cs:93`: seed enumeration follows `RequireEnabled`. | `Merge_WholeBoardScanHonorsDisabledCellOptInAndPreview` checks the default rejection, opt-in group/result/positions, actual final content and non-mutating preview. |
| G08 / High | Supply `Vector3Int.zero` as a custom TicTacToe win axis while a player mark exists. `CountInDirection` repeatedly inspected the same cell and never terminated, blocking the caller/main thread. | `TicTacToe/TicTacToeWinChecker.cs:68`: ignore zero axes before scanning. | `TicTacToe_ZeroAxesAreIgnoredAndValidAxesStillDetectTheWinner` checks zero-only and mixed axis lists, no false winner and a real winning row. |
| G09 / Medium | A TicTacToe board contains non-walkable or externally occupied cells. Moves could use a non-walkable cell, reset overwrote blocker content, blocked empty cells prevented draw detection, and player-valued blocker content could form a win. Direct calls also depended on `Awake` having populated the cache. | `TicTacToeBoardService.ResetBoard`, `TryMakeMove`, `IsBoardFull` and `TicTacToeWinChecker`: consistently use enabled, walkable, unoccupied playable cells; lazily resolve the required generator. Marks continue to live in `ContentId`; they do not claim external occupancy. | `TicTacToe_BlockersArePreservedAndDoNotPreventTheDraw` and `TicTacToe_BlockersBreakWinLines`, each with both blocker kinds, check retained payload, rejected move, accepted player marks, terminal draw and blocked win lines. |
| G10 / Medium | Configure only downward movement and a board whose only winning swap points downward. Coordinate-order deduplication removed every such neighbor, so search reported no moves even though the move API accepted that direction. | `Match3/Match3BoardService.cs:246`: inspect allowed directed neighbors; skip self-neighbors instead of discarding reverse-coordinate moves. | `Match3_SearchAndResolveHonorsDirectedRulesAndGeneratedDimensions` checks the exact returned orientation, unchanged board after search and at least three cleared tiles after submission, with and without a pending resize. |
| G11 / Medium | Shuffle an unplayable 2x1 board with custom content IDs 11 and 12. After the bounded attempts, fallback generation replaced its content with built-in IDs but returned false without publishing a shuffle. | `Match3BoardService.cs:541`: retain original content, require the fallback to satisfy the same no-current-match/playable conditions, and restore the original board on failure. | `FailedMatch3Shuffle_RestoresCustomContentAndDoesNotPublishAChange` is deterministic: fallback's built-in tile pool cannot accidentally reproduce IDs 11/12. |
| G12 / Low | Call standalone match finding with a zero/negative minimum on an empty board. Empty runs met the threshold and were returned as match groups. | `Match3/Match3MatchFinder.cs:26`: clamp the minimum to one. | `MatchFinder_NonPositiveThresholdNeverProducesEmptyMatchGroups` checks empty output on an empty board and real singleton groups after adding a tile, for both invalid thresholds. |
| G13 / Medium | A prefab array contains a missing/destroyed template. `SpawnAt` reached `Instantiate(null)` rather than its documented failure result. Spawn/query APIs also dereferenced an uninitialized generator cache before `Awake`. | `FieldSpawner.cs:52`, `FieldObjectSpawner.cs:109`: guard null templates and resolve the required local generator lazily. Bulk spawn reuses generated-cell enumeration. | Both spawner variants check null return, no objects/events/occupancy changes. Positive direct-call cases verify tracked instance identity, world position, parent, occupancy, and bulk skip of disabled cells during a pending resize. |
| G14 / Medium | Compile with `ODIN_INSPECTOR`: `FieldGeneratorConfig` referenced `BoxGroup` without importing its namespace. No matching attribute exists in the assembly or its property-attribute dependency. | `FieldGeneratorConfig.cs:5`: add the guarded `Sirenix.OdinInspector` import. | Static namespace inspection only. Compilation with the optional dependency/define is explicitly still required. |
| G15 / Low, performance | On a WxH board, each debug implementation used nested x/y loops and drew both full-axis lines in each iteration: `2*(W+1)*(H+1)` calls. At 100x100 this is 20,402 duplicate line calls per implementation. | `FieldGenerator.DrawFieldGizmos` and `FieldDebugDrawer.OnDrawGizmos`: independent axis loops draw `W+H+2` lines, 202 at 100x100. | Verified loop structure and draw-call arithmetic by inspection. Gizmos require central Editor visual verification; no implementation-mirroring test added. |

Runtime Match3/SlidingMerge Inspector actions now use the existing `Button.PlayModeOnly` guard. This prevents delayed Match3 resolution from trying to start scene gameplay coroutines in Edit Mode. Core grid authoring and component assembly buttons remain edit-safe authoring operations. Button availability requires a central Inspector check.

All serialized field names and existing public signatures are retained. `GeneratedSize` is additive. The size timing, blocker semantics, callback timing, invalid-threshold handling and failed-shuffle rollback are documented behavioral corrections.

## Complete source review ledger

Paths in this ledger are relative to `Assets/Neoxider/Scripts/GridSystem/`; the exact 30 full paths above match the manifest and nearest-asmdef inventory.

| File | Review result |
|---|---|
| `FieldCell.cs` | Reviewed every state field, flags, constructor defaults and runtime payload. Mutable public state is a compatibility constraint for future transactions. |
| `FieldDebugDrawer.cs` | Reviewed cached dependencies, Editor guards, world conversion and drawing loops. Fixed generated bounds and duplicate grid lines. Visual transform/label rendering remains unexecuted. |
| `FieldGenerator.cs` | Reviewed Awake/OnValidate/generation/event flow, singleton destruction/domain-reload reset, shape overrides, all overloads, BFS facade, placement, origin math and Gizmos. Fixed G01/G02/G04/G06/G15. |
| `FieldGeneratorConfig.cs` | Reviewed defaults, origin/passability enums, mask/override precedence, zero depth and optional Odin branch. Fixed G14; movement-rule authoring is discussed in the proposal. |
| `FieldGizmoSettings.cs` | Reviewed all draw flags, colors, fill scale, coordinate/path settings and default collections. No independent defect changed. |
| `FieldObjectSpawner.cs` | Reviewed spawn guards, transform parenting, object/cell registries, copy queries, blocker counts, removal events and lifetime coupling. Fixed G13; generation/external-despawn ownership remains a proposal below. |
| `FieldSpawner.cs` | Reviewed manual/bulk behavior, lazy local dependency, template boundaries, callback payload and instance ownership. Fixed G01/G13 and reused `GetAllCells`. |
| `GridCellMarker.cs` | Reviewed serialized local references, null-safe cell lookup, position setters and explicit binding. It resolves current cells after regeneration; no new fix required. |
| `GridGameBuilder.cs` | Reviewed feature flags, idempotent component additions, required Grid/FieldGenerator and generation order. Existing authoring role retained; no nested assembly additions. |
| `GridPathfinder.cs` | Reviewed all request/result/failure contracts, BFS visited/predecessor reconstruction, directed neighbors, endpoint/ignore/custom checks and start=end. Fixed G03. |
| `GridPlacementEntry.cs` | Reviewed offset/content/occupancy defaults and mutable request payload. Constructor behavior retained. |
| `GridPlacementResult.cs` | Reviewed success/failure output and cell/position collections; failure paths now have explicit duplicate/atomic regressions. |
| `GridPlacementService.cs` | Reviewed constructor guard, null/empty/null-entry/bounds checks, requirement overrides, predicate, overwrite policy and commit/notify ordering. Fixed G05/G06. |
| `GridShapeMask.cs` | Reviewed ScriptableObject data-only ownership, enabled/disabled modes, custom-grid behavior and validation cache reset. Contains no scene object references. |
| `GridSlotAllocator.cs` | Reviewed row-major/reverse mapping, topology/depth restrictions, preference order, occupancy, release/clear and notification options. Fixed generated capacity/mapping in G01. |
| `Match3/Match3BoardService.cs` | Reviewed initialization, non-mutating snapshots, directed swaps, clear/collapse/refill segments, bounded cascade/shuffle, coroutine cancellation and events. Fixed G01/G10/G11 plus Play Mode guards. |
| `Match3/Match3MatchFinder.cs` | Reviewed all layer/row/column scans, blockers, positive tile values, run flushing and invalid minimum. Fixed G01/G12. |
| `Match3/Match3TileState.cs` | Reviewed zero/positive enum values against empty-tile and generation contracts; retained. |
| `Merge/GridMergeGroupResult.cs` | Reviewed seed/result/source/cleared-cell/position outputs and adapter population; retained. |
| `Merge/GridMergeRequest.cs` | Reviewed all rule delegates, seed/direction fallbacks, enabled/walkable/occupied flags, preview, notification suppression, cascade cap and increment factory. Public defaults retained. |
| `Merge/GridMergeResolver.cs` | Reviewed generic-core mapping, whole-board vs explicit seeds, filters, cascades, previews and output/event mapping. Fixed G06/G07; generic core is dependency context only. |
| `Merge/GridMergeResult.cs` | Reviewed collections, cascade metadata and `HasChanges` as a resolved-group indicator, including preview; retained. |
| `MovementRule.cs` | Reviewed every 2D/3D/hex preset, generated 26-neighbor set, explicit direction copy and mutable-list semantics. BFS self/duplicate offsets are bounded by visited state; TicTacToe's custom zero-axis loop was fixed separately. |
| `SlidingMerge/SlidingMergeBoardService.cs` | Reviewed start/clear/slide/spawn/score/full events and wrapper/core separation. Added Play Mode button guards; content/occupancy conventions retained and documented. |
| `SlidingMerge/SlidingMergeDirection.cs` | Reviewed all six axis mappings and invalid-enum zero fallback; retained. |
| `SlidingMerge/SlidingMergeResolver.cs` | Reviewed all axes/signs, blocked segments, one-merge-per-pair processing, custom delegates, dry evaluation, random spawn and move/score output. Fixed generated line dimensions in G01. |
| `SlidingMerge/SlidingMergeResult.cs` | Reviewed step origin/destination/value/merge/result payload and aggregate counts; retained. |
| `TicTacToe/TicTacToeBoardService.cs` | Reviewed before-Awake calls, reset/player progression, rejected/finished moves, winner/draw transitions and external blockers. Fixed G09. |
| `TicTacToe/TicTacToeCellState.cs` | Reviewed empty/X/O integer values and cross-checker/service casts; retained. |
| `TicTacToe/TicTacToeWinChecker.cs` | Reviewed all canonical/custom axes, both scan directions, mark filtering, holes/blockers, zero direction and generated size threshold. Fixed G01/G08/G09. |

Dependency/workflow review: assembly references remain `Neo.PropertyAttribute`, `Neo.Extensions`, `Neo.Merge`. Existing diagnostics are used for invalid generation; no new runtime logs or per-call Unity-version switches were introduced. Runtime sources' `UNITY_EDITOR` branches remain guarded. Scene components retain scene references; the mask asset is reusable data. Views/UnityEvents remain optional consumers of typed runtime APIs.

Reuse receipt: read the `neoxider-tools` skill and relevant game-systems, modules, extensions and spawner reference sections before adding code. Reused `FieldGenerator` cell access/enumeration, existing `GridPathfinder`/placement contracts, the generic `Neo.Merge` core and `Button` guards. No parallel gameplay system, pool, NoCode bridge or new runtime dependency was introduced.

## Documentation

Updated nine existing owned English entry points/guides:

- `Assets/Neoxider/Docs/GridSystem/README.md`: runtime C# example, assembly reference, generated dimensions, transaction notifications, disabled-cell merge opt-in/preview, scene workflow and all sample root layouts.
- `FieldGenerator.md`: generated bounds/size timing, iterator lifetime, null configuration, path policies/null endpoints, placement callback contract and scene regeneration workflow. Existing example now uses an explicit C# array type.
- `GridPlacementService.md`: duplicate rejection, validation atomicity, final-state notifications, pure predicates, suppression and scene-controller integration; explicit example types.
- `GridSlotAllocator.md`: capacity/index mapping follow the generated board until regeneration.
- `Match3/Match3BoardService.md`: directed move search, generated scans, failed-shuffle rollback, invalid finder thresholds, scene setup and coroutine cancellation expectations.
- `SlidingMerge/SlidingMergeBoardService.md`: generated dimensions, content initialization, typed input methods and Play Mode Inspector actions.
- `TicTacToe/TicTacToeBoardService.md`: useful runtime API and scene setup, blocker/mark separation, draw/reset behavior, zero axes and the existing three-dimensional win-length rule.
- `FieldSpawner.md` and `FieldObjectSpawner.md`: concrete C# and scene APIs, failure results, lazy lookup, manual-vs-bulk rules, object lifetime and occupancy ownership.

No nested Dice documentation, shared/root/package guide, sample layout, scene, prefab, asmdef or package configuration was edited by this worker.

## Substantial improvement proposal

### Revision-aware board core and explicit object ownership (proposed; not implemented)

**Problem.** `FieldGenerator` currently combines mutable configuration, live cell storage, shape logic, world conversion, events and scene lifecycle. Pure services still require a MonoBehaviour-backed board. Public `FieldCell` fields permit writes that bypass notifications. `FieldObjectSpawner` keys registries by cell instance but changes occupancy by coordinate: regenerating a board while objects survive leaves old bindings, and later removal can free a replacement cell owned by another object. External destruction/pooling and multiple occupancy writers have no common ownership contract. An automatic despawn/rebind policy would change existing object lifetime and cannot be chosen as a silent audit fix.

**Proposed architecture/public API.** Introduce a plain C# `GridBoard` that owns the existing `FieldCell` instances, generated dimensions, validated topology and a monotonically increasing `GenerationId`. Expose an `IGridBoard` read contract (`Size`, `GetCell`, `GetNeighbors`, enumeration) and typed operations that reuse `GridPlacementRequest` and the current placement/merge result types. Dispatch a `CellsChanged` batch only after an operation commits. Keep world conversion on `FieldGenerator`; add `FieldGenerator.Board` as an adapter entry point. Add board snapshots for deterministic preview/replay and make Match3/SlidingMerge/TicTacToe consume the board contract rather than component lifecycle.

For objects, add an occupancy lease containing board identity, `GenerationId`, cell position and an owner token. Releasing an old lease must never modify a new generation or another owner's claim. `FieldObjectSpawner` owns leases and explicitly selects a regeneration policy (`KeepDetached`, `Despawn`, `RebindIfValid`); start with the current keep-detached behavior for compatibility. Provide an explicit unregister/despawn path for pooling integrations instead of assuming Unity destruction is always observable. Snapshot mutable placement requests before validation so reentrant client callbacks cannot mutate entries mid-operation.

Movement authoring should become explicit data: a serializable preset plus custom offsets, converted to the existing `MovementRule` runtime type. Its present class/list property is a useful runtime API but is not a complete Inspector serialization contract. Rule settings should also validate match thresholds, tile pools and configurable win lengths rather than distribute assumptions through private component fields.

**Compatibility/migration.** Add these contracts without renaming current serialized fields, changing enum values or replacing scene/prefab types. `FieldGenerator.Cells` and legacy `GetCell` keep returning the same live cell instances. Existing callers continue through forwarding APIs; new callers opt into batch operations and leases. Because public fields must remain writable, document an explicit `MarkDirty`/batch-completion path for legacy direct writes instead of claiming that every write can be observed. Preserve current singleton and UnityEvent entry points as scene adapters. Coordinate any new assembly reference with the coordinator and the Dice/Neo.Merge owners.

**Acceptance scenarios.**

1. Generate, place, pathfind, match, slide and check TicTacToe outcomes without constructing GameObjects; repeat the same input/seed sequence and obtain identical snapshots/results.
2. Exercise masks, custom shapes, blockers, one-way/zero/duplicate offsets, invalid dimensions and disabled-cell merge opt-in through shared public contracts.
3. A callback on the first changed cell sees the final batch. Preview emits no changes. Reentrant commands are either queued for the next batch or rejected with a typed result.
4. Regenerate while an old object remains, claim the same coordinate in the new board, then remove the old object: the new claim survives. External despawn/unregister and two independent blockers release only their own leases.
5. Existing serialized scenes/prefabs load without migration loss; current public API regressions and Dice integrations still pass in Unity 6.0 and 6.5+, with optional Odin coverage where installed.

**Effort/risk.** Approximately 6-9 developer days: 2-3 for core/adapters, 2-3 for services and leases, 2-3 for migration/integration tests and documentation. Main risks are mutable legacy fields bypassing transactions, event ordering/reentrancy, external pooling lifecycle and accidental scene serialization changes. Deliver the core adapter and leases separately before migrating gameplay services.

## Verification and remaining risks

**Checks actually run:**

- Exact manifest/nearest-asmdef inventory comparison: **30/30 C# files**, Dice excluded.
- Strict UTF-8 decoding and replacement-character scan: **42 owned source/doc/test/meta/report files**; passed. Russian text was not rewritten based on console rendering.
- Static scans of **31 C# files** (inventory plus new regression): no `var` declarations, retired instance-ID APIs or raw `Debug.Log` calls; conditional compilation directives and lexical delimiters balanced. This is not a C# compilation result.
- Local Markdown links in all **9 updated guides** resolve.
- New regression source/meta have no trailing whitespace and end with a newline. The new metadata GUID `46edac6fbb1140aa93beb17e2f069bd5` has exactly one match under `Assets`.
- Existing `Neo.Editor.Tests` references already include `Neo.GridSystem` and `Neo.Merge`; no reference/configuration edits needed.
- `git diff --check -- <the 30 inventory paths, 9 owned guides, new test/meta, own report>`: exit **0**. Untracked test/meta whitespace was checked separately because ordinary Git diff excludes untracked content.
- Reviewed scoped final source diffs and all newly written regression assertions. No commits, child agents, Unity/MCP, compilers, test/build runners or installs were used.

**Regression deliverable:** `Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023NeoGridSystemRegressionTests.cs` and its unique `.meta`: **22 test methods / 38 cases**, authored but **not executed**. Tests observe actual paths, content, occupancy, output groups/positions, object bindings, event snapshots and terminal round state. Fixture Random state is seeded and restored; no reflection or source-text assertions are used.

**Coordinator actions / unverified risks:**

- Compile the integrated changes in Unity 6.0-6.4 and 6.5+, plus the `ODIN_INSPECTOR` configuration where the dependency is installed. Neither branch was compiled in this audit.
- Run the 38 new cases and existing GridSystem, Neo.Merge and Dice suites against the shared final sources; generic merge behavior is a cross-assembly dependency and is being audited independently.
- Check Match3/SlidingMerge button availability and large-grid Gizmos in the Editor; verify delayed Match3 disable/re-enable behavior in Play Mode. No console, Play Mode or visual result was collected here.
- `FieldObjectSpawner` generation/external-despawn/competing-occupancy ownership remains the documented lifetime limitation addressed by the proposal. Remove tracked instances before regeneration with the current API.
- Delayed Match3 cancellation retains the partial resolve state; applications requiring rollback must reinitialize. Public state/delegates remain main-thread, mutation-aware contracts rather than a concurrency-safe board API.
