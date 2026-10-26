# Neo.Tools.Other — 11.0.0 implementation receipt

Status: **SOURCE-FINAL Root continuation; corrected full-order native acceptance PENDING.** Original author history and failed execution evidence remain below; Root completed the bounded fixture correction after subagent usage limits.

## Dependency/API notice

No additive public API or assembly reference is required by these actions. Dependents continue using the existing `Neo.Tools.AiNavigation` methods/properties/events. Request identity and scheduler bookkeeping remain private. No other owner's API is awaited.

E14 adds only a fixture-owned clock probe and clarifies CameraShake's existing SDK clock contract. CameraShake runtime, public/serialized API, DOTween settings/driver and other owners' resources are unchanged.

## Completed actions

| Action | Source result | Acceptance status |
|---|---|---|
| `OTHER-NAV-DUPLICATE-SCHEDULER` / A-34 | Removed the private path coroutine and its handle/start/stop paths. One explicit initial submission and one scaled-time Update interval now own follow refresh. Accepted submissions reserve the interval before callbacks. A pre-Start accepted/stopped command is preserved by startup. False-to-true AutoUpdatePath refreshes once; unchanged assignments are no-ops. Stationary targets retain periodic refresh/recovery. | Relevant cases Passed in pinned p5 6.3/6.6; no new execution. |
| `OTHER-NAV-CALLBACK-GENERATION` / A-35 | SetTarget commits accepted-request/public/native movement state before onPathUpdated and has no stale mutation afterward. Private request generations guard status dispatch after arrival/blocked callbacks and combined-mode start/stop-following continuations. Stop, disable, resume, accepted replacement, and return-to-patrol invalidate stale continuations. | Relevant cases Passed in pinned p5 6.3/6.6; no new execution. |
| `OTHER-NAV-NOPATH` | Private active-request identity, accepted destination and submission frame distinguish an accepted request from idle/stop/pending/same-frame submission. A resolved missing path outside stopping distance emits one blocked episode and effective PathInvalid status. Already being within stopping distance counts as arrival. Ordinary complete/partial handling remains; a fresh accepted request resets reached/blocked state. | Real native ResetPath and retained controls Passed in pinned p5 6.3/6.6; no new execution. |
| `IOTHER-01 / P2` | Update now observes the previous accepted request's settled status before an optional interval refresh. The existing pending/submission-frame guard is retained. Generation/lifetime fences after mode/status dispatch prevent obsolete, disabled or destroyed continuation into automatic refresh/animation; the automatic callback returns continuation eligibility before animation. One scheduler and interval reservation before callbacks are preserved. | Five cases Passed in pinned p5 6.3/6.6; no new execution. |
| `E71 / L` (7 failed cases) | Removed the CameraShake fixture's Edit-mode SendMessage Awake. Top-level UnitySetUp admits a real Play lifecycle for all eight existing cases, including both parameterized channel-restoration cases. Creation asserts playing/active state; UnityTearDown exits only a Play session entered by this fixture. Existing transform, tween, event-count, replacement and paused-time oracles are retained. | Seven synchronous cases Passed in pinned p5 on both versions. Unscaled case Failed 6.3 / Passed 6.6; current E14 delta UNEXECUTED. |
| `E72 / U` (7 failed cases) | Moved EnterPlayMode out of the two private navigation verification iterators into their seven top-level UnityTests. Helpers and CreateNavigation explicitly require playing state before native setup. The existing ready/on-NavMesh positive, real movement/arrival, patrol-wait, Stop/disable/destroy and Resume assertions remain. | Seven cases Passed in pinned p5 6.3/6.6; no new execution. |
| `E73 / U` (2 failed cases) | Fast-cadence observation waits for an actual Time.frameCount change with a realtime deadline. Same-frame Editor polling is no longer counted as a new frame. Original exact consecutive-frame, 0.15 scaled step, interval 0.1, every-frame submission, stationary-once and arrival-replacement assertions remain unchanged. The callback-cancellation variants use the same barrier. | Cases Passed in pinned p5 6.3/6.6; no new execution. |
| `NI52-01 / P2` | The shared four-case patrol cancellation helper now requires at least 0.35 scaled seconds after cancellation, actual native frame progression, and a further native frame after the window. It reuses the unchanged local frame barrier under one three-second realtime deadline. Missing frame/scaled progress or deadline overrun explicitly fails the precondition. Exact route-index/stopped/patrol/count negatives and Resume/index1 positive remain unchanged. | Four cases Passed in pinned p5 6.3/6.6; code/hash retained. |
| `p5 / E14` | Existing paused-shake case first requires actual updates of a separate owned Normal/independent clock probe, active SDK driver, positive finite SDK/owned clocks, native frame/unscaled-time progress and preserved pause. Root active/play/duration/update/elapsed state is traced. Probe readiness and the shake share the original one-second deadline; no manual tween update or wider timeout. Exact completion1, stopped state, disable/no-restart and starts2 remain; stop-count0→1 is added. | Source work complete; **UNEXECUTED**. Historical runtime-versus-fixture cause is unresolved until new native traces. |

