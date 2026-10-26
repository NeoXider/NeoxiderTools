# Extensions-Coroutines-Unity: implementation receipt

Status: **source-final for the released source3 caller correction; native closure DEFERRED**. Same original auditor/session, `demo` / `11.0.0`. Existing source edits, audit/plan and primary helper relocations are preserved. The current correction is UNEXECUTED; it does not close source3's independent coroutine failures by assertion.

## COROUTINE-POST-RELOCATION-NATIVE-CONTRACTS — current correction

Read the implementation contract/request, current three caller files, relocated phase/raycaster bodies and primary mapping receipt. Read the complete lifecycle and UI fixture XML nodes, including every case, failure, stack and output, from `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` (source3 XML SHA `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`). Actual historical results: lifecycle **13 passed / 6 failed / 1 skipped**, UI **2 passed / 2 failed**. These remain failures/skips of that frozen source, not results for this correction.

The failures have separate evidence:

- Custom-wait phase and fixed-frame cases report rejected Editor phase-probe attachment followed by null dereferences. Two positive UI cases similarly report rejected raycaster attachment; their output also rejects assigning an unregistered EventSystem.current.
- Self-stop, owner destruction and destroyed audio/look target cases report a still-running handle. The predicate-fault case reports its missing expected exception. None of these four failures depends on the phase/raycaster nulls. Their old bounded waits count a few `yield return null` iterations.
- End-of-frame was explicitly skipped for batch execution, even in this graphical run. It supplies no end-of-frame acceptance evidence.

Installed `EditModeRunner`, `TestEnumerator`, `EnumeratorHelper`, `BeforeAfterTestCommandBase` and `EnterPlayMode` source were read. Entering Play Mode does not change the EditMode test consumer into a native coroutine: its null yields still advance through Editor updates. A small number of those yields does not prove that the native coroutine, deferred Destroy or wait predicate has advanced. Mode transitions must also remain in the top-level test/setup/teardown enumerator, outside nested helpers. Installed uGUI BaseRaycaster/EventSystem registration contracts were inspected; production UIUtils does not own admission of inactive/unregistered test components.

Only these existing owned caller sources changed in this correction:

1. `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.cs`: retains direct top-level UnitySetUp EnterPlayMode and UnityTearDown ExitPlayMode; asserts Play Mode, captures/restores time settings, and establishes a live relocated runtime probe. Native barriers wait for the probe's actual Update **and** LateUpdate at `Time.frameCount + N`, with a two-second real-time deadline; no lifecycle method or PlayerLoop is invoked manually. Old one/two/three-tick waits and entry loops now wait for actual native frames, while self-stop waits for tracked completion plus a native replay barrier. Predicate failure is armed only after observed native polling, must poll again and emit the original expected exception, and must release its enclosing iterator exactly once. Deferred target/component destruction is explicitly observed. Existing terminal state, callback counts, disabled-component behavior and cleanup/order assertions remain.
2. `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.Acceptance.cs`: uses the same real barriers for entry, reset, replay and deferred Sprite destruction. Phase probes must attach from `Neo.Tests.Fixtures` and be active. Original WaitUntil/WaitWhile predicate phases, exact callback count two, fixed count/phase, raw-stop→tracked-stop cleanup order, MoveNext/Current disposal deferral and reset/cleanup-fault oracles remain. Additional observations require both wait callbacks in the satisfying predicate frames between Update and LateUpdate. End-of-frame retains its existing explicit batch capability branch, LateUpdate/same-frame checks, and now also observes exactly one callback without replay. Interactive Editor with an active rendering Game view remains required; Update/LateUpdate barriers do not prove rendering/end-of-frame support.
3. `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityUIUtilsTests.cs`: four cases now use supported top-level Play setup/teardown and UnityTest bodies. They require real native EventSystem admission and raycaster OnEnable registration in the public RaycasterManager list; they never add it manually. Exact original inside/outside coordinates, positive target hits, negative stale/target controls, returned count equality, independent lists, no-system zero/empty results and null-buffer parameter name remain. These bodies cover native component admission plus synchronous public coordinate/list dispatch; they do not claim input-module Update, device or rendering acceptance.

No files under Tests/Fixtures, asmdefs, aliases, GUIDs/metas, production sources/APIs, public guides, other owners' sources/reports or shared trackers/configuration were changed in this correction. The two moved helpers remain primary-exclusive in `Tests/Fixtures/Extensions`, with original namespace/type/GUID. Existing production patches were not repeated.

Callback/yield/finalization review: the existing handle completes before supported native Stop; Advance defers tracked disposal across MoveNext/Current, completes and rethrows original advancement/predicate exceptions; root finalization completes the registered nested tree before releasing its root. Owner destruction uses the existing native destroy token, while global audio/look operations observe target destruction on their subsequent native advancement. The owned native cancellation/cleanup/order oracles are retained rather than substituting a fake lifecycle or result. `CoroutineExtensions.cs` has no delta against frozen source3. No separately demonstrated production iterator defect can be isolated from the current failed waits without the corrected native run; no new runtime fix or exception/result substitution is claimed. Helper relocation alone does **not** close the nonhelper failures.

