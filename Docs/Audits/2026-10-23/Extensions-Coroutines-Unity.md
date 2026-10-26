# Extensions-Coroutines-Unity: additional deep audit

Status: full supplemental static review and source changes complete; central Unity compilation and execution remain unverified.

Model `gpt-6.1-sol`, reasoning `max`.

## Assignment

Extensions main owner finished source edits. Own only listed NONnumeric helpers, NEW tests Audit20261023CoroutineUnity*.cs, and public docs only Assets/Neoxider/Docs/Extensions/LifecycleAndOwnership.md. Deep review coroutine handle completion, external stop/owner destruction, race/reentry/disposal, global hosts, audio fades, cache disposal and Unity coordinate helpers. Preserve primary fixes. Numeric files belong to separate helper.

## Files

- `Assets/Neoxider/Scripts/Extensions/AudioExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/ColorExtension.cs`
- `Assets/Neoxider/Scripts/Extensions/ComponentExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/CoroutineExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/CreateFromMenuAttribute.cs`
- `Assets/Neoxider/Scripts/Extensions/DebugGizmos.cs`
- `Assets/Neoxider/Scripts/Extensions/DictionaryExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/EnumerableExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/Enums.cs`
- `Assets/Neoxider/Scripts/Extensions/GameObjectArrayExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/LayoutExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/LayoutUtils.cs`
- `Assets/Neoxider/Scripts/Extensions/LegacyComponentAttribute.cs`
- `Assets/Neoxider/Scripts/Extensions/NeoDiagnostics.cs`
- `Assets/Neoxider/Scripts/Extensions/ObjectExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/PlayerPrefsUtils.cs`
- `Assets/Neoxider/Scripts/Extensions/PrefabPreviewExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/ScreenExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/Shapes.cs`
- `Assets/Neoxider/Scripts/Extensions/StringExtension.cs`
- `Assets/Neoxider/Scripts/Extensions/TransformExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/UIUtils.cs`
- `Assets/Neoxider/Scripts/Extensions/UnityEventDelegateCache.cs`

## Scope and review method

All 23 assigned C# files above were read in full. This is a supplemental pass over the main owner's current working-tree implementation, rather than a replacement for the first pass. Existing main-owner fixes, tests and guides were preserved. The manifest's supplemental instruction restricts documentation ownership to `Assets/Neoxider/Docs/Extensions/LifecycleAndOwnership.md`; no other owner's guide or runtime source was edited.

Reuse receipt: read the `neoxider-tools` skill and its `references/extensions.md` coroutine/audio/component/layout/screen sections before adding implementation helpers. Reused the existing `CoroutineHandle`, runner, lifecycle tracker, `NeoDiagnostics`, `ObjectExtensions.SafeDestroy`, and Unity's `MonoBehaviour.destroyCancellationToken`. No new manager, assembly, package or scene component was introduced. The `unity-game-agent` workflow was adapted to the user's explicitly file-only audit: local review only, no child agents or independent agent QA, and central verification remains the coordinator's responsibility.

