# Native implementation review 6: shared interaction queries

Status: **COMPLETE — corrections required; native acceptance UNEXECUTED**. Independent read-only QA of Neo.Tools.InteractableObject, 2026-10-24. Only this report was written. No product/source/test/public-doc changes, Unity/MCP, compiler/tests/checkers or child agents. Input NI4, NoCode I5 and Quest corrections are outside this review.

## Source-final entry gate

`Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.implementation.md` was read completely first. It declares **source-final; acceptance UNEXECUTED**, completion 2026-10-24, no pending additive source API. Read SHA256: `755202c4c877ca65afc5078a2a83f1ee4dafa1ebd6dfd84d492b7172d8c07654`. Scope is admitted for review; no still-writing scope was accepted. A later changed implementation/receipt needs renewed review.

Read the complete current authoritative `INTERACTION-QUERY-DESIGN.md`; apply its exact-query, stable-epoch, callback/freshness, completeness and mechanics-preservation contract together with B-extra. Source counts/counter assertions will be distinguished from actual native execution evidence, which remains unexecuted.

## Disposition

The implementation follows the approved lazy shared-raw-query design in the main: no new controller, registration or scene wiring; complete overflow fallbacks; per-target classification; exact query-context comparisons; borrowed read-only views; callback and explicit invalidation boundaries. The native counters surround actual Physics calls, and the new dense/counter regression sources use actual colliders and native APIs. They are not a mock backend or an injected counter exercise.

Two source-confirmed P2 corrections remain for the **original owner `neo-audit-1023-b07-03`**. The first concerns target state ownership after callbacks; the second is a new camera availability rule outside the approved mechanics-preserving request. Neither needs a controller/session framework. An additional 2D direction round-trip needs parity evidence before exact native-argument equivalence can be accepted; this report does not claim a demonstrated native physics failure for that concern.

All execution acceptance remains **UNEXECUTED**. Authored synchronous Edit-mode Update calls are distinguished below from genuine PlayerLoop/frame/profiler results. No unmeasured native-call count, allocation budget or FPS improvement is approved.

## Confirmed findings and minimal corrections

### I6-01 — P2: callback reset does not own the rest of the automatic interaction operation

**Owner:** original InteractableObject owner/task `neo-audit-1023-b07-03`. **Files/methods:** `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs`, `UpdateInteraction` lines 269–298, `OnDisable` lines 301–305, `ResetInputState` lines 313–326, `UpdateMouseInput` lines 494–527, `ProcessClickEvent` lines 933–949. These are current-source control-flow findings, not an execution claim or a claim that every line was first introduced in this phase.

Concrete triggers and source traces:

1. A mouse down/up/click observer destroys or disables its target. `UpdateMouseInput` correctly returns after its local availability check at lines 495/507/524. Its caller nevertheless continues at line 274 into `IsInRange`, keyboard processing and debug work. With `DestroyImmediate` of the owning GameObject, `IsInRange` calls `GetInteractionTargetPosition`; when destroyed cached colliders compare null, that method reaches `transform.position` at line 925. The inner return does not stop the outer automatic operation. A disable should likewise end that operation, without further automatic range/input notifications.
2. An `onEnterRange` observer sets `enabled = false`. `OnDisable` resets `wasInRange = false` at line 305, then the older outer invocation writes `wasInRange = inRange` at line 285. Re-enable the component while still in range: the disabled lifetime's stale `true` suppresses the next genuine enter transition. Disabling and re-enabling inside the callback also defeats an availability-only check unless reset ownership is recorded.
3. A left-click observer closes/disables its own panel/target. `OnDisable` clears double-click history at line 318, but `ProcessClickEvent` resumes after `TriggerClick` and writes `clickTime` / `hasClickTime = true` at lines 948–949. Re-open within `doubleClickThreshold` and click: the first click of the new enabled lifetime can be reported as a double click. Expected behavior is that the existing reset remains authoritative.

