# Native implementation review 33 — Other navigation IOTHER-01 correction

Status: **SOURCE REVIEW COMPLETE; IOTHER-01 correction supported in source. No further bounded source defect found in this delta. All compilation, tests and native cadence/path outcomes remain UNEXECUTED.**

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. Own only this new report. No product/public-doc/config/other-report edit, Unity/MCP/API invocation, compiler/test/syntax/product checker, process probe/management, installer/service, children, commit or push. The gate remains closed while the primary Editor.Tests correction finishes; this report does not authorize execution.

## Admission and exact scope

Read the complete **158-line** IndependentImplementationReview-OtherNavigation, complete latest **79-line source-final receipt**, complete 52-line approved request, and both complete current guides. Root confirmed latest original writer 42592 exited; exact `neo-audit-1023-b07-08.meta` reads done/0 for the original session. No process was queried. Receipt's four current source/test/guide hashes match the admitted files.

Initial gate **2026-10-23T22:45:52.844Z**, closing **2026-10-23T22:52:46.835Z**: **11/11 scoped identities match**. Baseline source evidence is the independent report's runtime `93cbe119ee66f234de1518064d96c979a76cf583f27a64af1def5e3b25b46701` and navigation fixture `1b9694baf20206181689fe7090159135cd3f9c802919a6dca8b616a25fffd887`. Current runtime is `f0f13dd23518daed30ade535c8490a5db7f5e0da2e43f06d1613e981863955dc`, current fixture `2721d4235aa9f50efbe69a5ad43ccd9aeebf37d7b7f8f52bc00be8ee59f54b97`.

This is the bounded IOTHER status/scheduler/frame-tail correction, not another whole Other/Spine/NPC audit. Read current Update/status/scheduler/private submit and all relevant setter/lifecycle/Combined/reentry continuations. Read **the complete current 1011-line navigation fixture**, all five new cases/helpers and all retained 19 bodies, plus the unchanged complete 246-line/eight-case CameraShake fixture for retained oracle/cleanup context. Original audit/plan retain their recorded identities; their earlier broad source coverage is not newly claimed here.

## IOTHER-01 / P2 — previous settled status is observed before refresh

Owner: SAME **Neo.Tools.Other / `neo-audit-1023-b07-08`**. The prior valid interval 0.1/scaled frame 0.15 trace submitted each frame before CheckPathStatus, overwriting requestSubmittedFrame and excluding every observation.

Current `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs:348` captures requestVersion, executes mode work, and rejects a changed/disabled/Unity-null continuation. It then calls **CheckPathStatus at 363 before UpdatePathfinding at 369**. A prior settled request therefore reaches classification before an optional accepted refresh stamps the current frame.

The existing status admission at 996–1002 still excludes inactive/off-mesh/paused/stopped/idle, native pathPending and submission-frame requests. Captured request identity/destination, no-path proximity versus blocked/partial classification and post-callback guards remain. No blanket removal of pending/frame protection was used.

After mode/status callbacks, Update's version/lifetime fences at 357/364 stop obsolete continuation into automatic path work or animation. Stop invalidates identity and clears active observation; disable advances identity; accepted target/destination replacement advances identity; destroyed-adapter checks precede subsequent native-backed accesses. CheckPathStatus's own guard at 1039 suppresses stale trailing status, and the outer guard also catches a command issued from its final status event.

Automatic refresh now returns continuation eligibility through private bool UpdatePathfinding/UpdatePath. The interval is still reserved before callback/native retry work; successful TrySubmitPath captures destination/frame/scaled time before onPathUpdated. The helper captures the resulting request version and returns false after callback Stop/replacement/disable/destruction, so Update cannot continue into its animation tail. The direct setter/Resume callers have no post-helper stale mutation.

There remains one automatic Update scheduler, one explicit initial accepted submission and no catch-up loop/coroutine. Stationary refresh preserves hasStopped/HasReached unless an explicit/new sufficiently moved destination rearms it; a reached target cannot repeatedly emit arrival just because it refreshes. Existing Combined callback guards preserve latest-command ownership. No public/serialized migration, new manager, mock native state or transaction framework was introduced.

**The former starvation path is source-closed.** Native settlement, Update/native scheduling and event outcomes are still later acceptance, not inferred from this control-flow conclusion.

## All five new cases — substantive source oracles, UNEXECUTED

| New case | Oracle personally read |
| --- | --- |
| FollowTarget_RefreshDueEveryFrame_ObservesNativeArrivalOnce, line 131 | Real EnterPlayMode/NavMesh/agent and public SetTarget; interval 0.1, captureDeltaTime 0.15, consecutive actual frame IDs and observed scaled delta/time. Every observed frame must have exactly one accepted refresh. Requires real travel, settled native arrival/captured destination, public reached state, zero blockage, exactly one arrival plus four later stationary frames while refresh remains enabled. |
| ArrivalListenerStop_CancelsTheDueAutomaticRefresh, line 221 | Actual arrival callback calls public Stop; asserts public/native stopped state, no due-frame or later path update and four later frames. |
| ArrivalListenerDisable_CancelsTheDueAutomaticRefresh, line 233 | Actual arrival callback disables the adapter, retains reached state and cancels remaining/later automatic refresh. |
| ArrivalListenerDestroy_CancelsTheDueAutomaticRefresh, line 244 | Actual arrival callback destroys only the adapter immediately; no stale refresh, and the independent native agent remains on NavMesh. |
| ArrivalListenerReplacement_PreservesTheNewRequestAndItsArrival, line 254 | Actual first native arrival submits a replacement with public SetTarget; immediate new target/moving/unreached state and one synchronous accepted replacement, then its own second native arrival exactly once plus four stationary frames. |

