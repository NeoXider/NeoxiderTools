# Neo.NPC — implementation receipt

Status: **SOURCE-FINAL, P5 E17 partial-path fixture correction completed 2026-10-25;
NOT COMPILED OR NATIVE-VERIFIED AFTER THIS CORRECTION.**
Root reports that the preceding interrupted S3 actor attempt ended with exit code 3; neither
that attempt nor the older source-final receipt established S3 completion. This recovery made
the fixture changes documented below and retained the previous implementation. No compiler,
Unity, tests or product checker was executed by this worker during this correction.
Branch: `demo`. Target: `11.0.0`. Same original worker/session, `gpt-6.1-sol`, reasoning `max`.
Initial mode correction completed: 2026-10-24. S3 source recovery completed: 2026-10-25.

## Published API and dependencies

- **Additive public APIs: none.** Existing signatures and serialized fields remain intact.
- Completed action: **NPC-INITIAL-MODE-REENTRY** (P2), also reviewed as NativeReview-C C-26.
- Completed source correction: **NPC-NATIVE-NAVMESH-POSITIVE**, action-index **E78 / U**.
- Completed source arrangement correction: **P5 E17**, patrol partial-path prerequisite.
- No pending runtime API from another owner. The focused lifecycle regression lives in the
  existing `Neo.Editor.Tests` assembly and uses its existing `EnterPlayMode` support for real
  Unity callbacks when executed centrally. No Play assembly reference or shared configuration
  change is required; the current Edit assembly already references `Neo.NPC` and both test runners.
- All compilation/test/Unity/product validation remains behind the global source-writer barrier.

## Exact implementation files

| File | Completed source change |
|---|---|
| `Assets/Neoxider/Scripts/NPC/NpcNavigation.cs` | `Awake` captures the initial mode, sends the existing initial notification, then reconciles a changed deferred mode with `invokeEvent: false` and `enter: false` before setting `initialized`. The first behaviour entry remains in `OnEnable`. |
| `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCLifecycleTests.cs` | Two new coroutine regressions using real activation-driven callbacks and an owned transient NavMesh. |
| `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCLifecycleTests.cs.meta` | New test metadata, GUID `af64c4ce909144a389a82d8de64a5f21`. |
| `Assets/Neoxider/Docs/NPC/Navigation/NPCNavigation.md` | Documents the single initial notification, its initial-mode argument, last deferred selection and first `OnEnable` entry for C# and Inspector handlers. Existing canonical README link and module backlink remain in place. |
| `Docs/Audits/2026-10-23/Neo.NPC.implementation.md` | This source-final/dependency receipt. |

The correction reuses the existing mode dispatcher and Follow/Patrol/Combined behaviours. It adds
one explicit local variable and no framework, public method, serialized field, dependency or asset
migration. Normal post-initialization `SetMode` behaviour and all earlier coherent audit edits remain.
An initial event handler changing modes now starts the behaviour matching the final `Mode`; it no
longer starts the superseded behaviour. No extra startup mode notification is introduced.

## Authored acceptance sources — UNEXECUTED

1. `Navigation_InitialModeListener_EntersPatrolOnceWithoutEnteringFollow`: activate an inactive NPC
   with FollowTarget selected and a listener requesting Patrol. Assert the exact single-mode-event /
   callback-return / patrol-start sequence, `Mode == Patrol`, zero follow-target entry notifications,
   zero follow path submissions, and a complete native path ending at the distinct patrol waypoint.
2. `Navigation_InitialModeListener_UsesLastDeferredModeWithoutStartingPatrol`: start with Patrol;
   its initial listener requests Combined then FollowTarget. Assert only the initial Patrol mode
   notification, zero patrol starts, one follow-target notification and accepted follow request,
   and a complete path to the follow target rather than the distinct waypoint. Counts remain
   unchanged after native path completion.

Both cases enter/exit Play Mode through `UnitySetUp`/`UnityTearDown` in the existing Edit assembly;
neither invokes private lifecycle methods. Objects/listeners are prepared before activation. The
fixture builds one remote transient NavMesh for the configured agent type and removes only its own
data after destroying owned GameObjects. It requires real native binding and complete path output,
with a three-second unscaled deadline; no skip or no-exception substitute is used. It does not use
the unavailable `NonParallelizable` attribute; central execution must remain isolated/exclusive.