`UpdateKeyboardInput` also computes `canInteract` once (lines 667–669), notifies down at line 674 and can consume the old result for up at line 677. The same repair should stop/revalidate a remaining automatic keyboard tail after a down callback invalidates target lifetime or eligibility; the new mouse guards alone are not a general lifecycle fence.

**Minimal correction:** commit range/click bookkeeping before notification where that preserves the existing event contract, so a nested reset wins; add an outer automatic-operation availability/reset-generation fence after mouse processing and range notification, and cover disable → re-enable reentry. Revalidate remaining input eligibility after an observer changes the query epoch when it is actually reused. Do not use the global physics epoch as a universal cancellation flag: an ordinary hover notification always advances it and should not cancel all legitimate mouse/range work. Do not add a blanket active-state check to the common manual trigger methods: the documented explicit `Interact*` API intentionally bypasses automatic aim/range/active checks and retains its `interactable` policy.

**Observable acceptance, UNEXECUTED:** native Play-mode observer disables/destroys target during automatic input; no later automatic range/keyboard work or missing-reference exception; range enter → disable → re-enable still in range produces a new enter; disable/re-enable within the callback does not let the old operation overwrite reset state; click → disable → reopen → first click is single, with an ordinary two-click positive control still double. The existing hover-only Play fixture does not exercise these tails.

### I6-02 — P2: disabling rendering now disables an otherwise valid interaction camera

**Owner:** `neo-audit-1023-b07-03`. **File/method:** `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionRayProvider.cs`, `TryCreateRay`, line 97; propagated to `TryGetHoverRay`/`TryGetClickRay` and the target's center-ray route. Relevant dependency: `Core/InteractionCameraResolver.cs`, `Resolve`, lines 13–15 retains an existing live camera, independently of its enabled flag.

**Trigger:** use a live active camera with a valid render texture/pixel dimensions, a provider in ScreenCenter mode or a target with `UseScreenCenterRay = true`, and a collider on its ray. Resolve/cache it, then set only `Camera.enabled = false` while keeping the camera object, provider and target alive. This is a valid manually rendered/cached interaction-camera arrangement. Existing base `ca198556` provider ray methods checked the camera reference and still generated the ray. Current line 97 returns false solely because the Camera is not active and enabled, so the same configured geometry now loses hover/click eligibility.

**Expected versus actual:** the user's unchanged mechanics/settings contract and approved A-01/A-02/A-03 request authorize sharing/completeness/hover reentry, not a new render-enabled camera eligibility policy. Ray geometry availability should retain the established camera behavior. The new receipt and guide acknowledge the disabled-camera rule, but a documentation statement is not separate approval to change the mechanic. This finding is about disabling the **Camera component**, not the existing behavior of a disabled **InteractionRayProvider**.

**Minimal correction:** retain null/invalid-viewport protection while removing the new rendering-enabled requirement from geometry creation. Preserve existing Core cached-camera/checkpoint policy. If a render-enabled-only policy is desired later, it needs explicit approval and a distinct compatibility contract; it is not needed by the cache.

**Observable acceptance, UNEXECUTED:** compare the same camera/collider with Camera.enabled true then false using a fixed render texture and center ray; both direct provider APIs and the already-cached target center-ray path must retain the same hit/hover result. Include camera destruction/zero-size invalid context and retain the existing disabled-provider negative control for `FindOnMainCamera` selection. Do not impose a new active-state rule on a directly referenced geometry API as part of that control.

## Exact physical contexts and all six logical routes

Current target integration was read in full (1,631 lines), along with the complete backend (593 lines) and provider (180 lines). The six logical routes use four raw overload kinds; the no-collider LOS fallback is an additional 3D call site, not a seventh physical kind.

