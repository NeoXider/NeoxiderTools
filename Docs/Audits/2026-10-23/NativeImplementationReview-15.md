# Native implementation review 15 — Managers / UI / Move

Status: **COMPLETE — Managers/UI scoped source coherent; two bounded Move corrections required; execution UNEXECUTED**. Independent source-only review, 2026-10-24, in the same native `gpt-6.1-sol` / `max` session. Only this report was written; no product/tests/docs/other reports, Unity/compiler/checkers, services/process management, children or commits.

## Source-final intake

Read all three complete current receipts and requests. Managers declares SOURCE-FINAL, SHA256 `c974efb1edf66f548cd1e79ec25ebba4cda89f2f3b8d1b496092cde062d144ff`; UI declares SOURCE-FINAL with exact GM dependency resolved, SHA256 `03f999fe9b8916316010dea0bae4cfdc40a765d9ee663844f9b5ecdb145c55e1`; Move declares source-final, SHA256 `d2cb154f45f0a3ae17f5b7bf6dea1e7d1f358137393ad445aff6b1b88182c15d`.

The shared state file retains old source-running labels. Read-only tasklist snapshots at intake/completion found all recorded initial/recovery PIDs absent: Managers 21600/44944, UI 62872/49440, Move 36420/79116. Root also supplied its fresh completion/process observation. Receipts, reviewed critical runtime files and fixture hashes matched intake at completion. These are recorded-process/current-file observations, not exhaustive proof about every unrelated writer. No assigned moving source scope was accepted; this is not a global freeze or permission to execute.

## Disposition and ownership

| Original owner | Reviewed phase/current-source scope | Disposition |
| --- | --- | --- |
| Neo.Tools.Managers / SAME `neo-audit-1023-b07-06` | Full GM/EM and 610-line current game-state fixture; all three owned guides; full latest request/receipt; baseline-relative phase hunks | Effective transition version, stale broadcast/composite-flow suppression and the exact UI API are coherent by source. Native acceptance remains open. |
| Neo.UI / SAME `neo-audit-1023-b08-07` | Full PausePage, layout builder/preset, owner resolver; all three complete new fixtures (Edit replacement, Play replacement, PausePage); all three owned current guides; request/receipt/deltas | Rig whole-set preflight and scoped first/last page ownership appear coherent. Exact GM dependency is resolved in current source. No further confirmed UI defect in this bounded review. |
| Neo.Tools.Move / SAME `neo-audit-1023-b07-07` | Full CursorLockController, CameraRotationController, ScreenPositioner; complete ground-query phase hunk and relevant player startup/gate/authority/cursor context; all four complete new fixtures; RNG fixture delta/helpers; all five owned guides; request/receipt/deltas | Shared gate accounting, drag/angle, completeness and depth work are largely coherent. I15-01/P2 runtime composition and I15-02/P2 fixture isolation remain below. |

Both findings were source-confirmed and accepted by root during review. Return only these bounded corrections to the SAME Move actor. No public base-intent/pause/lease framework is required. The original reviewer's own Extensions work is not approved here; lower projection/input APIs are dependency context and have separate ownership/QA.

## Confirmed findings

### I15-01 — P2: restoring the last look gate can hide a different surviving cursor owner's UI

**Original owner/task:** SAME Neo.Tools.Move / `neo-audit-1023-b07-07`. **Exact current sources:** `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs`, `SetControllerEnabled` lines 726–733, `ReleaseControlInternal` lines 694–720, `ReleasePlayerGate` lines 880–893 and `SetPlayerGate` lines 896–904; `PlayerController3DPhysics.cs`, `SetLookEnabled` lines 422–427 and `HasExternalCursorControl` lines 465–469.

**Concrete trigger, source-confirmed / UNEXECUTED:**

