# Neo.Parallax: audit

Status: source audit and scoped fixes complete; Unity compilation and regression execution remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Parallax/Neo.Parallax.asmdef`.
- Scope: 1/1 owned C# files fully reviewed; no nested assemblies in this inventory.

## Scope

- `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs` (entire file, including both editor/player conditional paths).

Owned canonical documentation: `Assets/Neoxider/Docs/Parallax/ParallaxLayer.md`.

New regressions: `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs` and its unique GUID `.meta`. Existing tests and assembly definitions were read as context and left unchanged. No shared configuration, scenes, prefabs, samples, commits, installs, Unity/MCP calls, build/test runners, or child agents were used.

### Full source review

| Area | Reviewed code/contracts | Result |
| --- | --- | --- |
| Serialization and API | Attributes, all 15 serialized fields, `TargetCamera`, `SetTargetCamera`, nested `Tile` value type | Serialized names/types/defaults and public declarations preserved. Corrected the multiplier tooltip to describe the existing formula. |
| Lifecycle | `Awake`, `OnEnable`, `LateUpdate`, `OnDisable`, validation and editor delay callback | Checked initialization retries, active/disabled operation, delayed preview work and teardown. Added destroy cleanup and cancellation; validation rebuilds are deferred in the Editor, including Play Mode. |
| Binding and diagnostics | `ResolveTargetCamera`, missing renderer/sprite paths, explicit camera injection | Explicit dependency remains preferred. `Camera.main` stays optional. Existing `NeoDiagnostics` gates and missing-camera throttle/`StableId` remain in place; new invalid-stride warning uses the existing diagnostics gate. |
| Layout | `Initialise`, `ComputeCellSize`, `ComputeTileCounts`, `BuildTiles` | Corrected root/child transform accounting, validated usable sprite pool and positive finite strides before allocation. Reviewed orthographic counts, perspective fallback, padding and world/local spacing. |
| Resource ownership | `CreateTile`, `RemoveExistingTiles`, `Cleanup`, template snapshots | Only generated entries are removed. Play Mode retirement deactivates tiles before deferred destruction. Original source identity/enabled state survives rebinding or template replacement. |
| Renderer/sprite behavior | `CopyRendererSettings`, `BuildSpritePool`, `AssignSprite` | Shared material remains shared; no implicit material/texture ownership or allocations introduced. Checked draw modes, sorting, flips, variants/null filtering, maximum-size fitting and opt-out gaps. Fitting now separates unscaled sprite dimensions from source-relative scale. |
| Motion and recycling | `UpdateParallaxOffset`, `RecycleTiles`, `ShiftTile` | Existing inverse camera-delta formula and scaled-time scrolling preserved. Bounded period wrapping replaces distance-dependent loops; stationary padding does not reroll. Removed/destroyed tile entries are skipped. |
| Dependencies/version branches | `Neo.PropertyAttribute`, `Neo.Extensions`, Unity math/rendering and conditional editor code | No new dependency or asmdef edit. No raw instance-ID APIs or new version switches. Compilation of Editor/player and Unity version branches remains unverified. |

Reuse receipt: read `neoxider-tools` and `unity-game-agent`, checked the relevant parallax/module and transform/wrap reference entries. Adapted the existing `ParallaxLayer` implementation using Unity's `Matrix4x4`, `Quaternion`, and `Mathf.Floor`, existing `Tile` records, `NeoDiagnostics`, and existing editor delay callbacks. No parallel parallax system, generic helper library, manager, or NoCode bridge was added.

## Findings and fixes

### P1: cleanup deleted scene-authored children

- **Trigger:** initialize, rebind or disable a layer containing any child other than the immediate template transform. A nested template's parent also qualified for deletion.
- **Observable consequence:** authored objects were destroyed; deleting a nested template container could invalidate the renderer during construction. This also happened in Edit Mode through `DestroyImmediate`.
- **Source:** baseline `RemoveExistingTiles` enumerated every `transform` child and excluded only `templateRenderer.transform`.
- **Fix:** iterate only this instance's recorded `tiles`. Never infer ownership from hierarchy membership. Deferred runtime deletions deactivate retired objects immediately; disable/destroy teardown is idempotent.
- **Regressions:** `RebuildAndDisable_PreserveAuthoredChildren`; both `ChildTemplate_ScaleDeterminesTileSizeAndStride` cases assert source/hierarchy survival and actual rendered geometry.

### P1: layer/source transforms produced wrong anchors and tile strides

- **Trigger:** root renderer on a translated/scaled layer, or a scaled child/nested source renderer.
- **Observable consequence:** source translation or scale was applied again to generated children, while child source scale was omitted from the grid stride. Tiles overlapped or the pool was offset from the source. Independently, a unit-scale root at local X=12 anchored its clone at world X=24; a unit sprite on a root scaled by 2 produced width-4 clones at stride 2.
- **Source:** baseline `Initialise` treated source local scale/position as child-relative even when the renderer was on the layer itself. `ComputeCellSize` used layer scale without child source scale; `AssignSprite` mixed sprite-size fitting with this scale.
- **Fix:** compute the source anchor/rotation/scale relative to the layer. Root clones use identity relative scale/rotation. Preserve exact direct-child scale and measure axis-aligned nested relative scale through the source-to-layer matrix. Apply relative scale to cell dimensions and use separate unscaled maximum sprite dimensions for fitting. Source transforms are no longer rewritten during construction.
- **Regressions:** three `RootTemplate_AppliesPositionAndScaleOnce` cases cover translation and positive/negative root scales; direct/nested child cases assert expected world bounds, adjacent strides and source anchoring.

### P2: camera/template rebinding lost renderer state

- **Trigger:** clear the camera with fallback disabled after successful initialization; disable an initially disabled source; replace the serialized template while running.
- **Observable consequence:** clearing the camera could leave the original renderer hidden with no tiles. Cleanup forced intentionally disabled sources on, or restored the new template while leaving the previous renderer hidden.
- **Source:** baseline `Initialise` cleared `hasTemplateSnapshot` before cleanup; cleanup referenced the current `templateRenderer` and always assigned `enabled = true`.
- **Fix:** retain a snapshot of the actual renderer identity and enabled state. Release the old source before taking a new snapshot; preserve current authoring transforms during rebuild and restore captured transforms on disable/destroy. Clear consumed snapshots to make repeated cleanup safe.
- **Regressions:** `ClearingCamera_RestoresSourceAndRemovesPool`, `RebindingAndDisabling_RestoreInitiallyDisabledSource`, `ReplacingTemplate_RestoresPreviousRenderer`.

### P1: invalid strides could allocate huge pools or prevent recycling from terminating

- **Trigger:** zero layer scale on a tiled axis, spacing that cancels/exceeds the cell extent, or a non-finite stride.
- **Observable consequence:** the old count denominator clamped to `0.0001`, potentially creating tens of thousands of tiles for a normal small viewport. A zero/negative grid period could then make the recycling `while` loop fail to converge after camera movement.
- **Source:** baseline `ComputeTileCounts` guarded only the denominator; `gridWidthWorld`/`gridHeightWorld` retained the invalid stride used by `RecycleTiles`.
- **Fix:** reject non-finite strides or strides at most `0.0001` world units on enabled tiling axes before counts or allocation. Keep the source visible and permit a later camera rebind after configuration repair. Existing positive-stride overlap remains supported.
- **Regressions:** five `InvalidTilingStride_DoesNotAllocatePool_AndCanRecover` cases cover zero scale, zero/negative pitch, NaN and infinity, then assert successful recovery.

### P2: null-only variant arrays built invisible pools

- **Trigger:** null source sprite with a nonempty variants array containing only null entries.
- **Observable consequence:** generated renderers had no sprite and the source was hidden despite no usable visual content.
- **Source:** baseline `Initialise` checked array length before `BuildSpritePool` filtered nulls.
- **Fix:** validate the actual filtered sprite pool before building or hiding the source.
- **Regression:** `NullSpriteVariants_DoNotCreateInvisiblePool_AndCanRecover` observes the visible-source/no-pool state, then supplies a sprite and verifies real tiles appear.

### P2: stationary padding recycled repeatedly; teleports scaled work with distance

- **Trigger:** padding makes the pool period wider than twice the old camera threshold, or the camera moves multiple pool periods in a frame.
- **Observable consequence:** stationary outliers could move across the pool and immediately back every frame, consuming random choices and changing appearances. Large camera jumps required repeated per-period movement for every tile. Example: viewport width 4, unit stride and padding 2 give period 10 but threshold 3; X=-4 moves to 6 then back to -4 each update.
- **Source:** baseline `RecycleTiles` used independent forward/backward `while` loops with a viewport-derived threshold, even though padding increased the wrap period.
- **Fix:** choose a single whole-period displacement per enabled axis within the camera-centred pool interval. Move/randomize a changed tile once, combining both axis offsets. Work per live tile no longer depends on teleport distance.
- **Regressions:** `CameraTeleport_RecentresBothAxesWithoutChangingPoolSize` checks centred two-axis bounds and stable pool size; `StationaryCamera_DoesNotRerollPaddingTilesEveryFrame` checks unchanged sprites and unchanged Unity random state after settling, restoring test-owned random state afterward.

### P2: camera injection ignored disabled editor preview

- **Trigger:** call public `SetTargetCamera` on an active component with `generateInEditor = false` outside Play Mode.
- **Observable consequence:** hidden preview children were generated and the source was disabled despite the user's preview setting.
- **Source:** baseline setter called `Initialise` without the lifecycle methods' preview guard.
- **Fix:** apply the same preview policy to the public setter and release any previous pool. Also defer Inspector-driven Editor rebuilds outside `OnValidate` and cancel queued work on disable/destroy, avoiding synchronous Play Mode tile construction in validation callbacks.
- **Regression:** `SetTargetCamera_WithEditorPreviewDisabled_LeavesSourceVisible`. Deferred Editor/Play Mode callback timing still requires central integration verification.

### Documentation defect: zero multiplier description

The baseline tooltip/guide described zero as following the camera, while `UpdateParallaxOffset` adds `-cameraDelta * multiplier` to the initial layer position. Corrected the tooltip and guide to describe zero as world-stationary apart from auto-scroll. The established serialized multiplier behavior was preserved.

## Documentation

Updated the existing English canonical guide, `Assets/Neoxider/Docs/Parallax/ParallaxLayer.md`, rather than adding another module entry point. It now covers:

- Public camera injection, active/inactive operation, clearing/fallback behavior and rebuild origins.
- Scene authoring with root/child sources and the optional Inspector/UnityEvent route through the same typed API.
- Ownership, snapshot restoration, immediate tile retirement, deferred editor validation, valid sprite/stride requirements and bounded recycling.
- Actual multiplier/time semantics and explicit limitations for rotated/sheared hierarchies, pivots, zoom/aspect changes, perspective coverage and pool capacity.

Кратко: исправлены удаление чужих объектов, повторное применение трансформаций, потеря состояния шаблона и некорректная переработка тайлов. Публичный API и 15 сериализованных полей сохранены; Unity-проверки остаются за координатором.

## Substantial improvement proposal

### Proposed: explicit layout calculation and capacity-controlled configuration API (not implemented)

**Problem.** `ParallaxLayer` still combines viewport measurement, mutable camera/scroll state, pool allocation and rendering in one scene component. Camera injection is public, but layout configuration and explicit refresh are Inspector-only. Orthographic dimensions are sampled once; perspective uses fixed fallback counts. Arbitrarily small positive strides or very large padding can request excessive pools. Rotated/sheared spaces and dynamic viewport changes cannot be described or verified through a small layout contract.

**Architecture/public API.** Extract only deterministic layout decisions into a plain C# `ParallaxLayoutCalculator` and immutable `ParallaxLayout` result. Suggested surface:

```csharp
bool TryCreate(ParallaxLayoutSettings settings, Rect cameraBoundsInLayerSpace,
    out ParallaxLayout layout, out ParallaxLayoutError error);