| Logical route | Actual native context / physical policy | Target-local work retained | Source disposition |
| --- | --- | --- | --- |
| Camera 3D | `Physics.RaycastNonAlloc(Ray, ..., float.MaxValue, ~0, Collide/Ignore)`; exact hover/click ray geometry | explicit collider/hierarchy and rig exclusion; foreign triggers are not blockers; `checkObstacles` classification | Correct separation in source, subject to I6-02 |
| Camera 2D | `Physics2D.GetRayIntersectionNonAlloc(Ray, ..., float.MaxValue, ~0)`; legacy `queriesHitTriggers` and relevant start-inside global state | local mouse trigger flag drops returned trigger hits; it cannot resurrect a globally excluded hit; same target/blocker rules | Correct global-then-local semantics |
| Keyboard look 3D | Ray from look source, normalized forward; target/configuration-dependent maximum distance; `~0`; explicit Collide/Ignore | independent direct-look/mode gates and own target/rig classification; `checkObstacles` remains local | Correct routing; consumer-scale execution evidence missing |
| Keyboard look 2D | `Physics2D.RaycastNonAlloc` planar ray; existing forward/projection fallback and max length; `~0`; legacy global trigger policy | local look trigger flag, own target/blocker classification | Correct global/local split; direction concern below and consumer-scale debt |
| Range/LOS 3D | checkpoint → target ray, existing padded distance, `obstacleLayers`, explicit obstacle trigger mode; same for the no-collider fallback | nearest blocker after per-target/rig filtering; unique origins/directions/lengths/masks remain distinct | Correct unique-LOS separation in source |
| Range/LOS 2D | explicit `ContactFilter2D` with `obstacleLayers`, `useLayerMask = true`, configured `useTriggers`; existing projected direction and distance | own hierarchy/rig filtering; triggers use the explicit filter rather than an added global AND | Correct explicit-filter distinction; direction concern below |

`Key` lines 177–259 uses managed reference identity for Camera/render texture, exact component `.Equals` for ray vectors and distance, camera pixelRect/display, query kind, masks and full filter fields, both default physics scene identities, effective 3D trigger mode/backface flag, and applicable 2D trigger/start-inside globals. There is no approximate Vector3 operator, target id or rig id in the key. `QueryPurpose` only categorizes actual first-requester work and is not a key. Explicit 3D Collide/Ignore does not split just because the irrelevant global trigger flag changes. The explicit 2D filter is not incorrectly ANDed with `queriesHitTriggers`.

All calls remain static Physics/Physics2D queries against Unity's **default** physics scenes. A camera in another scene does not silently switch the raw API to that camera's local physics world. Storing a default-world identity does not implement a local-world query feature, and neither the receipt nor this review approves one. Camera/viewport fields conservatively partition otherwise identical contexts; they do not widen masks.

The cache shares raw hits, not a selected global target. `BuildInteractionHits3D/2D` traverse the complete valid prefix, grow each target's projection buffer and apply that target's exclusion, trigger and target ownership rules. Foreign trigger/obstacle policy and target aliasing can therefore yield different valid answers from the same raw query. Each traversal and target selection finishes before any external observer or subsequent fresh query. Only hit values/points are carried across range work, not a still-live borrow assumed to survive invalidation.

### Completeness and fallback

`Execute` lines 427–499 starts with a retained 16-hit buffer. `count == capacity` always triggers a complete equivalent native fallback: RaycastAll for 3D; GetRayIntersectionAll for camera 2D; RaycastAll for legacy planar 2D; **the same ContactFilter2D List overload** for filtered 2D. `CountNative` runs immediately before the initial and fallback calls. The retained arrays grow with one spare slot, so an unchanged dense warmed query does not deliberately saturate again. No full 16-hit prefix is treated as complete, and every target route requires `IsComplete` before authorizing a hit/LOS decision. The target's projection buffer also grows; solving only the shared raw buffer would not have been sufficient.

