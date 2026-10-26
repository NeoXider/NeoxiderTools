# Neo.Tools.InteractableObject — implementation receipt

Status: **owned P5 source-final; corrected acceptance UNEXECUTED; no pending owner API/reference dependency**.
P5 bounded delivery finalized 2026-10-25 in the SAME original `b07-03` auditor/session. Target: `demo` / `11.0.0`,
base `ca198556`; no branch switch, commits or pushes performed.

Authority: `IMPLEMENTATION-CONTRACT.md`, this assembly's implementation request and
`INTERACTION-QUERY-DESIGN.md`. The later approved raw-query design supersedes the historical
mandatory-controller proposal. The original audit and plan remain unchanged as historical evidence.
Resumed after the resource interruption; retained the existing implementation. Fully read the current
contract/request and all 155 lines of `NativeImplementationReview-6.md`; implemented the root-approved
I6-01/I6-02 corrections and conservative E6-01 exact-argument preservation below.

## Current P5 handoff — P5-02/03/04/05

Read the complete current implementation contract/request and
`Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`. Preserved the prior S3 patches,
three-fixture Play relocation and GUIDs, original audit/plan and every historical native receipt.
This section supersedes the execution/dependency status of the retained S3 handoff below.

### Actual P5 evidence

Read this owner's actual case identities, messages, complete failure stacks and outputs in
`Local/Audits/assembly-2026-10-23/unity/final/p5/`, including both installed-version results:

| Receipt | SHA256 | Owned outcomes |
| --- | --- | --- |
| `u63/Receipts/21-full-play-graphics.xml` | `7ae7f99a5f648cf31a86a445bdd59761bfc3390d4bcf9d4d3e1371a6e86d681c` | 112: 101 passed / 11 failed / 0 skipped |
| `u66/Receipts/21-full-play-graphics.xml` | `73d18c2935644dd92c8fecd6cd1415c5d8f9443b9f0fd3a7afadf2ce9974cc69` | 112: 101 passed / 11 failed / 0 skipped |

These executed the earlier frozen P5 source, identity
`956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`, under
`unity/snapshots/final-20261025T151934Z/source`. They do not execute the corrections recorded here.

- All SIX complete camera 2D/3D × 1/100/1000 cases **passed**, including positive hits, actual first
  miss/exits and the separate next stable miss. Positive and stable-miss frames have native1,
  cacheN-1, FrameAdvances1 and callbackBoundaries0. Positive classification visits are 1/10,000/
  1,000,000; stable misses visit zero hits. Transition callbacks remain real: camera N=100/1000
  transition frames logged nativeN and callbackBoundaries2N. Query counts are not CPU/allocation gains.
- Two reentrant-hover variants failed final IsHovered=false without proving an initial false state
  or that the newly installed enter observer actually ran. The separate native
  `NativeDisableFromHoverEnter_DeliversOnlyTheFinalFalseNotification` passed and is unchanged.
- Scene setup failed exactly-one raycaster after Unity rejected BoxCollider2D beside BoxCollider
  on one GameObject. This is an invalid fixture graph, not proof of a scene-setup runtime defect.
- All six keyboard matrix cases passed their initial native key-down and accepted consumer state.
  N=1 then failed callbackBoundaries0 with actual1; N=100/1000 failed native1 with actualN. The
  source overwrote the whole component with JsonUtility after CreateTarget nulled its events.
  Inline-event reconstruction is a supported setup explanation, not a measured P5 field-nullness
  result; the new setup/consumer-frame assertions and counters must establish the boundary.
- Both explicit 2D trigger cases returned 0 with global triggers=false/filter useTriggers=true,
  rather than their assumed 1/17. Production forwards the exact filter to native array/list calls.
  P5 did not compare those native overloads under that state, so it does not prove a runtime defect.

### Bounded corrections and observable acceptance — UNEXECUTED

- `InteractionTests`: move the target off the camera ray, sync transforms, yield a real PlayerLoop
  frame and require IsHovered=false before installing the reset observer. Move it back, yield native
  automatic hover, require that enter actually occurred, then retain exact enter→exit→changed:False
  and final false for both hover-off and component-disable variants. No manual lifecycle/Update.
  Scene setup now activates separate legal 3D/2D targets on both old and replacement cameras;
  exactly one real PhysicsRaycaster and one Physics2DRaycaster are still required on each camera.