1. Keep the player's existing default `CursorControlEnabled = true`, look enabled and Pause Look When Cursor Visible enabled. Assign/register controller A as that player's ExternalCursorLockController. A requests **look only**; controller B requests **movement only** for the same player.
2. A.ShowCursor then B.ShowCursor: B is top, cursor is visible/unlocked, look is held false by A and movement false by B. The independent gate baselines are correct.
3. A.DisableController sets A.ControllerEnabled=false **before** releasing A. A is non-top. Releasing its last look request restores the captured true through player.SetLookEnabled(true).
4. The player sees its assigned external A is no longer ControllerEnabled; it does not discover B through that reference. SetLookEnabled therefore calls SetCursorLocked(true) and writes the cursor Locked/hidden directly. ReleaseControlInternal re-applies the surviving controller only when the released owner **was top**, so it does not repair B's cursor request.

**Expected versus actual:** A's look gate should restore true, B's movement gate should remain false, and B's surviving visible/unlocked cursor request should remain authoritative. Actual source preserves the boolean gate baselines but internally locks/hides B's cursor; look can become active again because the visible-cursor backstop is also gone. A real component disable has the analogous enabled-flag path. This is an **internal cleanup side effect**, not an arbitrary external SetLookEnabled/base-intent write excluded by the documented snapshot policy.

The current new cursor fixture sets player.CursorControlEnabled=false at setup line 29. Its separate-gate case at lines 99–116 releases movement while another look owner remains; it does not cover releasing the last look request under a movement-only top owner. Those controls therefore cannot expose this default-enabled cursor interaction.

**Root-accepted minimal correction:** reconcile the surviving shared cursor request after internal gate restoration, avoiding duplicate cursor notifications and baseline rewrites. Preserve public SetLookEnabled's ordinary standalone behavior, serialized options, exact first gate values and explicit external-controller binding. Do not steal/rebind the player's chosen owner or fix it by leaving look disabled when B never requested look. No global input/base-intent framework is needed.

**Required meaningful acceptance, UNEXECUTED:** actual assigned-external A look-only + top B movement-only, with default CursorControlEnabled=true; disable A and assert B retains ownership/visible/unlocked cursor, look restores its first true baseline and movement stays blocked. Cover both closure orders/directions, initial false values, repeated cleanup, native disable/destroy when later permitted, and ordinary single-owner behavior. Observe actual cursor and public gate values plus event counts; reassertion must not duplicate B's unlock notification or overwrite captured baselines.

### I15-02 — P2, test only: CameraDrag counters leak across cases in one fixture instance

**Original owner/task:** SAME Move actor. **Exact source:** `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveCameraDragTests.cs`, `_starts` / `_ends` lines 13–14 and SetUp lines 17–30; TearDown lines 32–36.

SetUp creates a new object/controller and new listeners that increment fields on the fixture, but resets neither counter. Under the default shared fixture-instance lifetime, earlier Test/TestCase invocations leave values for later cases; teardown can additionally deliver an EndDrag notification. For example, consecutive InitialAngle parameter cases each start a drag, so a later case can observe `_starts == 2` where line 57 requires1. Later modifier/lifecycle exact-count assertions are likewise order-dependent. This is a source-level isolation defect, not a newly executed NUnit failure.

**Root-accepted minimal correction:** set both counters to zero at the **beginning** of SetUp, before creating the new controller or listeners/lifecycle work. Retain every exact event-count assertion and teardown. No runtime rewrite, new fixture or framework is needed.

**Acceptance, UNEXECUTED:** execute all 13 current CameraDrag cases together, including all four InitialAngle parameters and later modifier/disable/focus/pause cases; each begins with clean counts and preserves its existing positive motion/order assertions. Do not weaken counts or rely on executing one selected method in a fresh process.

## Managers / exact GM–UI contract

GM uses one instance `_transitionVersion`. State setter line60 advances it with unchecked +1 exactly once for an effective change, before `_state` assignment and state callbacks. Leaving a time-owned Pause restores the captured value before notifications, including zero and after the authoring pause option changes. Same-state assignment outside a state callback retains legacy notification without a version change; repeated current payload inside a state notification is suppressed. The marker is a snapshot of the common version, not a second counter.