`TrySubmitPath(Vector3 destination, bool restartArrival)` is **private**, centralizing successful native submissions and their bookkeeping. Combined-mode accepted submissions use the same tracking and avoid resubmitting an already active identical target at chase activation. There is no new navigation manager, callback transaction framework, mandatory component, or package dependency.

The review's additional fixture lead is addressed: temporary SerializedObject wrappers use explicit using scopes and are disposed before coroutine waits.

## Exact implementation-phase files

1. `Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs` — the four actions above; existing coherent audit fixes retained.
2. `Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherNavigationTests.cs` — the previous 19 cases retained, five IOTHER-01 cases added, with native-arrival/frame-cadence assertions and exact timing cleanup; E72/E73 correct their lifecycle admission/frame observation, and NI52-01 establishes the post-cancel scaled/native observation window. Existing ResetPath positive controls and ordinary cadence/moving-target/stop/disable/replacement cases are preserved.
3. `Assets/Neoxider/Docs/Tools/Other/AiNavigation.md` — public C#/Inspector scheduler controls, pending/idle/accepted path semantics, captured destinations, reentrant observers, actual-speed versus requested-movement distinction, SetDestination's acceptance result, and settled-status-before-refresh/canceled frame-tail behavior.
4. `Assets/Neoxider/Docs/Tools/Other/README.md` — canonical backlink and the corrected status/refresh ordering contract.
5. `Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherCameraShakeTests.cs` — E71 replaces forced lifecycle invocation with native top-level setup; E14 adds native clock admission/eligibility and root diagnostics to the same paused-shake case. All eight identities and substantive assertions remain.
6. `Assets/Neoxider/Docs/Tools/Other/CameraShake.md` — clarifies independent playback's dependence on DOTween global clocks and native driver delivery.
7. This implementation receipt.

The E71–E73 continuation wrote **only the two prefixed fixtures and this receipt**. The subsequent NI52-01 continuation changed **only VerifyPatrolCancellation in the navigation fixture and this receipt**; its existing local frame helper, five fast-cadence cases, CameraShake fixture, runtime and guides remain unchanged. Neither continuation changed production/API/serialized settings. No new package file or helper component was created.

The p5/E14 continuation wrote **only the CameraShake fixture, CameraShake guide and this receipt**. Patrol/navigation and other utility fixes remain byte-identical. The clock probe is an owned nonrecyclable Sequence, killed in finally with UnityTearDown fallback. Only Unity timeScale is changed and exactly restored; SDK clocks/settings/driver are observed, never reset or rewritten. No foreign tween/scene/provider cleanup is performed.

No new package file/meta/asmdef was needed. Other owners' sources/fixtures/receipts, shared trackers/configuration, original audit/plan, scenes/prefabs and sample layout were not edited in this implementation phase. Earlier CameraShake/Spine/RevertAmount audit edits and documentation remain intact.

## Compatibility details

- Existing public signatures, enum values, event fields, serialized field names/defaults, component GUID and scaled gameplay clock are retained. Removed fields were private, nonserialized coroutine bookkeeping.
- Each SetTarget call now produces only its accepted initial onPathUpdated instead of duplicate immediate events. Automatic requests are at most one per configured interval; no elapsed-interval catch-up loop was introduced. Stop/disable suppress the automatic scheduler.
- The latest callback-issued command wins. A canceled/replaced request does not publish its stale trailing path status or continue its Update tail. Arrival/blocked then status ordering within a request is retained; settled status now precedes the optional automatic onPathUpdated in that frame. Callback exceptions are not converted into rollback.
- Arrival/blocked Vector3 payloads identify the accepted destination, including positional requests, rather than reading a subsequently moved Transform. SetDestination true means native acceptance, not eventual arrival or continued movement after an observer cancels it.
- The no-path notification uses an effective PathInvalid classification for a missing accepted route; the public PathStatus property still exposes the ready native agent's status. No native flags are mocked or rewritten. Idle and explicit Stop have no active request to classify. Resume only observes a retained native route or a new target submission.

