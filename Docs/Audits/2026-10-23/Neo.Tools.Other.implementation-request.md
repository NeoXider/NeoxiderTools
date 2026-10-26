# Neo.Tools.Other: approved implementation request

Original actor: `neo-audit-1023-b07-08`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### OTHER-NAV-DUPLICATE-SCHEDULER (P2; approved-source-correction)

SetTarget initial UpdatePath plus immediately running coroutine plus Update scheduler duplicate work/events; single initial query and Update interval schedule.

### OTHER-NAV-NOPATH (P1; bounded-source-investigation-and-native-acceptance-required)

CheckPathStatus hasPath guard excludes PathInvalid/no-path accepted request. Track request identity/destination and pending vs idle; verify native trigger without reflection before final acceptance.

### OTHER-NAV-CALLBACK-GENERATION (P2; approved-source-correction)

A-35: initial onPathUpdated Stop/replacement overwritten by outer SetTarget; initialize before callback and reject stale continuation with request generation.

## Relevant full native sections

Source: NativeReview-A.md

### Neo.Tools.Other — source review complete; duplicate path scheduling/reentry remain

Original owner: `neo-audit-1023-b07-08`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Tools.Other.md` — `05c699013341da234c0f944567c7e47f691beed7955b02d9b412dc673d68c3be`.
- `Docs/Audits/2026-10-23/Neo.Tools.Other.plan.md` — `ae64f90ae0ddcc9cdeead8233c57f86e96e0dd5b9551f4cee410867de912eac9`.
- `Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherCameraShakeTests.cs` — `c1b42dd45022f6a8d880520c230783b142d9c5abe0d1af121651b99c508aa950`.
- `Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherNavigationTests.cs` — `225f235a64c04c3f32ada38687898a4a2b46f1ee9f9c31ac4febf03bd51b167e`.

All three changed source diffs (including both Spine branches), both complete fixtures, five changed guide diffs and relevant path/tween scheduling/ownership paths were read. **Completed fixes appear structurally sound** for root unscaled sequence/named shake arguments, latest-request tween ownership, captured channels, bounded unusable routes, tracked patrol waits, accepted path/warp readiness, target retention, arrival reset, gated diagnostics and conditional Spine initialization/state publication. Fixtures drive actual NavMesh islands/native movement or real DOTween transforms, with bounded waits and owned NavMesh cleanup/timeScale restoration; none ran. Navigation's temporary SerializedObject in VerifyPatrolCancellation should use a using scope to release its native wrapper. Optional Spine has no compiled evidence/vendor runtime here; do not install one as part of this review.

**A-34, P2, confirmed aggregate duplicate work:** `AiNavigation.SetTarget:492,497–499` calls UpdatePath then starts PathUpdateRoutine, whose first MoveNext immediately calls UpdatePath again at935; UpdatePathfinding:915–922 is a second interval scheduler. Expected one accepted initial submission/event and at most one automatic request per configured interval. Actual one SetTarget submits/notifies twice immediately and can duplicate periodic native path work across agents. Minimal repair: retain one Update interval scheduler and explicit initial request, preserve AutoUpdatePath/pause/disable/mode semantics. Later tests count accepted initial/path events and interval activity; no new navigation manager is needed. This is the original plan's confirmed duplicate path.

**A-35, P2, confirmed callback continuation:** `AiNavigation.SetTarget:492–499` calls UpdatePath, which invokes `onPathUpdated:960`, then resets hasStopped/reached/blocked state and may start its coroutine. An onPathUpdated listener Stop() sets hasStopped=true/isMovementPaused=true; the outer request resumes and sets hasStopped=false, disagreeing with the paused/stopped agent and enabling CheckPathStatus's active-state branch. Expected callback-issued Stop or replacement wins. Minimal repair: initialize operation state before callback and guard post-callback continuation with a request generation/current state, including SetTarget replacement; add public callback→Stop/replacement assertions alongside native agent state. Do not invent whole-engine atomic callbacks or change ordinary accepted-request event order.

**Needs evidence, retained P1 lead:** CheckPathStatus:995 requires hasPath before its PathInvalid branch. A ready agent with an accepted active request whose resolution loses all path is ignored, but actual engine trigger/transcript remains unexecuted. Track accepted request/destination and distinguish pending/idle/stopped before any narrow no-path repair; do not remove the hasPath guard indiscriminately or mock native flags. Rotated/scaled patrol zones, destroy during tween callbacks and optional Spine external-track ownership also remain evidence leads.

Aggregate cost: every agent polls native movement/path/animation state each Update and currently has duplicate path schedulers (A-34); patrol zone sampling and path calculations are request/interval work. Existing scaled gameplay time/route mechanics stay intact. Camera shake owns one sequence plus children, allocates per request and restores captured local channels; composing multiple transform writers requires the documented pivot. Spine lookup/list generation is initialization and event work. The amended procedural shake proposal correctly reuses Neo.Animations' existing sampler and remains **future optional bridge work**, not a required new noise generator or dependency in base Other. Native NavMesh/Spine/IL2CPP, real delays and rendering remain **needs evidence**.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Other.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root accepted auto refresh starvation of settled status

ROOT personally FULL-read all158lines IndependentImplementationReview-OtherNavigation.md andactualAiNavigation345–359/897–987; accept IOTHER-01/P2. SAMEoriginalowner boundedordering/refreshdeferral fix: whenvalidpathUpdateInterval0.1 andscaledframeincrements0.15, UpdatePathfinding resubmits/stampscurrentframeBEFORECheckPathStatus everyframe, guardthenreturnsforever evenifpreviousnativepathsettled. Let previousacceptedsettledstatebeobserved BEFORE optionalautomaticresubmit; preserveinitial/pending/submission-frameprotection, accepted destination/generation/callbackStop/replacement, singleUpdateintervalscheduler/reservebeforecallbacks. IfreorderingintroducescallbacksBEFOREremainingUpdate work, fenceobsolete/disabled/destroyedcontinuation beforeautomatic/animationtail; no newframework/publictestseam orblanketremovalframeguard. MeaningfulrealPlayerLoop/nativeagentpublicSetTarget case withintervalACTUALLYdueeveryobservedframe (assertframe/timecadence), readysettlednativearrival/exact1/publicReachedwhileautorefreshstillenabled; boundedrealtime andexactrestorepublictimingcontrols, no reflection/manualUpdate/mocknativeflags. Ordinarycadence/moving/stationaryonce controls andfreshframe/pending/Stop/disable/callbackreplacementnegatives retained, actualResetPathpositivecontrols preserved. Existing19nav+8CameraShakecases unaffectedexceptnecessaryadditions. Ownruntime/existingfixture/2guides/receipt only, noexecution untilglobalfreeze.

## Snapshot3 actual OTHER-NATIVE-NAVIGATION-AND-SHAKE

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. 16 own Edit groups:7 CameraShake illegal SendMessage Awake;7 navigation CreateNavigation Warp/native attachment positive false;2 same-frame count expected1 actual0. Use genuine admitted lifecycle/real frame barriers, correct fixture scheduling if needed and preserve exact consecutive-frame/due-every-frame auto refresh/arrival replacement/callbackStop/destroy controls. No manual Update, reflection fabricated path status, disabling automatic refresh or changing movement cadence to pass. Keep both captured transform shake channels/relative and nonrelative restoration.

## NI52-01 scaled native post-cancel patrol window

SAME original b07-08/session6.1sol/max. Root FULL-read NI52all71lines SHA7a175eda... andcurrent96linereceipte5603015... . Confirmed P2 NI52-01 shared VerifyPatrolCancellation913-954 inNavigationTestsb41dc97f... servesfour StopPatrol/Stop/Disable/ClearingWaypoints cases. Realinitialarrivalpositive stays, but WaitForSecondsRealtime(.35) aftercancel doesnotprove pendingauthored.2SCALED WaitAtPatrolPoint continuation everbecameeligible inEditor-assemblyUnityTest. FixONLYthissharedhelper/localwaithelper/ownreceipt: boundedactualnativeframe ANDscaledtime post-cancelwindow exceedingauthored.2 (retainexisting.35 scaledwindow or explicitlyequivalentsufficient interval) plus subsequentreal execution opportunity, withunscaled/monotonic faildeadline. Keep EXACTrouteindex0/stopped/IsPatrolling/reached1 negatives and existingResume/index1 positive/all32cases; no nativeprogress=>explicitfailedprecondition, notskip. No cadence/runtime/default/waitTime change, morearbitrarynullpolls, manualUpdate/privatePlayerLoop, framework/Ignore/relaxedvalues. Existingfast5realframebarriers/CameraShake/API docs unchanged. Preserveownedobjects/NavMesh/timeglobals cleanup/acquiredtopPlayexit/GUIDs/helpers/sharedrefs/otherowners. Accuratecurrentreceipt/hashes/source-onlyevidence; noUnity/compiler/tests/checkers/processcontrol/install/children/commit/push untilALLwriters/newfreeze. Finishsourceandstopforindependentdelta review.

## P5 native unscaled CameraShake cross version discrepancy

Read Local/Audits/assembly-2026-10-23/p5-source-correction-common.md. SAME b07-08, E14. Actual UnscaledShake_CompletesWhilePaused_AndDisableCannotRestartIt215 completion0 instead1 on6.3, PASSES6.6, same source. Runtime already Sequence.SetUpdate(useUnscaledTime). Trace owned root Sequence/global clocks/native frame/driver prerequisites before runtime-vs-fixture conclusion. Preserve exact completion1/stopped/disable-no-restart/start2 controls, source-owned resources/global-state restoration. Arbitrary timeout extension or manual DOTween update not accepted as native fix. Work only CameraShake bounded issue, retain prior patrol cancellation/other utility fixes. Own source/docs/receipt. No execution.