The state argument/debug payload uses the captured announced value. Notification nesting restores its guard in finally; failure propagates with committed state/version retained. Gameplay helpers capture the expected version and check before their later broadcast, including **both sides of lazy EM.I initialization**. Nested Resume from State(Pause) therefore suppresses the old Pause. Composite Menu/Start/restart abort after a newer resumed/stopped observer flow; their unchanged-version allowance preserves existing virtual no-op Resume/Stop overrides. Win/Lose/End re-check after OnStopGame so a newer transition suppresses the obsolete outcome. Returning to the same state through intervening changes does not revive the old version.

The exact additive `public ulong TransitionVersion => _transitionVersion` matches all three PausePage reads. UI expects unchecked(previous+1) before Pause and accepts only that resulting Pause generation; release also checks the exact live current singleton. The dependency remains UI → Managers. No serialized counter/setter, rewind or reverse UI dependency was introduced. Existing fields/defaults/enum values and method signatures are preserved. Direct EM helpers remain explicit broadcasts; an already-entered UnityEvent's remaining listeners are intentionally not individually canceled. Arbitrarily alternating callbacks/raw time writers remain outside a general transaction guarantee.

Read all 34 current GM authored cases / 24 methods, including the 21 additional expanded phase cases and strengthened old PauseStateListener. Public state/time/version/event transcripts are substantive: baselines0/0.35/1; exact +1 inside state and Pause callbacks; no-op repetition; nested Resume/re-pause +3; same-state/no-rewind/fault recovery; all three terminal outcomes; Menu/Start/restart reentry; custom no-op hooks; lazy hub OnAwake transition; and an ordinary full command sequence preserving its complete event order. Tests use bounded callback reentry and capture actual public traces, not a private counter substitute.

The Edit fixture's explicit AttachForTest/ReleaseForTest lifecycle seams do not certify native GM destruction. Its inactive task-owned hub/manager and time restoration require the existing isolated/exclusive runner. The earlier Bootstrap6/singleton3 source scopes are preserved and are not newly re-audited or executed here. All 43 declared Managers cases remain UNEXECUTED as an integrated suite.

## UI — page channels, ownership and fault cleanup

PausePage registers its captured per-enable requests before external GM callbacks. The first channel baseline is retained across overlapping pages. Time requests select the minimum finite/nonnegative scale; a time-controlling **owned** manager holds zero. If GM joins page-held time, AcquireManager gives it the original shared baseline before its Pause. Resume of that exact owned manager precedes final page-time restoration, preserving GM's own time-restoration/observer order. Event-only GM time behavior remains separate from page time ownership; this review does not promise an event-only GM restores a page's channel itself.

Manager ownership is reference + state + exact transition version + current singleton, not name/type/final Pause alone. Prepaused, destroyed/replacement and externally resumed/repaused managers cannot receive the old group's Resume. Group bookkeeping is cleared before invoking Resume so reentrant requests do not inherit the old owned reference. Foreign current time-controlling pauses remain at zero when page time is released. This is scoped page ownership, not arbitration of arbitrary raw time/cursor writes or every external policy setter.

Cursor requests retain the first lock/visibility baseline. A remembered ForceLockedHidden exit applies only after the **last cursor requester** closes; a time-only or unrelated page does not extend cursor ownership, and cursor-only pages do not delay time restoration. Captured flags release correctly even when serialized authoring options change while open.

Reentrant synchronization sets a pending flag rather than recursively traversing the group, re-reads requests around GM callbacks, and performs at most16 reconciliation passes. A shared frame pass occurs at most once per frame; enable/disable calls still synchronize immediately. Exception cleanup attempts manager/time/cursor work, retains the first fault and rethrows through ExceptionDispatchInfo; the synchronization guard releases. Subsystem session reset clears shared ownership, and active pages can re-register from LateUpdate. This is a processing/ownership policy, not a bound on arbitrary user callback code or a promise to repair a deliberately failing virtual GM command.

### UI authored acceptance — UNEXECUTED