| File | Review coverage and disposition |
| --- | --- |
| `AudioExtensions.cs` | Null/fake-null source, immediate fade, scaled-time pause, fade-to-zero playback stop, concurrent fades, global-host lifetime. Source preserved; instant-completion and destroyed-source checks added through the coroutine fix. |
| `ColorExtension.cs` | Alpha/channel bounds, finite HDR inputs, alpha preservation and exact HEX shape. Fixed out-of-range channel conversion. |
| `ComponentExtensions.cs` | Get-or-add identity, hierarchy paths, null path behavior and destroyed objects. No additional demonstrated fix. `GetOrAdd` requires a live receiver. |
| `CoroutineExtensions.cs` | Every overload, global/null fallback, synchronous startup, inactive hierarchy, owner component destruction, nested iterator disposal, predicate faults, callback reentry, external stops, reset reentry and tracker cleanup. Fixed the paths described below. |
| `CreateFromMenuAttribute.cs` | Attribute scope, inheritance and normalized menu/prefab strings; no runtime scene dependencies. No change. |
| `DebugGizmos.cs` | Null targets/collections, filtered iteration, temporary color restoration on ordinary completion, expected gizmo callback context. No change; an arbitrary throwing enumerable can still interrupt restoration. |
| `DictionaryExtensions.cs` | Null dictionary/factory, existing-null values, factory exceptions and insertion, integer/float counter semantics. Single-threaded, ordinary arithmetic contracts preserved. |
| `EnumerableExtensions.cs` | Null/default policies, negative wrapped indices, lazy group/filter behavior and enumeration ownership. No additional demonstrated fix; helpers without null-safe contracts continue to require valid receivers. |
| `Enums.cs` | Serialized member order and time/screen meanings. No enum changes. |
| `GameObjectArrayExtensions.cs` | Null collections/elements, lazy versus eager operations, single traversal of combined bounds, parent changes, distance and active-state semantics. Main owner's bounds fix preserved. `GetActiveObjects` tests `activeSelf`; destruction helpers remain runtime operations. |
| `LayoutExtensions.cs` | All placement entry points, one-shot enumeration, null slots, collection counts and world-space output. Main owner's materialization fix preserved. |
| `LayoutUtils.cs` | Nonpositive counts, invalid column normalization, circle/grid/spiral/sine construction, zero/parallel directions and random sphere semantics. No additional fix. `GetSphereSurface` intentionally samples random points, without uniform-spacing guarantees. |
| `LegacyComponentAttribute.cs` | Attribute metadata, replacement normalization and menu hiding; no change. |
| `NeoDiagnostics.cs` | Default log gates, throttle timing/key storage, reset, exception gate and centralized Unity identity switch. No new identity calls or per-call version directives introduced. Thread safety and long-session throttle-key cardinality are not claimed. |
| `ObjectExtensions.cs` | Fake-null, Edit/Play destruction choice, explicit immediate destruction, names and validity. Reused `SafeDestroy` for owned preview Sprites. |
| `PlayerPrefsUtils.cs` | Persisted CSV/legacy JSON compatibility, null/empty/default handling, culture, malformed bool data and manual Save contract. Main owner's fix preserved; legacy codec ambiguity remains covered by its existing proposal. |
| `PrefabPreviewExtensions.cs` | Borrowed renderer/image assets, editor-only thumbnail fallback, generated Sprite ownership, editor/domain/play lifecycle and repeated lookup. Fixed generated native-object release. |
| `ScreenExtensions.cs` | All visibility/side checks, missing cameras, clipping versus viewport semantics, four-corner bounds, sub-rectangle cameras, offset and depth units. Main owner's bounds fix preserved; fixed placement coordinates and orthographic depth. |
| `Shapes.cs` | Public value-struct field/constructor layout and absence of scene references; unchanged. Radius validation remains a consumer policy. |
| `StringExtension.cs` | Null/empty transformations, truncation, parser defaults/culture, UTF-16 operations, random input assumptions and rich-text endpoints. Main owner's singleton-gradient fix preserved. UTF-16 reverse/character coloring are not grapheme-aware APIs. |
| `TransformExtensions.cs` | Optional-axis precedence, local/world conventions, reset/copy/hierarchy helpers, closest lookup and coroutine target lifetime. Preserved explicit-type edits; fixed smooth-look target destruction and coincident targets. |
| `UIUtils.cs` | EventSystem fallback, cursor/raycast allocations, explicit world projection, overlay/local coordinates and canvas camera selection. Main owner's coordinate fix preserved. Legacy-input dependency noted below. |
| `UnityEventDelegateCache.cs` | Replacement/moving slots, wrong-event unsubscription, Add/manual event ownership, null listeners, Clear and repeated delegate identity. Main owner's fixes and explicit non-unsubscribing Clear contract preserved. |

## Findings, fixes and evidence

All reproduction descriptions below are established by source inspection. New behavioral regression cases have been authored; none were executed by this worker.

### XCU-01 / P2 — completed or rejected starts could report running

**Trigger and consequence:** the old `StartDelayedCoroutine` assigned `handle.Coroutine = owner.StartCoroutine(...)` after startup, and that setter always set `IsRunning = true`. A synchronous wrapper completion could clear the handle and then be overwritten by that assignment; a native null/rejected start was also marked running. Inactive GameObjects reached Unity's invalid-start path. A zero-duration audio fade used the same path.