The explicit-filter fallback does not switch to a globally filtered RaycastAll overload. This is necessary for `queriesHitTriggers = false` plus explicit `filter.useTriggers = true`. Native fallback still needs actual Unity-version execution; source equivalence is not an execution result.

### Unverified parity concern E6-01: planar Vector2 passes through a 3D Ray

`InteractionPhysicsQueries.Raycast2D` lines 367 and 380 builds `new Ray(Vector3(origin), Vector3(direction))`; `Execute` lines 457–458 extracts that Ray's XY components and passes them to the native planar query. The former target call site passed its original Vector2 directly. In particular, range LOS obtains XY from a normalized **3D** checkpoint direction (`InteractiveObject.cs` lines 756–765), so the planar vector need not have unit length. The currently authored planar dense tests use unit-axis directions.

This is a concrete transformation point requiring evidence under the exact-native-argument contract. Ray direction normalization/tiny-vector handling can differ from sending the original planar vector to Physics2D. **No native missed-blocker result has been demonstrated in this read-only review**, and this is not counted as a third confirmed runtime bug. Minimal conservative resolution is to preserve the original planar origin/direction in the key and native call rather than round-trip through Ray, or demonstrate direct native parity before accepting the current representation.

Required parity cases: unit direction positive control; finite non-unit direction; very small nonzero planar direction (for example `(1e-6f, 0f)`); zero direction; range checkpoint/target with a large Z separation and a small nonzero XY projection; both legacy-mask and explicit-filter overloads, with cold fallback and warmed paths. Compare actual native hit identities/points/distances against the original direct Physics2D overload, not another invocation of the same wrapper. Preserve distance/trigger/mask configuration. Do not infer native failure merely from a different intermediate managed vector.

## Epoch, ownership, lifecycle and memory review

- `Hits3D`/`Hits2D` are readonly borrowed views with indexed **value** access; no mutable backing array is exposed. Validity checks combine the entry generation and current backend epoch. Expired views return no valid complete prefix and indexed access throws. Frame changes, explicit invalidation, fresh requests and callback boundaries end borrows. Subsystem reset releases entry generations before resetting static epoch values, avoiding a surviving old view being accepted merely because the numeric epoch restarted.
- `BeginCallbacks` advances before external notification; its `CallbackScope.Dispose` advances afterward and unwinds callback depth under `using`/finally. A Shared query requested inside any wrapped callback bypasses reuse. InteractiveObject UnityEvents and hover Action, all changed 2D/3D relay dispatch sites, and ToggleObject Set's reactive/action dispatch are covered. Existing payload order and exception propagation remain. An empty or mutating callback may create a new epoch and extra native work; one native query is promised per identical context in a stable epoch, not across arbitrary observer changes.
- Public `IsInInteractionRange` forces Fresh independently of automatic nesting. New raw query APIs default to Fresh; explicit Shared is an opt-in borrowed query. These choices do not silently turn synchronous public reads into last-frame cache reads. Target enable/disable, collider-cache invalidation, provider mode/disable/main-provider replacement also invalidate. The callback lifetime machinery is sound as raw-cache ownership; I6-01 is the separate target/input continuation gap.
- External movement, collider changes, `SyncTransforms`, `Physics.Simulate` / `Physics2D.Simulate` between unrelated components are not automatically detected. `InteractionRayProvider.InvalidateQueries` / backend Invalidate is the explicit bridge after the application synchronizes/mutates its world; Fresh is the synchronous alternative. Invalidate does not itself synchronize physics. No automatic global world revision, mandatory scene driver or registration is claimed. Direct writes through exposed Toggle reactive state bypass the component method and need the same caller-owned boundary, as its guide discloses.
- `AdvanceEpoch` clears active keys and used entries, including used hit references. Moving rays do not accumulate a dictionary of historical contexts across frames. Buffer slots and capacities retain a **high-water mark**, with explicit ReleaseBuffers/subsystem reset for a scene/memory boundary. This is bounded by prior peak simultaneously used contexts/hit capacities, not a hard byte cap or an allocation-free promise. Borrowers must discard old views. Dense target-local projection buffers also retain capacity; 1,000 consumers classifying 1,000 hits can still entail substantial aggregate CPU and memory even with one shared native query. Measure it; do not claim constant total frame work.
- Hover generation handling at `SetHoverState` correctly protects enter → hover-off/disable and exit → reenter from an obsolete outer Changed event. A callback can still notify the remaining listeners already in that same UnityEvent invocation; cancelling UnityEvent's internal listener iteration is not claimed or required. The existing same-state transition and manual interaction semantics are retained.