## Authored acceptance source and historical execution

New methods in `Audit20261023NeoToolsOtherNavigationTests`:

- `FollowTarget_SubmitsOnce_AndUsesOneAutomaticInterval`: initial event count, minimum automatic spacing, false/true setting transition, and stop.
- `FollowTarget_DisableEnable_ResumesOnlyTheUpdateScheduler`: disabled silence and one resumed refresh without duplicate Start work.
- `SetTarget_PathUpdatedListenerStop_LeavesAgentAndPublicStateStopped`: synchronous public and native stopped state; no later automatic request/arrival.
- `PathUpdatedReplacementThenStop_PreservesTheLatestCommand`: nested target replacement and stop, accepted replacement destination, no stale restart.
- `AcceptedDestination_NativeResetPath_ReportsOneBlockedDestinationAndRecovers`: first requires a real native path on a ready agent, calls native ResetPath, asserts no pending/path while still on NavMesh, observes captured blockage/status once, then verifies arrival after a fresh accepted destination.
- `BlockedListenerReplacement_SuppressesTheOldInvalidStatus`: native path loss followed by callback replacement; old invalid status cannot leak into the new request.
- `IdleAndStoppedNativeResetPath_DoNotReportBlocked`: empty idle/stopped native routes are not failed active requests.
- `AlreadyAtAcceptedDestination_DoesNotReportNoPathBlocked`: a zero-distance accepted destination arrives without false blockage.
- `CombinedMode_StartFollowingListenerStop_CancelsTheTransition`: the chase transition cannot resume a callback-stopped agent.

Additional IOTHER-01 methods:

- `FollowTarget_RefreshDueEveryFrame_ObservesNativeArrivalOnce`: public SetTarget on a ready native agent, valid interval 0.1, public captureDeltaTime 0.15. Consecutive actual PlayerLoop frame IDs, scaled time/delta, and one accepted refresh per observed frame are asserted. Requires actual native travel, settled native arrival/captured destination, exactly one arrival, public reached state, no blockage, and four subsequent stationary frames with refresh still enabled and accepted every frame.
- `ArrivalListenerStop_CancelsTheDueAutomaticRefresh`: a real settled arrival callback stops both public/native movement; neither that frame's due refresh nor later refreshes may occur.
- `ArrivalListenerDisable_CancelsTheDueAutomaticRefresh`: disable during native arrival retains reached state and cancels the remaining automatic work.
- `ArrivalListenerDestroy_CancelsTheDueAutomaticRefresh`: immediate real adapter destruction during native arrival prevents continuation while the independent native agent remains on the NavMesh.
- `ArrivalListenerReplacement_PreservesTheNewRequestAndItsArrival`: a native arrival callback submits a replacement through public SetTarget, immediately observes fresh moving/unreached state, and requires the replacement's own native arrival exactly once followed by stationary frames.

The navigation fixture retains **24 authored cases** and CameraShake retains **eight**, including the Position/Rotation parameterized cases: **32 total**, with no lost cases. Source inventory is distinct from actual discovery/results below. The original 19 navigation cases, real ResetPath controls and existing initial/pending/stop/disable/replacement protections remain. No private lifecycle invocation, manual Update, fabricated native flags, new public seam or NonParallelizable attribute was introduced. CameraShake's existing root-field read supports public tween seeking in synchronous cases and clock observation in E14; E14 never seeks/completes/manually updates either sequence. Fast-cadence five-second and patrol three-second deadlines and all timing cleanup are unchanged. E14's one-second budget includes both native readiness and root eligibility, requires sufficient independent probe-clock progression after root submission, and asserts Unity pause throughout native updates. Fractional-motion, relative/nonrelative position/rotation, exact counts and reentry oracles remain. Current E14 source has not run.

## Read s3 evidence and 2026-10-25 correction

Read the exact assigned cases in `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` (SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`) and the E71–E73 entries in `full-edit-failure-action-index.json` under the same audit root. That earlier snapshot contains 32 own cases: 16 passed and 16 failed. E71 contains seven ShouldRunBehaviour assertions from CreateShake's SendMessage; E72 contains seven CreateNavigation readiness failures reached through nested EnterPlayMode helpers; E73 contains two exact-frame assertions reporting expected 1, actual 0. These are actual recorded failures, not passes of this correction.

Also read the assigned-name scope in `21-full-play-graphics.xml` (SHA-256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`): no cases from these two prefixed fixtures occur there. No unrelated Play group was changed. The 15 navigation cases that passed in the earlier Edit artifact include direct top-level native setup and the real ResetPath controls; they are retained as controls, not carried forward as current verification.

