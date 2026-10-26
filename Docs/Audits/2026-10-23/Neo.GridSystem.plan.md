# Neo.GridSystem: implementation plan

Phase 2, same audit session. Planning only; none of the work below has been implemented during this phase. Baseline and completed findings: [Neo.GridSystem.md](Neo.GridSystem.md).

Path prefixes: `S = Assets/Neoxider/Scripts/GridSystem/`, `D = Assets/Neoxider/Docs/GridSystem/`, `E = Assets/Neoxider/Tests/Edit/GridSystem/`, `P = Assets/Neoxider/Tests/Play/`. Filenames below are exact relative to these prefixes.

## 1. Completed outcome

All 30 manifest sources were reviewed; 15 sources and nine English guides changed. G01-G15 already cover generated dimensions, null configuration/endpoints, occupancy-aware paths, duplicate footprints, committed-state placement/merge notifications, disabled-cell merge opt-in, TicTacToe zero axes/blockers, directed Match3 search, failed-shuffle rollback, invalid match thresholds, spawner guards, Odin imports and duplicate Gizmos lines. Runtime Inspector buttons use existing Play Mode guards. These fixes are not pending proposals.

`E/Audit20261023NeoGridSystemRegressionTests.cs` contains 22 methods/38 cases. Inventory, UTF-8, metadata uniqueness, Markdown links, lexical/preprocessor consistency and scoped whitespace checks passed. Compilation, Unity tests, Play Mode and visual checks were not executed. Current relevant source, test, documentation changes and dependency boundaries were reread for this plan.

## 2. Remaining fixes and verification debt

**P0 — integration verification; coordinator owns execution.** After source-changing audits settle, compile Unity 6.0-6.4 and 6.5+, without Odin and with installed Odin/`ODIN_INSPECTOR`. Run the 38 cases, existing GridSystem tests, generic Merge tests and Dice integration tests. Preserve actual content, path, occupancy, callback and score assertions; fix demonstrated failures rather than changing expectations to match implementation. Inspect 100x100 Gizmos, runtime button availability and scene reload/domain-reload-disabled singleton behavior. No version branch is considered compiled from static inspection.

**P1 — confirmed stale-cell occupancy writes; Neo.GridSystem owns correction.** `S/FieldObjectSpawner.cs:180` retains `info.Cell` but line 195 frees by coordinate. Reproduce: spawn A, regenerate, spawn B at the same coordinate, remove A; B's replacement cell becomes free. A related path publishes `OnObjectSpawned` at line 124 before setting occupancy at line 127; a listener can regenerate between those operations. Smallest correction: revalidate registered-cell identity before every field occupancy write, including after callbacks; retire detached metadata without changing replacement cells. Preserve object destruction and existing event ordering. Update `D/FieldObjectSpawner.md` and add `P/Audit20261023NeoGridSystemLifecycleTests.cs`. Acceptance: B remains occupied/tracked, no replacement-cell freed notification occurs, and regeneration inside the spawn callback does not occupy the new field. This does not implement automatic rebinding/despawning.

**P1 — lifecycle verification, not a confirmed new defect.** `S/Match3/Match3BoardService.cs:89` cancels delayed resolution and leaves partial content. Documented behavior currently permits this. In the same Play fixture, disable during clear/collapse/refill, re-enable, call `InitializeBoard`, and submit a known valid move. Observe recovered content, accepted input and terminal phase notifications. Rollback/resume semantics need an explicit gameplay contract before implementation.

`Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` currently lacks `Neo.GridSystem`. Coordinator must add that reference before the new Play fixture is introduced. Existing EditMode references already suffice.

## 3. Substantial improvement: board core and generation-scoped ownership

**P2 proposal.** Programmers need placement, topology and previews without GameObjects; current services depend on `FieldGenerator`, and boolean occupancy cannot identify an owner. Implement incrementally rather than migrating every gameplay wrapper together.

Reuse `FieldCell`, `MovementRule`, mask coordinates, placement requests/results and `Neo.Merge.MergeResolver`. Existing `SpawnUtility`, `PoolManager` and `IPoolable` cover pooling; do not recreate them or add a mandatory pooling dependency.