Current authored inventory remains **37 cases: 13 synchronous and 24 entering Play Mode** (20 lifecycle + four UI). This is source inventory, not discovery or acceptance. No Unity/compiler/test/checker/product validator/install/service/child/commit/push was run. Evidence for this correction is source/XML reading and manual source-diff review only.

Deferred closure belongs to root after **all writers finish and a new freeze**: compile/discover on installed Unity 6.3/6.6; rerun the complete owned fixtures, including the four independent coroutine failures, raw-stop/deferred cleanup and exact count/order controls; validate domain reload on/off, native phases and an interactive rendered end-of-frame run. Missing native admission/frames must fail their explicit preconditions. Any still-running handle, missing original predicate exception, phase shift or cleanup replay after valid barriers requires a bounded runtime follow-up using that actual evidence. The primary helper/shared-reference boundary and root's execution freeze remain dependencies of native verification, not claims of source acceptance. This actor stops at source-final.

The sections below retain the earlier approved implementation history; the current source3 correction and inventory above supersede their original fixture locations/setup counts.

## Published API — INPUT-POINTER-API (P2, approved-source-correction)

The following public methods now exist in `Assets/Neoxider/Scripts/Extensions/UIUtils.cs` (`Neo.Extensions`, assembly `Neo.Extensions`):

```csharp
public static List<RaycastResult> GetUIElementsAtPosition(Vector2 screenPosition);
public static int GetUIElementsAtPosition(Vector2 screenPosition, List<RaycastResult> results);
```

Both use `EventSystem.current` and never read legacy or new input. The caller-buffer overload clears the supplied buffer before querying, including when there is no EventSystem, and returns the resulting count. A null buffer throws `ArgumentNullException`. `PointerEventData` remains per-call: this removes the result-list allocation for callers retaining a list; it is not a zero-allocation promise. Legacy `GetUIElementsUnderCursor()` and `IsPointerOverUI()` remain available and retain their legacy mouse-input path.

`Neo.Tools.Input` can compose its existing `MouseInputCompat.TryGetPosition(out Vector3 position)` with this API, using a retained result list. No reference from Extensions to Input is introduced. The Input owner owns any upper adapter; real New-only behavior remains unexecuted during the source barrier.

## Native lifecycle review follow-up

Completed the bounded follow-up requested by the full native Extensions review, without extracting a scheduler:

- In `CoroutineHandle.ReleaseRoutine`, a wrapper finalized while `Advance` is executing marks the handle complete and leaves its iterators registered. `Advance`'s existing finally path drains them after `MoveNext`/`Current` returns. The direct release path no longer bypasses the deferred-disposal guard.
- In `RunTrackedCoroutine`'s root-finalization path, complete the tracked tree before releasing the root. If native cancellation finalizes a parent wrapper before nested wrappers, the registry still disposes child before parent. Later wrapper releases remain idempotent.
- Raw `StopCoroutine`/`StopAllCoroutines` observability remains an explicit compatibility limit. These source guards handle wrapper finalization when Unity actually supplies it; they do not infer native coroutine liveness. Supported cancellation remains `handle.Stop()`, including after a raw stop.

These are source-level guards for the reviewed native paths, **not a claim that their platform behavior has been executed or proven**. Existing XCU-01–07 changes, scaled independent audio fades, preview-cache ownership, coordinate migration, and public signatures/serialized fields are preserved. `CoroutineScope` remains a future proposal in the unchanged individual plan.

## Exact changed and added files

Runtime changes in this implementation phase:

- `Assets/Neoxider/Scripts/Extensions/UIUtils.cs`: additive coordinate/caller-buffer methods above; legacy cursor method delegates to them after its existing EventSystem guard and legacy input read. Existing canvas conversion correction is preserved.
- `Assets/Neoxider/Scripts/Extensions/CoroutineExtensions.cs`: the two narrow finalization/disposal guards above. No new coroutine host, scheduler, version switch, assembly dependency or public operation framework.

Regression sources, all within this owner's `Audit20261023CoroutineUnity*` prefix:

- Updated `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.cs`: made the fixture partial, captured/restored time scale/fixed delta, and made teardown cancel a snapshot while immediately stopping newly tracked cleanup work. Reentrant cleanup cannot mutate the list being enumerated. Existing eight cases remain.
- Added `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.Acceptance.cs`: 12 further runtime cases sharing the existing Enter/Exit Play Mode fixture.
- Added `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityUIUtilsTests.cs`: four synchronous coordinate/buffer cases. The fixture restores suspended EventSystems and destroys only its owned temporary objects.
- Originally added `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityPhaseProbe.cs` and `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityRaycaster.cs`: test-only native phase and EventSystem-dispatch drivers. Primary subsequently moved both to `Assets/Neoxider/Tests/Fixtures/Extensions` without changing their type/namespace/GUID; this actor did not repeat or alter that relocation.