**Source/fix:** `CoroutineExtensions.cs`, `CoroutineHandle.Coroutine`, `StartDelayedCoroutine`, `WrapCoroutine`/`RunTrackedCoroutine`. Added terminal-state guarding, established the tracked running state before advancing, handled a null native result and startup exceptions, and rejected inactive hierarchies without executing actions. The wrapper now advances the actual root iterator directly, so immediate completion is recognized during startup. No artificial initial frame delay was added.

**Regression:** `ImmediateCompletion_DoesNotResurrectHandles` checks actual callback counts, `IsRunning`, native handles and the resulting audio volume for zero frames, already-satisfied repeat, a custom no-yield iterator and zero-duration fade. `InactiveHierarchy_CancelsWorkAndRejectsNewCallbacks` verifies no callback after rejection or hierarchy reactivation.

Unity explicitly documents synchronous execution and a null result for a coroutine completing without a yield: [StartCoroutine](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/monobehaviour/startcoroutine). Source reasoning is not a Unity execution receipt.

### XCU-02 / P2 — cancellation did not own iterator cleanup or fault completion

**Trigger and consequence:** the old wrapper only yielded the supplied iterator and relied on its own `finally`. `Stop()` called native `StopCoroutine` and cleared the handle, without explicitly disposing the supplied or nested iterators. Resources in nested `using`/`finally` blocks were not owned by the cancellation path. Nested/predicate exceptions were driven by Unity outside that wrapper, so handle cleanup depended on native unwinding behavior. Reentrant disposal could also act on an iterator currently executing.

**Source/fix:** `CoroutineExtensions.cs`, handle advancement/iterator registry and tracked wrappers. Track the yielded `IEnumerator` tree, release child before parent exactly once, clear terminal state before user cleanup, and defer disposal during `MoveNext`/`Current`. Ordinary yielded native instructions remain native instructions. A `CustomYieldInstruction` proxy preserves the native polling phase while capturing predicate faults and its optional `IDisposable` ownership. Custom iterator/predicate faults complete the handle and rethrow to Unity; cleanup failures use `NeoDiagnostics.LogException` and do not prevent remaining cleanup. Reset rejects replacement work scheduled from disposal callbacks while its helper is shutting down.

**Regression:** `Stop_DisposesNestedIteratorsOnceInReverseOrder` checks actual cleanup order/count and suppressed continuation. `Stop_FromInsideRoutineAndCleanup_IsIdempotent` checks callback/disposal reentry. `PredicateFault_CompletesHandleAndReleasesEnclosingIterator` expects the actual Unity exception and checks outer cleanup/state. `CustomYieldInstruction_IsDisposedOnCancellation` verifies a polled disposable wait is released once. Existing main-owner subsystem-reset coverage remains intact; the additional reset-reentry guard is source-reviewed rather than separately executed.

**Boundary:** independently started operations yielded as a native `Coroutine` remain independently owned. Raw external stop observability is still an open limitation, described under XCU-08.

### XCU-03 / P2 — hierarchy deactivation or component-only destruction stranded handles

**Trigger and consequence:** `CoroutineLifecycleTracker` lived on the GameObject and only implemented `OnDestroy`. Deactivation stops native coroutines without destroying the tracker. Destroying only the owner MonoBehaviour also leaves the tracker alive. The old code had no direct notification for either path and could retain a running handle indefinitely.

**Source/fix:** `CoroutineExtensions.cs`, `BindTracker`, `Complete`, tracker `OnDisable` and `CompleteAll`. Register Unity's owner destruction token before execution and dispose the registration at completion. On hierarchy deactivation, stop a snapshot of tracked handles; tracker destruction and global reset use the same explicit cancellation path. Merely disabling the owner component continues its coroutine, preserving Unity behavior.

**Regression:** `InactiveHierarchy_CancelsWorkAndRejectsNewCallbacks` uses an inactive ancestor while the child's `activeSelf` remains true. `DestroyingOwnerComponent_CancelsOnlyItsWork` destroys the component while keeping its GameObject, verifies a global callback still executes, and checks that disabling a different component alone does not cancel its callback.