- `PlayerLoopTests`: replace only the checkObstacles JSON overwrite with the existing fixture-local
  field setter. Require all eleven event objects to be null at setup and again after each positive
  consumer frame. Empty non-null UnityEvents still enter real callback boundaries; no runtime
  invalidation or observer dispatch was removed. Emit existing counters before stable assertions
  so a failed oracle retains native/cache/callback/classification evidence. All 12 identities,
  real keyboard press/release timing, accepted-down/hover states, actual first miss/exits, separate
  stable miss, native1/cacheN-1/FrameAdvances1/zero-callback and N×H oracles remain unchanged.
- `SharedQueryTests`: both disputed cases now obtain complete direct native array **and** List
  references under the same origin/direction/distance/filter/global state. Require exact count,
  collider identity, distance and point parity, then compare backend cold, same-epoch cached and
  next-epoch warmed results. Cold 17-hit positives still require two filtered native calls and one
  full fallback; warmed positives require one call/no fallback, cached results zero calls/one hit.
  Native reference calls are outside backend counters and are not presented as aggregate savings.
  The global=false/filter=true trigger combination uses that exact native reference, instead of
  presuming the filter overrides Unity. This is not a new promise that it includes or excludes hits.
  Hard 1/17-hit trigger positives with both flags allowing hits, solid positives with global triggers
  disabled, filter-trigger exclusions, zero-layer-mask negatives and disabled-mask positives prevent
  empty-reference agreement from passing the fixture. A native array/List disagreement, missing
  positive or backend mismatch remains a failure. Every flag is restored by the existing teardown;
  the backend must also leave global trigger policy unchanged. No gameplay/global-policy workaround.
- `SharedQueries.md`: clarify unchanged native filter forwarding, the component/global 2D trigger
  limitation, observer boundaries and the direct-native positive/negative measurement controls.

No production C#, public/serialized API/default, mask/tag/trigger/range/LOS policy or event-order
change was made in P5. Dense 17/64/1000 tests in all four query kinds, original-planar cold/warm
parity, different camera/viewport/masks, distinct LOS, blocker negatives, callback mutation/fault,
real tap/down-up, click/double-click and native lifecycle controls are retained. InputDriver,
LifecycleTests, PhysicsEventsTests and primary-owned legacy/helper files are unchanged. No manual
InputSystem.Update, fake input/physics, new Ignore, expected-error mask or Editor API in Play.

### Current P5 source pins and remaining proof

Changed test paths below are relative to `Assets/Neoxider/Tests/Play/Tools/`:

| File | SHA256 |
| --- | --- |
| `Audit20261023NeoToolsInteractableObjectInteractionTests.cs` | `c3c694f861e6cf9d82afdefee2059e45ddc579b5a1a4d0014d40822b63da0a5e` |
| `Audit20261023NeoToolsInteractableObjectPlayerLoopTests.cs` | `86d8f5445aab2f532182bf65aec7d61b81d049aba5d21d64845f450497538d81` |
| `Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs` | `b9b07706131eb525120dcb9040fa70d86236a364ccf7b9ebb1ec71a540704a19` |

Changed guide: `Assets/Neoxider/Docs/Tools/InteractableObject/SharedQueries.md`, SHA256
`a634e35ba969f97b5dda47798eaa8fe7d4c9f2a7834c227bb0a11789be77e3e3`.
Unchanged production, driver/lifecycle/relay test and moved-metadata hashes retain the S3 pins below.
The explicit old/new `.cs`/`.cs.meta` name/GUID map below remains valid.

The former Play→Core reference dependency is **resolved by primary integration**:
`Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` now names `Neo.Tools.InteractableObject.Core`,
observed SHA256 `2061bece0f06e2e646be1ae5d02e8c43a8cc83529faa62c3d495893ad3200007`.
This actor did not edit that shared file or nested Core. Relocation reservations were consulted;
these prefixed fixtures remain unreserved. No new owner API is pending.

Actual work: existing XML/source reads, source diff review against frozen P5, the four scoped
source/guide edits above, this receipt and artifact SHA256 bookkeeping. **No Unity, compiler,
test/discovery runner, product checker, install/service/process control, other agent, commit or
push was run.** No foreign Unity project, shared tracker/configuration, Samples~ or DocsEn.meta edit.
The current patch is source-final, not a native pass. After all writers finish and root freezes the
new source, rerun all retained fixtures and all 12 matrix cases on both installed versions. Require
real enter setup/reset sequencing, both cameras' raycasters, observer-null keyboard stable counts
and all direct native array/List/filter positives/negatives. Any native filter/cache mismatch still
requires actual evidence and root review before a production change. CPU/allocation, external
EventSystem query accounting and legacy hardware/remote admission remain separate evidence debt.
Owned writes stop with this handoff; root retains the freeze, execution and later authorized push.