The E72 failed native attachment precondition does not establish a production AiNavigation/Warp defect. This correction first requires genuine Play admission and retains the original native on-NavMesh positive before classifying behavior. E73 now filters repeated Editor polls by actual frame identity without changing movement/capture cadence, disabling automatic updates or relaxing exact assertions. All Enter/ExitPlayMode instructions are top-level UnitySetUp/UnityTearDown/UnityTest instructions; private navigation iterators contain only setup/assertions/ordinary waits. No nested ExitPlayMode, ExecuteAlways, expected-log whitelist, Ignore, reflection-based lifecycle call or mocked native path was added.

Checked the current `runtime-fixture-relocation-reservations.json` before each edit. Its actor is `neo-audit-1023-b03-04`, status source-final; neither owned fixture nor this receipt is reserved. No reserved caller, helper relocation/extraction, Fixtures source/assembly, shared reference or foreign resource was modified. **No reserved work is deferred from this scope.** Cleanup remains limited to owned GameObjects, the owned NavMesh instance/data and observed owned tweens; no scene/provider/user session reset was introduced.

## NI52-01 bounded follow-up

Read all of `NativeImplementationReview-52.md`, SHA-256 `7a175eda6c06669a06ff63c97c40052fccd932ee423e1ca14e50388a57186bdd`, and the appended exact request. Its admission pins matched this actor's prior receipt `e560301533978bd274e59d4b091ce9c2e1126e6d7e0c81cc2a300f0415aae012` and navigation fixture `b41dc97f93927700458eee30a855a41b9cc96105c73ef161ecbd7bcb33d2626e`. The confirmed gap was fixture evidence, not a new AiNavigation runtime defect.

After the retained real waypoint-arrival positive and cancellation, the helper captures Time.timeAsDouble and Time.frameCount. Each observation waits through the existing WaitForNextFrame barrier until at least 0.35 scaled seconds have elapsed, exceeding the unchanged authored patrolWaitTime=0.2. It then waits for another actual native frame before observing absence of stale work. One monotonic unscaled Time.realtimeSinceStartup deadline bounds both stages to three seconds; failed scaled/frame progress or overrun is an explicit assertion failure, never a skip. The exact index0, IsMoving=false, expected IsPatrolling, reachedPoints=1 assertions and enabled retained-patrol Resume/index1 positive are untouched across StopPatrol, Stop, Disable and ClearingWaypoints. No clock, movement cadence, runtime wait, global cleanup or top-level Play transition was changed.

Actual NI52 follow-up work was scoped source/review/request/receipt reads, manual inspection of that helper delta, reservation reads and raw SHA bookkeeping. Native execution was pending at that handoff; the subsequent pinned p5 outcomes for the unchanged helper are recorded below.

## P5 E14 — actual discrepancy and bounded correction

Read the exact E14 XML nodes on both versions and all own-case outcomes at frozen source `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`. Under `Local/Audits/assembly-2026-10-23/unity/final/p5/`, the original `u63/Receipts/20-full-edit-graphics.xml` SHA-256 is `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd`; the `u66` counterpart is `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b`. Own outcomes were **31 Passed / 1 Failed on 6.3; 32 Passed on 6.6**. The only failure was E14's exact completed1 assertion observing0 at old line215. The passing 6.6 node also recorded two SDK safe-mode missing-target warnings; no error whitelist or foreign-tween cleanup was added.

Frozen source hashes match the previous CameraShake fixture `3bf80889969ce75fad80d0ae5d934f54b60dad074794a641c972710cce4677f9`, unchanged navigation fixture `61862a9fc93694d438792e73ec9e75899dbc93918227e2b8e3da8450f92b2f01`, and unchanged CameraShake runtime `92e85c1a9fa84c8675c874f4ca2eb8b82d1b5f6a9382a798ab6c785f84bd76c2`. These are historical execution facts, not a pass claim for the new fixture/integration.

Installed `Assets/Plugins/Demigiant/DOTween/DOTween.XML` documents that the generic SetUpdate(bool) used by CameraShake sets Normal/independent update. Its independent-clock formula still multiplies DOTween.timeScale, DOTween.unscaledTimeScale and tween.timeScale. Read the shipped module's public DOTween.instance usage and the read-only settings asset (global clocks1/1, smooth delta off). Those defaults do not establish historical live clocks/driver delivery: the old XML contains no frame/root/global-clock trace. There is **no demonstrated new runtime defect**, and runtime remains unchanged.