References: [GameObject deactivation stops coroutines](https://docs.unity.com/en-us/engine/6000.0/manual/working-with-gameobjects/gameobject-fundamentals/deactivating-game-objects); [MonoBehaviour.destroyCancellationToken, Unity 6.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html). Both Unity version families still require actual compilation.

### XCU-04 / P2 — screen-edge placement ignored the camera viewport and orthographic near plane

**Trigger and consequence:** `GetEdgePosition` used global `Screen.width/height`, then passed those pixels to a potentially offset/split-screen camera. A camera with `pixelRect = (100, 150, 320, 240)` should place `Center` at `(260, 270)` before pixel offsets, regardless of display size. Orthographic conversion also passed `depth` directly while the public XML contract says distance from the near clip plane; perspective already added that plane distance.

**Source/fix:** `ScreenExtensions.cs`, `GetWorldPositionAtScreenEdge`, `GetEdgePosition`, `ScreenPointToWorldPosition`. Compute normalized edge positions through the camera's pixel rectangle, preserve pixel offsets, and use `nearClipPlane + depth` for both projections. The main owner's four-corner AABB correction is unchanged.

**Regression:** eight `ScreenEdge_UsesCameraPixelRectangleAndNearRelativeDepth` cases cover left/top-right/bottom-left/center, both projections, pixel offsets, non-origin camera transforms and non-default near/depth values. Assertions round-trip to observable screen pixels and measure forward world depth.

**Compatibility:** orthographic callers relying on the old implementation can pass `oldDepth - camera.nearClipPlane` to retain their exact former world depth. The documented contract now matches both camera modes. Cross-owner consumer `Tools/Move/ScreenPositioner.cs` uses this method; no consumer source or scene value was edited.

### XCU-05 / P2 — clearing preview cache leaked generated native Sprites

**Trigger and consequence:** editor fallback calls `Sprite.Create`, stores the generated native object in `CachedPreviewSprites`, and the reset hook only called `Dictionary.Clear`. No explicit owner released those Sprites, including repeated play transitions with disabled domain reload. Source renderer/image Sprites and AssetPreview textures have different ownership and must not be destroyed.

**Source/fix:** `PrefabPreviewExtensions.cs`, reset/cleanup hooks, `GetPreviewSprite`, new additive `ClearPreviewSpriteCache()`. Release only generated Sprites through existing `SafeDestroy`; register idempotent before-reload/editor-quit/play-exit hooks, retain existing subsystem/Unity 6.5 lifecycle attributes, and mark generated Sprites `HideAndDontSave`. The cache never destroys borrowed thumbnail textures or source assets. This introduces no package dependency or serialized field migration.

**Regression:** `PreviewCache_ClearDestroysGeneratedSpriteAndPreservesBorrowedTexture` checks repeated lookup identity, actual Unity destroyed-object state, surviving borrowed texture and replacement creation. `PreviewCache_ClearPreservesSpriteRendererAsset` checks source asset identity and lifetime. The thumbnail test depends on the Editor providing its GameObject mini-thumbnail; batch/headless behavior remains for central verification.

**Consumer contract:** clearing invalidates generated previews. Consumers must refresh them after cache clear; borrowed SpriteRenderer/Image assets remain valid. Play Mode destruction is deferred through Unity, while Edit Mode destruction is immediate.

### XCU-06 / P2 — smooth-look iterator accessed destroyed transforms or invalid zero direction

**Trigger and consequence:** the old iterator accessed `transform.rotation` after every yield and in its final assignment without a fake-null check. A globally hosted look operation outliving its target could throw. A target equal to the transform's position supplied a zero direction to `Quaternion.LookRotation`, producing an invalid rotation request instead of preserving orientation.

**Source/fix:** `TransformExtensions.cs`, `SmoothLookAtRoutine`. Reject absent/coincident targets, check lifetime on every continuation and before final assignment. Existing world-space rotation, interpolation and explicit-type fixes are preserved.

**Regression:** `SmoothLookAt_CoincidentTargetCompletesAndPreservesRotation` verifies actual unchanged orientation and completed iteration. `DestroyedAudioAndLookTargets_EndGlobalOperations` destroys both target types and checks that both global handles complete while the surviving audio owner GameObject remains valid.

### XCU-07 / P2 — finite HDR colors produced malformed HEX strings

**Trigger and consequence:** `ToHexString` formatted raw `channel * 255` as a minimum-width `X2` integer. `new Color(2f, -1f, 0.5f, 1f)` therefore emitted extra/sign-extended hex digits rather than an eight-digit RGBA color. Unity Colors commonly allow finite HDR channels.

**Source/fix:** `ColorExtension.cs`, `ToHexString`. Clamp each channel before the existing byte truncation/formatting. Ordinary `[0, 1]` output is unchanged; `0.5f` remains `7F`. No numeric helper owned by the other supplemental worker was edited.

**Regression:** two `ColorHex_ClampsHdrChannelsAndPreservesByteTruncation` cases check exact output, negative/above-one channels, alpha and successful public color parsing.

### XCU-08 / P2 — raw external coroutine stops remain unobservable (open limitation)

**Trigger:** start a long operation on a live owner, then call `owner.StopCoroutine(handle.Coroutine)` or `owner.StopAllCoroutines()` directly. Unity exposes no package-level completion notification for this escape hatch. The package can still retain its tracked iterator and report `IsRunning = true` until `handle.Stop()` or owner/hierarchy cleanup occurs.

**Disposition:** explicitly documented in the new guide and the handle XML summary; no claim of automatic observation was added. Replacing arbitrary native owner methods or polling inaccessible native coroutine internals would be incompatible/unreliable. Supported cancellation is `handle.Stop()`, including after an external stop. Central QA should record raw-stop behavior separately from the supported cancellation cases. The proposal below supplies a typed ownership path without promising detection that Unity does not expose.

## Documentation

Added the only supplemental-owned public guide: [LifecycleAndOwnership.md](../../../Assets/Neoxider/Docs/Extensions/LifecycleAndOwnership.md) (repository path `Assets/Neoxider/Docs/Extensions/LifecycleAndOwnership.md`). It explains typed C# usage, per-entry ownership, null fallback, inactive/disabled semantics, reentry/disposal, raw stops, audio fades, preview assets, UnityEvent teardown, coordinate spaces and scene/NoCode adapters. Includes a concise Russian summary and an explicit orthographic depth migration note. Source XML was updated at the changed public contracts.

Coordinator action: link the new Lifecycle and ownership guide from the existing Extensions README navigation. That README and other canonical guides belong to the primary owner and were intentionally not edited here.

## Substantial improvement proposal — explicit coroutine operation scopes

**Proposal only; not implemented.**

**Problem:** the current handle exposes a boolean and a raw native Coroutine. Callers cannot distinguish completion, cancellation and faults, and ownership policies are scattered across gameplay components. Global fades are independent, so every caller must implement replacement/cancellation correctly. Raw native stop calls cannot be observed reliably. Adding more implicit global behavior would weaken the library's composability.

**Architecture/public API:** add a small plain-C# `CoroutineScope : IDisposable` that owns only operations explicitly registered with it and accepts an injected execution adapter. Suggested API: `CoroutineOperation Run(IEnumerator routine, CancellationToken cancellation = default)`, `bool Cancel(CoroutineOperation operation)`, `void CancelAll()`, and optional `Replace(object key, IEnumerator routine)`. `CoroutineOperation` exposes `State` (`Running`, `Completed`, `Cancelled`, `Faulted`), `Exception Error` and the existing handle for interoperation. An optional `CoroutineScopeBehaviour` scene wrapper owns a scope and a documented component-disable policy. A Unity adapter uses this audit's handle/iterator cleanup path; the scope's ownership, replacement and terminal-state transitions remain testable without a scene. Audio adapters can opt into replacement by AudioSource key without making all fades globally exclusive. No ScriptableObject stores scene owners.

**Compatibility/migration:** additive types/overloads; existing extension signatures, native Coroutine property and default ownership behavior remain. Opt in per feature: replace hand-managed handle fields with a scope and cancel/dispose it at the feature boundary. Raw external stops remain explicitly outside observable scope cancellation; callers migrate stop sites to `operation.Cancel()`/`scope.CancelAll()`. Preserve scaled-time defaults and offer explicit scheduling/time options rather than silently changing them.

**Acceptance scenarios:** synchronous/no-yield completion; native wait phase preservation; nested and custom-wait disposal exactly once; cancel during callback or cleanup; owner component destruction versus disabled component/hierarchy; global operation surviving scene unload until its scope ends; scaled/unscaled pause; replacement of one keyed audio fade without cancelling another; fault state with original exception; idempotent scope disposal; domain-reload-disabled sessions without duplicate persistent hosts; the same state/output through C# and the optional scene adapter. Separate tests must prove the documented raw-stop boundary rather than infer native state from a non-null Coroutine.

**Effort/risk:** approximately 3–5 engineering days plus central multi-version Unity verification. Medium risk, concentrated in native wait scheduling, shutdown reentry and allocation cost at high operation counts. Start with the plain ownership/state core and compatibility tests; keep the scene adapter and keyed replacement optional. Profile before introducing pooling or a custom scheduler.

## Verification, remaining risks and coordinator actions

- **Authored:** `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityTests.cs` (13 cases) and `Audit20261023CoroutineUnityLifecycleTests.cs` (8 UnityTests), with unique `.meta` files. All tests inspect state/output, callback/cleanup counts, object identity/destruction or coordinates. The integration fixture uses `EnterPlayMode`/`ExitPlayMode` from the existing EditMode assembly; it cleans owned handles, objects and only newly created global helpers. No shared asmdef change was needed: `Neo.Editor.Tests` already references `Neo.Extensions`; `Neo.Tests.Play` does not.
- **Execution:** Unity compilation, all 21 authored cases, existing tests, builds, native-object profiling and Play Mode were **not run** under the worker contract. Actual scoped static-check results are recorded below after the final source pass. This is self-review only; no independent QA result is claimed.
- **Preservation:** the other 18 assigned source files were read and left as found. Numeric sources, primary tests/docs, all other owners' files, samples, package/settings/asmdef files and shared configuration were not edited. No agents, commits, installs, Unity/MCP calls, test runners or services were started.
- **Central verification required:** compile Unity 6.0–6.4 and 6.5+ branches, run both new fixtures plus existing extension/lifecycle cases, and inspect custom-wait polling and cancellation order under domain reload enabled/disabled. Existing Unity identity and `[OnExitingPlayMode]` branches are preserved, not compilation-proven by this worker. Check reset-cleanup reentry, cleanup exceptions, Play Mode deferred Sprite destruction and headless mini-thumbnail availability. Coroutine bookkeeping adds per-operation iterator tracking and destruction registration; allocation/performance cost was not measured.
- **Cross-owner consumer review:** `ScreenPositioner` uses corrected orthographic depth semantics; owners should review depth-enabled scene values against the migration note. Inventory preview consumers should reacquire generated Sprite references after explicit cache clears. No consumer edits were made.
- **Input compatibility risk, not reproduced here:** `UIUtils.cs:24` reads legacy `Input.mousePosition`; current `ProjectSettings.asset` uses `activeInputHandler: 2` (Both). A consuming project using only the new Input System needs explicit validation and an input adapter/pointer-position API. `Neo.Extensions.asmdef` references only `Neo.PropertyAttribute`, so adding a direct new-Input-System dependency is outside this worker's shared-config authority. A typed screen-position raycast overload plus an optional input-module adapter would avoid reflection and forced package coupling. No project input settings were changed.
- **Existing documented limits:** cache `Clear` does not unsubscribe listeners; shared delegate identity cannot represent independent indexed listeners; global fades can overlap and pause on scaled time; screen visibility is a viewport test, not occlusion; legacy string-array persistence and UTF-16 text operations retain their established formats/units. Non-finite timing/color values are not a newly supported contract.

### Executed static checks

Final results: static content/link checks returned exit code 0 with zero issues across 30 files; scoped `git diff --check` returned exit code 0; the three GUIDs each occurred exactly once. This is static evidence only, not compilation or test execution.

- Strict UTF-8 decoding, replacement-character, trailing-whitespace and explicit-type scans covered all 23 owned C# files, the two new test files, the new guide, this report and three metadata files (30 files total). An initial link scan caught a report-local Markdown example pointing to the wrong relative location; the example was replaced with prose and the final scan was rerun.
- Local Markdown links in the guide/report were checked against actual files. New test attributes account for 21 cases: 13 synchronous EditMode cases and 8 runtime integration cases.
- Scoped `git diff --check` covers all five changed runtime files, the new tests, guide and report. Untracked-file content is also covered directly by the static whitespace scan.
- A GUID search under `Assets` found exactly one occurrence for each new GUID: `31b9427e1e4b43f9b46d2d4f18741b20` (synchronous tests), `17745332817342ee9eab26cbb8c03120` (runtime fixture), and `59690097722d47da9180ed77e9d5634d` (guide).
- Scoped final status contains only the five owned runtime paths and the six new package files plus this report for this supplemental work; pre-existing primary-worker changes in those runtime paths remain part of the shared working tree.
