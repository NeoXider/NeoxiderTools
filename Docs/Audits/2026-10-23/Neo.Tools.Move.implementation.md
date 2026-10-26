# Neo.Tools.Move — implementation receipt

- Status: **SOURCE-FINAL — S3 mode/numerics/cursor source corrections complete on 2026-10-25; native validation remains OPEN**. No execution before all writers finish and a new global freeze.
- Actor: `neo-audit-1023-b07-07`, same audit/planning/implementation session. Initial handoff 2026-10-24; S3 recovery completed 2026-10-25.
- Target: existing `demo` / `11.0.0` approval.
- Scope: [implementation request](Neo.Tools.Move.implementation-request.md), following [IMPLEMENTATION-CONTRACT.md](IMPLEMENTATION-CONTRACT.md). Historical [audit](Neo.Tools.Move.md) and [plan](Neo.Tools.Move.plan.md) remain unchanged.

## Completed source work

The earlier audit's M01–M11 fixes, regression source and public documentation remain in place. This continuation adds the approved bounded fixes below; it does not repeat those patches or implement the larger design proposals.

| Request | Final behavior and source evidence | Regression source |
| --- | --- | --- |
| `MOVE-CURSOR-OWNERS` (P1) | `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs`: separate shared player/gate ownership records capture the first look/movement value. Every requesting controller registers even when another has already disabled that gate. Repeated calls do not duplicate requests. Release, hide, unregister and lifecycle cleanup remove only that controller; the last release restores the captured value, including an initially false value. Static scene reset clears gate records and local ownership sets. The existing cursor-state stack remains in use. | `Audit20261023NeoToolsMoveCursorOwnershipTests.cs`: both release orders, all initial gate-value combinations, independent look/movement requests, duplicate calls, hide, unregister/re-register, cleanup and destroyed-player references. |
| `MOVE-INITIAL-ANGLE` (P2) | `Assets/Neoxider/Scripts/Tools/Move/CameraRotationController.cs`: initial/entry Euler values select an equivalent full turn near the authored range. X=350 becomes -10 for -45..45, while unsigned/full-turn ranges retain equivalent values. Disabled axes do not consult their unused limits. Initial and stationary samples do not write a new rotation. | `Audit20261023NeoToolsMoveCameraDragTests.cs`: signed, unsigned, negative-turn and shifted-turn ranges; disabled-axis limits; first nonzero displacement. |
| `MOVE-DRAG-MODIFIER` (P2) | Same source: active drag is pointer available + button held + modifier satisfied. Every entry recaptures pointer baseline and pose, including modifier pressed after the button. Release, modifier loss, pointer loss, disable, focus loss and pause end the current drag idempotently. A zero-time sample refreshes the baseline. The internal sample method allows focused regression coverage without creating a new public input API. | Same fixture: transitions/event counts, modifier re-entry, missing pointer, disabled component, focus/pause callbacks, zero-time baseline, legacy sensitivity at two delta times. |
| `MOVE-3D-GROUND-CAPACITY` (P2) | `Assets/Neoxider/Scripts/Tools/Move/PlayerController3DPhysics.cs`: the per-player 16-entry overlap buffer doubles and retries while saturated. Only the current returned hit count is filtered. Own-hierarchy rejection, ground mask and `QueryTriggerInteraction.Ignore` remain intact; retained buffer tails cannot become ground. | `Audit20261023NeoToolsMoveGroundQueryTests.cs`: 16/48/80 self colliders plus foreign floor, actual `IsGrounded` and landed event assertions, complete native-query membership oracle, floor removal, mask and trigger policy. |

Additional assigned work:

- `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveRegressionTests.cs` now captures `UnityEngine.Random.state` in setup and restores it in teardown's `finally`, after owned-object cleanup. Force tests no longer leave their RNG changes behind.
- `Assets/Neoxider/Scripts/Tools/Move/ScreenPositioner.cs` has explicit tooltip/XML depth documentation. Its forwarding, defaults and serialized values are unchanged. New `Audit20261023NeoToolsMoveScreenDepthTests.cs` covers full XYZ projection for orthographic/perspective and both `Use Depth` modes, plus manual orthographic depth migration with a rotated camera and nonzero pixel-rect origin.
- The four initial implementation fixtures retain their `.cs.meta` files. I15 adds one Play fixture and its metadata; root supplied its shared assembly reference, confirmed below. No asmdef/shared test configuration was edited by this actor. No `[NonParallelizable]` attribute was introduced.