## Historical S3 handoff — NI42-01

Read the complete current implementation contract/request and all 72 lines of
`NativeImplementationReview-42.md`, SHA256
`0446912973233b31a7627e8a193490882d4e6a9d93d8617e3867383b8e73374d`.
The earlier S3 attempt terminated with exit1. Its partial edits were retained. The old 253-line
receipt (`ee62cac2c9e789aa9ad6ed79eafbb29e642dde77f9087bdf0ac43a20dea7fc2a`) was byte-identical
to frozen source3 and did not establish S3 completion. This current handoff supersedes its stale
path/execution statements; the original audit, plan and frozen execution receipts remain untouched.

### Actual frozen evidence, not corrected-source results

Evidence root: `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/`.

- `21-full-play-graphics.xml`, SHA256
  `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`:
  own 33 cases = **2 passed / 31 failed / 0 skipped**. The passed cases are native hover-enter
  disable ordering and disabled-target manual API gating. Six camera full cases failed at the
  first miss transition, six keyboard matrix cases failed their warm-up input positive, and
  19 input/lifecycle cases failed before the later reset/revalidation acceptance could be established.
- `22-full-edit-graphics.xml`, SHA256
  `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`:
  own three fixtures contained **79 cases: 42 passed / 37 failed / 0 skipped**; nested Core is excluded.
  Failures include forbidden Edit-mode SendMessage/ShouldRunBehaviour, scene-Awake arrangement,
  and explicit 2D filter positives returning 0 instead of 1 / 17. These do not establish a bounded
  production defect. Dense 17/64/1000 raw queries and original-planar direct-native parity had
  passing frozen slices; their new Play admission remains unexecuted.

All SIX camera **positive stable slices** passed before their full cases failed at the first miss:

| N | Dimensions, independently | Native calls | Cache hits | Classification passes / raw hit visits |
| ---: | --- | ---: | ---: | ---: |
| 1 | 2D and 3D | 1 | 0 | 1 / 1 |
| 100 | 2D and 3D | 1 | 99 | 100 / 10,000 |
| 1000 | 2D and 3D | 1 | 999 | 1000 / 1,000,000 |

The old N=1 first miss had callbackBoundaries2; N=100/1000 first misses executed N casts as hover
exits ended epochs. This is transition work, not evidence that positive stable camera reuse failed.
Counts prove neither CPU nor allocation speedups: target-local N x H work remains explicit.

### Preserved and finalized candidate

- All 12 actual PlayerLoop camera/keyboard-look 2D/3D 1/100/1000 cases remain. First moved-off-ray
  frame asserts every false state and exactly one camera hover exit per consumer (zero keyboard
  exits), then logs transition counters. Only after observers settle are counters reset for the
  next unchanged camera miss frame. Keyboard is rearmed through a real release/press. Stable
  oracles remain native1/cacheN-1, FrameAdvances1, zero callback/fresh/LOS/fallback work, N local
  classifications and zero hit visits, with no repeated exit. Callback/world-mutation controls remain.
- Driver still only queues real Input System states. Temporary test-owned IgnoreFocus,
  AllDeviceInputAlwaysGoesToGameView, Dynamic mode and runInBackground are saved/restored alongside
  the legacy-unavailable latch and previous current devices. Only its two devices and onAfterUpdate
  subscription are removed. Installed SDK source supports that focus-routing arrangement; it does
  not prove this was the actual source3 input failure cause or that native delivery is now fixed.
- A bounded six-snapshot diagnostic records actual update type/Dynamic frame, queued frame/state,
  owned/current device identities and enabled/added/background state, raw held/down/up versus
  compatibility reads, focus/settings and consumer-frame assertions. Positive assertions remain
  hard failures. Released warm frame, real simultaneous tap down/up, normal single/double-click,
  reopen/reset, aim/range/input-flag revalidation and callback order/count controls remain intact.
  No manual InputSystem.Update, fake input/physics, new blanket ignore or expected-error masking.