All three complete fixtures were read. They contain 20 methods / 26 expanded source cases; internal loops additionally exercise all three scene adapters and both closure orders. These are not discovery/pass counts.

| Family | Meaningful positive/negative controls | Boundary |
| --- | --- | --- |
| Single/overlapping pages | Exact baselines1/0.35/0, time inside Resume observer, one shared Pause/Resume, both closure orders, GM joining time-only owner in both orders | Actual activation/yields authored; no results yet |
| Foreign GM ownership | Already paused, destroyed/replaced, external resume/re-pause; no wrong-manager Resume, final foreign pause retained; later ordinary Resume positive controls | Exact generation dependency present in source, compilation/runtime still owed |
| Reentry / observer fault | OnPause closes page, throwing close still releases, nested State(Pause)→Resume produces no stale Pause/ownership | Real scene lifecycle source plus expected exception log, not manual copied ownership logic |
| Cursor/time independence | Both cursor closure orders, remembered force-lock and next-group forget, independent channels, minimum time requests, option changes while open | Native cursor/focus capability still needs observation |
| Mesh replacement rejection | Root point; inactive foreign owner under an owned branch plus a disposable sibling; live keys, mode, counts and hierarchy unchanged across all three scene renderers | Whole-set safety assertions, not merely an exception assertion |
| Normal replacement / append | Old ordinary points removed, new Bounce binding/identity present, independent inactive rig preserved; append retains protected/root branches | Positive controls prevent accepting a builder that rejects every call |
| Deferred Destroy boundary | Three Play methods loop all adapters, yield after rejection/success and assert both hierarchies/keys/modes/counts and old-point retirement | Authored native/deferred cases, all UNEXECUTED |

Layout preflight enumerates the full selected owned-point set before removal/creation/binding mutation or scheduling Destroy. Root equality rejects the owner GameObject. Each removed branch scans components including inactive descendants for another IUIMeshRigOwner. The second removal loop runs only after every selected branch passes. Nearest-owner resolver starts at the candidate transform, so root points and independent nested ownership have the intended classification. Ordinary replacement/append and non-rig disposable-branch content policy remain explicit; no editor Undo or general transactional hierarchy framework is added.

UI fixtures own/destroy their created hierarchies and probes, remove owned hub listeners, restore time/frame rate/cursor in finally and depend on the declared exclusive world. Native focus/cursor behavior, deferred destruction, reload-disabled repopulation, renderer/atlas/mask/raycast acceptance and all compiler branches remain owed. The runtime guide now links the separately owned Editor topic; that topic's paragraph handoff remains its owner's responsibility, not a runtime source dependency or an editor approval from this report.

## Move — coherent work apart from I15-01/02

### Separate player gates and default compatibility

Two private dictionaries keep look and movement ownership independently. First acquire captures the exact gate boolean; every controller registers even if another already disabled it. Local sets prevent duplicate requests. Release removes only that controller and restores the captured value only after the last owner, including an initially false value. Hide, unregister, release and lifecycle/reset paths use these records; destroyed player wrappers are removed without applying native state to them. Reset removes corresponding local ownership sets and clears records. This repairs the original premature re-enable defect, but I15-01 shows cursor-state reconciliation is still incomplete when the last look release has its own player auto-lock side effect.

The current13 cursor cases cover eight baseline/closure combinations, repeated calls, separate gates in one direction, unregister/re-register, Hide, cleanup and destroyed/replacement player baseline. They observe public LookEnabled/MovementEnabled. They intentionally disable the player's cursor path, so they are **gate-accounting controls**, not proof of default-enabled end-to-end cursor composition. Private callback invocation is cleanup simulation rather than native delivery. Keep these controls and add I15-01's missing default/directional cases.

The documented snapshot policy does not redefine baselines after arbitrary external SetLookEnabled/SetMovementEnabled writes. That limit and coordination of cutscene/pause setters are explicit; no global base-intent platform is demanded here. Existing cursor modes, lifecycle snapshot options, manual/automatic controls, external binding and serialized defaults must remain when fixing the internal side effect.