There are no known remaining confirmed source fixes from this approved request. This status does not assert compilation or runtime success.

## Root-accepted I15 corrections

Read the complete updated implementation contract/request and all 170 lines of `NativeImplementationReview-15.md`. The historical audit/plan and the independent review remain unchanged.

**I15-01 / P2 — surviving cursor after internal look restoration:** corrected in `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs`. The existing mode-specific cursor writes are factored into private `WriteCursorState(bool)`. After restoring a last look request's true baseline through the existing player setter, the current top controller writes its requested cursor state silently. This covers assigned-external A being disabled before its last look release while movement-only B remains top. Look restores true, movement stays false, and B retains visible/unlocked cursor authority.

The silent path neither invokes cursor events nor synchronizes/reacquires gates or binds players. First baselines and the explicitly assigned external owner are retained. Existing top-to-lower activation still uses the original `ApplyCursorState` notification; it is not duplicated by the silent repair. `PlayerController3DPhysics.SetLookEnabled`, serialized defaults and ordinary standalone auto-lock behavior were not changed.

Authored 21 additional Edit cases in the existing cursor fixture: 16 combinations of both show directions, both closure orders and all four look/movement baselines, using a newly created player's actual default `CursorControlEnabled=true`; four single-owner baseline controls; one standalone public-setter control. They assert actual cursor lock/visibility, public gates, ownership, exact unlock counts, repeated cleanup and unchanged external binding. The original 13 cursor cases remain intact.

Authored `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsMoveCursorOwnershipPlayTests.cs` and `.cs.meta` (`fc2567ec19944b55bb3d1c610eeaed4e`). Two UnityTest methods exercise 32 mixed-gate scenarios (directions × closure orders × native disable/deferred destroy × four baselines) and eight single-owner controls. They use actual `enabled=false` / `Object.Destroy` and frame yields, with no manual lifecycle callback delivery. Each creates/destroys its own hierarchy; teardown waits for owned destruction before restoring cursor/global state in `finally`. These are authored acceptance, not executed native evidence.

**I15-02 / P2 — drag fixture isolation:** the first statements of `Audit20261023NeoToolsMoveCameraDragTests.SetUp` now reset `_starts` and `_ends`, before object/controller creation or listeners. All 13 exact cases, assertions and teardown remain unchanged. No runtime drag change or replacement fixture was made for this finding.

Owned `CursorLockController.md` now documents silent internal reconciliation, independent restored look versus blocked movement, preserved external binding, existing lower-controller activation notifications and standalone behavior.

## S3 failure correction and Oct25 recovery

Approved scope: `MOVE-NATIVE-MODE-NUMERICS-CURSOR`, action-index groups E43/E44/E45 and the two Move Play methods. Root reported the preceding S3 actor attempt ended with **exit=1**; the raw process error was not supplied here. That attempt's partial edits were preserved. The older Oct24 source-final receipt did not establish S3 completion. This recovery read the updated contract/request, current reservation map, current files and evidence, completed source review and wrote this updated receipt. Read/edit/hash commands completed successfully; no execution result is inferred from them.

Read the exact assigned XML failures and action-index entries. Evidence paths below are relative to `Local/Audits/assembly-2026-10-23/`; hashes identify the supplied pre-correction run, not the current source:

| Evidence | SHA256 | Actual Move failures |
| --- | --- | --- |
| `unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` | E43: seven cursor Locked→None failures; E44: nine local-physics scene setup exceptions; E45: one orthographic depth readback assertion. |
| `unity/final/s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` | Both Move cursor methods failed at a Locked→None assertion. Their loops did not establish completion of all 40 scenarios. |
| `full-edit-failure-action-index.json` | `df467fde346b0a1f7eca09a82b972f2e7532b7274758981969400628b1770e97` | E43/C, E44/P, E45/U; native capability/arrangement assessment precedes runtime conclusions. |