The fixture now admits a genuine native independent-clock update before CameraShake starts, then requires further native frame/time progression and a probe-clock window twice the root's 0.05-second duration. Both stages share one monotonic **one-second total deadline**. Root updates/elapsed, probe progress, driver activity, Unity/SDK/root clocks and smooth-clock settings are captured for a failed precondition or completion oracle. Missing native progress is an explicit failure, not Ignore or forced delivery. Completion1/IsShaking=false and subsequent disabled no-restart/started2/completion1 are preserved, with stop-count0/1 added. Probe/global cleanup is owned-only and exact. **Whether this arrangement closes the 6.3 discrepancy remains unverified until the new native run; a source-supported prerequisite correction is not a runtime-vs-fixture verdict.**

## Actual source-phase work and remaining evidence

Read the implementation contract/request (including p5/E14), p5 common contract, independent navigation review/NI52, current relevant owned source/docs, installed SDK references, historical audit/plan and assigned existing XML/index evidence. Inspected source/lifecycle/clock references manually. Before IOTHER-01, scoped `git diff --check` returned exit0; that historical check does not validate this correction. Subsequent continuations used source/artifact/SDK reads, reference enumeration and raw hashes only. Hashes below are bookkeeping, not validation. This worker started no compiler, Unity/MCP, test runner, product/syntax checker, service/install, process control, commit/push or child actor.

| Source-final file | SHA-256 |
|---|---|
| `AiNavigation.cs` | `f0f13dd23518daed30ade535c8490a5db7f5e0da2e43f06d1613e981863955dc` |
| `CameraShake.cs` | `92e85c1a9fa84c8675c874f4ca2eb8b82d1b5f6a9382a798ab6c785f84bd76c2` |
| `Audit20261023NeoToolsOtherNavigationTests.cs` | `61862a9fc93694d438792e73ec9e75899dbc93918227e2b8e3da8450f92b2f01` |
| `Audit20261023NeoToolsOtherCameraShakeTests.cs` | `24f6248e7ee44f826fd48a72198a1c9149394c814a064934f4bdf8e65b2174ff` |
| `CameraShake.md` | `358a562615a661f44862b343f3badaf4e4a43ae5d26f7e5dfa9a456bbc3af325` |
| `AiNavigation.md` | `f6a88a855d731c853732cabcafe77f9918e9e2f30157d77341d3e631328dd763` |
| `Tools/Other/README.md` | `4a1fc333972df0f261e86b63021e9fdefc23ddd6bcc9f517d3e07359838b675d` |

The historical audit/plan raw hashes still match the approved request. AiNavigation runtime and both listed guides retain their previous source-final hashes. Source enumeration is not test discovery.

**No cross-owner source/API dependency remains.** Remaining gate: independent E14 delta review, completion of every writer, new root freeze/execution authorization, then compilation/discovery/native rerun on both approved hosts. Capture the new E14 native probe admission/eligibility and root/global/frame traces; establish completion1, actual pause, no manual clock, disable-no-restart, starts2 and stop1. If readiness/eligibility fails, report that actual host/SDK prerequisite rather than labeling CameraShake broken; if it passes while completion fails, retain the root trace as runtime evidence. Re-run the other seven CameraShake cases and retained 24 navigation/ResetPath/patrol-window controls in the integrated frozen source. Other supported but unavailable versions, optional Spine/vendor/IL2CPP and broader audit risks remain unverified. Historical XML and source review do not prove this revision passes.

The procedural shake framework/bridge remains a proposal. Historical audit/plan are preserved. Compilation, Unity/native acceptance, tests, and product/package validation are deferred until every source writer finishes.

## Root P8 full-order continuation

Root continuation after reviewer usage limit. Before pausing, the unscaled case now admits a genuinely later native frame and successful PUBLIC DOTween.Init with an active native driver. It never changes private SDK quitting/initialized flags or enables a foreign driver. Readiness/probe/root completion share the original one-second deadline. Adds actual initialization state diagnostics; exact completion1/start2/stop1/native clock and reentry assertions remain. No runtime/guide/meta change; actual full-order cause remains qualified until rerun.

Current fixture: Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherCameraShakeTests.cs; SHA256 147eb9417c3e938d88fc83139e97f0b193f6fa65ec023ad81562fdc52dfc3903. Root personally reviewed the complete frozen-to-current delta. Independent new model review was unavailable because the selected subagent usage limit was exhausted; no independent approval or native pass is claimed. Root owns new frozen compilation/targeted/full-order verification.