- The three owned fixtures below were moved into Play with their metadata. Interaction uses
  inactive complete-object assembly, one native activation, real enabled setters and yielded
  PlayerLoop frames where lifecycle is required. Public pointer-interface cases remain public API
  controls. PhysicsEvents' explicitly sent callbacks and SharedQuery's synchronous private
  Update/ray controls are **unit/source controls, not native automatic or collision acceptance**.
  The separate 12-case PlayerLoop matrix supplies automatic acceptance. Dense, masks/layers/tags,
  global-versus-explicit triggers (including positive1/17), unique LOS, blocker negatives, both
  camera raycasters, original planar arguments and callback mutation/fault oracles are retained.
- Finalization found one test-only incomplete move beyond NI42's pinned candidate: SharedQuery
  still referenced `SerializedObject` after its UnityEditor import was removed. Replaced its two
  editor-only configuration sites with fixture-local field assignment, assembling the explicit
  collider/configuration on an inactive object before one real activation. No oracle or runtime API
  changed. This small owned delta needs renewed source review; NI42's former SharedQuery hash is historical.
- `SharedQueries.md` had a stale Edit-mode fixture description. It now distinguishes transition
  versus next stable miss and unit controls versus actual PlayerLoop acceptance. No production C#,
  serialized/default setting, layer/tag/trigger/range/occlusion rule or callback invalidation changed.

### Exact three-fixture source/metadata map

All paths in this table are relative to `Assets/Neoxider/Tests/`. Each arrow includes the `.cs`
**and** `.cs.meta` file. Class basenames are unchanged; namespace changes from `Neo.Editor.Tests`
to `Neo.Tests.Play`. The six old current Edit paths are absent; each moved metadata file is
byte-identical to its frozen original and retains its GUID/importer.

| Old files | Current files | Preserved GUID | Identical old/current metadata SHA256 |
| --- | --- | --- | --- |
| `Edit/Tools/Audit20261023NeoToolsInteractableObjectInteractionTests.cs` and `.cs.meta` | `Play/Tools/Audit20261023NeoToolsInteractableObjectInteractionTests.cs` and `.cs.meta` | `ccb667d691cd4a2984308b2696f920f1` | `e52b20865bdbad935382b8aab4be16c796f01dcd613f9d75db72bed740a57f33` |
| `Edit/Tools/Audit20261023NeoToolsInteractableObjectPhysicsEventsTests.cs` and `.cs.meta` | `Play/Tools/Audit20261023NeoToolsInteractableObjectPhysicsEventsTests.cs` and `.cs.meta` | `54d254428398464ebfbd543cf8c278a2` | `59d8642ba142560b0d1bdace27928d06693afdf1d93483268d13160b69848d0f` |
| `Edit/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs` and `.cs.meta` | `Play/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs` and `.cs.meta` | `7b95be83bfcd4a4ea478dda4e7f40d96` | `401fe8ca85e933c10ff1eec9461fe0c65e24943b4b39d985ef86dd5ee06f866b` |

Frozen old source SHA256, in the same row order:
`a51994352c18c83f9dd3cc856cd0fae46449ea59b4512e6ec41930e8ff03f222`,
`a1fb3dcea2af450154ac3e666fa653003d4c53157112a2b6a29076f45035925b`,
`5095512a8cba91b5b7f7db8429b8917dbbbc53ec3caa019a786567c88128d245`.

### S3 finalized source pins and then-pending integration dependency

S3 finalized source paths below are relative to `Assets/Neoxider/Tests/Play/Tools/`;
the three changed current P5 hashes are recorded above:

| Current file | SHA256 |
| --- | --- |
| `Audit20261023NeoToolsInteractableObjectInteractionTests.cs` | `416a8a5f2a9205941d719c0cc2f65777490b284792eda935a267235fcb5fa9c0` |
| `Audit20261023NeoToolsInteractableObjectPhysicsEventsTests.cs` | `50f89ed1353ef8700596693d01afd6197e8c29f338ea3063000350a87e69c295` |
| `Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs` | `ed366a2737886965d0aa714d0c68dd5d5944507f255dc0d0b21d9b4a42c99986` |
| `Audit20261023NeoToolsInteractableObjectInputDriver.cs` | `9b57e2e8b407dd4f02f5c660b9ddb9468d56a5be066ffe0ca064f8b5d6e86a46` |
| `Audit20261023NeoToolsInteractableObjectLifecycleTests.cs` | `5823d234d696c9ed7b38773bdc1d0218bf0d0a00b596f14df882d83e9bd10ce9` |
| `Audit20261023NeoToolsInteractableObjectPlayerLoopTests.cs` | `ff6584587100f18193bdc7eaa7e9aa06541ba12f24367d31d87fc45bd33212e4` |