## Regression-source and proof review

Read the complete new shared-query Edit fixture, complete current interaction Edit fixture and complete lifecycle Play fixture, including setup/cleanup and assertions. The following are **authored source scenarios only**, all UNEXECUTED:

| Source scenario | What the assertions can demonstrate if run successfully | Limits / remaining evidence |
| --- | --- | --- |
| Shared fixture lines 85–115: 1/100/1000 `AutomaticCameraPass_OneRawQueryForAllConsumers` | Actual colliders/native Physics; full target Update body invoked with SendMessage; warmed stable 3D camera context; all target aliases report hovered; Raycasts3D and CameraNativeCalls are 1 and CacheHits is N−1 | Edit-mode synchronous calls; all consumers bind the same selected collider; not actual per-frame PlayerLoop/profiler evidence, not 2D/keyboard or dense N×H scaling proof |
| Dense fixture lines 293–335: 17/64/1000 hits × all four raw kinds | Actual scene colliders and native query/fallback; count, completeness and unique collider identities; forced cold ReleaseBuffers yields two native calls/one fallback, next warmed epoch one/zero | Four physical overloads do not alone exercise every target-level logical route. No executed counts are available |
| Dense target/blocker parity lines 360–409, 3D and 2D | Target beyond the initial prefix plus nearby wall; complete uncached Physics All reference; wall-on negative and wall-disabled positive expected hover; catches fail-closed/no-query fake optimizations | Reference reuses existing Core selection policy, so it checks full-query/classification parity rather than independently proving all Core math or all keyboard/range branches |
| Distinct range LOS / target-local policies / masks / cameras / center-pointer | Different target answers can share a raw query; masks and tiny geometry changes partition contexts; actual center hit and pointer miss; unique blocked and clear LOS contexts; range work counted separately | Keyboard look consumer-scale matrix and all global/backface/filter-depth permutations are not present; unique LOS correctly may increase aggregate queries |
| 2D globals / 3D explicit triggers / filtered dense fallback | Trigger-only actual collider positive/negative controls, start-inside behavior, same explicit 2D filter across saturation; explicit 3D policy independent of global trigger flag | Must execute on both installed Unity branches; direct non-unit/tiny planar direction parity is E6-01 |
| Fresh range / expired borrows / callback mutation+throw | Unannounced blocker change visible through Fresh; old borrow invalid and indexed access throws; callback creates geometry, nested Shared obtains fresh actual hits; exception unwinds callback depth and later read sees updated geometry | Explicit before/after Simulate/SyncTransforms invalidation, frame advance and subsystem reset need real integration acceptance |
| Hover callback adds wall before cached click | An actual world mutation followed by callback epoch invalidation should reject the stale click | This focused negative case would be stronger with its own wall-off/click-positive control; the separate Both-mode fixture does have a working positive click path |
| Existing/new interaction fixture and Play lifecycle | Three additional hover-reentry sequences; UI/no-collider click and Both-mode positive controls; actual OnDisable from enter in Play mode asserts final disabled/hover state and exact notification order | Play fixture tests hover disable only, not I6-01 mouse/range/click continuation or PlayerLoop native scaling |