Before S3 recovery there were 29 authored NPC cases across three fixtures. The recovery retains
all original cases and adds two controls: navigation 18, components 11, initial-mode lifecycle 2,
for **31 authored cases**, plus the existing eight decision cases. These are source declaration
counts, not new execution results.

## Actual work and remaining evidence

- Read the current implementation contract/request, own historical audit/plan and relevant owned
  navigation/core/test/docs source. Re-read the contract/request after the resource interruption;
  preserved the partial patches rather than replaying them.
- Reviewed the final source and documentation diff and complete new regression source. Reference
  enumeration confirms the existing Edit assembly reference. An ignore-independent Assets metadata
  search finds the new GUID only in its own `.meta` file. These are source reads/bookkeeping only.
- No compiler, Unity/MCP, test runner, product checker, installer, service, commit/push or child agent
  was launched. Historical static checks in the audit are not new-source validation results.
- **Pending source/API dependencies: none.** After every writer is finished, the coordinator's
  verification operator must compile/discover the integrated snapshot on Unity 6.3 and 6.6 and run
  the three prefixed fixtures plus existing decision tests. In particular, confirm real initial
  native agent binding, transient NavMesh creation, event counts and path completion for these two
  new cases. Any fixture adjustment must use observed native evidence. Earlier partial-path,
  remaining-distance/velocity and Animator setup evidence debt remains as recorded in the audit.
- No supported-version branch is newly introduced. Broader Unity version support, package UTF-8,
  metadata/GUID and product checks remain central acceptance work after the source barrier.

The original audit and plan remain historical artifacts. Optional planner/backoff and full takeover
snapshot frameworks remain proposals.

## S3 native-precondition correction and recovered source

### Read evidence and diagnosis

Read every assigned case/message/stack in action-index E78 and the corresponding XML fixture.
All **16/16 navigation cases failed** at the shared `CreateAgent` native `Warp` precondition
(`:390` in the S3 copy), with `Expected: True / But was: False`. They did not reach their
behaviour assertions. The source used ordinary Edit `SetUp` and unregistered Edit agents.
This establishes a fixture/context correction; it does not establish a navigation runtime bug.

The same historical Edit XML records components 11/11 and initial-mode lifecycle 2/2 passed.
The latter already enters Play Mode. These are attributed S3 results, not this worker's test
execution and not acceptance of the new fixture. The supplied Play XML has no NPC suites.

