# Neo.GridSystem.Dice — implementation receipt

Status: **source-final** for the assigned implementation request. Date: 2026-10-24. Original actor/session: `neo-audit-1023-b03-06`; profile `gpt-6.1-sol max`. Current branch read from Git: `demo`; release target: `11.0.0`.

## Approved actions and work completed

The implementation request assigns preservation of the reviewed Dice patch and completion of canonical documentation/navigation. It assigns **no additional confirmed local runtime defect**. The NativeReview-B Dice section accepts the earlier bounded corrections; DICE-01–05 below are completed source changes, not another implementation plan.

| Audit action | Exact runtime file | Retained behavior |
|---|---|---|
| DICE-01 | `Assets/Neoxider/Scripts/GridSystem/Dice/DicePieceGenerator.cs` | Inclusive pool cardinality calculated in `long`, oversized counts rejected before allocation, bounded index loop terminates at `int.MaxValue`. |
| DICE-02 | Same `DicePieceGenerator.cs` | Pair candidate filtering excludes every occurrence of the first value. One distinct value falls back to a single, preserving input multiplicities and leaving the input collection unchanged. |
| DICE-03 | `Assets/Neoxider/Scripts/GridSystem/Dice/DicePiece.cs` | Rotation applies to an off-anchor single cell as well as larger footprints; source/value/Z are preserved. |
| DICE-04 | Same `DicePieceGenerator.cs` | Positive weight total is validated before aggregation or RNG use, preventing aggregate/cursor overflow. The accepted maximum is `int.MaxValue`. |
| DICE-05 | `Assets/Neoxider/Scripts/GridSystem/Dice/DiceBoard.cs` | Merge addition widens before saturation to the positive configured cap or `int.MaxValue`; final content, occupancy and existing board-event ordering are retained. |

`Assets/Neoxider/Scripts/GridSystem/Dice/DiceBoardService.cs` retains the earlier XML-only clarification about the field-backed core and representable merge ceiling. This resumed implementation pass did not rewrite any runtime file or duplicate the accepted regression fixture.

Completed owned documentation:

- `Assets/Neoxider/Docs/GridSystem/Dice/README.md`: canonical runtime/scene entry, actual field dependency, integer boundaries, repeated pools, sample layouts, and links back to GridSystem/FieldGenerator/merge and forward to the service contracts.
- `Assets/Neoxider/Docs/GridSystem/Dice/DiceBoardService.md`: C#/Inspector/UnityEvent workflow, live setter requirements, placement rejection/results, original footprint versus surviving merge cells, deferred merge/null-seed behavior, event order, enabled-only clearing, weighted RNG contracts, explicit 11.0 migration notes, and module/placement/merge navigation.

The parent GridSystem README already links `Dice/README.md`; its existing backlink was observed by reference enumeration. No parent documentation or shared navigation file was edited.

## API and compatibility

No additive public API, assembly reference, component, editor helper, scene/prefab rewrite or serialized-field migration was introduced. Existing C# signatures, five service settings and UnityEvent names remain available. The ordinary C# core still requires a live generated `FieldGenerator`; it is not a detached data-only board.

The deliberate behavior corrections documented for 11.0 are: repeated-value pools no longer yield equal pair values; a single distinct value can produce a single even with `forcePair: true`; overflowing positive weight totals now throw `ArgumentException` for `weights` before a draw; overflowing merge values saturate instead of wrapping; sequential maximum endpoints terminate and unrepresentable cardinalities fail before allocation; off-anchor single cells rotate. Consumers should inspect `CellCount`/`IsPair` and reduce oversized weight totals. A representable pool count is not an allocation-feasibility guarantee.

Public sample guidance preserves all three relevant locations: release `Assets/Neoxider/Samples~/Demo`, development `Assets/Neoxider/Samples/Demo`, and imported `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/...`. No sample layout or package metadata was changed by this actor.

## Regression sources and acceptance — AUTHORED, UNEXECUTED

Retained exact files:

- `Assets/Neoxider/Tests/Edit/GridSystem/Dice/Audit20261023NeoGridSystemDiceRegressionTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/Dice/Audit20261023NeoGridSystemDiceRegressionTests.cs.meta`, retained GUID `547cc5637c0440f7b9d30745fae5b70c`.

The existing fixture contains nine methods / 17 cases:

| Cases | Expected observable result |
|---|---|
| Integer-boundary pools: 2 | Exact two-element output at each integer endpoint. |
| Single maximum endpoint: 1 | Exactly one `int.MaxValue` entry; no runaway enumeration. |
| Unrepresentable cardinalities: 3 | `ArgumentOutOfRangeException` identifies `maxValue` before pool allocation. |
| Single-cell rotations: 2 | Correct clockwise/counter-clockwise position, source/value/Z preservation and inverse restoration. |
| Repeated-value pair selection: 2 | Opposite deterministic endpoint draws yield different pair values; input is unchanged. |
| One distinct value: 1 | Forced-pair request returns one cell with the expected value. |
| Oversized weighted totals: 2 | Both distinct and duplicate inputs throw for `weights` with zero random draws. |
| Maximum valid total: 1 | RNG receives `int.MaxValue`; both distinct values remain selectable at opposite boundaries without assuming dictionary order. |
| Overflowing merged values: 3 | Uncapped/largest-cap/cap-6 results, survivor and cleared occupancy, one merge event, and one board event observing final content. |

The fixture uses public results/state and destroys only its own created GameObjects. No additional tests, parallelization annotations or shared fixture changes were introduced in this pass. Execution relies on the coordinator's isolated runner contract.

## Dependencies and deferred improvements

- No pending additive API blocks this actor's completed source. Existing `FieldGenerator`, placement APIs and `GridMergeResolver` contracts are retained.
- Native review handoffs **B-05/B-06** cover generic increment saturation and widened footprint coordinates in `Neo.GridSystem`. Their owner remains `neo-audit-1023-b03-05`; integration must include the owner's final `FieldGenerator.cs`, `GridPlacementService.cs`, `Merge/GridMergeRequest.cs` and `Merge/GridMergeResolver.cs` changes and regressions. This receipt does not certify their final integrated state or duplicate their edits.
- The substantial data-only backend/shared-provider improvement remains a **proposal** in `Neo.GridSystem.Dice.plan.md`. Its refinement reuses `FieldCell`/existing result DTOs and requires one coordinated GridSystem provider contract. No `IDiceBoardState`, competing DTOs, new constructor overload or provider framework was implemented. Optional distribution snapshots/allocation reductions also remain proposals.
- Original `Neo.GridSystem.Dice.md` and `Neo.GridSystem.Dice.plan.md` were kept as historical evidence without rewriting their earlier check results.

## Actual work evidence and remaining debt

After resuming, read the complete implementation contract/request, historical audit/plan, current manifest entry, relevant current source and complete 17-case fixture/meta. Reviewed scoped Git source/docs diffs and enumerated navigation targets; Git reported branch `demo`. Finished the two owned documentation files and this receipt. Runtime and regression sources were preserved exactly as resumed. No shared tracker/configuration or other actor's files were modified.

**No Unity, compiler, tests, product checker/validator, installer, service, child agent, commit or push was started during this implementation pass.** Historical static checks in the audit are not renewed verification of the 11.0 integrated snapshot.

After every writer finishes, root must obtain frozen-snapshot import/compile and nonempty Edit/Play discovery on the installed Unity 6.3 and 6.6 hosts, run the 17-case fixture plus `DiceBoardCoreTests` and relevant `GridMergeAndDiceTests`, and exercise sample placement → merge → clear/reload with actual state/event/console observations. Package/docs/UTF-8/meta/GUID checks and sample import acceptance remain central and unexecuted here. Other unsupported/unavailable Unity-version branches must remain explicitly unverified rather than inferred from these source edits.

Source ownership is complete and released for coordination; further code changes are needed only if later integrated verification exposes an assigned failure.