**E44 / genuine local Physics2D world:** `Audit20261023NeoToolsMovePhysics2DTests.cs` now enters Play in top-level `UnitySetUp` before `SceneManager.CreateScene(LocalPhysicsMode.Physics2D)`. It asserts a valid world distinct from `Physics2D.defaultPhysicsScene`. Top-level `UnityTearDown` destroys only its owned body, awaits unloading its exact owned scene handle, records that release before leaving Play, then directly yields `ExitPlayMode`. No borrowed scene or global physics settings are reset.

All five force tuples and their exact ±2 X / zero Y velocity oracles remain. Configuration occurs while the owned object is inactive; native Awake binds the body and public `ApplyForce(2f)` is followed by actual local `Simulate`. Private Awake invocation was removed. The four rotation tuples `(X→0, Y→0, +Z→+1, −Z→−1)` are four explicit UnityTests with suffixes `PositiveX`, `PositiveY`, `PositiveZ`, `NegativeZ`, retaining the same 1e-3 angle tolerance. Each first proves native +Z rotation of one degree, resets only its owned body, then checks the assigned axis. Native `Time.fixedTimeAsDouble` must advance within a three-second unscaled bound before each simulation; this avoids treating an Edit runner yield as a fixed callback. No private FixedUpdate/Awake call supplies native evidence. All nine logical cases remain.

**E45 / projection readback:** `Audit20261023NeoToolsMoveScreenDepthTests.cs` still builds its independent expected XYZ using native `Camera.ScreenToWorldPoint` at `nearClipPlane + depth`, before calling Configure. It now compares the component's depth readback with `WorldToScreenPoint` of that independent native point. The supplied orthographic result was 7.75011063 for an input of 7.75, just outside the former 1e-4 inverse-projection assertion. The full XYZ distance limit and depth-readback limit remain **1e-4**, and preserve-world-Z remains exactly 17. No tolerance, serialized depth, camera configuration, projection implementation or preserve-Z semantics was changed. The five cases remain.

**E43 / cursor capability before ownership:** the 34 existing Edit cursor cases now use top-level UnitySetUp/UnityTearDown Play transitions. They request Game-view focus through the public Editor menu. Before any default-enabled case with an initial true look gate, and before the standalone auto-lock control, a direct native Locked/hidden → None/visible write/read control requires actual application and Game-view focus. Its failure is explicitly identified as `MOVE_NATIVE_CURSOR_CAPABILITY`; None is never accepted instead of Locked. Owned players use owned camera pivots and zero move overrides, avoiding writes to a borrowed main camera; unrelated pause/settings subscriptions are disabled only on those test instances.

The two original Play methods retain all 32 mixed-gate and eight single-owner scenarios, including public gates, default CursorControlEnabled=true, exact baselines, both orders/directions, external binding, repeated cleanup, cursor visibility/lock and exact unlock counts. Each scenario first runs a focused native write/read control, verifying both states immediately and across a real Play frame. One additional independent `NativeCursor_FocusedWriteReadCapabilityControl` UnityTest exposes the host precondition directly. No Ignore, expected-error whitelist, arbitrary None fallback, default change or broad runtime repair was added. Legacy explicitly invoked cleanup callbacks remain unit cleanup controls, not proof of native delivery; the Play methods still use actual disable/deferred destroy.

These S3 edits change four owned fixture sources only. Runtime APIs, serialized settings, all earlier I15 corrections and public module contract/documentation remain unchanged. The reservation map was checked before every edit; its current owner status is source-final and no Move fixture is reserved. No helper extraction/move, Fixtures source, shared reference, other owner's caller or tracker was edited. There is no deferred reserved Move work.

Current S3 source fingerprints, read on 2026-10-25 after preserving/reviewing the partial edits:

| Repository-relative source | SHA256 | Preserved meta GUID |
| --- | --- | --- |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMovePhysics2DTests.cs` | `5efaa8d9f9351ed8f3d4f5e8956ba5ead3b77889482124f90d779370a1b896a5` | `4f269b8e8f3d4d0e862f1de32551ea8d` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveScreenDepthTests.cs` | `c3f59e7f6b4a12603fc5feec6885f7200c3de0bb2fd976f72d7b476218328938` | `28e16c49896c48cea1d5156d7d3b4d30` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveCursorOwnershipTests.cs` | `1eba89fc48b8de44904672fb35dbe85d28ca76349d6d78b62730c985197ac92f` | `3672f799695349b38924e3acb4b003c3` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsMoveCursorOwnershipPlayTests.cs` | `8ac2921ee199a4e3e6e15e1fc8e019238bc2041d296f9db9a1954d9a035ec98d` | `fc2567ec19944b55bb3d1c610eeaed4e` |