The counters are incremented beside actual native operations at backend lines 436–442 and 459–482, not by a fake implementation. Cold overflow and positive hit identity assertions prevent accepting an optimization that merely returns no hits. The authored real-native tests deserve that specific credit. They cannot be reported as passing before execution, and SendMessage-driven Edit-mode work cannot be described as a measured PlayerLoop/physics-frame budget.

Fixture cleanup owns created objects/render textures, restores temporarily changed existing MainCamera tags and physics global flags, and invalidates the backend. It does not delete unrelated objects or saves. Remote coordinates reduce accidental collisions but are not a separate physics world; these tests depend on the declared isolated/exclusive runner. A live unrelated default-world collider can still affect them in a nonisolated run. No unsupported NUnit serialization attribute was introduced in these reviewed fixtures.

### Acceptance still needed after every source writer finishes

The original owner should resolve I6-01/I6-02, provide corrected receipt and focused regressions, then return that exact snapshot for review. E6-01 requires either preserving the original planar values or the specified direct-native parity evidence. These are narrow corrections, not prerequisites for a new controller/capture framework.

Root's later native acceptance must include compile/discovery/weaving on the installed Unity 6.3/6.6 branches, MIRROR on/off integration where supported, actual automatic PlayerLoop cases for stable 1/100/1000 consumers in **3D and 2D camera and keyboard look paths**, plus distinct LOS and 17/64/1000 dense completeness. Account separately for callback-created epochs, unique contexts and EventSystem raycaster physics; these are outside a one-stable-camera-key assertion. Record actual counters, profiler/native-call evidence, cold/warm allocation and aggregate target classification costs. A 1000×1000 dense consumer measurement should not be inferred from running independent N-only and H-only fixtures.

Required functional parity includes target/foreign trigger separation, checkObstacles on/off, actual masks, different cameras/viewports/display/geometry, Both-mode distinct rays, explicit 2D filter versus global trigger state, backfaces/start-inside/depth as relevant, own rig/explicit collider/child-target rules, unique LOS, callback disable/reentry/mutation/throw, public Fresh, explicit external simulation invalidation and epoch expiry. Preserve observable outcomes, serialized defaults and mechanics; a native-call reduction alone is not acceptance. Unsupported versions/platforms must be recorded as skips, not passes.

## Documentation and delta coverage

Read the full implementation receipt, current authoritative design and original request; applied the earlier B-extra boundaries. Read current complete backend/provider/target, ToggleObject, SceneSetup and Core camera/math dependency bodies. Reviewed all changed physics-relay callback hunks against `ca198556`; did not re-approve unrelated unchanged network RPC internals or independently own Core/Input/Network runtime work. Current module diffs were inspected as context, including earlier corrections retained by this receipt. New untracked files were read directly rather than assumed to appear in tracked git diff statistics.

Read all seven owned current guides in full: `SharedQueries.md`, canonical `README.md`, `InteractiveObject.md`, `InteractionRayProvider.md`, `PhysicsEvents2D.md`, `PhysicsEvents3D.md`, `ToggleObject.md`. Their raw/shared versus fresh behavior, explicit invalidation, no-controller migration, default-world scope, native-counter boundary and high-water memory caveats match source. The disabled-camera statement is the I6-02 policy change, not a documentation/source mismatch. The receipt's automatic-input-stop statement is too broad until I6-01 is corrected. Existing Core math reusable-array statements describe the Core API, not proof that the entire backend/fallback/classification flow never allocates.

This review wrote only this Markdown file, using source/diff reads and SHA256 bookkeeping. No Unity/native query/test, compiler, package checker, install/service, commit/push or child process intended to validate the product was run. Report freshness is tied to the hashes below; new source changes need a renewed read, not automatic approval from the old receipt.

## Read fingerprints (SHA256)

All paths are repository-relative. Full source/document reads are described above; relay entries designate reviewed complete changed dispatch hunks, not a claim to re-audit all unrelated unchanged network code. Metadata/GUID/package validation belongs to the later root barrier and was not executed here.

| Artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.implementation.md` | `755202c4c877ca65afc5078a2a83f1ee4dafa1ebd6dfd84d492b7172d8c07654` |
| `Docs/Audits/2026-10-23/INTERACTION-QUERY-DESIGN.md` | `d64726990331b14b8e6614d231a482378fa9cee85ee63e4ef095d76c1a75f6f1` |
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.implementation-request.md` | `5a156c99f1fd0100ce26aa7cae4969e245d60671bc4fbce545136f0a0b7e7cce` |
| `Docs/Audits/2026-10-23/NativeReview-B-extra.md` | `95b1df741f09abdd60a69522b162fa3947bdd98660401bb3e175bef2cf5670d2` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionPhysicsQueries.cs` | `f416384396409a32a6bae727baba2364f8e8f910204d18c78b576314912fceac` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionRayProvider.cs` | `2dafa65ed8543eeea5b31d39a0a1ae3c57b7e1219f96562d935b14227de69904` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs` | `303c7b4d71f228d78a8b935a6fc6bc82b543a1182e92deb32fc5bb301e747d53` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs` | `93e0d4036b376fc4fb273b595bd3653a583da4f6b2adc2e5bcdf2cec9d7474d5` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs` | `e3f3852b0a9512e4f393219be542b28c0e29004953e74c20f65b78c1da9c488f` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/ToggleObject.cs` | `1455dea38f77dfc697d719ac2a2aaa65551f792b8bd6b82d2cfed957e3164a13` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Internal/InteractiveObjectSceneSetup.cs` | `e31f36afde2d160779e9068f5e278b6cab82f6186fd046ac7921f689857d0168` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionCameraResolver.cs` | `bfc77d156d998c289411e258c6af661631331e10f288f7a9f511e8f3cd82eca0` |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionQueryMath.cs` | `50b93c0856073f866ba10c58c52dc3b79dc176b8115c0ab5a14156578e082423` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs` | `12af19083f20c25a5e56f84a9af6bfd560c91ee8930c5d02fee7d8f83d5ace34` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectInteractionTests.cs` | `a51994352c18c83f9dd3cc856cd0fae46449ea59b4512e6ec41930e8ff03f222` |
| `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectLifecycleTests.cs` | `a61cf88e8f348725d21438d36908ea62a13a5c06f299cc86c13af13dda9f7158` |
| `Assets/Neoxider/Docs/Tools/InteractableObject/SharedQueries.md` | `7d3b61063cd8baae92e6ba352ce65b6192d9d2f1da982eef5fe53cbd282c67f0` |
| `Assets/Neoxider/Docs/Tools/InteractableObject/README.md` | `8f12a12004a90192b6a98b59d23a4d46b3197429b5bb20106b9cfcf19fddf247` |
| `Assets/Neoxider/Docs/Tools/InteractableObject/InteractiveObject.md` | `f6fbd9082088b9d3b2159e9f61358547d24427d29312226f6f6557b5c3870c27` |
| `Assets/Neoxider/Docs/Tools/InteractableObject/InteractionRayProvider.md` | `594336da0637140b84c3807cbc2edbfe2517ed0ebbd7b84925dec922007154f6` |
| `Assets/Neoxider/Docs/Tools/InteractableObject/PhysicsEvents2D.md` | `98f6f94cc729f95a04e9f62a16df15e7d7d354607cdc8c6daf4a9b6429a216d5` |
| `Assets/Neoxider/Docs/Tools/InteractableObject/PhysicsEvents3D.md` | `650658397755c8a8f9e97cbc2e13c55ba4c5c4e7e60b12a80917df6c2e000f27` |
| `Assets/Neoxider/Docs/Tools/InteractableObject/ToggleObject.md` | `7a0581a1504a334c0eaab6bc973da58f2223cfa548a4f7857deb80d6585b7e5f` |