Vector2 ParallaxLayout.GetWrapOffset(Vector2 tilePosition, Vector2 cameraCenter);
```

`ParallaxLayoutSettings` contains sprite extents, spacing, axes, padding and a maximum tile count. The result exposes validated row/column counts, pitch and total count. Non-finite extents, overflow or budget exhaustion return a typed error before any scene mutation. `ParallaxLayer` remains the optional scene adapter: it measures the camera footprint at the layer plane, creates/renderers and owns their lifecycle. Add explicit component methods `Configure(ParallaxLayerSettings settings)` and `RefreshLayout()` so ordinary C# has the same authoring path as the Inspector. Keep motion/scroll input explicit in the calculator-facing evaluation path so pure geometry checks do not require Unity callbacks or `Time.deltaTime`.

Use a validate-then-commit rebuild: evaluate the next layout and capacity first, then retire/swap the old pool. The caller can choose a documented fallback (retain the last valid pool or expose the source) after invalid configuration. Recalculate only when camera footprint or source geometry changes; no extra manager, singleton, or required NoCode object.

**Compatibility/migration.** Keep `ParallaxLayer`, `TargetCamera`, `SetTargetCamera`, all existing serialized field names and the inverse multiplier formula. Map existing Inspector fields into the settings value rather than moving/removing them. Preserve legacy perspective fallback behind an explicit mode while offering frustum-based coverage. Make the capacity policy and failure behavior explicit before imposing a new default on existing projects. Existing source renderers and authored hierarchies remain untouched by layout calculation.

**Acceptance scenarios.** Root/direct/nested axis-aligned sources and signed scales preserve measured dimensions; stationary padding makes no random choices; negative coordinates and million-unit teleports use bounded work; orthographic resize and perspective depth produce adequate coverage; invalid/non-finite or oversized requests fail before allocation; old serialized scenes preserve their current movement behavior; C# configuration and Inspector configuration produce equivalent layouts. Define the supported rotation/shear policy explicitly and test it instead of inferring it from lossy scale.

**Effort/risk.** Approximately 3–5 engineer-days including migration fixtures, Edit Mode layout regressions and Play Mode coverage on supported Unity versions. Medium risk: camera-plane projection and legacy pool sizing need visual/integration comparison; extracting a narrowly scoped calculator is lower risk than introducing a general background-rendering framework. No implementation of this proposal is claimed in this audit.

## Verification and remaining risks

### Actual checks

- Read the complete root `AGENTS.md`, worker contract, exact manifest entry and initial report; checked for more-specific instruction files on owned paths.
- Matched the complete source directory against the exact manifest inventory: **1/1 C# files**, no nested assembly or unreviewed owned source.
- Compared current source to baseline `ca198556` with a read-only Python inspection: **all 15 serialized field declarations and public API declarations unchanged**; no `var` in edited/new C#.
- Statically inspected the new fixture: **11 test methods / 18 parameter-expanded cases**, using public `SetTargetCamera`, enabled state and serialized Inspector authoring. The two frame scenarios dispatch Unity's existing `LateUpdate` message to observe renderer positions/sprites; no private fields/methods are accessed through reflection. This is test-source inspection, not a passing test execution.
- Verified the existing `Neo.Editor.Tests.asmdef` already references `Neo.Parallax`; no shared reference change is required. New test GUID `10dc6a4a6fcf4089936e9862875c1618d` has one `.meta` occurrence under `Assets`.
- Reviewed the scoped runtime/docs diff and checked baseline arithmetic for doubled root transforms and padding's non-convergent forward/backward behavior. Arithmetic examples establish the source-level mechanism; they are not Unity runtime evidence.
- Strict UTF-8, replacement-character, whitespace and source delimiter checks completed for the five owned changed/new files. Scoped `git diff --check` passed. New untracked text was checked separately because normal `git diff` excludes it.

### Central verification required / remaining risks

- **Compilation/import, console, Unity tests, Play Mode, screenshots and builds: not run**, as prohibited by the worker contract/user instruction. No claim that C# compiled or tests passed. Coordinator should run `Neo.Editor.Tests.Audit20261023NeoParallaxRegressionTests` (18 cases), existing Parallax tests and relevant integration checks.
- In Play Mode verify camera injection/rebind, disable/re-enable, immediate deactivation before deferred destruction, queued Inspector validation and stop/domain-reload cleanup. Verify sprite/material/sorting appearance and scaled-time scrolling in a real scene. The Edit Mode message driver cannot prove Unity callback timing or deferred destruction.
- Compile applicable Editor/player paths on Unity 6.0–6.4 and 6.5+; no new obsolete instance-ID usage or distributed version conditionals were introduced, but source inspection is not cross-version compilation.
- Existing design constraints remain: axis-aligned tiling and consistent pivots; orthographic zoom/aspect/scale changes need explicit rebuild; perspective uses fixed counts; no configurable capacity bound for huge positive requests. These are documented and addressed by the separate proposal, not presented as implemented features.
- Cross-boundary actions: **no external source/doc fixes or asmdef edits requested**. Only central validation is outstanding. Manifest/shared audit indexes remain untouched.
