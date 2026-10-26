# Neo.Parallax: implementation receipt

Status: **source-final; corrected source awaits native verification**. Date: 2026-10-25. Target: `demo` / `11.0.0`; original actor/session `neo-audit-1023-b01-02`, requested profile `gpt-6.1-sol`, reasoning `max`.

Root reported that the preceding S3 owner CLI attempt ended nonzero (`exit=3`). The older source-final receipt was therefore not accepted as recovery/completion evidence. This recovery retained the existing source patches, finished the bounded fixture correction below, and updated this receipt. The exit status is root-provided recovery evidence, not a Unity test outcome. The corrected fixture has **not** been compiled, discovered or executed.

## NI48-01 / P2 — native frame observation correction

Read all 64 lines of `Docs/Audits/2026-10-23/NativeImplementationReview-48.md`, raw SHA-256 `d96af5815c725ec3178353e1be0c14c2d384660b67c8f8760724355d64122786`, and the appended approved request/implementation contract. The review pins this owner's preceding receipt at `f476e1f783fa83f36fa4e3d11982c54c082bc9538879ca1f8b2bd499611e15cb` and fixture at `756bb75817895e2d5468aa141df2a1daec4d0321d6e73752772b2a7bcbfac9f7`. Those are historical source artifacts, not completion evidence for this correction.

Confirmed evidence gap: a single Edit-runner `yield return null` after RNG capture could resume in the same native frame. The stationary negative therefore did not prove an intervening LateUpdate opportunity. The accepted correction changes only the existing prefixed fixture and this receipt; no runtime defect or public contract change is inferred.

- Added private `WaitForNativeFrames`: each boundary captures actual `Time.frameCount`, polls until at least **two native frame advances** while Play Mode remains active, and fails if progress is absent within a **10-second monotonic Stopwatch deadline**, independent of timeScale/native frame-clock progress. Null yields only poll; they are never counted as frames. Two advances include a complete intervening native frame/LateUpdate opportunity, even if the Edit runner resumes before LateUpdate in the current frame. No callback is invoked manually.
- Applied that barrier to teleport/stationary pool settling, the actual camera-movement positive, the post-capture stationary RNG/sprite observation, root-template deferred destruction, lifecycle initial/re-enabled settling, disabled-pool destruction and component destruction. Immediate retirement/source-restoration checks still occur before the destruction barrier. All 100-tile/pool/position/RNG/sprite and 8/6/7-tile bounds/stride/anchor/source-enabled/authored-child hierarchy assertions remain.
- Teardown uses the same bounded barrier after owned GameObject and sprite/texture destruction. Barrier faults are retained with boundary/frame/elapsed diagnostics, cleanup and RNG restoration continue, and the faults fail teardown after its top-level ExitPlayMode. It still exits only when `ownsPlayMode && Application.isPlaying`. Existing native cleanup/Exit failures are not caught, filtered or relabeled successful.
- All **45 authored scenarios** and previous renames remain (39 explicit Edit cases plus six native UnityTests). No runtime/settings/geometry/default/API/GUID/canonical-guide, foreign-owner, Fixtures-helper, shared-reference or asmdef change was made. The deadline controls test termination only; no product numerical tolerance or acceptance oracle was relaxed.

Actual validation in this follow-up is limited to full NI48/request/contract reads, current reservation/source reads, inspection of the changed boundaries and raw artifact hashes. No compiler, Unity, test, checker, installer, process control, child actor, commit or push was launched. Source correction is complete; the same independent reviewer must reread it. Actual discovery, native frame progress/LateUpdate opportunities, RNG/sprite stability, nonempty motion/geometry and deferred cleanup remain **UNEXECUTED on this snapshot** until all writers finish and root establishes a new reviewed freeze.

## S3 evidence and correction