S3 guide: `Assets/Neoxider/Docs/Tools/InteractableObject/SharedQueries.md`, SHA256
`cd86b4fd7348cc43f2d5672c2f32853eb9f52b142c4d0cb76e89ed1af3fe66fc`.
The six production C# sources retain NI42/source3 hashes; none changed in this S3 correction.
No new extraction/helper assembly or public/serialized API is introduced.

**Primary-owned dependency at S3 finalization (resolved before this P5 correction):** the moved SharedQuery fixture retains direct public
`InteractionQueryMath` / `InteractionRayHit` oracles from `Neo.Tools.InteractableObject.Core`.
At that handoff `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` named only the parent interaction assembly,
not Core. Primary `b03-04` / root subsequently integrated the Core reference, as observed above.
Shared asmdefs, reserved helpers/legacy callers and nested Core remain
untouched by this actor. This is a source-reference integration requirement, not a new compiler result.
The relocation reservation file was consulted; these owned fixture paths are unreserved.

Owned source work is complete. No current compile, discovery, Unity/native test, product checker,
install/service/process control, foreign Unity project mutation, new agent, commit or push occurred.
Only source/evidence reads, scoped edits and explicit artifact hashes/metadata bookkeeping were used.
Root must integrate the shared reference, obtain renewed review of the final small SharedQuery/doc
delta, finish all writers, then freeze and rerun moved fixtures and all 12 matrix cases. Native
input/focus delivery, same-frame tap, click/double-click, explicit-trigger 1/17 positives, stable miss,
native collisions/pointer delivery, CPU/allocation and version/MIRROR parity remain unproven here.

## Original implementation context (retained)

## Approved actions completed in source

### INTERACTION-SHARED-QUERY — A-01/A-02

New `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionPhysicsQueries.cs` provides the
shared main-thread raw backend. Each exact query context is executed lazily by its first consumer
and reused by the others during the same stable epoch. The key includes camera/viewport/display/
render-target identity, default physics worlds, query kind, exact float geometry/distance, physical
mask, effective 3D trigger/backface policy, relevant 2D global flags and complete ContactFilter2D
fields. Target id, hierarchy exclusion and target classification are not keys. No physical masks
are widened, and no tags or new target filters are introduced.

`InteractiveObject.cs` routes all seven former native query sites through this backend: 3D/2D
camera, keyboard look, and range/LOS including explicit-filter 2D. Automatic Update/pointer
processing shares results; the public synchronous range getter requests fresh data. The target
still classifies every raw hit through its own explicit collider, hierarchy, range and obstacle
rules, then uses existing `InteractionQueryMath`. There is no global single-target winner.
Different query lengths/directions, masks or physics kinds remain different contexts.

Saturation now uses a complete equivalent fallback: 3D RaycastAll, 2D GetRayIntersectionAll /
RaycastAll, or the explicit ContactFilter2D List overload. Every initial and fallback Physics call
is counted. Retained arrays grow with spare capacity; per-target classification storage grows too.
No populated 16-hit prefix is accepted as a complete result. The cold fallback may allocate; the
normal warmed query reuses its buffer. S3's limited positive camera counts are recorded above;
corrected full-case results and CPU/allocation measurements remain pending.

`Hits3D` / `Hits2D` expose read-only indexed value access, Count, IsValid and IsComplete. They are
borrowed until epoch/frame invalidation, not immutable permanent snapshots. Expired indexed reads
throw; expired views do not report completeness. Old keys and used hit references are cleared;
buffer slots reuse capacity across epochs. `ReleaseBuffers` frees retained high-water capacity,
and subsystem reset also invalidates old entries for domain-reload-disabled users.

Callback boundaries invalidate before dispatch and in Dispose/finally afterward. Shared-mode
queries made inside a callback bypass cache reuse. This covers InteractiveObject hover/click/range/
down/up callbacks, both physics relay event surfaces and ToggleObject's reactive/action dispatch.
The native relay payload, filters, event order and exception propagation remain unchanged.
Cached click eligibility is refreshed after an epoch boundary before authorizing input; automatic
input stops after its callback disables the target. Targets finish borrowed-hit traversal before
any observer executes.

### I6-01 — automatic target lifetime and reset ownership

