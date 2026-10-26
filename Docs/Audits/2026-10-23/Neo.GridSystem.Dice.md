# Neo.GridSystem.Dice: audit

Status: source audit complete; static validation passed. Unity compilation and regression execution remain with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/GridSystem/Dice/Neo.GridSystem.Dice.asmdef`.
- Scope: 7 C# files; nested assemblies excluded.

## Scope

Прочитаны все 7 C# файлов сборки. Исправлены пять подтверждённых дефектов, добавлены 17 регрессионных сценариев; Unity и тестовые раннеры не запускались по контракту.

All filenames below are relative to `Assets/Neoxider/Scripts/GridSystem/Dice/`. The exact manifest inventory matches the seven C# files on disk; this directory contains no nested assembly.

| Reviewed file | Review coverage and outcome |
|---|---|
| `DiceBoard.cs` | Constructor null handling; clamped rule setters; null/empty pieces; placement delegation, masks, blockers, walkability and occupancy; merge seeds/cascades; per-cell and board events; enabled-only clearing. Fixed merge overflow. Replaced existing implicit locals with explicit types. |
| `DiceBoardService.cs` | Required component lookup, lazy initialization/Awake, rule setters, fallback results, serialized fields, UnityEvent forwarding and object lifetime. Settings are copied on initialization; Inspector edits after initialization do not call the public setters. Documented the supported live C# path and corrected the claim that the core needs no scene dependencies. Runtime implementation unchanged. |
| `DicePiece.cs` | Null enumerable/serialized list normalization, struct copying, clone independence and XY/Z rotation semantics for empty, single, pair and larger footprints. Fixed skipped off-anchor single-cell rotation; removed the implicit local type. |
| `DicePieceCell.cs` | Serializable value-type offset/value contract and constructor; no owned resources or scene references. No defect requiring a change found. |
| `DicePieceGenerator.cs` | Missing/empty pools, optional RNG, pair selection, duplicate values, forced-pair fallback, inclusive integer boundaries, weighted filtering/aggregation/cursors. Fixed range overflow, repeated-value pairs and weight-sum overflow. |
| `DicePlacementResult.cs` | Default failure state and non-null independently allocated result collections/merge result. Mutable result DTO remains source-compatible. No change needed. |
| `DiceValueWeight.cs` | Serialized value/weight names, constructor's nonnegative clamp, serialized negative inputs filtered by generator, no scene references. No change needed. |

Read-only context: existing `DiceBoardCoreTests.cs`, `GridMergeAndDiceTests.cs`, runtime/test asmdefs, relevant `FieldGenerator` placement/enumeration code, `GridMergeRequest`, `GridMergeResolver`, `GridMergeResult`, and generic `MergeResolver` resolution. Ownership rules and the complete worker contract were read before edits. Dependency source was changing concurrently; cross-boundary observations below refer to the inspected snapshot.

Reuse: retained `FieldGenerator` placement validation, `GridMergeResolver`/`MergeResolver`, and the existing injected `Func<int, int>` RNG. Consulted the neoxider-tools GridSystem and extension references. The catalog's float/global-RNG weighted helpers are not substitutes for this integer/injected-RNG contract. No new runtime helper/system, dependency, global service, logging, Unity identity API, or version-specific branch was introduced. Local source review and self-checks were performed by this worker; independent QA remains central.

## Findings and fixes

### DICE-01 — High: inclusive pools ending at the largest integer never finish

Source: `DicePieceGenerator.CreateSequentialPool`. The prior loop incremented the value while testing `value <= maxValue`. With `maxValue == int.MaxValue`, increment wraps to `int.MinValue` in unchecked arithmetic and the condition remains true; even a one-value request keeps allocating until failure. The capacity expression also overflowed: the full integer range became capacity zero.

Fix: calculate cardinality as `long`, reject counts above `int.MaxValue` with `ArgumentOutOfRangeException(nameof(maxValue))` before allocation, and enumerate a bounded index. Reversed ranges retain their existing `ArgumentException`. Representable but enormous pools remain subject to available memory.

Regression: both integer endpoints, a single `int.MaxValue`, and three unrepresentable cardinalities. Sequential-pool cases have a timeout to bound a recurrence of the old runaway loop.

### DICE-02 — Medium: repeated pool entries violate distinct pair values

Source: `DicePieceGenerator.Generate`. Removing only the first selected index allowed `[1, 1, 2, 2]` with a zero-returning RNG to generate `(1, 1)`. A pool `[7, 7]` incorrectly produced a pair despite having only one distinct value, contradicting the canonical README's pair guarantee.

Fix: preserve the first draw and input multiplicities, then select from entries with a different value. If none remain, return a single. Unique pools retain their prior random-call sequence and selection mapping; input collections are not mutated. Pair generation now allocates one temporary candidate list.

Regression: first/last deterministic draws over repeated entries and forced-pair fallback for one distinct value.

### DICE-03 — Medium: single cells away from the anchor do not rotate

Source: `DicePiece.Rotate`. `CellCount < 2` returned a clone without applying rotation. A cell at `(2, 3, 4)` incorrectly stayed there instead of moving to `(3, -2, 4)` clockwise or `(-3, 2, 4)` counter-clockwise.

Fix: apply the existing coordinate transform to every cell regardless of count. Values, Z offsets, source data, empty-piece behavior, and all serialized members are preserved.

Regression: both directions assert the new position, unchanged value/Z/source, and inverse-rotation restoration.

### DICE-04 — Medium: positive weights can overflow before selection

Source: `DicePieceGenerator.GetUsableWeights` / `RollWeightedIndex`. Two positive weights totaling `int.MaxValue + 1` wrapped the integer total negative. Duplicate-value aggregation could also wrap, after which `DiceValueWeight` clamped the aggregate to zero. Valid-looking inputs therefore reached the RNG with an invalid bound or lost their intended distribution.

Fix: validate the cumulative sum of positive input weights before aggregation. Totals above `int.MaxValue` now throw `ArgumentException(nameof(weights))` before any random draw. A total exactly equal to `int.MaxValue` is accepted. This bound also proves each merged weight and selection cursor stays representable.

Regression: overflowing duplicate and distinct entries assert the exception parameter and zero random draws. A maximum-total pool with duplicates keeps both distinct values selectable at opposite roll boundaries without relying on dictionary enumeration order. Migration: callers with oversized weights must reduce the total; silently incorrect selection is no longer accepted.

### DICE-05 — Medium: merged values wrap before an explicit cap is applied

Source: `DiceBoard.ResolveMergesInternal`. `(int.MaxValue - 1) + int.MaxValue` previously became `-3`. Applying `Mathf.Min(-3, 6)` still yielded `-3`; the uncapped path inherited the same overflow through `GridMergeRequest.Increment`.

Fix: always override the dice result calculation, widen addition to `long`, and clamp to the positive configured cap or the representable `int.MaxValue` ceiling. Normal-range behavior and event ordering are preserved. With no explicit cap, overflow now saturates instead of producing unrelated negative IDs.

Regression: no explicit cap, the largest cap, and cap 6. Assertions cover the returned group value, surviving cell, cleared content/occupancy, one merge event, and one board event observing the final value.

All public declarations and serialized field declarations match HEAD. No scenes, prefabs, samples, asmdefs, settings, shared tests, or other workers' files were edited.

## Documentation

- Updated the canonical `Assets/Neoxider/Docs/GridSystem/Dice/README.md`: rotation, repeated-value selection, sequential range validation, the `DiceBoard` API example/dependencies, merge ceilings, scene workflow, and release/development/imported sample locations.
- Updated `Assets/Neoxider/Docs/GridSystem/Dice/DiceBoardService.md`: initialization timing, live C# setters, movement directions, UnityEvent wiring, typed placement, enabled-only clearing, reserved empty IDs, weighted input validation and deterministic RNG bounds.
- Corrected XML documentation in `DiceBoard` and `DiceBoardService`. The core is an ordinary C# object but still requires a live generated Unity `FieldGenerator`; the wrapper is optional.

## Substantial improvement proposal

### Optional data-only board state with a FieldGenerator adapter — proposed, not implemented

Problem: `DiceBoard` is not a MonoBehaviour, but its constructor still requires one. Tests, simulations, replay validation and server consumers must create a Unity GameObject/Grid/FieldGenerator graph. Placement and merge state are stored directly in mutable `FieldCell` objects, tying the rules' lifetime and notifications to that Unity graph. The existing wrapper documentation overstated this independence.

Proposed design/API:

- Introduce a narrow `IDiceBoardState` contract for reading enabled/walkable/occupied/content state, enumerating cells/neighbors, and applying a batch of cell changes. Use immutable `DiceCellState`/`DiceCellChange` structs; topology stays supplied by the state adapter rather than hardcoded rectangular rules.
- Add a data-only `DiceBoardState` implementation and `DiceBoard(IDiceBoardState state, DiceBoardRules rules)` overload. Keep the coordinate type compatible with the grid API initially; no claim of a UnityEngine-free package is required to eliminate scene objects.
- Implement a `FieldGeneratorDiceState` adapter. Reuse the generic `Neo.Merge.MergeResolver` for graph resolution; avoid duplicating flood-fill/cascade rules. Apply all content and occupancy changes before adapter notifications, then raise the existing board events.
- Keep `DiceBoardService` as the authoring/UnityEvent adapter; Inspector settings populate a rules snapshot. Define the adapter's valid field lifetime explicitly.

Compatibility/migration: retain `DiceBoard(FieldGenerator)`, all service serialized fields and methods, and existing events. The original constructor selects the FieldGenerator adapter. Preserve `Generator` for that path; decide and document its nullable result for the new data-only path before introducing the overload. Preserve seed order, movement directions, disabled-cell behavior, placement atomicity, and existing board-event counts. Consumers opt into the new state backend; no scene/prefab migration is needed.

Acceptance scenarios: run the same placement/rotation/merge/cascade fixtures against a data-only board and the Unity adapter; cover irregular masks, disabled cells, blockers, custom neighbor rules, rejected footprints with no mutation, capped integer boundaries, and final consistent notification state. Prove a replay over data-only state needs no GameObject and produces the same final content/occupancy as the adapter. Existing public-contract tests must continue to pass unchanged.

Estimated effort/risk: 4–6 developer days plus central Unity/version QA. Medium risk around compatibility of `Generator`, seed ordering and notification timing; stage the state interface/adapter first and compare existing fixtures before moving logic. This requires coordination with the `Neo.GridSystem` owner and is deliberately outside this worker's focused fixes.

## Verification and remaining risks

Checks actually run:

- Scoped `git diff --check --` over the owned source/docs, new regression/meta, and this report: exit 0 before report finalization; repeated for the finalized report at handoff. New/untracked files are additionally covered by the whitespace scan because Git's normal diff does not inspect their content.
- Inline read-only Python validation: exact 7/7 manifest inventory, no nested asmdef, strict UTF-8 decoding/no replacement characters/trailing whitespace in 11 source/doc/test/meta files, no `var` declarations in 8 C# files, and balanced lexical delimiters. Delimiter checking is not C# compilation.
- Compared public and serialized field declaration lines with Git HEAD for all 7 owned sources: unchanged. Existing EditMode asmdef already references `Neo.GridSystem`, `Neo.GridSystem.Dice`, and `Neo.Merge`; no reference edit required.
- Verified the new test meta GUID `547cc5637c0440f7b9d30745fae5b70c` is valid and appears exactly once among Assets metadata. Checked relative links in both owned docs and the Dice demo path against the active `Samples~/Demo` package entry and scene file.
- Bounded Python signed-32-bit arithmetic diagnostics confirmed the old maximum increment becomes `-2147483648`, full-range capacity becomes zero, oversized positive weight total becomes negative, and the merge example becomes `-3`. These are static numeric evidence, not execution of compiled C#.

New regression: `Assets/Neoxider/Tests/Edit/GridSystem/Dice/Audit20261023NeoGridSystemDiceRegressionTests.cs` with its unique `.meta`: 9 test methods / 17 NUnit cases, using observable public state/results and no reflection or source-text assertions. They have **not been executed**. Coordinator action: compile/import and run the `Audit20261023NeoGridSystemDiceRegressionTests`, existing `DiceBoardCoreTests`, and dice cases in `GridMergeAndDiceTests`; validate supported Unity 6.0–6.4 and 6.5+ branches centrally. Play Mode/console/sample verification was not run by this worker, as explicitly prohibited.

Known limits: very large representable pools can exhaust memory; custom RNG delegates must honor `[0, maximum)`; callers must reserve the empty ID outside all dice/merge values; live Inspector edits require the documented C# setters or reinitialization. The wrapper/core graph has no static subscriptions, coroutines or disposable handles, but retaining `service.Board` also retains its UnityEvent forwarding closures and field dependency; callers should respect the scene lifetime until an explicit adapter lifetime contract is introduced.

Cross-boundary actions for the coordinator / `Neo.GridSystem` owner (observed in dependency source, not edited here):

1. `GridMergeRequest.Increment` and the default `GridMergeResolver` result lambda add integer content without widening. Direct generic-grid clients can still wrap at the integer limit. Dice is now protected by its own override; verify/fix the generic contract in its owning assembly.
2. `FieldGenerator.ValidateContentFootprint` calculates `Vector3Int position = anchor + entry.Offset` without an overflow guard. With both X coordinates `int.MinValue`, signed 32-bit addition wraps to zero, so an extreme out-of-range footprint can target `(0, 0, 0)`. Check the coordinate sum in a wider type before converting/validating. This was established by source inspection/arithmetic, not a Unity run; add the owning assembly's placement regression when fixing it.

No Unity/MCP, compile/build/test runner, install, child agent, shared service, or commit operation was invoked.