Read the shared cancellation enumerator at 780–839, frame override/restore at 841–863, serialized valid-interval authoring at 865–872, due-frame assertions at 874–881 and native-arrival assertions at 883–894. No reflection, manual Update call or fabricated native flag is used by these navigation additions. SerializedObject only configures the real Inspector interval in a disposed scope.

The due test does not merely request a slow frame: it **asserts** frameCount increments 1, Time.time and Time.deltaTime 0.15 within tolerance and elapsed>=interval on every observed frame. It also records accepted submission frame/time arrays and requires their exact per-frame cadence. Native assertions require on-mesh, nonpending, complete remaining path when present and actual position near the captured destination. The travel assertion prevents an already-at-target shortcut. These would expose the original accepted-refresh starvation without disabling AutoUpdatePath.

Every new frame-controlled wait has a five-second realtime deadline. UseFramesDueEveryInterval captures exact prior public timeScale/captureDeltaTime/maximumDeltaTime, applies the test cadence, and each try/finally restores all three. UnityTearDown provides fallback restoration before destroying owned objects, removing only the owned NavMeshDataInstance/data and yielding ExitPlayMode. No global NavMesh removal or unavailable NUnit serial attribute is added. Shared timing/NavMesh tests still require isolated/exclusive execution.

## Retained controls, docs and limits

Readonly removal of only the new fixture fields, teardown restore, five added cases and timing/cancellation helpers reproduces the **exact 693-line previous fixture SHA 1b9694ba…**. This is raw source identity bookkeeping, not a test run. All 19 earlier navigation cases are retained. In particular ResetPath controls first require an actual native path on a ready nonpending agent outside stopping distance, call the real ResetPath, then assert one captured blocked/invalid episode and recovery. Idle/Stop/current-position negatives, complete/partial/moving-target arrival, initial/minimum cadence, false/true AutoUpdatePath, disable/re-enable and patrol cancellation remain meaningful.

The eight CameraShake cases retain exact hash `c1b42dd45022f6a8d880520c230783b142d9c5abe0d1af121651b99c508aa950`. They observe real DOTween sequence/transform/channel/reentry/unscaled outcomes with owned cleanup; their existing reflective sequence observation/manual Edit initialization is unchanged and is not attributed to the new navigation tests. No fresh CameraShake runtime rewrite or vendor approval is implied.

Current inventories: **24 navigation + 8 CameraShake = 32 authored cases**, all UNEXECUTED. Both guides correctly describe settled-status-before-refresh, captured destination/event order, callback cancellation of the frame tail, one scaled scheduler and independent native-agent lifetime. C# and Inspector paths/backlinks remain. The intentional order is arrival/blocked/status before that frame's optional onPathUpdated, preserving status ordering within the accepted request.

Aggregate work stays per active agent: mode/native status/animation polling plus at most one interval refresh per Update, without duplicate periodic scheduler/catch-up. No shared-path/query backend or measured CPU/FPS claim is added. Optional shake bridge/NPC migration/Spine/platform policies remain outside this delta.

No additional source correction is requested. After all source writers finish and root explicitly freezes, compile/discover/run the updated 32 cases and existing OtherToolsBehaviorTests on Unity 6.3/6.6. Preserve actual per-frame submission/time/native readiness/arrival transcripts, ResetPath complete/partial/no-path evidence, callback tail/animation/native destruction behavior and cleanup. The already-at-destination case still does not prove the engine specifically chose hasPath=false; native flags must be observed later. Source counts/hash agreement do not close any native/compiler/version capability debt.

## Raw identities

| Full path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/IndependentImplementationReview-OtherNavigation.md` | 21507 | `3c5b6c98d723166ddb6f18a57c1366a26c1bd527271395c0f0332051eaa781ce` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Other.implementation.md` | 12398 | `e733c5eadedacf5a07ef0a5d2f3ac31eb1d8c90978543c54d28393449bcf5015` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Other.implementation-request.md` | 7737 | `9c5fc957d6f1d5dc9020bb73eca188bd62cf5360b93fd92b7f045231a8226bb6` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b07-08.meta` | 233 | `95aa64d7966ff9426280305679582d212c843a043ad892f24c04c63529d5f7cb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs` | 46950 | `f0f13dd23518daed30ade535c8490a5db7f5e0da2e43f06d1613e981863955dc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherNavigationTests.cs` | 45126 | `2721d4235aa9f50efbe69a5ad43ccd9aeebf37d7b7f8f52bc00be8ee59f54b97` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherCameraShakeTests.cs` | 9133 | `c1b42dd45022f6a8d880520c230783b142d9c5abe0d1af121651b99c508aa950` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Other/AiNavigation.md` | 7680 | `f6a88a855d731c853732cabcafe77f9918e9e2f30157d77341d3e631328dd763` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Other/README.md` | 1659 | `4a1fc333972df0f261e86b63021e9fdefc23ddd6bcc9f517d3e07359838b675d` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Other.md` | 16015 | `05c699013341da234c0f944567c7e47f691beed7955b02d9b412dc673d68c3be` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Other.plan.md` | 14217 | `ae64f90ae0ddcc9cdeead8233c57f86e96e0dd5b9551f4cee410867de912eac9` |