`InteractiveObject` now captures its own bounded `ulong` reset generation for an automatic operation.
`ResetInputState` advances it and commits range/input/click cleanup before hover observers. Continuation
checks after hover, mouse, range and keyboard stages require the same target generation and current
availability. Disable → enable inside the same listener still ends the old operation. A reentrant
request for the same private automatic Update returns instead of recursively starting another pass.
This does not cancel remaining listeners already inside one UnityEvent invocation.

Range state, held/pressed mouse bookkeeping and left-click history are committed before external
notifications. A nested OnDisable reset therefore cannot be overwritten by the old stack. Mouse-up
captures its accepted press locally, clears stored press state before notification and rechecks current
click geometry afterward. Keyboard-down checks lifetime/input enablement and re-evaluates range/aim
before a same-frame up. The outer operation also stops after an inner input reset/destroy, before any
range, keyboard or debug tail can access the destroyed target.

Target reset generation is separate from the global physics epoch. Ordinary hover notification may
end a raw-hit epoch without cancelling valid mouse/range input. Manual `InteractDown`, `InteractUp`
and `Click` retain only their existing `interactable` gate, with deliberate active/range/aim bypass.

### I6-02 — disabled rendering camera retains valid geometry

Removed the new `Camera.isActiveAndEnabled` condition from `InteractionRayProvider.TryCreateRay`.
A live disabled Camera with a valid render texture/viewport remains usable through direct provider
calls and the cached target center-ray path. Null/zero-size protection, Core camera retention and
disabled-provider/main-camera selection controls remain. No new camera eligibility setting or migration.

### E6-01 — conservative planar 2D argument preservation

The backend key stores original planar `Vector2` origin/direction separately; both initial and full
fallback Physics2D calls receive those exact values. Planar calls no longer construct/extract a 3D Ray.
This preserves non-unit/tiny/zero directions and projected normalized-3D LOS directions without an
extra normalization. The review did not demonstrate a native missed-blocker defect; this is the
approved conservative contract correction, with direct-native parity acceptance authored, UNEXECUTED.

### Measurement source required by NI6

Added profiler markers around actual native attempts/fallbacks, target-local projection, callbacks
and total automatic Update. Existing native counters remain immediately beside the Physics calls.
`Statistics.ClassificationPasses` and `ClassifiedHits` count local projection passes/raw entries
separately from native work, callback boundaries and native calls inside callbacks. They do not
pretend that N consumers classifying H hits have constant CPU/memory cost.

### INTERACTION-HOVER-REENTRY — A-03

`InteractiveObject.SetHoverState` captures a monotonic hover generation and checks it after the
enter/exit callback. A reset or nested opposite transition suppresses the obsolete outer Changed
notification. An enter listener that turns availability off triggers the final false transition.
Ordinary order remains enter/exit → Changed. The reverse exit → reenter sequence likewise avoids
an obsolete Changed(false). Existing independent hover/input and network dispatch fixes are retained.

### INTERACTION-CORE-NAVIGATION

The parent canonical entry links `Core/README.md`. The Core directory metadata was already present
on continuation with GUID `f6b175603e19421a83153dd9dedc5b7f`; it was retained without creating a
duplicate or replacing another owner's metadata. No Core source or Core document was edited.

## Exact runtime files and additive API

Paths below are under `Assets/Neoxider/Scripts/Tools/InteractableObject/`:

- **New:** `InteractionPhysicsQueries.cs` and `.meta`.
- **Changed:** `InteractiveObject.cs`, `InteractionRayProvider.cs`, `PhysicsEvents2D.cs`,
  `PhysicsEvents3D.cs`, `ToggleObject.cs`.
- `Internal/InteractiveObjectSceneSetup.cs` retains its earlier coherent audit corrections; this
  implementation did not add scene wiring or a controller there.

Public additive API in namespace `Neo.Tools`, existing parent assembly:

- `InteractionPhysicsQueries.Raycast3D(Camera, Ray, float, int, QueryTriggerInteraction, QueryMode, QueryPurpose)`.
- `GetRayIntersection2D(Camera, Ray, float, int, QueryMode, QueryPurpose)`.
- `Raycast2D(Camera, Vector2, Vector2, float, int, QueryMode, QueryPurpose)` and the ContactFilter2D overload.
- `QueryMode.Fresh` (default external synchronous behavior) / `Shared`.
- `Invalidate()`, `ReleaseBuffers()`, `Epoch`, `Counters`, `ResetCounters()`.
- Borrowed `Hits3D`, `Hits2D`; read-only `Statistics`; diagnostic `QueryPurpose`.
- `Statistics.ClassificationPasses`, `ClassifiedHits` (raw entries visited, including later exclusions).
- `InteractionRayProvider.InvalidateQueries()` forwards explicit world-change invalidation.