Each of the four new C# files has a `.meta`. GUID reference enumeration under `Assets` returned its own single occurrence for each new GUID: UI fixture `0c8b951d0e2b4668a12c13992cbe3572`, acceptance partial `7b3cfff1aa1b4cbc839328f03d0f051b`, phase probe `20dee4576f374a14b4a2bbeaa91dc301`, raycaster `f3ecca1d50d940d4b3e8c49bef29a591`. Existing source/guide metadata was retained; no folder was added.

Public documentation changed only in `Assets/Neoxider/Docs/Extensions/LifecycleAndOwnership.md`: exact UI overloads, buffer clearing/count/null behavior, per-call allocation limit, C# example, optional upper input composition, native-finalization deferral and ordering, and the rule against serializing transient thumbnails. The existing orthographic depth migration note remains. English contracts and a Russian summary are present. Canonical README/UI guide integration belongs to primary `Neo.Extensions` and was not edited here.

## Authored acceptance — UNEXECUTED

The initial implementation authored 16 further cases beyond the original 21: **37 cases total** (then 17 synchronous, 20 runtime). The current source3 correction converts four UI cases to Play setup, giving the 13/24 split above. These are source counts, not discovery/execution evidence.

The four new UI cases observe explicit-coordinate routing through EventSystem, stale-result replacement and buffer reuse, independent returned lists, empty/cleared results without an EventSystem, and the null-buffer argument contract. A deterministic raycaster supplies real dispatcher output; no input-backend setting is changed.

The 12 new runtime cases cover:

1. Raw StopCoroutine and raw StopAllCoroutines followed by tracked Stop: no continuation, one cleanup per iterator, child-to-parent ordering, cleared public state. They do not require raw stop alone to update the handle.
2. Native cancellation inside MoveNext and inside Current, plus hierarchy deactivation inside Current: traces assert that Dispose occurs after advancement exits and only once.
3. Subsystem reset with cleanup attempting to start replacement work: zero replacement callbacks, completed replacement handle, no replacement helper creation. Reflection only drives the actual lifecycle hook and locates the real global owner for native cancellation; handle state is not synthesized.
4. An inner Dispose throwing: one diagnostic exception, parent cleanup still attempted, no cleanup replay.
5. WaitUntil/WaitWhile predicates between Update and LateUpdate, fixed-frame continuation after FixedUpdate and before Update, and end-of-frame continuation after LateUpdate in that frame.
6. Scaled versus unscaled delay while paused and after resume.
7. Play Mode preview-cache release: generated Sprite destruction is deferred; the borrowed thumbnail texture survives.

Runtime waits use bounded frame or unscaled real-time deadlines. The end-of-frame case explicitly reports Ignore in batch mode and requires a render-capable interactive Editor execution; a batch skip is not end-of-frame evidence. Thumbnail cases require the editor mini-thumbnail capability and must be assessed on the validation host. Time settings and owned hosts/objects are restored during fixture teardown.

## Cross-owner handoff and compatibility

- **Neo.Tools.Input / INPUT-POINTER-API:** both overloads are source-final and ready to consume. Compose the existing public `MouseInputCompat.TryGetPosition(out Vector3 position)` above Extensions; pass explicit pixel coordinates and a retained `List<RaycastResult>`. No reverse reference or shared asmdef edit is needed. The Input owner owns its optional upper adapter and legacy/Both/New-only/IL2CPP device acceptance. No New-only runtime failure or pass is claimed here.
- **Neo.Tools.Inventory / A-08:** persistent `_icon` must not acquire generated cache-owned thumbnails from `OnValidate`. Keep authored assets and acquire temporary fallbacks in the view during Bind/refresh. That consumer fix belongs to the Inventory owner; cache disposal was deliberately not undone. The Extensions guide now states the persistence boundary explicitly.
- **Neo.Tools.Move / ScreenPositioner:** retain explicit near-plane-relative orthographic depth migration (`previousDepth - nearClipPlane` to preserve former depth). The Move owner and root release migration guide own consumer documentation/integration; no serialized scene value was rewritten here.
- **Primary Neo.Extensions:** integrate the owned lifecycle/UI topic into canonical navigation when its documentation pass completes. No numeric source, primary regression fixture, other actor report, shared tracker/configuration, sample layout or package version was changed by this actor.

## Actual work/evidence boundary

Performed source/reference reads, manual source/diff review, branch/status reads, GUID generation and metadata/reference bookkeeping using small cmd.exe/Python operations. No Unity/MCP, compiler, test runner, product validator/checker, installer, service, child agent, commit or push was launched. Earlier static-check receipts were not reused as passes on this source.

After **all writers** finish, root's original verification operator must compile/discover/execute the frozen source on installed Unity 6.3 and 6.6, run the owned fixtures plus existing extension regressions, and exercise the domain-reload enabled/disabled matrix and render-dependent phases. Retained version branches, real New-only input, callback/native cancellation order, mini-thumbnail availability, deferred destruction and per-operation allocations remain unverified. No performance or zero-allocation result is claimed. Root alone owns later thorough integration/release validation and the subsequently authorized push.