**First increment:** new `S/GridOccupancyLease.cs` and `S/GridOccupancyService.cs`; update `S/FieldGenerator.cs` and `S/FieldObjectSpawner.cs`. Add `FieldGenerator.GenerationId`, incremented whenever storage is replaced or invalidated. Proposed C# API: `TryAcquire(position, owner, out lease)`, `Release(lease)` and `SetExternalOccupancy(position, occupied)`. Tokens bind board identity, generation, coordinate and owner. Release is idempotent; stale tokens cannot affect another generation, and one owner cannot release another owner's claim. Multiple tracked blockers keep a cell occupied until the final claim is released. Direct legacy `IsOccupied` assignments remain outside ownership guarantees.

Add `FieldObjectSpawner.Unregister(GameObject)` for callers that subsequently invoke existing pooling/despawn APIs; retain `RemoveObject` as unregister plus destruction. Keep current detached-object lifetime as default. Automatic despawn/rebind policies remain separate opt-in proposals.

**Second increment:** new `S/IGridBoard.cs`, `S/GridBoard.cs`, `S/GridBoardDefinition.cs`, `S/GridBoardSnapshot.cs`, `S/GridCellChangeBatch.cs`. `GridBoard` is ordinary C# using Unity value types, with no scene objects or coroutines. Definitions copy validated dimensions, offsets and mask/override data. Expose size/generation, cell/neighborhood/passability queries, placement and immutable value snapshots. Batch notifications follow complete commits; preview writes nothing. Reject nested new batch operations without partial writes; legacy direct writes require an explicit dirty-publication path.

Add `FieldGenerator.Board`; preserve its existing serialized fields, `Cells` identity semantics, origin conversion and UnityEvents. First adapt `S/GridPlacementService.cs`, `S/GridPathfinder.cs`, `S/Merge/GridMergeResolver.cs` with additive board overloads. Later parity-gated adapters are `S/Match3/Match3BoardService.cs`, `S/Match3/Match3MatchFinder.cs`, `S/SlidingMerge/SlidingMergeResolver.cs`, `S/TicTacToe/TicTacToeBoardService.cs`, `S/TicTacToe/TicTacToeWinChecker.cs`. Avoid ambiguous overloads and retain existing signatures.

Minimum new tests: `E/Audit20261023NeoGridSystemOwnershipTests.cs` and `E/Audit20261023NeoGridSystemBoardCoreTests.cs`: stale/two-owner release, duplicate release, failed-placement atomicity, final-state callbacks, preview immutability and identical results through component/core APIs without GameObjects for the core cases. Replay determinism requires an explicit random source before extending randomized Match3; it is not guaranteed by a snapshot alone.

Estimated effort: P1 correction/tests 1 day; P2 staged core/ownership/adapters 6-9 days. Main risks are mutable legacy fields, callback reentrancy and serialization/API drift. Serializable movement authoring and configurable win lengths remain follow-up design proposals, not confirmed regressions.

## 4. Dependencies, order and acceptance gates

1. Freeze integration evidence, reproduce P1, apply the identity guard, then execute focused regressions centrally.
2. Introduce leases, then core storage/contracts, then additive services; migrate wrappers only after parity passes.
3. Neo.Merge owner `neo-audit-1023-b04-03` first stabilizes `Assets/Neoxider/Scripts/Merge/MergeResolver.cs` result/preview semantics. Dice owner `neo-audit-1023-b03-06` verifies `S/Dice/DiceBoard.cs` suppression/occupancy notifications afterward; neither boundary is edited by this worker. Pooling integration, if required, belongs with Neo.Tools.Spawner owner `neo-audit-1023-b08-03` after unregister semantics stabilize.
4. Update existing `D/README.md`, `FieldGenerator.md`, `FieldObjectSpawner.md`, `GridPlacementService.md` and affected gameplay guides with C# and scene/UnityEvent usage. Keep assets data-only; add unique metadata for new package files. Coordinator handles shared references and sample/release validation across `Samples~`, developer `Samples` and imported `Assets/Samples/NeoxiderTools/<version>/<sample name>`; no automatic layout migration.

Stop a migration increment on serialization loss, callback/API parity failure or an unstable dependency contract; retain independently verified fixes. Release requires recorded compiler/test evidence for both Unity version ranges and the stated Play scenarios. Keep version compatibility in existing centralized helpers. Product decisions about cancellation and automatic object lifetime remain explicit outstanding decisions, not reasons to delay the safe identity fix.