## Regression-source inventory

Edit paths below are under `Assets/Neoxider/Tests/Edit/Tools/Move/`; the Play path is under `Assets/Neoxider/Tests/Play/`. Counts describe authored test cases/methods, not discovery or executed results.

| Fixture | Cases | State |
| --- | ---: | --- |
| `Audit20261023NeoToolsMoveRegressionTests.cs` | 30 | Retained audit regressions; RNG cleanup added here. |
| `Audit20261023NeoToolsMovePhysics2DTests.cs` | 9 | Five force cases plus four explicit native rotation UnityTests; owned local 2D world after a real Play transition. |
| `Audit20261023NeoToolsMoveCameraDragTests.cs` | 13 | New source. |
| `Audit20261023NeoToolsMoveCursorOwnershipTests.cs` | 34 | All cases/oracles retained; Play transitions and focused native lock preconditions added. |
| `Audit20261023NeoToolsMoveGroundQueryTests.cs` | 4 | New source. |
| `Audit20261023NeoToolsMoveScreenDepthTests.cs` | 5 | New source. |
| `Audit20261023NeoToolsMoveCursorOwnershipPlayTests.cs` | 3 | Original two methods / 40 internal scenarios retained; one independent S3 native capability control added. |
| **Total** | **98** | **All prior 97 logical cases/oracles retained; one S3 capability case added. Current corrections are unexecuted.** |

The cursor fixtures restore cursor visibility/lock and the global cursor-management flag. Other new fixtures destroy their own objects. Direct delivery of private lifecycle callbacks in the retained Edit source verifies cleanup logic only; it is not evidence that Unity delivered native callbacks. The new Play source authors native delivery separately.

## Public documentation and compatibility

Updated owned pages under `Assets/Neoxider/Docs/Tools/Move/`:

- `CursorLockController.md`: first/last ownership, nested-panel setup, C# example, separate gates and snapshot limitation.
- `CameraRotationController.md`: scene/C# setup, equivalent angles, balanced transitions and unchanged legacy sensitivity.
- `PlayerController3DPhysics.md`: complete ground-query behavior, reusable buffer growth and self/mask/trigger policy.
- `ScreenPositioner.md`: scene/C# use, pixel-rect origin, full 3D depth contract and explicit 11.0 migration.
- `README.md`: entry links to the updated contracts and depth migration.

No public signatures, serialized field names, enum values, prefab/scene values or assembly references were removed or migrated. Drag sensitivity remains **pixels × mouseSensitivity × axis speed × scaled deltaTime**, retaining existing tuning and frame-rate dependence. Buffer growth allocates on saturation and retains that capacity per player.

Cursor ownership captures a baseline; an external `SetLookEnabled` / `SetMovementEnabled` call during an outstanding request does not redefine it. The last release restores the first captured value. A general scoped/base-intent input API remains a proposal, not a delivered contract.

## Dependencies and reuse

- Reused `MouseInputCompat`, `KeyInputCompat`, `AxisRotationSettings.ClampAngle` and existing player gate setters. No replacement input framework or public gate manager was added.
- Read current `Assets/Neoxider/Scripts/Extensions/ScreenExtensions.cs` as dependency context. Its projection uses `Camera.ScreenToWorldPoint(..., camera.nearClipPlane + depth)`; depth is **camera-space Z beyond near clip**, not distance along a perspective ray. Its existing three-argument screen-edge API satisfies Move's call site. No Extensions edit or outstanding API blocker is required from this worker.
- To retain an old orthographic camera-relative depth, the documented manual conversion is `newDepth = oldDepth - camera.nearClipPlane`. For an existing world point it is `camera.WorldToScreenPoint(oldWorldPosition).z - camera.nearClipPlane`. Matching 2D XY or restoring world Z alone does not prove full XYZ compatibility. No automatic asset rewrite was performed.
- `Neo.UI` pause-page behavior and nested assemblies remain with their owners. No UI/Move coupling, shared configuration or tracker edits were made. The concurrent modification of legacy `TransformMoveTests.cs` was left untouched.
- MotionMath extraction, a public scoped-input API and a frame-independent drag option remain unimplemented proposals from the plan; none is needed for these bounded corrections.