Counters include all backend attempts/fallbacks, cache hits, fresh requests, invalidations,
callback boundaries, frame advances and calls inside callbacks. Camera/look/range native-call
categories identify the first requester; purpose is not a physical key. External EventSystem
physics is outside this counter and needs separate profiler accounting.
Markers: `Neo.Interaction.Native.Raycast3D`, `.Intersection2D`, `.Raycast2D`, `.FilteredRaycast2D`;
`Neo.Interaction.Classify3D`, `.Classify2D`, `.Callbacks`, `.AutomaticUpdate`. Projection markers
cover buffer growth/hierarchy classification; Core selection is included in total AutomaticUpdate.

## Compatibility and boundaries

No new serialized fields, required interface members, controller, registration API or scene setup
were introduced. Existing field names/defaults, collider bindings, masks, input modes, UnityEvents,
manual interaction API and network authority/rate paths are retained. No scenes/prefabs/samples,
asmdefs, shared trackers/configuration, package metadata, migration guide or changelog were edited.

The declared stable epoch replaces repeated automatic native queries. This cannot automatically
detect arbitrary external collider/world mutations between unrelated components' Update calls.
After movement, manual simulation or SyncTransforms inside a pass, the caller must invoke
InvalidateQueries/Invalidate, or choose Fresh. Invalidation itself does not synchronize physics.
Module-owned callbacks, target enable/disable, collider-cache invalidation, provider mode/disable
and main-provider replacement supply their own boundaries. A live disabled rendering camera retains
valid viewport-ray geometry. Existing Core cached-camera retention/checkpoint policy is not changed.

All raw APIs query Unity's default physics scenes, matching the original static Physics calls;
passing a Camera does not switch to that camera's local physics world. No approximate vector
comparison or direct obsolete identity API was added. Raw-hit classification and input/ray
generation still have target-local CPU work. Unique LOS and callback-driven new epochs can add
native work; this is not a claim of constant aggregate physics across all possible settings.

## Documentation completed

Under `Assets/Neoxider/Docs/Tools/InteractableObject/`:

- New `SharedQueries.md` and `.meta`: exact sharing, full fallback, borrowed lifetime, explicit
  invalidation/fresh API, original planar vectors, default-world scope, C# example, markers and
  diagnostic interpretation, including separate N x H projection work and unexecuted Play acceptance.
- Updated `README.md`: shared-query and Core links, no-wiring upgrade path and explicit boundary.
- Updated `InteractiveObject.md`: generation-safe hover sequences, automatic shared/fresh paths,
  target-local policies, callback invalidation, reset ownership, click lifetime and cached-click refresh.
- Updated `InteractionRayProvider.md`: invalidation facade, provider lifecycle and valid disabled-camera geometry.
- Updated `PhysicsEvents2D.md`, `PhysicsEvents3D.md`, `ToggleObject.md`: callback epoch boundaries
  without changing their payload/order/action semantics.

No DocsEn or nested Core document was touched. Existing English canonical entry remains the entry.

## Regression inventory — current paths; corrected acceptance UNEXECUTED

- Updated `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectInteractionTests.cs`:
  enter → hover-off/disable and exit → reenter event-sequence regressions (3 additional cases).
- `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs`
  and `.meta`: 1/100/1000 full automatic Update consumers in a stable context; post-filter
  divergence; actual masks; sub-epsilon geometry; cameras/viewport; distinct center/pointer rays;
  3D global versus explicit triggers; 2D global/explicit/start-inside policy; 17/64/1000 hits for
  all four query kinds, forced cold complete fallback and warmed capacity; dense target/blocker
  parity against complete uncached physics; fresh range; expired borrows; callback mutation/fault;
  cached click after hover-world mutation; distinct LOS with separately counted range work.
  NI6 additions compare enabled/disabled Camera direct rays and cached target hover, zero viewport
  and destruction; 10 planar parity cases (legacy/explicit filter x unit/non-unit/tiny/zero/large-Z
  projection) compare actual direct Physics2D identities/points/distances with cold/warm/cached
  backend paths using the original sufficiently sized native array overloads. An actual large-Z
  checkpoint/Collider2D target also compares public range/LOS against the direct explicit-filter
  native oracle, cold and warm; original planar directions must partition keys. Cold fallback counts follow the
  direct result, rather than assuming a particular native outcome for zero/tiny directions.