Read the complete implementation contract/request, the assigned XML cases and their failure payloads, action-index groups E18–E23, and the relevant independent Edit/Play triage. Existing graphical Edit results contain **41 owned cases: 23 passed and 18 failed**; the two separate legacy Parallax cases passed. These historical results do not certify the corrected source. The corresponding graphical Play XML contains **zero Parallax cases**.

| Input receipt | Raw SHA-256 |
| --- | --- |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` |

The assigned failures are consistent with fixture admission/lifecycle mistakes; they do not establish a new runtime geometry defect:

| Group | Failures | Actual failed oracle |
| --- | --- | --- |
| E18 | 2 | Edit tests sent `LateUpdate` through `SendMessage`, triggering the native `ShouldRunBehaviour()` assertion. |
| E19 | 4 | Source-enabled restoration after disabling a never-Awake Edit component: direct/nested template, replaced template and initially disabled live-pool recovery. The preceding count/geometry assertions had completed. |
| E20 | 1 | Final cleanup expected zero generated renderers but retained one in the disabled-both-axes case. |
| E21 | 9 | Final cleanup expected zero but retained eight: disabled-axis, invalid pitch/endpoints and ordinary recovery/source-ownership cases. |
| E22 | 1 | Final cleanup expected zero but retained twelve in the negative-spacing case. |
| E23 | 1 | Final cleanup expected zero but retained five in the representable centered-extrema control. |

The former Edit arrangement could build through the explicit public camera API while its GameObject remained inactive. Its disable/destroy operations did not establish that native Awake/OnDisable/OnDestroy had run. Pool existence is now positively established before testing release; actual callbacks are covered separately through admitted Play Mode objects.

Only the existing owned fixture and this receipt changed in this recovery:

- `Neo.Editor.Tests.Audit20261023NeoParallaxRegressionTests` retains **39 parameter-expanded Edit cases** using serialized public authoring plus `SetTargetCamera(...)`. Explicit `SetTargetCamera(null)` with fallback disabled releases the pool, including teardown; it is described as camera release rather than native disable evidence. Authored-child and template-replacement tests also assert a live eight-tile positive precondition. `RebuildAndDisable_PreserveAuthoredChildren` is renamed `RebuildAndReleaseCamera_PreserveAuthoredChildren`; `RebindingAndDisabling_RestoreInitiallyDisabledSource` becomes `RebindingAndReleasingCamera_RestoreInitiallyDisabledSource`.
- The same file now also contains `Neo.Editor.Tests.Audit20261023NeoParallaxNativeLifecycleTests`: **six UnityTest methods**, with top-level UnitySetUp `EnterPlayMode` and top-level UnityTearDown `ExitPlayMode`. The fixture requires an idle Edit session before entering and exits only the Play session it owns. It creates/destroys only its own camera, layer, renderer, sprite/texture and authored children, and restores its captured RNG state. No scene/provider/session reset or helper extraction is used.
- The two existing motion cases retain their method names and move to the native fixture: `CameraTeleport_RecentresBothAxesWithoutChangingPoolSize` and `StationaryCamera_DoesNotRerollPaddingTilesEveryFrame`. They establish an active 100-tile pool, then observe real frames and the original camera-distance, stable-pool, sprite and RNG oracles. There is no manual Update, SendMessage, private lifecycle invocation or UserCode bypass.
- Four additional native cases cover initially enabled/disabled root sources and direct/nested child templates. They establish exact active pool counts (8/6/7), assigned sprite identity, real world dimensions/stride/anchor and source state before disabling. They require immediate retirement, actual deferred destruction, exact source restoration, re-enable/rebuild, and component destruction while source/authored children survive with their original hierarchy/transform. Enter/ExitPlayMode occur only in the top-level setup/teardown, never in the shared assertion coroutine.

The authored source now contains **45 owned scenarios: the original 41 retained plus four native lifecycle controls** (39 explicit Edit cases and six UnityTests). Native discovery/counts remain unverified. Every numerical rejection/retry, geometry, source-enabled, authored-child, sprite/state, padding and RNG oracle remains. In particular, the finite local/world extrema arrays, rejected five-tile layouts and eight-tile ordinary recovery, disabled axes/extreme unused spacing, representable overflow control and ordinary negative-spacing overlap are preserved. No tolerance, camera/default/spacing change, arbitrary tile cap, expected-error whitelist, ExecuteAlways, Ignore or weakened inactive-renderer enumeration was introduced.

Runtime and the two canonical guides were not changed in this recovery: no additional public behavior change was justified by these failures. Their earlier fixes/documentation are retained below. The public/serialized API, all 15 settings/defaults and existing GUIDs remain intact.

## Earlier approved runtime actions retained

**PARALLAX-COUNT-OVERFLOW — P2**, corresponding to NativeReview-B B-10 and historical plan F1.

**PARALLAX-SRC-01 — P2**, accepted by root after its full read of `IndependentImplementationReview-Parallax.md`. Read the complete independent report and the appended request; corrected the separate admission gap between finite world periods and generated local positions. No particular Unity setter response/log is asserted: native execution remains barred.

Resumed the existing source without replacing the earlier ownership, transform, snapshot, invalid-stride, recycling or preview corrections. The original audit, plan and independent review remain unchanged as historical evidence. No broad layout/configuration framework was implemented.

## Exact files and behavior

- `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs`: retained the completed private `ComputeTileCounts()` boolean guard. Padding, viewport/count/product and world-period validation still precede allocation. Replaced the nonserialized float `spacingLocal` vector with nonserialized double pitch/origin fields. New private `ComputeLocalGeometry()` validates enabled-axis positive finite local pitch, centered first/last X/Y coordinates and finite Z before `BuildTiles` can create anything. Positive pitch makes these endpoints bound every generated coordinate. Disabled axes use zero pitch without evaluating unused spacing. `BuildTiles` computes each final centered position in `double`, casts only the final validated result to `float`, then creates the tile. `Initialise()` rejects either count or geometry failure through the existing gated warning/cleanup/source-state/retry path.
- `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs`: earlier source phases expanded 17 methods / **31 cases** with five methods / **10 local-geometry cases**. S3 executed those 41 cases with the results above. The current correction retains their substantive oracles, uses explicit release for pure Edit contracts and adds native lifecycle coverage, for **45 authored cases**. Real renderer coordinates, sprite identity, enabled/active state, source state and recovery geometry are observed. No unsupported NUnit scheduling attribute was added.
- `Assets/Neoxider/Docs/Parallax/ParallaxLayer.md`: documented count/product/world-period and local-coordinate preflight, widened centered position construction, disabled-axis spacing policy, source restoration, camera/Inspector retry and remaining practical memory limits.
- `Assets/Neoxider/Docs/Parallax/README.md`: retained the component-guide entry, expanded its C#/Inspector/count/local-geometry recovery description, and corrected demo/controller references to the active `Samples~` layout while explaining developer `Samples` and imported `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes/` paths.
- `Docs/Audits/2026-10-23/Neo.Parallax.implementation.md`: this receipt.

The existing regression filename/meta were reused; no new package file or GUID was needed. No legacy fixture, asmdef, package metadata, shared tracker, other actor report, scene or prefab was edited.

## API and compatibility

No additive public API. `TargetCamera`, `SetTargetCamera(Camera)`, all 15 serialized fields/defaults, `MovedFrom`, multiplier direction, auto-scroll and existing source ownership remain intact. Disabled tiling axes still use one tile and ignore their padding; their unused spacing is no longer evaluated, avoiding `0 * Infinity`. Ordinary negative-spacing overlap remains supported. Perspective still uses its existing fixed-count formula with widened arithmetic.

Deliberate boundary behavior: requests that formerly overflowed to an unrelated small pool, exceeded count/product representation, or produced non-finite stored wrap periods/local coordinates now fail before tile creation. An existing pool is released through the established rebuild path, and the original enabled state is preserved even for an intentionally disabled source. This is not the proposed transactional keep-old-pool design. Double viewport/ceiling arithmetic may change a row/column count at a former float-rounding boundary; local coordinate arithmetic now also stays widened through centering. Serialized inputs and movement semantics are unchanged.

There is **no arbitrary new capacity default**. `Int32.MaxValue` follows the existing count/list representation; period and final-coordinate bounds follow stored `float` values. Representable centered positions are admitted even if an uncentered `index * pitch` would overflow float arithmetic. A very large representable request can still exhaust memory; configurable capacity, pure layout extraction, perspective coverage and automatic refresh remain proposals in the historical plan.

## Retained count/local-geometry acceptance

The following are authored acceptance requirements. The old S3 fixture has historical partial results above; none is a pass claim for the current corrected fixture. Cleanup in explicit Edit cases now means public camera release; actual disable/destroy is observed separately in the six native UnityTests.

| New scenario | Cases | Required observable result |
| --- | --- | --- |
| Extreme enabled-axis padding | 4 | Orthographic/perspective and horizontal/vertical requests with `int.MaxValue` padding create no tiles and retain the source; repaired padding creates the expected 8 or 6 tiles. |
| Representable axes, excessive product | 2 | Padding `(25000, 25000)` with both axes enabled rejects before allocation in both camera modes; repair produces 64 orthographic or 48 perspective tiles. |
| Huge finite orthographic viewport | 1 | `orthographicSize = float.MaxValue` produces no pool; resetting size to 2 recovers an 8-tile pool. |
| Unrepresentable stored world period | 1 | Finite spacing `float.MaxValue` is rejected before creating invalid tile transforms; zero spacing recovers. |
| Extreme padding on disabled axes | 3 | Disabled axes do not reject valid one-axis/single-tile configurations; actual pool size is 8 or 1. |
| Rejection after a live pool | 2 | Previously active tiles are released, initially enabled/disabled source state is restored exactly, and a repaired request creates a working pool and preserves that state after camera release. |

The table above records the previously authored 13 count cases, alongside the original 18 scenarios. All **31** retain their substantive oracles; two motion cases now observe native frames, and Edit cleanup uses explicit camera release.

Additional PARALLAX-SRC-01 scenarios use the same orthographic size 2/aspect 1, unit source sprite and default padding. Let `F = float.MaxValue`:

| Local-geometry scenario | Cases | Required observable result |
| --- | --- | --- |
| Cold/live invalid five-tile layout | 4 | X scale `1/16`, spacing `F/8` rejects without generated renderers for initially enabled/disabled sources, both cold and after a live pool. An authored child survives. Repair to unit scale/zero spacing creates exactly eight active/enabled renderers with the assigned sprite, unit bounds/stride and correct anchor; camera release removes the pool and restores the exact source state. |
| Finite pitch, unrepresentable centered endpoints | 1 | X scale `1/8`, spacing `F/8` has local pitch `F` but endpoints outside float storage, so no pool is created. Unit-scale/zero-spacing repair produces eight renderers with actual unit geometry and normal cleanup. |
| Representable centered positive control | 1 | X scale `1/4`, spacing `F/8` creates exactly five active/enabled renderers with sprite identity and explicit local X coordinates `[-F, -F/2, 0, F/2, F]`. Explicit world X coordinates are `[-F/4, -F/8, 0, F/8, F/4]`; all stored local/world components must be finite. Camera release restores the source. |
| Disabled-axis unused extreme spacing | 3 | Disabled X or Y spacing with scale `1/16` on that axis does not reject the ordinary other axis or the single-tile case. One or eight renderers have finite coordinates, source-anchored disabled coordinates and correct sprite/state; cleanup restores the source. |
| Ordinary negative-spacing overlap | 1 | Spacing X `-0.5` produces 12 actual unit-width renderers at half-unit stride, centered from X `-2.5` to `3`, with correct sprite/state and cleanup. |

All count/local-geometry scenarios exercise public camera injection and serialized Inspector authoring. They do not call private count/geometry code or compute expected positions by copying its algorithm. The rejection controls include actual geometry recovery, and the positive control rejects an unconditional-refusal implementation. Historical S3 failures include completed extrema/geometry oracles followed by failed cleanup. No specific Transform setter response, console or rendering result is inferred for the corrected source.

## Current source snapshot

Raw SHA-256 values captured after the correction; these identify artifacts, not successful compilation or test execution. The runtime matches the recovery's pre-edit hash. The existing fixture/meta are reused; no package asset or GUID was added.

| File | Raw SHA-256 |
| --- | --- |
| `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs` | `cd3ee3ee2f9d7bd72c282f07549e0ea366a15a827e3a5b0dc8185961d3331e6d` |
| `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs.meta` | `68a5b17c58afd9033096f410b99031abb4de805ff7f40d1975b1b36f5485e30c` |
| `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs` | `4931864cb972c6a299344ce78123e479cfefb43ed039bad6179b808f41a2337c` |
| `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs.meta` | `33f1d08de1121134f990348e8ad0094ed0fd4b79961fe40c3e984c886b761b8c` |
| `Assets/Neoxider/Docs/Parallax/ParallaxLayer.md` | `e536547ef5d99f6e10d718df53e10618edf0ce90e199530a358b27e2ee75b829` |
| `Assets/Neoxider/Docs/Parallax/README.md` | `32da81863e65f23fd31f2de65306b1413eae6f6c04543e32d6b576d37fa600a4` |

The reservation map was checked before each edit. Its observed status is `source-final`; neither owned caller nor receipt is reserved. Observed map hash: `14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7`. Primary-owned Fixtures helpers, moves/extractions, references and shared asmdefs remain untouched. No reserved work remains deferred from this owner; no shared tracker was updated.

## Dependencies and evidence debt

No pending additive API or cross-owner source dependency blocks this action. Existing `Neo.Extensions` diagnostics and Unity math are reused; no assembly-reference change is required. Runtime/editor version compatibility stays in the existing common diagnostics gates.

Actual work across the source phases: read the full implementation contract/request and historical audit/plan; read the complete independent review and root's appended residual request; read current owned runtime, regression source and public docs; inspect the count/local-geometry branches, regression branches and documentation changes; enumerate the actual parallax demo/controller paths. In this recovery, read the assigned existing S3 results/action groups/triage, review the corrected source, and capture raw hashes. Source-only reads/diffs and artifact bookkeeping used small `cmd.exe`/Python calls. Initial shell-quoting issues were resolved using encoded Python input; final source writes and hash capture completed successfully, without a repeated resource interruption. No Unity, compiler, tests, product validator/checker, installer, service, commit, push or child actor was launched by this owner during the correction. Prior audit checks, historical S3 results and independent source hashes are not verification of the current fixture.

After **every writer finishes and root establishes a new global freeze**, the coordinator must compile the frozen snapshot on installed Unity 6.3 and 6.6, including applicable Editor/player and centralized version branches. Discover/run both owned classes in the existing Edit assembly (39 explicit Edit cases and six Edit-to-Play UnityTests), plus the existing legacy Parallax tests, and retain actual counts/results/console output. Native precondition/transition behavior, the bounded two-frame barriers and actual LateUpdate opportunities, source restoration, deferred tile destruction and teardown isolation/fault reporting are unverified for the new fixture. Observe the retained huge-coordinate control through native Transform getters; no result is inferred here. Inspector queue cancellation, stop/domain-reload cleanup and real sprite coverage still need integration evidence. Signed nested scale, rotation/shear and perspective coverage retain the limitations recorded in the audit. Unsupported/unavailable Unity versions are not certified by this receipt. Source writes stop with this receipt; no execution is authorized by it.