**Resolved integration dependency:** root added the exact **`"Neo.Tools.Move"` entry to the `references` array** in `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`. This actor read the file and confirmed that entry on 2026-10-24. Existing NUnit/TestRunner references and UnityTest support were present. No runtime API change or test patch was needed solely for that dependency, and this actor did not edit shared configuration. No known source-integration dependency remains for the 95 Edit-suite cases and three Play methods; compilation/discovery of the current corrections remain unexecuted.

## Actual review and verification debt

Performed under the writer barrier: UTF-8 source/document reads, scoped reference searches, scoped `git status`, runtime/document diff inspection, and manual review of new regression source. In particular, the ground regression uses a complete native-query oracle rather than depending on overlap ordering, and screen-depth assertions compare full 3D positions. For I15, read the current release/lifecycle/player-setter paths and shared Play asmdef; reviewed the silent write path and the new default-enabled/public-state/event assertions as source. No product-checker result is claimed.

The earlier dependency closure read the then-current contract/request, receipt and actual shared asmdef reference. Only this receipt changed in that earlier resumption; the I15 corrections were preserved. S3 correction/recovery evidence is recorded separately above. The installed test-framework source was also read to confirm supported top-level UnitySetUp/UnityTearDown transitions and distinguish Edit enumerator updates from actual native fixed-loop advancement.

**This actor launched no Unity, compiler, build/test runner, product checker or validation script during source correction/recovery.** The supplied S3 operator XML contains actual failures of the earlier snapshot; it is not a pass of this corrected source. No installs, services, children, commits or push were performed. No new test pass count is claimed.

After all writers finish, the coordinator must compile and execute against the frozen supported Unity/input matrix, including the planned 6.3/6.6 coverage. Observable acceptance still owed:

1. Compile the changed runtime and all seven fixture files with the installed Unity NUnit/API surface; discover/run all 98 authored cases/methods together and confirm cleanup/RNG isolation. Verify all nine Physics2D cases enter genuine Play, use a distinct local world, deliver native positive rotation/force results, and unload the exact owned scene before top-level ExitPlayMode. Run all 13 CameraDrag cases in one fixture lifetime to verify I15-02.
2. **Required GUI evidence:** in an isolated interactive validation host on supported Unity 6.3/6.6, keep the application and Game view focused in Play. Run `NativeCursor_FocusedWriteReadCapabilityControl` and capture successful Locked/hidden and None/visible native readbacks, including the next frame. Then run all 34 Edit-suite cursor cases through their Play transitions and both Play ownership methods through all 40 scenarios. Preserve default player cursor control, exact look/movement baselines, owner/binding/order/count and standalone auto-lock assertions. If the host cannot demonstrate the capability, report its exact focus/write/read failure and retain ownership/auto-lock as unverified; do not Ignore it or accept None in place of Locked. The supplied batch artifacts do not establish this capability, and the seven Edit/two Play failures are not yet proven runtime defects or proven fixed native outcomes.
3. With real supported input backends, press the modifier after mouse-down, release/re-enter it, lose the pointer/focus, pause and disable/re-enable. Each active interval must produce one start/end, no entry jump, and unchanged legacy sensitivity. Callback invocation in source alone does not satisfy this acceptance.
4. Exercise dense 3D self-collider geometry: foreign ground must be detected beyond the original capacity; self-only, masked-out and trigger-only overlaps must stay ungrounded. Check landed-event transitions and retained capacity after the first saturated probe.
5. Re-run all five depth cases: retain full XYZ agreement within 1e-4, independent native projection/readback agreement within 1e-4 and exact preserve-Z assertions. Check orthographic/perspective rotated cameras and nonzero pixel rectangles. Manually migrate representative existing depths; source tests do not establish project-wide asset parity.

SOURCE-FINAL is based on this Oct25 S3 source review/recovery and current fingerprints, not the older receipt or the failed actor invocation. No known source-integration dependency or reserved Move edit remains. Compilation/discovery, local-physics lifecycle/cleanup, projection readback and especially focused GUI cursor capability/ownership remain native evidence debt until a new coordinated freeze and execution pass. Source writes stop here.