Read artifact SHA-256 values:

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`:
  `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`:
  `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`.
- `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json`:
  `df467fde346b0a1f7eca09a82b972f2e7532b7274758981969400628b1770e97`.

### Actual source changes and retained oracles

Changed only `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCNavigationTests.cs` and this
receipt during S3 recovery. The earlier initial-mode/runtime/docs patches remain intact.

- Top-level `UnitySetUp` yields `EnterPlayMode`, requires an actual increasing native frame,
  builds/registers the same owned two-island NavMesh, verifies its agent type and both sampled
  islands, then activates one native agent per case. Registration and successful `Warp` must
  precede the test body. `CreateAgent` places that case's already registered agent and retains
  both exact positive `Warp == true` / `isOnNavMesh == true` assertions.
- Retained all 16 case names, including both original `TestCase(true/false)` instances. The
  installed Test Framework's `UnitySetUpAttribute` documentation explicitly supports ordinary
  `[Test]` methods after a Play transition; public core-contract calls stay ordinary tests.
- Component cases now use actual AddComponent/Awake/OnEnable, enabled transitions, Update and
  target destruction. Removed the navigation fixture's private lifecycle reflection entirely.
  Movement-start now requires actual native movement and captures positive native velocity.
  Exact one-start/one-stop counts and repeated Stop remain asserted.
- Partial follow/patrol cases still require CalculatePath success, actual `PathPartial`, accepted
  SetPath, finite settled remaining distance within the original stopping distance, and zero
  arrival/waypoint notifications. Their existing 100-unit stopping-distance arrangement and
  every original 0.2-unit destination tolerance remain unchanged.
- Combined cases retain accepted initial manual-update paths, target replacement, destruction,
  missing-route stopping and retry. The unreachable case pauses through a public Stop listener
  only after the first failed attempt completes, then moves the target and calls Resume. This
  preserves exact attempt counts 1 then 2 without an uncontrolled number of native Updates.
- Added `Follow_CompletePath_ReportsOrdinaryArrivalOnceWithRequestedPayload`: actual complete
  native path and movement, exactly one arrival with the requested Vector3 payload, one accepted
  path event and no duplicate arrival on later public core ticks.
- Added `Follow_OwnedNavMeshRemovedAndRebound_PreservesFailureAndRecoveryOracles`: positive
  complete path first; disable the owned agent/remove only owned data; require missing surface,
  rejected update and no extra accepted-path/arrival events; restore the same data and agent,
  require real rebinding/Warp and the second accepted complete path. Foreign surfaces cause a
  failed precondition rather than deletion of foreign data.
- Top-level `UnityTearDown` destroys only owned objects/data, restores the captured Random state,
  then yields `ExitPlayMode` directly. No nested Play transition, helper extraction, shared ref,
  unavailable NUnit attribute, skip, error whitelist, mock path or production fallback was added.
  Three-second unscaled bounds also require native frame-count progress, not Editor polling alone.

No production/public/serialized contract or setting changes in this correction, so the existing
canonical navigation documentation needs no further contract edit. Combat, target-finder,
animation and the other two NPC fixtures were not changed. No test/helper file was moved, no
GUID was changed and no user/foreign scene, provider or session was reset or deleted.

### Current file fingerprints

SHA-256 of final source bytes; hashing is artifact bookkeeping, not compilation or a product check.

| Current path | SHA-256 |
|---|---|
| `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCNavigationTests.cs` | `a4b0129985f29a202a9ef9847243b965e17ca0bb135c46295dec8499ef2301bb` |
| `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCNavigationTests.cs.meta` | `8e29128b2c1d5354c21c92c9e503cfcf8fe2c4bd984774197a9a5673ff28fd11` |
| `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCLifecycleTests.cs` | `3117544612afd1642ca975f19e030c2433f90e4f284791eee8d9cebd716b0b51` |
| `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCComponentTests.cs` | `8d3889a7e237b0726c5d7b3086441aefaba85b87f042d7a7ba4c977362eff855` |
| `Assets/Neoxider/Scripts/NPC/NpcNavigation.cs` | `e477ea8e98d67a415dfa270ff5ff03fc5471ac8d9c1d121d211f4a009d8dec47` |
| `Assets/Neoxider/Docs/NPC/Navigation/NPCNavigation.md` | `9d9e89df615c55e5e03deae64706e9ddbac45275f004e8fa98f9a4ebe56dc276` |

The navigation fixture's pre-correction SHA-256 was
`2530695e09fb9899e9424359e4c7bd80d7693352542d9c6ea3ca28dc04585f25`.
Its metadata remains untouched, GUID `b3fc084ffd6d46208591457a74b9a070`.

### Dependencies and honest completion boundary

The reservation map was read before each edit batch. It reports `source-final` for the primary
helper extraction, has no NPC reserved entries and no remaining source dependency. Read map hash:
`14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7`.
No Fixtures/shared configuration or another actor's file/receipt was edited.

**No unresolved source/API/helper dependency.** Source reads, XML/JSON receipt reads, source diff
review and byte hashes were the only verification/bookkeeping actions here. The prior exit=3
attempt is explicitly superseded by this completed source recovery, not by an asserted test pass.

**Unresolved native evidence:** after every writer finishes and root declares a new freeze,
centrally compile/discover on Unity 6.3/6.6, require all 18 navigation cases (no lost original
names/boolean variants), and execute with real native frames. Confirm agent registration/Warp,
both island samples/partial paths, finite distance, automatic callbacks, exact movement/retry/
destruction counts, ordinary arrival payload and owned remove/rebind recovery. Confirm teardown
and Play exit after both success and failure. Re-run the unchanged 11 component and two initial-mode
cases plus eight existing decision cases on the integrated snapshot. No current native pass,
compiler pass, package check or supported-version parity is claimed. Source writes stop here.

## Oct25 v2 compiler correction — source-final, execution pending

Root reports actual Unity 6.3 native job `c8f8b7e6aaa445d89eb55d1292f9be46` stopped with
compiler exit 1: `Audit20261023NeoNPCNavigationTests.cs(58,38)`, **CS1061**, because
`NavMeshData` has no `agentTypeID` member. This is an actual compile failure, not a native
behaviour result. I introduced that unsupported redundant assertion in the S3 fixture recovery.

The entire fixture change in this follow-up removes that one assertion. It retains
`settings.agentTypeID`, the build settings, typed `NavMeshQueryFilter`, both positive island
samples, exact prepared-agent type/binding/Warp assertions, all 18 cases, every behaviour/
payload/count/ownership oracle and teardown. Exact typed sampling and native agent registration
remain the agent-configuration evidence; no replacement unsupported API or reflection was added.

Before this one-line correction the fixture SHA-256 was
`11e972b8b05e7901dcc38ba554d86983d8a7b80a7a9f56be677c0d5fac05eaf9`.
The v2 fixture SHA-256 was
`f03c66560ea05513c9345a6ef6f56dc781461cc708b23569c841cc4d1590c637`; the P5 correction below
supersedes that fingerprint in the current-file table. Only this fixture and this receipt changed; public/runtime/docs APIs,
settings, metadata/GUIDs, shared trackers and frozen evidence remain untouched.

No source/API dependency remains. No Unity/compiler/tests/checkers/process-control operation
was run here. Fresh compilation on installed Unity 6.3/6.6 and all native execution remain pending
root's next permitted verification after the source-writer barrier. The existing failure and
native evidence debt remain open; removing the invalid member access is not a compile/pass claim.

## P5 E17 — stable native partial-path arrangement (source-final)

Read the common P5 source contract, exact assigned request, frozen fixture and both actual Edit
XML cases. P5 snapshot identity is
`956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`, under
`Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261025T151934Z/source`.
The frozen and pre-correction working fixtures both hash to the v2 `f03c6656...` value above.

Actual historical result in **both Unity 6.3 and 6.6: 17/18 navigation cases passed, one failed**.
`Patrol_PartialPathWithinStoppingDistance_DoesNotReportWaypointReached` failed at line 260:
the calculated path was Partial and SetPath was accepted, but the agent stored Complete after
native settling, before `core.Tick`. The zero-waypoint-event guard was not reached. Read evidence:

- `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/20-full-edit-graphics.xml`, SHA-256
  `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd`.
- `Local/Audits/assembly-2026-10-23/unity/final/p5/u66/Receipts/20-full-edit-graphics.xml`, SHA-256
  `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b`.

Source trace: `NpcPatrolCore.StartPatrol` resolves the waypoint and submits asynchronous
`NavMeshAgent.SetDestination`; the old fixture immediately submitted `SetPath` before settling
that request. Native automatic replanning was also left enabled. The XML does not identify which
native operation replaced the stored path, and it does not demonstrate a patrol-core defect.

Changed only that existing test method and this receipt. The owned agent temporarily disables
`autoRepath`, restored in `finally`. The fixture starts patrol through its public API and checks
one start / active patrol; stops movement while arranging; waits for the initial destination
request to settle; clears its path through public `ResetPath`; and requires no pending/current
path before calculating and installing the genuine disconnected Partial path. After a real native
frame and finite-distance settling, it still requires `PathPartial` and remaining distance within
the original **100** stopping distance. It checks zero waypoint events before/after Tick, retains
active patrol, then advances another native frame, requires Partial again and repeats Tick with
one start / zero waypoint events. No status is injected and no private lifecycle is invoked.

Both islands, typed samples, matching agent type, real registration/Warp, all **18 existing case
identities**, complete-path arrival/payload and owned remove/rebind controls remain intact. The
unsupported `NavMeshData.agentTypeID` assertion stays removed. No timeout or tolerance changed;
no foreign NavMesh, scene or provider was touched. No runtime/public/serialized contract changed,
so canonical module documentation remains accurate without another edit. No shared/helper file,
GUID, frozen artifact, audit or plan was changed; the reservation map was checked before edits.

Current fixture SHA-256:
`a4b0129985f29a202a9ef9847243b965e17ca0bb135c46295dec8499ef2301bb`.
No source/API dependency remains. Only artifact reads, source diff review and hash bookkeeping
were performed; **no compiler, Unity, tests or checker was executed**. After all writers finish and
root freezes source again, require native compilation and all 18 navigation cases on both versions,
particularly stable Partial across both frame boundaries, finite distance within 100, one patrol
start and zero false waypoint arrivals. Current native stability and a new pass remain unverified.