### Drag and initial equivalent angles

ProcessDragSample uses available pointer + held button + modifier to define an active interval, so modifier-after-button captures a fresh pointer and current pose. Initial/stationary samples return without assigning rotation. EquivalentAngle selects a full turn near the authored range midpoint with widened arithmetic; disabled/null axes avoid unused limits. Thus350 maps to−10 for−45..45 while unsigned/shifted ranges retain an equivalent turn. EndDrag commits inactive before notification and repeated release/loss/disable/focus/pause ends once. Zero-time samples update pointer baseline without accumulating later movement.

The historical pixels × sensitivity × axis speed × scaled deltaTime formula remains, with its frame-rate dependence; no silent new sensitivity mode or serialized rewrite. The internal seam is test visibility, not a new public input API/framework. All13 new drag bodies were read: four angle ranges, disabled-axis NaN limits, two positive sensitivity values, modifier reentry, missing pointer, disable/re-enable pose, focus/pause and zero-time recovery. They assert actual quaternion movement and counts. The runtime appears coherent, but exact count execution is blocked by I15-02's test-isolation defect until reset is added.

### Dense 3D grounding and retained buffer correctness

The ground query keeps the actual probe position/radius, configured mask and QueryTriggerInteraction.Ignore. Full count triggers capacity doubling and the same native OverlapSphereNonAlloc retry until the result fits. Only that current count is post-filtered for own hierarchy; retained tail entries cannot become ground. Initial16 is capacity, not an accepted result limit; exact-fit also retries conservatively. Capacity is retained per player, allocating on growth. Distinct player geometry remains distinct fixed-step work, including the existing pre-authority probe; no unsafe global frame cache or measured performance claim is introduced.

The four new source cases use real native geometry/query APIs. For16/48/80 child self colliders plus floor, an uncapped Physics.OverlapSphere oracle must itself exceed16, and every returned collider must be found in the grown buffer. This ordering-independent membership check fails a truncated array even if a lucky prefix included the floor. Public IsGrounded and one landed notification are required; repeated check does not land again, moving floor away must become airborne despite retained capacity. A separate mask/Ignore-trigger negative case has a nontrigger/in-mask positive floor control. These are genuine authored physics controls, **not executed native proof or fake counters**.

### Depth forwarding and RNG isolation

ScreenPositioner runtime forwarding/defaults remain; only tooltip/XML explain camera-space Z beyond near clip. Full new depth fixture reads compare actual public placement with direct Camera.ScreenToWorldPoint at pixelRect origin/edge plus nearClipPlane+depth, for orthographic/perspective and both Use Depth values. Use Depth=false preserves prior **world Z**, not full projected pose. A rotated-camera/nonzero-viewport orthographic migration compares all XYZ after oldDepth−nearClip; a matching 2D XY result alone is not accepted. No scene/prefab depth values are rewritten. The lower Extensions projection is dependency context, not self-approval of that separately owned runtime.

The retained force fixture now captures UnityEngine.Random.state before test work and restores in teardown finally **after** owned-object cleanup, so even cleanup callbacks cannot leave its RNG changes. The force-range positive control remains. The other retained30/Physics2D9 cases carry their prior source disposition; Physics2D fixture hash matches the original request. I did not claim to re-read all39 unchanged legacy bodies here. All35 new Move cases and relevant RNG delta/helpers were read directly, including untracked additions ordinary git diff omits.

## Documentation, source coverage and later acceptance

Read all11 current owned guides: GM/EM/Managers entry; PausePage/UIMeshRig/UI entry; CursorLockController/CameraRotationController/PlayerController3DPhysics/ScreenPositioner/Move entry. They document the new generation API, intentional11.0 stale-broadcast suppression, exact pause ownership/order, whole-set replacement safety, snapshot gate limitations, legacy drag tuning and explicit full-depth migration. Existing renderer/native claims outside this phase are historical documentation, not new execution evidence from this QA. I15-01 must be reconciled before claiming complete cursor default compatibility; I15-02 must be fixed before interpreting a whole drag fixture run.