- `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectLifecycleTests.cs`
  and retained `.meta`: automatic hover → OnDisable exact sequence; mouse down/up/click observers
  disable, disable-reenable or DestroyImmediate and cannot resume range/keyboard tails; range
  re-entry after reset and range-exit reset cancelling an otherwise eligible keyboard tail;
  same-frame native keyboard tap positive control followed by aim/range/
  keyboard-flag/lifetime invalidation suppressing up; click → close/reopen yields a single, with
  ordinary double-click controls before and after; ordinary hover epoch still permits input;
  disabled-target manual API/interactable negative control.
- New `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectPlayerLoopTests.cs`
  and `.meta`: 12 real PlayerLoop cases, 1/100/1000 distinct physical targets for each of 3D/2D
  camera and keyboard look. No SendMessage/manual Update. A warmed positive pass asserts all
  hovered/accepted-down states, one native call, N-1 cache hits, N projection passes and N x N raw
  hit visits. Moving every target off-ray first asserts its actual transition/exits, then the next
  stable miss requires one query, false states and zero hit visits. Callback/LOS counters are separate.
  Six frozen positive camera slices are recorded above; all corrected full-case results remain pending.
- New `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectInputDriver.cs`
  and `.meta`: reflection-only optional Input System virtual keyboard/mouse, QueueStateEvent consumed
  by the real Dynamic PlayerLoop. No fake physics/input result or target Update invocation. Tests
  temporarily select the existing Input System compatibility fallback, Dynamic mode and the owned
  focus-routing settings described above. Teardown removes only owned devices/subscription and restores
  prior selection, update mode, focus settings, runInBackground and current devices.
  Absence of the optional assembly explicitly skips input-dependent acceptance; camera matrix cases
  do not require it. This is not legacy Input Manager device-delivery evidence.
- `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectPhysicsEventsTests.cs`
  retains the earlier explicitly driven offline/filter dispatch controls; native collision proof is separate.

New tests use the isolated/exclusive runner contract and owned cleanup. They do not use the
unavailable NUnit serialization attribute. Test scene objects, render textures, main-camera tags
and changed global physics/input settings are cleaned/restored by the fixture. Frozen S3 execution
has the actual outcomes recorded above; this correction/finalization did not run, discover or compile tests.

## Dependencies and evidence debt

No additive runtime owner API is pending. Existing Core target/hit/math/camera contracts and
Network/Input APIs are reused without edits. The precise primary-owned Play-to-Core reference
dependency was recorded in the historical S3 handoff and is resolved in the P5 handoff above. Owned fixtures use the existing Play test
assembly; this actor did not change shared references. Six originally new package assets have
individual GUID metadata, and moved GUIDs are preserved; root performs final
metadata/GUID validation after the source barrier.

Actual work this phase: source/reference/document reads, scoped diff review, source/docs/test-source
edits and artifact bookkeeping. Read the installed Input System source signatures for AddDevice,
RemoveDevice, QueueStateEvent, KeyboardState, MouseState and frame-edge tracking as context for the
reflection driver; this is source review, not compiled/executed integration evidence. **No Unity, compiler, test/discovery runner, product validator,
static product-checker, install, service, commit, push or child agent was executed.** Historical
audit static-check results do not validate the new implementation.

After every writer finishes, root must obtain frozen-snapshot compilation/weaving and real Edit/
Play results on installed Unity 6.3 and 6.6, MIRROR on/off dependency evidence, native lifecycle/
collision/UI and complete-query parity, 1/100/1000 counter/profiler results and aggregate external
query accounting. Cold fallback allocation, dense per-target projection storage and distinct LOS
cost need measurement. New native-input reflection integration and PlayerLoop cases require actual
compile/discovery/execution; optional Input System absence must be a skip, not a lifecycle pass.
Legacy hardware input, real pointer/EventSystem and direct-native tiny/zero planar outcomes also
remain execution debt. Separate-peer remote admission evidence belongs to the approved harness/
verification owners; host/private Command invocation is not a substitute. Unsupported optional
versions/devices remain explicit skips until exercised. No execution pass is claimed here.

Root retains release/version/sample parity, global source freeze and the later user-authorized
push after complete verification. Optional controller/session/capture frameworks remain proposals;
they are not prerequisites or implemented behavior of this receipt.
Owned writes have stopped after this handoff. Prior NI6/NI42 source pins and frozen S3 results
remain historical; they are not a current full native pass or permission to skip the integration gate.