Full current bodies described above and all new regression bodies/helpers were read. All relevant baseline-relative runtime hunks were inspected; the initially truncated combined cursor diff was re-read in a smaller complete scope. For PlayerController3DPhysics, the complete new ground hunk and relevant gate/cursor/startup/input/authority context were read; its unrelated unchanged movement/network implementation is not re-approved wholesale. Other original UI/Managers/Move audit patches remain under their prior native review coverage. No nested Editor/CharacterController scope, optional framework or pending consumer is silently approved.

Root should return I15-01 and I15-02 to SAME Move actor, retaining exact first baselines, event semantics and all meaningful tests, then independently review the corrected source-final receipt/snapshot. Managers/UI need no additional bounded source correction from this review. After **every** writer finishes, compile/discover and run the frozen closure on installed Unity6.3/6.6 with nonempty suites: Managers game-state and existing Bootstrap/singleton, all26 authored UI cases/adapter loops, all74 current Move cases plus corrections. Source counts are not discovered/pass counts.

Actual native cursor/focus, disabled/destroyed owners in both directions, shared-page overlap, reentry/fault logging, reload-disabled reset, default/event-only GM channel combinations, real legacy/Both/New input modifier/device transitions, dense physics and manual depth migration still need execution. Physics/profile/allocation work must be measured, not inferred from reuse/count assertions. Metadata/GUID/UTF-8/package and optional-configuration compiler validation remain central barrier work.

Performed only source/doc/request/receipt/diff/reference reads, recorded-PID read-only snapshots and SHA256 bookkeeping. Wrote only this report. No Unity/MCP/compiler/build/test/discovery/product checker, install/service/process management, child, product edit or Git mutation. Execution remains **UNEXECUTED**.

## Reviewed fingerprints (SHA256)

Repository-relative paths identify the reviewed current snapshot, not compiled results. All three receipts and critical runtime/fixture hashes were unchanged at the end-of-read gate; later same-owner corrections need a new review.

| Artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/Neo.Tools.Managers.implementation.md` | `c974efb1edf66f548cd1e79ec25ebba4cda89f2f3b8d1b496092cde062d144ff` |
| `Docs/Audits/2026-10-23/Neo.UI.implementation.md` | `03f999fe9b8916316010dea0bae4cfdc40a765d9ee663844f9b5ecdb145c55e1` |
| `Docs/Audits/2026-10-23/Neo.Tools.Move.implementation.md` | `d2cb154f45f0a3ae17f5b7bf6dea1e7d1f358137393ad445aff6b1b88182c15d` |
| `Assets/Neoxider/Scripts/Tools/Managers/GM.cs` | `92e31e196583d879b57a28142d9cf68710a452e11bc32ecc039295ec7fa8b7d8` |
| `Assets/Neoxider/Scripts/Tools/Managers/EM.cs` | `33069a53ab42b5bdc28a88d129c56ffadd44535b3a0cbdbd01420b8ad73c0876` |
| `Assets/Neoxider/Scripts/UI/PausePage.cs` | `0af88ddcd149b57e83af1f81d41c272255c58a30803fb3e8ce68f788c5d635f1` |
| `Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs` | `fe8ddb6f7dcea8a0b2c257ef0765936a6f6017f9f113f381788d2fd8a9c1f271` |
| `Assets/Neoxider/Scripts/UI/MeshRig/IUIMeshRigOwner.cs` | `72ab787322e2497774dc60f4dde38e4a066c2f8a42b73a3c58c0088dac8b8e25` |
| `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs` | `bb9639130d57c3c6a554ce3944e64dddcb665584eaa70abc4285d9e6b0b05943` |
| `Assets/Neoxider/Scripts/Tools/Move/PlayerController3DPhysics.cs` | `aa2a9c9252552141d3f91e40fa3af295dcd52ebc9df4939d172e88982dfcb9cc` |
| `Assets/Neoxider/Scripts/Tools/Move/CameraRotationController.cs` | `206f8e363dcc5026429cdc67bef798018fa0c137703249baaf07a85fe0a82f71` |
| `Assets/Neoxider/Scripts/Tools/Move/ScreenPositioner.cs` | `266ba89df1f8a48e97ac9745838d8918b553ecc2d2d4527c82ab64df94968418` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsManagersGameStateTests.cs` | `273de1fad62177429d60b53da16d8933b1ff6b19f0b82208a8d5e7443b1fc34e` |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIRuntimeReplacementTests.cs` | `35eebe5d933991c2999be85dfa0318765162f54b738a96fc988b53cdf65348a4` |
| `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIRuntimeReplacementPlayTests.cs` | `20e6b69220e8893cc138f7aeeca2611c8962fab26727b28e61044b357f42766c` |
| `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIPausePageTests.cs` | `91d801d2092a94ff5757307af79b2633a62188c0d9cdf6d9fd8f5f2797b09d9a` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveCameraDragTests.cs` | `ca4ff623a2590caddd04f06b485019400026a0e2dff6f179fa182ee2560775e2` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveCursorOwnershipTests.cs` | `f8d9e3f708bafb919e1e0b80ea4830008d5b9bc0fdce31f13d2551778f38829b` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveGroundQueryTests.cs` | `eb1fd80002d7633dd27a551ab87ddc78c47054efd789c446785fd39066430eea` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMovePhysics2DTests.cs` | `b7bf04f5591982f9910ba7c65adfb1983e7e6a8591781f599d4f1aae14eb3737` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveRegressionTests.cs` | `0aa5838e3e947defdfc28345ad35c042f36821d88b9cc64601fe74c338663c80` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveScreenDepthTests.cs` | `ff17ea4f3ab65ad687063595863cf236daf02834a4035a29acaa19b3f39d6ca1` |
| `Assets/Neoxider/Docs/Tools/Managers/GM.md` | `91f210f6eb49c185a544fa7d5f1f54dc10db59cfe94e9dd3bd1ee86c79ca609c` |
| `Assets/Neoxider/Docs/Tools/Managers/EM.md` | `bbd9c6f4304eff22e9955a0d57d851db080c1b20621d9c6d2ae75223d6ecbd8b` |
| `Assets/Neoxider/Docs/Tools/Managers/README.md` | `8c981c14cb21e1e54d83030488db879d34aa871b5a85dc8572c95586b52b3c64` |
| `Assets/Neoxider/Docs/UI/PausePage.md` | `4fc7bf8288b5eb2747b484a72c7733fb41e4c511ee03a512b8f1dd0069fb1c02` |
| `Assets/Neoxider/Docs/UI/UIMeshRig.md` | `fa03ea95c99233c311eeed74f340f043823718b0d32cf4e2ec5258258833b52c` |
| `Assets/Neoxider/Docs/UI/README.md` | `1668633351a3e57800640050d7638b4582e937d80f61754ed5d845e7f765a2e1` |
| `Assets/Neoxider/Docs/Tools/Move/CursorLockController.md` | `767bbe59b68324bb09112461e66488e01a8608000a89cd6328493bfc8fe540a2` |
| `Assets/Neoxider/Docs/Tools/Move/CameraRotationController.md` | `9af3b8c5a62ce6ce4646b3d2c95a3bb062befd6b0c238a7fd586431686efb09a` |
| `Assets/Neoxider/Docs/Tools/Move/PlayerController3DPhysics.md` | `0225ced895b0b8392a5472c69a10563d3a98e3579b64dba78ce1678e7bd18206` |
| `Assets/Neoxider/Docs/Tools/Move/ScreenPositioner.md` | `5e26ddd676002833bc9fefa8ef329e4009a398467850064c353dacd4e6416d4f` |
| `Assets/Neoxider/Docs/Tools/Move/README.md` | `63465a7c5db4ecd7cab5ecfbd98b847e040ff778e80801b15d5d5ab9c94402f4` |
